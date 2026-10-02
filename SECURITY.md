# Security Policy

## Supported versions

Only the latest release receives fixes.

| Version | Supported |
|---|---|
| latest `1.x` | ✅ |
| older | ❌ |

## Reporting a vulnerability

Please **do not** open a public issue for security problems.

Use GitHub's private reporting instead: **Security → Report a vulnerability** on
https://github.com/Rathio12/SE_BlueprintEditor/security/advisories/new

Include the app version, what you did, what happened and, if possible, a sample file that triggers it.
You should get a first answer within 7 days.

## Scope

The app reads untrusted files (blueprints, mod definitions, mod scripts). Bugs where a crafted file
causes code execution, writes outside the app's data folder, or reaches the network are in scope.
Mod scripts are only read as text and never compiled or run; a report showing otherwise is high priority.

## Verifying downloads

Every release lists a SHA-256 checksum (`.sha256` file). Compare it before running the exe:

```powershell
Get-FileHash .\SEBlueprintInspector-v1.0.0-win-x64.zip -Algorithm SHA256
```
