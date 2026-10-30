# Specialize triple-upgrade regression — 2026-10-30

Authorization: already authorized by the reported regression and standing implementation/release instructions.

| Requirement | Acceptance | Stage |
|---|---|---|
| T01 | Reproduce the chest reward followed by lances and scythe through native acquisition; identify why the promised triple applications fail or are lost. | T1 |
| T02 | Correct the shared cause for affected upgrades. Create 3 dragon eggs, 3 mirrors, 3 ghosts, 3 scythes and 6 lances. Audit other direct summon-producing cards under the same rule. Stat upgrades receive their appropriate three applications without multiplying both summon count and individual damage. Preserve native intentional one-use/idempotent behavior and unrelated balance. | T1 |
| T03 | Use the existing native screenshot and successful actor/application checks, as explicitly directed by the user. Review that the correction leaves the ordinary repeat, Split Fire and Continue recording/replay paths unchanged; do not rerun gameplay simulations. | T1 |
| T04 | Update README/anomaly ledger, commit and deliver a verified normal release through the existing updater. Preserve live Steam/profile/phone; no save reset unless a demonstrated requirement makes one unavoidable. | T2 |

| Stage | Status | Remaining acceptance | Blockers | Deferrals |
|---|---|---|---|---|
| T1 Complete Specialize correction | COMPLETE | None; T01–T03 verified to the requested boundary | None | None |
| T2 Final delivery | ACTIVE | T04 | Package, publication/readback and cleanup | None |

Use the established isolated native Windows player. Disposable small-file/native build work: `D:/Temp/20mtd-specialize-20261030`; cleanup transaction `7bfa3b63f0dcbaae596955acdfb39366`. Required evidence/releases: `D:/Projects/20MTD-Modernization-evidence/specialize-20261030`. Keep validation limited to the affected upgrade flow and save boundary; no unrelated roster or gameplay revalidation.

## Evidence and scope correction

Native run1 reproduces the missing summons. Controlled run2 confirms that using the pristine definition fixes the problem. Run4 verifies all six direct summon roots (including Electro Bug), the requested 6/3/3/3/3 actor counts, three journal entries each and stat-only scaling. Its screenshot shows the real actors. The overall run failed later on an unrelated test prerequisite. Run5 repeated those successful checks but its extra rear-shot observer was gated to another mode. Run6 was cancelled through the supported cancellation signal at the user's instruction; no Continue simulation was launched. Failed/cancelled whole runs are not recorded as PASS.

The user's 2026-10-30 instruction to use the existing screenshot supersedes the earlier proposed repeat/Split Fire/Continue simulation sweep. Gameplay acceptance uses the already-passed relevant assertions and image, plus source readback. The correction changes only the two extra Specialize Equip inputs, resolving them from the pristine registry. It leaves repeat application, recorder/replay, serialization, the rebuilding guard and the explicit Split Fire branch unchanged. No additional summon-damage multiplier is introduced. The retained regression test ends after the covered actor/stat checks; the unnecessary extended simulation was removed.

Delivery will reuse that verified gameplay evidence. Release source comparisons permit only version metadata changes after qualification; packaging/install/rollback/public-download checks do not launch the game. Save behavior is unchanged, but this task makes no new runtime Continue verification claim.
