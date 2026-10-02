<div align="center">

<img src="assets/icon/app-icon-256.png" width="64" alt="SE Blueprint Inspector logo">

# Privacy Policy

![Offline](https://img.shields.io/badge/app-fully%20offline-5BC07A?style=flat-square&labelColor=0B151B)
![No tracking](https://img.shields.io/badge/website-no%20tracking-5BC07A?style=flat-square&labelColor=0B151B)
![No uploads](https://img.shields.io/badge/files-never%20uploaded-5BC07A?style=flat-square&labelColor=0B151B)

<sub>Last updated: 2026-10-02</sub>

</div>

> [!TIP]
> **Short version:** nothing you open ever leaves your computer. There is no telemetry, no analytics, no account and no tracking.

## Windows app

| | |
|:--|:--|
| **Internet** | None. The app has no network code, no auto-updater and no crash upload. An automated test fails the build if network code, native calls or foreign URLs are added. |
| **Your files** | Game, mod and blueprint files are **only read**, never changed. Mod scripts are read as plain text and never executed. |
| **What it reads** | The Steam install path from the registry, Steam's `libraryfolders.vdf`, the Space Engineers 1/2 folders, your blueprint folders (`%AppData%\SpaceEngineers`, `%AppData%\SpaceEngineers2`) and any folder you add in Settings. |
| **Links** | The GitHub, Report a bug, Star and Releases buttons open this project's GitHub pages in your browser — only when you click them. |

### What the app stores

Everything lives in the `SEBlueprintInspector-data` folder next to `SEBlueprintInspector.exe`
(or `%LocalAppData%\SEBlueprintInspector` if the program folder is read-only):

| File | Contents |
|:--|:--|
| `settings.json` | Folder overrides, selected limit profile, cost options |
| `profiles/*.json` | Limit profiles you created or imported |
| `gamedb.json` | Cache of block / item / recipe data from your game and mods |
| `log.txt` | Local diagnostic messages (file paths and error messages), capped at ~2 MB |

Deleting that folder removes all of it.

## Website

| | |
|:--|:--|
| **Your files** | Blueprints, mods and (optionally) icon folders you pick are read **inside your browser** with WebAssembly. They are never uploaded. |
| **Storage** | Your limit profiles and cost options are kept in your browser's local storage. Clear the site data to remove them. |
| **Tracking** | No analytics, no cookies, no third-party scripts, fonts or embeds. |
| **Hosting** | The site is hosted on GitHub Pages; GitHub may keep standard server logs as described in [GitHub's privacy statement](https://docs.github.com/en/site-policy/privacy-policies/github-general-privacy-statement). |

## Contact

Questions about this policy: [open an issue](https://github.com/Rathio12/SE_BlueprintEditor/issues).
