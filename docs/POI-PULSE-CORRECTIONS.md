# Marker tracking and Pulsing Summons — 2026-10-30

Authoritative scope: correct the reported scythe/lance Pulsing Summons issue; remove all HUD avoidance from chest indicators; retain each indicator as its chest enters view and move it to the chest's actual screen location. Add a native Timer / score opacity setting. Existing user authorization covers implementation, verification, commit and normal public release. No live Steam/phone deployment or save reset.

| ID | Required behavior and acceptance | Stage |
|---|---|---|
| P01 | Indicator position follows the target's projected ray at the screen perimeter and follows the target inward when on screen. No HUD reservation or other marker relocates it. North-axis motion is continuous through zero, independent of timer/score/metrics/skills. Keep content icon, distance, safe area, nearest-three exploration selection and boss identity. Remove indicators on collection/lifecycle termination, not on entering view. | P1 |
| P02 | Native controller/pointer Options includes Timer / score opacity, default100%,25/50/75/100%. Persist the setting and apply it to the existing timer/score only, preserving menu-owned visibility and all other HUD/gameplay. | P1 |
| P03 | Establish whether actual scythe and lance pulse ticks damage nearby registered enemies, with acquisition both before and after Pulsing. Correct the demonstrated root cause; supply readable pulse feedback consistent with the native art and effects-opacity setting. Preserve source50 damage/2 seconds/radius3 unless evidence requires a different correction. Verify the complete affected flow and Continue reconstruction. | P1 |
| P04 | Update README/changelog/anomaly records, verify focused native behavior and current package install/rollback/preservation, commit and publish a normal release through the existing channel. Independently retrieve/verify the published package and clean exact disposable scratch through the installed helper. | P1 / P2 |

| Stage | Status | Satisfied | Remaining / blockers | Deferrals |
|---|---|---|---|---|
| P1 Integrated corrections | COMPLETE | P01–P03 and P04 documentation verified | None | None |
| P2 Final delivery | COMPLETE | P04 delivered and independently verified | None | None |

P01 explicitly supersedes the UI-avoidance/world-visible-handoff portions of POI-HONEYCOMB C02 and its visual contract. Existing icon/font/size/color rules remain. Timer/score opacity does not affect the objective, health, ammo or XP bar. Main agent executes directly. New .NET/native scratch uses `D:/Temp/20mtd-poi-pulse-20261030` for sustained small-file I/O; retained verification/release assets use `D:/Projects/20MTD-Modernization-evidence/poi-pulse-20261030`.

## Focused acceptance

Native1 verifies actual scythe and both lance damage ticks after acquiring Pulsing. Native2 verifies controller/pointer opacity cycling and real configuration reload. Native7/8 record actual timed lance/scythe damage, feedback and effects alpha after fresh-process Continue. Pulsing before/after lance acquisition and its Scythe prerequisite are exercised through actual acquisition replay. Native9 measures the internal1244×700 render viewport versus the final1280×720 display, establishing the alignment root cause. Native10 verifies normalized-display tracking on all four edges at1280×720/720×720, continuous north-axis motion, persistent on-screen icons, lifecycle hiding and timer/Score-only alpha. Passed unchanged criteria are reused; diagnostic runs with later failures are not whole-run acceptance claims. Fixed target geometry/direct-attack isolation are explicit; no natural encounter/full-run claim. P1-acceptance.json binds current source and evidence. No unrelated parity/weapon replay.

## Final delivery

Normal [v1.3.11](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.3.11) is published from `024ce2221aae0655e419bf62d5c9d62d505e67af`. Independent ZIP SHA-256: `a65e3df9be81e4030060f867e27dbe7f954d78e6ec7a223e495e5df54ce23c9b`. All 92 public members, 614 update targets, copied installation/rollback and 142 original files verify. Only the DLL changes; 593 prepared assets and 613 other targets remain unchanged. Schema 2, updater 0.2.0 and save epoch 1 remain compatible. Live Steam, profile, configuration and phone are untouched. Reviewed cleanup `17970ff6f45ca9514f5da133fb880325` removed 317,676,407 logical bytes; the exact task scratch is absent. Retained native diagnostics and release receipts preserve the acceptance and root-cause audit.

P01–P04 satisfied; no blocker or tracked deferral.
