# PlayWithEliteItem — Risk of Rain 2 Mod

**Um dos meus projetos publicados para jogadores de Risk of Rain 2.** O mod adiciona variedade às partidas ao distribuir um equipamento de elite aleatório no começo de uma run.

[**🎮 Instalar pelo Thunderstore**](https://thunderstore.io/c/riskofrain2/p/CONDE/PlayWithEliteItem/) · [**💻 Código-fonte**](https://github.com/HelioConde/PlayWithEliteItem)

![Thunderstore](https://img.shields.io/badge/Thunderstore-publicado-1ab394) ![Versão pública](https://img.shields.io/badge/versão_publicada-1.0.1-blue) ![Linguagem](https://img.shields.io/badge/C%23-.NET-512BD4?logo=dotnet&logoColor=white)

## Publicação e desenvolvimento

| | Thunderstore | Código do GitHub |
| --- | --- | --- |
| **Versão** | **1.0.1**, publicada | **1.1.0**, refatoração em testes |
| **Comportamento** | Sorteio de equipamento de elite para o segundo slot, conforme descrição da publicação original | Distribuição server-authoritative, usando slot ativo e descoberta de aspectos via catálogo |
| **Estado** | Disponível para instalação; compatibilidade atual precisa de teste no jogo | Código em modernização, ainda não publicado no Thunderstore |

**Alcance:** aproximadamente **4 mil downloads** registrados no Thunderstore até outubro de 2026. Este número é uma referência histórica, não um contador em tempo real.

> **Importante:** as funcionalidades e instruções técnicas abaixo descrevem o **código 1.1.0 do GitHub**. O pacote 1.0.1 do Thunderstore ainda tem o comportamento da implementação original. A compilação e os testes em jogo da refatoração precisam ser concluídos antes de um novo lançamento.

## About the current source (v1.1.0 — work in progress)

A server-authoritative **Risk of Rain 2** mod that grants each player a random **elite aspect** at the beginning of a run. The modernized code targets safer multiplayer behavior and configurable assignment, but **has not yet been validated or released**.

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
