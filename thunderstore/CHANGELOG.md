# Changelog

## 1.1.2 — Test candidate (not published)

- **Restored the original two-equipment mechanic**: elite aspects are placed in a server-side virtual second slot, not the active slot.
- Picking up a normal equipment no longer ejects the initial elite aspect.
- Successful equipment use rotates between active and reserved equipment after 0.3 seconds.
- Preserves charges and cooldown through full EquipmentState swaps.
- Protects the elite from being dropped if a pickup happens while the elite is active.
- All inventory mutations run on the server; no new HUD or custom networking.
- Added regression tests for equipment switching and pickup protection.
- Excludes special multi-slot/multi-set inventories (MUL-T, Functional Coupler) pending game testing.

## 1.1.1 — GitHub refactor (not published)

- Modernized initial random elite assignment and removed obsolete R2API usage.

## 1.0.1 — Published Thunderstore version

- Original second-slot equipment swap implementation.
