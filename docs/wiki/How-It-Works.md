# How it works

The project is three pieces that share one engine.

```text
                    ┌──────────────────────────────┐
                    │        SEBlueprint.Core       │
                    │  (no UI, plain .NET 8 code)   │
                    │                               │
 game files ───────▶│  Paths      find Steam + game │
 mod folders ──────▶│  Data       read definitions  │
 bp.sbc ───────────▶│  Blueprints read blueprints   │
                    │  Analysis   totals + cost     │
                    │  Limits     profile checks    │
                    │  Presentation  what to show   │
                    └──────┬───────────────┬────────┘
                           │               │
             ┌─────────────▼───┐   ┌───────▼──────────────┐
             │ SEBlueprint.App │   │   SEBlueprint.Web     │
             │ Windows (WPF)   │   │   Blazor WebAssembly  │
             │ portable .exe   │   │   runs in the browser │
             └─────────────────┘   └──────────────────────┘
```

## SEBlueprint.Core

All the logic lives here and has no user interface code, which is why the website can run exactly the
same calculations as the Windows app.

| Folder | Responsibility |
|:--|:--|
| `Paths` | Finds Steam, every Steam library, Space Engineers 1 and 2, the Workshop folder and blueprint folders |
| `Data` | Reads block, item and recipe definitions (`.sbc`), localisation and mod scripts; caches the result |
| `Blueprints` | Reads `bp.sbc` files: grids, blocks, orientation and the mods a blueprint lists |
| `Analysis` | Turns a blueprint into a report: blocks, PCU, mass, guns, cargo, thrust, power, jump range, cost |
| `Limits` | Limit profiles and the check that compares a report with a profile |
| `Presentation` | Builds the readouts, limit lines and cost lists both front ends display |
| `Games` | One adapter per game (SE1 full analysis, SE2 listing) |

## The Windows app

1. On start it picks its data folder (`SEBlueprintInspector-data` next to the exe).
2. It detects the game and mods, then loads the game database — from the cache if nothing changed.
3. It finds every blueprint folder and analyses all blueprints in parallel, filling the list as each finishes.
4. Selecting a blueprint shows its detail; changing the profile re-checks every blueprint instantly.
5. Game icons are decoded straight from the `.dds` files of your install.

## The website

The same Core is compiled to WebAssembly. Because a website cannot look into your folders on its own:

- Vanilla block data comes from a snapshot embedded in the site (exported from the game with `tools/SEBlueprint.Export`).
- Vanilla game icons are bundled with the site (`docs/icons`).
- You pick blueprint folders and mod folders yourself; the browser reads them locally.
- Profiles and options are kept in your browser's local storage.

Next: [Reading game data](Reading-Game-Data.md)
