using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.RuneSystem;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(DropHPOnKillRune),"OnDeath")]
class AndroidHealingDeaths
{
 static bool Prefix(DropHPOnKillRune __instance,object sender,ref int ____counter)
 {
  var health=(Health)sender;
  if(++____counter>=__instance.killThreshold)
  {AccessTools.Method(typeof(DropHPOnKillRune),"DropHP").Invoke(__instance,new object[]{health.transform.position});____counter=0;}
  return false;
 }
}
[HarmonyPatch(typeof(EtherealRune),"OnDeath")]
class AndroidEtherealDeaths
{
 static bool Prefix(EtherealRune __instance,object sender,bool ____isActive,ref int ____counter,int ___killsToActivate)
 {
  if(____isActive)return false;
  var target=((Health)sender).gameObject;
  if(++____counter>=___killsToActivate)
  {__instance.StartCoroutine((IEnumerator)AccessTools.Method(typeof(EtherealRune),"EtherealStateCR").Invoke(__instance,null));____counter=0;}
  return false;
 }
}
public sealed class AndroidBodySoul:MonoBehaviour
{
 public int Limit,Grants;public float Remaining=300;
 void Update()
 {
  Remaining-=Time.deltaTime;if(Remaining>0)return;Remaining+=300;
  if(Grants>=Limit)return;Grants++;PlayerController.Instance.playerHealth.shp++;
 }
}
[HarmonyPatch(typeof(SoulAttunedRune),"Init")]
class AndroidBodySoulInit
{
 static bool Prefix(SoulAttunedRune __instance,int ___level,int ___shpMaxPerLevel)
 {
  int limit=3+___level*___shpMaxPerLevel;
  PlayerController.Instance.playerHealth.maxSHP+=limit;
  __instance.gameObject.AddComponent<AndroidBodySoul>().Limit=limit;return false;
 }
}
[HarmonyPatch(typeof(DropPrefabOverTime),"Start")]
class AndroidBodySoulPickupOwner
{static bool Prefix(DropPrefabOverTime __instance)=>__instance.GetComponent<SoulAttunedRune>()==null;}
[HarmonyPatch(typeof(ProjectileShieldRune),"OnCollisionEnter2D")]
class AndroidProjectileShieldCollision
{
 static bool Prefix(Collision2D collision)
 {
  if(collision.gameObject.layer!=(int)TagLayerUtil.EnemyProjectile)return false;
  // Windows pool ownership is retained by deactivation; Android Release returns
  // this same projectile to its pool before starting the shield cooldown.
  collision.gameObject.SetActive(false);return true;
 }
}
[HarmonyPatch]
class AndroidCloseCombatCollision
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(DamageBoostInRange),"OnCollisionEnter2D"),AccessTools.Method(typeof(DamageBoostInRange),"OnCollisionExit2D")};
 static bool Prefix(DamageBoostInRange __instance,Collision2D other,List<Health> ____targetsInRange,MethodBase __originalMethod)
 {
  var health=other.gameObject.GetComponent<Health>();if(health==null)return false;
  var callback=(System.Action<object,object>)Delegate.CreateDelegate(typeof(System.Action<object,object>),__instance,AccessTools.Method(typeof(DamageBoostInRange),"OnTweakDamage"));
  if(__originalMethod.Name=="OnCollisionEnter2D")
  {__instance.AddObserver(callback,Health.TweakDamageEvent,health);____targetsInRange.Add(health);}
  else{__instance.RemoveObserver(callback,Health.TweakDamageEvent,health);____targetsInRange.Remove(health);}
  return false;
 }
}
[HarmonyPatch]
class AndroidRegenerationRecipe
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  if(RunResume.Key(__instance)!="regeneration_name")return;
  var effects=(PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(__instance);
  Plugin.SetField(__instance,"effects",effects.Where(e=>AccessTools.Field(typeof(PerkEffect),"trigger").GetValue(e) is PeriodicTrigger).ToArray());
 }
}
