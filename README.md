# 20 Minutes Till Dawn - Revamped

A community Windows mod for 20 Minutes Till Dawn by Flanne, maintained by Darkaxt. Selected, source-verified Android gameplay additions and fixes are integrated with native Windows gameplay, achievement-based unlocks, revised runes, run continuation and Custom game menus.

Source and packaging for **Revamped 1.0.1**. Use the versioned [release package](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases) and [installation instructions](INSTALL.md); see [delivery status and verification limits](docs/PROGRESS.md). This repository contains original mod and validation code, excluding the game, extracted artwork, saves, private research and third-party mod binaries. It is an independent mod repository; the proprietary game has no public upstream source to fork.

Implemented and verified in an isolated copied Windows player:

- Charge Cannon and three evolutions; Gale and Bulwark forms and exclusive upgrades.
- Bounded existing-feature fixes, finite repeat choices and thematic miniboss/boss unlocks.
- Rune prices increasing 20% per rank, column prerequisites and simultaneous row selections.
- Challenge mode, semantic run continuation and native Custom controls with owned-mode defaults.
- Transparent native icon stats/skills HUD, optional compatible recolours, and pixel-art “MAINTAINED BY DARKAXT” logo credit. The Emberpath promotion is removed in 1.0.1.

**Delivery:** 1.0.0 is the latest published release and its payload passed a DroidDeck 0.5.0 menu/bootstrap smoke test. The unpublished 1.0.1 update is installed locally on Windows and DroidDeck; its pixel-art credit/ad removal passed windowless Windows rendering at 720p and 1080p, including the final package. The updated phone installation passes file/preservation checks, but its new artwork has not yet been visually verified on the phone. Publication and further deployment are on hold while the integration disparity ledger is reviewed. [Delivery history and limits](docs/DROIDDECK.md).

Custom runs use obtained content and award credits while disabling unlocks, achievements and leaderboard submission. Switching mode defaults during play explicitly starts a new Custom run; restoring the current mode defaults removes its overrides.

Revamped is a selective migration, not complete Android parity. The versioned installer prepares owned-input assets locally and verifies the supported game and owned installation files. The delivery ledger distinguishes verified releases from an update still in progress.

## Controls and behavior

F2 or Options toggles the transparent stats HUD. Numbers show effective values followed by base and net bonus, before charge/conditional effects. Page Up/Page Down scroll the right-hand collected skill icons; T1/T2/T3 indicate tree tiers, S indicates a special upgrade, and ×N is its acquired count. Native movement, shooting, auto-targeting and controller bindings remain.

Pause → Save & return suspends a supported run without cashing out its score. Continue reconstructs that build on the next launch. New run explicitly discards the checkpoint. Custom mode and its pause settings use native menus; Load mode defaults can restore the current mode or explicitly start another owned mode as a new Custom run.

See [gameplay migration decisions](docs/MIGRATION.md) and the [anomaly/balance ledger](docs/ANOMALIES.md) for scope and retained differences.

## Android integration and disparity ledger

**Not all Android updates have been ported.** This comparison uses the supplied Android **8.6 (1093)** package and Windows Steam **12679152**; it does not establish parity with later Android releases. “Reviewed”, “retained” and “implemented” have different meanings. The earlier completed stages describe the selected implementation scope, not complete Android parity.

Status meanings: **Integrated** = implemented and verified at the stated boundary; **Windows retained** = the Android difference is known but not imported; **Open** = investigation or verification remains; **Excluded** = outside the requested gameplay scope. Retaining Windows behavior is not a claim that the two versions are equivalent. Rows group related changes and must not be counted as individual patches.

### Integrated gameplay additions and fixes

