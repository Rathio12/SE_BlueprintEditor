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

## Status

| Status | When |
|:--|:--|
| **OK** | below 90 % of the limit |
| **NEAR** | from 90 % up to the limit |
| **OVER** | above the limit |
| — | no limit set |

A blueprint's overall status is its worst check.

## Built-in profiles

| Profile | Source |
|:--|:--|
| Vanilla – No limits | Always present |
| Vanilla – *world* | Read from the game's own world presets (`CustomWorlds\*\Sandbox_config.sbc`); identical presets are merged |
| Sigma Draconis – Expanse | The server's published limit: 50,000 PCU per grid (sigmadraconis.games) |
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

`null`, `0` or a missing field means "no limit". Block type keys are **block pair names** as used in a world's
`BlockTypeLimits` setting.

Next: [Building and releasing](Building-and-Releasing.md)
