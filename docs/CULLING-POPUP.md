# Culling feedback — 2026-10-30

Authorized presentation change: replace Culling's enormous numeric damage popup with **Culled!**. This changes feedback only, preserving the imported 15% summon-hit roll, creature exclusions, signed999999 Powerup damage, modifiers, protection, death notifications and save compatibility. Ordinary damage, including unrelated hits of999999, remains numeric. Damage numbers Off also hides this message. Use the existing pooled popup, font, animation, opacity and zoom sizing.

| Stage | Status | Acceptance | Remaining / blockers | Deferrals |
|---|---|---|---|---|
| P1 Culling popup | COMPLETE | Explicit label carried only by Culling's enemy damage call; native acquisition/trigger verifies text, unchanged damage/death, subsequent ordinary numeric hits and setting Off; inspect screenshot | Verified by native run1; no blocker | None |
| P2 Delivery | ACTIVE | README/release notes; normal versioned release, copied install/rollback, independently downloaded package/hash/source verification, original game/profile preservation and exact scratch cleanup | Package/public verification and cleanup remain | None |

Execute directly with the established background-desktop player. Native/build scratch uses `D:/Temp/20mtd-culling-popup-20261030` for sustained small-file I/O. Retain requested verification evidence and release under `D:/Projects/20MTD-Modernization-evidence/culling-popup-20261030`. No phone/live Steam deployment or reset.

Native run1 verifies actual acquisition, controlled trigger, unchanged damage and death, native pooled Culled! rendering, subsequent ordinary999999 feedback and setting Off. No extra gameplay replay. Closure review: the explicit call argument adds no shared mutable popup state; native health remains authoritative.
