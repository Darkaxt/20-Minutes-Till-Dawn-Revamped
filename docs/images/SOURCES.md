# Windows screenshot provenance

These PNGs are byte-for-byte copies of existing Windows test captures, inspected before inclusion. No generated or retouched screenshot is used. Test profiles/builds are deliberate; they do not demonstrate natural progression or full-run balance.

| Published image | Original local evidence | SHA-256 |
|---|---|---|
| [title-maintained.png](title-maintained.png) | `evidence/S1/run-24/pixel-art-title.png` | `cec22dfc90512350b38129d0947909a1e975d968afb8625487b11011ed2f31fa` |
| [upgrade-paths.png](upgrade-paths.png) | `evidence/S1/run-131/build-icons-MagicLens.png` | `dbc015b60dd1bf6b3403766977a75673e298700bfd5f6a9c00f0d2c01e052a5d` |
| [hud-build.png](hud-build.png) | `evidence/S1/run-131/grouped-hud-1280.png` | `9008d05d34790d349cdbb8ea4b52441a34f16892902e11f198de18b245750858` |
| [skill-interactions.png](skill-interactions.png) | `evidence/S1/run-131/build-icons-SummonMastery.png` | `00ccd122158c071e40c1deff258f19828e76bdef59c0011036ed2ce41e8d091c` |
| [mode-clears.png](mode-clears.png) | `evidence/S1/run-122/mode-crowns-darkness-2.png` | `d9e3363b97ec9c9c5df475e40def9f625f702b2e93fc86b1a63e6edb6a32b2e4` |
| [exploration-spread.png](exploration-spread.png) | `evidence/S1/run-157/exploration-markers-spread.png` | `bdf5411cce72748f1e4daca399a7d09f70bb79edaf63b7236e06bcf8a8fad5fa` |
| [native-options.png](native-options.png) | `evidence/S1/run-125/native-title-options-1.png` | `1b16c20c48f3cf497a4ca3d3c62c7a536294be1f71b7ed93508e1221d700a9c9` |
| [unlock-diamond.png](unlock-diamond.png) | `evidence/S1/run-65/unlock-character-preview.png` | `9a28aef0e8613dc9b8b741b2d7377b9efb01bbe508da9d49ed2ee689347825a1` |
| [runes-progression.png](runes-progression.png) | `evidence/S3/run-98/rune-multiple-enabled-and-column-gates.png` | `972a6cb9ad145bcd298dfef579b74049b6aa8adb598716bc1bdea722450b3a56` |
| [custom-mode-defaults.png](custom-mode-defaults.png) | `evidence/S4/run-523/custom-native-run-menu.png` | `862bfbfa0804efee2eb01ee9c94c53b9383befa4ccbf0a1424e81ff94a21eaf6` |
| [android-bulwark.png](android-bulwark.png) | `evidence/S2/run-22/character-selection.png` | `0d90430cab1b15a52946e0f255556a4764e111ab710d492c6e413e1a9142f98f` |

Title/rune/Custom captures retain their recorded historical workflows. The current HUD is the1280×7201.1.7 workflow with controlled real Lilith/Ghost Friend/stat acquisitions; paired900×900/1024×768 and fresh-process Continue renders are inspected. The upgrade chart is the native Magic Lens selection in the current run; no natural progression claim. The native Options and concise Diamond preview are actual1920×1080 controller workflows. Source evidence remains local; proprietary game inputs and raw profiles are not distributed.

The Bulwark selection capture comes from S2/run-22. Its displayed description and starting HP were compared with the current installed 1.1.2 `Assets/characters/characters.json`; both match. Its unlocked roster is a test profile.

The current native Options capture includes Metrics and Zoom out with an opaque background. Fullscreen request flags are verified while the isolated renderer stays windowed, preserving the desktop. Companion HUD captures at1024×768 verify the corrected right skill column. No phone deployment/runtime of1.1.5 is claimed.
# Android updater capture

`updater-native.png` is an unaltered native Android window render from the installed updater0.1.0 on the connected test phone. The actual production UI fetched and verified public game release1.1.3; the separate `ui-deploy` receipt records1.1.2→1.1.3 deployment. `updater-compact.png` uses the same native client in its own390dp window. No global display configuration or game assets were changed to obtain these images. These updater images are Android evidence, distinct from the Windows gameplay gallery.

The1.1.7 HUD and interaction captures are current127 controlled native acquisitions/offers. Miniature skill/portrait/weapon icons use the actual PowerupFrame outline, with separate Benefits/Penalties rows. The crown capture122 shows actual recorded fixture victories per selected Darkness, inside the native campaign buttons.125 verifies the Metrics row has no shortcut suffix while F2 still works. All are unretouched Windows renders.

The final1.1.7 interaction layout supersedes127: currentrun-131 places the native path left beside its description and aligned Benefits/Penalties in the right column. Square/4:3/16:9 full bounds and rendered captures are verified, including character portrait and weapon penalty. Native Options125 has no Metrics shortcut suffix. All gallery files are exact unretouched evidence copies.
