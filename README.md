# 20 Minutes Till Dawn - Revamped

A community Windows mod for 20 Minutes Till Dawn by Flanne, maintained by Darkaxt. Selected, source-verified Android gameplay additions and fixes are integrated with native Windows gameplay, achievement-based unlocks, revised runes, run continuation and Custom game menus.

Source and packaging for **Revamped 1.0.1**. Use the versioned [release package](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases) and [installation instructions](INSTALL.md); see [delivery status and verification limits](docs/PROGRESS.md). This repository contains original mod and validation code, excluding the game, extracted artwork, saves, private research and third-party mod binaries. It is an independent mod repository; the proprietary game has no public upstream source to fork.

Implemented and verified in an isolated copied Windows player:

- Charge Cannon and three evolutions; Gale and Bulwark forms and exclusive upgrades.
- Bounded existing-feature fixes, finite repeat choices and thematic miniboss/boss unlocks.
- Rune prices increasing 20% per rank, column prerequisites and simultaneous row selections.
- Challenge mode, semantic run continuation and native Custom controls with owned-mode defaults.
- Transparent native icon stats/skills HUD, optional compatible recolours, and pixel-art “MAINTAINED BY DARKAXT” logo credit. The Emberpath promotion is removed in 1.0.1.

**Delivery:** 1.0.0 is the latest published release and its payload passed a DroidDeck 0.5.0 menu/bootstrap smoke test. The unpublished 1.0.1 update is installed locally on Windows and DroidDeck; its pixel-art credit/ad removal passed windowless Windows rendering at 720p and 1080p, including the final package. The updated phone installation passes file/preservation checks, but its new artwork has not yet been visually verified on the phone. Publication and further deployment are on hold until the remaining Android gameplay integration and its verification are complete. [Delivery history and limits](docs/DROIDDECK.md).

Custom runs use obtained content and award credits while disabling unlocks, achievements and leaderboard submission. Switching mode defaults during play explicitly starts a new Custom run; restoring the current mode defaults removes its overrides.

Revamped is a selective migration, not complete Android parity. The versioned installer prepares owned-input assets locally and verifies the supported game and owned installation files. The delivery ledger distinguishes verified releases from an update still in progress.

## Controls and behavior

F2 or Options toggles the transparent stats HUD. Numbers show effective values followed by base and net bonus, before charge/conditional effects. Page Up/Page Down scroll the right-hand collected skill icons; T1/T2/T3 indicate tree tiers, S indicates a special upgrade, and ×N is its acquired count. Native movement, shooting, auto-targeting and controller bindings remain.

Pause → Save & return suspends a supported run without cashing out its score. Continue reconstructs that build on the next launch. New run explicitly discards the checkpoint. Custom mode and its pause settings use native menus; Load mode defaults can restore the current mode or explicitly start another owned mode as a new Custom run.

See [gameplay migration decisions](docs/MIGRATION.md) and the [anomaly/balance ledger](docs/ANOMALIES.md) for scope and retained differences.

## Android integration and disparity ledger

**Not all Android updates have been ported.** This comparison uses the supplied Android **8.6 (1093)** package and Windows Steam **12679152**; it does not establish parity with later Android releases. “Reviewed”, “retained” and “implemented” have different meanings. The earlier completed stages describe a partial implementation, not completion of the requested Android gameplay migration.

Status meanings: **Integrated** = implemented and verified at the stated boundary; **Required — not yet ported** = a known Android gameplay difference that must be integrated; **Open** = investigation or verification remains; **Excluded** = monetization, unreachable placeholders or a specific user override. Existing Windows behavior is recorded as current state, not an approved target. Retaining Windows behavior is not a claim that the two versions are equivalent. Rows group related changes and must not be counted as individual patches.

### Integrated gameplay additions and fixes

