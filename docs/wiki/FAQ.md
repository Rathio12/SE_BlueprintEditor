# FAQ

**Is it safe? Does it upload anything?**
No. The app has no network code at all (a test enforces it) and the website reads your files inside the browser.
See [PRIVACY.md](../../PRIVACY.md).

**Why does the exe import KERNEL32, USER32, ADVAPI32…?**
Every Windows program does. They come from the .NET runtime and WPF bundled into the portable exe — windows,
files, registry, clipboard, file dialogs. The project's own source contains no native calls.

**Some blocks are "unknown" — why?**
The blueprint uses a mod that isn't on your PC (or, on the website, that you haven't added). The missing mods are
listed with their Workshop IDs; subscribe in Steam (app) or add the mod folder in the MODS tab (website).

**Numbers differ from what the game shows.**
Check the assembler efficiency and yield modules in the build cost header, and whether the blueprint relies on
mods. If it still looks wrong, [report a bug](https://github.com/Rathio12/SE_BlueprintEditor/issues/new/choose)
with the blueprint and the mod IDs.

**Why don't game icons show on the website straight away?**
They are Keen Software House's artwork and are not hosted. Use *Load game icons* and pick
`…\SpaceEngineers\Content\Textures\GUI\Icons` from your own install.

**Does it work with Space Engineers 2?**
SE2 blueprints are listed with blocks and PCU. Full support is coming soon — see
[Reading blueprints](Reading-Blueprints.md#space-engineers-2-blueprints).

**Where are my settings?**
App: the `SEBlueprintInspector-data` folder next to the exe. Website: your browser's local storage.

Back to the [wiki home](Home.md)
