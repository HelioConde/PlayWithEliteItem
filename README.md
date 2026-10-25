# PlayWithEliteItem

A server-authoritative **Risk of Rain 2** mod that grants each player a random **elite aspect** at the beginning of a run.

> **v1.1.0 — modernized source (testing required).** This release rewrites the original 2024 logic. Compilation and live-game compatibility must be verified before publishing a release.

## Features

- Automatically equips an elite aspect in the **active equipment slot** (slot index `0`) so its passive elite effect can apply.
- Detects aspects via the game's equipment catalog and `passiveBuffDef.isElite` flag; supports registered DLC/mod aspects when enabled.
- Assigns different aspects until the available pool runs out, **then allows repeats**. Works with any player count.
- Makes inventory changes on the **server only**; checks for players joining or spawning late.
- Does not intercept equipment usage, alter cooldowns, or swap equipment slots.
- Protects a player's existing active equipment by default.

## Installation

1. Install the PC game and the appropriate **BepInExPack** through r2modman/Thunderstore.
2. Build the mod or obtain its release DLL.
3. Place `GiveEliteItem.dll` in the game's `BepInEx/plugins` directory.
4. Run the game once to create `BepInEx/config/AlchemyFlame.PlayWithEliteItem.cfg`.

The host/server must have the mod to distribute the aspects. Test every intended multiplayer setup; vanilla synchronization does not guarantee compatibility with every other mod.

## Configuration

The configuration is generated on first launch.

| Section | Key | Default | Meaning |
| --- | --- | --- | --- |
| General | Enabled | true | Enable automatic distribution |
| Distribution | UniqueBeforeRepeats | true | Prefer distinct aspects, then allow repeats |
| Distribution | IncludeAllRegisteredEliteAspects | true | Include catalogued elite aspects from DLC and mods |
| Distribution | AllowedEliteEquipment | Seven original elite equipment names | Used when auto-discovery is disabled |
| Equipment | ReplaceExistingEquipment | false | Replace players' current active equipment, if any |
| Diagnostics | VerboseLogging | false | Log player assignments |

**Important:** This mod grants an elite **equipment**, not a permanent independent buff. Replacing the equipment later can remove its elite effect. Existing equipped items are skipped by default.

## Development

Requires the .NET SDK (8+ recommended).

```bash
dotnet restore GiveEliteItem.sln
dotnet build GiveEliteItem.sln -c Release --no-restore
```

The project uses .NET Standard 2.1, BepInEx and RiskOfRain2.GameLibs. Dependencies are restored from NuGet; no copied Unity DLLs, generated `bin/` or `obj/` files are committed.

The GitHub Actions workflow attempts a build and uploads the compiled plugin as an artifact. A successful compilation **does not** replace an in-game multiplayer test.

## Manual test checklist

- [ ] Solo: exactly one active elite aspect, with its passive effect.
- [ ] Multiplayer host: one aspect for each joined player, without duplicates until pool exhaustion.
- [ ] Client joins late: receives an aspect once inventory is ready.
- [ ] More players than available aspects: allows repeats without hanging.
- [ ] Starting with a non-empty active equipment slot: skipped unless replacement is enabled.
- [ ] New run: allocations reset and items are granted again.
- [ ] DLC/modded elite catalogues: only entries with elite passive buffs are included.

## Changes since 1.0.1

Removed the fragile delayed equipment swap and its global flag, the hard-coded numeric equipment index, obsolete unused bonus-item enums, optional lobby adapter and manually copied build libraries.

## License

MIT — see [LICENSE](LICENSE). Original concept and authorship: AlchemyFlame (Hélio Conde).
