# Changelog

## 1.2.0 — Permanent elite in physical slot 1 (candidate, not published)

- Confirmed in player screenshots: the native secondary slot can display an elite and its visual effect while Q holds normal equipment.
- Keep elite equipment permanently in physical inventory slot 1; leave active Q equipment slot 0 untouched.
- Remove automatic 0/1 swaps, delayed swap coroutines, pickup hooks and manual buff injection.
- Remove custom virtual equipment fallback to avoid replacing or dropping equipment in unsupported inventories.
- No MMHOOK hooks remain; only BepInExPack is declared as a direct runtime requirement.
- Preserve run/player distribution, uniqueness before repeats and throttled server-side diagnostics.
- Live testing remains required for passive effects, pickup replacement, late joins and multiplayer.

## 1.1.9 — Original physical slot experiment

- Restored the 2024 slot-1 grant, with swapping and fallback.

## 1.1.8 — Virtual reserve experiments

- Experimented with server-injected passive buffs for the virtual reserve.

## 1.0.1 — Public Thunderstore release

- Original equipment slot switching implementation.
