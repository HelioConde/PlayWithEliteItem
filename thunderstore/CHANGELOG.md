# Changelog

## 1.1.4 — Diagnostic build (not published)

- Treat the configured classic elite equipment names as an explicit allowlist.
- Resolve classic aspects directly by their EquipmentCatalog internal name, without depending on the isElite flag being initialized first.
- Log why a player is skipped due to unsupported equipment slot/set counts.
- Log when there are no player controllers registered in an active run.
- No changes to the server-side/no-HUD requirement.
- Still requires testing the spawn grant and equipment exchange in-game.

# Changelog

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
