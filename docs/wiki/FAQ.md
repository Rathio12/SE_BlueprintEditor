# FAQ

**Is it safe? Does it upload anything?**
No. The app only goes online for the opt-in update check (off by default), which talks to this repository's GitHub
releases and nothing else — a test enforces that. The website reads your files inside the browser.
See [PRIVACY.md](../../PRIVACY.md).

**Why does the exe import KERNEL32, USER32, ADVAPI32…?**
Every Windows program does. They come from the .NET runtime and WPF bundled into the portable exe — windows,
files, registry, clipboard, file dialogs. The project's own source contains no native calls.

**Some blocks are "unknown" — why?**
The blueprint uses a mod that isn't on your PC (or, on the website, that you haven't added). The blueprint is
marked **INCOMPLETE** and the missing mods are listed with their Workshop IDs; subscribe in Steam (app) or add the mod folder in the MODS tab (website).

**Numbers differ from what the game shows.**
Check the assembler efficiency and yield modules in the build cost header, and whether the blueprint relies on
mods. If it still looks wrong, [report a bug](https://github.com/Rathio12/SE_BlueprintEditor/issues/new/choose)
with the blueprint and the mod IDs.

**Why does a modded item show a plain icon on the website?**
Vanilla game icons are built into the site. Icons of modded items come from the mod folders you add in the MODS tab;
you can also use *Load game icons* there and pick `…\SpaceEngineers\Content\Textures\GUI\Icons` from your install.

**A blueprint says INCOMPLETE — what does that mean?**
Some of its blocks come from mods that aren't on your PC (or not added on the website). Those blocks still count
towards block limits, but their PCU, cost, guns and cargo are unknown, so the totals are too low. Install or add the
listed mods to get the full numbers.

**Can I type `50,000` or `50.000` in a profile?**
Yes. Limits accept the number format of your system, with or without thousands separators. An empty field or `0`
means no limit; anything that isn't a whole positive number is rejected with a message instead of being dropped.

**Does it work with Space Engineers 2?**
SE2 blueprints are listed with blocks and PCU. Full support is coming soon — see
[Reading blueprints](Reading-Blueprints.md#space-engineers-2-blueprints).

**How do I update the app?**
Settings → **Check now**, or tick **Check for updates on start**. When a new version is out, *Install and restart*
replaces the exe in place — no re-download from the website, settings and profiles stay. If the app's folder is
read-only, use *Release page* and replace the exe by hand.

**Where are my settings?**
App: the `SEBlueprintInspector-data` folder next to the exe. Website: your browser's local storage.

Back to the [wiki home](Home.md)
