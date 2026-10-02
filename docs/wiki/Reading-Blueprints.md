# Reading blueprints

## Space Engineers blueprints (`bp.sbc`)

A blueprint is a folder with `bp.sbc` (the ship) and usually `thumb.png` (the picture). `bp.sbc` is XML:

```xml
<ShipBlueprint xsi:type="MyObjectBuilder_ShipBlueprintDefinition">
  <Id Type="MyObjectBuilder_ShipBlueprintDefinition" Subtype="My Ship" />   ← name
  <DisplayName>PlayerName</DisplayName>
  <CubeGrids>
    <CubeGrid>                                                              ← one per grid (subgrids too)
      <GridSizeEnum>Large</GridSizeEnum>
      <CubeBlocks>
        <MyObjectBuilder_CubeBlock xsi:type="MyObjectBuilder_Thrust">        ← TypeId
          <SubtypeName>LargeBlockLargeThrust</SubtypeName>                  ← SubtypeId
          <BlockOrientation Forward="Backward" Up="Up" />                   ← used for thrust direction
        </MyObjectBuilder_CubeBlock>
        …
  <Mods>
    <ModItem FriendlyName="Some Weapons Pack">
      <PublishedFileId>1919062467</PublishedFileId>                         ← mods the blueprint needs
```

For every block the type and subtype give the block id (`Thrust/LargeBlockLargeThrust`), which is looked up in
the game database. Properties stored in the blueprint (colours, names, inventory contents) are not needed for
cost and limits and are ignored.

## What happens when a block is unknown

If no definition is found — usually because a mod is not on your PC — the block is listed under
**Unknown blocks** and the mods the blueprint lists but you don't have are shown with their Workshop IDs.
Known blocks are still counted; unknown ones are not guessed.

## Compressed blueprints

Newer game versions sometimes write `bp.sbc` gzip-compressed. Those files start with the bytes `1F 8B` and are
unpacked before reading. `bp.sbcB5` (the game's binary format) is not read; open the blueprint in the game once
and it writes a `bp.sbc`.

## Space Engineers 2 blueprints

SE2 stores blueprints differently:

| File | Content | Read? |
|:--|:--|:--|
| `.container-info` | JSON with title, description, dates, **block count** and **PCU** | yes |
| `grid.json.vrb` | The grid itself, in an undocumented binary format (`VR3B`) | not yet |
| `snapshot.vrb` | Preview data, binary | no |

That is why SE2 blueprints show blocks and PCU and can be checked against PCU / block limits, while cost,
weapons and cargo are **coming soon** — they need the grid file.

Next: [Mods, weapons and cargo](Mods-Weapons-and-Cargo.md)
