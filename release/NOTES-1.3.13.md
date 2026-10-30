# 1.3.13 — Compact pixel-art corner HUD

- Replace the full-width XP/level strip with the current character portrait, stepped pixel-art frame and internal level badge. The portrait artwork is unchanged.
- Native hearts/soul hearts, a short XP bar and ammunition occupy three compact rows. Existing variable/event bindings continue to drive them.
- The cluster stays within the safe area and above the Score text’s bottom edge. Expanded health wraps inside the same height.
- Raise timer, Score and objective into the space released by the old bar. The Custom badge moves above the timer; metrics and skill paths remain vertically centered.
- Preserve the existing HUD opacity, gameplay, settings, progression and POI logic. Verified at1280×720,720×720 and1920×1080, including live updates, reload, level-up, pause, Continue and a ported portrait.

No save reset. Compatible with updater0.2.0, schema2 and save epoch1.
