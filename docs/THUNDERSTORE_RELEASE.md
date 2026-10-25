# Reduced Thunderstore dependencies — release preparation

## Why did version 1.0.1 show 30 required packages?

The public Thunderstore release declares a broad R2API dependency. The legacy R2API suite and its submodules cause a long chain of installed packages. Thunderstore shows both direct and transitive dependencies.

The **current GitHub source v1.1.1** does not import or call `R2API`. Its runtime uses BepInEx and the built-in Risk of Rain 2/Unity APIs. NuGet `RiskOfRain2.GameLibs` and `UnityEngine.Modules` are compile-time reference assemblies, not Thunderstore dependencies.

## Minimal manifest

`thunderstore/manifest.json` declares:

```json
"dependencies": ["bbepis-BepInExPack-5.4.2122"]
```

That is **one direct dependency**. BepInExPack itself has required packages; the installer may therefore display several additional packages. These transitive prerequisites should not be manually duplicated in the mod manifest. No R2API modules are needed by the current source.

## Creating a candidate zip

1. Run `dotnet build GiveEliteItem.sln -c Release`.
2. Run `python scripts/validate_manifest.py`.
3. Run `python scripts/package_thunderstore.py`.

The packager uses the author's own icon from `thunderstore/icon.png`. If it does not exist, it attempts to recover the **original icon** from the already-public `CONDE/PlayWithEliteItem/1.0.1` release. You can also copy `icon.png` from that ZIP manually; never invent a replacement logo.

GitHub Actions runs validation and builds a candidate archive. The workflow **does not upload to Thunderstore**. Inspect the ZIP and validate it at https://thunderstore.io/tools/manifest-v1-validator/ .

## Publishing later

- In the game's host/server profile, validate elite passive effects, late joins, disconnects, stages, and vanilla-client multiplayer interoperability.
- Install the candidate from a clean profile and confirm that the dependency resolver no longer installs R2API.
- Upload the candidate ZIP under the **existing CONDE team** and package name **PlayWithEliteItem**.
- Versions on Thunderstore are immutable: this needs a new version, not an edit of the existing 1.0.1.
- **Do not** change source-version behavior just to shrink dependencies. In particular, public 1.0.1 uses the original second-slot mechanism; source 1.1.1 behaves differently.
