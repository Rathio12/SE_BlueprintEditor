[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$Version,
    [switch]$NoPush
)

$ErrorActionPreference = 'Stop'

function Step([string]$Text) { Write-Host "==> $Text" -ForegroundColor Cyan }
function Fail([string]$Text) { Write-Host "ERROR: $Text" -ForegroundColor Red; exit 1 }
function Run([string]$Exe, [string[]]$Arguments) {
    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) { Fail "$Exe $($Arguments -join ' ') failed with exit code $LASTEXITCODE" }
}

if ($Version -notmatch '^\d+\.\d+\.\d+$') { Fail "Version must look like 1.2.3 (got '$Version')" }
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Step "Checking repository"
if (git status --porcelain) { Fail "Working tree has uncommitted changes. Commit or stash them first." }
if (git tag --list "v$Version") { Fail "v$Version is already released" }
Run 'git' @('pull', '--rebase', 'origin', 'main')

$props = Join-Path $root 'Directory.Build.props'
$xml = [xml](Get-Content $props)
$current = $xml.Project.PropertyGroup.Version
if ([version]$Version -le [version]$current -and (git tag --list "v$current")) {
    Fail "New version $Version must be higher than $current"
}

Step "Running tests"
Run 'dotnet' @('test', 'tests/SEBlueprint.Core.Tests', '-c', 'Release', '--nologo')

Step "Setting version $current -> $Version"
$content = Get-Content $props -Raw
$content = $content -replace "<Version>[^<]*</Version>", "<Version>$Version</Version>"
[System.IO.File]::WriteAllText($props, $content, (New-Object System.Text.UTF8Encoding($false)))
Run 'git' @('add', 'Directory.Build.props')
Run 'git' @('commit', '-m', "chore(release): bump version to $Version")

if ($NoPush) {
    Step "Committed locally. Push to main to publish v$Version."
    exit 0
}

Step "Pushing to main"
Run 'git' @('push', 'origin', 'HEAD:main')

Step "GitHub Actions now builds and publishes v$Version"
Write-Host "Follow it with:  gh run watch"
Write-Host "Release page:    https://github.com/Rathio12/SE_BlueprintEditor/releases/tag/v$Version"
