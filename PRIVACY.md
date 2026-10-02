# Privacy Policy

_Last updated: 2026-10-02_

SE Blueprint Inspector is a desktop tool that runs entirely on your computer.

## What the app does not do

* It does **not** connect to the internet. There is no telemetry, analytics, crash upload, advertising or auto-update.
* It does **not** collect, store or transmit personal data.
* It does **not** modify your game, mod or blueprint files. They are only read.
* It does **not** contain native (P/Invoke) code. An automated test in this repository fails if network code, native calls or foreign URLs are added to the app source.

## What the app reads

* The Steam install path from the Windows registry, Steam's `libraryfolders.vdf`, and the Space Engineers / Space Engineers 2 folders it finds there.
* Your blueprint folders under `%AppData%\SpaceEngineers` and `%AppData%\SpaceEngineers2`, plus any folder you add in Settings.
* Workshop mod folders, including mod script files, which are read as plain text and never executed.

## What the app stores

Everything is stored in the `SEBlueprintInspector-data` folder next to `SEBlueprintInspector.exe`
(or in `%LocalAppData%\SEBlueprintInspector` if the program folder is read-only):

| File | Contents |
|---|---|
| `settings.json` | Folder overrides, selected limit profile, cost options |
| `profiles/*.json` | Limit profiles you created or imported |
| `gamedb.json` | Cache of block/item/recipe data read from your game and mods |
| `log.txt` | Local diagnostic messages (file paths and error messages), capped at ~2 MB |

These files never leave your computer unless you share them yourself. Deleting the folder removes all of them.

## Links

The only link in the app is the "GitHub" button on the Info tab, which opens this project's page in your browser when you click it.

## Contact

Questions about this policy: open an issue at https://github.com/Rathio12/SE_BlueprintEditor/issues.
