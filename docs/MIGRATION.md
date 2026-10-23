# Gameplay migration mapping

Current A258 follow-through: projectile damage now shares the source Moth's live child Harmful across spawn and later Lens/modifier writes. Source size-operation order is retained; existing summoned-weapon/lifecycle adapters remain authoritative. This is source-only, uncompiled/unverified; no child/projectile phase snapshots or independent balance correction are added.

Current A257 follow-through: Lilith's actual death-summon callback follows Android Health-death scope and pool/placement/parenting ownership. Full stated Hastur/Hina/Luna helper paths match or use existing source adapters; their distinct acquisition orders and unsaved phases remain. Source-only, uncompiled/unverified.

Current A255/A256 follow-through: Charged Up preserves Android's ordered remainder callback. Source counted replay includes all seven actual BuffPlayerStats roots, including NonStackingBuff/TemporaryRefreshingBuff subclasses; saved counts and fresh unsaved flags/phases preserve the A143 lifecycle mismatch. Missing old inherited counts rebuild without aggregate/timer inference. Source-only, uncompiled/unverified.

Current A253/A254 follow-through: Overload uses source positioned acquisition, its original missing audio and no camera-shake call. Matching Blazing Speed and Kunoichi helpers stay at their inspected source boundaries; Kunoichi's shared retained-clock limit branch is recorded and preserved. Source-only, uncompiled/unverified; no pre-validation gameplay correction is authorized by classification.

Current A252 status acquisition routing: seven Android helper roots replace native inline recipes, with complete global Dark Arts/Reaper helpers and bounded reuse of five matching native helpers. Source acquisition/replay is written; current/legacy/Custom and whole-port acceptance remain unverified. Details and original-input evidence are in docs/ANOMALIES.md A252.

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

Scope correction, 2026-10-22: Android gameplay changes, including Culling/Mini Clip/Tomes, shield cooldown arithmetic, temporary-buff stacking/refresh and map/Endless tuning, are required migration work. Their current Windows behavior remains only because integration is unfinished. The implementer incorrectly excluded these changes without user approval. [The anomaly ledger](ANOMALIES.md) records the evidence and limits.

Monetization, mobile stores/account rewards, ad/GamePass resurrection/starting boons, promotional rune/hero rentals, coupons and placeholder characters are excluded. Native auto-targeting remains; no new autofire implementation is included. Third-party reference mods were not copied into the implementation.

Continue is not a frozen simulation: ordinary live enemies, projectiles and RNG state are not serialized. Build/runtime state is reconstructed through native owners; defeated encounter and reward-drop records prevent duplication. Unknown or unsupported checkpoints remain preserved and are rejected rather than partially applied.

The audit records structural and bounded method/consumer evidence. It does not assert every unknown Android engine method is equivalent or that every platform difference improves balance. Full-length balance playtests are distinct from the seeded native integration checks used here.

## Android parity work after scope correction

Historical partial-port checks covered Android numeric recipes for Culling, Mini Clip, Giant and five Tomes, Stalwart cooldown/recovery and the three refreshing perks. Their recorded acquisition/expiry/continuation results apply to those earlier revisions. The current whole-port source replaces that partial continuation path with source acquisition/runtime replay; older claims about preserving native helper deadlines do not validate this implementation. The current README and anomaly ledger distinguish source integration from outstanding whole-port acceptance.

The user selected **Rebuild Android state** for legacy schemas3–6. Conversion happens in memory and leaves the checkpoint file unchanged; the next normal save uses source schema7. Known counters and source acquisition contributions are reconstructed, while source-unsaved helper phases restart. This is not a frozen simulation or a reconstruction of missing historical events.

A250 includes a concrete soul-capacity conversion: Windows supplied3 initial capacity and Soul Reap spawned pickups; Android starts at0 and grants3 capacity per Soul Reap acquisition. Legacy rebuilding therefore removes that known initial3 and adds3 per recorded Soul Reap pickup, retaining other recorded capacity contributions. Saved soul health is then requested through Android's setter, which rejects a request above rebuilt capacity. No extra clamp or native-capacity exception is added. Current schema7 saves retain their actual source capacity unchanged. Soul Reap's unlimited90-second helper, Soothing Warmth and Recharge restart their source-unsaved lifecycles after health/ammo restoration; native periodic grant limits and remaining deadlines are not carried over.

These continuation changes are source-integrated and uncompiled/unverified. Current/legacy resave/restart, original-file preservation, repeated grants, runes, Custom and the full release/install/rollback boundary remain required acceptance work after source completion and final anomaly review.

A251 similarly reconstructs Fire Starter, Fan Fire and Ice Shards through their actual source attachments. Their partial shot counters and pending burst coroutine are source-unsaved and restart on continuation. Ice Shards' missing native sound is prepared from the owned Android input by the normal asset builder; the resulting audio files participate in installer hashing/ownership. Previously prepared assets need the new builder preparation, rather than reusing a stale preparation marker. Actual fresh installation and playback remain unverified.

Current uncompiled source follow-through (A259): Gun start/update/release and actual ShootingState entry/exit redirect to a complete source firing/state implementation. Ordinary/charged recipe-event timing, source pending re-fire, zero-ammo waits and release-sound branch are retained, alongside the existing source reload/recipe/shooter/pool owners. Native desktop aim replaces joystick input. Auto Reload displays On/read-only under the source rule while retaining the stored Windows preference; source-unsaved timers/charge/pending input reconstruct on current/rebuilt legacy Continue. Original XP leaves/disabled GM/excluded monetized revival/configured easing families are bounded in the source record. Whole-port source/anomaly review and actual firing/input/options/Custom/current-legacy/package/delivery acceptance remain open; no build/game validation.
