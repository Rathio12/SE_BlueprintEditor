# SE Blueprint Inspector — Wiki

How the app and the website work under the hood: where data comes from, how game files are read,
and how every number on screen is calculated.

| Page | What you'll learn |
|:--|:--|
| [How it works](How-It-Works.md) | The three parts (Core, Windows app, website) and how data flows through them |
| [Reading game data](Reading-Game-Data.md) | How Steam, the game and mods are found and which files are read |
| [Reading blueprints](Reading-Blueprints.md) | What is inside a `bp.sbc`, gzip blueprints, SE2 blueprints |
| [Mods, weapons and cargo](Mods-Weapons-and-Cargo.md) | Per-mod data, WeaponCore detection, cargo volume |
| [Cost calculation](Cost-Calculation.md) | Components → ingots → ore, assembler speed and yield modules |
| [Limits and profiles](Limits-and-Profiles.md) | How limit checks work and the profile JSON format |
| [Building and releasing](Building-and-Releasing.md) | Build, tests, the offline guard, versions, releases, in-app updates and the website |
| [FAQ](FAQ.md) | Common questions |

**Quick facts**

- Everything runs **locally**. The app only goes online for an update check you turn on; the website reads files inside your browser.
- Game, mod and blueprint files are **only read**, never changed.
- The Windows app and the website share the same engine (`SEBlueprint.Core`), so they show the same numbers.

Back to the [README](../../README.md) · [Open the website](https://rathio12.github.io/SE_BlueprintEditor/)
