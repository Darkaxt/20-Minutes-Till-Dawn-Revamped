using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using flanne;
namespace TwentyMinutes.Modernization;

// Android 8.6 source-bound stat recipes. Specialized effects are left attached;
// these application paths use the same native flat/bonus/reduction API.
// Values and semantic names: powerup-stat-comparison.json, A02-A05/A48.
[HarmonyPatch]
class AndroidStatRecipes
{
 static StatChange Flat(StatType type,int value)=>new StatChange{type=type,isFlatMod=true,flatValue=value};
 static StatChange Scale(StatType type,float value)=>new StatChange{type=type,value=value};
 static readonly Dictionary<string,StatChange[]> Recipes=new Dictionary<string,StatChange[]>
 {
  ["culling_name"]=new[]{Scale(StatType.SummonAttackSpeed,.3f)},
  ["vengeful_ghost_name"]=new[]{Scale(StatType.SummonDamage,.15f)},
  ["assassin_name"]=new[]{Scale(StatType.ProjectileSpeed,.1f)},
  ["hina_perk_enhanced_shadow_name"]=new[]{Scale(StatType.SummonDamage,.25f)},
  ["lilith_perk_necromancer_name"]=new[]{Scale(StatType.SummonDamage,.2f)},
  ["mini_clip_name"]=new[]{Scale(StatType.BulletDamage,.5f),Flat(StatType.MaxAmmo,-4),Scale(StatType.ReloadRate,.75f)},
  ["giant_name"]=new[]{Scale(StatType.CharacterSize,.5f),Flat(StatType.MaxHP,2)},
  ["tome_of_elasticity_name"]=new[]{Flat(StatType.Bounce,2),Scale(StatType.BulletDamage,-.05f),Flat(StatType.MaxHP,-1)},
  ["tome_of_power"]=new[]{Scale(StatType.BulletDamage,.5f),Scale(StatType.FireRate,-.05f),Flat(StatType.MaxHP,-1),Flat(StatType.Piercing,1),Scale(StatType.ProjectileSize,1)},
  ["tome_of_rage"]=new[]{Scale(StatType.BulletDamage,-.1f),Scale(StatType.FireRate,.66f),Scale(StatType.Knockback,-.2f),Flat(StatType.Spread,60)},
  ["tome_of_stillness_name"]=new[]{Scale(StatType.BulletDamage,1),Scale(StatType.ProjectileSize,1),Flat(StatType.Projectiles,1),Scale(StatType.MoveSpeed,-1)},
  ["tome_of_summoning"]=new[]{Scale(StatType.ReloadRate,-.1f),Scale(StatType.SummonAttackSpeed,.5f),Scale(StatType.SummonDamage,.5f)},
  ["tome_of_elements_name"]=new[]{Scale(StatType.BulletDamage,-.5f)},
  ["death_rounds_name"]=new[]{Flat(StatType.BulletDamage,10),Flat(StatType.SummonDamage,10)}
 };
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack)),AccessTools.PropertyGetter(typeof(Powerup),nameof(Powerup.description))};
 [HarmonyPriority(Priority.First)]
 static void Prefix(Powerup __instance)=>Configure(__instance);
 internal static void Configure(Powerup power)
 {
  string key=((LocalizedString)AccessTools.Field(typeof(Powerup),"nameStrID").GetValue(power)).key;
  if(Recipes.TryGetValue(key,out var stats))Plugin.SetField(power,"statChanges",stats);
 }
}
