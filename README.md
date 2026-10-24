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

## Android 8.6 missing-feature ledger

Post-release audit, **2026-10-24**, against the production source in 1.0.1. These are source-confirmed gaps, not optional balance decisions. The scan uses original Android ARM bodies and asset ownership, original Windows managed bodies, and the current entry-point wiring. Predicted symptoms below have not received a new gameplay reproduction during this documentation audit.

| ID / feature | Android behavior | Current Windows gap | Evidence / required work |
|---|---|---|---|
| GAP-01 — Charged weapon behavior | At maximum charge, another held-fire tick posts the shot event and invokes release automatically. Release returns when ammo is empty and clamps a negative charge to zero before applying damage, size and piercing. | Crossbow, Quantum Bolt, Quick Load and Wind Bolt retain a charged shooter that does nothing at maximum charge until input is released. Its release lacks the source ammo check and negative-charge normalization. | Android `ChargedShooter.Shoot` at `0x197910c` and `OnStopShoot` at `0x1978e40`; original Windows `ChargedShooter`; [current gun dispatch](src/AndroidShooting.cs) calls the retained subclass. [Cannon replacement](src/Plugin.cs) covers registered Cannon modes, leaving this family unchanged. Port the complete charged-shooter behavior and verify its actual weapon consumers. |
| GAP-02 — Laser arming and rearming | The laser coroutine activates the beam children with their colliders disabled, waits 0.1 seconds, then re-enables the colliders for the attack. Each activation restores those collider states. | Shoggoth and Hastur's tentacle attacks retain the native coroutine, which activates the group without either collider pass. Beams arm early, and a beam collider disabled by player contact lacks the source rearm path on subsequent attacks. | Android `AILasersSpecial.LaserCR.MoveNext` at `0x18b1410`; original Windows laser coroutine; [player collision](src/AndroidPlayerCollision.cs) disables `PF_Laser` colliders on contact. Port the source coroutine, including collider timing/reuse. |
| GAP-03 — Spawn-attack sound timing | A non-null spawn-attack sound plays once when the special attack starts; the spawn coroutine only waits and spawns. | Yog plays three delayed cues and Reaper two, instead of one immediate cue. The retained Windows coroutine plays the sound after each windup, also adding sound/RNG calls. | Android `AISpawnSpecial.Use` at `0x18b2b90` and `WaitToSpawn.MoveNext` at `0x18b2d10`; original Windows `AISpawnSpecial`. [AI integration](src/AndroidAI.cs) replaces the spawner and other AI paths, but leaves this special's sound owner/timing native. Move it to the source activation boundary. |

No gameplay fix for these findings is included in this documentation update. A published package, a type-routing disposition or a passing roster smoke test does not close them.

## Audit coverage and unresolved evidence

The completed audit inventory-routed all **277 Android-only `flanne` namespace types** and **1,072 serialized-change rows** against existing routes/aliases. It independently compared **58 semantic method bodies across 27 types**. Its new exports contain 109 metadata-bounded ranges; those include boilerplate and are not 109 independently reviewed functions. The wider corpus contains 2,912 captured symbol names, which are not all reviewed behavior. These figures describe research coverage, not delivered-feature counts. **The three confirmed feature groups above are a minimum, not an exhaustive missing-feature count.**

| Remaining question | Current limit |
|---|---|
| Retained Windows bodies | Many bodies outside the 58 explicit comparisons remain independently unreviewed by this audit. The final exact-name capture diagnostic lists 14 instantiated types, including ShootDetector, DifficultyModifier, CameraRig, TieredUnlockManager, bounce/visual helpers and presentation/link controls. Aliases, existing replacements and explicit overrides must be resolved before treating any as missing code. |
| Smite targeting predicate | A source layer/tag predicate difference needs a concrete reachable original target before it can be classified as an observable gameplay gap. It is not silently accepted as equivalent. |
| Combinations and old saves | Recorded checks cover bounded current-run, selected legacy and Custom workflows. Exhaustive character × weapon × evolution × perk combinations, every historical save schema and natural full-run balance are not established. These are coverage limits, not evidence of additional missing features. |

[PROGRESS.md](docs/PROGRESS.md) and [ANOMALIES.md](docs/ANOMALIES.md) retain dated implementation/acceptance history. Earlier COMPLETE or unverified labels there describe those checkpoints; the source-confirmed gaps above reopen parity acceptance. Classification of the prior anomaly records through A275 does not establish complete gameplay parity. Android quirks remain part of the required port; an independent balance/bug-fix patch has not been applied.

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

The validation assembly is separate from production and must not be installed in a normal player. Tests use an isolated profile, in-process synthetic input and windowless offscreen rendering. Seeded clocks/builds establish the declared integration behavior, not natural balance or universal parity. A documentation-only audit does not rebuild or change the installed release.

Original repository code is MIT licensed. Game assets and dependencies retain their owners and licenses. [Third-party notices](docs/THIRD-PARTY.md).
