using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PowerupSystem;
namespace TwentyMinutes.Modernization;

// The native helpers lack Android's initialization flag and applied count.
// Keep those two source fields per helper; native components still own Update,
// serialized recipes, their cached summon lists and their elapsed timer.
internal static class AndroidSummonGrowth
{
 internal sealed class Runtime{internal bool Initialized;internal int Applied;}
 static readonly ConditionalWeakTable<MonoBehaviour,Runtime> States=new ConditionalWeakTable<MonoBehaviour,Runtime>();
 internal static readonly Type[] Types={typeof(BuffSummonDamageOverTime),typeof(BuffSummonAttackSpeedOverTime)};
 internal static readonly AccessTools.FieldRef<ShootingSummon,int> Damage=AccessTools.FieldRefAccess<ShootingSummon,int>("baseDamage");
 internal static readonly AccessTools.FieldRef<AttackingSummon,float> Cooldown=AccessTools.FieldRefAccess<AttackingSummon,float>("attackCooldown");
 internal static Runtime Get(MonoBehaviour owner)=>States.GetValue(owner,_=>new Runtime());
 internal static bool IsGrowth(Type type)=>type==Types[0]||type==Types[1];
 internal static string Target(MonoBehaviour owner)=>(string)AccessTools.Field(owner.GetType(),"SummonTypeID").GetValue(owner);
 static float Seconds(MonoBehaviour owner)=>(float)AccessTools.Field(owner.GetType(),"secondsPerBuff").GetValue(owner);
 static float Timer(MonoBehaviour owner)=>(float)AccessTools.Field(owner.GetType(),"_timer").GetValue(owner);
 internal static void Initialize(MonoBehaviour owner)
 {
  var state=Get(owner);if(state.Initialized)return;state.Initialized=true;
  var candidates=PlayerController.Instance.GetComponentsInChildren<ShootingSummon>(true);
  var summons=new List<ShootingSummon>();Plugin.SetField(owner,"_summons",summons);
  string id=Target(owner);foreach(var summon in candidates)if(summon.SummonTypeID==id)summons.Add(summon);
  state.Applied=0;
 }
 internal static ResumeRuntime.GrowthState Capture(MonoBehaviour owner,int ordinal)=>new ResumeRuntime.GrowthState
 {type=owner.GetType().FullName,ordinal=ordinal,count=Get(owner).Applied,remaining=Mathf.Max(0,Seconds(owner)-Timer(owner))};
 internal static void Restore(MonoBehaviour owner,float remaining,int count)
 {
  var state=Get(owner);if(!state.Initialized)Initialize(owner);
  var summons=(List<ShootingSummon>)AccessTools.Field(owner.GetType(),"_summons").GetValue(owner);
  int target=Math.Max(0,count),delta=unchecked(target-state.Applied);
  if(delta>0)
  {
   if(owner is BuffSummonDamageOverTime)
   {
    int amount=unchecked((int)AccessTools.Field(owner.GetType(),"damageBuff").GetValue(owner)*delta);
    foreach(var summon in summons)if(summon!=null)Damage(summon)=unchecked(Damage(summon)+amount);
   }
   else
   {
    float buff=(float)AccessTools.Field(owner.GetType(),"attackSpeedBuff").GetValue(owner);
    for(int i=0;i<delta;i++)foreach(var summon in summons)
     if(summon!=null&&Cooldown(summon)>.33f)Cooldown(summon)/=1+buff;
   }
  }
  // A lower restored count is recorded without undoing old growth in Android.
  state.Applied=target;Plugin.SetField(owner,"_timer",Mathf.Max(0,Seconds(owner)-Mathf.Max(0,remaining)));
 }
}
[HarmonyPatch]
class AndroidSummonGrowthInitialization
{
 static IEnumerable<MethodBase> TargetMethods()=>AndroidSummonGrowth.Types.Select(t=>AccessTools.Method(t,"Start"));
 static bool Prefix(MonoBehaviour __instance){AndroidSummonGrowth.Initialize(__instance);return false;}
}
[HarmonyPatch(typeof(BuffSummonDamageOverTime),"Update")]
class AndroidDragonDamageGrowth
{
 static bool Prefix(BuffSummonDamageOverTime __instance,List<ShootingSummon> ____summons,ref float ____timer,float ___secondsPerBuff,int ___damageBuff)
 {
  if(____timer>___secondsPerBuff)
  {
   foreach(var summon in ____summons)if(summon!=null)AndroidSummonGrowth.Damage(summon)=unchecked(AndroidSummonGrowth.Damage(summon)+___damageBuff);
   unchecked{AndroidSummonGrowth.Get(__instance).Applied++;}____timer-=___secondsPerBuff;
  }
  ____timer+=Time.deltaTime;return false;
 }
}
[HarmonyPatch(typeof(BuffSummonAttackSpeedOverTime),"Update")]
class AndroidDragonSpeedGrowth
{
 static bool Prefix(BuffSummonAttackSpeedOverTime __instance,List<ShootingSummon> ____summons,ref float ____timer,float ___secondsPerBuff,float ___attackSpeedBuff)
 {
  if(____timer>___secondsPerBuff)
  {
   foreach(var summon in ____summons)if(summon!=null&&AndroidSummonGrowth.Cooldown(summon)>.33f)AndroidSummonGrowth.Cooldown(summon)/=1+___attackSpeedBuff;
   unchecked{AndroidSummonGrowth.Get(__instance).Applied++;}____timer-=___secondsPerBuff;
  }
  ____timer+=Time.deltaTime;return false;
 }
}
