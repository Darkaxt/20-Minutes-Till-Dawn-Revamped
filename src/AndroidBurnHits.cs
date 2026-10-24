using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source collision dispatch is layer-based. Projectile samplers already call
// BurnOnCollision in original component order; standalone fire effects enter
// through this Unity callback instead.
[HarmonyPatch(typeof(BurnOnCollision),"OnCollisionEnter2D")]
class AndroidBurnCollision
{
 static bool Prefix(BurnOnCollision __instance,Collision2D __0,int ___burnDamage)
 {
  var target=__0.gameObject;
  if(__instance.GetComponent<AndroidProjectile>()==null&&target.layer==(int)TagLayerUtil.Enemy)
   BurnSystem.SharedInstance.Burn(target,___burnDamage);
  return false;
 }
}

internal static class AndroidBurnHitOwners
{
 internal static string Model(BurnOnHit owner)=>owner.transform.parent?.name.Replace("(Clone)","");
 internal static bool Removed(BurnOnHit owner)
 {string model=Model(owner);return model=="PF_FlameCannon"||model=="PF_CombustionGun";}
 internal static Action<object,object> Handler(BurnOnHit owner)=>(Action<object,object>)Delegate.CreateDelegate(typeof(Action<object,object>),owner,AccessTools.Method(typeof(BurnOnHit),"OnImpact"));
}

// These two native gun-model helpers are absent from the original Android
// models. Their burning belongs to the source collision-effect prefabs.
[HarmonyPatch]
class AndroidBurnHitLifecycle
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(BurnOnHit),"Start"),AccessTools.Method(typeof(BurnOnHit),"OnDestroy")};
 static bool Prefix(BurnOnHit __instance,MethodBase __originalMethod)
 {
  if(AndroidBurnHitOwners.Removed(__instance))return false;
  if(__originalMethod.Name=="Start")
  {
   if(AndroidBurnHitOwners.Model(__instance)=="PF_MeteorGun")__instance.burnDamge=6;
   __instance.AddObserver(AndroidBurnHitOwners.Handler(__instance),Projectile.ImpactEvent);
  }
  else __instance.RemoveObserver(AndroidBurnHitOwners.Handler(__instance),Projectile.ImpactEvent);
  return false;
 }
}
[HarmonyPatch(typeof(BurnOnHit),"OnImpact")]
class AndroidBurnHit
{
 static bool Prefix(BurnOnHit __instance,object args)
 {
  if(AndroidBurnHitOwners.Removed(__instance))return false;
  // Native notifications carry the GameObject represented by Android's ID.
  var target=(GameObject)args;if(target==null)return false;
  if(target.layer==(int)TagLayerUtil.Enemy&&UnityEngine.Random.Range(0f,1f)<__instance.chanceToHit)
   BurnSystem.SharedInstance.Burn(target,__instance.burnDamge);
  return false;
 }
}
[HarmonyPatch(typeof(BurnOnHitBelowAmmoThreshold),"Start")]
class AndroidOverheatBurnValues
{
 static void Prefix(BurnOnHitBelowAmmoThreshold __instance)
 {__instance.burnDamge=0;__instance.burnDamageBelowThreshold=6;}
}
[HarmonyPatch(typeof(BurnOnHitBelowAmmoThreshold),"OnImpact")]
class AndroidOverheatBurnHit
{
 static bool Prefix(BurnOnHitBelowAmmoThreshold __instance,object args,BurnSystem ___BurnSys,bool ____active)
 {
  // Source has no tag filter or zero-damage shortcut in this callback.
  ___BurnSys.Burn((GameObject)args,____active?__instance.burnDamageBelowThreshold:__instance.burnDamge);
  return false;
 }
}
