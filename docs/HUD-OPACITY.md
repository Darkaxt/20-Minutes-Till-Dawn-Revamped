# Gameplay HUD opacity — 2026-10-30

Authoritative scope: all gameplay HUD elements should be 80–90% opaque so enemies and attacks beneath them remain visible. The user clarified that mobs themselves must retain their current appearance. Use 85% by default, and expand the existing native timer/Score setting into **HUD opacity**, selectable at 80%, 85% and 90%. This supersedes the timer-only opacity scope in POI-PULSE-CORRECTIONS P02. Preserve opaque menus and reward windows, existing visibility ownership, gameplay, progress and other settings. Existing authorization includes a normal release through the established updater channel; no phone or live Steam deployment.

| Requirement | Acceptance | Stage |
|---|---|---|
| H01 | Health, ammo (including cursor ammo), XP, timer, Score, objective, control hints, left metrics, right skills, POI icons/arrows/distances, gameplay notifications and the Custom badge inherit the same HUD opacity once, without cumulatively multiplying it. Newly created elements inherit it. | H1 |
| H02 | Native controller/pointer Options shows HUD opacity with 80/85/90%, default85%. The new preference persists. Existing timer-only choices do not make the rest of the HUD unexpectedly faint. Menus stay opaque; mobs, projectiles, combat effects and vision retain their existing presentation. | H1 |
| H03 | Compile and verify the complete native setting/HUD flow, inherited alpha and underlying enemy visibility at square/16:9 sizes. Reuse unchanged gameplay/indicator evidence. Update README/changelog and deliver a verified normal package/release, independent download, preservation/rollback and exact scratch cleanup. | H1 / H2 |

| Stage | Status | Satisfied | Remaining / blockers | Deferrals |
|---|---|---|---|---|
| H1 Unified gameplay HUD opacity | COMPLETE | H01–H02 and H03 documentation verified | None | None |
| H2 Final delivery | ACTIVE | H1 reconciled | H03 package/public verification and cleanup | None |

The existing native HUD and separate stats/skills/POI canvas require separate opacity roots. Notifications and the Custom badge retain their own lifetimes. Use their existing hierarchy owners, keeping menus outside these groups. Main-agent execution only. Temporary .NET/native work uses `D:/Temp/20mtd-hud-opacity-20261030` for sustained small-file I/O; retained acceptance diagnostics/releases use `D:/Projects/20MTD-Modernization-evidence/hud-opacity-20261030`.

## Focused acceptance

Native1 verifies controller/pointer cycling and saved reload. Its later native-group collision identifies the existing HUDPanel CanvasGroup; separate wrappers now preserve native visibility owners. Native6 verifies 80/85/90% inherited renderer alpha at square and 16:9 sizes, including cursor image/ammo, a shared factor above naturally hidden keyboard/controller hints, actual new skill/POI icons, notification/Custom badge owners, enemies beneath the metrics with unchanged appearance, and opaque pause/resume. Earlier unchanged menu criteria are reused; native1 is not claimed as a whole-run pass. H1-acceptance.json binds source and evidence. No unrelated gameplay replay.
