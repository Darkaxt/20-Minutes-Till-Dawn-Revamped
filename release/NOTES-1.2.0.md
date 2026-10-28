# 1.2.0 — Elemental roles and elite enemies

Lightning, fire and wind now have distinct crowd-control roles alongside the existing ice branch. This release also adds enhanced regular enemies earned by the run's kill count.

## Added and refined

- Lightning strikes splash with damage falloff and a separate two-second electrical stun. Electro Mastery adds up to **3 + Pierce + Bounce** jumps using full weapon metrics, retaining 70% damage per jump. Chains stop below1 damage, without zero-damage stuns or arcs. Energized gives **+1% Reload Speed per visible electrocuted enemy**, replacing ammunition refunds.
- Fresh fire causes brief slow retreat. Burns spread at 70% underlying potency without multiplying bonuses twice. Fire Starter explodes with twice its current visible radius and ignites blast victims.
- Gales gather enemies and bend hostile shots outward. Eye pulses push enemies and scatter shots; Mastery disorients movement and ranged aiming. Friendly shots retain their trajectories.
- Every **200 kills** earns an elite opportunity. The next regular wave enemy gains **3× final health and normal XP**, with a red aura. Elite Eyes fire **50% more frequently**. Scheduled minibosses and bosses keep their existing roles.
- Blue lightning/stun/chain, orange fire/explosion and mint wind/disorientation cues use native artwork in both visual styles. Descriptions explain the new rules.

## Fixed during integration

- Lightning damage is applied once per victim, without the old additional collision callback from a disabled pooled effect.
- Returned burning enemies stop carrying and propagating their old burn.
- Fireball explosions retain their actual primary target, including map-instantiated actors outside the pool's identity table.

Ice rules remain unchanged. These are requested Revamped refinements, separate from the completed Android 8.6 port. [Exact values](../docs/ELEMENTS.md) · [Anomaly ledger](../docs/ANOMALIES.md).

## Continue and installation

Current and supported older checkpoints remain supported. New elemental owners rebuild from acquisitions; transient electrical counts do not become permanent saved bonuses. Elite opportunities retain their cadence through Continue. Older saves begin at their next 200-kill boundary; a new run starts from zero.

Use the existing installer or Android updater with your owned game files. No updater APK change is required. The package contains mod code and installation tooling; game assets are prepared locally from the user's files.

Verification covers controlled native Windows workflows, actual shooting/contacts/XP grants, all modes, Custom, pooling, controller menus and current/older saves. Controlled checks do not substitute for natural player balance feedback. Final package/download verification is recorded in [PROGRESS.md](../docs/PROGRESS.md). Live Steam and phone deployment remain user-controlled.
