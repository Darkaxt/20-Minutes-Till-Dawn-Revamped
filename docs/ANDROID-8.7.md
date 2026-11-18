# Android 8.7 / 1105 comparison

Source inspection dated 2026-11-18. Revamped's gameplay reference remains Android **8.6 / 1093**. Version1.4.0 integrates the complete **8.7 /1105 appearance catalog** with permanent victory rewards. Saint Rosa and the remaining gameplay comparisons below are separate pending work; appearance integration does not establish exhaustive8.7 gameplay parity. [Appearance rules and verification](COSMETICS.md).

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
| Existing asset references | Candidate differences in Rhogog's projectile, pulse prefab and rune-prefab references | Resolve target objects by source identity/contents; anonymous component names and changed IDs are not sufficient evidence of behavior changes |
| Weapon evolutions | 22 selected GunEvolution assets have unresolved managed-reference trigger layouts in this decoder pass | Recover the trigger serialization before drawing evolution-parity conclusions |
| Unchanged declarations | A method can change without changing its declaration | Complete relevant method-body comparison before claiming exhaustive 8.7 parity |

## Evidence and limits

Private source-bound results are retained under `D:/Projects/20MTD-Modernization-evidence/android-8.7-1105-20261118`: package hashes/member inventory, full asset inventory, localization, decoded metadata/assemblies, selected gameplay data, derived deltas, findings and an exported/authenticated REA Evidence bundle. The inventory reads 37,217 Unity objects. Selected generated-layout decoding recovered 3,197 records and recorded 23 failures: the 22 evolution assets above and Unity's built-in GUISkin. Failures are preserved explicitly rather than counted as matching content.

The comparison excludes build-local reference IDs and serializer-only metadata; numeric comparisons tolerate baseline rounding noise. Raw source values remain preserved. Name joins cannot prove equivalence of anonymous or duplicate components. No Android/Windows runtime parity or natural-run balance claim follows from this inspection.

The analysis did not modify or launch the installed game, change saves, deploy to the phone or publish a new game package. The exact disposable .NET scratch root was removed through reviewed cleanup transaction `e0737b8e91721b66b91f55dfe1213f59`; the owned REA workflow exported and authenticated its Evidence, closed its target and verified native process-tree exit.
