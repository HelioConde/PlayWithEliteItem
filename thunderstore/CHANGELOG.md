# Changelog

## 1.1.9 — Original physical slot restoration experiment (not published)

- Revisit the original 2024 implementation and its `SetEquipmentIndexForSlot(elite, 1)` call.
- Attempt to place the elite in real equipment slot 1 on spawn; read it back before considering the grant successful.
- Swap equipment states between physical slots 0 and 1 after a use, preserving cooldowns/charges.
- Allow a Q press on the passive elite equipment to swap back, even when PerformEquipmentAction returns false.
- Protect physical slot 1 during an ordinary equipment pickup.
- If the current RoR2 inventory rejects slot 1, log the reason and fall back to the previous server-side virtual reserve.
- Keep all logic server-side without HUD or custom RPCs. Gameplay acceptance test is still required.

## 1.1.8 — Experimental elite effect while reserved (not published)

- The live log confirmed the elite was stored in the virtual reserve, not dropped.
- Maintain the original elite BuffDef on the server while the aspect is in reserve.
- Relinquish the injected buff when the elite is equipped again or a run ends.
- Track owner body to handle respawns; continue using vanilla buff replication.
- Add Elite.KeepEliteBuffWhileReserved=true and tests for buff-ownership conditions.
- No custom UI, client RPCs or new content; gameplay verification required.

## 1.1.7 — Corrige dependência em tempo de execução (candidato, não publicado)

- Declara HookGenPatcher 1.2.9 como dependência junto ao BepInExPack.
- O log real da 1.1.6 demonstrou a ausência de `MMHOOK_RoR2.dll`; o código de gameplay nem iniciava.
- Atualiza o validador e a documentação sem reintroduzir a suíte R2API.
- Mantém a lógica de gameplay da 1.1.6; ainda exige teste no jogo.

## 1.1.6 — Diagnostic instrumentation (unpublished candidate)

- Add `Diagnostics.TraceGameplay=true`, enabled by default for current troubleshooting.
- Trace startup, run state, equipment catalog, player discovery and grant attempts/results.
- Audit the active equipment 1.5 seconds after successful granting to detect overrides.
- Trace world equipment pickups and automatic swaps.
- Throttle routine player discovery logs to once every 10 seconds; avoid duplicate grant failure warnings.
- No intentional changes to equipment distribution or second-slot behavior.
- Requires a real in-game test and a fresh LogOutput.log to identify the remaining defect.

## 1.1.5 — Restore original grant behavior (testing candidate)

- Remove the exact equipmentSlotCount/equipmentSetCount gate that could silently skip players.
- Use the original SetEquipmentIndexForSlot API to grant elite equipment at spawn (with an active-set-aware fallback).
- Continue verifying the actual inventory after assignment instead of claiming success without a grant.
- Log when the player's character body is not ready and when the current process is not the server.
- Preserve the second logical equipment reserve and automatic post-use switching.
- No additional client HUD or custom RPCs. In-game verification still required.


## 1.1.4 — Diagnostic build (not published)

- Treat the configured classic elite equipment names as an explicit allowlist.
- Resolve classic aspects directly by their EquipmentCatalog internal name, without depending on the isElite flag being initialized first.
- Log why a player is skipped due to unsupported equipment slot/set counts.
- Log when there are no player controllers registered in an active run.
- No changes to the server-side/no-HUD requirement.
- Still requires testing the spawn grant and equipment exchange in-game.


## 1.1.3 — Test candidate (not published)

- Fix: equip the elite aspect in the normal active equipment slot when the player spawns.
- Wait for the character body to exist before granting, and verify that the inventory changed.
- Fix: protect the **first** ordinary equipment pickup when the virtual reserve is empty.
- Preserve the elite aspect in the server-side virtual second slot when another equipment is collected.
- Preserve any initial equipment from other mods in reserve.
- Add test cases covering spawn grants and the initially empty reserve.
- No custom HUD, inputs or client RPCs; still requires live in-game testing.

## 1.1.2 — GitHub test candidate

- Introduced virtual second equipment reserve and post-use rotation.
- Server-authoritative inventory mutation, no R2API dependency.

## 1.0.1 — Published Thunderstore version

- Original dual-equipment swap.
