$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

try {

   $saxonPath = Resolve-Path ..\packages\Saxon-HE.*

   java -jar \lib\trang\20091111\trang.jar -o any-process-contents=lax -o indent=3 xcst.rng xcst.xsd
   java -jar \lib\trang\20091111\trang.jar -o any-process-contents=lax -o indent=3 xcst-app.rng xcst-app.xsd

   &"$saxonPath\tools\Transform" -s:xcst.xsd -xsl:xsd-comment.xsl -o:xcst.xsd
   &"$saxonPath\tools\Transform" -s:xcst-app.xsd -xsl:xsd-comment.xsl -o:xcst-app.xsd

} finally {
   Pop-Location
}