# 1.3.4 — Visible combat and sprite status effects

**Fixed:** the ported Android performance sampler could permanently hide damage numbers after eight low-FPS samples. It has been removed. **Options → Damage numbers: On/Off** is now the explicit, persistent choice, enabled by default and accessible with controller or pointer. Old automatic-disable preferences no longer silently suppress hits. Existing damage arithmetic, pooling, animation and zoom compensation are retained.

**Improved:** burning, electrical stun and wind disorientation use animated GPU patterns masked by the enemy's own sprite mesh and atlas alpha, replacing the filled status disks. Warm rising fire, cyan crackles and mint streaks follow animation/facing and coexist with enlarged elite contours. Fire remains visible for the complete burn lifetime. Pause, expiry, pool return/reuse and scene teardown clear or freeze the effects with their native owners. These materials reuse the game's shipped compiled shader; no editor, extra runtime, external shader package or original-art replacement is required.

**Fixed:** chest indicators previously hid when only the chest's center entered the screen, including beneath the skills HUD. They now hand over only when the complete chest artwork is inside the game viewport and clear of the HUD. Unclaimed exploration chests do not expire. Reward amounts, placement, RNG and collection are unchanged.

Windows verification includes actual hit numbers and native Options/Continue, square/4:3/16:9, both artwork styles, zoom0/100, real GPU alpha-mask output, mixed statuses on an enlarged elite, native render handoff/contact, full burn lifetime, pause and pool/scene lifecycle. Final committed-package, copied install/rollback and independent public-download verification are recorded in [PROGRESS.md](../docs/PROGRESS.md).

No balance or save-format changes. Updater0.2.0/schema2/save-epoch1 remain compatible; no new save reset. Publication does not change the live Steam or phone installation.
