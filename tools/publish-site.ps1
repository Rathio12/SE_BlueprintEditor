[CmdletBinding()]
param(
    [string]$BasePath = '/SE_BlueprintEditor/',
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$out = Join-Path $root 'out/site'
if (Test-Path $out) { Remove-Item $out -Recurse -Force }

$publishArgs = @('publish', 'src/SEBlueprint.Web', '-c', 'Release', '-o', $out, '--nologo', '-p:BlazorEnableCompression=false')
if ($Version) { $publishArgs += "-p:Version=$Version" }
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed" }

$docs = Join-Path $root 'docs'
New-Item -ItemType Directory -Force $docs | Out-Null
$siteOwned = @('index.html', '404.html', '.nojekyll', 'favicon.png', 'app-icon.svg', '_framework', 'css', 'js', 'data', 'robots.txt', 'sitemap.xml', 'icons', 'site.webmanifest')
foreach ($name in $siteOwned) {
    $target = Join-Path $docs $name
    if (Test-Path $target) { Remove-Item $target -Recurse -Force }
}

Copy-Item (Join-Path $out 'wwwroot/*') $docs -Recurse -Force
Remove-Item (Join-Path $docs 'data') -Recurse -Force -ErrorAction SilentlyContinue

$index = Join-Path $docs 'index.html'
$utf8 = New-Object System.Text.UTF8Encoding($false)
$html = [System.IO.File]::ReadAllText($index, $utf8) -replace '<base href="/" />', "<base href=`"$BasePath`" />"
[System.IO.File]::WriteAllText($index, $html, $utf8)
Copy-Item $index (Join-Path $docs '404.html')
New-Item -ItemType File -Force (Join-Path $docs '.nojekyll') | Out-Null

& dotnet run --project tools/SEBlueprint.SiteGen -c Release -- $docs "https://rathio12.github.io$BasePath"
if ($LASTEXITCODE -ne 0) { throw "Wiki / sitemap generation failed" }

$size = (Get-ChildItem $docs -Recurse -File | Measure-Object Length -Sum).Sum / 1MB
Write-Host ("Site published to docs/ ({0:N1} MB) with base path {1}" -f $size, $BasePath)
