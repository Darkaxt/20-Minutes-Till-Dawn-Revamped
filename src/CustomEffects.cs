using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(Powerup),"ApplyStats")]
static class CustomPerkStats
{
 static readonly FieldInfo Changes=AccessTools.Field(typeof(Powerup),"statChanges");
 internal static StatChange[] Values(Powerup power){AndroidStatRecipes.Configure(power);return (StatChange[])Changes.GetValue(power);}
 static StatChange Scaled(StatChange value,float scale)
 {if(value.isFlatMod)value.flatValue=Mathf.RoundToInt(value.flatValue*scale);else value.value=Mathf.Max(-.99f,value.value*scale);return value;}
 static void Prefix(Powerup __instance,out StatChange[] __state)
 {
  __state=null;if(!CustomMode.Active)return;
  __state=Values(__instance);float scale=CustomMode.Get("perk:"+RunResume.Key(__instance)+":scale");Changes.SetValue(__instance,__state.Select(c=>Scaled(c,scale)).ToArray());
 }
 static Exception Finalizer(Exception __exception,Powerup __instance,StatChange[] __state){if(__state!=null)Changes.SetValue(__instance,__state);return __exception;}
 internal static void ChangeScale(string setting,float scale)
 {
  string key=setting.Substring(5,setting.Length-11);float old=CustomMode.Get(setting);
  int count=RunRecorder.Instance.Acquired.Count(a=>!a.evolution&&a.key==key);if(count==0)return;
  var power=Resources.FindObjectsOfTypeAll<Powerup>().First(p=>RunResume.Key(p)==key);var stats=PlayerController.Instance.stats;
  foreach(var original in Values(power))
  {
   var before=Scaled(original,old);var after=Scaled(original,scale);
   if(original.isFlatMod)stats[original.type].AddFlatBonus(count*(after.flatValue-before.flatValue));
   else if(original.value>=0)stats[original.type].AddMultiplierBonus(count*(after.value-before.value));
   else stats[original.type].AddMultiplierReduction(Mathf.Pow((1+after.value)/(1+before.value),count));
  }
 }
}
// Spawner values are scoped to their actual native spawn call. This preserves
// Darkness/Endless modifiers, checkpoint values and other callers exactly.
static class CustomSpawnValues
{
 internal static object[] Apply(object owner,string[] fields,string[] keys)
 {
  if(!CustomMode.Active)return null;var saved=new object[fields.Length];
  for(int i=0;i<fields.Length;i++){var f=AccessTools.Field(owner.GetType(),fields[i]);saved[i]=f.GetValue(owner);float value=Convert.ToSingle(saved[i])*CustomMode.Get("spawn:"+keys[i]);f.SetValue(owner,Convert.ChangeType(value,f.FieldType));}return saved;
 }
 internal static void Restore(object owner,string[] fields,object[] saved){if(saved==null)return;for(int i=0;i<fields.Length;i++)AccessTools.Field(owner.GetType(),fields[i]).SetValue(owner,saved[i]);}
}
[HarmonyPatch(typeof(HordeSpawner),"Spawn")]
class CustomEnemyValues
{
 static readonly string[] Fields={"healthMultiplier","eliteHealthMultiplier","speedMultiplier","eliteSpeedMultiplier","enemyDamage","eliteDamage"};
 static void Prefix(HordeSpawner __instance,out object[] __state){__state=CustomSpawnValues.Apply(__instance,Fields,Fields);}
 static Exception Finalizer(Exception __exception,HordeSpawner __instance,object[] __state){CustomSpawnValues.Restore(__instance,Fields,__state);return __exception;}
}
[HarmonyPatch(typeof(HordeSpawner),"Update")]
class CustomSpawnAmount
{
 static readonly string[] Fields={"spawnRateMulitplier"};
 static void Prefix(HordeSpawner __instance,out object[] __state){__state=CustomSpawnValues.Apply(__instance,Fields,Fields);}
 static Exception Finalizer(Exception __exception,HordeSpawner __instance,object[] __state){CustomSpawnValues.Restore(__instance,Fields,__state);return __exception;}
}
[HarmonyPatch]
class CustomBossValues
{
 static readonly string[] Fields={"healthMultiplier","enemyDamage","cooldownRate"};
 static MethodBase TargetMethod()=>AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(BossSpawner),"WaitToSpawnCR"));
 static void Prefix(object __instance,out object[] __state){var owner=AccessTools.Field(__instance.GetType(),"<>4__this").GetValue(__instance);__state=CustomSpawnValues.Apply(owner,Fields,new[]{"bossHealth","bossDamage","bossCooldown"});}
 static Exception Finalizer(Exception __exception,object __instance,object[] __state){CustomSpawnValues.Restore(AccessTools.Field(__instance.GetType(),"<>4__this").GetValue(__instance),Fields,__state);return __exception;}
}
[HarmonyPatch(typeof(ChallengeMode),nameof(ChallengeMode.SpawnMultiplier))]
class CustomChallengeSpawns{static void Postfix(ref float __result){if(CustomMode.Active)__result*=CustomMode.Get("spawn:spawnRateMulitplier");}}
[HarmonyPatch(typeof(ShootingSummon),"GetProjectileRecipe")]
class CustomSummonDamage{static void Postfix(ShootingSummon __instance,ref ProjectileRecipe __result){if(CustomMode.Active)__result.damage=Mathf.FloorToInt(__result.damage*CustomMode.Get("summon:"+__instance.SummonTypeID+":damage"));}}
[HarmonyPatch(typeof(AttackingSummon),"get_finalAttackCooldown")]
class CustomSummonSpeed{static void Postfix(AttackingSummon __instance,ref float __result){if(CustomMode.Active)__result=Mathf.Max(.1f,__result/Mathf.Max(.01f,CustomMode.Get("summon:"+__instance.SummonTypeID+":speed")));}}
