# 1.2.1 — Larger elite enemies

Elite regular enemies now have **25% larger width and height**, making them easier to distinguish alongside their red aura. Their sprite and collision bounds scale together from the original spawned size. Death and pool return restore that exact size; repeated promotion does not compound growth.

The existing 200-kill cadence, 3× health/XP and 50% faster elite Eye firing remain. Scheduled minibosses and bosses retain their own size and encounter rules.

For clarity, damage divided by maximum enemy health scales **wind pulse knockback only**. Electrical stun remains two seconds, fresh fire retreat/slow uses its existing durations, and wind disorientation remains 1.5 seconds, with existing boss resistance. This update does not change those balance rules.

Verification covers native rendered size in both artwork modes, proportional collision bounds, actual Eye movement/attacks, death, ordinary reuse, signed/nonuniform repeated promotion, current/older Continue and source modes/Custom. Exact-package installation/rollback and independent public-download verification are recorded in [PROGRESS.md](../docs/PROGRESS.md).

Install through the existing updater APK or Windows installer. No updater APK change is required. Live game installation remains user-controlled.
