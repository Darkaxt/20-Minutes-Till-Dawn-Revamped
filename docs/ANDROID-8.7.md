# Android 8.7 / 1105 comparison

## Current integration — v1.5.0 source

Saint Rosa's complete passive, three exclusive upgrades, authored chamber/projectile/burst artwork, optional recolouring, achievement gate and Continue integration pass integrated Windows acceptance. Her gate is a Temple miniboss victory on Darkness3+ as Abby with at least eight shots per second. Native controller/unlock/combat/Retry/Custom/current-and-older Continue, animated summon-pulse/opacity and exact installer checks pass. Final release/download verification remains the delivery gate. [Acceptance contract](SAINT-ROSA.md).

The changed-system review recovered137 common native procedures across the two packages after matching coroutine identities, retaining annotated disassembly rather than treating changed addresses as changed behavior. Field layout, runtime relocation and coroutine numbering differences are distinguished from gameplay changes in `method-comparison.json`, `method-comparison.diff` and `method-call-changes.json`. Nine resolved call-sequence differences concern tick artwork, effective-shot dispatch, cosmetic pools, score restoration and mobile skill lifecycle. The reload/read-fire coroutine bodies retain their branches/constants after runtime relocations and generated names are accounted for.

| Remaining row | Source result and integration |
|---|---|
| Summons / Pulsing Summons | Pulse damage, interval and range retain their previous contracts.8.7 adds `SetTickEffect`/`PlayTickEffect` and a three-frame pooled SpriteFX. These authored frames replace Revamped's generated ring: source lifetime0.2s, interval0.05s, placement/rotation/scale and effects-opacity control. |
| Gun dispatch |8.7 adds dispatch depth/pending-event ownership around shooting and release, with eligibility determined before OnShoot. Integrated at those complete boundaries; one effective-shot event per successful dispatch. Appearance already supplies the added skin-pool override. |
| Skill / reload lifecycle | New `CanPerformSkill`, immediate reload completion and pending-reload cancellation support Rose Chamber. Integrated with the existing source firing/reload owners and native desktop input/cleanup. Cooldown updates after activation. |
| Save/load | The battle-start date belongs to Halloween kill accounting. Score restoration now calls `RestoreEnemiesKilled`; Revamped already restores the actual kill count. Rosa runtime is an additional optional checkpoint member; no profile wipe. |
| Halloween Trial | `CommitBattleKills` records date-bound totals; `ClaimChest` grants item10001 (Pumpkin Key) and battle-pass XP. No new map, enemy, combat encounter or independent survival mode. Calendar/pass/draw reward plumbing is excluded; its character/cosmetic payloads use permanent gameplay unlocks and victory pools. |
| References / evolutions | All24 rune-prefab targets, Rhogog's projectile and all36 complete evolution records resolve and match8.6. No missing serialized gameplay change remains in these rows. |

