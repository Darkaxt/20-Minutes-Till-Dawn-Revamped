# 20 Minutes Till Dawn - Revamped

A community Windows mod for 20 Minutes Till Dawn by Flanne, maintained by Darkaxt. Selected, source-verified Android gameplay additions and fixes are integrated with native Windows gameplay, achievement-based unlocks, revised runes, run continuation and Custom game menus.

**Development source; the final release has not been published.** See [stage status and verification limits](docs/PROGRESS.md). This repository contains original mod and validation code, excluding the game, extracted artwork, saves, private research and third-party mod binaries. It is an independent mod repository; the proprietary game has no public upstream source to fork.

Implemented and verified in an isolated copied Windows player:

- Charge Cannon and three evolutions; Gale and Bulwark forms and exclusive upgrades.
- Bounded existing-feature fixes, finite repeat choices and thematic miniboss/boss unlocks.
- Rune prices increasing 20% per rank, column prerequisites and simultaneous row selections.
- Challenge mode, semantic run continuation and native Custom controls with owned-mode defaults.

Custom runs use obtained content and award credits while disabling unlocks, achievements and leaderboard submission. Switching mode defaults during play explicitly starts a new Custom run; restoring the current mode defaults removes its overrides.

Gameplay and integrated HUD/title acceptance are complete. The usable installer/package and final delivery verification remain in progress. One normal release follows completion; stage commits are not releases.

## Controls and behavior

F2 or Options toggles the transparent stats HUD. Numbers show effective values followed by base and net bonus, before charge/conditional effects. Page Up/Page Down scroll the right-hand collected skill icons; T1/T2/T3 indicate tree tiers, S indicates a special upgrade, and ×N is its acquired count. Native movement, shooting, auto-targeting and controller bindings remain.

Pause → Save & return suspends a supported run without cashing out its score. Continue reconstructs that build on the next launch. New run explicitly discards the checkpoint. Custom mode and its pause settings use native menus; Load mode defaults can restore the current mode or explicitly start another owned mode as a new Custom run.

See [gameplay migration decisions](docs/MIGRATION.md) and the [anomaly/balance ledger](docs/ANOMALIES.md) for scope and retained differences.

## Development

The tested Windows baseline is Steam build 12679152, Unity 2019.4.40f1 Mono x64. The gameplay reference is Android 8.6 (1093). You must own the required game inputs. Automated asset preparation and installation belong to the final delivery stage and are not yet provided here.

For an already prepared development player, set MODERNIZATION_SCRATCH to its root. It must contain game/MinutesTillDawn_Data/Managed and game/BepInEx/core. Build validation/Validation.csproj with a .NET SDK supporting netstandard2.0; outputs stay under that scratch root. Optional local build paths belong in ignored Directory.Build.local.props.

The validation assembly is separate from production and must never be installed in a normal player. It uses an isolated profile, synthetic in-process input and offscreen rendering. Seeded encounter clocks and build preconditions establish event/integration correctness, not unassisted full-run balance.

Original repository code is MIT licensed. Game assets and third-party dependencies retain their respective owners and licenses; this license grants no rights to redistribute them.
