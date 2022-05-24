param(
   [Parameter(Mandatory=$true, Position=0)]
   [string]$ProjectName,
   [Parameter(Mandatory=$true, Position=1)]
   [ValidateSet('major','minor','patch', 'pre')]
   [string]$Increment
)

$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

$solutionPath = Resolve-Path ..
$solution = "$solutionPath\XCST.sln"
$configuration = "Release"

function BuildProj($target) {

   $pack = $target -eq "Pack"

   if ($pack) {

      $itemXml = "<ItemGroup xmlns='$($project.doc.DocumentElement.NamespaceURI)'>
         <None Include='$solutionPath\LICENSE.txt' Pack='true' PackagePath=''/>
         <None Include='$tempNotice' Pack='true' PackagePath=''/>
         <None Include='$(Resolve-Path icon.png)' Pack='true' PackagePath=''/>
      </ItemGroup>"

      $itemReader = [Xml.XmlReader]::Create((New-Object IO.StringReader $itemXml))
      $itemReader.MoveToContent() | Out-Null
      $itemNode = $project.doc.ReadNode($itemReader)
      $project.doc.DocumentElement.AppendChild($itemNode) | Out-Null
      $itemNode.RemoveAttribute("xmlns")

      $project.doc.Save($project.file)
   }

   MSBuild $project.file /t:$target /v:minimal `
      /p:Configuration=$configuration `
      /p:AssemblyVersion=$assemblyVersion `
      /p:FileVersion=$pkgVersion `
      /p:VersionPrefix=$pkgVersion `
      /p:VersionSuffix=$versionSuffix `
      /p:ContinuousIntegrationBuild=true `
      /p:Product=$($notice.work) `
      /p:Copyright=$($notice.copyright) `
      /p:Company=$($notice.website) `
      /p:Authors=$($notice.authors) `
      /p:PackageLicenseExpression=$($notice.license.name) `
      /p:PackageProjectUrl=$($notice.website) `
      /p:PackageOutputPath=$outputPath `
      /p:RepositoryBranch=$(git branch --show-current) `
      /p:PackageIcon=icon.png

   if ($pack) {
      $project.doc.DocumentElement.RemoveChild($itemNode) | Out-Null
      $project.doc.Save($project.file)
   }
}

function PackageNotice {

   Add-Type -AssemblyName System.Xml.Linq
   $noticeDoc = [Xml.Linq.XDocument]::Load("$solutionPath\NOTICE.xml")

   foreach($fw in $noticeDoc.Root.Elements("foreign-work").Where({$_.Attribute("type").Value -eq "source"})) {
      if ($fw.Elements("used-by-source").Where({
         [string]::Join('/', $_.Attribute("path").Value.Split('/', [StringSplitOptions]::RemoveEmptyEntries)) -eq ("src/" + $project.name)}, 'First').Count -eq 0) {

         # Only include notices for dependencies used by the project
         $fw.Remove()
      }
   }

   $noticeDoc.Root.Descendants("foreign-work").Where({$_.Attribute("type").Value -eq "object"}) | %{$_.Remove()}
   $ubs = [Collections.Generic.List[object]]$noticeDoc.Root.Descendants("used-by-source")
   $ubs | %{$_.Remove()}

   $tempNotice = $project.temp + "\NOTICE.xml"
   $noticeDoc.Save($tempNotice)

   return $tempNotice
}

function NuPack {

   if (-not (Test-Path nupkg -PathType Container)) {
      md nupkg | Out-Null
   }

   $outputPath = Resolve-Path nupkg
   $tempNotice = PackageNotice

   BuildProj "Pack" | Out-Null

   return Join-Path $outputPath "$($project.name).$pkgVer.nupkg"
}

function ProjectData([string]$projName) {

   $project = @{ }
   $project.name = $projName
   $project.path = Resolve-Path $solutionPath\src\$($project.name)
   $project.file = "$($project.path)\$($project.name).csproj"
   $project.doc = [xml](Get-Content $project.file)
   $project.temp = Join-Path (Get-Item .) temp\$($project.name)

   if (-not (Test-Path $project.temp -PathType Container)) {
      md $project.temp | Out-Null
   }

   return $project
}

function Release {

   if ($Increment -eq "major" -and $ProjectName -ne "*") {
      throw "Major increment should release all packages."
   }

   $lastTag = .\last-tag.ps1
   $lastVer = $lastTag -replace "^v|-.+$", ""
   $lastVersion = New-Object Version $lastVer

   $pkgVersion = if ($Increment -eq "major") {
      New-Object Version ($lastVersion.Major + 1), 0, 0
   } elseif ($Increment -eq "minor") {
      New-Object Version ($lastVersion.Major), ($lastVersion.Minor + 1), 0
   } elseif ($Increment -eq "patch")  {
      New-Object Version ($lastVersion.Major), ($lastVersion.Minor), ($lastVersion.Build + 1)
   } else {
      New-Object Version ($lastVersion.Major), ($lastVersion.Minor), ($lastVersion.Build), ($lastVersion.Revision + 1)
   }

   $versionSuffix = $null
   $pkgVer = $pkgVersion.ToString($(if ($Increment -eq "pre") { 4 } else { 3 }))

   if ($Increment -eq "pre") {
      $versionSuffix = "pre"
      $pkgVer = $pkgVer + "-" + $versionSuffix
   }

   if ($pkgVersion -lt $lastVersion) {
      throw "The package version ($pkgVersion) cannot be less than the last tag ($lastTag)."
   }

   $assemblyVersion = if ($pkgVersion.Major -eq 0) {
      New-Object Version 1, 0, 0
   } else {
      New-Object Version ($pkgVersion.Major), ($pkgVersion.Minor), 0
   }

   if (-not (Test-Path temp -PathType Container)) {
      md temp | Out-Null
   }

   [xml]$noticeDoc = Get-Content $solutionPath\NOTICE.xml
   $notice = $noticeDoc.notice

   # Make sure everything builds first
   ""
   MSBuild $solution /p:Configuration=$configuration /verbosity:minimal

   $projects = "Xcst.Runtime", "Xcst.Compiler"
   $projectsToRelease = if ($ProjectName -eq '*') { $projects } else { @($ProjectName) }
   $newPackages = New-Object Collections.Generic.List[string]
   $createdTag = $false

   foreach ($projName in $projectsToRelease) {

      $project = ProjectData $projName

      ""
      $newPackages.Add((NuPack))

      if (-not $createdTag -and
            $pkgVersion -gt $lastVersion -and
            (Prompt-Choices -Message "Create tag?" -Default 1) -eq 0) {

         $newTag = "v$pkgVer"
         git tag -a $newTag -m $newTag
         Write-Warning "Created tag: $newTag"
         $createdTag = $true
      }
   }

   if ($createdTag) {

      if ((Prompt-Choices -Message "Push package(s) to gallery?" -Default 1) -eq 0) {
         foreach ($pkgPath in $newPackages) {
            &$nuget push $pkgPath -Source nuget.org
         }
      }

      if ((Prompt-Choices -Message "Push new tag $newTag to origin?" -Default 1) -eq 0) {
         git push origin $newTag
      }
   }
}

function Prompt-Choices($Choices=("&Yes", "&No"), [string]$Title="Confirm", [string]$Message="Are you sure?", [int]$Default=0) {

   $choicesArr = [Management.Automation.Host.ChoiceDescription[]] `
      ($Choices | % {New-Object Management.Automation.Host.ChoiceDescription $_})

   return $host.ui.PromptForChoice($Title, $Message, $choicesArr, $Default)
}

try {

   $nuget = .\ensure-nuget.ps1
   MSBuild $solution -t:Restore
   Release
   
} finally {
   Pop-Location
}
