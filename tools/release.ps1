[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [switch]$DryRun,
    [switch]$Draft,
    [switch]$AllowDirty
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Step([string]$Text) { Write-Host "==> $Text" -ForegroundColor Cyan }
function Fail([string]$Text) { Write-Host "ERROR: $Text" -ForegroundColor Red; exit 1 }
function Run([string]$Exe, [string[]]$Arguments) {
    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) { Fail "$Exe $($Arguments -join ' ') failed with exit code $LASTEXITCODE" }
}

if ($Version -notmatch '^\d+\.\d+\.\d+$') { Fail "Version must look like 1.2.3 (got '$Version')" }
$tag = "v$Version"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Step "Checking tools"
foreach ($tool in 'git', 'dotnet', 'gh') {
    if (-not (Get-Command $tool -ErrorAction SilentlyContinue)) { Fail "$tool is not installed or not on PATH" }
}
if (-not $DryRun) {
    gh auth status *> $null
    if ($LASTEXITCODE -ne 0) { Fail "GitHub CLI is not logged in. Run: gh auth login" }
}

Step "Checking repository state"
$dirty = git status --porcelain
if ($dirty -and -not $AllowDirty) { Fail "Working tree has uncommitted changes. Commit them or pass -AllowDirty." }
if (git tag --list $tag) { Fail "Tag $tag already exists" }

$changelog = Join-Path $root 'CHANGELOG.md'
$notes = ''
if (Test-Path $changelog) {
    $text = Get-Content $changelog -Raw
    $pattern = "(?ms)^## \[$([regex]::Escape($Version))\][^\n]*\n(.*?)(?=^## \[|^\[[^\]]+\]:|\z)"
    $match = [regex]::Match($text, $pattern)
    if ($match.Success) { $notes = $match.Groups[1].Value.Trim() }
}
if (-not $notes) { Fail "CHANGELOG.md has no section '## [$Version]'. Add release notes first." }

Step "Running tests"
Run 'dotnet' @('test', 'tests/SEBlueprint.Core.Tests', '-c', 'Release', '--nologo')

Step "Publishing portable exe $Version"
$out = Join-Path $root "out/release/$tag"
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
Run 'dotnet' @('publish', 'src/SEBlueprint.App', '-c', 'Release', '-o', "$out/app", "-p:Version=$Version", '--nologo')
$exe = Join-Path $out 'app/SEBlueprintInspector.exe'
if (-not (Test-Path $exe)) { Fail "Publish did not produce $exe" }

Step "Packing release zip"
$stage = Join-Path $out 'SEBlueprintInspector'
New-Item -ItemType Directory -Force $stage | Out-Null
Copy-Item $exe $stage
foreach ($file in 'README.md', 'CHANGELOG.md', 'LICENSE', 'LICENSE-MIT', 'LICENSE-GPL', 'PRIVACY.md') {
    if (Test-Path $file) { Copy-Item $file $stage }
}
$zipName = "SEBlueprintInspector-$tag-win-x64.zip"
$zip = Join-Path $out $zipName
Compress-Archive -Path "$stage/*" -DestinationPath $zip -CompressionLevel Optimal
$hash = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
$shaFile = "$zip.sha256"
Set-Content -Path $shaFile -Value "$hash  $zipName" -Encoding ascii -NoNewline

$notesFile = Join-Path $out 'notes.md'
$body = @"
$notes

### Download
``$zipName`` — unzip anywhere and run ``SEBlueprintInspector.exe``. No installer and no .NET install needed.

SHA-256: ``$hash``
"@
Set-Content -Path $notesFile -Value $body -Encoding utf8

Write-Host ""
Write-Host "  zip     $zip"
Write-Host "  sha256  $hash"
Write-Host "  size    $([math]::Round((Get-Item $zip).Length / 1MB, 1)) MB"
Write-Host ""

if ($DryRun) {
    Step "Dry run finished. Nothing was tagged, pushed or released."
    exit 0
}

Step "Tagging $tag"
Run 'git' @('tag', '-a', $tag, '-m', "SE Blueprint Inspector $tag")
Run 'git' @('push', 'origin', 'HEAD')
Run 'git' @('push', 'origin', $tag)

Step "Creating GitHub release $tag"
$ghArgs = @('release', 'create', $tag, $zip, $shaFile, '--title', "SE Blueprint Inspector $tag", '--notes-file', $notesFile, '--verify-tag')
if ($Draft) { $ghArgs += '--draft' }
Run 'gh' $ghArgs

Step "Done"
gh release view $tag --json url --jq .url
