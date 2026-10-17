# Builds the LD submission archive: the published WebGL build with index.html at the ZIP root,
# which is exactly what the ldjam.com "web play" upload expects. Output: dist/ under the repo root
# (generated, gitignored - like export/).
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$wwwroot = Join-Path $root 'export/web/wwwroot'
$dist = Join-Path $root 'dist'
$zip = Join-Path $dist 'charlottes-balloonatic-ld60-web.zip'

if (-not (Test-Path (Join-Path $wwwroot 'index.html'))) {
    Write-Error 'no published build at export/web/wwwroot - run npm run web first'
    exit 1
}

New-Item -ItemType Directory -Force -Path $dist | Out-Null
if (Test-Path $zip) { Remove-Item $zip }

# Contents of wwwroot at the archive root (not the folder itself): LD wants index.html on top.
Compress-Archive -Path (Join-Path $wwwroot '*') -DestinationPath $zip -CompressionLevel Optimal

$mb = [math]::Round((Get-Item $zip).Length / 1MB, 2)
Write-Output ('wrote ' + $zip + ' (' + $mb + ' MB)')
