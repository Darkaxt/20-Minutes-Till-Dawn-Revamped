# 20 Minutes Till Dawn - Revamped

A community Windows mod for 20 Minutes Till Dawn by Flanne, maintained by Darkaxt.

**Windows 1.0.1 is released and installed, but Android 8.6 gameplay parity is incomplete.** The post-release source audit below found concrete behavior missing from the retained Windows routines. Earlier completion claims overstated the port's coverage.

[Download 1.0.1](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.0.1) · [Install / upgrade / uninstall](INSTALL.md) · [Anomaly ledger](docs/ANOMALIES.md)

The reference is the supplied **Android 8.6 / versionCode 1093** package. The supported Windows baseline is **Steam build 12679152, Unity 2019.4.40f1 Mono x64**. This comparison makes no claim about later Android versions. The target includes Android gameplay arithmetic, stacking, cooldowns, event ordering and quirks; monetization is excluded. The requested Windows controls, progression and interface changes are explicit overrides.

## What is delivered in 1.0.1

These features have implementation and recorded Windows gameplay/integration evidence. The checks establish the workflows described, rather than every possible build or full source equivalence.

| Area | Delivered behavior / evidence |
|---|---|
| New weapon and characters | Charge Cannon and its three evolutions; Gale and Bulwark with their exclusive upgrades, effects and native selection. Cannon modes have actual projectile/contact and Continue/restart checks. |
| Combat and upgrades | Reconstructed projectile/contact/pool, burn/freeze/curse/lightning, damage/stat composition, perk recipes/repeats and summon/character owners. Recorded acquisition, event/physics, repeat and Continue/restart checks cover the named workflows in the evidence ledger. This does not establish every retained Windows method as equivalent. |
| Existing characters | Recorded native ability/upgrade/actor workflows include Shana's Specialize/Ascension/Halo, Hina, Scarlett, Yuki, Lilith, Abby, Diamond, Hastur, Katana, Raven, Luna and Dasher. |
| Scenarios and modes | Source-linked Forest, Temple, Pumpkin Patch, Quickplay, Challenge and Endless scheduling/clock/encounter/affix behavior, with recorded native mode/Continue/restart checks. |
| Save and Continue | Save & return and fresh-process reconstruction of current runs. Selected legacy saves rebuild Android state under the user's chosen policy; the historical schema-3 reader corpus is checked. Every old schema/build combination is not established. |
| Unlocks and notices | Thirty thematic miniboss/boss/build/attribute/Darkness/scenario gates, preservation of prior ownership, persistent queued unlock notifications and fresh-process deduplication. |
| Rune progression | Prices increase 20% per purchased rank, rounded upward. Lower tiers require preceding runes in the same column fully upgraded. Multiple choices in one row can be enabled; the native five-rank cap remains. |
| Custom game | Native configuration and live menus, obtained-content limits and Load mode defaults. Runs award credits while suppressing unlocks, achievements and leaderboard submission. Configured native input/spawning, save/Continue, victory and original-mode restart are checked. [Control inventory](docs/CUSTOM.md). |
| HUD and artwork | Transparent icon-led attributes on the left and collected skills on the right, effective/base/bonus values, tiers/counts and paging. Optional reversible recolours; native pixel-art MAINTAINED BY DARKAXT credit preserving Flanne's credit; Emberpath panel/link removed. Recorded native UI checks cover 720p and 1080p. |
| Windows delivery | The final package passes the installed isolated-player roster/input test covering 15 character entries and 12 base weapons, plus actual firing/reload and repeated scene exits. The actual Steam installation verifies 614 payload files; rollback preserves 142 original files and three real profile files. |

The public release is tagged **v1.0.1**, built from **2df4458f72fe5b3199053abe832762c5459796ea**. Its ZIP was independently downloaded and checked against the packaged file manifest, DLL, version, source revision and installed payload. The archive contains the mod and extraction/installation recipes; **593 runtime assets are prepared locally from owned game inputs**, rather than redistributed in the download.