| Feature | What has been done | Remaining limit / evidence |
|---|---|---|
| Charge Cannon and three evolutions | Integrated native weapon selection, charge behavior, ammunition/reload and evolution choices. | Recovered source contracts and Windows runtime checks; not a transplant of Android's engine. [Migration](docs/MIGRATION.md). |
| Hina-Gale and Diamond-Bulwark | Integrated both playable forms, animation, skills/passives and all six exclusive upgrades. | Native selection, skill behavior, effects and cleanup verified. A10/A11 document source interpretation. |
| Six fallback upgrades | Integrated the recovered fallback effects, finite allowances and Adrenaline expiry; late/depleted choice pools remain usable. | Pool/time preconditions are seeded tests, not a naturally played 100-level run. A15. |
| Repeated upgrades | Corrected deep-copy identity so repeated picks invoke the intended stack path instead of duplicating first-pick owners. | Keeps Windows per-perk stack recipes; Android repeat strengths are not universally imported. A01/A39. |
| Combustion fire puddles | Uses Android pool/center-distance targeting without the two-collider truncation or duplicate collider burns. | Native evolution, kill-spawned field, five actors, strict-radius exclusion and unchanged 3-damage/0.5-second delivery verified. A13. |
| Pooled sword/summon teardown | Corrected teardown of never-initialized pooled instances. | Does not replace the summon/pool architecture or change DPS. A14. |
| Holy Spear / Tome of Might | Holy Spear uses maximum HP. Tome of Might now follows Android's fixed signed modifier rather than the earlier Windows HP-scaling correction. | Android Tome of Might reduces damage by two; this source anomaly is explicitly recorded and reproduced in the Windows damage pipeline. A21/A49/A73. |
| Generator | Restored the missing effect attachment and corrected reactivation after expiry. | Native ammo-gain/refresh/repeat/lifecycle checks pass; other temporary buffs retain their own rules. A36. |
| Growth and Seismic Ward | Corrected Growth to the documented 4% XP per rank and Seismic cooldown reduction direction. | Native XP and real pulse intervals verified; not an import of the entire Android rune backend. A56/A57. |
| Boss arenas | Added boss-death ownership so an early-defeated boss's arena is released. | Native lifetime fallback and independent overlapping arenas retained. A65. |
| Challenge mode | Integrated the recovered 36-session/10-boss schedule, five quota tiers, progression gate and native results. | Seeded encounter/quota verification; full-length balance benchmarking remains open. |
| Save and Continue | Integrated semantic build/runtime checkpoints, finite pools, clocks/cycles, defeated encounters/reward drops and supported native state. | Ordinary live enemies, projectiles and RNG are not frozen snapshots. Pending Blessing resumes once at its saved delay. A71. |

### Android gameplay disparity ledger

The table tracks **43 grouped gameplay disparities**, not 43 individual patches. Gameplay differences are required migration work, including balance, cooldown, stacking and mode rules. They were incorrectly treated as optional by the implementer; no further balance approval is required. Status applies only to the stated boundary. Source anomalies still need actual reachability/behavior evidence. Full detail and source boundaries are in the [anomaly ledger](docs/ANOMALIES.md).

