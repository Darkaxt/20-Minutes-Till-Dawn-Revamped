# v1.3.8 — Weapon familiarity

Weapons now gain **+2% reload speed, fire rate and projectile speed per highest recorded crown level**: +10% at5, up to +30% at15. The existing weapon crown is shared across characters and includes Challenge records; repeating a victory does not stack bonuses.

Spread stays unchanged, preserving weapons such as the shotgun. Damage, magazine size, projectile count, Pierce, Bounce, bullet size and knockback stay unchanged too. Familiarity composes with native rune/skill modifiers and weapon evolutions. The weapon description shows its bonus; the existing metrics HUD shows the effective values and beneficial reload coloring.

Continue retains the familiarity rank with which the run started. Older checkpoints adopt the current crown once. Retry and new runs use the current crown. Custom inherits obtained weapon familiarity without adding progression rewards.

No new save reset. The existing Android updater0.2.0 remains compatible (update schema2 / save epoch1). This is the Windows mod, including play through DroidDeck; no new game APK is required.

Validation uses the real Windows player on an isolated desktop: every weapon, native victory/Retry/evolution and Custom startup, controller/pointer preview, current and public1.3.7 Continue. Exact-package install/rollback and public-download verification are recorded in [PROGRESS.md](../docs/PROGRESS.md).
