# Culling feedback — 2026-10-30

Authorized presentation change: replace Culling's enormous numeric damage popup with **Culled!**. This changes feedback only, preserving the imported 15% summon-hit roll, creature exclusions, signed999999 Powerup damage, modifiers, protection, death notifications and save compatibility. Ordinary damage, including unrelated hits of999999, remains numeric. Damage numbers Off also hides this message. Use the existing pooled popup, font, animation, opacity and zoom sizing.

| Stage | Status | Acceptance | Remaining / blockers | Deferrals |
|---|---|---|---|---|
| P1 Culling popup | COMPLETE | Explicit label carried only by Culling's enemy damage call; native acquisition/trigger verifies text, unchanged damage/death, subsequent ordinary numeric hits and setting Off; inspect screenshot | Verified by native run1; no blocker | None |
| P2 Delivery | COMPLETE | README/release notes; normal versioned release, copied install/rollback, independently downloaded package/hash/source verification, original game/profile preservation and exact scratch cleanup | Delivered and verified; no blocker | None |

Execute directly with the established background-desktop player. Native/build scratch uses `D:/Temp/20mtd-culling-popup-20261030` for sustained small-file I/O. Retain requested verification evidence and release under `D:/Projects/20MTD-Modernization-evidence/culling-popup-20261030`. No phone/live Steam deployment or reset.

Native run1 verifies actual acquisition, controlled trigger, unchanged damage and death, native pooled Culled! rendering, subsequent ordinary999999 feedback and setting Off. No extra gameplay replay. Closure review: the explicit call argument adds no shared mutable popup state; native health remains authoritative.

Normal [v1.3.14](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.3.14) is published from `656bcf37103ba9f1c075e304e11fb9d4010eec86`. Independent ZIP SHA256 `cad0f68fad12f21dff61f536884faa33094f7d42b0e5acc074cbe12089162f62`; all 98 public members and614 update targets verify. Copied install/rollback preserves142 originals. Only the DLL changes; prepared assets and613 other targets remain unchanged. Live Steam/profile/configuration and phone untouched. Exact reviewed cleanup62a7bc6a1b988de7ba92cd6f0556b749 removed 318,127,317 logical bytes and task scratch is absent.

All requirements satisfied; no blockers or tracked deferrals. Final review reuses the unchanged P1 state-ownership assessment.
