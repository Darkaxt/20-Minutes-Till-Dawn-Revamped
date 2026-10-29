# 1.3.2 — Clean selection header

Removed the obsolete currency badge, including its icon and background, from the shared character/weapon selection header. Rune ranks are earned from bosses, so this purchase-era display no longer belongs there.

The complete Runes tab now sits at the left-hand header inset. Its background, prompt arrow, hit area and native show/hide animation move together; reopening the selectors retains the placement. Native controller and mouse navigation remain available.

Stored credits, game/results scoring, unlocks and character rune ranks/toggles are preserved. Updater0.2.0 remains compatible with schema2/save-epoch1; updating an already transitioned installation does not reset saves. No additional Android gameplay behavior is ported in this presentation fix.

Native UI verification covers both selectors and rune entry points, controller/pointer navigation, Back/title re-entry and square/4:3/16:9 views. Exact-package, copied installation/rollback and independent public-download evidence is recorded in [PROGRESS.md](../docs/PROGRESS.md). Publication does not deploy to the live Steam or phone installation.
