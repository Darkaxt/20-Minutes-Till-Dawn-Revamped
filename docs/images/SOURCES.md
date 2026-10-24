# Windows screenshot provenance

These PNGs are byte-for-byte copies of existing Windows test captures, inspected before inclusion. No generated or retouched screenshot is used. Test profiles/builds are deliberate; they do not demonstrate natural progression or full-run balance.

| Published image | Original local evidence | SHA-256 |
|---|---|---|
| [title-maintained.png](title-maintained.png) | `evidence/S1/run-24/pixel-art-title.png` | `cec22dfc90512350b38129d0947909a1e975d968afb8625487b11011ed2f31fa` |
| [hud-build.png](hud-build.png) | `evidence/S5/run-9/integrated-colorful-hud.png` | `25ef22a82b13e8eb830aa69acd4b98cad8542d8730944a36cffbb67678427036` |
| [unlock-diamond.png](unlock-diamond.png) | `evidence/S3/run-103/achievement-locked-character-v2.png` | `548319e914d13c9c73e44f0b5fd199695576581013c827ad134248c328ad08b4` |
| [runes-progression.png](runes-progression.png) | `evidence/S3/run-98/rune-multiple-enabled-and-column-gates.png` | `972a6cb9ad145bcd298dfef579b74049b6aa8adb598716bc1bdea722450b3a56` |
| [custom-mode-defaults.png](custom-mode-defaults.png) | `evidence/S4/run-523/custom-native-run-menu.png` | `862bfbfa0804efee2eb01ee9c94c53b9383befa4ccbf0a1424e81ff94a21eaf6` |

The title, rune and progression captures come from their completed UI workflows; HUD capture is the 1920×1080 integration workflow. The Custom capture is the later source-integrated run-523 workflow. Final v1.1.2 Windows delivery separately passed S6/run-17; unchanged UI contracts and earlier 720p/1080p evidence were reconciled at delivery. Source evidence is intentionally local; proprietary game inputs and the raw test profiles are not distributed.