## Android 8.6 parity ledger

Post-release audit and implementation, **2026-10-24**. Released **1.0.1 still contains the gaps below**. The working source reconstructs their complete reached methods/coroutines, keeping the original serialized weapon and AI owners. Source changes are not a delivered update until their affected acceptance and final delivery gates pass.

| ID / feature | Android contract missing from 1.0.1 | Working source / acceptance |
|---|---|---|
| GAP-01 / A276 — Crossbow family | The next full-charge tick posts the shooter-owned shot event and releases automatically. Release returns on empty ammo and clamps negative charge before recipe arithmetic. Applies to Crossbow, Quantum Bolt, Quick Load and Wind Bolt. | [Complete charge/release owner](src/AndroidChargedShooting.cs). S4/run-400 passes all four native recipes/evolution selection, held auto-fire, release/reload, real projectile damage and directed empty/negative-charge edges. Run-402 passes exact-save Continue, controller Aim/Fire/release and an obtained, edited Custom Crossbow with real damage and no achievement leakage. |
| GAP-02 / A277 — Laser arming/rearming | Activate direct beam children with colliders disabled, wait0.1 seconds, then enable colliders. Repeat attacks restore colliders disabled by previous player contact. Shoggoth and the original tentacle attack use this coroutine. | [Complete laser coroutine](src/AndroidAISpecials.cs). Run-400 passes actual Shoggoth AI, both collider phases and repeated physical contact on the same beam. Run-402 also passes both arming phases on two original tentacle AI attacks and repeats Shoggoth physical contact after exact Continue. |
| GAP-03 / A278 — Spawn attack cue | Yog/Reaper play one immediate cue at special activation; their iterators only wait and spawn. Native repeated the cue after each windup, adding sound/random calls. | [Complete activation/iterator](src/AndroidAISpecials.cs). Run-400 passes one immediate cue and every real Yog3/Reaper2 batch through native AI/pool boundaries. Run-402 repeats both full AI/spawn sequences after exact Continue and in Custom. |
| GAP-04 / A279 — Camera follow | Source CameraRig.Update is empty. Matching zero-offset hierarchy follows the player; Windows adds mouse-driven camera pan. Aiming is owned separately by the native input/cursor. | [Source camera Update](src/AndroidCameraHeight.cs). Run-400 passes far-cursor input and actual movement/follow for all four Crossbow recipes. Run-402 passes the affected native mouse/controller, exact Continue and Custom workflows. |
| A280 — Blessing source state arithmetic | Source startup waits0.1 seconds, equips/removes the Holy Shield offer and calls ReduceCD, updating its reduction/deadline owner. Native changes the base cooldown field. Fresh effective cooldown previously matched; no normal positive-deadline counterexample was established. | [Complete source iterator](src/AndroidBlessingRune.cs) now uses the existing source shield owner while preserving the bootstrap/Continue observer. Run-403 saves a genuinely pending Custom startup;404 completes it once after exact Continue, preserves base120/reduction5/effective115 and removes the finite offer;405 repeats exact completed-bootstrap Continue and native navigation/exit. This source-contract difference is ported without claiming an unproved visible symptom. |

Original Android entry points: ChargedShooter.Shoot0x197910c / OnStopShoot0x1978e40; laser iterator0x18b1410; spawn Use0x18b2b90 / iterator0x18b2d10; CameraRig.Update0x187c988; Blessing iterator0x184f8c4 / ReduceCD0x196229c. Original assets and Windows/current routes are pinned in the local audit. Android quirks are preserved; these changes introduce no independent balance corrections.

## Audit coverage and unresolved evidence

