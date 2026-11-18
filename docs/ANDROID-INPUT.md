# Single Android input migration

Authorization: migrate the installer requirement to Android8.7/1105 only. Evaluate the remaining8.7 gameplay additions later; this migration must preserve Revamped1.4.0's gameplay, cosmetic collection and compatible saves.

Requirements:

- A01: Prepare gameplay resources and cosmetics from the exact supported8.7/1105 APKM, using one metadata decode. No8.6 input or previous prepared cache is needed for a fresh installation.
- A02: Resolve the existing whitelisted character, powerup, audio, achievement, camera and animation resources against8.7 identities/layouts. Preserve existing gameplay semantics. Record actual payload differences; do not import new character/event behavior or rebalance as part of this migration.
- A03: Verify a complete fresh local preparation, all exported references and relevant payload equivalence against the retained1.4.0 preparation; verify actual packaged installation and a background native fresh/Continue workflow with the new assets. Preserve original game files, user saves and settings. No live deployment.
- A04: Publish one normal1.4.1 release from verified source, with package/input hashes, updated one-input instructions/README/updater guidance and independent public-download checks. Proprietary assets remain locally prepared. Clean exact task scratch after consumers finish.

| Stage | Status | Acceptance |
|---|---|---|
| I1 Integrated single-input migration | COMPLETE | A01/A02: fresh one-input preparation and payload equivalence pass. A03: packaged install verifies1,738 files, preserves142 originals/configuration and is idempotent; native fresh play and an actual1.4.0 checkpoint Continue/evolution pass using the new assets. |
| I2 Release and delivery | COMPLETE | A04: normal1.4.1 release from9a993d7, independent public ZIP/source/hash/assembly checks and exact reviewed cleanup pass. |

The single-input installer,8.7 whitelist, shared decoder, complete exporters, versioned package and current instructions are integrated and verified. Fresh preparation exports1,717 files from8.7 alone; comparison against1.4.0 finds no gameplay or render differences after normalizing package-local character sprite IDs/filenames and two audio provenance IDs. All runtime-consumed character fields, pixels, animation timings, geometry, other JSON/text/binary assets and the entire cosmetic catalog match. No remaining blockers or tracked deferrals. Saint Rosa and other8.7 gameplay additions are explicitly outside this migration.

Evidence: input87-20261118/prepare-command.json, prepare.log, payload-equivalence.json and installer-acceptance.json. Native S1/run-1 proves fresh gameplay, character/gun/wing art, Original/Reborn coexistence and checkpoint capture. S1/run-2 restores the actual1.4.0 checkpoint from cosmetics-20261118/native/S1/run-7 and verifies saved appearance and native evolution. Those focused fixtures do not repeat the unchanged cosmetic collection/controller/reward acceptance from1.4.0. All owned player and desktop exit receipts are retained. Updater transition checks inspect the existing pre-write asset contract; no Android runtime/deployment claim.

Storage: disposable extraction/build/copied-player work uses D:/Temp/20mtd-input87-20261118 because it requires sustained native/small-file I/O. Required evidence and release artifacts use D:/Projects/20MTD-Modernization-evidence/input87-20261118. A verified new preparation will be retained under D:/Projects/20MTD-Modernization-maintenance/input-8.7 for ongoing maintenance; existing preparations remain intact. The native QA identity will be CodexModernization/20MTD-Revamped-Input87-Validation.

Final delivery: [v1.4.1](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.4.1), normal published release from `9a993d73458d3edc567a081766c864793c5bafdf`. Independently downloaded ZIP SHA-256 `f734c7ee44b27184178983f354897378138a548b55b5b4e652ded8436f5725a6`; all110 package members and the1,738-target schema2/updater0.2.0/save-epoch1 contract verify. Assembly version1.4.1.0. Final type/method IL exactly matches the native-tested implementation; only module build provenance changes. The merged preparation is retained at the maintenance path above. Reviewed cleanup `790e56074759be549f5fdd9c5f277562` removed659,792,314 logical bytes: temporary preparation/player/build/install/download and QA-profile roots, plus expendable full-history diagnostic snapshots. No exclusions or residual task roots. Installed game, player profile/settings and phone were not changed.