Source quirks are classified in [ANOMALIES.md](ANOMALIES.md#android87-saint-rosa-source-classification--2026-11-19). Historical1.4.x findings below describe those releases. Structural comparison or unchanged call lists alone do not prove every procedure in the game equivalent.

## Historical 1.4.x findings

The following records describe the published1.4.x releases and the gaps entering this integration; the current disposition is in the table above. Source inspection dated 2026-11-18. Version1.4.0 integrated the complete **8.7 /1105 appearance catalog** with permanent victory rewards, while its gameplay reference remained8.6/1093. [Appearance rules and verification](COSMETICS.md).

Version1.4.1 migrates local preparation to **8.7/1105 as the only Android input**, retaining existing gameplay semantics. The8.6 package is no longer an installation requirement. This resource migration leaves the pending gameplay rows below for separate evaluation. [Single-input migration](ANDROID-INPUT.md).

The user-supplied APKMirror bundle is `com.Flanne.MinutesTillDawn.roguelike.shooting.fr.gp_8.7-1105_2arch_d7d3805da654643d3593e0b8166dac52_apkmirror.com.apkm`, SHA-256 `1fe5b56f2b8e5abd44461fbc754da178122a22b65e44ad53daf73568ed21039d`. It contains base, ARM64 and ARMv7 APKs. Comparison uses ARM64, matching the retained 8.6 reference. Both its native code and IL2CPP metadata differ from 8.6.

## Content ledger

| Item | Shipped-package evidence | Revamped integration disposition |
|---|---|---|
| Abby: Saint Rosa | New CharacterData, RoseChamberPassive, RosethornShellProjectile, chamber UI, burst visuals, animations and exclusive pool | Required gameplay addition; not implemented in the current release |
| Thorn Ammo Belt | New SaintRosaPowerup, perk 0, icon/localization | Required exclusive upgrade |
| Piercing Petal | New SaintRosaPowerup, perk 1, icon/localization | Required exclusive upgrade |
| Floral Burial - Finale | New SaintRosaPowerup, perk 2, icon/localization | Required exclusive upgrade |
| Rose of Versailles: Hina | Character skin 610301, animation controller and portrait | Integrated in1.4.0 Appearance; related permanent victory pool |
| Rose of Versailles: Luna | Character skin 610801, portrait/controller, black-hole sprite and effect frames | Integrated, including matching black-hole artwork and damage frames; native actor/pool verification |
| Rose of Versailles: Raven | Character skin 610111, portrait/controller and crow animation controller | Integrated, including matching crow animation; native actor/pool verification |
| Wondrous Dual SMGs | Item 4211, weapon cosmetic catalog type 8 | Integrated; native fresh/Continue/evolution render verification |
| Wondrous Cannon | Item 4509, weapon cosmetic catalog type 8 | Integrated in the85 weapon-skin catalog; related permanent victory pool |
| Wondrous Broom | Item 4809, weapon cosmetic catalog type 8 | Integrated, including source projectile artwork; native pool/Retry verification |
| Night Wings | Item 2048, wing catalog type 5 | Integrated with source placement, frame interval and animation; native verification |
| Wondrous Wings | Item 2049, wing catalog type 5 | Integrated with source placement, frame interval and animation; native verification |
| Halloween Trial | New trial manager, daily-save data, style/tier data and HUD/page classes | Inspect actual objective behavior; retain useful gameplay challenges with friendly rewards |
| Draw/pass/shop rewards | Source catalogs identify gameplay/cosmetic payloads separately from price, probability, pity, ads and purchase state | Cosmetic catalog integrated through permanent victory pools, without monetization/calendar gates. Saint Rosa gameplay remains pending |

The new cosmetic rows above are derived from the 8.6→8.7 catalog/character-skin difference, not every item available in the event lottery. Existing cosmetics also occur in that lottery and are not counted as new content.

## Saint Rosa's recovered configuration

Serialized CharacterData supplies 4 starting HP, movement speed 3.2 and shooting-movement multiplier 0.45. RoseChamberPassive supplies an 8-second cooldown, one shell per six ammunition spent, capacity eight, three shell bounces and six-degree shell spacing. Its serialized damage parameters are 2.6 plus a 0.2 extra-shell parameter; splash radius 1.2 and multiplier 0.45; Bloom radius 3.5, multiplier 1.5 and knockback 10. These are recovered configuration fields, not a claim that the full damage formula or callback order has been reconstructed.

The supplied English descriptions specify:

- Spend six ammunition to store a Rose Bullet, up to eight. Activate to volley stored bullets toward the crosshair; a full release triggers Bloom Impact.
- Thorn Ammo Belt: capacity +3, one less ammunition required per stored bullet, and +1 rose-bullet pierce.
- Piercing Petal: petal splash radius +50%, splash damage +30%, and a larger Bloom Impact radius.
- Floral Burial - Finale: volley cooldown 6 seconds, Bloom threshold six instead of eight, and +25% fire rate for three seconds after the volley.

The declarations and serialized values are available for cohesive reconstruction. The IL2CPP dump contains metadata/declarations and addresses; it is not recovered compilable original gameplay C#.

## Existing-system review ledger

| System | Observed difference | Remaining evidence needed |
|---|---|---|
| Summons and Pulsing Summons | Changed Summon, SummonEgg, DurationDamageBuff, summon damage/attack-speed powerup declarations; new pulse-effect prefab field | Compare executable callbacks, hitboxes, cooldowns, pooling and Continue behavior against current Revamped fixes; declarations alone do not establish a bug fix |
| Shooting/reloading | Changed Gun, SkillPassive, ReloadState and ShootingState declarations | Separate Saint Rosa integration from changes to existing weapon behavior |
| Save/load | Changed in-game save API/data and save-system declarations; new event save types | Recover new gameplay state and applicable lifecycle changes; preserve Revamped's existing progression and deliberate rules |
| Character cosmetics | Added summon animation/effect fields; changed skin manager and summon-sprite binding | Complete appearance path integrated: six character skins,85 weapon skins,49 wings,seven free bullet colours; source actor/projectile/pooled render paths and saved appearances verified as documented in COSMETICS.md |
| Existing asset references | Rune-prefab and Rhogog references point to unnamed components on their same named GameObjects | Resolved against source component identities and serialized fields: all24 rune targets and Rhogog's projectile match8.6. The actual new pulse-effect prefab remains part of the summons review above |
| Weapon evolutions | Embedded managed-reference type layouts were stripped; the matching8.7 DLL metadata recovers all36 evolution records with complete byte-consumption checks | Resolved during Saint Rosa integration: all36 serialized evolution contracts match8.6 after resolving references and normalizing numeric precision. No evolution-data change to import. This establishes data equivalence, not executable callback equivalence |
| Unchanged declarations | A method can change without changing its declaration | Complete relevant method-body comparison before claiming exhaustive 8.7 parity |

## Evidence and limits

Private source-bound results are retained under `D:/Projects/20MTD-Modernization-evidence/android-8.7-1105-20261118`: package hashes/member inventory, full asset inventory, localization, decoded metadata/assemblies, selected gameplay data, derived deltas, findings and an exported/authenticated REA Evidence bundle. The inventory reads 37,217 Unity objects. Selected generated-layout decoding recovered 3,197 records and recorded 23 failures: the 22 evolution assets above and Unity's built-in GUISkin. Failures are preserved explicitly rather than counted as matching content.

The subsequent `gameplay87-20261118/evolutions-decoded.json` and `evolutions-comparison.json` resolve those22 failures: every one of the36 source evolution records decodes, and all36 match the retained8.6 serialized contracts. Resolution uses the managed-reference class/namespace/assembly named by each serialized object and the same8.7 metadata, removing the generator's synthetic Unity object header for plain serialized reference classes. Original strict byte-consumption checks remain enabled; no fields or bytes are skipped to force a match. The initial failure report remains retained as historical evidence.

The same investigation's `reference-contracts.json` and `reference-comparison.json` resolve the apparent rune and Rhogog reference changes. It matches the referenced component's script type, owning GameObject and serialized fields, rather than comparing blank component names. All24 rune-prefab targets and Rhogog's EnemyAreaProjectile fields match the baseline; this does not assert executable method-body equivalence.

The comparison excludes build-local reference IDs and serializer-only metadata; numeric comparisons tolerate baseline rounding noise. Raw source values remain preserved. Name joins cannot prove equivalence of anonymous or duplicate components. No Android/Windows runtime parity or natural-run balance claim follows from this inspection.

The analysis did not modify or launch the installed game, change saves, deploy to the phone or publish a new game package. The exact disposable .NET scratch root was removed through reviewed cleanup transaction `e0737b8e91721b66b91f55dfe1213f59`; the owned REA workflow exported and authenticated its Evidence, closed its target and verified native process-tree exit.
