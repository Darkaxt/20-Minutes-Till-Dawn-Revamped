# Elemental roles

## Local fire panic — 2026-10-29

The user reports that fire retreat sends enemies almost off screen and requests erratic running, such as circles. This authorizes a focused replacement of that movement, superseding the original outward-retreat rule below.

- F01: Fresh ordinary-enemy ignition produces a short, local looping panic instead of sustained outward travel. Keep the existing 0.75-second panic and 25% two-second slow. Use the ignition heading and opposite turn directions across enemies, with an uneven turn rate, rather than synchronized straight retreat. Movement must remain local at both ordinary and faster native speeds.
- F02: Panic affects applied displacement only; it must not overwrite stored native pursuit velocity. Normal AI pursuit resumes when panic ends. Preserve native stun/freeze priority, boss slow-only behavior, reapplication without restarting panic, propagation potency, damage, modifiers and visuals. Pool reuse resets all panic-local state; pause stops its progress. No new saved enemy state, RNG consumption, tuning setting or framework.
- F03: Verify actual native movement/path extent and turning, immediate pursuit recovery, simultaneous enemies, no repeated panic, pause, freeze/stun, boss behavior, pool reuse, current/prior Continue and existing fire damage/propagation. Use an isolated non-input Windows desktop; retain actual movement samples and screenshots. Controlled fixtures do not establish natural full-run balance.
- F04: Update gameplay documentation and README; deliver one normal verified release through the established Windows/DroidDeck updater channel after F05–F06 below. Preserve schema2/save-epoch1/updater0.2.0 compatibility, live Steam/profile/settings and phone state. Verify the exact committed package, install/rollback, public source/tag/assets/download and clean task scratch.
- F05 (user-added): The exploration magnet reward must stagger XP attraction by orb distance, giving distant orbs a proportional delay instead of pulling the entire map into the player within roughly two seconds. Implement the wave at12 world units/second followed by the native0.3-second flight; equal-distance clusters receive at least1/60 combat second between starts, avoiding one arrival burst. A normal nearby pickup may claim a still-waiting orb immediately. Preserve XP value, ordinary pickup range, pause, pooled-orb reuse, other magnet consumers and Continue. Verify near/far native orbs and the complete actual reward collection, without discarding earned XP or frame-budget timeouts. These are the requested gameplay delays, not execution deadlines; existing transient ground-pickup save semantics remain unchanged.
- F06 (user-added): Replace the XP exploration reward's cross-like edge marker with a recognizable cluster of larger orb dots in the native XP palette, consistent between preview and collected reward. Healing and other reward markers stay distinct. Verify actual rendered edge markers in both artwork styles/viewports and truthful full-health/old-credit conversions.

| Stage | Status | Acceptance satisfied | Remaining / blockers / deferrals |
|---|---|---|---|
| F1 Complete local panic behavior | COMPLETE | F01–F03 pass native3–5: actual normal/fast local paths, stored pursuit/recovery, opposite simultaneous turns, pause, refresh, stun/freeze, boss/pool ownership, byte-bound Continue and existing fire damage/propagation/fireball/heal checks. F1-acceptance.json binds source and evidence. Root cause was overwritten move.vector plus suspended pursuit. | None. Native1 reproduces the old stored-velocity defect; native2 passes the first implementation. Closure review: local displacement remains in the existing status owner; no new movement subsystem or saved state. |
| F2 Magnet pacing and XP marker | COMPLETE | F05–F06 pass native7–10:122 real orbs, linear near/far delay and dense-ring pacing, exact XP, normal/pending/pool paths, native pause, saved reward, full-health/exhausted and legacy-credit previews, seven content types, boss separation, both styles/viewports/zoom and teardown. F2-acceptance.json plus native10 bind evidence. | None. Native6 reproduces the simultaneous0.3-second magnet burst. Closure review: pending flight belongs to AndroidPickup; wave ordering and the generated symbol belong to the existing ExplorationRun. No new scheduler subsystem or ground-pickup persistence. |
| F3 Final delivery | COMPLETE | F04: exact committed native package/install/rollback, normal public1.3.6 source/tag/assets/download, unchanged updater contract, documentation synchronization and reviewed cleanup pass. | None. |

