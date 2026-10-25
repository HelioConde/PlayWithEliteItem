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
