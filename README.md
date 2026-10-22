# 20 Minutes Till Dawn - Revamped

A community Windows mod for 20 Minutes Till Dawn by Flanne, maintained by Darkaxt. Selected, source-verified Android gameplay additions and fixes are integrated with native Windows gameplay, achievement-based unlocks, revised runes, run continuation and Custom game menus.

Source and packaging for **Revamped 1.0.1**. Use the versioned [release package](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases) and [installation instructions](INSTALL.md); see [delivery status and verification limits](docs/PROGRESS.md). This repository contains original mod and validation code, excluding the game, extracted artwork, saves, private research and third-party mod binaries. It is an independent mod repository; the proprietary game has no public upstream source to fork.

Implemented and verified in an isolated copied Windows player:

- Charge Cannon and three evolutions; Gale and Bulwark forms and exclusive upgrades.
- Bounded existing-feature fixes, finite repeat choices and thematic miniboss/boss unlocks.
- Rune prices increasing 20% per rank, column prerequisites and simultaneous row selections.
- Challenge mode, semantic run continuation and native Custom controls with owned-mode defaults.
- Transparent native icon stats/skills HUD, optional compatible recolours, and pixel-art “MAINTAINED BY DARKAXT” logo credit. The Emberpath promotion is removed in 1.0.1.

1.0.0 was released and installed on Windows; its exact payload also booted under DroidDeck 0.5.0 with a game-specific Proton loader override. [DroidDeck deployment](docs/DROIDDECK.md) records current verification and limits. The 1.0.1 artwork correction and updated delivery verification are in progress until the release is published.

Custom runs use obtained content and award credits while disabling unlocks, achievements and leaderboard submission. Switching mode defaults during play explicitly starts a new Custom run; restoring the current mode defaults removes its overrides.

Revamped is a selective migration, not complete Android parity. The versioned installer prepares owned-input assets locally and verifies the supported game and owned installation files. The delivery ledger distinguishes verified releases from an update still in progress.

## Controls and behavior

F2 or Options toggles the transparent stats HUD. Numbers show effective values followed by base and net bonus, before charge/conditional effects. Page Up/Page Down scroll the right-hand collected skill icons; T1/T2/T3 indicate tree tiers, S indicates a special upgrade, and ×N is its acquired count. Native movement, shooting, auto-targeting and controller bindings remain.

Pause → Save & return suspends a supported run without cashing out its score. Continue reconstructs that build on the next launch. New run explicitly discards the checkpoint. Custom mode and its pause settings use native menus; Load mode defaults can restore the current mode or explicitly start another owned mode as a new Custom run.

See [gameplay migration decisions](docs/MIGRATION.md) and the [anomaly/balance ledger](docs/ANOMALIES.md) for scope and retained differences.

## Remaining parity and balance work

The [anomaly ledger](docs/ANOMALIES.md) records confirmed differences and fixes. A remaining difference is not automatically a defect or a balanced improvement.

| Area | Current disposition |
|---|---|
| Android source coverage | All 442 structural API records have bounded dispositions, but 140 retained native APIs have uninspected bodies. Complete Android method parity is unproven. |
| Culling / Mini Clip | Windows summon attack speed +15% versus Android +30%; max-ammo adjustment -5 versus -4. Recorded candidates; not applied. |
| Fire / passive mechanics | Windows burn application lifetimes and cap remain. Android shared refresh/stack behavior, Reflex growth/cap, heart pickup delivery, Dragon Bond and Gun Glyph composition differ and need separate decisions/tests. |
| Other balance differences | Tomes, Giant, Grenade Launcher, Abby, Flash Mag and other recorded tuning remain native. Elemental Barrage's shared fire-rate/reload tooltip mismatch is recorded, not silently changed. |
| Maps and Endless | Windows wave/boss/XP/scaling rules remain. Android map tuning and the larger Endless rewrite are not ported. Challenge is separately integrated. |
| Practical balance | Freeze dominance, summon versus weapon builds and Temple versus Forest have not received controlled full-run benchmarks. Seeded tests verify integration, not overall balance. |
| Save continuation | Reconstructs the supported build and known runtime owners; it does not freeze ordinary enemies, projectiles or RNG. |
| Reference mod parity | Custom uses the documented/recovered fields and native effects; unseen third-party binary parity is not claimed. Native auto-targeting remains; no new autofire is included. |

The small optional balance proposal starts with Culling, then Mini Clip, after focused gameplay verification. It remains unapplied pending the requested later balance decision. Mobile commerce, advertisements/reward systems and account services are excluded.

## Development

The tested Windows baseline is Steam build 12679152, Unity 2019.4.40f1 Mono x64. The gameplay reference is Android 8.6 (1093). You must own the required game inputs. Automated asset preparation and installation are included in the versioned package.

For an already prepared development player, set MODERNIZATION_SCRATCH to its root. It must contain game/MinutesTillDawn_Data/Managed and game/BepInEx/core. Build validation/Validation.csproj with a .NET SDK supporting netstandard2.0; outputs stay under that scratch root. Optional local build paths belong in ignored Directory.Build.local.props. [Installation and asset preparation](INSTALL.md) use the owned inputs; the release builder packages owned code and extraction recipes without game assets.

The validation assembly is separate from production and must never be installed in a normal player. It uses an isolated profile, synthetic in-process input and offscreen rendering. Seeded encounter clocks and build preconditions establish event/integration correctness, not unassisted full-run balance.

Original repository code is MIT licensed. Game assets and third-party dependencies retain their respective owners and licenses; this license grants no rights to redistribute them.
