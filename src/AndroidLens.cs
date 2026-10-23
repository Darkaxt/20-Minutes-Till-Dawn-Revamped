using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
namespace TwentyMinutes.Modernization;

internal static class AndroidLens
{
 internal static void Apply(BulletEnhanceSummon lens,Projectile shot)
 {
  shot.damage=lens.summonDamageMod.Modify(shot.damage)*lens.overallMultiplier*lens.damageMultiplier;
  shot.size=Mathf.Clamp(shot.transform.localScale.x*(lens.sizeMultiplier*lens.overallMultiplier),0,5);
  shot.bounce+=Mathf.FloorToInt(lens.additionalBounces*lens.overallMultiplier);
  var state=shot.GetComponent<AndroidProjectile>();state.IsBurn=lens.addBurn;state.BurnDamage=Mathf.FloorToInt(3*lens.overallMultiplier);
 }
}
[HarmonyPatch(typeof(Summon),"Start")]
class AndroidLensInitialData
{static void Prefix(Summon __instance){if(__instance is BulletEnhanceSummon lens&&lens.SummonTypeID=="MagicLens")lens.damageMultiplier=1.3f;}}
[Serializable]
public sealed class AndroidLensUpgrade:flanne.PerkSystem.Action
{
 public int Kind;
 public override void Activate(GameObject target)
 {
  foreach(var lens in PlayerController.Instance.GetComponentsInChildren<BulletEnhanceSummon>())
  {
   if(lens.SummonTypeID!="MagicLens")continue;
   if(Kind==0){lens.transform.localScale=Vector3.one*Mathf.Max(.2f,lens.transform.localScale.x*.5f);lens.overallMultiplier+=1;}
   else if(Kind==1)lens.additionalBounces+=2;
   else lens.addBurn=true;
  }
 }
}
[HarmonyPatch]
class AndroidLensRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);int kind=key=="focal_point_name"?0:key=="refraction_name"?1:key=="igniting_lens_name"?2:-1;if(kind<0)return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new flanne.PerkSystem.Triggers.InstantTrigger());Plugin.SetField(effect,"action",new AndroidLensUpgrade{Kind=kind});
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
[HarmonyPatch(typeof(BulletEnhanceSummon),"OnCollisionEnter2D")]
class AndroidLensNativeCollision
{static bool Prefix(UnityEngine.Collision2D __0)=>__0.gameObject.GetComponent<AndroidProjectile>()==null;}
