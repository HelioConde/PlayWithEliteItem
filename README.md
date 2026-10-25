<div align="center">
  <h1>PlayWithEliteItem · Risk of Rain 2</h1>
  <p><strong>Random elite aspects at the start of a run — a mod shared with the Risk of Rain 2 community.</strong></p>
  <p>
    <a href="https://thunderstore.io/c/riskofrain2/p/CONDE/PlayWithEliteItem/"><img src="https://img.shields.io/badge/THUNDERSTORE-PUBLISHED-18b981?style=for-the-badge" alt="Published on Thunderstore"></a>
    <a href="https://github.com/HelioConde/PlayWithEliteItem"><img src="https://img.shields.io/badge/C%23-.NET-512BD4?style=for-the-badge&amp;logo=dotnet&amp;logoColor=white" alt="C# and .NET"></a>
    <a href="LICENSE"><img src="https://img.shields.io/badge/LICENSE-MIT-334155?style=for-the-badge" alt="MIT License"></a>
  </p>
  <p><a href="https://thunderstore.io/c/riskofrain2/p/CONDE/PlayWithEliteItem/">🎮 Install the published mod</a> · <a href="https://github.com/HelioConde/PlayWithEliteItem">💻 Explore the source</a> · <a href=".github/workflows/build.yml">⚙️ Build workflows</a></p>
</div>

## 📦 Published mod vs. current source

| | **Thunderstore package** | **GitHub source** |
| --- | --- | --- |
| **Version** | **1.0.1** | **1.1.1** |
| **Availability** | Released for community downloads | Modernized code, awaiting manual in-game validation |
| **Purpose** | Original automatic elite equipment distribution | Server-authoritative grants, configuration, tests and broader catalogue support |

The published package had **approximately 4,000 downloads** as shown on Thunderstore in October 2026. Downloads are a historical snapshot, not a live count.

> **Important:** the installation instructions and feature list below refer to the **modernized GitHub 1.1.1 source**, not necessarily to the **Thunderstore 1.0.1** build. The new implementation has automated checks, but in-game and multiplayer compatibility must still be tested before a release.

---

## Features

- Grants a random elite equipment in the **active equipment slot** (index `0`) so its passive buff can function.
- **Server-authoritative** inventory edits and support for players joining after a run begins.
- Picks unique aspects first; after the available list is exhausted, duplicates are allowed. No infinite loops.
- Excludes aspects belonging to DLCs **disabled for that run**.
- Defaults to the **seven classic aspects**. You can edit the allowlist to include more, or opt into automatic catalogue discovery.
- Leaves existing equipment untouched by default; does not intercept equipment use or swap slots.

## Installing

1. Install Risk of Rain 2 for PC and a matching BepInExPack (r2modman or Thunderstore Mod Manager).
2. To test the **1.1.1 development version**, open a successful run in [GitHub Actions](../../actions), download the **PlayWithEliteItem** artifact and extract `GiveEliteItem.dll`. Installing from Thunderstore currently provides **1.0.1**.
3. Copy the DLL into `BepInEx/plugins`, then start the game.
4. Configuration appears in `BepInEx/config/AlchemyFlame.PlayWithEliteItem.cfg` after first launch.

Only the host/server assigns equipment. Other mod combinations may require additional testing. This plugin is not yet a published, in-game-certified release.

## Configuration

| Section | Key | Default | Description |
| --- | --- | --- | --- |
| General | Enabled | true | Turn automatic aspect grants on/off |
| Distribution | UniqueBeforeRepeats | true | Avoid repeats until pool exhausted |
| Distribution | IncludeAllRegisteredEliteAspects | **false** | Advanced: include every registered elite aspect with an elite passive buff |
| Distribution | AllowedEliteEquipment | Seven classic equipment names | Comma-separated allowlist when discovery is disabled |
| Equipment | ReplaceExistingEquipment | false | Skip occupied active slots to avoid replacing another equipment |
| Diagnostics | VerboseLogging | false | Log each grant and skip |

The classic allowlist is: `EliteEarthEquipment`, `EliteFireEquipment`, `EliteHauntedEquipment`, `EliteIceEquipment`, `EliteLightningEquipment`, `EliteLunarEquipment`, `ElitePoisonEquipment`.

For newly added expansions, first confirm the equipment internal name in the game's catalogue. Add it to `AllowedEliteEquipment` or deliberately enable automatic discovery. **Automatic discovery may include hidden/nonstandard aspects**; it is not the default.

The game catalog may contain equipment for expansions that are disabled in a specific run. This plugin checks `requiredExpansion` before distributing.

**Important:** This mod grants an elite *equipment*, not an independently permanent elite buff. Replacing that equipment removes the passive elite effect. Existing equipment is preserved by default.

## Building and testing

Install the .NET 8 SDK and run:

```bash
dotnet restore GiveEliteItem.sln
dotnet build GiveEliteItem.sln -c Release --no-restore
dotnet run --project tests/PlayWithEliteItem.LogicTests/PlayWithEliteItem.LogicTests.csproj -c Release
```

The pure-C# regression suite tests uniqueness, exhausted pools, 100+ sequential draws, single-entry pools and invalid input. GitHub Actions runs these checks and uploads the compiled DLL as an artifact.

### Manual gameplay checklist (still pending)

- [ ] Solo: player receives an aspect, and the actual elite passive effect applies.
- [ ] Multiplayer: host and clients receive exactly one aspect, with synchronized inventories.
- [ ] Joining late: arriving players get an aspect once inventory is available.
- [ ] Full lobby: exhaustion does not freeze the game; repeats work.
- [ ] New run: the pool resets; no repeated grants on stage changes or respawns.
- [ ] Existing equipment: remains untouched unless explicitly configured otherwise.
- [ ] DLC toggle: aspect for a disabled expansion never appears.
- [ ] Optional modded aspects: all included effects and displays work correctly.
- [ ] Other popular mods and R2API/BepInEx versions: no startup exceptions.

## Repository layout

- `GiveEliteItem/GiveEliteItem.cs` — server-only grants, configuration, catalog filtering.
- `GiveEliteItem/AspectSelection.cs` — deterministic, game-independent random selection.
- `tests/PlayWithEliteItem.LogicTests/` — lightweight automated regression tests.
- `.github/workflows/build.yml` — CI build, tests, DLL artifact.

MIT License — AlchemyFlame (Hélio Conde).
