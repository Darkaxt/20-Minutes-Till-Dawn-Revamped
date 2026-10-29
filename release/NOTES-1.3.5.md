# 1.3.5 — Shared rune head starts

**Added:** mastering a rune on one character establishes rank1 as the minimum for that rune on the other characters. Further character masteries raise the minimum to2,3 and4. Existing higher progress is preserved: a rank4 recipient stays at4 rather than receiving an additive fifth rank. Every character earns its own rank5 through the existing boss lottery.

Existing mastered profiles count retroactively. Characters first played later inherit the head start. Each character retains its enabled selections, and shared ranks do not enable effects automatically. An ongoing battle keeps its established rune loadout; subsequent runs and Retry use current owned ranks. Original boss schedules, lottery pools, capped-rune XP and current-run reward activation choices remain unchanged.

The minimum derives from existing personal rank5 records. Grants, shared ranks and boss outcomes use the existing atomic journal write; reloads and pre-boss checkpoint replay cannot compound the minimum. No new save schema, updater requirement or reset is introduced: updater0.2.0/schema2/save-epoch1 remain compatible.

**Updated:** the README opens with an executive summary of the delivered Android ports, progression/runes, combat and Elites, build HUD, exploration, controller/save/Custom workflows, presentation fixes and Windows/DroidDeck delivery. Detailed screenshots, release history and audit ledgers remain available.

Native Windows verification covers real boss grants to minima1–4, preserved partial/future/legacy profiles, personal fifth ranks and actual battle/Retry effects, capped XP, controller navigation/all24 descriptions, both artwork styles/three viewport shapes, current/public1.3.4 Continue and pre-boss replay. Final committed-package, copied install/rollback, independent public download and reviewed cleanup are recorded in [PROGRESS.md](../docs/PROGRESS.md). Live Steam/profile/settings and phone installations are not modified by publication.
