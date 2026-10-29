# 1.3.7 — Compact damage numbers

Damage numbers are now half their previous size at every zoom level, while retaining camera zoom compensation. Their native animation, lifetime, pooling, damage values and existing Options On/Off control are unchanged. The smaller size is always derived from the original prefab size, so repeated zoom changes and pooled reuse cannot compound it.

Actual Windows checks cover all five zoom steps, both artwork styles, square/4:3/16:9 viewports, rendered glyph bounds, native controller/pointer controls and exact saved preference/Continue. Final package, copied install/rollback and independent public download checks are recorded in PROGRESS.md. Updater 0.2.0/schema 2/save epoch 1 remain compatible; no save reset or phone deployment.
