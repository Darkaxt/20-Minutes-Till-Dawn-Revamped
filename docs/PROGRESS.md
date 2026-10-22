# Current Android gameplay completion

P2 Android gameplay completion and integrated delivery is ACTIVE and is the only active stage. The user's 2026-10-22 clarification supersedes the implementer-added balance exclusions. Known Android gameplay disparities and unresolved reachable-method coverage are internal advancement blockers to completion; they are required implementation work, not optional proposals. No further release or deployment occurs until that integration and its acceptance pass. First corrected gameplay batch: Android numeric recipes for Culling, Mini Clip, Giant and five Tomes pass native acquisition, effective stat application, title exit, original restart and real Revolver firing (S3/run-74). Stalwart flat/minimum cooldown and recovery advance pass S3/run-77; Fresh Clip/Anger Point/Abby Whirlwind refresh behavior passes run-79. S4/run-66/69 verifies native save/fresh-process Continue, exact helper ownership, remaining-time expiry, reactivation and original restart. This closes only those stated boundaries; other combat/mode differences and coverage remain active blockers. The existing 1.0.0 release and local unpublished 1.0.1 branding update are partial migration outputs. Historical stage completion below does not establish complete Android parity.

# Current 1.0.1 update

P1 compatibility/preservation is COMPLETE. The earlier P2 branding work is subsumed by the active gameplay-completion stage above. The user corrected the footer implementation: the actual logo must say MAINTAINED BY DARKAXT while preserving Flanne's credit, and the full Emberpath promotion/link must be removed. The README now exposes retained Android differences, incomplete method coverage and required outstanding gameplay differences. The actual maintained-credit artwork, original pixels/190-vertex source geometry, hidden promotion/link and Options/title navigation pass native windowless rendering at 720p and 1080p. Integrated HUD/gameplay and installer ownership/recovery checks pass. No refactoring is needed. Final exact-package verification, 1.0.1 publication/download, PC/phone update and scratch cleanup remain; this update is not yet delivered.

The six stages below document the historical verified 1.0.0 delivery.

# Implementation and delivery status

Stage updates are committed and synchronized after verification. One final release follows completion of the whole plan. This initial source snapshot records the accumulated S1–S4 implementation; it does not invent separate historical stage commits.

| Stage | Status | Acceptance and remaining work |
|---|---|---|
| S1: weapon and enhancements | COMPLETE | Native Charge Cannon, three evolutions, original weapons, save/settings and reversible visual style verified. The later requested HUD revision belongs to S5. |
| S2: playable forms | COMPLETE | Gale/Bulwark artwork, animation, passives, exclusive upgrades, native input, scene cleanup and persistence verified. |
| S3: existing gameplay and progression | COMPLETE | Bounded source-linked fixes, finite repeat choices, encounter unlocks, preserved legacy ownership and revised rune economy/selections verified. |
| S4: new flows and continuation | COMPLETE | Recovered Challenge schedule/quotas/tier gates, opaque native menus, semantic run continuation, Custom controls/defaults and credits-only isolation verified. |
| S5: integrated acceptance | COMPLETE | Transparent native icon HUD/title at both sizes; fresh/legacy/complete progression, Custom results/defaults/navigation/Continue and reversible art/settings verified. |
| S6: final delivery | COMPLETE | Normal v1.0.0 release, independent download/tag/source/digest checks, actual Steam installation verification and reviewed scratch cleanup pass. |

All six stages are COMPLETE. No required blocker or tracked deferral remains. [Revamped 1.0.0](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.0.0) is delivered and independently verified.

## S4 verified workflows

