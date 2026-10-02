<div align="center">

<img src="assets/icon/app-icon-256.png" width="64" alt="SE Blueprint Inspector logo">

# Changelog

[![Release](https://img.shields.io/github/v/release/Rathio12/SE_BlueprintEditor?style=flat-square&label=latest&labelColor=0B151B&color=46B4E6)](https://github.com/Rathio12/SE_BlueprintEditor/releases/latest)
![Keep a Changelog](https://img.shields.io/badge/format-Keep%20a%20Changelog-5D7682?style=flat-square&labelColor=0B151B)
![Versions](https://img.shields.io/badge/versions-1.x.y-F0A030?style=flat-square&labelColor=0B151B)

<sub>Generated from commit messages on every push · `1.x.y` — x = build, y = fixes</sub>

</div>

## [Unreleased]

### Added
- Search-friendly website (title, description, canonical, social cards, structured data, welcome section)
- WIKI tab on the website
- Website shows real game icons decoded locally from your own Space Engineers install
- Website version (same engine, same look) built into docs/ for GitHub Pages
- Profile editor shows descriptions and per-grid PCU
- Report-a-bug and support links in settings, occasional star-on-GitHub prompt
- Per-grid PCU limit and built-in Sigma Draconis Expanse and modded server profiles

### Changed
- Sitemap, robots.txt, 404 page and README wiki links
- Wiki (how game files, blueprints, mods, cost and limits work) as markdown and static pages
- Publish website to docs/, link it from README, rebuild it on each release
- Shared ReportView in Core so app and website show identical numbers
- Cleaner README with grouped badges, SE2 coming soon, release guide

### Fixed
- Keep UTF-8 characters intact when publishing the site and updating the changelog
- Keep website files byte-exact so browser integrity checks pass

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
