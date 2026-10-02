# Building and releasing

## Build and run

```powershell
dotnet test tests/SEBlueprint.Core.Tests                        # all tests
dotnet run --project src/SEBlueprint.App                        # Windows app
dotnet run --project src/SEBlueprint.Web                        # website on localhost
dotnet publish src/SEBlueprint.App -c Release -o out/portable   # single portable exe
./tools/publish-site.ps1                                        # build the website into docs/
dotnet run --project tools/SEBlueprint.Export                   # refresh the website's vanilla data snapshot
```

## Tests

`tests/SEBlueprint.Core.Tests` covers the engine with small fixture files — no game install needed:
number parsing (game files and typed-in limits), path detection, definition parsing, mods and WeaponCore, cache, blueprint analysis, cost math,
limits, profiles, SE2 metadata and gzip files.

### The offline guard

`OfflineGuardTests` scans all source in `src/` and **fails the build** if it finds network code
(`HttpClient`, `WebClient`, sockets, `fetch(`…) anywhere except `Services/Updater.cs`, native calls
(`DllImport`, `LibraryImport`) anywhere, or any URL other than this repository and its release API. It also checks that
the updater has no URLs of its own, verifies checksums, and that update checks are off by default. That keeps the
privacy promise enforceable instead of just written down.

## In-app updates

The update check is **off by default**. Settings → *Check now*, or *Check for updates on start*:

1. `GET https://api.github.com/repos/Rathio12/SE_BlueprintEditor/releases/latest` — drafts and pre-releases are ignored.
2. `ReleaseFeed` (in Core, unit-tested) reads the version, notes, the `SEBlueprintInspector.exe` asset and its
   `sha256` digest. Download links outside `github.com/Rathio12/SE_BlueprintEditor/releases/download/` are rejected.
3. *Install and restart* streams the exe to `SEBlueprintInspector.exe.download` next to the app, hashing as it goes.
   Wrong size or checksum → the download is deleted and nothing changes.
4. The running exe is renamed to `.old`, the new one takes its name, the new version starts and the old one exits.
   The new version deletes `.old` on start.

Copies that can't replace themselves (read-only folder, `dotnet run`) get a *Release page* button instead.

## Versions

`1.x.y` — `x` is the build, `y` the fixes on that build. The single source is `<Version>` in `Directory.Build.props`.

## Releasing

```powershell
./tools/release.ps1          # next build:  1.1.0 -> 1.2.0
./tools/release.ps1 -Fix     # next fix:    1.2.0 -> 1.2.1
```

The script runs the release checklist (below), bumps the version and pushes. GitHub Actions then:

1. runs the tests,
2. regenerates the *Unreleased* part of `CHANGELOG.md` from commit messages,
3. for a new version: dates the changelog section, builds the exe, rebuilds the website in `docs/`,
   tags the commit and publishes a release with only `SEBlueprintInspector.exe` attached.

### Release checklist (enforced by `tools/release.ps1`)

1. **README up to date** — if features were added since the last release, `README.md` must have changed too.
2. **Changelog up to date** — the *Unreleased* section must contain the changes going out.
3. **No known bugs** — all tests pass, the app, website and tools build with **zero warnings**, and every
   relative link in the README and wiki points to a file that exists.

### Commit messages

[Conventional Commits](https://www.conventionalcommits.org/) feed the changelog:
`feat:` → Added, `fix:` → Fixed, `perf` / `refactor` / `docs` → Changed; `chore`, `ci`, `test`, `build` are left out.

## The website

GitHub Pages serves the `docs/` folder of `main` (Settings → Pages → *Deploy from a branch*, `main`, `/docs`).
`tools/publish-site.ps1` builds `src/SEBlueprint.Web` into `docs/` and only replaces the files it owns;
`docs/wiki` and `docs/images` are never touched. A `.nojekyll` file makes GitHub serve the files unchanged.

Next: [FAQ](FAQ.md)
