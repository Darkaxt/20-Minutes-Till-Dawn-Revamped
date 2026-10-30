# 1.3.12 — Unified gameplay HUD opacity

- The gameplay HUD defaults to **85% opacity**, so mobs and attacks remain visible beneath it. Native Options offers **80%, 85% and 90%**, supporting controller, pointer and saved preferences.
- Health, ammo (including cursor ammo), XP, timer, Score, objective, control hints, left stats, right skill icons, POI icons/arrows/distances, unlock notifications and the Custom badge share the setting. New HUD elements inherit it automatically.
- Separate wrappers preserve the native HUD visibility/fade owners; opacity does not compound on timer/Score. Menus and reward windows retain opaque backgrounds. Mobs, attacks, combat effects and vision retain their current appearance.
- This replaces the timer/Score-only control. Old timer-only values do not make the whole HUD unexpectedly faint. Existing projectile/effects settings remain independent.

No save reset. Compatible with the existing updater 0.2.0, schema 2 and save epoch 1.
