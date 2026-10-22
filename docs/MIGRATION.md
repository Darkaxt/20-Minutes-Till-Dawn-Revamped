# Gameplay migration decisions

The Windows game keeps its native combat, input, UI, stats, progression records and content. Android numeric enum IDs are mapped semantically. Unity 2022/IL2CPP libraries are not transplanted into the Unity 2019/Mono player.

| Area | Integrated behavior |
|---|---|
| Charge Cannon | Separate native weapon selection, charge thresholds/multipliers, reload/ammunition integration and three evolutions. Original guns remain. |
| Gale/Bulwark | Separate playable forms, source animation/skill geometry and timing, native Skill input, exclusive upgrades and per-run cleanup. |
| Repeated upgrades | Semantic identity resolves deep-copied perks; repeat choices use native ApplyStack and finite pool limits. |
| Existing defects | Burn-area coverage/resize, unused pooled-summon teardown, Holy Spear/Tome of Might maximum-HP contribution, Generator attachment/reactivation, Growth amount, Seismic cooldown direction and boss-arena ownership. See stable IDs in the anomaly ledger. |
| Added fallback choices | Six source stat-only powers enter only the appropriate fallback pools with finite native repeats; ordinary choices remain intact. |
| Unlock progression | Actual miniboss/boss/build/attribute/scenario/Darkness conditions replace content currency purchases. Existing ownership survives; old clears do not invent unsupported new encounter facts. Transparent native-icon notices persist without retrospective floods. |
| Runes | Native five-rank cap; first price unchanged, subsequent prices rounded up from 120% of the previous rank. Fully upgrade ancestors in the same column to buy lower runes. Multiple owned row runes can be enabled, with stock-compatible representative selections retained. |
| Challenge | Recovered 36-session/10-boss schedule, five quota tiers, native encounter/results integration, earned Forest-clear gate and separate persisted tier. |
| Continue | Atomic semantic checkpoint with exact build, finite pools, known runtime providers, native clocks/cycle, defeated encounters and uncollected reward drops. Pending starting Blessing resumes once at its remaining native delay. |
| Custom | Native controls over owned loadouts/maps, weapon/global/XP/enemy/summon settings, eligible finite upgrades, acquired stat contributions and scenario objects. Native result credits remain; unlocks/achievements/leaderboards are disabled. |
| Mode defaults | Actual owned MapData supplies schedules/timers/rune flags/repeat limits. Live same-mode reset clears overrides; another mode requires an explicit new Custom run. |
| HUD/title | Transparent native icon/value attributes left, collected skill icons/counts/tiers right, no background/border. F2/Options and keyboard paging remain. The actual owned logo artwork preserves Flanne's credit and adds MAINTAINED BY DARKAXT as pixel lettering; the former footer is removed. The native Emberpath promotional panel, wishlist and click target are disabled. |

## Deliberately retained or excluded

Existing Windows numeric map/Endless tuning and unapproved combat balance proposals remain unchanged. Differences such as Culling/Mini Clip/Tomes, shield cooldown arithmetic and overlapping temporary buffs are decisions to consider, not automatically imported fixes. [The anomaly ledger](ANOMALIES.md) records the evidence and limits.

Monetization, mobile stores/account rewards, ad/GamePass resurrection/starting boons, promotional rune/hero rentals, coupons and placeholder characters are excluded. Native auto-targeting remains; no new autofire implementation is included. Third-party reference mods were not copied into the implementation.

Continue is not a frozen simulation: ordinary live enemies, projectiles and RNG state are not serialized. Build/runtime state is reconstructed through native owners; defeated encounter and reward-drop records prevent duplication. Unknown or unsupported checkpoints remain preserved and are rejected rather than partially applied.

The audit records structural and bounded method/consumer evidence. It does not assert every unknown Android engine method is equivalent or that every platform difference improves balance. Full-length balance playtests are distinct from the seeded native integration checks used here.
