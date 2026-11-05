# Maintenance storage migration

Authorized by the user's 2026-11-05 instruction to move reusable inputs out of Temp, update their consumers and clean disposable project material. This is a local maintenance-storage migration; the installed game, saves, settings and published 1.3.17 game package are preservation targets.

| Requirement | Acceptance | Stage |
|---|---|---|
| M01 | Inventory the six existing `D:/Temp/20mtd-*` roots, identify reusable/unique inputs and current consumers, and preserve required source, evidence and recovery data. | M1 |
| M02 | Retain only required maintenance inputs under `D:/Projects/20MTD-Modernization-maintenance`; verify copied data and make the retained Python runtime usable at its new path. | M1 |
| M03 | Update current project build, validation and release helper consumers to the permanent paths; retain historical evidence as historical evidence. Preserve the canonical checkout and other projects. | M1 |
| M04 | Prove the complete migrated maintenance workflow with a fresh isolated build/package, the existing asset/updater contracts and copied install/rollback. Native behavior is unchanged and does not require replaying gameplay simulations. | M1 |
| M05 | After current-consumer and retention verification, use reviewed transactional cleanup to remove exact disposable members. Verify residuals, preserved live state and the delivered permanent workspace; commit/sync applicable source and documentation changes. | M2 |

| Stage | Status | Satisfied | Remaining / blockers | Deferrals |
|---|---|---|---|---|
| M1 Migrate and verify maintenance inputs | COMPLETE | M01–M04: exact inventory; hash-verified retained inputs; fresh pinned Python environment; shared helper updates; Release build/package; 614-target copied installation and exact rollback; live game/profile/public package preserved | None | None |
| M2 Retire disposable storage and deliver | ACTIVE | Reviewed cleanup applied with no exclusions/errors; six legacy roots and the new validation scratch are absent; permanent copies and unchanged live state verified | M05 commit/synchronization | None |

Retained private inventory/verification evidence: `D:/Projects/20MTD-Modernization-evidence/storage-migration-20261105`. Disposable new build/validation output will use an exact task root on D: only for sustained small-file/native I/O and will be cleaned when its last consumer exits. No unrelated repository migration or game deployment is authorized by this task.

## Permanent input layout

The maintained local root is `D:/Projects/20MTD-Modernization-maintenance`:

| Directory | Ongoing use |
|---|---|
| `prepared/Assets`, `prepared/BepInEx`, `prepared/downloads` | Complete 593-file asset contract, pinned loader and original pinned dependency downloads used by packaging and copied installation. |
| `prepared/runtime` | Fresh Python environment launchers with hash-verified pinned site-packages. Invoke `Scripts/python.exe -B` and `-m pip`; old environment launchers are not copied. |
| `reference/android-analysis`, `reference/pylibs` | Original Android code/metadata, recovered declarations and analysis libraries used by the existing reconciliation helpers. |
| `reference/game` | Stock copied player with the existing isolated QA company/product identity and QA configuration. No live saves or obsolete installed mod payload. |
| `reference/bin/Modernization/netstandard2.0/TwentyMinutes.Modernization.dll` | Exact historical binary read by the old bug-correction evidence generator; it is not the current production plugin. |
| `reference/legacy-source` | Unique controller helper sources retained for recovery/reference. Candidate installer sources are compared against their Git revision and retained only when different. |

`tools/controller_menu_work.py`, `tools/selector_delivery.py` and the retained reconciliation helpers use these permanent paths. Their `DOTNET_CLI_HOME` belongs to each invocation's disposable build root. `maintenance/storage_migration.py` records the exact local helper replacements and verifies the permanent copies. The private receipt includes before/after source and hashes because `tools/` is a pre-existing ignored local directory; this migration does not publish private analysis tooling or proprietary inputs.

For subsequent maintenance, choose a new exact build scratch directory on D: for native/small-file I/O and an evidence directory under the retained project evidence root. Set `MODERNIZATION_WORK_SCRATCH`, `MODERNIZATION_WORK_EVIDENCE` and `MODERNIZATION_EVIDENCE_ROOT` explicitly when invoking the reusable helpers. Packaging accepts `--prepared-assets D:/Projects/20MTD-Modernization-maintenance/prepared/Assets`; validation uses `MODERNIZATION_PREPARED_ASSETS` with that same value. Clean the invocation's scratch through transactional cleanup after its last consumer exits.

Historical evidence scripts keep their original workflow and path records. They are not current entry points; re-running historical release/evidence generators is not part of this migration. The current shared helpers resolve their reusable inputs from permanent storage.

## Verification and cleanup result

The relocated Python passes dependency/pinned-version checks and loads the asset decoder, image library and ARM disassembler. A fresh isolated production/validation Release build and candidate package pass the existing 593-asset and 614-install-target contracts. Copied installation verifies every target; uninstall removes exactly its 614 owned files and leaves the copied stock player intact. No gameplay implementation changed, so existing native gameplay evidence was not replayed.

All 21 local helper sources containing reusable legacy input references were updated and their before/after contents and hashes retained privately. Python sources compile, the changed PowerShell helper parses, and the bounded process/scheduled-task/helper scan found no remaining active legacy consumers. One historical evidence script outside the shared helpers retains its original path as part of the immutable earlier workflow record.

The helper inspected all 9,491 signed manifest members over ten pages, then applied ticket `13c480aec3920208171c35ef4a2fd73acc0fef27ef008575e855eddcb54686df` for transaction `a127effcc71dbe47b3f4271d32f6b4e6`. It removed **1,263,455,959 logical bytes**, including this migration's disposable build/test output, with no exclusions, errors or residual paths. Permanent hash-verified copied inputs total **529,538,401 bytes**; freshly generated environment launchers and the input-store README are additional maintained files.

Final read-back confirms all six original roots and `D:/Temp/20mtd-storage-verification-20261105` are absent. Steam installation, real profile/configuration and the retained published v1.3.17 package/ZIP match their pre-migration hashes. No phone deployment or replacement gameplay release is part of this storage-only migration. Private acceptance receipts are `workflow-verification.json`, `consumer-verification.json`, `cleanup-inspected.json`, `cleanup-result.json` and `final-verification.json` under the retained evidence directory above.
