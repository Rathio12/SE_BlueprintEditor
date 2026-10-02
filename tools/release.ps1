[CmdletBinding()]
param(
    [switch]$Fix,
    [string]$Version,
    [switch]$NoPush
)

$ErrorActionPreference = 'Stop'

function Step([string]$Text) { Write-Host "==> $Text" -ForegroundColor Cyan }
function Fail([string]$Text) { Write-Host "ERROR: $Text" -ForegroundColor Red; exit 1 }
function Run([string]$Exe, [string[]]$Arguments) {
    & $Exe @Arguments
    if ($LASTEXITCODE -ne 0) { Fail "$Exe $($Arguments -join ' ') failed with exit code $LASTEXITCODE" }
}

$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Step "Checking repository"
if (git status --porcelain) { Fail "Working tree has uncommitted changes. Commit or stash them first." }
Run 'git' @('pull', '--rebase', 'origin', 'main')

$props = Join-Path $root 'Directory.Build.props'
$current = ([xml](Get-Content $props)).Project.PropertyGroup.Version
if ($current -notmatch '^1\.(\d+)\.(\d+)$') { Fail "Current version '$current' is not in the 1.x.y format" }
$build = [int]$Matches[1]
$fixes = [int]$Matches[2]
if (-not $Version) {
    if (-not (git tag --list "v$current")) { $Version = $current }
    elseif ($Fix) { $Version = "1.$build.$($fixes + 1)" }
    else { $Version = "1.$($build + 1).0" }
}
if ($Version -notmatch '^1\.\d+\.\d+$') { Fail "Version must look like 1.x.y (got '$Version')" }
if ([version]$Version -lt [version]$current) { Fail "New version $Version is lower than $current" }
Write-Host "Version scheme 1.x.y: x = build, y = fixes on that build. Releasing v$Version (was $current)."

Step "Running tests"
Run 'dotnet' @('test', 'tests/SEBlueprint.Core.Tests', '-c', 'Release', '--nologo')

if (git tag --list "v$Version") { Fail "v$Version is already released" }

Step "Setting version $current -> $Version"
$content = Get-Content $props -Raw
$content = $content -replace "<Version>[^<]*</Version>", "<Version>$Version</Version>"
[System.IO.File]::WriteAllText($props, $content, (New-Object System.Text.UTF8Encoding($false)))
if ($Version -ne $current) {
    Run 'git' @('add', 'Directory.Build.props')
    Run 'git' @('commit', '-m', "chore(release): bump version to $Version")
}

if ($NoPush) {
    Step "Committed locally. Push to main to publish v$Version."
    exit 0
}

Step "Pushing to main"
Run 'git' @('push', 'origin', 'HEAD:main')

Step "GitHub Actions now builds and publishes v$Version"
Write-Host "Follow it with:  gh run watch"
Write-Host "Release page:    https://github.com/Rathio12/SE_BlueprintEditor/releases/tag/v$Version"
