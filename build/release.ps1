param(
   [Parameter(Mandatory=$true, Position=0)][string]$ProjectName
)

$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

$nuget = "..\.nuget\nuget.exe"
$solutionPath = Resolve-Path ..
$configuration = "Release"

function script:PackageVersion([string]$projName) {
   
   $assemblyPath = Resolve-Path $solutionPath\src\$projName\bin\$configuration\$projName.dll
   $fvi = [Diagnostics.FileVersionInfo]::GetVersionInfo($assemblyPath.Path)
   return New-Object Version $fvi.FileVersion
}

function script:DependencyVersionRange([string]$projName) {

   $dependencyVersion = PackageVersion $projName
   $minVersion = $dependencyVersion
   $maxVersion = New-Object Version $minVersion.Major, ($minVersion.Minor + 1), 0

   return "[$minVersion,$maxVersion)"
}

function script:NuSpec {

   $packagesPath = "$projPath\packages.config"
   [xml]$packagesDoc = if (Test-Path $packagesPath) { Get-Content $packagesPath } else { $null }

   "<package xmlns='http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd'>"
      "<metadata>"
         "<id>$projName</id>"
         "<version>$pkgVersion</version>"
         "<authors>$($notice.authors)</authors>"
         "<licenseUrl>$($notice.license.url)</licenseUrl>"
         "<projectUrl>$($notice.website)</projectUrl>"
         "<copyright>$($notice.copyright)</copyright>"
         "<iconUrl>$($notice.website)nuget/icon.png</iconUrl>"

   if ($projName -eq "Xcst") {

      "<description>XCST runtime API. For compilation install the Xcst.Compiler package.</description>"

      "<frameworkAssemblies>"
         "<frameworkAssembly assemblyName='System'/>"
         "<frameworkAssembly assemblyName='System.Xml'/>"
      "</frameworkAssemblies>"

   } elseif ($projName -eq "Xcst.Compiler") {

      "<description>XCST compiler API. Use this package to translate your XCST programs into C# code.</description>"

      "<dependencies>"
         "<dependency id='Xcst' version='$(DependencyVersionRange Xcst)'/>"
         "<dependency id='Saxon-HE' version='$($packagesDoc.DocumentElement.SelectSingleNode('package[@id=''Saxon-HE'']').Attributes['allowedVersions'].Value)'/>"
      "</dependencies>"

      "<frameworkAssemblies>"
         "<frameworkAssembly assemblyName='System'/>"
         "<frameworkAssembly assemblyName='System.Xml'/>"
      "</frameworkAssemblies>"

   } elseif ($projName -eq "Xcst.Web.Mvc") {

      "<description>XCST view engine for ASP.NET MVC 5.</description>"

      "<dependencies>"
         "<dependency id='Xcst.Compiler' version='$(DependencyVersionRange Xcst.Compiler)'/>"
         "<dependency id='Microsoft.AspNet.Mvc' version='$($packagesDoc.DocumentElement.SelectSingleNode('package[@id=''Microsoft.AspNet.Mvc'']').Attributes['allowedVersions'].Value)'/>"
      "</dependencies>"

      "<frameworkAssemblies>"
         "<frameworkAssembly assemblyName='System'/>"
         "<frameworkAssembly assemblyName='System.Web'/>"
      "</frameworkAssemblies>"
	
   } elseif ($projName -eq "Xcst.AspNet") {
      
      "<description>XCST pages for ASP.NET.</description>"

      "<dependencies>"
         "<dependency id='Xcst.Compiler' version='$(DependencyVersionRange Xcst.Compiler)'/>"
         "<dependency id='Microsoft.Web.Infrastructure' version='$($packagesDoc.DocumentElement.SelectSingleNode('package[@id=''Microsoft.Web.Infrastructure'']').Attributes['allowedVersions'].Value)'/>"
      "</dependencies>"

      "<references>"
         "<reference file='AspNetLib.AntiXsrf.dll'/>"
         "<reference file='AspNetLib.Mvc.dll'/>"
         "<reference file='AspNetLib.Mvc.DataAnnotations.dll'/>"
         "<reference file='AspNetLib.Mvc.ModelBinding.dll'/>"
         "<reference file='AspNetLib.Mvc.ViewEngine.dll'/>"
         "<reference file='AspNetLib.Mvc.ViewEngine.Compilation.dll'/>"
         "<reference file='Xcst.AspNet.dll'/>"
      "</references>"

      "<frameworkAssemblies>"
         "<frameworkAssembly assemblyName='System'/>"
         "<frameworkAssembly assemblyName='System.Web'/>"
      "</frameworkAssemblies>"
   }

   "</metadata>"

   "<files>"
      "<file src='$tempPath\NOTICE.xml'/>"
      "<file src='$solutionPath\LICENSE.txt'/>"
      "<file src='$projPath\bin\$configuration\$projName.*' target='lib\$targetFxMoniker'/>"

      if ($projName -eq "Xcst.AspNet") {
         "<file src='$projPath\bin\$configuration\AspNetLib.*' target='lib\$targetFxMoniker'/>"
      }

   "</files>"

   "</package>"
}

function script:NuPack([string]$projName) {

   if (-not (Test-Path temp -PathType Container)) {
      md temp | Out-Null
   }

   if (-not (Test-Path temp\$projName -PathType Container)) {
      md temp\$projName | Out-Null
   }

   if (-not (Test-Path nupkg -PathType Container)) {
      md nupkg | Out-Null
   }

   $tempPath = Resolve-Path temp\$projName
   $nuspecPath = "$tempPath\$projName.nuspec"
   $outputPath = Resolve-Path nupkg

   [xml]$projDoc = Get-Content $projFile

   [string]$targetFx = $projDoc.Project.PropertyGroup.TargetFrameworkVersion
   $targetFxMoniker = "net" + $targetFx.Substring(1).Replace(".", "")

   NuSpec | Out-File $nuspecPath -Encoding utf8

   $saxonPath = Resolve-Path $solutionPath\packages\Saxon-HE.*
   &"$saxonPath\tools\Transform" -s:$solutionPath\NOTICE.xml -xsl:pkg-notice.xsl -o:$tempPath\NOTICE.xml projectName=$projName

   &$nuget pack $nuspecPath -OutputDirectory $outputPath
}

function script:Release([string]$projName) {
   
   $projPath = Resolve-Path $solutionPath\src\$projName
   $projFile = "$projPath\$projName.csproj"

   MSBuild $projFile /p:Configuration=$configuration

   $lastTag = git describe --abbrev=0 --tags
   $lastRelease = New-Object Version $lastTag.Substring(1)
   $pkgVersion = PackageVersion $projName

   if ($pkgVersion -lt $lastRelease) {
      throw "The package version ($pkgVersion) cannot be less than the last tag ($lastTag). Don't forget to update the project's AssemblyInfo file."
   }

   NuPack $projName

   if ($pkgVersion -gt $lastRelease) {
      $newTag = "v$pkgVersion"
      git tag -a $newTag -m $newTag
      Write-Warning "Created tag: $newTag (don't forget to push)"
   }
}

try {

   ./ensure-nuget.ps1
   ./restore-packages.ps1
   
   [xml]$noticeDoc = Get-Content $solutionPath\NOTICE.xml
   $notice = $noticeDoc.DocumentElement

   Release $ProjectName

} finally {
   Pop-Location
}