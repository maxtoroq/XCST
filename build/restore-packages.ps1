$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

$solutionPath = Resolve-Path ..

try {

   MSBuild $solutionPath\XCST.sln -t:restore -p:RestorePackagesConfig=true

} finally {
   Pop-Location
}
