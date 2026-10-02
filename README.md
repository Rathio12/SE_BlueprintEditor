<div align="center">

<img src="assets/icon/app-icon-256.png" width="104" alt="SE Blueprint Inspector">

# SE Blueprint Inspector

**Build cost, blocks, PCU, guns and cargo for every Space Engineers blueprint —<br>checked against vanilla or server limits, with modded weapons and containers counted correctly.**

[![Release](https://img.shields.io/github/v/release/Rathio12/SE_BlueprintEditor?style=flat-square&label=release&labelColor=0B151B&color=46B4E6)](https://github.com/Rathio12/SE_BlueprintEditor/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/Rathio12/SE_BlueprintEditor/total?style=flat-square&labelColor=0B151B&color=46B4E6)](https://github.com/Rathio12/SE_BlueprintEditor/releases)
[![Release build](https://img.shields.io/github/actions/workflow/status/Rathio12/SE_BlueprintEditor/release.yml?branch=main&style=flat-square&label=build&labelColor=0B151B)](https://github.com/Rathio12/SE_BlueprintEditor/actions/workflows/release.yml)
[![License](https://img.shields.io/badge/license-MIT%20%7C%20GPL--3.0-46B4E6?style=flat-square&labelColor=0B151B)](LICENSE)

![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-DCE8EE?style=flat-square&labelColor=0B151B&logo=windows11&logoColor=white)
![Portable](https://img.shields.io/badge/portable-single%20exe-5BC07A?style=flat-square&labelColor=0B151B)
[![Web](https://img.shields.io/badge/web-open%20in%20browser-46B4E6?style=flat-square&labelColor=0B151B)](https://rathio12.github.io/SE_BlueprintEditor/)
![Offline](https://img.shields.io/badge/offline-no%20telemetry-5BC07A?style=flat-square&labelColor=0B151B)
![.NET](https://img.shields.io/badge/.NET-8-8E7CC3?style=flat-square&labelColor=0B151B&logo=dotnet&logoColor=white)
![Space Engineers](https://img.shields.io/badge/Space%20Engineers-supported-F0A030?style=flat-square&labelColor=0B151B)
![Space Engineers 2](https://img.shields.io/badge/Space%20Engineers%202-coming%20soon-5D7682?style=flat-square&labelColor=0B151B)

[**Download**](https://github.com/Rathio12/SE_BlueprintEditor/releases/latest) · [**Open in browser**](https://rathio12.github.io/SE_BlueprintEditor/) · [Wiki](https://rathio12.github.io/SE_BlueprintEditor/wiki/Home.html) · [Features](#features) · [Limit profiles](#limit-profiles) · [Report a bug](https://github.com/Rathio12/SE_BlueprintEditor/issues/new/choose) · [Changelog](CHANGELOG.md)

<img src="docs/images/blueprints.png" alt="Blueprint list and detail view" width="920">

</div>

---

## Download

1. Download **`SEBlueprintInspector.exe`** from the [latest release](https://github.com/Rathio12/SE_BlueprintEditor/releases/latest).
2. Put it anywhere — desktop, a tools folder, a USB stick — and run it.

That's it: no installer, no .NET install, no admin rights. The app keeps its settings in a
`SEBlueprintInspector-data` folder next to the exe. Delete both and it's gone without a trace.

## Use it in the browser

**[rathio12.github.io/SE_BlueprintEditor](https://rathio12.github.io/SE_BlueprintEditor/)** runs the same engine
right in your browser — open a blueprint folder or `bp.sbc`, add the mods it uses, and get the same numbers as the
app. Nothing is uploaded: files are read locally by your browser. (The desktop app additionally scans your whole
library automatically and shows the real game icons.)

## Features

| | |
|:--|:--|
| **Finds everything** | Steam, every Steam library, Space Engineers, Workshop mods and all blueprint folders (local, cloud, Workshop) are detected automatically. |
| **Counts what matters** | Blocks, PCU, mass, fixed guns, turrets, cargo containers and liters, thrust, power and jump range — per blueprint, at a glance. |
| **Build cost** | Components → ingots → ore with the real in-game icons, assembler speed (realistic / x3 / x10) and refinery yield modules. Copy to a spreadsheet or export CSV. |
| **Limit checks** | Every blueprint gets an **OK / NEAR / OVER** status for the selected profile, with segmented gauges for PCU, blocks, guns and cargo. |
| **Mods done right** | Modded blocks are read from the mods on your PC. WeaponCore / CoreSystems weapons (usually hidden as conveyor sorters) are recognised as guns or turrets. Mod recipes only apply to blueprints that use that mod. |
| **Honest about gaps** | Blocks from mods that aren't on your PC are listed with the missing Workshop IDs instead of being silently ignored. |

## Limit profiles

<img src="docs/images/profiles.png" alt="Limit profile editor" width="760">

| Profile | What it is |
|:--|:--|
| **Vanilla – …** | Read from the world presets of your installed game (`TotalPCU`, `MaxGridSize`, `MaxBlocksPerPlayer`, `BlockTypeLimits`). |
| **Sigma Draconis – Expanse** | The server's published limit: **50,000 PCU per grid**. |
| **Modded server (typical)** | A starting point for modded survival servers — duplicate and adjust. |
| **Your own** | PCU, PCU per grid, blocks per grid, blocks total, guns, turrets, cargo and per-block-type limits. Share them as JSON files. |

Built-in profiles are read-only; duplicate one to change it.
Status: **OK** below 90 % of a limit, **NEAR** from 90 % to the limit, **OVER** above it.

## Modded weapons and cargo

- **Vanilla weapons** by block type — `LargeGatlingTurret`, `LargeMissileTurret`, `InteriorTurret` and `*Turret` are turrets; `SmallGatlingGun`, `SmallMissileLauncher`, `SmallMissileLauncherReload` and blocks with a weapon definition are fixed guns.
- **WeaponCore / CoreSystems** weapons are found in the mod's `Data/Scripts` weapon definitions, read as text and never executed: a mount point with an azimuth part is a turret, otherwise a fixed gun.
- **Cargo** uses the block's `InventorySize`; containers without one get the in-game default (block volume × 1000 L).

## Space Engineers 2 — coming soon

> [!NOTE]
> **Full SE2 support is coming soon.** SE2 blueprint files can't be read yet — the grid data
> (`grid.json.vrb`) uses an undocumented binary format.

Already working: SE2 is detected, its blueprints are listed with **blocks and PCU** (from each blueprint's
info file), and PCU / block limits are checked. Build cost, weapons and cargo follow once the format can be read.

## Privacy

> [!TIP]
> The app is **fully offline**: no internet access, no telemetry, no auto-updater, no native code.
> Game, mod and blueprint files are only read, never changed. A test fails the build if network code
> or foreign URLs are ever added. → [PRIVACY.md](PRIVACY.md)

## Build from source

Requires Windows and the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer.

```powershell
git clone https://github.com/Rathio12/SE_BlueprintEditor.git
cd SE_BlueprintEditor
dotnet test                                                     # core tests
dotnet run --project src/SEBlueprint.App                        # run the app
dotnet publish src/SEBlueprint.App -c Release -o out/portable   # single portable exe
```

`SEBlueprintInspector.exe <blueprint folder | bp.sbc>` opens that blueprint directly;
`--page profiles|settings|info` picks the start tab.

## Versions and releases

Versions follow **`1.x.y`** — `1` is the product line, **`x` the build**, **`y` the fixes** on that build.

```powershell
./tools/release.ps1          # next build:  1.0.0 -> 1.1.0
./tools/release.ps1 -Fix     # next fix:    1.1.0 -> 1.1.1
```

The script first runs the **release checklist** — README updated for new features, changelog has the changes,
tests pass, the solution builds with zero warnings and all document links work — then bumps the version in
[`Directory.Build.props`](Directory.Build.props) and pushes.
GitHub Actions ([release.yml](.github/workflows/release.yml)) then does the rest on every push to `main`:

1. runs the tests,
2. rewrites the *Unreleased* part of [CHANGELOG.md](CHANGELOG.md) from the commit messages,
3. for a new version: turns *Unreleased* into the version's section, builds the exe and publishes the release.

Commit messages use [Conventional Commits](https://www.conventionalcommits.org/):
`feat:` → **Added**, `fix:` → **Fixed**, `perf` / `refactor` / `docs` → **Changed**; `chore`, `ci`, `test` and `build` are left out.

## Project layout

```text
src/SEBlueprint.Core   game data, mods, blueprint analysis, cost and limits (no UI)
src/SEBlueprint.App    Windows app (WPF, custom UI)
src/SEBlueprint.Web    website (Blazor WebAssembly, same Core) -> built into docs/
tests/                 xUnit tests with small fixture files
tools/                 release and changelog scripts
assets/icon/           app icon source
docs/                  GitHub Pages site (generated by tools/publish-site.ps1) and screenshots
```

## Wiki

How the app reads game files, blueprints and mods, and how every number is calculated:
**[Wiki on the website](https://rathio12.github.io/SE_BlueprintEditor/wiki/Home.html)** · [same pages on GitHub](docs/wiki/Home.md)

## Contributing

Bug reports, mod-compatibility reports and pull requests are welcome —
see [CONTRIBUTING.md](CONTRIBUTING.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) and [SECURITY.md](SECURITY.md).
If the tool helps you, a ⭐ on the repo helps others find it.

## License

Dual-licensed **MIT OR GPL-3.0-or-later** — use whichever fits your project.
See [LICENSE](LICENSE), [LICENSE-MIT](LICENSE-MIT) and [LICENSE-GPL](LICENSE-GPL).

## Credits

- Inspired by [SE-BlueprintEditor](https://github.com/ScriptedEngineer/SE-BlueprintEditor) by ScriptedEngineer — this is an independent rewrite; no code from that project is used.
- [BCnEncoder.NET](https://github.com/Nominom/BCnEncoder.NET) (MIT) decodes the game's icons.
- Sigma Draconis Expanse limits from [sigmadraconis.games](https://sigmadraconis.games/).
- Space Engineers, its data and icons © Keen Software House. Not affiliated with or endorsed by Keen Software House.
