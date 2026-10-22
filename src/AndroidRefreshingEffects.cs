using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using flanne;
using flanne.PowerupSystems;
using flanne.PowerupSystem;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

// Reconstruct the three source attachments using already-shipped native
// refreshing helpers. Each acquisition owns its own child and subscriptions.
[Serializable]
class AndroidRefreshAttachment:flanne.PerkSystem.Action
{
 public string key;
 public override void Activate(GameObject target)
 {
  var root=new GameObject("Revamped_"+key);root.transform.SetParent(PlayerController.Instance.transform,false);
  if(key=="fresh_clip_name")
  {
   var buff=root.AddComponent<DamageUpOnReload>();Plugin.SetField(buff,"damageBonus",.3f);Plugin.SetField(buff,"duration",1f);
  }
  else if(key=="anger_point_name")
  {
   var buff=root.AddComponent<FireRateOnHurt>();Plugin.SetField(buff,"fireRateBoost",.7f);Plugin.SetField(buff,"duration",30f);
  }
  else
  {
   var buff=root.AddComponent<TemporaryRefreshingBuff>();Plugin.SetField(buff,"duration",2);
   Plugin.SetField(buff,"statChanges",new[]{new StatChange{type=StatType.FireRate,value=.35f},new StatChange{type=StatType.MoveSpeed,value=.35f}});
   root.AddComponent<OnPlayerReloadEvent>().onReload=new UnityEvent();root.GetComponent<OnPlayerReloadEvent>().onReload.AddListener(buff.Activate);
  }
 }
}
[HarmonyPatch]
class AndroidRefreshRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack)),AccessTools.PropertyGetter(typeof(Powerup),nameof(Powerup.description))};
 static void Prefix(Powerup __instance,MethodBase __originalMethod)
 {
  string key=((LocalizedString)AccessTools.Field(typeof(Powerup),"nameStrID").GetValue(__instance)).key;
  if(key!="fresh_clip_name"&&key!="anger_point_name"&&key!="abby_perk_whirlwind_name")return;
  if(key=="fresh_clip_name")Plugin.Text["fresh_clip_description"]="Reloading grants +30% Bullet Damage for 1 second. Reloading again refreshes the duration.";
  if(__originalMethod.Name=="get_description")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidRefreshAttachment{key=key});
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
[HarmonyPatch]
class AndroidAngerPointStats
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(FireRateOnHurt),"AddBoost"),AccessTools.Method(typeof(FireRateOnHurt),"RemoveBoost")};
 static bool Prefix(FireRateOnHurt __instance,MethodBase __originalMethod)
 {
  if(__instance.name!="Revamped_anger_point_name")return true;
  float amount=__originalMethod.Name=="AddBoost"?.7f:-.7f;
  PlayerController.Instance.stats[StatType.BulletDamage].AddMultiplierBonus(amount);PlayerController.Instance.stats[StatType.FireRate].AddMultiplierBonus(amount);return false;
 }
}
