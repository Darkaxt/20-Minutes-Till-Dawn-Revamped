# Windows screenshot provenance

These PNGs are byte-for-byte copies of existing Windows test captures, inspected before inclusion. No generated or retouched screenshot is used. Test profiles/builds are deliberate; they do not demonstrate natural progression or full-run balance.

| Published image | Original local evidence | SHA-256 |
|---|---|---|
| [title-maintained.png](title-maintained.png) | `evidence/S1/run-24/pixel-art-title.png` | `cec22dfc90512350b38129d0947909a1e975d968afb8625487b11011ed2f31fa` |
| [hud-build.png](hud-build.png) | `evidence/S1/run-63/hud-little-16x9.png` | `e962f59f1c7cf6c6764f9c593b624f028e4d6c00fb66feed214b8a8726bcf10f` |
| [native-options.png](native-options.png) | `evidence/S1/run-68/native-title-options-1.png` | `564471c3ec6c3f9df7eeac2240bfbee03abedf3171e7a74c330454d513ada4e9` |
| [unlock-diamond.png](unlock-diamond.png) | `evidence/S1/run-65/unlock-character-preview.png` | `9a28aef0e8613dc9b8b741b2d7377b9efb01bbe508da9d49ed2ee689347825a1` |
| [runes-progression.png](runes-progression.png) | `evidence/S3/run-98/rune-multiple-enabled-and-column-gates.png` | `972a6cb9ad145bcd298dfef579b74049b6aa8adb598716bc1bdea722450b3a56` |
| [custom-mode-defaults.png](custom-mode-defaults.png) | `evidence/S4/run-523/custom-native-run-menu.png` | `862bfbfa0804efee2eb01ee9c94c53b9383befa4ccbf0a1424e81ff94a21eaf6` |
| [android-bulwark.png](android-bulwark.png) | `evidence/S2/run-22/character-selection.png` | `0d90430cab1b15a52946e0f255556a4764e111ab710d492c6e413e1a9142f98f` |

Title/rune/Custom captures retain their recorded historical workflows. The current HUD is the1280×720 Little-mode1.1.5 workflow with real Big Shot/Haste acquisitions and explicit stat/kill-count operands; no natural progression claim. The native Options and concise Diamond preview are actual1920×1080 controller workflows. Source evidence remains local; proprietary game inputs and raw profiles are not distributed.

The Bulwark selection capture comes from S2/run-22. Its displayed description and starting HP were compared with the current installed 1.1.2 `Assets/characters/characters.json`; both match. Its unlocked roster is a test profile.

The current native Options capture includes Metrics and Zoom out with an opaque background. Fullscreen request flags are verified while the isolated renderer stays windowed, preserving the desktop. Companion HUD captures at1024×768 verify the corrected right skill column. No phone deployment/runtime of1.1.5 is claimed.
# Android updater capture

`updater-native.png` is an unaltered native Android window render from the installed updater0.1.0 on the connected test phone. The actual production UI fetched and verified public game release1.1.3; the separate `ui-deploy` receipt records1.1.2→1.1.3 deployment. `updater-compact.png` uses the same native client in its own390dp window. No global display configuration or game assets were changed to obtain these images. These updater images are Android evidence, distinct from the Windows gameplay gallery.
