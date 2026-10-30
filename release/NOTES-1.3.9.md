# 1.3.9 — Specialize triples actual summons

Fixed Shana's Specialize chest reward losing additional summon actors when applying the next upgrade. It now uses the pristine upgrade definition for its two extra applications.

- Dragon Egg: 3 eggs.
- Magic Lens: 3 mirrors.
- Ghost Friend: 3 ghosts.
- Magic Scythe: 3 scythes.
- Magic Spear: 6 lances.
- Electro Bug: 3 bugs.

Stat upgrades retain their three applications. No extra damage multiplier is added on top of the additional summons. Ordinary upgrades, Split Fire and save serialization are unchanged.

The existing Android updater 0.2.0 remains compatible (schema 2 / save epoch 1). No save reset or new updater APK is required. This is the Windows mod, also playable through DroidDeck.

Gameplay evidence reuses the existing native screenshot and passed summon-count/application/stat checks at the user's instruction. Package installation/rollback and public-download verification are recorded in [PROGRESS.md](../docs/PROGRESS.md).
