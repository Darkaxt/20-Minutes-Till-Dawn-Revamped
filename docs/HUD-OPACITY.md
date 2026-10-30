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
| H2 Final delivery | COMPLETE | H03 delivered and independently verified | None | None |

The existing native HUD and separate stats/skills/POI canvas require separate opacity roots. Notifications and the Custom badge retain their own lifetimes. Use their existing hierarchy owners, keeping menus outside these groups. Main-agent execution only. Temporary .NET/native work uses `D:/Temp/20mtd-hud-opacity-20261030` for sustained small-file I/O; retained acceptance diagnostics/releases use `D:/Projects/20MTD-Modernization-evidence/hud-opacity-20261030`.

## Focused acceptance

Native1 verifies controller/pointer cycling and saved reload. Its later native-group collision identifies the existing HUDPanel CanvasGroup; separate wrappers now preserve native visibility owners. Native6 verifies 80/85/90% inherited renderer alpha at square and 16:9 sizes, including cursor image/ammo, a shared factor above naturally hidden keyboard/controller hints, actual new skill/POI icons, notification/Custom badge owners, enemies beneath the metrics with unchanged appearance, and opaque pause/resume. Earlier unchanged menu criteria are reused; native1 is not claimed as a whole-run pass. H1-acceptance.json binds source and evidence. No unrelated gameplay replay.

## Final delivery

Normal [v1.3.12](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.3.12) is published from `486789f27f8f979880ef7cda452a6aceabde2b4b`. Independent ZIP SHA-256: `7555220bd54c551adf4973c7b2a4bc2aac3d89aa510c0d1fda047cf3812d32fa`. All 94 public members, 614 update targets, copied installation/rollback and 142 original files verify. Only the DLL changes; 593 prepared assets and 613 other targets remain unchanged. Schema 2, updater 0.2.0 and save epoch 1 remain compatible. Live Steam, profile, configuration and phone are untouched. Reviewed cleanup `8b282e0851c1b1d5f6ce132c13f63407` removed 317,716,930 logical bytes; task scratch is absent. Retained diagnostics and release receipts preserve acceptance evidence.

H01–H03 satisfied; no blocker or tracked deferral.
