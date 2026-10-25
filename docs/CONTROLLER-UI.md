# Controller UI audit — 2026-10-25

The user requested a read-only agent audit and asked that additional findings be fixed later. These entries distinguish source findings, existing runtime evidence and checks still required. This is not a claim that every UI section passed a fresh controller test.

## Current requested correction

| Section | Finding | Required correction and acceptance |
|---|---|---|
| Campaign/mode selection | Confirmed in the isolated Windows player (`evidence/S1/run-31`). Starting through the actual controller input module, the original three modes have navigation routes; Challenge and Custom have none. They clone original toggles without repairing incoming links (`src/ChallengeMode.cs`, `src/CustomMode.cs`). | Real input-module D-pad selection of all five modes, native descriptions/difficulty, Play/Back and re-entry, preserving locks. |
| Metrics and sprite settings | Confirmed native-menu integration gap. `EnhancementSettingsCanvas`/buttons are outside native Options entries (`src/StatsOverlay.cs:123`, `src/ColorfulStyle.cs:94`); run-31 confirms the separate ownership. Native Automatic navigation can point outside the Options hierarchy, so lack of registration alone does not prove both buttons are completely unreachable. Earlier tests invoked direct clicks. | Integrate both as native Options rows in title and pause; controller/pointer activation, labels/live effects, persistence and viewport fit. |

## Additional findings reserved for later fixes

| ID | Section | Evidence and confidence | Next bounded check |
|---|---|---|---|
| UI-01 | Title → Achievements | Cleared for the checked path. The worker flagged a static candidate because the appended title entry does not repair links (`src/SourceAchievementMenu.cs:33`). Actual Windows controller input reaches it, opens the panel, advances to page 2 and cancels back (`evidence/S1/run-32`). Both enabled panel buttons have incoming routes. | No repair established. Eligible reward claims and every page were not exercised; no claim was granted for this audit. |
| UI-02 | Custom numeric fields | Confirmed controller focus trap in `evidence/S1/run-33`. D-pad traversal reaches `CustomValue1` and immediately starts editing. Right leaves focus there; Submit followed by Down also leaves it there. B closes the entire Custom panel and returns to Start. `TMP_InputField` controls sit between Minus/Plus in the traversal ring (`src/CustomMenu.cs:137`, `:152`), while the global Cancel handler closes the panel (`:39`). Run-32's initial graph-based probe failed while traversing these editing controls; run-33 observes each real input transition directly. | Implement a deliberate controller edit/exit policy, then verify reaching Plus/next row, changing values and leaving an edit without inadvertently leaving Custom. No repair made in this audit. |
| UI-03 | Failed Continue → Cancel | Definite source omission. `ResumeUI.RestoreFailed` builds its error dialog without a Cancel binding (`src/ResumeUI.cs:39`); ordinary Continue binds `UI/Cancel`. Focused Back has a Submit action, so total inability to dismiss is not established. | Trigger one controlled rejection in an isolated save fixture; test B dismissal and A on Back independently. |

## Existing evidence and limits

- Characters/weapons: prior actual controller/pointer input-module checks cover15 characters/12 weapons, locked guards and remembered selections in v1.1.3 (`evidence/S6/run-18`). This is copied-player evidence, not a fresh test of the user's current phone session.
- Runes: native selectable controls remain. Existing tests submit through keyboard after selecting a rune; traversing both trees with a controller is a coverage gap rather than a demonstrated defect.
- Ordinary Continue: initial focus, explicit horizontal links and Cancel are present (`src/ResumeUI.cs:36`, `:58`).
- Pause/Custom entry: explicit vertical links exist (`src/ResumeUI.cs:124`, `src/CustomMenu.cs:162`). Check reachable active controls when Save & return is unavailable before claiming a fault.

The worker audit was read-only. The main agent independently inspected the cited source and reproduced the campaign/navigation, Options ownership and Custom numeric-field findings in a windowless copied Windows player. Achievements passed the bounded path above. Synthetic gamepad events entered Unity's actual input module; no desktop input was injected. To inspect Custom downstream of its broken entry route, only the copied player's Custom toggle was selected programmatically; this is not evidence that the normal Custom entry works. The installed Windows game, phone and user progress were not changed. This audit does not deliver the pending UI fixes.