| Feature | What has been done | Remaining limit / evidence |
|---|---|---|
| Charge Cannon and three evolutions | Integrated native weapon selection, charge behavior, ammunition/reload and evolution choices. | Recovered source contracts and Windows runtime checks; not a transplant of Android's engine. [Migration](docs/MIGRATION.md). |
| Hina-Gale and Diamond-Bulwark | Integrated both playable forms, animation, skills/passives and all six exclusive upgrades. | Native selection, skill behavior, effects and cleanup verified. A10/A11 document source interpretation. |
| Six fallback upgrades | Integrated the recovered fallback effects, finite allowances and Adrenaline expiry; late/depleted choice pools remain usable. | Pool/time preconditions are seeded tests, not a naturally played 100-level run. A15. |
| Repeated upgrades | Corrected deep-copy identity so repeated picks invoke the intended stack path instead of duplicating first-pick owners. | Keeps Windows per-perk stack recipes; Android repeat strengths are not universally imported. A01/A39. |
| Combustion fire puddles | Removed the reachable two-collider query truncation; all overlapping test enemies receive the existing burn. | Windows burn strength, lifetime and stacking remain. A13. |
| Pooled sword/summon teardown | Corrected teardown of never-initialized pooled instances. | Does not replace the summon/pool architecture or change DPS. A14. |
| Holy Spear and Tome of Might | Corrected their documented maximum-HP damage contribution and verified injury/HP-upgrade/repeat behavior. | Android Tome of Might's separate apparent scaling regression is not imported. A21/A49, including A49's later Windows remediation. |
| Generator | Restored the missing effect attachment and corrected reactivation after expiry. | Native ammo-gain/refresh/repeat/lifecycle checks pass; other temporary buffs retain their own rules. A36. |
| Growth and Seismic Ward | Corrected Growth to the documented 4% XP per rank and Seismic cooldown reduction direction. | Native XP and real pulse intervals verified; not an import of the entire Android rune backend. A56/A57. |
| Boss arenas | Added boss-death ownership so an early-defeated boss's arena is released. | Native lifetime fallback and independent overlapping arenas retained. A65. |
| Challenge mode | Integrated the recovered 36-session/10-boss schedule, five quota tiers, progression gate and native results. | Seeded encounter/quota verification; full-length balance benchmarking remains open. |
| Save and Continue | Integrated semantic build/runtime checkpoints, finite pools, clocks/cycles, defeated encounters/reward drops and supported native state. | Ordinary live enemies, projectiles and RNG are not frozen snapshots. Pending Blessing resumes once at its saved delay. A71. |

### Known Android gameplay differences still not ported

The table lists **42 grouped, known retained disparities**, not 42 individual missing patches. These remain **Windows retained**. Some are potential improvements, some are tradeoffs or apparent Android regressions. None should be silently described as completed migration work. Full detail and source boundaries are in the [anomaly ledger](docs/ANOMALIES.md).

| Area / feature | Current mod behavior versus Android | Ledger |
|---|---|---|
| Culling | Windows +15% summon attack speed; Android +30%. Candidate change remains unapplied. | A02 |
| Mini Clip | Windows -5 max ammo; Android -4. Reload-trigger implications still need focused evaluation. | A03 |
| Tomes / Giant | Windows penalties, HP tradeoffs and size modifier retained; Android changes several tome penalties and Giant's size increase. | A04/A05 |
| Existing gun/evolution/character base tuning | Grenade Launcher remains 60 damage rather than Android 45; Flash Mag retains its Windows five-shot trigger rather than 20. Abby HP and Hina/Shana movement/skill tuning remain native. | A48 |
| Burn backend | Windows independently expires additive burn applications. Android accumulates/refreshes a shared burn and uses a different cap. The puddle coverage fix does not port this algorithm. | A12 |
| Health limits | Windows red/soul-health capacity cap and rounding retained; inspected Android health assignment paths differ. | A16 |
| Shield recovery | Windows cooldown-rate arithmetic retained; Android uses flat reductions and a minimum cooldown. Sword and Shield's completion boundary also differs. | A17/A42 |
| Aero/Electro Mastery and Glare | Modifier placement, flat/percentage bonuses and Glare consumers differ. Windows composition retained; matching descriptions do not prove equal damage. | A18/A26 |
| Thunderspawns / Pulsing Summons / Soul Link | Proc probability and/or area geometry differ. Windows values retained. | A19 |
| Bloodsuckers / Soul Drain / Soul Conversion | Windows spawns collectible hearts; Android directly grants health/shield in the inspected paths. | A20 |
| Soul Knight / Justice | Damage-growth interpretation and per-perk HP-growth caps differ. Windows behavior retained. | A22/A24 |
| Energized | Windows deterministic cadence retained instead of Android's random trigger. | A23 |
| Fresh Clip / Anger Point | Windows strength and independent overlapping temporary buffs retained; Android changes strength and/or refresh behavior. | A25 |
| Reflex / Vision | Movement-to-dodge growth/cap and vision-transform limits differ. Windows consumers retained. | A27/A31 |
| Best Friends / Magic Lens | Attack-speed and damage composition, plus Lens size clamping, differ. Windows behavior retained. | A28/A29 |
| Death Rounds / Hellspawns | Flat-damage placement/event scope and Android's added boss restriction are not imported. | A30/A32 |
| Kill Clip / Siege / Regeneration | Kill qualification, ammo conservation versus gain, and first-heart timing differ. Windows behavior retained. | A33/A34/A35 |
| Dragon / Scythe / Trained Dragon / Soul Powered | Base recipes, rotation, additive versus multiplicative growth and damage-modifier ordering differ. Windows behavior retained. | A37/A38 |
| Frostbite / Shatter / Assassin / Ghost Wizard repeats | Windows first-pick and repeat recipes retained. Android helper duplication changes repeat strength and/or event scope. | A39 |
| Aero Magic / Eye of the Storm | Windows spread and player-centered area retained; Android changes spread and moves the area to the Gale object. | A40 |
| Electro Mage / Magic Spear | Native cursor targeting and spear radial eligibility retained instead of the inspected Android target queries. | A41/A47 |
| Shatter frozen-target guard | Windows requires a frozen target. The inspected Android death handler lacks this guard; that apparent regression is not imported. | A43 |
| Splinter | Windows inherited projectile size and per-action activation limit retained; Android uses different size/counter ownership. | A44 |
| Holy Arts | Windows repeated-pick strength and delayed damage cadence retained; Android attaches full-strength producers differently. | A45 |
| Dragon Bond | Windows bullet-damage addition retained; Android issues separate summon-classified damage with different rounding/composition. | A46 |
| Enhanced Shadow / Necromancer | Android's additional global summon bonuses and changed clone cooldown recipe are not imported. | A50 |
| Abby Whirlwind | Windows independent overlapping reload buffs retained instead of Android refresh-only behavior. | A51 |
| Hastur / Katana and existing summon exclusives | Windows pool/modifier ownership and existing summon recipes retained rather than Android's keyed stat providers/helper architecture. | A52; [migration](docs/MIGRATION.md) |
| Tome of Electro | Windows area lightning retained; Android selects one random target. | A53 |
| Hina Hidden Blade | Windows triple-damage recipe and protection ownership retained; Android changes both damage and invulnerability. | A54 |
| Body and Soul | Windows already has a timed soul-heart pickup. Android's direct grants, extra capacity and finite counter are not imported. | A55 |
| Elemental Barrage | Both inspected versions add Fire Rate twice despite the Fire Rate/Reload Rate tooltip. The shared mismatch remains unfixed. | A58 |
| Dedication | Windows live summon-count callback retained; the inspected Android callback appears empty. | A59 |
| Healing / Ethereal / Close Combat / Wind Shield | Native kill qualification, collision filters and projectile lifecycle retained. Android callback/layer boundaries differ. | A60/A63 |
| Cadence / Tenacity | Shared tooltip/behavior mismatches remain; Android is not evidence of a correction. | A61 |
| Gun Glyph | Windows secondary-bullet flat-damage composition and reduced knockback retained. | A62 |
| Dodge | Windows modifier notification and hit-counter guarantee retained; Android's inspected random-roll path differs. | A64 |
| Forest / Temple | Android wave populations, HP, boss HP and some timings are not imported. Temple-versus-Forest balance is not benchmarked. | A66 |
| Endless | Android's mixed pools, extra waves, 60-second scaling, boss frequency/affixes and XP curve are not ported. Windows mode remains native. | A67 |
| Pumpkin Patch EyeBat reference | Valid Windows reference retained; Android's final-wave spelling is a possible source anomaly, not a proven runtime failure. | A68 |
| Quickplay opening wave | Windows elite flag retained instead of Android's non-elite flag. | A69 |
| Native Endless opening timer | Brief negative display at startup remains a known Windows issue. Continue preserves independent wave/cycle timing; it does not fix this display lifecycle. | A70 |