- The recovered Challenge contract contains 36 horde sessions, 10 major bosses, five elite sessions and five quota tiers. The real native scheduler, encounter deaths, both-quota victory, ownership/tier persistence and original-mode restart pass. Clock/quota preconditions are explicitly seeded; this is not a claim of full-length unassisted balance testing.
- Native acquisition â†’ pause/save â†’ process exit â†’ Continue restores semantic loadout, ordered repeated upgrades/evolution, rune ranks, health/XP/ammo, finite pools, known runtime effect state, encounter/drop records and hitless/shooting history. Normal, Challenge, Endless, trained summon/timed effect, evolution and Custom variants pass. Malformed, unsupported and unowned-content checkpoints are preserved and rejected.
- Saving before the native starting Blessing completes preserves its remaining delay. Continue grants one shield and consumes its offer once. Completed-Blessing Custom save/Continue also passes.
- Custom controls cover native weapon/global attributes/XP, enemy/elite/boss factors, obtained summons, finite available upgrades, collected stat contributions and scenario objects. Native triggers/actions retain their existing behavior. The reference mod's unseen binary is not claimed to have exact field parity.
- Owned-mode defaults read actual native MapData. Pre-run defaults clear overrides and choose initial difficulty; live same-mode reset retains the build/clock. Another mode requires an explicit new-run action, retains native credits and does not award unlocks or achievements. Preset checkpoint continuation and keyboard/controller navigation pass.
- Original installation verification finds all 142 baseline files unchanged, with no added files. Testing uses a copied player and a separate profile, muted and windowless with offscreen captures and synthetic input.

## Source coverage and exclusions

442 structural API differences have explicit bounded dispositions. 125 link to feature-specific reviews; 140 retain native APIs with uninspected bodies explicitly unproven. Remaining rows retain native-only/presentation APIs, exclude commerce/reward flows, or map the required new flows to native implementations. This is not whole-engine method equivalence.

Promotional character/rune rentals, coupons and ad/GamePass rewards are excluded. Source Rebirth has no free gameplay revival when neither ad nor GamePass is available. Native Second Wind remains. Android map/Endless numeric rewrites and other balance proposals are recorded for consideration and have not been silently applied.

Continuation reconstructs the supported build/runtime contract, not every live enemy/projectile or RNG state. Ordinary world actors are not frozen snapshots. Existing Windows Endless tuning remains native; its independent round deadline and cycle scaling survive continuation.

## Integrated acceptance

The HUD/title passed real offscreen native rendering at 1280x720 and 1920x1080, including actual stat modifiers, skill tiers/counts, clipping/paging, Options/F2, shooting and scene cleanup. The full 30-gate graph, fresh-process exact persistence and legacy migration pass. Early-profile Custom respects obtained content/runes/prerequisites. Actual Custom Challenge victory retains credits with unchanged tier/ownership; the native disabled-achievement caller is verified. Custom defaults and keyboard/controller continuation pass at 1080p. Exact original GPU pixels for all 203 recolored loaded sheets restore over two toggle cycles; settings survive relaunch. Source audits remain bounded; see the migration and anomaly ledgers. Seeded clocks/builds/quotas are not unassisted balance playtests.

## Final delivery evidence

Owned-input preparation passes in a clean pinned runtime: 511 PNGs match reviewed RGBA pixels, consumed character/fallback fields match at native float32 precision, and both compressed payloads plus the 203-sheet manifest match exactly. The Release package installs/verifies 538 runtime files. Native weapon/evolutions, both forms/exclusives and Custom save/fresh-process Continue pass using installer-generated assets without developer-asset copying. Rollback removes all 538 owned files while preserving all 142 original files and every unowned file/settings. Installer conflict, pending-recovery, shared-loader, modified-file and stock-file ownership guards pass. Release paths are mapped to public placeholders; canonical managed code/static data remain unchanged, and packaging/validation builds produce identical bytes. Publication, independent download/local delivery and reviewed cleanup are verified below.

Release v1.0.0 is a normal published release, tagged to runtime source commit cead67c93aecc48012cdaf8847b760f8eccae998. Independent download matches ZIP SHA-256 b069dfa41b2dee9c7a0eb6f6c2d14b4b8b22f7a00de8f30579e32150db2c1fce; every package member and the delivered production DLL match their manifests. The exact final package passes native copied-player acceptance. Its downloaded installer is installed in the intended Steam folder: all 538 runtime files and 142 originals verify, and the three real profile files are unchanged. The real profile was not launched for testing. Installed files still verify after the disposable preparation/build/player/profile/diagnostic scratch and local build-path override are removed through four reviewed cleanup transactions. Maintained source, research evidence, the versioned local package, installed game and prior private experiment are preserved. This closure update changes documentation only; the immutable release tag identifies the verified runtime source.
