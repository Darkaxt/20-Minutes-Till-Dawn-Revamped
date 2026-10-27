# 1.1.10 — Phoenix facing

**Fixed:** Scarlett's phoenix/fire-wave visual could begin facing right or retain the previous pooled shot's direction. The native Projectile angle setter updated the physics body before its visible Transform was synchronized. The existing setter now publishes that same angle to the Transform immediately, before frame rendering and particle simulation.

**Checked beyond the phoenix:** all 37 loaded Projectile templates at eight angles through two pool activations; matching body angle, velocity and authored child rotations; the separate spirit/icicle movement-facing path; actual Scarlett third-shot burn contact, Fire Starter damage and Yuki's animated moth after scene restart. Original and Sprites Reborn renders are inspected. Separate shooting/orbiting/stabbing summon owners retain their own orientation logic. This audit targets the confirmed timing defect; it does not claim every possible visual combination was played.

**Preserved:** particle alignment, texture pixels, animation frames, source damage/size/speed/contacts, progression and save format. No new Android feature or balance change.

Native verification uses the windowless Windows game with synthetic application input and explicitly placed contact targets. Final committed package, copied installation/rollback and independent public-download checks are recorded in [PROGRESS.md](../docs/PROGRESS.md).

The existing Android updater remains compatible. Publication does not install the update into Steam or DroidDeck.
