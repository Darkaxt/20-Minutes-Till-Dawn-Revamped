# Compact corner status HUD — 2026-10-30

Authoritative scope: implement the agreed portrait-and-three-row corner HUD inspired by the supplied screenshots. The user explicitly limits its bottom edge to the bottom of the Score text, then asks that removing the full-width level bar also move the top HUD upward. Existing authorization includes implementation, focused verification, commit and a normal release through the established updater channel. No live Steam/phone deployment or save reset.

Current amendment, authorized2026-10-30: move the level below the avatar as **Lvl. 37**, using75% of the ammunition font size. Remove the overlaid badge/background. C1/C2 below track this revision for1.3.15; the earlier1.3.13 acceptance/delivery at the end remains historical. Preserve the Score-height limit, portrait artwork and stepped frame; the portrait may scale down only when needed to fit the caption in a constrained viewport.

| ID | Required behavior and acceptance | Stage |
|---|---|---|
| C01 | Replace the full-width XP/level strip with a compact upper-left cluster: current character portrait in a thin stepped circular frame, **Lvl. N** centered below it at75% of the ammo font size, hearts/soul hearts on the first row, short thin XP bar on the second, native bullet icon/current-max ammunition on the third. Use the existing native live bindings. No enclosing panel or level-label background; no portrait overlap. | C1 |
| C02 | The entire cluster fits inside the reported safe area and ends at or above the actual Score text's bottom edge. Move the centered timer, Score and objective upward by24 native-canvas units to reclaim the former bar area; move the Custom badge into the freed strip above the timer without overlap. Keep the existing vertically centered metrics and acquired-skill rows, and the existing HUD opacity setting/default85%. | C1 |
| C03 | Handle ordinary and expanded health/soul hearts, different native/ported portrait shapes, live health/XP/ammunition updates, level-up, reload, pause/resume and Continue. Keep existing visibility/input owners; do not introduce another progression/stat counter. Preserve gameplay, settings, progress and POI direction logic. | C1 |
| C04 | Verify native render/layout at1280×720,720×720 and1920×1080, with measured bounds against Score ink and representative fresh/expanded-health/Continue states. Inspect actual screenshot evidence. Update README/changelog and publish a normal verified package with copied install/rollback, independent public download, preservation checks and exact scratch cleanup. | C1 / C2 |
| C05 | Give only the portrait's circular frame a stepped pixel-art contour. Preserve the approved native portrait rendering, composition and all other widgets. User clarified that the portrait itself already looks good. | C1 |

| Stage | Status | Satisfied | Remaining / blockers | Deferrals |
|---|---|---|---|---|
| C1 Integrated compact status HUD | COMPLETE | Revised caption/native37→38,75% font, expanded health and square/widescreen bounds/render verified; unchanged native bindings retain prior acceptance | None | None |
| C2 Final delivery | ACTIVE | Current native acceptance reconciled; earlier1.3.13 delivery retained below | Revised README screenshot,1.3.15 package/public verification and cleanup | None |

Visual contract: retain the game's pixel art, font and cream/coral/muted-green palette. Approximately260 reference pixels wide; height is constrained by the Score ink, not a fixed taller card. Portrait, caption and all three status rows remain within that height. The caption sits3 reference pixels below the circle, font13.5 versus ammunition18. Preserve native heart and soul-heart artwork and compact ammo count; compact health layout can wrap and scale within its assigned row area. Use measured display/canvas geometry so square/resized views honor the same contract. Match the established screen-HUD opacity without applying it twice.

Current revision native/build scratch: `D:/Temp/20mtd-level-caption-20261030` for sustained small-file I/O. Retained acceptance/release evidence: `D:/Projects/20MTD-Modernization-evidence/level-caption-20261030`. No live installation or phone mutation.

Current native run2 passes actual level37 selection and update to38, the below-avatar caption without an Image background, font-size ratio, centered alignment, inherited opacity, native health/ammunition and Score-height bounds at1280×720,720×720 and1920×1080, plus expanded hearts. Captures inspected. Run1 stopped because the generic AndroidPick fixture ignored its seed-level argument; run2 uses the existing native AchievementPick seed directly. Production layout was unchanged by that fixture correction. Closure review found no new state owner: the native number binding remains intact and the prefix is static presentation; no required refactoring or deferral.

Use the existing background-desktop Windows fixture and app-owned input, structured hierarchy snapshots and rendered bounds; no foreground window or OS input injection. Main agent executes directly. Temporary .NET/native work uses `D:/Temp/20mtd-corner-hud-20261030` for sustained small-file I/O. Retain acceptance diagnostics/screenshots/releases in `D:/Projects/20MTD-Modernization-evidence/corner-hud-20261030`; preserve existing shared assets/runtime bindings and live Steam/profile/configuration.

Acceptance: runs3/4 passed native fresh/Continue workflows at all three viewports, expanded hearts, pause/reload/level-up and ported portrait. Run6 verifies the only later production change: a stepped frame; portrait sampling and other widgets remain unchanged. No duplicate gameplay replay. C1-acceptance.json binds the inspected evidence to source. Closure review found no conflicting state owners or required refactoring: native bindings remain authoritative.

## Final delivery

Normal [v1.3.13](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.3.13) is published from `7ec89666db6c966ae239bdf15c2580812703782b`. Independent ZIP SHA-256: `d989509bb5839a8839d1bb2d5bbeb7636411ce5f1bd9968dab4779684452f48f`. All 96 public members, 614 targets, copied install/rollback and 142 original files verify. Only the DLL changes; 593 prepared assets/613 other targets remain unchanged. Schema2, updater0.2.0 and save epoch1 are preserved. Live Steam/profile/configuration and phone are untouched. Reviewed cleanup `de46c0823fc63e0bb0a512c71f03a70b` removed 318,112,542 logical bytes; task scratch is absent. Acceptance evidence and final release remain retained.

C01–C05 satisfied; no blockers or tracked deferrals. Final closure review reuses the unchanged C1 state-ownership assessment.
