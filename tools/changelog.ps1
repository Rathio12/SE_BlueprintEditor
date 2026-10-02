[CmdletBinding()]
param(
    [string]$Release,
    [string]$Path = 'CHANGELOG.md',
    [string]$Repo = 'Rathio12/SE_BlueprintEditor',
    [string]$Date = (Get-Date -Format 'yyyy-MM-dd')
)

$ErrorActionPreference = 'Stop'

function Get-LastTag {
    $tags = @(git tag --merged HEAD --list 'v[0-9]*' --sort=-v:refname)
    if ($tags.Count -eq 0 -or -not $tags[0]) { return $null }
    return $tags[0]
}

function Get-Entries {
    $last = Get-LastTag
    $range = if ($last) { "$last..HEAD" } else { 'HEAD' }
    $subjects = git log $range --no-merges --format='%s'
    if (-not $subjects) { return @() }
    $entries = @()
    foreach ($s in @($subjects)) {
        if ($s -match '\[skip changelog\]') { continue }
        if ($s -match '^(?<type>[a-zA-Z]+)(\([^)]*\))?(?<bang>!)?:\s*(?<text>.+)$') {
            $type = $Matches['type'].ToLowerInvariant()
            $text = $Matches['text'].Trim()
            if ($Matches['bang']) { $text = "**Breaking:** $text" }
        } else {
            $type = 'other'
            $text = $s.Trim()
        }
        $group = switch ($type) {
            'feat' { 'Added' }
            'fix' { 'Fixed' }
            { $_ -in 'perf', 'refactor', 'docs', 'other' } { 'Changed' }
            default { $null }
        }
        if (-not $group) { continue }
        $text = $text -replace '\s*\[skip ci\]', ''
        $text = $text.Substring(0, 1).ToUpperInvariant() + $text.Substring(1)
        $entries += [pscustomobject]@{ Group = $group; Text = $text }
    }
    return $entries
}

function Format-Entries($entries) {
    $out = New-Object System.Text.StringBuilder
    foreach ($group in 'Added', 'Changed', 'Fixed') {
        $items = @($entries | Where-Object { $_.Group -eq $group } | Select-Object -ExpandProperty Text -Unique)
        if ($items.Count -eq 0) { continue }
        [void]$out.Append("### $group`n")
        foreach ($i in $items) { [void]$out.Append("- $i`n") }
        [void]$out.Append("`n")
    }
    return $out.ToString()
}

if (-not (Test-Path $Path)) { throw "$Path not found" }
$text = (Get-Content $Path -Raw) -replace "`r`n", "`n"

$headerPattern = '(?ms)^## \[Unreleased\][^\n]*\n(?<body>.*?)(?=^## \[|^\[[^\]]+\]:|\z)'
$m = [regex]::Match($text, $headerPattern)
if (-not $m.Success) { throw "No '## [Unreleased]' section in $Path" }

$generated = Format-Entries (Get-Entries)

if ($Release) {
    if ($Release -notmatch '^\d+\.\d+\.\d+$') { throw "Release version must look like 1.2.3" }
    $existing = [regex]::Match($text, "(?m)^## \[$([regex]::Escape($Release))\]")
    if ($existing.Success) {
        $replacement = "## [Unreleased]`n`n"
    } else {
        $body = if ($generated.Trim()) { $generated } else { "### Changed`n- Maintenance release.`n`n" }
        $replacement = "## [Unreleased]`n`n## [$Release] - $Date`n`n$body"
    }
    $text = $text.Substring(0, $m.Index) + $replacement + $text.Substring($m.Index + $m.Length)

    $prev = Get-LastTag
    $text = [regex]::Replace($text, '(?m)^\[Unreleased\]:.*$', "[Unreleased]: https://github.com/$Repo/compare/v$Release...HEAD")
    if (-not [regex]::IsMatch($text, "(?m)^\[$([regex]::Escape($Release))\]:")) {
        $link = if ($prev) { "https://github.com/$Repo/compare/$prev...v$Release" } else { "https://github.com/$Repo/releases/tag/v$Release" }
        $text = [regex]::Replace($text, '(?m)^(\[Unreleased\]:.*)$', "`$1`n[$Release]: $link", 1)
    }
} else {
    $replacement = "## [Unreleased]`n`n$generated"
    $text = $text.Substring(0, $m.Index) + $replacement + $text.Substring($m.Index + $m.Length)
}

$text = [regex]::Replace($text, "\n{3,}", "`n`n").TrimEnd() + "`n"
$utf8 = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText((Resolve-Path $Path), $text, $utf8)
