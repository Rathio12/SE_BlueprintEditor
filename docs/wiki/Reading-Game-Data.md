# Reading game data

## Finding Steam and the game

1. Read `HKEY_CURRENT_USER\Software\Valve\Steam\SteamPath` from the registry (the only registry value the app reads).
   If it is missing, `C:\Program Files (x86)\Steam` is tried.
2. Read `steamapps\libraryfolders.vdf` and collect every `"path"` entry — those are all your Steam libraries.
3. In each library look for:

| Looked for | Meaning |
|:--|:--|
| `steamapps\common\SpaceEngineers` | Space Engineers install |
| `steamapps\workshop\content\244850` | Workshop items (mods and blueprints) |
| `steamapps\common\SpaceEngineers2` | Space Engineers 2 install |

Blueprint folders come from `%AppData%\SpaceEngineers\Blueprints\local`, `\cloud` and `\workshop`, the Workshop
folder itself, and `%AppData%\SpaceEngineers2\AppData\Blueprints` for SE2. Any path can be overridden in Settings.

## Which game files are read

All files are under `SpaceEngineers\Content\Data`:

| Files | Gives us |
|:--|:--|
| `CubeBlocks\*.sbc` | Every block: id, size, PCU, components, inventory size, thrust, power, jump drive values |
| `Components*.sbc`, `PhysicalItems*.sbc`, `AmmoMagazines*.sbc` | Items with mass, volume and icon |
| `Blueprints*.sbc` and `Blueprints\**\*.sbc` | Assembler and refinery recipes |
| `Localization\MyTexts.resx` | English names for keys like `DisplayName_Item_SteelPlate` |
| `..\CustomWorlds\*\Sandbox_config.sbc` | The game's world presets, used for the vanilla limit profiles |

### A block definition

```xml
<Definition xsi:type="MyObjectBuilder_CargoContainerDefinition">
  <Id><TypeId>CargoContainer</TypeId><SubtypeId>LargeBlockLargeContainer</SubtypeId></Id>
  <DisplayName>DisplayName_Block_LargeContainer</DisplayName>
  <CubeSize>Large</CubeSize>
  <Size x="3" y="3" z="3" />
  <PCU>10</PCU>
  <Components>
    <Component Subtype="InteriorPlate" Count="360" />
    …
  </Components>
</Definition>
```

The id becomes `CargoContainer/LargeBlockLargeContainer` (`MyObjectBuilder_` is always removed).

### Reading rules that keep numbers correct

- **Numbers are culture independent.** `1.5` is always one and a half, on German, Russian or English Windows alike.
- **Gzip is detected automatically.** Some files start with the gzip marker `1F 8B` and are unpacked transparently.
- **One bad file never stops loading.** A broken `.sbc` is skipped and written to `log.txt`.
- **Recipes with several outputs** (stone → gravel + iron + nickel + silicon) are not used for cost, otherwise
  stone would replace iron ore in every cost list. Scrap and ice recipes are skipped for the same reason.

## The cache

Reading 250+ mods takes a while the first time, so the result is stored in `gamedb.json` in the data folder.
It is rebuilt automatically when the game files, the list of mods, any mod folder or the app version changes.
**Settings → Rebuild game data** forces a rebuild.

Next: [Reading blueprints](Reading-Blueprints.md)
