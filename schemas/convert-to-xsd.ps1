$ErrorActionPreference = "Stop"
Push-Location (Split-Path $script:MyInvocation.MyCommand.Path)

function EnsureSaxon {

   if (-not (Test-Path $saxonPath -PathType Container)) {
      $saxonTemp = Join-Path (Resolve-Path .) saxonhe.zip
      [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
      Invoke-WebRequest https://www.nuget.org/api/v2/package/Saxon-HE/9.9.1.5 -OutFile $saxonTemp
      Expand-Archive $saxonTemp $saxonPath
      rm $saxonTemp
   }
}

function EnsureTrang {

   if (-not (Test-Path $trangPath -PathType Container)) {
      $trangTemp = Join-Path (Resolve-Path .) trang.zip
      [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
      Invoke-WebRequest https://github.com/relaxng/jing-trang/releases/download/V20181222/trang-20181222.zip -OutFile $trangTemp
      Expand-Archive $trangTemp $trangPath
      rm $trangTemp
   }
}

try {

   $saxonPath = Join-Path (Resolve-Path .) saxonhe
   $trangPath = Join-Path (Resolve-Path .) trang-

   EnsureSaxon
   EnsureTrang

   $saxonTransform = Resolve-Path $saxonPath\tools\Transform.exe
   $trangJar = Resolve-Path $trangPath\*\trang.jar

   &$saxonTransform -s:xcst.rng -xsl:rng-mod.xsl -o:xcst-mod.rng
   java -jar $trangJar -o any-process-contents=lax -o indent=3 xcst-mod.rng xcst.xsd
   &$saxonTransform -s:xcst.xsd -xsl:xsd-comment.xsl -o:xcst.xsd
   rm xcst-mod.rng

   java -jar $trangJar -o any-process-contents=lax -o indent=3 xcst-code.rng xcst-code.xsd
   &$saxonTransform -s:xcst-code.xsd -xsl:xsd-comment.xsl -o:xcst-code.xsd

} finally {
   Pop-Location
}
