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
Run 'git' @('fetch', '--tags', '--force', 'origin')

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

if (git tag --list "v$Version") { Fail "v$Version is already released" }

$lastTag = @(git tag --merged HEAD --list 'v[0-9]*' --sort=-v:refname) | Select-Object -First 1
$range = if ($lastTag) { "$lastTag..HEAD" } else { 'HEAD' }
$subjects = @(git log $range --no-merges --format='%s')

Step "Checklist 1/3: README is up to date"
$features = @($subjects | Where-Object { $_ -match '^feat(\(|!|:)' })
$readmeChanged = if ($lastTag) { [bool](git diff --name-only $lastTag HEAD -- README.md) } else { $true }
if ($features.Count -gt 0 -and -not $readmeChanged) {
    Fail "There are $($features.Count) new features since $lastTag but README.md did not change. Update the README first."
}
Write-Host "  README ok ($($features.Count) new features, README changed: $readmeChanged)"

Step "Checklist 2/3: changelog is up to date"
$preview = Join-Path ([System.IO.Path]::GetTempPath()) "changelog-preview-$PID.md"
Copy-Item CHANGELOG.md $preview
& (Join-Path $PSScriptRoot 'changelog.ps1') -Path $preview -Release $Version
$utf8 = New-Object System.Text.UTF8Encoding($false)
$previewText = [System.IO.File]::ReadAllText($preview, $utf8) -replace "`r`n", "`n"
Remove-Item $preview -Force
$section = [regex]::Match($previewText, "(?ms)^## \[$([regex]::Escape($Version))\][^\n]*\n(.*?)(?=^## \[|^\[[^\]]+\]:|\z)")
if (-not $section.Success -or -not $section.Groups[1].Value.Trim() -or $section.Groups[1].Value -match 'Maintenance release') {
    Fail "No changelog entries for v$Version. Commit with feat:/fix:/docs: messages, or add notes under [Unreleased]."
}
Write-Host "  Release notes for v${Version}:" -ForegroundColor DarkGray
$section.Groups[1].Value.Trim().Split("`n") | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }

Step "Checklist 3/3: bug check"
Run 'dotnet' @('test', 'tests/SEBlueprint.Core.Tests', '-c', 'Release', '--nologo')
Run 'dotnet' @('build', 'SEBlueprintInspector.slnx', '-c', 'Release', '-warnaserror', '--nologo')
$broken = @()
foreach ($md in @(Get-Item README.md) + @(Get-ChildItem docs/wiki -Filter *.md)) {
    $text = [System.IO.File]::ReadAllText($md.FullName, $utf8)
    foreach ($m in [regex]::Matches($text, '\]\(([^)#\s]+)(#[^)]*)?\)')) {
        $link = $m.Groups[1].Value
        if ($link -match '^(https?:|mailto:)') { continue }
        $target = Join-Path $md.DirectoryName $link
        if (-not (Test-Path $target)) { $broken += "$($md.Name): $link" }
    }
}
if ($broken.Count -gt 0) { Fail "Broken links:`n  $($broken -join "`n  ")" }
Write-Host "  Tests pass, build has no warnings, all document links resolve"

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
