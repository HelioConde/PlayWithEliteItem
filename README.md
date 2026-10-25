<div align="center">

<h1>⚡ PlayWithEliteItem</h1>

<strong>Risk of Rain 2 mod · Random elite aspects at the start of a run</strong>

<p>Give runs an extra twist by distributing elite equipment to players automatically. Built in C# with BepInEx and the Risk of Rain 2 API.</p>

<a href="https://thunderstore.io/c/riskofrain2/p/CONDE/PlayWithEliteItem/"><img alt="Published on Thunderstore" src="https://img.shields.io/badge/Thunderstore-Published-15aa84?style=for-the-badge"></a>
<a href="https://github.com/HelioConde/PlayWithEliteItem"><img alt="C# and .NET" src="https://img.shields.io/badge/C%23-.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white"></a>
<a href="LICENSE"><img alt="MIT license" src="https://img.shields.io/badge/License-MIT-334155?style=for-the-badge"></a>

<p><a href="https://thunderstore.io/c/riskofrain2/p/CONDE/PlayWithEliteItem/"><strong>Install published mod</strong></a> · <a href="#building-from-source"><strong>Build from source</strong></a> · <a href="#compatibility--testing"><strong>Compatibility</strong></a></p>

</div>

---

## 🎮 What does the mod do?

On run start, PlayWithEliteItem assigns an **elite aspect equipment** to each eligible player. The passive bonus associated with that elite equipment changes how the run feels without requiring the player to pick up the item manually.

This is a **community mod**, not an official Risk of Rain 2 feature.

### Published version and development version are different