| Area / feature | Status | Current behavior / target | Ledger |
|---|---|---|---|
| Culling | Integrated numeric recipe | Android +30% summon attack speed now applies through native acquisition; native scene restart and original gun behavior verified. | A02 |
| Mini Clip | Integrated numeric recipe | Android -4 max ammo, +50% damage and +75% reload bonus now apply through native acquisition; native restart verified. | A03 |
| Tomes / Giant | Integrated numeric recipes | Android Giant +50% size/+2 HP; Elasticity -5% damage/-1 HP; Power -5% fire rate; Summoning -10% reload; Rage -10% damage/-20% knockback; Stillness MoveSpeed penalty now apply through native menus. Might uses Android's signed -2 damage behavior; Elements applies -50% Bullet Damage per tome and adds +35% burn/lightning damage/freeze duration. Native acquisition and fresh-process Continue verified for these paths. | A04/A05/A49/A73 |
| Existing gun/evolution/character base tuning | Integrated numeric and native-input boundary | Grenade Launcher 45 damage, Flash Mag 20-shot trigger, Abby 3 HP, Shana movement 3.2/shooting factor 0.45, Hina one-second cooldown/six-times dash. Native selection, actual firing/evolution activation/dash and scene transitions pass. Other exclusive/targeting mechanics remain separate rows. | A48 |
| Burn backend | Integrated core; Endless affix immunity remains open | Android shared accumulated damage, refreshed five-tick lifetime, preserved tick phase and shipped 1000-damage cap pass native damage/cleanup/restart checks. Affix immunities belong to the remaining Endless integration. | A12/A67 |
| Health limits | Integrated assignment/stat boundary | Android floored health stat with minimum one, uncapped red maximum, independent soul capacity and rejection of over-cap soul assignments. Native damage, vision scaling and scene restart pass. Specialized health-grant consumers remain listed separately. | A16 |
| Shield recovery | Integrated arithmetic; broader shield parity still open | Stalwart now subtracts 60 seconds per pick with a 10-second minimum; recovery advancement uses Android's one-second overshoot boundary. Native protection/recovery, restart and fresh-process Continue pass. Other shield/repeat/owner differences still require reconciliation. | A17/A42 |
| Aero/Electro Mastery and Glare | Electro integrated; Aero/Glare required | Electro Mastery adds 12 before elemental scaling and multiplies lightning size by 1.75. Native acquisition, Elements/Might composition, fresh-process Continue and clean restart pass. Aero Mastery's placement and Glare consumers remain required work. | A18/A26/A76 |
| Thunderspawns / Pulsing Summons / Soul Link | Thunderspawns callback integrated; other consumers required | Thunderspawns uses Android's 15% surviving Summon-damage callback, including bosses. Native damage, probability boundary, Continue and restart pass. Pulsing Summons/Soul Link geometry and Endless affix consumers remain required. The source English tooltip still says 30%; that shared mismatch is recorded in A19. | A19/A75/A76 |
| Shared lightning backend | Core integrated; affix consumers required | Android delayed Bullet-classified collisions, positive signed positional HP changes, strict pool-centre queries, per-target notifications and last-released-first reuse are reproduced. Real physics, Elements/Might composition, Energized, native save/Continue and restart pass. A67 immunity/armor consumers remain required with Endless. | A76/A67 |
| Bloodsuckers / Soul Drain / Soul Conversion | Integrated direct grants | Five-hundredth native summon kill grants health/soul health immediately; other damage categories do not count. Conversion adds two capacity then grants one soul heart. Counter persistence/Continue and restart pass. | A20 |
| Soul Knight / Justice | Required — not yet ported | Damage-growth interpretation and per-perk HP-growth caps differ. Windows behavior retained. | A22/A24 |
| Energized | Integrated callback and core lightning consumers | Android's independent 20% roll replaces every-five-hit cadence; one success grants three ammo plus native particle/sound feedback. Targeted and positional lightning notifications, failed rolls, ammo caps, fresh-process Continue and observer cleanup pass. Endless affix interaction remains part of A67. | A23/A76 |
| Fresh Clip / Anger Point | Integrated refresh behavior | Fresh Clip now grants +30% damage for one second; Anger Point grants +70% damage/fire rate for 30 seconds. Each acquired helper refreshes its active duration instead of accumulating independent overlapping trigger buffs. Native acquisition, expiry, reactivation and fresh-process Continue pass. | A25 |
| Reflex / Vision | Integrated stated consumers | Reflex follows subsequent movement growth/loss up to the Android ten-percent growth cap, with one provider across repeated picks. Vision follows its stat beyond 2.5. Native acquisition, capped/uncapped Continue and restart pass; Glare remains separate. | A27/A31 |
| Best Friends / Magic Lens | Required — not yet ported | Attack-speed and damage composition, plus Lens size clamping, differ. Windows behavior retained. | A28/A29 |
| Death Rounds / Hellspawns | Integrated stated consumers | Death Rounds supplies +10 flat Bullet/Summon Damage before their stat multipliers. Hellspawns observes surviving Summon-classified damage, excludes actual bosses and permits ordinary Champion-tagged actors. Native acquisition/damage, boss spawning, Continue and clean restart pass. | A30/A32/A75 |
| Kill Clip / Siege / Regeneration | Regeneration integrated; Kill Clip/Siege required | Regeneration now waits ninety seconds before its first native heart pickup, including repeated acquisitions. Native acquisition and clock verified. Kill qualification and ammo conservation versus gain remain required work. | A33/A34/A35 |
| Dragon / Scythe / Trained Dragon / Soul Powered | Required — not yet ported | Base recipes, rotation, additive versus multiplicative growth and damage-modifier ordering differ. Windows behavior retained. | A37/A38 |
| Frostbite / Shatter / Assassin / Ghost Wizard repeats | Required — not yet ported | Windows first-pick and repeat recipes retained. Android helper duplication changes repeat strength and/or event scope. | A39 |
| Aero Magic / Eye of the Storm | Required — not yet ported | Windows spread and player-centered area retained; Android changes spread and moves the area to the Gale object. | A40 |
| Electro Mage / Magic Spear | Required — not yet ported | Native cursor targeting and spear radial eligibility retained instead of the inspected Android target queries. | A41/A47 |
| Shatter frozen-target guard | Required — not yet ported | Windows requires a frozen target. The inspected Android death handler lacks this guard; that apparent regression is not imported. | A43 |
| Splinter | Required — not yet ported | Windows inherited projectile size and per-action activation limit retained; Android uses different size/counter ownership. | A44 |
| Holy Arts | Required — not yet ported | Windows repeated-pick strength and delayed damage cadence retained; Android attaches full-strength producers differently. | A45 |
| Dragon Bond | Required — not yet ported | Windows bullet-damage addition retained; Android issues separate summon-classified damage with different rounding/composition. | A46 |
| Enhanced Shadow / Necromancer | Required — not yet ported | Android's additional global summon bonuses and changed clone cooldown recipe are not imported. | A50 |
| Abby Whirlwind | Integrated refresh behavior | Native exclusive acquisition now creates the refreshing two-second +35% movement/fire-rate helper. Repeated reloads refresh its duration; expiry, scene cleanup and fresh-process Continue pass. | A51 |
| Hastur / Katana and existing summon exclusives | Required — not yet ported | Windows pool/modifier ownership and existing summon recipes retained rather than Android's keyed stat providers/helper architecture. | A52; [migration](docs/MIGRATION.md) |
| Tome of Electro | Required — not yet ported | Windows area lightning retained; Android selects one random target. | A53 |
| Hina Hidden Blade | Required — not yet ported | Windows triple-damage recipe and protection ownership retained; Android changes both damage and invulnerability. | A54 |
| Body and Soul | Integrated direct grants/capacity/counter | Android adds three plus rank-scaled soul capacity and grants souls directly every five minutes, with a finite grant count and no immediate pickup. Native timer, exhausted counter, fresh-process Continue and original restart pass. | A55 |
| Elemental Barrage | Equivalent at reviewed activation/removal boundary | Both versions add Fire Rate twice despite the Fire Rate/Reload Rate tooltip. This shared anomaly is not a missing Android update. | A58 |
| Dedication | Equivalent at reviewed rune boundary | Corrected source reading: Android OnSummonChanged tail-branches to CheckSummons, using the same active-summon count and rank-scaled activation/removal as Windows. The earlier empty-callback report was wrong. | A59 |
| Healing / Ethereal / Close Combat / Wind Shield | Integrated stated consumers | Healing/Ethereal count native Health deaths without the Enemy-tag filter; Close Combat tracks colliding Health; Wind Shield filters the projectile layer and returns the projectile to the native pool. Real death/collision/damage, active-effect Continue/expiry and original restart pass. | A60/A63/A74 |
| Cadence / Tenacity | Equivalent at reviewed rune boundary | Matching shipped recipes/callbacks retain their shared tooltip mismatches; those are balance/text anomalies, not missing Android updates. | A61 |
| Gun Glyph | Required — not yet ported | Windows secondary-bullet flat-damage composition and reduced knockback retained. | A62 |
| Dodge | Integrated random-roll consumer | Android stat/cap-based random roll replaces the Windows hit-counter guarantee. Seeded rolls, native collision dispatch, modified cap and restart verified. | A64 |
| Forest / Temple | Required — not yet ported | Android wave populations, HP, boss HP and some timings are not imported. Temple-versus-Forest balance is not benchmarked. | A66 |
| Endless | Required — not yet ported | Android's mixed pools, extra waves, 60-second scaling, boss frequency/affixes and XP curve are not ported. Windows mode remains native. | A67 |
| Pumpkin Patch EyeBat reference | Required — not yet ported | Valid Windows reference retained; Android's final-wave spelling is a possible source anomaly, not a proven runtime failure. | A68 |
| Quickplay opening wave | Required — not yet ported | Windows elite flag retained instead of Android's non-elite flag. | A69 |
| Native Endless opening timer | Required — not yet ported | Brief negative display at startup remains a known Windows issue. Continue preserves independent wave/cycle timing; it does not fix this display lifecycle. | A70 |
Existing Scarlett/Raven/Yuki/Luna/Shana and other exclusive effects also retain Windows burn/curse/freeze/aim/repeat consumers where the application recipe matches. Matching recipes are bounded evidence, not proof that every downstream Android algorithm was imported. See [migration decisions](docs/MIGRATION.md) and A06/A12/A38/A39/A50/A52.

