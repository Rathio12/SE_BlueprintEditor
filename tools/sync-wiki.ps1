param(
    [Parameter(Mandatory)][string]$Target,
    [string]$Source = (Join-Path $PSScriptRoot '..\docs\wiki')
)

$ErrorActionPreference = 'Stop'
$repo = 'https://github.com/Rathio12/SE_BlueprintEditor'
$site = 'https://rathio12.github.io/SE_BlueprintEditor'
$utf8 = New-Object System.Text.UTF8Encoding($false)

$pages = @(
    @{ File = 'Home'; Title = 'Home' },
    @{ File = 'How-It-Works'; Title = 'How it works' },
    @{ File = 'Reading-Game-Data'; Title = 'Reading game data' },
    @{ File = 'Reading-Blueprints'; Title = 'Reading blueprints' },
    @{ File = 'Mods-Weapons-and-Cargo'; Title = 'Mods, weapons and cargo' },
    @{ File = 'Cost-Calculation'; Title = 'Cost calculation' },
    @{ File = 'Limits-and-Profiles'; Title = 'Limits and profiles' },
    @{ File = 'Building-and-Releasing'; Title = 'Building and releasing' },
    @{ File = 'FAQ'; Title = 'FAQ' }
)

foreach ($p in $pages) {
    if (-not (Test-Path (Join-Path $Source "$($p.File).md"))) { throw "Missing wiki page $($p.File).md in $Source" }
}

Get-ChildItem $Target -File -Filter *.md | Remove-Item

function Convert-Links([string]$text) {
    $text = [regex]::Replace($text, '\]\(\.\./\.\./([^)]+)\)', { param($m) "]($repo/blob/main/$($m.Groups[1].Value))" })
    $text = [regex]::Replace($text, '\]\(\.\./([^)]+)\)', { param($m) "]($repo/blob/main/docs/$($m.Groups[1].Value))" })
    $text = [regex]::Replace($text, '\]\(([A-Za-z0-9-]+)\.md(#[^)]*)?\)', { param($m) "]($($m.Groups[1].Value)$($m.Groups[2].Value))" })
    return $text
}

foreach ($p in $pages) {
    $text = [IO.File]::ReadAllText((Join-Path $Source "$($p.File).md"), [Text.Encoding]::UTF8)
    [IO.File]::WriteAllText((Join-Path $Target "$($p.File).md"), (Convert-Links $text), $utf8)
}

$sidebar = New-Object System.Text.StringBuilder
[void]$sidebar.AppendLine("### [SE Blueprint Inspector]($repo)")
[void]$sidebar.AppendLine()
foreach ($p in $pages) { [void]$sidebar.AppendLine("- [$($p.Title)]($($p.File))") }
[void]$sidebar.AppendLine()
[void]$sidebar.AppendLine("**Get it**")
[void]$sidebar.AppendLine()
[void]$sidebar.AppendLine("- [Download]($repo/releases/latest)")
[void]$sidebar.AppendLine("- [Open in browser]($site/)")
[void]$sidebar.AppendLine("- [Discussions]($repo/discussions)")
[void]$sidebar.AppendLine("- [Report a bug]($repo/issues/new/choose)")
[IO.File]::WriteAllText((Join-Path $Target '_Sidebar.md'), $sidebar.ToString(), $utf8)

$footer = "<sub>Generated from [docs/wiki]($repo/tree/main/docs/wiki) on every push - edit the files there, not here. " +
          "Also on the [website]($site/wiki/Home.html) | [MIT OR GPL-3.0-or-later]($repo/blob/main/LICENSE)</sub>`n"
[IO.File]::WriteAllText((Join-Path $Target '_Footer.md'), $footer, $utf8)

Write-Host "Wrote $($pages.Count) pages, sidebar and footer to $Target"
