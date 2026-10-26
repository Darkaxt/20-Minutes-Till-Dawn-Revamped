# 1.1.8 — Exploration chests

**Added:** mint exploration chests with weighted XP, credits, healing, magnet, freeze, frenzy, normal upgrades and character upgrades. Common waves can place multiple rewards in different directions, with no more than three uncollected exploration chests. Waves occur every60–90 combat seconds; pauses and upgrade menus stop the clock.

Placement targets20–30 seconds of walking at the player's speed when spawned. Compact arenas cap the journey at reachable walls. Edge numbers are distance in game world units, not seconds. Rewards, positions, timers, exploration credits and the separate random sequence survive Continue. Upgrade rewards use eligible native pools and Take/Leave menus; credits join the normal run payout. Custom retains its no-unlock/no-achievement rules.

**Fixed:** shared boss/exploration pointer placement now keeps the entire icon, arrow and distance inside the safe area and separates targets sharing a direction. Collected boss pointers clear even while their reward menu disables the HUD.

**Ported:** no additional Android behavior in this release. Exploration rewards are a requested Revamped addition; the completed Android8.6 integration and approved bug corrections are retained.

The Windows native feature checks include all scenarios, Custom, actual physics collection of every reward, pointer/controller menus, current and original1.1.7 checkpoints, native credit payout, boss pickup regression, square/4:3/16:9 layouts and all zoom settings. Tests use isolated profiles and controlled clocks/random draws, not a claim of full-run balance or phone verification. Final package and public-download evidence is recorded in [PROGRESS.md](../docs/PROGRESS.md).

The existing Android updater remains compatible: only the mod DLL changes among614 managed targets. Saves and settings are preserved. This release does not deploy to Steam or DroidDeck automatically.
