# 20 Minutes Till Dawn - Revamped

A community Windows mod for 20 Minutes Till Dawn by Flanne, maintained by Darkaxt.

**Version 1.1.2 closes the Android 8.6 gameplay integration and retained-code ledger, including the approved bug corrections.** The final source pass imports the remaining shooting, Abby, observer, reload, physics, pickup and transition behavior, and fixes Glass multiplying again on Continue.

[Download 1.1.2](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.1.2) · [Install / upgrade / uninstall](INSTALL.md) · [Anomaly ledger](docs/ANOMALIES.md) · [Bug corrections](docs/BUGFIXES.md)

The reference is the supplied **Android 8.6 / versionCode 1093** package. The supported Windows baseline is **Steam build 12679152, Unity 2019.4.40f1 Mono x64**. This comparison makes no claim about later Android versions. The target includes Android gameplay arithmetic, stacking, cooldowns, event ordering and quirks; monetization is excluded. The requested Windows controls, progression and interface changes are explicit overrides.

## Features

These features have implementation and recorded Windows gameplay/integration evidence. The checks establish the workflows described, rather than every possible build or full source equivalence.

| Area | Delivered behavior / evidence |
|---|---|
| New weapon and characters | Charge Cannon and its three evolutions; Gale and Bulwark with their exclusive upgrades, effects and native selection. Cannon modes have actual projectile/contact and Continue/restart checks. |
| Combat and upgrades | Reconstructed projectile/contact/pool, burn/freeze/curse/lightning, damage/stat composition, perk recipes/repeats and summon/character owners. Recorded acquisition, event/physics, repeat and Continue/restart checks cover the named workflows in the evidence ledger. The retained-entry audit below records reconstructed owners and native integration boundaries separately. |
| Existing characters | Recorded native ability/upgrade/actor workflows include Shana's Specialize/Ascension/Halo, Hina, Scarlett, Yuki, Lilith, Abby, Diamond, Hastur, Katana, Raven, Luna and Dasher. |
| Scenarios and modes | Source-linked Forest, Temple, Pumpkin Patch, Quickplay, Challenge and Endless scheduling/clock/encounter/affix behavior, with recorded native mode/Continue/restart checks. |
| Save and Continue | Save & return and fresh-process reconstruction of current runs. Selected legacy saves rebuild Android state under the user's chosen policy; the historical schema-3 reader corpus is checked. Every old schema/build combination is not established. |
| Unlocks and notices | Thirty thematic miniboss/boss/build/attribute/Darkness/scenario gates, preservation of prior ownership, persistent queued unlock notifications and fresh-process deduplication. |
| Rune progression | Prices increase 20% per purchased rank, rounded upward. Lower tiers require preceding runes in the same column fully upgraded. Multiple choices in one row can be enabled; the native five-rank cap remains. |
| Custom game | Native configuration and live menus, obtained-content limits and Load mode defaults. Runs award credits while suppressing unlocks, achievements and leaderboard submission. Configured native input/spawning, save/Continue, victory and original-mode restart are checked. [Control inventory](docs/CUSTOM.md). |
| HUD and artwork | Transparent icon-led attributes on the left and collected skills on the right, effective/base/bonus values, tiers/counts and paging. Optional reversible recolours; native pixel-art MAINTAINED BY DARKAXT credit preserving Flanne's credit; Emberpath panel/link removed. Recorded native UI checks cover 720p and 1080p. |
| Windows delivery | The final package passes the installed isolated-player roster/input test covering 15 character entries and 12 base weapons, plus actual firing/reload and repeated scene exits. The actual Steam installation verifies 614 payload files; rollback preserves 142 original files and three real profile files. |

The download contains the mod and extraction/installation recipes. **593 runtime assets are prepared locally from owned game inputs**, rather than redistributed. The installer owns 614 payload files, preserves the original game and user configuration, and supports exact rollback. [Current delivery receipt](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/blob/main/docs/PROGRESS.md).

**v1.1.2 is published and installed on the verified Windows target**, source **7df67d5af2f45d6b280e191a63d2bb14c108946a**. Its independent public download matches the installed DLL; packaged roster/input, exact rollback and original/profile/configuration preservation pass. [Delivery receipt](docs/PROGRESS.md).

## Android 8.6 parity ledger

The retained-gameplay comparison and anomaly classification are closed. Every identified required Android 8.6 gameplay difference is implemented; no gameplay integration is parked for a later balance decision. Source quirks remain unless covered by the user's explicit bug-fix authorization.

