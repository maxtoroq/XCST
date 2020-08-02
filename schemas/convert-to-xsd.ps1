$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

function EnsureTrang {

   if (-not (Test-Path $trangPath -PathType Container)) {
      $trangTemp = Join-Path (Resolve-Path .) trang.zip
      [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
      Invoke-WebRequest https://github.com/relaxng/jing-trang/releases/download/V20181222/trang-20181222.zip -OutFile $trangTemp
      Expand-Archive $trangTemp (Resolve-Path .)
      rm $trangTemp
   }
}

try {

   $saxonPath = Resolve-Path ..\packages\Saxon-HE.*
   $trangPath = Join-Path (Resolve-Path .) trang-20181222

   EnsureTrang

   &"$saxonPath\tools\Transform" -s:xcst.rng -xsl:rng-mod.xsl -o:xcst-mod.rng
   java -jar $trangPath\trang.jar -o any-process-contents=lax -o indent=3 xcst-mod.rng xcst.xsd
   &"$saxonPath\tools\Transform" -s:xcst.xsd -xsl:xsd-comment.xsl -o:xcst.xsd
   rm xcst-mod.rng

   java -jar $trangPath\trang.jar -o any-process-contents=lax -o indent=3 xcst-code.rng xcst-code.xsd
   &"$saxonPath\tools\Transform" -s:xcst-code.xsd -xsl:xsd-comment.xsl -o:xcst-code.xsd

} finally {
   Pop-Location
}
