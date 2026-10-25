# PlayWithEliteItem

**Risk of Rain 2 mod (BepInEx, C#)** — each player starts a run with one randomly chosen *elite aspect* equipped.

> Version **1.1.1**. GitHub Actions builds the plugin and runs regression tests, but a live Risk of Rain 2 test (especially multiplayer) is still necessary.

## Features

- Grants a random elite equipment in the **active equipment slot** (index `0`) so its passive buff can function.
- **Server-authoritative** inventory edits and support for players joining after a run begins.
- Picks unique aspects first; after the available list is exhausted, duplicates are allowed. No infinite loops.
- Excludes aspects belonging to DLCs **disabled for that run**.
- Defaults to the **seven classic aspects**. You can edit the allowlist to include more, or opt into automatic catalogue discovery.
- Leaves existing equipment untouched by default; does not intercept equipment use or swap slots.

## Installing

1. Install Risk of Rain 2 for PC and a matching BepInExPack (r2modman or Thunderstore Mod Manager).
2. From [GitHub Actions](../../actions), open a successful run and download the **PlayWithEliteItem** artifact; extract `GiveEliteItem.dll`.
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