### Audit and verification gaps

| Status | Exact evidence / remaining work |
|---|---|
| Open — method coverage | **442 structural API records** have dispositions, not 442 completed ports. The routing ledger contains **139 entries requiring finer reachable-gameplay review** (97 structurally changed, 42 Android-only). Lightning now has an explicit method/consumer link. Some already-ported types, including Charge Cannon and the new character passives, remain in the generic bucket; **139 is not a count of missing features or wholly unread method bodies**. Even feature-linked reviews do not cover every method in a class. Unreviewed gameplay and map/Endless rules remain mandatory work. |
| Open — asset matching | The decoded comparison has **1,072 serialized change records** and **155 ambiguous matching groups**. These include schema/platform/presentation changes and are not counts of missing gameplay features. Ambiguous groups were not arbitrarily paired; targeted original-pointer reviews resolve individual cases without closing every group. |
| Open — ledger consistency | The early default-powerup export still has an unresolved Hastur Frenzy row; the later exclusive-perk review resolves its modifier-ownership difference under A52. Use the later source-linked finding. Reconcile stale exports before deriving an unresolved-feature total from them. |
| Open — balance acceptance | Freeze dominance, summon versus weapon builds and scenario difficulty need controlled full-run benchmarks. Seeded integration tests do not establish overall balance or Android runtime parity. |
| Open — cross-device acceptance | 1.0.0 passed the phone's title/character-menu smoke test. 1.0.1 installation/preservation checks pass, but its updated phone artwork and full gameplay have not been verified. Final 1.0.1 publication/download verification remains outstanding. |

