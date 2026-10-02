# Contributing

Thanks for helping! Bug reports, mod-compatibility reports and pull requests are all welcome.

## Reporting bugs

Open an issue with the **Bug report** template. The most useful things to attach:

* The app version (bottom right of the window).
* `log.txt` from the `SEBlueprintInspector-data` folder next to the exe.
* For wrong numbers: the blueprint (`bp.sbc`) and the Workshop IDs of the mods it uses.

## Development setup

* Windows 10/11
* .NET 8 SDK or newer
* Space Engineers installed (for running the app; the tests use small fixture files and need no game)

```powershell
dotnet test
dotnet run --project src/SEBlueprint.App
```

## Ground rules for code

* **Tests first.** Every change to `SEBlueprint.Core` comes with a test in `tests/SEBlueprint.Core.Tests` that fails before the change and passes after it.
* **Offline only.** No network code, telemetry, updater, native/P-Invoke calls or URLs other than this repository. `OfflineGuardTests` enforces this.
* **Never crash.** Every file and parse operation is guarded; one bad blueprint or mod must never stop a scan.
* **Read-only.** The app never writes to game, mod or blueprint folders.
* **No code comments.** Keep code self-explanatory through naming; put explanations in the PR description or the docs.
* **Invariant parsing.** Numbers in game files are parsed with `CultureInfo.InvariantCulture` (`Num.D/F/I`).
* Keep `SEBlueprint.Core` free of WPF references.

## Pull requests

1. Fork, create a branch (`fix/…`, `feat/…`).
2. Make the change with tests; run `dotnet test`.
3. Add a line under `## [Unreleased]` in `CHANGELOG.md`.
4. Open the PR using the template and describe how you verified it.

By contributing you agree that your contribution is licensed under the project's dual license (MIT OR GPL-3.0-or-later).
