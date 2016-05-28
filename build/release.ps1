param(
   [Parameter(Mandatory=$true, Position=0)][string]$ProjectName,
   [Parameter(Mandatory=$true)][Version]$AssemblyVersion,
   [Parameter(Mandatory=$true)][Version]$PackageVersion
)

$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

$nuget = "..\.nuget\nuget.exe"
$solutionPath = Resolve-Path ..

function script:DownloadNuGet {

   $nugetDir = Split-Path $nuget

   if (-not (Test-Path $nugetDir -PathType Container)) {
      md $nugetDir | Out-Null
   }

   if (-not (Test-Path $nuget -PathType Leaf)) {
      write "Downloading NuGet..."
      Invoke-WebRequest https://www.nuget.org/nuget.exe -OutFile $nuget
   }
}

function script:RestorePackages {
   &$nuget restore $solutionPath\XCST.sln
}

function script:NuSpec {

   $packagesPath = "$projPath\packages.config"
   [xml]$packagesDoc = if (Test-Path $packagesPath) { Get-Content $packagesPath } else { $null }

   $targetFx = $projDoc.DocumentElement.SelectSingleNode("*/*[local-name() = 'TargetFrameworkVersion']").InnerText
   $targetFxMoniker = "net" + $targetFx.Substring(1).Replace(".", "")

   $xcstMinVersion = New-Object Version $PackageVersion.Major, $PackageVersion.Minor, 0
   $xcstMaxVersion = New-Object Version $PackageVersion.Major, ($PackageVersion.Minor + 1), 0

   "<package xmlns='http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd'>"
      "<metadata>"
         "<id>$projName</id>"
         "<version>$PackageVersion</version>"
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

      "<description>XCST compiler API.</description>"

      "<dependencies>"
         "<dependency id='Xcst' version='[$xcstMinVersion,$xcstMaxVersion)'/>"
         "<dependency id='Saxon-HE' version='$($packagesDoc.DocumentElement.SelectSingleNode('package[@id=''Saxon-HE'']').Attributes['allowedVersions'].Value)'/>"
      "</dependencies>"

      "<frameworkAssemblies>"
         "<frameworkAssembly assemblyName='System'/>"
         "<frameworkAssembly assemblyName='System.Xml'/>"
      "</frameworkAssemblies>"

   } elseif ($projName -eq "Xcst.Web") {

      "<description>XCST pages for ASP.NET. See also Xcst.Web.Mvc.</description>"

      "<dependencies>"
         "<dependency id='Xcst.Compiler' version='[$xcstMinVersion,$xcstMaxVersion)'/>"
         "<dependency id='Microsoft.Web.Infrastructure' version='$($packagesDoc.DocumentElement.SelectSingleNode('package[@id=''Microsoft.Web.Infrastructure'']').Attributes['allowedVersions'].Value)'/>"
      "</dependencies>"

      "<frameworkAssemblies>"
         "<frameworkAssembly assemblyName='System'/>"
         "<frameworkAssembly assemblyName='System.Web'/>"
      "</frameworkAssemblies>"

   } elseif ($projName -eq "Xcst.Web.Mvc") {

      "<description>XCST pages and views for ASP.NET MVC.</description>"

      "<dependencies>"
         "<dependency id='Xcst.Web' version='[$xcstMinVersion,$xcstMaxVersion)'/>"
         "<dependency id='Microsoft.AspNet.Mvc' version='$($packagesDoc.DocumentElement.SelectSingleNode('package[@id=''Microsoft.AspNet.Mvc'']').Attributes['allowedVersions'].Value)'/>"
      "</dependencies>"

      "<frameworkAssemblies>"
         "<frameworkAssembly assemblyName='System'/>"
         "<frameworkAssembly assemblyName='System.Web'/>"
      "</frameworkAssemblies>"
	}

   "</metadata>"

   "<files>"
      "<file src='$tempPath\NOTICE.xml'/>"
      "<file src='$solutionPath\LICENSE.txt'/>"
      "<file src='$projPath\bin\Release\$projName.*' target='lib\$targetFxMoniker'/>"
   "</files>"

   "</package>"
}

function script:NuPack([string]$projName) {

   $projPath = Resolve-Path $solutionPath\src\$projName
   $projFile = "$projPath\$projName.csproj"

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
   $outputPath = Resolve-Path nupkg

   ## Read project file

   $projDoc = New-Object Xml.XmlDocument
   $projDoc.PreserveWhitespace = $true
   $projDoc.Load($projFile)

   ## Create nuspec using info from project file and notice

   [xml]$noticeDoc = Get-Content $solutionPath\NOTICE.xml
   $notice = $noticeDoc.DocumentElement

   $nuspecPath = "$tempPath\$projName.nuspec"

   NuSpec | Out-File $nuspecPath -Encoding utf8

   ## Create package notice

   $saxonPath = Resolve-Path $solutionPath\packages\Saxon-HE.*
   &"$saxonPath\tools\Transform" -s:$solutionPath\NOTICE.xml -xsl:pkg-notice.xsl -o:$tempPath\NOTICE.xml projectName=$projName

   ## Create assembly signature file

   $signaturePath = "$tempPath\AssemblySignature.cs"
   $signature = @"
using System;
using System.Reflection;

[assembly: AssemblyProduct("$($notice.work)")]
[assembly: AssemblyCompany("$($notice.website)")]
[assembly: AssemblyCopyright("$($notice.copyright)")]
[assembly: AssemblyVersion("$AssemblyVersion")]
[assembly: AssemblyFileVersion("$PackageVersion")]
"@

   $signature | Out-File $signaturePath -Encoding utf8

   ## Add signature to project file

   $signatureXml = "<ItemGroup xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
      <Compile Include='$signaturePath'>
         <Link>AssemblySignature.cs</Link>
      </Compile>
   </ItemGroup>"

   $signatureReader = [Xml.XmlReader]::Create((New-Object IO.StringReader $signatureXml))
   $signatureReader.MoveToContent() | Out-Null

   $signatureNode = $projDoc.ReadNode($signatureReader)

   $projDoc.DocumentElement.AppendChild($signatureNode) | Out-Null
   $signatureNode.RemoveAttribute("xmlns")

   $projDoc.Save($projFile)

   ## Build project and remove signature

   MSBuild $projFile /p:Configuration=Release /p:BuildProjectReferences=false

   $projDoc.DocumentElement.RemoveChild($signatureNode) | Out-Null
   $projDoc.Save($projFile)

   ## Create package

   &$nuget pack $nuspecPath -OutputDirectory $outputPath
}

try {

   DownloadNuGet
   RestorePackages

   if ($ProjectName -eq '*') {

      NuPack Xcst
      NuPack Xcst.Compiler
      NuPack Xcst.Web
      NuPack Xcst.Web.Mvc

   } else {
      NuPack $ProjectName
   }

} finally {
   Pop-Location
}