The first audit inventory-routed all **277 Android-only flanne types** and **1,072 serialized-change rows**. It independently compared **58 semantic bodies across27 types**; its109 exported ranges include boilerplate. Pass2 disposed all14 named retained-type candidates and34 exact method-name candidates with54 metadata-bounded ranges; it found the camera gap. Pass3 resolved the ShootDetector teardown thunk and traced the Blessing helper/state difference. These are bounded comparisons, not an exhaustive missing-feature count. The wider2,912-symbol capture corpus is available evidence, not2,912 reviewed behaviors.

| Remaining question | Current evidence / limit |
|---|---|
| Other retained Windows bodies | Broad retained-body coverage outside the explicit comparisons is not established. The14 previously named candidates now have dispositions; an empty candidate list does not prove whole-game equivalence. |
| Smite targeting predicate | The bounded original target inventory has Enemy/EnemyChampion/EnemyPassive tags on the reached monster layers, satisfying the native predicate. No rejected-target/random-order counterexample was established. Arbitrary modded targets and unreviewed routes are not covered. |
| Blessing recovery deadline | Raw source behavior is reconstructed, including deadline advancement. A naturally reached positive deadline before startup completion was not established; no invented gameplay reproduction is claimed. |
| Combinations and old saves | Recorded checks cover bounded current-run, selected legacy and Custom workflows. Every character × weapon × evolution × perk combination, every historical save schema and natural full-run balance remain unproved. These limits are not confirmed missing features. |

[PROGRESS.md](docs/PROGRESS.md) and [ANOMALIES.md](docs/ANOMALIES.md) retain dated implementation/acceptance history. P2 remains **ACTIVE**. Earlier COMPLETE labels describe historical checkpoints and do not prove global parity. A276–A280 were classified before affected builds/game checks. The confirmed minimum is source-integrated and verified. A further bounded discovery pass covers eight retained buff/action types; final source reconciliation and delivery remain. No intermediate software release has been published.

## Controls and intentional differences

- F2 or Options toggles the transparent HUD. Values show effective attributes followed by base and net bonus, before charge/conditional effects. Page Up/Page Down scroll skills; T1/T2/T3 show tree tier, S a special upgrade, and ×N the acquisition count.
- Native keyboard, mouse, controller and existing Space-toggle auto-targeting remain. The extra auto-targeting request was withdrawn. There is no separate new autofire toggle.
- Pause → Save & return suspends a run without cashing out its score. Continue reconstructs it; New run discards the checkpoint.
- Custom uses owned content and credits-only results. Restoring the current mode defaults removes overrides; switching to another mode explicitly starts a new Custom run.
- Milestone unlocks, the rune economy/selections, native desktop controls and the HUD/artwork are requested overrides of the Android presentation/progression.
- Mobile commerce, advertisements/reward/account/GamePass flows and source-proven unused placeholders are excluded. These exclusions do not cover ordinary gameplay effects or source bugs.
- DroidDeck installation/gameplay acceptance is a separate unfinished device request. It does not gate Windows validation or release. [Device history](docs/DROIDDECK.md).

## Development

You must own the supported Windows game and exact Android package described in [INSTALL.md](INSTALL.md). This is an independent mod repository; the proprietary game has no public source repository to fork. Game binaries, extracted artwork, saves, raw disassembly and private research are not published here. Third-party mod binaries/artwork are not bundled.

For an already prepared development player, set `MODERNIZATION_SCRATCH` to its root, containing `game/MinutesTillDawn_Data/Managed` and `game/BepInEx/core`. Build `validation/Validation.csproj` with a .NET SDK supporting netstandard2.0; outputs stay under that scratch root. Optional local paths belong in ignored `Directory.Build.local.props`.

The validation assembly is separate from production and must not be installed in a normal player. Tests use an isolated profile, in-process synthetic input and windowless offscreen rendering. Seeded clocks/builds establish the declared integration behavior, not natural balance or universal parity. The installed release remains1.0.1 while the current parity batch is implemented and verified.

Original repository code is MIT licensed. Game assets and dependencies retain their owners and licenses. [Third-party notices](docs/THIRD-PARTY.md).
