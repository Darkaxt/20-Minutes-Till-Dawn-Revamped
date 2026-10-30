# Weapon familiarity — 2026-10-30

Authorization: already authorized. Use the existing weapon crown, including Challenge clears (explicit user choice); preserve spread and integer counts.

Current correction, authorized2026-10-31: familiarity must belong to each **character–weapon pair**. The user explicitly requests immediate correction of existing Continue saves and recovery of all legacy weapon-familiarity history onto **Shana**, the default character. The previous shared-crown behavior recorded in the1.3.8 delivery below is superseded. Keep native global crowns as historical achievement UI; copy their handling ranks into Shana's pair records once, preserving higher/existing records and all other progress. Persist a recovery marker atomically with those ranks so later global crowns never leak into Shana or other characters. New runs/previews always use the selected pair. Old checkpoints recompute from that pair immediately; new checkpoints carry an explicit pair-scope marker and retain that run's starting rank.

| Requirement | Required behavior | Stage |
|---|---|---|
| W01 | Each character–weapon pair gains +2% reload speed, fire rate and projectile speed per highest qualifying victory tier; level0/unwon gives0%, level15 gives30%. Challenge clears still count. Repeat/lower wins do not stack or reduce ranks; victories on other characters or weapons do not leak. Recover old global weapon ranks to Shana once, preserve original crowns/other progress and existing pair records, and persist pairs/recovery marker through the existing journal. | W1 |
| W02 | Apply through native stat modifiers once per battle. Preserve damage, magazine/projectile counts, Pierce/Bounce, spread, size and knockback. Do not mutate shared weapon data. All weapons/evolutions and Custom inherit the same rule. | W1 |
| W03 | Native weapon descriptions and the existing base/bonus HUD reflect the selected character's weapon familiarity. Retry/new runs and Custom startup read the selected pair; Custom/death do not earn ranks. Correct old checkpoints immediately on resume by recomputing the pair bonus. New checkpoints with an explicit pair marker keep their own starting rank without duplication. No saved run grants pair progress merely by loading. | W1 |
| W04 | Native background Windows validation proves rank0/5/15, independent weapon records, native modifiers/evolution, controller/pointer preview, current and old Continue, and unchanged weapon identity attributes. No live Steam/profile/phone changes. | W1 |
| W05 | Update README/changelog; commit, publish and independently verify a normal release through the established updater0.2/schema2/epoch1 contract; clean exact task scratch. | W2 |

The three bonuses are native additive percentage stat bonuses, alongside existing character/rune/skill modifiers. Reload duration uses the native inverse rate calculation: e.g. +30% reload speed means duration divided by1.3, not duration minus30%.

## Reconciliation

| Stage | Status | Acceptance | Remaining / blockers | Deferrals |
|---|---|---|---|---|
| W1 Complete familiarity feature | COMPLETE | Current native3/4 verify one-time recovery for all12 weapons, isolated victories, Challenge/Custom/loss handling, descriptions/HUD, persistence and immediate old-Continue correction | None | None |
| W2 Final delivery | COMPLETE | W05 current release independently verified | None | None |

Current native/build scratch: `D:/Temp/20mtd-pair-familiarity-20261031`, for sustained small-file I/O. Retain acceptance/release evidence in `D:/Projects/20MTD-Modernization-evidence/pair-familiarity-20261031`. Preserve live Steam/profile/configuration and phone; no reset or live deployment.

Current acceptance: native3 passes recovery of each of12 legacy weapon ranks to Shana, original crown preservation, recovery-marker reload/no later leakage, separate Shana/Diamond and Revolver/Shotgun records, native standard wins/repeat nonstacking, controlled Challenge victory, Custom non-earning, actual native modifiers/identity attributes, description and visible HUD, scoped checkpoint rank8 while profile advances to15. Native4 reloads those records in a fresh process, preserves the new scoped run's rank8, uses15 on the next Shana run and2 on Diamond, rejects higher-Darkness loss progression, immediately replaces a legacy-shaped Diamond rank9 with2, writes the scope marker, and handles an older absent rank. The synthetic legacy wire shape is stated explicitly; no natural playthrough or byte-identical historical binary claim. Earlier native1/2 are diagnostics before the user-selected Shana recovery/immediate migration revision and do not qualify the final source. The unchanged three-stat modifier loop, weapon identity/evolution integration and native controller/pointer binding reuse the bounded earlier evidence below; no unrelated combat replay. Closure review found one journal owner, one battle bonus owner, atomic one-time recovery and no required refactoring or deferral.

