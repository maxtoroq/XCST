param(
   [Parameter(Mandatory=$true, Position=0)][string]$ProjectName
)

$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

$solutionPath = Resolve-Path ..
$configuration = "Release"

function PackageVersion($project) {
   
   $assemblyPath = if ($project.sdkStyle) { Resolve-Path "$($project.path)\bin\$configuration\$($project.targetFx.Split(';')[0])\$($project.assemblyName).dll" }
      else { Resolve-Path "$($project.path)\bin\$configuration\$($project.assemblyName).dll" }

   $fvi = [Diagnostics.FileVersionInfo]::GetVersionInfo($assemblyPath.Path)
   return New-Object Version $fvi.FileVersion
}

function DependencyVersionRange($project) {

   $dependencyVersion = PackageVersion $project
   $minVersion = $dependencyVersion
   $maxVersion = New-Object Version $minVersion.Major, ($minVersion.Minor + 1), 0

   return "[$minVersion,$maxVersion)"
}

function NuSpec {

   $packagesPath = "$($project.path)\packages.config"
   [xml]$packagesDoc = if (Test-Path $packagesPath) { Get-Content $packagesPath } else { $null }

   "<package xmlns='http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd'>"
      "<metadata>"
         "<id>$($project.name)</id>"
         "<version>$pkgVersion</version>"
         "<description>$($project.nuspec.package.metadata.description)</description>"
         "<authors>$($notice.authors)</authors>"
         "<license type='expression'>$($notice.license.name)</license>"
         "<projectUrl>$($notice.website)</projectUrl>"
         "<copyright>$($notice.copyright)</copyright>"
         "<icon>icon.png</icon>"
         "<repository type='git' url='$(git remote get-url origin)' branch='$(git branch --show-current)' commit='$(git rev-parse HEAD)'/>"

   $dependencies = $project.nuspec.package.metadata.dependencies

   if ($dependencies) {

      $depsCopy = $dependencies.CloneNode($true)

      foreach ($dep in $depsCopy.SelectNodes("//*[local-name() = 'dependency']")) {

         $local = $dep.GetAttribute("local-dependency", "http://maxtoroq.github.io/XCST/nuspec")
         $version = $null

         if ($local -eq 'yes') {
            $version = DependencyVersionRange $solution[$dep.id]

         } elseif ($packagesDoc -ne $null) {

            $pkgRef = $packagesDoc.packages.package | where { $_.id -eq $dep.id }
            $version = $pkgRef.allowedVersions

            if ($version -eq $null) {
               $version = $pkgRef.version
            }

         } else {

            # $project.sdkStyle should be true
            $pkgRef = $project.doc.SelectSingleNode("//PackageReference[@Include = '$($dep.id)']")
            $version = $pkgRef.Version
         }

         $dep.SetAttribute("version", $version)
      }

      $depsCopy.OuterXml
   }

   $project.nuspec.package.metadata.frameworkAssemblies.OuterXml

      "</metadata>"

   "<files>"
      "<file src='$($project.temp)\NOTICE.xml'/>"
      "<file src='$solutionPath\LICENSE.txt'/>"
      "<file src='$(Resolve-Path icon.png)'/>"

   if ($project.sdkStyle) {
      foreach ($tf in $project.targetFx.Split(';')) {
         $binPath = "$($project.path)\bin\$configuration\$tf"
      "<file src='$binPath\$($project.assemblyName).*' target='lib\$tf' exclude='**\*.deps.json'/>"
      }
   } else {
      $binPath = "$($project.path)\bin\$configuration"
      "<file src='$binPath\$($project.assemblyName).*' target='lib\$($project.targetFx)'/>"
   }


   foreach ($file in $project.nuspec.package.files.file) {
      foreach ($path in Resolve-Path (Join-Path $project.path $file.src)) {
      "<file src='$path' target='$($file.target)'/>"
      }
   }

   "</files>"

   "</package>"
}

function NuPack {

   if (-not (Test-Path nupkg -PathType Container)) {
      md nupkg | Out-Null
   }

   $noticePath = "$($project.temp)\NOTICE.xml"
   $nuspecPath = "$($project.temp)\$($project.name).nuspec"
   $outputPath = Resolve-Path nupkg
   $saxonPath = (Resolve-Path $solutionPath\packages\Saxon-HE.*)[0]

   &"$saxonPath\tools\Transform" -s:$solutionPath\NOTICE.xml -xsl:pkg-notice.xsl -o:$noticePath projectName=$($project.name)

   [xml]$noticeDoc = Get-Content $noticePath
   $notice = $noticeDoc.notice

   NuSpec | Out-File $nuspecPath -Encoding utf8

   &$nuget pack $nuspecPath -OutputDirectory $outputPath | Out-Host

   return Join-Path $outputPath "$($project.name).$($pkgVersion.ToString(3)).nupkg"
}