Existing Scarlett/Raven/Yuki/Luna/Shana and other exclusive effects also retain Windows burn/curse/freeze/aim/repeat consumers where the application recipe matches. Matching recipes are bounded evidence, not proof that every downstream Android algorithm was imported. See [migration decisions](docs/MIGRATION.md) and A06/A12/A38/A39/A50/A52.

### Audit and verification gaps

| Status | Exact evidence / remaining work |
|---|---|
| Open — method coverage | **442 structural API records** have dispositions, not 442 completed ports. The routing ledger contains **140 generic retained-API entries** (98 structurally changed, 42 Android-only). They need finer review/traceability before whole-engine coverage can be claimed. Some already-ported types, including Charge Cannon and the new character passives, are still in that generic bucket; **140 is not a count of missing features or wholly unread method bodies**. Even feature-linked reviews do not cover every method in a class. |
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

The optional small balance proposal starts with Culling, then Mini Clip, after focused gameplay checks. It remains unapplied; no new balance change is made by this documentation update.

## Development

The tested Windows baseline is Steam build 12679152, Unity 2019.4.40f1 Mono x64. The gameplay reference is Android 8.6 (1093). You must own the required game inputs. Automated asset preparation and installation are included in the versioned package.

For an already prepared development player, set MODERNIZATION_SCRATCH to its root. It must contain game/MinutesTillDawn_Data/Managed and game/BepInEx/core. Build validation/Validation.csproj with a .NET SDK supporting netstandard2.0; outputs stay under that scratch root. Optional local build paths belong in ignored Directory.Build.local.props. [Installation and asset preparation](INSTALL.md) use the owned inputs; the release builder packages owned code and extraction recipes without game assets.

The validation assembly is separate from production and must never be installed in a normal player. It uses an isolated profile, synthetic in-process input and offscreen rendering. Seeded encounter clocks and build preconditions establish event/integration correctness, not unassisted full-run balance.

Original repository code is MIT licensed. Game assets and third-party dependencies retain their respective owners and licenses; this license grants no rights to redistribute them.
