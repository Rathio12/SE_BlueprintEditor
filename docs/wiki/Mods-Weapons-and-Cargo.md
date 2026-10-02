# Mods, weapons and cargo

## How mods are read

Every Workshop item that has a `Data` folder is treated as a mod. From each mod the app reads:

- every `Data\**\*.sbc` file (blocks, items, recipes),
- `Data\Localization\MyTexts.resx` (names),
- `Data\Scripts\**\*.cs` files — **as plain text only**, to find weapon definitions. Scripts are never compiled or run.

Mods are read in parallel and merged in a fixed order, so the result is the same every time.

## Mods only affect blueprints that use them

Each mod's blocks, items and recipes are stored **separately**. When a blueprint is analysed, a definition is
looked up in this order:

1. the mods the blueprint lists in `<Mods>` (in that order),
2. vanilla,
3. any other mod on your PC (counted, but marked as coming from a mod the blueprint didn't list).

This matters: one mod on the test PC redefined the vanilla steel plate recipe (21 → 14 iron). Without per-mod
data, every blueprint would have shown wrong costs just because that mod was installed.

## Weapons

| Kind | How it is recognised |
|:--|:--|
| Turret | Block type `LargeGatlingTurret`, `LargeMissileTurret`, `InteriorTurret` or any type ending in `Turret` |
| Fixed gun | Block type `SmallGatlingGun`, `SmallMissileLauncher`, `SmallMissileLauncherReload`, or any block with a `WeaponDefinitionId` |
| WeaponCore / CoreSystems | See below |

**WeaponCore** weapons usually appear in the `.sbc` as `ConveyorSorter` blocks, so their type says nothing. Their
real definitions are C# files in the mod's `Data\Scripts` folder:

```csharp
new MountPointDef {
    SubtypeId = "M12Swarm",          ← the block this weapon belongs to
    AzimuthPartId = "None",          ← "None" = fixed gun, a part name = rotating turret
    ElevationPartId = "None",
}
```

The text is scanned for every `MountPointDef`; matching blocks of that mod become fixed guns or turrets.

## Cargo

- If a container defines `<InventorySize>` (in m³), cargo = X × Y × Z × 1000 liters.
- Otherwise the game's default applies: block volume × 1000 liters, with a cube edge of 2.5 m (large grid) or
  0.5 m (small grid). Example: a 3×3×3 large container = 27 × 2.5³ × 1000 = **421,875 L**, matching the game.

Next: [Cost calculation](Cost-Calculation.md)
