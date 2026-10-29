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
| W2 Final delivery | ACTIVE | W05 | Versioned package, exact-package verification, public release/readback and cleanup | None |

W1 evidence: `W1-acceptance.json`; normal-render native9 (all twelve weapons),5 (ranks, unchanged attributes, real controller/pointer preview, wins, Retry, evolution and Custom),6 (byte-bound current Continue),7 (byte-bound public1.3.7 Continue) and8 (visible metrics and beneficial reload color). The same production DLL is independently hash-checked in each run. Early runs1/4 were cancelled through the supported request after test-only menu navigation errors;2/3 exposed a test-only rune-selection precondition. These are retained diagnostics, not passing acceptance runs. No production workaround was added. Closure review found no required refactor or unresolved blocker.

Disposable .NET/native work: `D:/Temp/20mtd-weapon-familiarity-20261030` (small-file/build I/O exception). Cleanup transaction `f4192150c08b62d81ad051da93d91f66`. Required evidence and releases: `D:/Projects/20MTD-Modernization-evidence/weapon-familiarity-20261030`. Shared source assets and live installations are preserved.
