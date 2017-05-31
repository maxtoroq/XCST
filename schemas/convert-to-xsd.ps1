$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

try {

   $saxonPath = Resolve-Path ..\packages\Saxon-HE.*

   &"$saxonPath\tools\Transform" -s:xcst.rng -xsl:rng-mod.xsl -o:xcst-mod.rng
   java -jar \lib\trang\20091111\trang.jar -o any-process-contents=lax -o indent=3 xcst-mod.rng xcst.xsd
   &"$saxonPath\tools\Transform" -s:xcst.xsd -xsl:xsd-comment.xsl -o:xcst.xsd
   rm xcst-mod.rng

} finally {
   Pop-Location
}