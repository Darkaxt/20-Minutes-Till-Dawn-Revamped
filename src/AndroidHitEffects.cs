using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using flanne;
using UnityEngine;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

[HarmonyPatch]
class AndroidSparkLifecycle
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(ThunderOnHit),"Start"),AccessTools.Method(typeof(ThunderOnHit),"OnDestroy")};
 static bool Prefix(ThunderOnHit __instance,ref ThunderGenerator ___TGen,MethodBase __originalMethod)
 {
  var callback=(Action<object,object>)Delegate.CreateDelegate(typeof(Action<object,object>),__instance,AccessTools.Method(typeof(ThunderOnHit),"OnImpact"));
  if(__originalMethod.Name=="Start")
  {__instance.AddObserver(callback,Projectile.ImpactEvent);___TGen=ThunderGenerator.SharedInstance;}
  else __instance.RemoveObserver(callback,Projectile.ImpactEvent);
  return false;
 }
}
[HarmonyPatch(typeof(ThunderOnHit),"OnImpact")]
class AndroidSparkImpact
{
 static bool Prefix(object args,float ___chanceToHit,int ___baseDamage,ThunderGenerator ___TGen)
 {
  var target=(GameObject)args;
  if(target!=null&&target.layer==(int)TagLayerUtil.Enemy&&UnityEngine.Random.Range(0f,1f)<___chanceToHit)
   ___TGen.GenerateAt(target,___baseDamage);
  return false;
 }
}

// The producer already bridges source actor IDs to native GameObjects. Keep
// the complete source target/roll/dispatch order at this shared consumer too.
[HarmonyPatch(typeof(OnHitTrigger),"OnImpact")]
class AndroidHitTrigger
{
 static readonly System.Reflection.MethodInfo Raise=AccessTools.Method(typeof(Trigger),"RaiseTrigger");
 static bool Prefix(OnHitTrigger __instance,object args,float ___triggerChance,bool ___actionTargetPlayer)
 {
  var target=(GameObject)args;if(target==null)return false;
  var player=PlayerController.Instance;
  if(target!=player&&UnityEngine.Random.Range(0f,1f)<___triggerChance)
   Raise.Invoke(__instance,new object[]{___actionTargetPlayer?player.gameObject:target});
  return false;
 }
}

// Android Health.HPChange posts a typed notification only after a valid
// operation leaves the target alive. Windows' producer-only SummonOnHit event
// includes lethal/protected hits and misses other Summon-classified damage.
[HarmonyPatch(typeof(Health),nameof(Health.TakeDamage))]
class AndroidSurvivingHurt
{
 internal const string Event="Revamped.AndroidSurvivingHurt";
 static void Prefix(Health __instance,int damage,out bool __state)=>__state=AndroidHealthEvents.Source(__instance)==null&&!__instance.isDead&&!__instance.isInvincible.value&&damage>=0;
 static void Postfix(Health __instance,DamageType damageType,bool __state)
 {if(__state&&!__instance.isDead)__instance.PostNotification(Event,damageType);}
}
