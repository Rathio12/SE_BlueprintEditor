<p align="center">
  <img src="assets/icon/app-icon-256.png" width="112" alt="SE Blueprint Inspector icon">
</p>

<h1 align="center">SE Blueprint Inspector</h1>

<p align="center">
  Build cost, blocks, PCU, guns and cargo for every Space Engineers blueprint —<br>
  checked against vanilla or server limits, with modded weapons and containers counted correctly.
</p>

<p align="center">
  <a href="https://github.com/Rathio12/SE_BlueprintEditor/releases/latest"><img alt="Latest release" src="https://img.shields.io/github/v/release/Rathio12/SE_BlueprintEditor?style=flat-square&color=46B4E6"></a>
  <a href="https://github.com/Rathio12/SE_BlueprintEditor/actions/workflows/ci.yml"><img alt="CI" src="https://img.shields.io/github/actions/workflow/status/Rathio12/SE_BlueprintEditor/ci.yml?branch=main&style=flat-square&label=build"></a>
  <img alt="Platform" src="https://img.shields.io/badge/platform-Windows%2010%20%7C%2011-0B151B?style=flat-square">
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square">
  <img alt="Portable" src="https://img.shields.io/badge/portable-single%20exe-5BC07A?style=flat-square">
  <img alt="Space Engineers" src="https://img.shields.io/badge/Space%20Engineers-1-F0A030?style=flat-square">
  <img alt="Space Engineers 2" src="https://img.shields.io/badge/Space%20Engineers%202-coming%20soon-5D7682?style=flat-square">
  <a href="LICENSE"><img alt="License" src="https://img.shields.io/badge/license-MIT%20OR%20GPL--3.0-blue?style=flat-square"></a>
</p>

<p align="center">
  <img src="docs/images/blueprints.png" alt="Blueprint list and detail view" width="900">
</p>

## Download

