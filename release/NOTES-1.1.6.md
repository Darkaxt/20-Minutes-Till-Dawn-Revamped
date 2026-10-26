# Cleaner HUD and playtest fixes

- Collected skills use transparent, vertically centered rows: one per acquired upgrade path, plus character, Tome and Synergy rows. Icons have no count/tier labels; a tripled skill gets a subtle glow.
- Left metrics are vertically centered. Shorter reload times are green; longer ones are red. Off/Little/Extended remain native Options settings.
- Enemy damage numbers keep their readable size when zooming out.
- Native level-up panels show their four-node upgrade paths again. Build help identifies summon interactions, including Ghost Friend and Lilith’s spirits, and named Synergy requirements completed by a pick.
- Glass and Stillness descriptions show their benefits as well as penalties. Their damage/movement trade-offs are preserved.
- Off-screen boss reward chests have a small directional chest icon, cleared on collection and restored through Continue.
- Sprites Reborn now colors neutral projectile trails, particle tints and uncovered neutral sprite bodies, including Shotgun pellets and Fire Starter. Original mode restores original rendering; pixel shapes, alpha and shared materials are preserved.

Verified in the native Windows player at square,4:3 and16:9 sizes, with controller/pointer/F2, actual damage, real four-shot Fire Starter production, chest pickup/teardown and fresh-process Continue. Controlled acquisitions and synthetic input are explicitly test fixtures; no natural full-run balance or phone-runtime claim. [Contracts and color-audit coverage](../docs/PLAYTEST-FIXES.md).

The existing Android updater APK remains compatible. Only the mod DLL changes in the614-target installed payload;613 other hashes and prepared assets are unchanged. Deployment remains user-controlled.
