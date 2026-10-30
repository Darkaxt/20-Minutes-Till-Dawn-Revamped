# Controller UI corrections

## v1.1.9 — Menu ownership and results

Victory and death omit the skill list and keep all directional navigation on Retry/Quit. Native shown menus own focus through presentation, hide/disable, input-scheme changes and actual controller navigation/submit/cancel events. Valid pointer/controller selection is preserved; there is no per-frame focus override. The roster's grid and shared Runes/Back panel form one composed menu even though they are sibling hierarchy roots. Custom, achievement and Continue dialogs register their own existing controls.

Native173/174/175 verifies the full title/pause/Options, roster, rune, campaign, Custom, reward and Continue-error flows, plus actual Forest victory/Retry/death/Quit and first-event focus recovery after switching from the pointer. Earlier171/172 exposed incorrect ownership boundaries for the shared roster panel; those failed probes are retained rather than counted as acceptance. Final package evidence is recorded in [PROGRESS.md](PROGRESS.md).

## Historical v1.1.4 audit

Current v1.1.5 also adds native Zoom out, fullscreen Resolution access, three metrics modes, viewport-correct skill icons, centered timer/run credits and on-demand locked-entry progress. [Current changelog](../README.md#v115--display-readable-unlocks-and-isaac-style-hud) · [Current acceptance and delivery](PROGRESS.md). The v1.1.4 audit below is retained as history; its binary On/Off metrics behavior is superseded by Off/Little/Extended.

All confirmed findings from the controller audit are implemented and verified. The worker's source audit was followed by native Windows reproductions and corrections; it was not a request to leave repairs pending. [Original audit and evidence classifications](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/blob/e94464ffffac911447fd448c283cc223781c85cc/docs/CONTROLLER-UI.md).

| Section | Correction | Verification |
|---|---|---|
| Campaign selection | Link all five modes in order, with native difficulty/Play/Back routes. Challenge and Custom retain their ownership rules. | Actual D-pad/Submit selects every mode and updates descriptions. Unearned Challenge rejects Play without spending credits. Earned tier Right/Left works and cannot exceed its limit. Back, re-entry and controller Custom start pass. |
| Title and pause Options | Replace the floating enhancement canvas with two native rows, preserving the original font, feedback, indices and option callbacks. An opaque, centered360×350 native-unit panel covers the previous menu/title artwork. Explicit navigation stays inside Options and refreshes with available settings/input scheme. | Controller and pointer toggle HUD/artwork exactly once; original Outline is unchanged. Both contexts pass repeated re-entry and Cancel from an added row. Hit rectangles/rendered glyphs fit720p/1080p. Both settings persist into a separate process and control the live HUD/artwork. |
| Custom numeric fields | Controller traversal uses Minus/Plus and skips direct text-edit focus. Pointer/keyboard retain direct entry. Cancel leaves a text edit for Plus; another Cancel returns to mode selection. | All seven tabs and their pages are browsed with controller input; Plus/Minus change values. Real pointer focus, legacy TMP key-event processing and pointer commit preserve direct typing. Ownership/credits remain unchanged. |
| Failed Continue dialog | Bind native Cancel to the same return-to-title operation as Back, with lifecycle unsubscription. | Controlled validation-only restore failures pass through the real restoration error handler. B/Cancel and A on Back both return to title; checkpoint bytes remain identical. Ordinary Continue Cancel also passes. |

## Other audit findings

- **Achievements:** the tentative missing-link concern was cleared. Controller entry, page navigation and Cancel work. No repair was required; reward claims were not granted for the audit.
- **Runes:** controller traversal reaches all24 native rune slots and Confirm returns to character selection, closing the earlier coverage gap without changing rune behavior.
- **Characters/weapons:** the full15-character/12-weapon controller/pointer regression passes, including initial/re-entry previews, locked guards and obtained confirmation.

## Evidence and boundaries

`evidence/S1/run-44` and `run-45` pass the complete affected workflows at1280×720 and1920×1080. `run-46` passes fresh-process setting persistence/re-entry; `run-48` passes the roster regression. These runs use the same production DLL. Final package/install/rollback/release receipts are recorded in [delivery history](PROGRESS.md).

These are native Unity input-module checks in a windowless copied Windows player with an isolated profile. Synthetic gamepad/pointer events do not inject desktop input. The shipped legacy TMP field receives key events through its public processor after real pointer focus; no OS keyboard/IME claim is made. Restore errors are injected only by the separate validation assembly. No validation assembly is delivered in the mod.

Implementation-stage failures are retained:34–36 exposed early-created hidden controls retaining interaction;37–38 exposed a geometry check counting native labels' empty100-unit layout boxes rather than hit boxes/glyphs;39–42 corrected fixture navigation/readiness and legacy typing/Plus expectations;43 exposed automatic navigation escaping to the gameplay HUD slider, corrected with explicit Options links. Probe47 was cancelled through the owned fixture interface because a retained checkpoint opened Continue before the roster;48 supplies that fixture precondition explicitly. Failed/cancelled probes are not acceptance results.

No confirmed controller finding remains pending. These bounded checks do not claim every possible game-state/input combination is bug-free.

## Visible focus — 1.3.10

[Authoritative scope](POI-HONEYCOMB.md). A native-parented, non-interactive cream chevron follows the EventSystem selection. Text labels use a left gap; icon-only controls use an inside-left gutter. It inherits the actual menu scale and lifetime, rather than converting world-camera UI units into overlay pixels. Native input/focus ownership is unchanged.

Two independent opacity rows use the same native button, callback, focus and explicit navigation owners as the other settings. They each cycle25/50/75/100%, default100, and persist in the existing config. Bullet-like bodies use the projectile channel; player ability/summon/explosion/trail/particle visuals use the effects channel. Native8 verifies controller/pointer, GPU output, saved reload and ownership exclusions.