| ID | Integrated behavior | Native Windows acceptance |
|---|---|---|
| A129 | Rebuild Glass from acquisitions at each run boundary; three picks remain factor 6 across repeated warm and fresh Continue. | 480/481 |
| A281 | Anger Point's separate Fire Rate/Bullet Damage operands apply, refresh and expire in source order. | 480/481 |
| A282 | Complete Courage/Death Spin reload activation/lifetime with no extra Windows sound. | 482/484 |
| A283–A284 | Null-before-roll on-hit dispatch; standalone fire uses NPC layers; sampled projectiles burn once. | 482/484 and 485/486 |
| A285 | Android 0.03-second physics cadence, retaining the matching 0.1 maximum frame step. | Combined affected contact/movement/pool/AI/rune/weapon/Continue matrix below. |
| A286–A287 | Global Dragon Bond/Spark observer lifetimes, first-Dragon selection and source target/RNG order. | 485/486: real non-player spore arc/landing/explosion, primary contact, teardown and Continue. |
| A288 | Complete Abby spray/final-shot/lock-release flow; one gun event after sound for the whole shot. | 482/484: native skill input and single/multiple shooters. |
| A289–A292/A295 | Source base/charged emission, raw projectile-count getter, powerup singleton replacement, disabled entry and gameplay transition ownership. | 482/484: native firing/reload, directed count/singleton/nested-transition edges and source Continue/restart. |
| A293–A294 | XP body-contact exclusion while hearts remain collectible; functional native Hina/Shield/Glyph contacts retain the approved correction to Android's Default-layer exclusion defect. | 482/484 plus actual shield/Hina/Glyph workflows at 0.03. |

The current runtime receipt pins the exact source and production DLL for every affected workflow and byte-binds fresh Continue inputs to successful saved checkpoints. No required source/runtime item remains unresolved. Final artifact publication and installation verification are recorded in [PROGRESS.md](docs/PROGRESS.md).

Audit and implementation, **2026-10-24**. These behaviors were absent from 1.0.1 and are included in 1.1.0. The complete reached methods/coroutines retain the original serialized weapon and AI owners.

| ID / feature | Android contract missing from 1.0.1 | 1.1.0 implementation / acceptance |
|---|---|---|
| GAP-01 / A276 — Crossbow family | The next full-charge tick posts the shooter-owned shot event and releases automatically. Release returns on empty ammo and clamps negative charge before recipe arithmetic. Applies to Crossbow, Quantum Bolt, Quick Load and Wind Bolt. | [Complete charge/release owner](src/AndroidChargedShooting.cs). S4/run-400 passes all four native recipes/evolution selection, held auto-fire, release/reload, real projectile damage and directed empty/negative-charge edges. Run-402 passes exact-save Continue, controller Aim/Fire/release and an obtained, edited Custom Crossbow with real damage and no achievement leakage. |
| GAP-02 / A277 — Laser arming/rearming | Activate direct beam children with colliders disabled, wait0.1 seconds, then enable colliders. Repeat attacks restore colliders disabled by previous player contact. Shoggoth and the original tentacle attack use this coroutine. | [Complete laser coroutine](src/AndroidAISpecials.cs). Run-400 passes actual Shoggoth AI, both collider phases and repeated physical contact on the same beam. Run-402 also passes both arming phases on two original tentacle AI attacks and repeats Shoggoth physical contact after exact Continue. |
| GAP-03 / A278 — Spawn attack cue | Yog/Reaper play one immediate cue at special activation; their iterators only wait and spawn. Native repeated the cue after each windup, adding sound/random calls. | [Complete activation/iterator](src/AndroidAISpecials.cs). Run-400 passes one immediate cue and every real Yog3/Reaper2 batch through native AI/pool boundaries. Run-402 repeats both full AI/spawn sequences after exact Continue and in Custom. |
| GAP-04 / A279 — Camera follow | Source CameraRig.Update is empty. Matching zero-offset hierarchy follows the player; Windows adds mouse-driven camera pan. Aiming is owned separately by the native input/cursor. | [Source camera Update](src/AndroidCameraHeight.cs). Run-400 passes far-cursor input and actual movement/follow for all four Crossbow recipes. Run-402 passes the affected native mouse/controller, exact Continue and Custom workflows. |
| A280 — Blessing source state arithmetic | Source startup waits0.1 seconds, equips/removes the Holy Shield offer and calls ReduceCD, updating its reduction/deadline owner. Native changes the base cooldown field. Fresh effective cooldown previously matched; no normal positive-deadline counterexample was established. | [Complete source iterator](src/AndroidBlessingRune.cs) now uses the existing source shield owner while preserving the bootstrap/Continue observer. Run-403 saves a genuinely pending Custom startup;404 completes it once after exact Continue, preserves base120/reduction5/effective115 and removes the finite offer;405 repeats exact completed-bootstrap Continue and native navigation/exit. This source-contract difference is ported without claiming an unproved visible symptom. |