function ProjectData([string]$projName) {

   $project = @{ }
   $project.name = $projName
   $project.path = Resolve-Path $solutionPath\src\$($project.name)
   $project.file = "$($project.path)\$($project.name).csproj"
   $project.doc = [xml](Get-Content $project.file)
   $project.sdkStyle = $project.doc.Project.GetAttributeNode("Sdk") -ne $null
   $project.assemblyName = ((($project.doc.Project.PropertyGroup | select -first 1).AssemblyName, $projName) -ne $null)[0]
   $project.targetFx = if ($project.sdkStyle) { (($project.doc.Project.PropertyGroup | select -first 1) | %{ (($_.TargetFramework, $_.TargetFrameworks) -ne $null)[0] }) }
      else { "net" + ($project.doc.Project.PropertyGroup | select -first 1).TargetFrameworkVersion.Substring(1).Replace(".", "") }

   $project.nuspec = [xml](Get-Content "$($project.path)\$($project.name).nuspec")

   $project.temp = Join-Path (Get-Item .) temp\$($project.name)

   if (-not (Test-Path $project.temp -PathType Container)) {
      md $project.temp | Out-Null
   }

   return $project
}

function Release {

   if (-not (Test-Path temp -PathType Container)) {
      md temp | Out-Null
   }

   [xml]$noticeDoc = Get-Content $solutionPath\NOTICE.xml
   $notice = $noticeDoc.notice

   $projects = "Xcst.Runtime", "Xcst.Compiler"
   $solution = @{ }

   foreach ($projName in $projects) {

      $project = ProjectData $projName
      $solution[$projName] = $project

      $signaturePath = "$($project.temp)\AssemblySignature.cs"
      $signature = @"
using System;
using System.Reflection;

[assembly: AssemblyTitle("$($project.assemblyName)")]
[assembly: AssemblyDescription("$($project.nuspec.package.metadata.description)")]
[assembly: AssemblyProduct("$($notice.work)")]
[assembly: AssemblyCompany("$($notice.website)")]
[assembly: AssemblyCopyright("$($notice.copyright)")]
"@

      $signature | Out-File $signaturePath -Encoding utf8

      $signatureXml = "<ItemGroup xmlns='$($project.doc.DocumentElement.NamespaceURI)'>
         <Compile Include='$signaturePath'>
            <Link>AssemblySignature.cs</Link>
         </Compile>
      </ItemGroup>"

      $signatureReader = [Xml.XmlReader]::Create((New-Object IO.StringReader $signatureXml))
      $signatureReader.MoveToContent() | Out-Null

      $signatureNode = $project.doc.ReadNode($signatureReader)

      $project.doc.DocumentElement.AppendChild($signatureNode) | Out-Null
      $signatureNode.RemoveAttribute("xmlns")

      $project.doc.Save($project.file)
   }

   try {
      ""
      MSBuild $solutionPath\XCST.sln /p:Configuration=$configuration /verbosity:minimal

   } finally {

      foreach ($projName in $projects) {
         $project = $solution[$projName]
         $project.doc.DocumentElement.RemoveChild($project.doc.DocumentElement.LastChild) | Out-Null
         $project.doc.Save($project.file)
      }
   }
   
   $projectsToRelease = if ($ProjectName -eq '*') { $projects } else { @($ProjectName) }
   $newPackages = New-Object Collections.Generic.List[string]
   $createdTag = $false

   foreach ($projName in $projectsToRelease) {

      $project = $solution[$projName]

      ""

      $lastTag = .\last-tag.ps1
      $lastRelease = New-Object Version $lastTag.Substring(1)
      $pkgVersion = PackageVersion $project

      if ($pkgVersion -lt $lastRelease) {
         throw "The package version ($pkgVersion) cannot be less than the last tag ($lastTag). Don't forget to update the project's AssemblyInfo file."
      }

      $newPackages.Add((NuPack))

      if (-not $createdTag -and
            $pkgVersion -gt $lastRelease -and
            (Prompt-Choices -Message "Create tag?" -Default 1) -eq 0) {

         $newTag = "v$pkgVersion"
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
   .\restore-packages.ps1
   Release
   
} finally {
   Pop-Location
}
