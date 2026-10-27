# 1.1.11 — Visible exploration rewards and restored vision

**Fixed:** exploration chests now retain normal chest fog-reveal artwork, keeping them visible through shadows when their edge pointer disappears. Continue also restores the player's idle vision: the previous restore path skipped the opening animation and left the reveal root at zero size. Earned vision bonuses remain owned by the native stat system.

**Added:** a small, steady native-art glow identifies nearby chest content. It fades in within eight world units and reaches full strength within two. Teal means XP, gold credits, rose healing, violet magnet, ice blue freeze, orange frenzy, lime a regular upgrade and pink a character upgrade. Distant pointers remain mint; boss chests retain their original presentation.

**Preserved:** unclaimed exploration chests do not expire. Continue retains their exact positions and rewards. Reward values, spawn rules, three-chest cap, combat, progression and save format are unchanged.

Native Windows verification covers all eight colors in both artwork modes, square/4:3/16:9 views and zoom, real collection, pause, later spawn waves, scenario lighting, Custom, current and older checkpoints, and fresh-process idle vision with an acquired Night Vision bonus. Exact packaged-runtime, copied installation/rollback and independent public-download evidence is recorded in [PROGRESS.md](../docs/PROGRESS.md).

The existing Android updater remains compatible. Publication does not install into Steam or DroidDeck, and saves do not need to be deleted.
