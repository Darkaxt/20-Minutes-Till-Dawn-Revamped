# Playtest corrections — v1.1.6

These changes implement the reported HUD, upgrade-menu, zoom, projectile-color and chest-location issues. Android gameplay formulas, eligibility, rewards and Tome trade-offs are preserved.

| Requirement | Result and native verification |
|---|---|
| Collected skills | Icon-only rows per acquired path, progression order within each row; character/Tome/Synergy groups separate. Tripled acquisitions glow. All icons survive Continue. Both HUD groups center vertically using reported safe-area bounds at900×900,1024×768 and1280×720. |
| Metrics and timer | Off/Little/Extended retained; lower reload time green, higher red. Native screen UI no longer inherits stale pixel-perfect camera dimensions after square resizing. Timer/credits hide during upgrade/reward selection to keep its heading clear. |
| Damage numbers | Glyph size compensates zoom0/25/50/75/100%, leaving native world position, motion, colors and lifetime intact. Actual enemy damage creates the rendered numbers. |
| Upgrade paths | All25 loaded regular trees bind their exact four native nodes. Preview slots remain visible and cannot become unoffered selections. Real controller focus and pointer confirmation refresh the chosen skill. The defect was a finite-offer binder treating chart previews as unused offers. |
| Build help | Uses actual stat changes and attached summon components. Ghost Friend uses summon damage and attack speed; Lilith spirits use summon damage, without claiming attack speed creates more souls. Magic Lens uses summon damage; Trainer legitimately helps because it also grants damage. Named Synergy recipes are identified separately when a pick completes their prerequisites. |
| Tomes | Glass first pick actually doubles damage, then×4/×6 on repeats; no dodge and fatal unprotected damage remain. Stillness actually adds100% bullet damage/size and one projectile, with zero movement. Benefits now precede penalties. This fixes presentation, without changing the intended risk or promising those risks suit every build. |
| Boss chests | Each actual pooled chest owns its edge icon; visible/collecting/disabled chests hide it. Physical collection, native reward confirmation, scene teardown and current Continue are covered. No separate saved marker inventory. |

## Projectile-color audit

The requested read-only worker compared projectile source/serialized rendering routes. The texture pack covered named sheets but missed neutral procedural trails, particle shader tints and neutral child sprite bodies. Shotgun pellets use a TrailRenderer; the large Fire Starter shot uses direct pooled production every fourth shot, child particles and a `T_Circle` SpriteRenderer. Its root’s missing sprite reference did not mean its whole hierarchy lacked sprites.

The correction snapshots each actual player-projectile hierarchy through the existing pool registration. Neutral trail colors receive the palette while alpha keys remain exact. Particle shader colors use per-renderer property blocks so already emitted particles are colored too. Uncovered neutral sprite textures receive renderer tints; pack-covered textures, colored source accents and dark colors retain their artwork. Original mode restores captured values. Shared materials, mesh/sprite geometry, animation, emission modules and collision behavior are unchanged.

| Coverage | Evidence and limit |
|---|---|
| Named source trail families | Shotgun, Shadow Bat, Hellfire, Close Quarters, Vampiric, Bat Gun, Wind Knife, Hail Shot, Quick Load, Spore Gun, Wind Bolt, Firey Payload, Revolver, Quantum Bolt, Crossbow, Sword, Dual SMGs, Salvo Knives and Headshot were traced by the worker. |
| Loaded renderer census | Main-agent native inspection additionally records all loaded player-layer projectile prefab hierarchies with trails/particles, including Fireball, Dragon, Shatter, Icicle, Fire Wave, Frost Moth, explosions and retrieving projectiles. This is an inventory and common-route check, not a claim that every effect was separately rendered. |
| Actual producer/render proof | Native Shotgun input fires four rounds; actual Fire Starter creates its shot. Inspected pixels show gold pellet trails and orange body/particles. Pooled Shotgun/Fireball/Hellfire checks exercise recolored→Original restoration and alpha preservation. |
| Intentional white detail | Existing pack texture highlights and colored source accents are not blindly replaced. The sprite test samples uncovered neutral textures only. No extracted or newly generated artwork is distributed. |

## Acceptance boundaries

Local evidence: S1/95 fresh and96 fresh-process Continue; S1/90 real projectile capture; S1/85 all25 tree bindings, finite/empty rewards and physical chest cleanup; S1/84 full1080p controller/Options/Custom/rune/failed-Continue regression. Later changes to the latter runs’ binaries are confined to visual tint/build-help filtering; their relevant input/binder/lifecycle code is unchanged. The final packaged player repeats all four playtest modes and the controller workflow before publication.

These are real windowless Windows scenes with isolated profiles, controlled acquisitions/enemies/clock and synthetic input. The runs do not establish natural unlock pacing, every possible build combination, or phone presentation. The user deploys through the existing updater APK. Source/candidate/install/rollback/public-download receipts are recorded in [PROGRESS.md](PROGRESS.md).