There is **no validated exact total of remaining unique gameplay changes**. The known retained differences above and the open coverage buckets overlap; adding their counts would produce a misleading number. Completing the selected mod features does not close those coverage gaps.

### Requested improvements beyond Android parity

| Feature | Status / remaining boundary |
|---|---|
| Achievement-based content unlocks and notices | Integrated thematic miniboss/boss/build/attribute/Darkness/scenario gates, preserved legacy ownership and persistent native-icon notifications. This is the requested progression redesign. |
| Rune progression | Integrated 20% rank-price growth, same-column prerequisite ranks and multiple enabled runes per row; native five-rank cap retained. |
| Custom game and mode defaults | Integrated native menus for obtained content and real owned-mode defaults. Credits remain available; unlocks, achievements and leaderboard submission are disabled. Exact parity with the unseen reference mod binary is unproven. |
| Isaac-style HUD | Integrated transparent icon-led attributes and collected skills with effective/base/bonus values, tiers/counts, paging and Options/F2. |
| Compatible recolours | Integrated the optional reversible style with original artwork restored when disabled. |
| Pixel-art maintenance credit / advertisement removal | Implemented in unpublished 1.0.1; preserves Flanne's credit, adds MAINTAINED BY DARKAXT to the logo artwork and removes the Emberpath panel/link. Final-package Windows rendering passes; phone visual verification remains open. |
| Auto-targeting / autofire | Existing native auto-targeting remains. The additional auto-targeting request was withdrawn; a new autofire feature has not been implemented. |

**Excluded:** mobile commerce, ads/reward systems, GamePass/account services, promotional rentals/coupons and unused placeholder content. These exclusions are not missing gameplay integrations. Third-party mod binaries/artwork are not bundled.

Culling, Mini Clip, Giant/Tomes, shared burn/area targeting, health/vision, base weapon/character tuning, direct heart grants and damage composition have passed their recorded native checks. Stalwart and refreshing effects, plus heart counters and elemental modifiers, also pass fresh-process Continue. The remaining reachable Android gameplay changes are required work and do not await a separate balance decision. No gameplay-completion release or deployment has occurred.

## Development

The tested Windows baseline is Steam build 12679152, Unity 2019.4.40f1 Mono x64. The gameplay reference is Android 8.6 (1093). You must own the required game inputs. Automated asset preparation and installation are included in the versioned package.

For an already prepared development player, set MODERNIZATION_SCRATCH to its root. It must contain game/MinutesTillDawn_Data/Managed and game/BepInEx/core. Build validation/Validation.csproj with a .NET SDK supporting netstandard2.0; outputs stay under that scratch root. Optional local build paths belong in ignored Directory.Build.local.props. [Installation and asset preparation](INSTALL.md) use the owned inputs; the release builder packages owned code and extraction recipes without game assets.

The validation assembly is separate from production and must never be installed in a normal player. It uses an isolated profile, synthetic in-process input and offscreen rendering. Seeded encounter clocks and build preconditions establish event/integration correctness, not unassisted full-run balance.

Original repository code is MIT licensed. Game assets and third-party dependencies retain their respective owners and licenses; this license grants no rights to redistribute them.
