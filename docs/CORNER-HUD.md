# Compact corner status HUD — 2026-10-30

Authoritative scope: implement the agreed portrait-and-three-row corner HUD inspired by the supplied screenshots. The user explicitly limits its bottom edge to the bottom of the Score text, then asks that removing the full-width level bar also move the top HUD upward. Existing authorization includes implementation, focused verification, commit and a normal release through the established updater channel. No live Steam/phone deployment or save reset.

| ID | Required behavior and acceptance | Stage |
|---|---|---|
| C01 | Replace the full-width XP/level strip with a compact upper-left cluster: current character portrait in a thin circular frame, level badge inside its lower edge, hearts/soul hearts on the first row, short thin XP bar on the second, native bullet icon/current-max ammunition on the third. Use the existing native live bindings. No enclosing background panel. | C1 |
| C02 | The entire cluster fits inside the reported safe area and ends at or above the actual Score text's bottom edge. Move the centered timer, Score and objective upward by24 native-canvas units to reclaim the former bar area; move the Custom badge into the freed strip above the timer without overlap. Keep the existing vertically centered metrics and acquired-skill rows, and the existing HUD opacity setting/default85%. | C1 |
| C03 | Handle ordinary and expanded health/soul hearts, different native/ported portrait shapes, live health/XP/ammunition updates, level-up, reload, pause/resume and Continue. Keep existing visibility/input owners; do not introduce another progression/stat counter. Preserve gameplay, settings, progress and POI direction logic. | C1 |
| C04 | Verify native render/layout at1280×720,720×720 and1920×1080, with measured bounds against Score ink and representative fresh/expanded-health/Continue states. Inspect actual screenshot evidence. Update README/changelog and publish a normal verified package with copied install/rollback, independent public download, preservation checks and exact scratch cleanup. | C1 / C2 |
| C05 | Give only the portrait's circular frame a stepped pixel-art contour. Preserve the approved native portrait rendering, composition and all other widgets. User clarified that the portrait itself already looks good. | C1 |

| Stage | Status | Satisfied | Remaining / blockers | Deferrals |
|---|---|---|---|---|
| C1 Integrated compact status HUD | COMPLETE | C01–C03/C05 and C04 native acceptance verified in runs3/4/6 | None | None |
| C2 Final delivery | ACTIVE | C1 reconciled; README and release notes updated | C04 package/public verification and cleanup | None |

Visual contract: retain the game's pixel art, font and cream/coral/muted-green palette. Approximately260 reference pixels wide; height is constrained by the Score ink, not a fixed taller card. Portrait and all three status rows remain within that height, with the level badge inside the portrait. Preserve native heart and soul-heart artwork and compact ammo count; compact health layout can wrap and scale within its assigned row area. Use measured display/canvas geometry so square/resized views honor the same contract. Match the established screen-HUD opacity without applying it twice.

Use the existing background-desktop Windows fixture and app-owned input, structured hierarchy snapshots and rendered bounds; no foreground window or OS input injection. Main agent executes directly. Temporary .NET/native work uses `D:/Temp/20mtd-corner-hud-20261030` for sustained small-file I/O. Retain acceptance diagnostics/screenshots/releases in `D:/Projects/20MTD-Modernization-evidence/corner-hud-20261030`; preserve existing shared assets/runtime bindings and live Steam/profile/configuration.

Acceptance: runs3/4 passed native fresh/Continue workflows at all three viewports, expanded hearts, pause/reload/level-up and ported portrait. Run6 verifies the only later production change: a stepped frame; portrait sampling and other widgets remain unchanged. No duplicate gameplay replay. C1-acceptance.json binds the inspected evidence to source. Closure review found no conflicting state owners or required refactoring: native bindings remain authoritative.
