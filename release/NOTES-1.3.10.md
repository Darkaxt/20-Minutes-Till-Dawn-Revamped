# 1.3.10 — Honeycomb exploration and clear menu focus

- Replaced timed exploration waves with a continuous honeycomb layout. Walk between shared vertices; the radar shows the three nearest uncollected exploration targets. Only the current cell's chests need physical objects.
- Each unobstructed cell contains XP, healing, magnet, freeze, frenzy and one upgrade opportunity. A character upgrade replaces the normal upgrade in 10% of upgrade vertices. A fixed run seed varies their arrangement; shared vertices keep the same contents in neighboring cells.
- Collected vertices stay collected through revisits and Continue. Discovered rewards do not reroll. Existing checkpoints transition without a reset and retain their old unopened chests, Score and frenzy.
- Northern markers stay on the northern screen edge instead of jumping between left and right to avoid the HUD.
- Added a small native-style chevron to the focused menu button. Text buttons use a left margin; icon buttons use an inside-left gutter, including the first roster column. The arrow follows existing controller/keyboard focus without intercepting input.

- Added two independent native Options entries: **Player projectiles opacity** and **Player effects opacity**, each25/50/75/100%, default100. Projectiles, summons, abilities, explosions, trails and particles use the appropriate separate channel. Character, HUD, vision, enemies and hostile attacks stay unchanged. Damage, dimensions and hitboxes are unaffected.

The reported basic Triple Upgrade + Mirror + Fireball combination was audited: all three mirrors retain30% damage/size bonuses and overall multiplier1; a40-damage size1 Fireball becomes52 damage / size1.3 once across the three colliders. This does not identify the full oversized build, and no unrelated scaling change was made.

Existing reward amounts and eligibility conversions remain intact. Boss treasure keeps its separate icon. Blocked/out-of-bounds vertices are omitted without adding extra high-value rewards.

Use the existing Android updater 0.2.0 with DroidDeck (schema 2 / save epoch 1). No new game APK or save reset is required. Focused native behavior, current/old save handling and release verification are documented in [POI-HONEYCOMB.md](../docs/POI-HONEYCOMB.md).
