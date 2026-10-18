# Builds the LD submission archive: the published WebGL build with index.html at the ZIP root,
# ready for a browser-game host. Output: dist/ under the repo root
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
if (Test-Path -LiteralPath $zip) {
    $retired = Join-Path $root '_toDelete'
    New-Item -ItemType Directory -Force -Path $retired | Out-Null
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')
    Move-Item -LiteralPath $zip -Destination (Join-Path $retired ('balloonatic-web-' + $stamp + '.zip'))
}

# Contents of wwwroot at the archive root (not the folder itself): LD wants index.html on top.
Compress-Archive -Path (Join-Path $wwwroot '*') -DestinationPath $zip -CompressionLevel Optimal

$mb = [math]::Round((Get-Item $zip).Length / 1MB, 2)
Write-Output ('wrote ' + $zip + ' (' + $mb + ' MB)')
$hashStream = [IO.File]::OpenRead($zip)
$digest = [Security.Cryptography.SHA256]::Create()
try {
    Write-Output ('SHA256 ' + [BitConverter]::ToString($digest.ComputeHash($hashStream)).Replace('-', ''))
}
finally {
    $hashStream.Dispose()
    $digest.Dispose()
}
