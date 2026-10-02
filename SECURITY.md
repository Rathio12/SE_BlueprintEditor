<div align="center">

<img src="assets/icon/app-icon-256.png" width="64" alt="SE Blueprint Inspector logo">

# Security Policy

![Supported](https://img.shields.io/github/v/release/Rathio12/SE_BlueprintEditor?style=flat-square&label=supported&labelColor=0B151B&color=46B4E6)
![Reports](https://img.shields.io/badge/reports-private%20advisory-F0A030?style=flat-square&labelColor=0B151B)

</div>

## Supported versions

| Version | Supported |
|:--|:--:|
| Latest `1.x` release | ✅ |
| Older releases | ❌ |

## Reporting a vulnerability

> [!IMPORTANT]
> Please **do not** open a public issue for security problems.

Report privately through GitHub: **[Security → Report a vulnerability](https://github.com/Rathio12/SE_BlueprintEditor/security/advisories/new)**.

Include the app version, what you did, what happened and, if possible, a sample file that triggers it.
You should get a first answer within 7 days.

## Scope

The app and website read untrusted files — blueprints, mod definitions and mod scripts. In scope:

- a crafted file causing code execution,
- writes outside the app's data folder,
- any network access from the app other than the opt-in update check to this repository's GitHub releases,
- an update being installed that is not the exact file published in this repository's release,
- mod scripts being compiled or run (they must only ever be read as text).

## Verifying downloads

Only download `SEBlueprintInspector.exe` from the [Releases](https://github.com/Rathio12/SE_BlueprintEditor/releases) page.
GitHub shows the SHA-256 of every release file next to it — compare it with your download:

```powershell
Get-FileHash .\SEBlueprintInspector.exe -Algorithm SHA256
```

The in-app updater does this check for you: it only installs a download whose SHA-256 matches the checksum
GitHub publishes for the release, and only from this repository.
