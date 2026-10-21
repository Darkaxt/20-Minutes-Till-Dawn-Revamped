# 20 Minutes Till Dawn - Revamped

A community Windows mod for 20 Minutes Till Dawn by Flanne, maintained by Darkaxt. Selected, source-verified Android gameplay additions and fixes are integrated with native Windows gameplay, achievement-based unlocks, revised runes, run continuation and Custom game menus.

**Development source; the final release has not been published.** See [stage status and verification limits](docs/PROGRESS.md). This repository contains original mod and validation code, excluding the game, extracted artwork, saves, private research and third-party mod binaries. It is an independent mod repository; the proprietary game has no public upstream source to fork.

Implemented and verified in an isolated copied Windows player:

- Charge Cannon and three evolutions; Gale and Bulwark forms and exclusive upgrades.
- Bounded existing-feature fixes, finite repeat choices and thematic miniboss/boss unlocks.
- Rune prices increasing 20% per rank, column prerequisites and simultaneous row selections.
- Challenge mode, semantic run continuation and native Custom controls with owned-mode defaults.

Custom runs use obtained content and award credits while disabling unlocks, achievements and leaderboard submission. Switching mode defaults during play explicitly starts a new Custom run; restoring the current mode defaults removes its overrides.

The transparent icon HUD, title credit, final integrated verification and usable installer/package remain in their assigned stages. One normal release follows their completion; stage commits are not releases.

## Development

The tested Windows baseline is Steam build 12679152, Unity 2019.4.40f1 Mono x64. The gameplay reference is Android 8.6 (1093). You must own the required game inputs. Automated asset preparation and installation belong to the final delivery stage and are not yet provided here.

For an already prepared development player, set MODERNIZATION_SCRATCH to its root. It must contain game/MinutesTillDawn_Data/Managed and game/BepInEx/core. Build validation/Validation.csproj with a .NET SDK supporting netstandard2.0; outputs stay under that scratch root. Optional local build paths belong in ignored Directory.Build.local.props.

The validation assembly is separate from production and must never be installed in a normal player. It uses an isolated profile, synthetic in-process input and offscreen rendering. Seeded encounter clocks and build preconditions establish event/integration correctness, not unassisted full-run balance.

Original repository code is MIT licensed. Game assets and third-party dependencies retain their respective owners and licenses; this license grants no rights to redistribute them.
