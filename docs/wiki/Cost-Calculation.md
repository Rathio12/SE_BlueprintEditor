# Cost calculation

## 1. Components

Each block definition lists the components needed to build it. The blueprint's components are the sum over all
known blocks. **Mass** is the sum of the components' masses (an empty ship, no cargo or ammo).

## 2. Ingots

Assembler recipes turn ingots into components. Recipes are normalised to "inputs per one result":

```text
Steel Plate  ←  21 Iron Ingot           (vanilla)
```

For every component: `ingots += count × input per unit ÷ assembler efficiency`.

- **Assembler efficiency** is the world's assembler setting: Realistic = 1, x3 = 3, x10 = 10.
- If a recipe needs another product that has its own recipe (a modded part, a hydrogen bottle, …), that product is
  broken down further first. Loops between recipes are stopped after 16 levels.

## 3. Ore

Refinery recipes turn ore into ingots:

```text
Iron Ingot  ←  1 / 0.7 = 1.4286 Iron Ore
```

For every ingot: `ore += amount × ore per ingot ÷ yield`.

| Yield modules on the refinery | Yield multiplier |
|:--:|:--:|
| 0 | 1.00 |
| 1 | 1.19 |
| 2 | 1.41 |
| 3 | 1.68 |
| 4 | 2.00 |

Only real ores end up in the ore list; anything else a modded refinery recipe consumes is left out.

## Worked example

10 steel plates, realistic assembler, no yield modules:

```text
components : 10 Steel Plate
ingots     : 10 × 21           = 210 kg Iron Ingot
ore        : 210 × 1.4286      = 300 kg Iron Ore
```

With an x3 assembler the ingots become 70 kg; with 4 yield modules the ore becomes 150 kg.

## Copy and CSV

**Copy** puts all three lists on the clipboard tab-separated (paste straight into Excel or Google Sheets).
**CSV** saves `Type,Item,Amount` rows; ingots and ore are in kg.

Next: [Limits and profiles](Limits-and-Profiles.md)