W1 evidence: `W1-acceptance.json`; normal-render native9 (all twelve weapons),5 (ranks, unchanged attributes, real controller/pointer preview, wins, Retry, evolution and Custom),6 (byte-bound current Continue),7 (byte-bound public1.3.7 Continue) and8 (visible metrics and beneficial reload color). The same production DLL is independently hash-checked in each run. Early runs1/4 were cancelled through the supported request after test-only menu navigation errors;2/3 exposed a test-only rune-selection precondition. These are retained diagnostics, not passing acceptance runs. No production workaround was added. Closure review found no required refactor or unresolved blocker.

Disposable .NET/native work: `D:/Temp/20mtd-weapon-familiarity-20261030` (small-file/build I/O exception). Cleanup transaction `f4192150c08b62d81ad051da93d91f66`. Required evidence and releases: `D:/Projects/20MTD-Modernization-evidence/weapon-familiarity-20261030`. Shared source assets and live installations are preserved.

W2 qualification history: the first exact package (`81bf573`) passed its twelve-weapon workflow but rejected a pointer assertion before the queued synthetic mouse state had changed. Diagnostic11–13 record actual device/action state; these PROBE runs are not acceptance passes. The helper now observes mouse position/press/release consumption, and familiarity fixtures mute physical events only inside Unity without an OS/backend disable command. Source14 passes the complete lifecycle with the corrected fixture. Its production DLL is byte-identical to the first candidate (`53bb8841cce618e26f17e238c5544d3ff9a79567f05759ea772162a548cb093a`); no game workaround was added. That failed copied installation was rolled back (614 owned files/142 intact stock files) and its two exact generated output roots were removed through reviewed transaction `d83f66eb4dfd28c2be207a95ce2a3634`. Final committed-package qualification remains a W2 advancement blocker until the corrected workflow passes. The separate unconfirmed held-fire Retry observation is recorded in ANOMALIES.md and is not represented as fixed here.

## Final delivery

Normal [v1.3.8](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.3.8) is published from `885aefd85fd049c8e747576e1d7650cc7bcf814d`. Independently retrieved ZIP SHA-256: `28070aa60ecb908af46dc4797f4cb966b077557d049b5fdd42e8786b7a340728`. All 85 public members verify. Only the production DLL changes among614 update targets;593 prepared assets and613 other targets stay unchanged. Updater0.2.0/schema2/save-epoch1 remain compatible without a reset.

Exact packaged normal-render S6/run-3, run-4, run-5, run-6 pass all twelve weapons, ranks0/5/15, unchanged identity attributes, native controller/pointer preview, real victories/nonstacking, Retry, evolution/native stat composition, Custom startup, visible beneficial metrics and byte-bound current/public1.3.7 Continue. Copied install/rollback verifies614 owned targets and142 stock files. Live Steam/profile/configuration and phone remain unchanged.

Reviewed cleanup transaction `f4192150c08b62d81ad051da93d91f66` is applied: {'deleted': 1461}; 318185944 logical bytes removed. The exact disposable root is absent. Required evidence/packages remain at `D:/Projects/20MTD-Modernization-evidence/weapon-familiarity-20261030`.

W01–W05 are satisfied; no blocker or tracked deferral. Closure review found no additional required refactor. Final acceptance and remote documentation readback are bound by `W2-acceptance.json`.

## Character–weapon recovery delivery — 2026-10-31

Normal [v1.3.17](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.3.17) is published from `6e04bd8c7960f5f1bf75aab7c9119bc04ec497c6`. Independent ZIP SHA256 `aba392403233676b716532a2c33845780474342a3a4c81c8725319de72fdc7ce`; all 101 public members and 614 targets verify. Copied install/rollback preserves 142 originals. Only the DLL changes; prepared assets and 613 other targets remain unchanged. Schema 2/updater 0.2.0/save epoch 1 are preserved, with no reset. Live Steam/profile/configuration and phone untouched. Exact reviewed cleanup `e50301959466e16913c298db345faa78` removed 318,373,753 logical bytes; scratch is absent.

Current W01–W05 satisfied; no blockers or tracked deferrals. Final ownership review reuses the unchanged W1 assessment.