1. Grab `SEBlueprintInspector-vX.Y.Z-win-x64.zip` from **[Releases](https://github.com/Rathio12/SE_BlueprintEditor/releases/latest)**.
2. Unzip anywhere (desktop, USB stick, a tools folder) and run `SEBlueprintInspector.exe`.

No installer, no .NET install, no admin rights. Everything the app saves lives in a
`SEBlueprintInspector-data` folder next to the exe — delete the folder and the app is gone.

## What it does

| | |
|---|---|
| **Finds everything** | Steam, every Steam library, Space Engineers, Workshop mods and all blueprint folders (local, cloud, Workshop) are detected automatically. Space Engineers 2 blueprints are listed too (full SE2 support coming soon). |
| **Counts what matters** | Blocks, PCU, mass, fixed guns, turrets, cargo containers and liters, thrust, power and jump range — per blueprint, at a glance. |
| **Build cost** | Components → ingots → ore with the real in-game icons, assembler speed (realistic / x3 / x10) and refinery yield modules. Copy to a spreadsheet or export CSV. |
| **Limit checks** | Pick a profile and every blueprint gets an OK / near / over status. Vanilla profiles come from the game's own world presets; make your own for a server and share it as a JSON file. |
| **Mods done right** | Modded blocks are read from the mods on your PC. WeaponCore / CoreSystems weapons (usually hidden as conveyor sorters) are recognised as guns or turrets. Mod recipes only apply to blueprints that use that mod. |
| **Honest about gaps** | Blocks from mods that aren't on your PC are listed with the missing Workshop IDs instead of being silently ignored. |

<p align="center">
  <img src="docs/images/profiles.png" alt="Limit profile editor" width="760">
</p>

## Limit profiles

| Profile | Source |
|---|---|
| `Vanilla – …` | Read from `Content/CustomWorlds/*/Sandbox_config.sbc` of your installed game (`TotalPCU`, `MaxGridSize`, `MaxBlocksPerPlayer`, `BlockTypeLimits`). Identical presets are merged. Read-only. |
| Your own | Max PCU, blocks per grid, blocks total, guns, turrets, cargo liters and per-block-type limits. Saved as `SEBlueprintInspector-data/profiles/<name>.json`. |

Status rules: **OK** below 90 % of a limit, **NEAR** from 90 % up to the limit, **OVER** above it. Empty fields mean "no limit".

## Modded weapons and cargo

* **Vanilla weapons** are classified by block type: `LargeGatlingTurret`, `LargeMissileTurret`, `InteriorTurret` and `*Turret` are turrets; `SmallGatlingGun`, `SmallMissileLauncher`, `SmallMissileLauncherReload` and other blocks with a weapon definition are fixed guns.
* **WeaponCore / CoreSystems** weapons are found by reading the mod's `Data/Scripts` weapon definitions as text (never executed): a mount point with an azimuth part is a turret, otherwise a fixed gun.
* **Cargo** uses the block's `InventorySize`; containers without one get the in-game default (block volume × 1000 L).

## Space Engineers 2 — support coming soon

> **SE2 support is coming soon.** Right now SE2 blueprint files can't be read: the grid data
> (`grid.json.vrb`) is stored in an undocumented binary format.

What already works: SE2 is detected, its blueprints are listed, and **blocks and PCU** are shown
(taken from each blueprint's info file), so PCU and block limits can be checked. Build cost, weapons
and cargo for SE2 will be added as soon as the grid format can be read.

## Privacy

The app works fully offline: no internet access, no telemetry, no auto-updater, no native code. Game, mod and blueprint files are only ever read. A unit test fails the build if network code or foreign URLs are ever added. Details: [PRIVACY.md](PRIVACY.md).

## Build from source

Requirements: Windows, [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or newer.

```powershell
git clone https://github.com/Rathio12/SE_BlueprintEditor.git
cd SE_BlueprintEditor
dotnet test                                    # core tests
dotnet run --project src/SEBlueprint.App       # run the app
dotnet publish src/SEBlueprint.App -c Release -o out/portable   # single portable exe
```

Command line: `SEBlueprintInspector.exe <blueprint folder | bp.sbc>` opens that blueprint; `--page profiles|settings|info` picks the start tab.

## Releasing

Releases are automatic. The version lives in [`Directory.Build.props`](Directory.Build.props).

```powershell
./tools/release.ps1 -Version 1.1.0      # runs tests, bumps the version, commits and pushes
```

On every push to `main`, GitHub Actions ([release.yml](.github/workflows/release.yml)):

1. runs the tests,
2. rewrites the `## [Unreleased]` part of [CHANGELOG.md](CHANGELOG.md) from the commit messages since the last release,
3. if the version has no `vX.Y.Z` tag yet: turns *Unreleased* into the new version section, builds the portable exe,
   zips it with a SHA-256 checksum, tags the commit and publishes the GitHub release with those notes.

Use [Conventional Commits](https://www.conventionalcommits.org/) so the changelog sorts itself:
`feat: …` → **Added**, `fix: …` → **Fixed**, `perf/refactor/docs: …` → **Changed**, `chore/ci/test/build: …` are left out.

## Project layout

```
src/SEBlueprint.Core     game data, mods, blueprint analysis, cost, limits (no UI)
src/SEBlueprint.App      Windows app (WPF, custom UI)
tests/                   xUnit tests with small fixture files
tools/release.ps1        release script
assets/icon/             app icon source (build_icon.py)
docs/                    screenshots, wiki pages, design notes
```

## Contributing

Bug reports and pull requests are welcome — see [CONTRIBUTING.md](CONTRIBUTING.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) and [SECURITY.md](SECURITY.md).

## License

Dual-licensed under **MIT OR GPL-3.0-or-later** — use whichever fits your project. See [LICENSE](LICENSE), [LICENSE-MIT](LICENSE-MIT) and [LICENSE-GPL](LICENSE-GPL).

## Credits

* Inspired by [SE-BlueprintEditor](https://github.com/ScriptedEngineer/SE-BlueprintEditor) by ScriptedEngineer. This is an independent rewrite — no code from that project is used.
* [BCnEncoder.NET](https://github.com/Nominom/BCnEncoder.NET) (MIT) decodes the game's icons.
* Space Engineers, its data and icons © Keen Software House. Not affiliated with or endorsed by Keen Software House.
