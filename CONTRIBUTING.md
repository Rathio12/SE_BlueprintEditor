<div align="center">

<img src="assets/icon/app-icon-256.png" width="64" alt="SE Blueprint Inspector logo">

# Contributing

![PRs welcome](https://img.shields.io/badge/PRs-welcome-5BC07A?style=flat-square&labelColor=0B151B)
![Conventional Commits](https://img.shields.io/badge/commits-conventional-46B4E6?style=flat-square&labelColor=0B151B)
![.NET](https://img.shields.io/badge/.NET-8-8E7CC3?style=flat-square&labelColor=0B151B&logo=dotnet&logoColor=white)

Bug reports, mod-compatibility reports and pull requests are all welcome.

</div>

## Reporting bugs

Open an issue with the **[Bug report](https://github.com/Rathio12/SE_BlueprintEditor/issues/new/choose)** template and attach:

- the app version (bottom right of the window),
- `log.txt` from the `SEBlueprintInspector-data` folder next to the exe,
- for wrong numbers: the blueprint (`bp.sbc`) and the Workshop IDs of the mods it uses.

## Development setup

| Need | Version |
|:--|:--|
| Windows | 10 or 11 |
| .NET SDK | 8 or newer |
| Space Engineers | Only to run the app — tests use small fixture files |

```powershell
dotnet test tests/SEBlueprint.Core.Tests
dotnet run --project src/SEBlueprint.App
dotnet run --project src/SEBlueprint.Web
```

## Ground rules

| Rule | Why |
|:--|:--|
| **Tests first** | Every change to `SEBlueprint.Core` comes with a test that fails before and passes after. |
| **Offline only** | No network code, telemetry, updater, native calls or foreign URLs — `OfflineGuardTests` enforces it. |
| **Never crash** | Every file and parse step is guarded; one bad blueprint or mod must never stop a scan. |
| **Read-only** | Never write to game, mod or blueprint folders. |
| **No code comments** | Keep code self-explanatory; explanations go in the PR or the [wiki](docs/wiki/Home.md). |
| **Invariant parsing** | Numbers in game files are parsed with `CultureInfo.InvariantCulture` (`Num.D/F/I`); numbers typed by users go through `UserNumbers`. |
| **Core stays UI-free** | `SEBlueprint.Core` is shared by the Windows app and the website. |

## Pull requests

1. Fork and create a branch (`fix/…`, `feat/…`).
2. Make the change with tests and run `dotnet test tests/SEBlueprint.Core.Tests`.
3. Write commit messages as [Conventional Commits](https://www.conventionalcommits.org/) — `feat:`, `fix:`, `docs:` …
   The [changelog](CHANGELOG.md) is generated from them automatically.
4. Open the PR with the template and describe how you verified it.

> [!NOTE]
> By contributing you agree that your contribution is licensed under the project's dual license (**MIT OR GPL-3.0-or-later**).
