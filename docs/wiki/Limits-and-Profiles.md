# Limits and profiles

## What is checked

| Check | Compared value |
|:--|:--|
| PCU | Total PCU of all grids |
| Largest grid PCU | PCU of the biggest grid (for servers that limit PCU per grid) |
| Blocks | All blocks |
| Largest grid | Blocks in the biggest grid |
| Guns | Fixed guns + turrets |
| Turrets | Turrets |
| Cargo (L) | Total cargo volume |
| Type: *name* | Count of blocks with that block pair name (e.g. `Assembler`, `Refinery`, `JumpDrive`) |
| *Rule* per grid | Blocks matching a grouped rule in the grid that has the most of them |
| *Rule* per player | Blocks matching a grouped rule in the whole blueprint |

## Status

| Status | When |
|:--|:--|
| **OK** | below 90 % of the limit |
| **NEAR** | from 90 % up to the limit |
| **OVER** | above the limit |
| **INCOMPLETE** | nothing is over, but blocks from missing mods could not be read |
| — | no limit set |

A blueprint's overall status is its worst check. When a profile has only a per-grid limit, the PCU and block gauges
use that per-grid check.

## Built-in profiles

| Profile | Source |
|:--|:--|
| Vanilla – No limits | Always present |
| Vanilla – *world* | Read from the game's own world presets (`CustomWorlds\*\Sandbox_config.sbc`); identical presets are merged |
| Sigma Draconis – Expanse | The server's published limit: 50,000 PCU per grid (sigmadraconis.games) |
| Stone Industries (SI) | 40,000 blocks per grid, no PCU limit and SI's 22 grouped rules from the SI Gaming #block-limits channel |
| Modded server (typical) | A starting point for modded survival servers |

Built-in profiles are read-only — duplicate one to change it.

## Profile files

Your own profiles are saved as JSON (app: `SEBlueprintInspector-data\profiles\`; website: browser storage,
export/import as files). Share them with your server's players:

```json
{
  "Name": "My Server",
  "TotalPcu": 100000,
  "MaxPcuPerGrid": 50000,
  "MaxBlocksPerGrid": 25000,
  "MaxBlocksTotal": null,
  "MaxGuns": 40,
  "MaxTurrets": 30,
  "MaxCargoLiters": 1500000,
  "BlockTypeLimits": { "Assembler": 6, "Refinery": 6, "JumpDrive": 8 }
}
```

## Grouped rules

Many servers limit *any combination* of blocks — "10 reactors of any tier per grid", "10 drills per player".
A grouped rule has a name, a scope, a maximum and the blocks that count:

```json
"GroupLimits": [
  { "Name": "Reactors", "Scope": "Grid", "Max": 10, "Blocks": [ "Reactor" ] },
  { "Name": "T4 + T5 reactors", "Scope": "Grid", "Max": 6, "Blocks": [ "Reactor/*16x", "Reactor/*32x" ] },
  { "Name": "Drills", "Scope": "Player", "Max": 10, "Blocks": [ "Drill", "!Drill/Goliath*" ] },
  { "Name": "Shield Air Pressurizer", "Scope": "Player", "Max": 0, "Blocks": [ "OxygenGenerator/DSSupergen" ] }
]
```

| Part | Meaning |
|:--|:--|
| `Blocks` | `TypeId` matches every block of that type; `TypeId/Subtype` matches one block. `*` is a wildcard, `!` excludes. Case doesn't matter. |
| `Scope: Grid` | Checked against the grid with the most matching blocks. |
| `Scope: Player` | Checked against the whole blueprint. Your other grids on the server count too, so leave some room. |
| `Max: 0` | The block is not allowed at all. |

Block IDs are the ones in the blueprint's `bp.sbc` (`MyObjectBuilder_` removed) — e.g. `Reactor/LargeBlockLargeGenerator8x`.
Rules that are over are listed first in the limit check; rules with no matching blocks are hidden. The profile editor
shows the rules read-only — change them by exporting the profile, editing the JSON and importing it again.

`null`, `0` or a missing field means "no limit". In the profile editor numbers can be typed with or without
thousands separators in your system's format (`50,000`, `50.000`, `50 000`); invalid input is rejected and the
profile is not saved. Block type keys are **block pair names** as used in a world's
`BlockTypeLimits` setting.

Next: [Building and releasing](Building-and-Releasing.md)
