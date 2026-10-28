# Elemental roles

These are user-requested Revamped balance changes, superseding the corresponding Android rules. The ice branch remains unchanged.

| Element | Role | Gameplay |
|---|---|---|
| Lightning | Area stun | 22 base damage; small splash with linear 100%–35% damage falloff. Ordinary enemies are stunned for two combat seconds. Another strike refreshes the stun. |
| Fire | Damage over time | Direct burns accumulate. Fresh ignition causes 0.75 seconds of retreat and a 25% slow for two seconds. Each tick can ignite the nearest unburned neighbor at 70% potency. |
| Wind | Defensive displacement | Gales gather enemies and steer hostile shots outward. Eye pulses push enemies and scatter shots; Mastery makes enemies pursue and aim at the last observed player position. |
| Ice | Existing freeze and health damage | Existing freeze, Frostbite and Shatter behavior is retained. Electrical stun does not count as freezing. |

## Lightning

- **Electro Mage:** every second shot calls a 22-base-damage strike. Splash radius is 1.1 ordinary BrainMonster collider widths, for roughly three closely grouped ordinary mobs.
- **Electro Bug:** retains two strikes every three seconds and summon scaling, using the shared splash/stun rules.
- **Energized:** +1% Reload Speed per living, electrically stunned enemy currently on screen, per acquired Energized owner. No cap or ammunition refund. Contributions disappear on recovery, death or leaving the screen.
- **Electro Mastery:** +12 Lightning Damage; replaces the size increase with 3 + current Pierce + current Bounce chain jumps. Each jump retains 70% unrounded potency, chooses a new target within three body widths and stuns without another splash. Each victim is hit once per original strike.

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

Lightning uses native strike artwork, blue impact/status cues and connecting arcs. Fire uses native burn artwork, an orange status cue and an impact blast. Wind retains Gale artwork with mint pulse and disorientation cues. Both artwork modes use the same geometry and rules. Effects use combat time and stop during native pause.

Acquisitions rebuild the new owners on Continue, including supported older checkpoints. Enemy-local control, projectiles and temporary Energized counts do not become permanent saved bonuses. Pool return clears elemental status; a new run clears the previous run's owners.

Verification status is recorded in [PROGRESS.md](PROGRESS.md). Controlled fixtures establish arithmetic and lifecycle behavior, not natural playtest balance.

## Enhanced regular enemies

Every 200 kills in the native run counter earns one elite opportunity. The next eligible regular wave enemy receives **3× its final spawned maximum/current health** and **3× its normal XP drops**, with a visible red aura. Elite Eyes fire **50% more frequently**: their normal special cooldown is multiplied by 2/3. Other movement, contact damage and non-XP drops remain unchanged.

This promotion is separate from the game's scheduled minibosses and bosses. Those encounters do not consume the opportunity; a promoted regular enemy still counts as one normal enemy, including in Challenge quotas. Health scaling from difficulty, modes and Custom settings is applied before the promotion.

The existing run checkpoint stores how many opportunities have been claimed. A pending opportunity survives Continue, and a new run starts at zero. Older checkpoints without the new member begin at their next 200-kill boundary, retaining their partial progress without retroactively spawning elites for all previous kills. Ordinary enemy world instances follow the game's existing Continue behavior; this addition preserves the cadence rather than introducing a separate enemy-world save system.

Pool reuse removes the health multiplier, red aura and faster Eye firing. The source returns an enemy before dispatching its drops, so its XP multiplier remains available for that death dispatch and resets on its next activation.
