# Changelog

All notable changes to this project are documented here.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- Profile editor shows descriptions and per-grid PCU
- Report-a-bug and support links in settings, occasional star-on-GitHub prompt
- Per-grid PCU limit and built-in Sigma Draconis Expanse and modded server profiles

### Changed
- Cleaner README with grouped badges, SE2 coming soon, release guide

## [1.0.0] - 2026-10-02

### Added
- Portable single-file Windows app (`SEBlueprintInspector.exe`), no installer and no .NET install needed; data is kept in `SEBlueprintInspector-data` next to the exe.
- Automatic detection of Steam libraries, Space Engineers, Workshop mods and all blueprint folders (local, cloud, Workshop).
- Blueprint list with search, game/source filters, sorting and a live OK / near / over status per blueprint.
- Blueprint detail: blocks, PCU, guns (fixed + turrets), cargo, mass, thrust, power and jump range with segmented limit gauges.
- Build cost (components, ingots, ore) with real in-game icons, assembler and yield-module options, copy and CSV export.
- Limit profiles from the game's world presets plus your own server profiles (JSON import/export, per-block-type limits).
- Modded blocks, items and recipes, applied per mod; WeaponCore / CoreSystems weapons recognised as guns or turrets.
- Missing-mod report listing Workshop IDs of blocks that could not be identified.
- Space Engineers 2 blueprints listed with blocks and PCU.
- Gzip-compressed blueprint and definition files.
- Fast restarts through a game-data cache that rebuilds itself when the game or mods change.
- Fully offline: no network access, telemetry, updater or native code (enforced by a test).

[Unreleased]: https://github.com/Rathio12/SE_BlueprintEditor/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Rathio12/SE_BlueprintEditor/releases/tag/v1.0.0