| | Thunderstore release | Current GitHub source |
| --- | --- | --- |
| **Version** | **1.0.1** | **1.1.1** |
| **Status** | Distributed publicly | Build and regression checks; in-game validation pending |
| **Equipment behavior** | Original second-slot implementation described in the published listing | Uses the active equipment slot (index `0`) so the elite passive buff can apply |
| **How to get it** | [Thunderstore package](https://thunderstore.io/c/riskofrain2/p/CONDE/PlayWithEliteItem/) | [GitHub Actions artifact](https://github.com/HelioConde/PlayWithEliteItem/actions), or compile the source |

The Thunderstore page displayed **approximately 4,000 downloads** in October 2026. This is a dated milestone, not a live counter.

> **Do not mistake source version 1.1.1 for a released Thunderstore update.** The modernized code is under test. Installing the Thunderstore package currently means installing the separately published 1.0.1 build.

## 📦 Minimal Thunderstore dependencies (next release)

The currently published **1.0.1** package on Thunderstore still lists approximately **30 required packages** because it includes the old full R2API distribution. These reflect the **published package**, not the newer GitHub source.

The refactored **1.1.1** plugin does not use R2API. A [new minimal Thunderstore manifest](thunderstore/manifest.json) now declares just **one direct dependency**:

```json
"dependencies": ["bbepis-BepInExPack-5.4.2122"]
```

BepInExPack has its own *transitive* requirements, so the mod manager may still install several framework packages. They do **not** need to be duplicated in PlayWithEliteItem's manifest.

To generate a **local release candidate** after compiling:

```bash
python scripts/validate_manifest.py
python scripts/package_thunderstore.py
```

The packager reuses the original icon from the publicly released 1.0.1 ZIP if the repository has no local `thunderstore/icon.png`. [GitHub Actions](https://github.com/HelioConde/PlayWithEliteItem/actions) also produces the ZIP as a **candidate artifact**, but it is **not automatically published**.

See [Thunderstore release preparation](docs/THUNDERSTORE_RELEASE.md). **Gameplay testing is still required** before uploading the reduced-dependency version under the existing CONDE team.

## ✨ Modernized source: feature overview (v1.1.1)

- **Server-authoritative distribution:** only the host/server modifies player inventories.
- **Automatic assignment:** scans for eligible players, including participants who arrive later.
- **Distinct aspects first:** selects unused elite aspects until the available pool is exhausted, then permits repeats.
- **Expansion awareness:** avoids aspects that require a DLC disabled for the run.
- **Conservative defaults:** chooses from seven classic aspects unless configuration is expanded.
- **Equipment safety:** by default, players who already have active equipment keep it.
- **Configurability:** toggle the feature, allowlist, replacement behavior and diagnostic logging.

The plugin grants **elite equipment**, not an independently permanent buff. If the equipment changes, its passive elite effect can disappear.

## 📥 Installation

### Option A — published mod (recommended for normal players)

1. Install a compatible [BepInExPack](https://thunderstore.io/c/riskofrain2/p/bbepis/BepInExPack/) using the Thunderstore Mod Manager or r2modman.
2. Install [PlayWithEliteItem on Thunderstore](https://thunderstore.io/c/riskofrain2/p/CONDE/PlayWithEliteItem/).
3. Launch Risk of Rain 2 with the mod profile enabled.

This installs **1.0.1**, which may not match the behavior of the latest source shown below. Compatibility with your current game/modpack still needs to be verified.

### Option B — test the current GitHub source (for developers)

1. Open [Actions](https://github.com/HelioConde/PlayWithEliteItem/actions) and choose a successful **Build and test mod** run.
2. Download the **PlayWithEliteItem** artifact and extract `GiveEliteItem.dll`.
3. Copy the file into your game's `BepInEx/plugins` folder.
4. Start the game, then configure `BepInEx/config/AlchemyFlame.PlayWithEliteItem.cfg`.

**This build is experimental.** Back up your mod profile and test solo before hosting multiplayer with it.

## ⚙️ Configuration (current source)

| Setting | Default | Purpose |
| --- | --- | --- |
| `General.Enabled` | `true` | Enables automatic grants |
| `Distribution.UniqueBeforeRepeats` | `true` | Prefer different aspects until the list runs out |
| `Distribution.IncludeAllRegisteredEliteAspects` | `false` | Opt in to wider catalog discovery |
| `Distribution.AllowedEliteEquipment` | Seven classic aspects | Explicit allowlist when discovery is disabled |
| `Equipment.ReplaceExistingEquipment` | `false` | Avoid replacing a player's active equipment |
| `Diagnostics.VerboseLogging` | `false` | Log grants and skipped assignments |

The default allowlist includes `EliteEarthEquipment`, `EliteFireEquipment`, `EliteHauntedEquipment`, `EliteIceEquipment`, `EliteLightningEquipment`, `EliteLunarEquipment` and `ElitePoisonEquipment`.

Automatically discovering additional aspects can include internal or nonstandard equipment. Use that option deliberately and test third-party compatibility.

## Building from source

Install the **.NET 8 SDK** and run from the repository root:

```bash
dotnet restore GiveEliteItem.sln
dotnet build GiveEliteItem.sln -c Release --no-restore
dotnet run --project tests/PlayWithEliteItem.LogicTests/PlayWithEliteItem.LogicTests.csproj -c Release
```

The build generates `GiveEliteItem.dll` under `GiveEliteItem/bin/Release/netstandard2.1/`.

### Project layout

```text
GiveEliteItem/
  GiveEliteItem.cs              Game events, inventory grants and config
  AspectSelection.cs           Game-independent selection logic
tests/
  PlayWithEliteItem.LogicTests/ Regression tests for selection behavior
.github/workflows/build.yml     Automated build, tests and DLL artifact
GiveEliteItem.sln               .NET solution
LICENSE                         MIT
```

## Compatibility & testing

The workflow compiles the plugin and exercises the selection algorithm (unique picks, exhausted pools, invalid inputs and repeated draws). **It does not run Risk of Rain 2 itself.**

In-game verification is still needed for:

- [ ] Solo: correct elite aspect and visible passive effect.
- [ ] Multiplayer: synchronized inventory and one grant per eligible player.
- [ ] Late joins: player receives an aspect once inventory is ready.
- [ ] More players than available aspects: repeats work without a freeze.
- [ ] New run: distribution state resets.
- [ ] Occupied equipment slot: existing item stays unless replacement is enabled.
- [ ] Disabled DLC and modded aspects: only valid aspects are assigned.
- [ ] Compatibility with current BepInEx, Risk of Rain 2 and other installed mods.

Issues and reproduction steps are welcome through [GitHub Issues](https://github.com/HelioConde/PlayWithEliteItem/issues).

## 📜 Credits and license

Developed by **Hélio Conde (AlchemyFlame)**. Source code is available under the [MIT License](LICENSE).

*Risk of Rain 2 and its assets belong to their respective rights holders. This is an independent community mod.*
