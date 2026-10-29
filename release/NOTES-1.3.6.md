# 1.3.6 — Local fire panic and paced XP collection

Burning enemies now run in a short, uneven local loop instead of fleeing toward the screen edge. The old movement wrote outward momentum into the enemy's stored pursuit vector; panic now changes only applied displacement. Its 0.75-second duration, 25% two-second slow, burn damage/propagation, boss slow-only response and stun/freeze priority remain unchanged. Repeated burns do not restart it.

Exploration magnets send an attraction wave outward at 12 game units per combat second. Each orb then takes its normal 0.3-second flight. Dense equal-distance groups start at least 1/60 second apart per reward, spreading pickup work across frames. Ordinary nearby pickup can take a waiting orb sooner; already-flying orbs are not restarted. Pause freezes the wave and pooled orbs clear pending attraction. Every original orb retains its XP and normal collection callbacks.

The exploration XP marker is now three pixel-rounded orbs in the native XP tint. Full-health healing and exhausted/old-credit conversions use the same symbol. Actual XP drops and other reward art stay unchanged. The README uses a current content-marker capture.

Native Windows checks cover measured normal/fast panic paths and pursuit recovery, mixed restrictions, fire arithmetic, 122 real XP orbs including a dense ring, native reward collection, exact saved checkpoints, conversions, both artwork modes and square/4:3/16:9 viewports. Final committed-package, copied install/rollback, independent public download and cleanup receipts are recorded in PROGRESS.md.

Updater 0.2.0/schema 2/save epoch 1 remain compatible. No new save reset or phone installation is part of this publication. Ground pickups retain their existing transient save behavior.