Original Android entry points: ChargedShooter.Shoot0x197910c / OnStopShoot0x1978e40; laser iterator0x18b1410; spawn Use0x18b2b90 / iterator0x18b2d10; CameraRig.Update0x187c988; Blessing iterator0x184f8c4 / ReduceCD0x196229c. Original assets and Windows/current routes are pinned in the local audit. The parity ports preserve their source arithmetic; the separately approved correction contract governs the22 repaired bug families.

## Audit coverage and boundaries

The final audit binds the original **798 Windows types / 4274 methods** to observed production routes, exact method/owner reviews, serialized acquisition roots and explicit native integration dispositions. Source-only gameplay additions and changed serialized identities are joined to the existing powerup/character/weapon/rune/scenario reviews. Captured ARM ranges and inventory membership are not counted as semantic reviews.

| Boundary | Final disposition |
|---|---|
| Retained nontrivial gameplay entries | Exact source-linked method or reconstructed-owner disposition; no unresolved required entry remains in the finite checklist. |
| Replaced native bodies and old action graphs | Complete Android owners or source-shaped acquisition recipes own the flow. Native signatures, serialized fields and still-rooted helpers remain necessary ABI. |
| Constructors, accessors and generated methods | Actual gameplay scalars/defaults and coroutine owners were separated from storage, iterator scaffolding and serialized ABI. This does not claim all compiled bodies are identical. |
| Global helpers and libraries | Source numeric/modifier/notification/state primitives are compared or replaced; Windows Unity/Mono, tween/audio/camera libraries, copying ABI and desktop presentation remain platform integration boundaries. |
| Source quirks | A144's cleanup spelling, A150/A254's capped writer/retained-clock rule and A266's first-Start cadence have explicit source/lifetime conclusions. Unknown developer intent is not deferred implementation. |
| Selected legacy formats | The native reader checks historical schema3 inputs and explicitly derived schema4–6 layouts for identical full source reconstruction and input preservation. Real legacy Continue/combat/save7/restart and current source Continue are checked separately. Unrecoverable history uses the user's Android-rebuild choice. |
| Verification limits | The connected checks are deterministic Windows workflows with declared seeds/builds/geometry. They do not claim every character × weapon × perk combination, natural full-run balance, mathematical source equivalence or Android-device observation. These are verification limits, not known omitted gameplay features. |

Primary new-flow proof: Glass/Anger 480/481; Abby/shooting/reload/fire/pickups/state 482/484; Spark/Dragon impacts 485/486. The combined matrix also covers corrected combat/status, barriers, runes/shields, Hina/Katana, AI/world, arrows/Cannon/Crossbow, legacy and Custom input/results. The immutable local evidence records source ranges, original asset identities, native routes, checkpoints and exact binaries; proprietary game code/assets are not published.

[ANOMALIES.md](docs/ANOMALIES.md) keeps the current classification and dated original observations; [PROGRESS.md](docs/PROGRESS.md) keeps acceptance/delivery receipts. Older open/COMPLETE labels describe their recorded revisions and are superseded by the current reconciliation.

## Approved bug corrections and dead-code audit

The approved 23-family review produced **22 corrections and one verified absent Windows equivalent** in 1.1.0. Version 1.1.1 adds three demonstrated lifecycle corrections and truthful descriptions, under the user's subsequent authorization. [The correction ledger](docs/BUGFIXES.md) maps the exact contracts and native evidence. These approvals override Android-bug parity for the named findings; other gameplay refinements remain required.

| 1.1.1 finding | Correction |
|---|---|
| A136 — Hina | The first Shadow Clone retains its actual skill activation position when it detaches, rather than jumping to the world origin. |
| A242 — Rhogog | A reused explosion reactivates its existing damage child; contact still consumes that child for the current activation. |
| A234 — Thousand Blades | Destroyed sword-spawn helpers unsubscribe; disabled live helpers still receive the upgrade. |
| A19/A22/A61/A219 — Descriptions | Thunderspawns, Soul Knight, Cadence, Tenacity and Reckless text describes the existing mechanics without changing their values or achievement conditions. |

Glass's demonstrated Windows warm-Continue multiplication is corrected in 1.1.2. Moth cadence, the capped Kunoichi writer and Ammo's source cleanup spelling retain their recovered behavior; no speculative balance changes were added.

The requested [redirected-flow dead-code audit](docs/DEAD-CODE.md) found native bodies skipped by live Harmony replacements and per-owner recipes superseded by Android graphs. It established no definitely removable mod class/method. Native entry points, reused helpers and legacy readers remain rooted. No dead-code deletion was performed.

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

The validation assembly is separate from production and must not be installed in a normal player. Tests use an isolated profile, in-process synthetic input and windowless offscreen rendering. Seeded clocks/builds establish the declared integration behavior; natural balance and universal parity are not established by those fixtures. Publication and installed-package verification are recorded against the exact release artifact.

Original repository code is MIT licensed. Game assets and dependencies retain their owners and licenses. [Third-party notices](docs/THIRD-PARTY.md).