Normal [v1.3.6](https://github.com/Darkaxt/20-Minutes-Till-Dawn-Revamped/releases/tag/v1.3.6) is published from `c336072f34890866c8e30141d2ff3fe8c9357796`. Independently downloaded ZIP SHA-256: `12049e9055e09f3b87c809549e3295c9b46237f0fa7e67f89206419163bfe605`. All 81 public members verify. Only the production DLL changes among614 update targets;593 prepared assets and613 other targets remain unchanged. Updater0.2.0/schema2/save-epoch1 stay compatible without a reset.

Exact packaged S6/run-1–5 pass local panic and native lifecycle, byte-bound public1.3.5 Continue,122 native XP orbs/dense-ring pacing and exact XP, current reward Continue, all seven content markers, old-credit conversion, boss separation, both artwork styles and viewport/zoom checks. Source native3–5 additionally retain complete fire damage/propagation/fireball/healing arithmetic;7–10 cover full-health/exhausted conversions. Copied install/rollback verifies614 owned targets and142 stock files. Live Steam/profile/config and phone remain unchanged.

Reviewed cleanup transaction `2be078114d6a7072dc90bda9e8cf5f17` is applied: {'deleted': 1455}; 319214099 logical bytes removed. The exact disposable root is absent. Required evidence/packages remain at `D:/Projects/20MTD-Modernization-evidence/fire-panic-20261029`.

Temporary native/.NET small-file I/O uses `D:/Temp/20mtd-fire-panic-20261029`. Required movement evidence, captures and final packages use `D:/Projects/20MTD-Modernization-evidence/fire-panic-20261029`. Existing project/shared-input bindings remain unchanged.

These are user-requested Revamped balance changes, superseding the corresponding Android rules. The ice branch remains unchanged.

| Element | Role | Gameplay |
|---|---|---|
| Lightning | Area stun | 22 base damage; small splash with linear 100%–35% damage falloff. Ordinary enemies are stunned for two combat seconds. Another strike refreshes the stun. |
| Fire | Damage over time | Direct burns accumulate. Fresh ignition causes 0.75 seconds of uneven local circling and a 25% slow for two seconds, without replacing stored pursuit velocity. Each tick can ignite the nearest unburned neighbor at 70% potency. |
| Wind | Defensive displacement | Gales gather enemies and steer hostile shots outward. Eye pulses push enemies and scatter shots; Mastery makes enemies pursue and aim at the last observed player position. |
| Ice | Existing freeze and health damage | Existing freeze, Frostbite and Shatter behavior is retained. Electrical stun does not count as freezing. |

## Lightning

- **Electro Mage:** every second shot calls a 22-base-damage strike. Splash radius is 1.1 ordinary BrainMonster collider widths, for roughly three closely grouped ordinary mobs.
- **Electro Bug:** retains two strikes every three seconds and summon scaling, using the shared splash/stun rules.
- **Energized:** +1% Reload Speed per living, electrically stunned enemy currently on screen, per acquired Energized owner. No cap or ammunition refund. Contributions disappear on recovery, death or leaving the screen.
- **Electro Mastery:** +12 Lightning Damage; replaces the size increase with up to 3 + current Pierce + current Bounce chain jumps, including weapon-base values. Each jump retains 70% unrounded potency, chooses a new target within three body widths and stuns without another splash. A chain ends when its next jump would deal less than 1 damage; that jump produces no strike, arc or stun. Each victim is hit once per original strike.

## Fire

**Pyro Mage** retains 50% bullet ignition chance, three base burn damage and a five-second duration. Reapplication strengthens and refreshes the burn without restarting panic or slow. Propagation copies fractional underlying potency before global modifiers, applies modifiers once, and stops below one final tick damage. Neighbors must be within two ordinary body widths between centers.

**Fire Starter** retains its four-shot cadence and Bullet Damage scaling from base 40. Each impact creates one explosion with radius twice the current visible fireball radius, including size changes. Damage falls from 100% at the center to 35% at the edge; blast victims ignite.

**Intense Burn** retains +35% Burn Damage. **Soothing Warmth** retains a 0.05% heal chance per accepted burn application; propagation qualifies. The existing 1000 burn tick cap remains.

## Wind

**Aero Magic** retains 20 damage per 0.5 seconds of contact, a two-second creation cadence and 4.5-second lifetime. Gathering speed is half an ordinary body width per second. Hostile shots steer outward at up to 90 degrees per second; overlapping Gales use the closest qualifying Gale once. Speed, damage and lifetime are preserved.

**Windborne** retains movement-stat damage scaling. **Eye of the Storm** retains +5 Gale Damage and a double-damage pulse every two seconds within radius three; it scatters hostile shots up to 60 degrees. NPC displacement ranges from half to 1.5 body widths according to actual damage divided by maximum health.

**Aero Mastery** retains +15 Gale Damage and adds 1.5 seconds of disorientation to pulses. Movement and ranged aiming use the player's position observed at that pulse. Friendly shots remain unchanged.

## Control, visual cues and saves

Champion control durations are 20% of ordinary durations; displacement is 25%. Boss ignition applies a brief 10% slow instead of retreat. Native freeze and electrical stun stop movement before fear or wind displacement. Slow modifies displacement without repeatedly reducing stored velocity.

Damage relative to maximum enemy health scales **wind pulse displacement only**. Electrical stun, fire retreat/slow and wind disorientation use the fixed durations above; they do not grow with damage or the victim's missing health.

Lightning uses native strike artwork and connecting arcs, with cyan/white crackles masked inside the stunned enemy's animated sprite. Fire retains native burn artwork and its impact blast, adding warm rising masked tongues/embers for the full actual burn lifetime. Wind retains Gale impact/pulse artwork with moving mint sprite streaks for disorientation. The three persistent effects no longer use fog-circle status disks; separate GPU layers allow mixed statuses. Native sprite geometry/atlas alpha, facing, fog, hit flashes and elite contours remain intact in both artwork modes. Effects use combat time and stop during native pause; expiry/pool return and scene exit release their visuals. [Presentation contract and verification](VISUALS.md).

Acquisitions rebuild the new owners on Continue, including supported older checkpoints. Enemy-local control, projectiles and temporary Energized counts do not become permanent saved bonuses. Pool return clears elemental status; a new run clears the previous run's owners.

Verification status is recorded in [PROGRESS.md](PROGRESS.md). Controlled fixtures establish arithmetic and lifecycle behavior, not natural playtest balance.

## Enhanced regular enemies

### Elite contour correction — 2026-10-29

Authoritative requirements for the reported Forest exploder presentation:

- Q01: Verify actual sprite/collision dimensions stay 25% larger through native Boomer movement, animation and explosion windup. Trace any overwrite before changing size ownership; preserve signed/nonuniform prefab proportions and the ordinary spawn/pool lifecycle.
- Q02: Replace the circular elite aura with thin glowing red lines following the animated sprite's opaque contour, including its facing and silhouette changes. Use a crisp narrow inner edge and softer outer glow, with no filled disk/background. Keep original interior artwork, both artwork modes and fog readability. Inspect close-up and normal-zoom captures at square/4:3/16:9 sizes.
- Q03: Preserve the 200-kill cadence, 3x health/XP, Eye firing modifier, contacts, explosion/death dispatch, pause and Continue rules. Pool return removes the contour and restores ordinary geometry; repeated promotion must not compound scale or retain visual resources.
- Q04: Update the documentation/screenshots, commit/synchronize and publish a verified normal game1.3.1 release. Keep updater0.2.0/save epoch1 compatible; no additional reset for an already transitioned installation. Verify the actual package and copied install/rollback, public source/assets/download hashes. Preserve live Steam/profile/phone installations and retire expendable scratch through reviewed cleanup.
- Q05 (2026-10-29 playtest correction): Remove credits from random exploration chest rewards. Preserve other reward rules and convert already-saved, unopened credit exploration drops to the existing experience reward so they cannot keep paying credits. Arena-boss/native chests and ordinary end-of-run credit accounting are unchanged.
- Q06 (2026-10-29 requested completion): Non-boss exploration edge indicators show the actual stored reward's content icon, retaining direction/distance, visibility and nearby content glow. Indicator and collected reward must agree across Continue and old credit-drop conversion. Boss treasure retains its distinct chest pointer.
- Q07 (2026-10-29 approved Endless escalation): Keep Elite opportunities every200 native kills. In Endless, every10 defeated tier-1 Elites earns an Elite+ promotion and every5 defeated Elite+ enemies earns an Elite++ promotion. Higher-tier pending promotions take priority on the next eligible regular wave spawn without consuming an owed lower-tier promotion; do not create extra waves or promote scheduled bosses/minibosses. Higher tiers repeat the existing multipliers: health/XP3/9/27x, XY size1.25/1.5625/1.953125x, Eye frequency1.5/2.25/3.375x. Explosion radius and existing crowd-control rules remain unchanged. Use distinct contour colours, preserve pooled ordinary reuse, native kill/quota identity and exact saved counters/pending claims. Older checkpoints start new higher-tier counters at zero; retain their existing tier-1 cadence. Other modes retain ordinary Elites.

| Stage | Status | Acceptance |
|---|---|---|
| Q1 Complete elite presentation | COMPLETE | Q01–Q03 pass native6–10: eleven regular types, Boomer facing/movement/explosion windup, outside-alpha glow, animation, both styles/three viewports, pause/pool/resource teardown, real200-kill/health/XP/Eye cadence, current/prior Continue, six modes and Custom. Q1-acceptance.json binds inputs. No blocker or deferral. |
| Q2 Exploration reward and pointer correction | COMPLETE | Q05–Q06 pass native11–14,16–17: all seven content icons, credit-free selection/actual collection, native boss separation, both styles/three viewports/zoom, byte-bound current and actual older credit-drop Continue, all maps/Custom and nearby visibility. Q2-acceptance.json binds inputs. No blocker or deferral. |
| Q3 Endless elite tiers | COMPLETE | Q07 passes native18,20–25: actual50 Elite/five Elite+ defeats, priority without consuming lower claims, compounded health/XP/size/Eye intervals, exact pending Continue/new-run reset, ordinary reuse, all-mode isolation and eleven-type contour regression. Q3-acceptance.json binds inputs. No blocker or deferral. |
| Q4 Final delivery | COMPLETE | Q04 passes: normal public v1.3.1 from d156068; exact packaged native S6/run6–19,614-file copied install/rollback with142 stock files preserved, independent70-member download and hashes, updater0.2.0/epoch1 compatibility and reviewed scratch cleanup. No blocker or deferral. |

Storage: `D:/Temp/20mtd-elite-contour-20261029` is the .NET/native small-file I/O exception, registered before generation. Retained diagnostic/verification evidence and usable release artifacts belong in `D:/Projects/20MTD-Modernization-evidence/elite-contour-20261029`. Existing shared native inputs remain in place. No tracked deferrals.

Every 200 kills in the native run counter earns one elite opportunity. The next eligible regular wave enemy receives **3× its final spawned maximum/current health** and **3× its normal XP drops**, with a thin glowing red sprite contour and **25% larger width and height**. The sprite and collision bounds scale together from that actor's original size, preserving its proportions. Elite Eyes fire **50% more frequently**: their normal special cooldown is multiplied by 2/3. Other movement, contact damage and non-XP drops remain unchanged.

Endless adds two tiers. Every **10 defeated ordinary Elites** earns an Elite+; every **5 defeated Elite+** enemies earns an Elite++. Promotion takes the next eligible regular spawn, with the highest owed tier first. A higher-tier claim does not consume a pending lower-tier opportunity, and no additional wave is spawned.

| Tier | Health / XP vs ordinary | Width / height vs ordinary | Eye frequency vs ordinary | Contour |
|---|---:|---:|---:|---|
| Elite |3×|1.25×|1.5×|Red|
| Elite+ |9×|1.5625×|2.25×|Amber|
| Elite++ |27×|1.953125×|3.375×|Violet|

Each tier repeats the previous tier's ratios. Explosion radius, movement/contact damage and crowd-control resistance keep their existing rules. Pool return/despawn does not count as a defeated Elite. Standard, Quickplay, Challenge, other scenarios and Custom retain ordinary Elites.

This promotion is separate from the game's scheduled minibosses and bosses. Those encounters do not consume the opportunity; a promoted regular enemy still counts as one normal enemy, including in Challenge quotas. Health scaling from difficulty, modes and Custom settings is applied before the promotion.

The existing run checkpoint stores claimed opportunities and Endless tier defeat/claim counters. Pending opportunities survive Continue, and a new run starts at zero. Older checkpoints without the higher-tier member start those new counters at zero; their ordinary elite cadence is preserved. Checkpoints predating ordinary elite claims begin at their next 200-kill boundary, retaining partial progress without a retrospective elite burst. Ordinary enemy world instances follow the game's existing Continue behavior; this addition preserves the cadence rather than introducing a separate enemy-world save system.

Pool return restores the exact pre-promotion size and removes the health multiplier, contour and faster Eye firing. A new promotion starts from that actor's ordinary pose rather than retaining its previous tier's scale. The source returns an enemy before dispatching its drops, so its XP multiplier remains available for that death dispatch and resets on its next activation.
