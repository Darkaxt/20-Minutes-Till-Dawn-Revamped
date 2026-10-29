# Weapon familiarity — 2026-10-30

Authorization: already authorized. Use the existing weapon crown, including Challenge clears (explicit user choice); preserve spread and integer counts.

| Requirement | Required behavior | Stage |
|---|---|---|
| W01 | Each weapon gains +2% reload speed, fire rate and projectile speed per highest recorded crown level; level0/unwon gives0%, level15 gives30%. Repeated wins do not stack. Characters share the weapon's existing record. | W1 |
| W02 | Apply through native stat modifiers once per battle. Preserve damage, magazine/projectile counts, Pierce/Bounce, spread, size and knockback. Do not mutate shared weapon data. All weapons/evolutions and Custom inherit the same rule. | W1 |
| W03 | Show the handling bonus in the native weapon description and existing base/bonus HUD. Continue retains the run's starting rank without duplication; older checkpoints adopt the current crown on first load. Retry/new runs read the current crown. | W1 |
| W04 | Native background Windows validation proves rank0/5/15, independent weapon records, native modifiers/evolution, controller/pointer preview, current and old Continue, and unchanged weapon identity attributes. No live Steam/profile/phone changes. | W1 |
| W05 | Update README/changelog; commit, publish and independently verify a normal release through the established updater0.2/schema2/epoch1 contract; clean exact task scratch. | W2 |

The three bonuses are native additive percentage stat bonuses, alongside existing character/rune/skill modifiers. Reload duration uses the native inverse rate calculation: e.g. +30% reload speed means duration divided by1.3, not duration minus30%.

## Reconciliation

| Stage | Status | Acceptance | Remaining / blockers | Deferrals |
|---|---|---|---|---|
| W1 Complete familiarity feature | COMPLETE | W01–W04 | None | None |
| W2 Final delivery | COMPLETE | W05 | None | None |

W1 evidence: `W1-acceptance.json`; normal-render native9 (all twelve weapons),5 (ranks, unchanged attributes, real controller/pointer preview, wins, Retry, evolution and Custom),6 (byte-bound current Continue),7 (byte-bound public1.3.7 Continue) and8 (visible metrics and beneficial reload color). The same production DLL is independently hash-checked in each run. Early runs1/4 were cancelled through the supported request after test-only menu navigation errors;2/3 exposed a test-only rune-selection precondition. These are retained diagnostics, not passing acceptance runs. No production workaround was added. Closure review found no required refactor or unresolved blocker.

Disposable .NET/native work: `D:/Temp/20mtd-weapon-familiarity-20261030` (small-file/build I/O exception). Cleanup transaction `f4192150c08b62d81ad051da93d91f66`. Required evidence and releases: `D:/Projects/20MTD-Modernization-evidence/weapon-familiarity-20261030`. Shared source assets and live installations are preserved.

W2 qualification history: the first exact package (`81bf573`) passed its twelve-weapon workflow but rejected a pointer assertion before the queued synthetic mouse state had changed. Diagnostic11–13 record actual device/action state; these PROBE runs are not acceptance passes. The helper now observes mouse position/press/release consumption, and familiarity fixtures mute physical events only inside Unity without an OS/backend disable command. Source14 passes the complete lifecycle with the corrected fixture. Its production DLL is byte-identical to the first candidate (`53bb8841cce618e26f17e238c5544d3ff9a79567f05759ea772162a548cb093a`); no game workaround was added. That failed copied installation was rolled back (614 owned files/142 intact stock files) and its two exact generated output roots were removed through reviewed transaction `d83f66eb4dfd28c2be207a95ce2a3634`. Final committed-package qualification remains a W2 advancement blocker until the corrected workflow passes. The separate unconfirmed held-fire Retry observation is recorded in ANOMALIES.md and is not represented as fixed here.

## Final delivery

Normal [v1.3.8](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.3.8) is published from `885aefd85fd049c8e747576e1d7650cc7bcf814d`. Independently retrieved ZIP SHA-256: `28070aa60ecb908af46dc4797f4cb966b077557d049b5fdd42e8786b7a340728`. All 85 public members verify. Only the production DLL changes among614 update targets;593 prepared assets and613 other targets stay unchanged. Updater0.2.0/schema2/save-epoch1 remain compatible without a reset.

Exact packaged normal-render S6/run-3, run-4, run-5, run-6 pass all twelve weapons, ranks0/5/15, unchanged identity attributes, native controller/pointer preview, real victories/nonstacking, Retry, evolution/native stat composition, Custom startup, visible beneficial metrics and byte-bound current/public1.3.7 Continue. Copied install/rollback verifies614 owned targets and142 stock files. Live Steam/profile/configuration and phone remain unchanged.

Reviewed cleanup transaction `f4192150c08b62d81ad051da93d91f66` is applied: {'deleted': 1461}; 318185944 logical bytes removed. The exact disposable root is absent. Required evidence/packages remain at `D:/Projects/20MTD-Modernization-evidence/weapon-familiarity-20261030`.

W01–W05 are satisfied; no blocker or tracked deferral. Closure review found no additional required refactor. Final acceptance and remote documentation readback are bound by `W2-acceptance.json`.
