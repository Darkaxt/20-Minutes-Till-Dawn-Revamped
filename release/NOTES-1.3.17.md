# 1.3.17 — Character-specific weapon familiarity

- Track familiarity separately for every character–weapon pair. A victory with one character or weapon no longer boosts another.
- Recover all existing shared weapon-familiarity ranks onto **Shana**, the default character, once. Keep original crown history, existing pair records and other progress. Later global crowns cannot repeat recovery.
- Immediately correct old Continue saves to the selected character/weapon rank on resume. New scoped checkpoints keep their own starting rank and never duplicate the bonus.
- Preserve 2% per qualifying tier for reload speed, fire rate and projectile speed, capped at 30%. Challenge still counts; repeated/lower clears do not stack or reduce ranks. Custom runs and losses grant none.
- Native descriptions and the stats HUD use the selected pair. Damage, spread and weapon counts stay unchanged.

No save reset. Compatible with updater 0.2.0, schema 2 and save epoch 1. Recovery occurs when the updated game loads its profile.
