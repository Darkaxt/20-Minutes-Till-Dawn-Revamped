using System.Collections.Generic;
using HarmonyLib;
using flanne;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(Powerup),nameof(Powerup.description),MethodType.Getter)]
class ElementalDescriptions
{
 static readonly Dictionary<string,string> Text=new Dictionary<string,string>
 {
  ["electro_mage_name"]="Every 2nd shot calls Lightning for 22 base damage. A small splash deals less damage toward its edge and stuns enemies for 2 seconds.",
  ["electro_bug_name"]="Summon an Electro Bug that calls 2 Lightning strikes every 3 seconds. Its strikes gain summon bonuses, splash and electrical stun.",
  ["energized_name"]="Reload Speed +1% for each living, electrically stunned enemy currently on screen. Each enemy counts once; there is no cap.",
  ["electro_mastery_name"]="Lightning Damage +12. Chain to up to 3 + Pierce + Bounce more enemies, retaining 70% damage per jump. Jumps stun without splash. Chains end below 1 damage.",
  ["pyro_mage_name"]="Bullets have a 50% chance to apply 3 Burn damage per second for 5 seconds. Burns accumulate. Fresh ignitions cause brief slow retreat and spread weaker fire nearby.",
  ["fire_starter_name"]="Every 4th shot launches a Fireball with 40 base damage, improved by Bullet Damage. It explodes with twice its radius, dealing less damage toward the edge and igniting enemies.",
  ["intense_burn_name"]="Burn Damage +35%, including propagated burns. Fire spreads with 70% of its parent's accumulated potency; bonuses are applied once.",
  ["soothing_warmth_name"]="Each accepted Burn application has a 0.05% chance to heal 1 HP. Propagated ignitions also count.",
  ["aero_magic_name"]="Every 2 seconds launch a Gale dealing 20 damage every 0.5 seconds on contact. It gently gathers enemies and bends enemy bullets outward.",
  ["windborne_name"]="Gale damage gains your Move Speed bonuses, even while standing still.",
  ["eye_of_the_storm_name"]="Gale Damage +5. Every 2 seconds, each Gale pulses for double damage, pushes enemies outward and scatters enemy bullets.",
  ["aero_mastery_name"]="Gale Damage +15. Gale pulses disorient enemies for 1.5 seconds, making them pursue and aim at your last known position."
 };
 static bool Prefix(Powerup __instance,ref string __result)
 {if(!Text.TryGetValue(RunResume.Key(__instance),out string value))return true;__result=value;return false;}
}
