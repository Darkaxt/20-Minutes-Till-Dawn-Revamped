# Windows screenshot provenance

These PNGs are byte-for-byte copies of existing Windows test captures, inspected before inclusion. No generated or retouched screenshot is used. Test profiles/builds are deliberate; they do not demonstrate natural progression or full-run balance.

| Published image | Original local evidence | SHA-256 |
|---|---|---|
| [title-maintained.png](title-maintained.png) | `evidence/S1/run-24/pixel-art-title.png` | `cec22dfc90512350b38129d0947909a1e975d968afb8625487b11011ed2f31fa` |
| [hud-build.png](hud-build.png) | `evidence/S5/run-9/integrated-colorful-hud.png` | `25ef22a82b13e8eb830aa69acd4b98cad8542d8730944a36cffbb67678427036` |
| [native-options.png](native-options.png) | `evidence/S1/run-45/native-title-options-1.png` | `e2007ab2e9306e30d0add8f5924a57af8b8dbb86cc708cc11279db20196f3436` |
| [unlock-diamond.png](unlock-diamond.png) | `evidence/S3/run-103/achievement-locked-character-v2.png` | `548319e914d13c9c73e44f0b5fd199695576581013c827ad134248c328ad08b4` |
| [runes-progression.png](runes-progression.png) | `evidence/S3/run-98/rune-multiple-enabled-and-column-gates.png` | `972a6cb9ad145bcd298dfef579b74049b6aa8adb598716bc1bdea722450b3a56` |
| [custom-mode-defaults.png](custom-mode-defaults.png) | `evidence/S4/run-523/custom-native-run-menu.png` | `862bfbfa0804efee2eb01ee9c94c53b9383befa4ccbf0a1424e81ff94a21eaf6` |
| [android-bulwark.png](android-bulwark.png) | `evidence/S2/run-22/character-selection.png` | `0d90430cab1b15a52946e0f255556a4764e111ab710d492c6e413e1a9142f98f` |

The title, rune and progression captures come from their completed UI workflows; HUD capture is the 1920×1080 integration workflow. The Custom capture is the later source-integrated run-523 workflow. Final v1.1.2 Windows delivery separately passed S6/run-17; unchanged UI contracts and earlier 720p/1080p evidence were reconciled at delivery. Source evidence is intentionally local; proprietary game inputs and the raw test profiles are not distributed.

The Bulwark selection capture comes from S2/run-22. Its displayed description and starting HP were compared with the current installed 1.1.2 `Assets/characters/characters.json`; both match. Its unlocked roster is a test profile.

The native Options capture is the actual 1920×1080 v1.1.4 controller workflow. It shows the integrated rows and opaque background in the copied Windows player; the same affected workflow also passes at1280×720. No phone deployment of1.1.4 is claimed.
# Android updater capture

`updater-native.png` is an unaltered native Android window render from the installed updater0.1.0 on the connected test phone. The actual production UI fetched and verified public game release1.1.3; the separate `ui-deploy` receipt records1.1.2→1.1.3 deployment. `updater-compact.png` uses the same native client in its own390dp window. No global display configuration or game assets were changed to obtain these images. These updater images are Android evidence, distinct from the Windows gameplay gallery.
