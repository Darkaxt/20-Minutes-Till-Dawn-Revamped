# Redirected-flow dead-code audit

Read-only audit,2026-10-24, based on30179a9 plus the concurrent correction batch. This is a bounded static inspection, not proof that every source region has been checked. No code was removed.

The confirmed unreachable regions are predominantly original method bodies in the shipped game assembly. Harmony still uses their signatures as entry points, and the Android replacements still use native types, fields, serialized components and callbacks. Removing their patches would restore Windows behavior rather than remove dead mod code.

| Region | Actual roots / disposition |
|---|---|
| Gun/shooting/reload/ammo | AndroidShooting, AndroidChargedShooting, AndroidReload and AndroidAmmo replace selected native bodies. Unity state/input callbacks, native Gun signatures and the nonvirtual base Shooter.Shoot call remain live. |
| Burn, area burn, pools and projectile explosions | Unity messages and native public methods route to the Android owners. Older-looking filenames such as BurnAreaCoverage and GeneratorBuff still hold active implementations. |
| Buffs | Native ApplyBuff/RemoveBuff still perform the stat changes under the corrected counter/owner patches. They are not wholly replaced methods. |
| Superseded perk recipes | Named acquisition graphs are replaced per owner. Native action/helper types are reused by other recipes, inherited helpers and serialized assets; no global deletion follows from one graph replacement. |
| Continue / legacy readers | RunResume.Read still reaches schema3–6 reconstruction and validation. Old DTO fields, coroutine identities and reflection tables remain compatibility roots even though current capture uses sourcePowerups. |
| Charge Cannon | PortedShooter remains the actual Cannon owner. The new ChargedShooter port serves Crossbow and its evolutions; it does not replace Cannon. |
| ShotPlan.ProjectileFactor | All inspected plans store1 and no source/validation reader was found. This public field is an uncertain redundant-data candidate, not a proven removable class/method or a demonstrated redirect consequence. External/reflection/packaging consumers remain to be established before any proposed removal. |

Registration is assembly-wide through Plugin.Awake → Harmony.PatchAll. Attribute-only patch classes and private Unity/delegate/coroutine callbacks cannot be classified from ordinary textual callers alone.

Result: **no definitely removable mod class or method established**. A removal proposal would need an exact source diff, compiled/reflection/serialized-root checks, and affected native/current-and-legacy Continue verification. This report does not authorize cleanup, change gameplay, or claim runtime/release verification. The private audit retains per-region file/line evidence.
