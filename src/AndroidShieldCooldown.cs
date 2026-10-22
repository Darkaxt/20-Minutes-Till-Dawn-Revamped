using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Actions;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

// Android PreventDamage.ReduceCD / ReduceApplyTime / realCooldownTime.
// Initialized shields use the source game-clock owner. Preserve native damage
// and visual events, with explicit counted lifecycle/checkpoint reconstruction.
static class AndroidShieldCooldown
{
 static readonly AccessTools.FieldRef<PreventDamage,float> Timer=AccessTools.FieldRefAccess<PreventDamage,float>("_timer");
 public static void Advance(PreventDamage shield,float seconds)
 {
  var source=shield.GetComponent<AndroidShieldLifecycle>();if(source!=null){source.Advance(seconds);return;}
  if(shield.isActive)return;
  float remaining=shield.finalCooldown-Timer(shield)-seconds;
  Timer(shield)=shield.finalCooldown-(remaining<0?1:remaining);
 }
 public static void Reduce(PreventDamage shield,float seconds)
 {
  var source=shield.GetComponent<AndroidShieldLifecycle>();if(source!=null){source.Reduce(seconds);return;}
  float remaining=shield.finalCooldown-Timer(shield)-seconds;
  shield.cooldownTime-=seconds;
  if(!shield.isActive)Timer(shield)=shield.finalCooldown-(remaining<0?1:remaining);
 }
}
[System.Serializable]
class AndroidStalwartAction:flanne.PerkSystem.Action
{
 public override void Activate(GameObject target)=>AndroidShieldCooldown.Reduce(PlayerController.Instance.GetComponentInChildren<PreventDamage>(),60);
}
[HarmonyPatch]
class AndroidStalwartRecipe
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack)),AccessTools.PropertyGetter(typeof(Powerup),nameof(Powerup.description))};
 static void Prefix(Powerup __instance,MethodBase __originalMethod)
 {
  if(((LocalizedString)AccessTools.Field(typeof(Powerup),"nameStrID").GetValue(__instance)).key!="stalwart_shield_name")return;
  Plugin.Text["stalwart_shield_description"]="Holy Shield cooldown -60 seconds. Minimum cooldown: 10 seconds.";
  if(__originalMethod.Name=="get_description")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidStalwartAction());
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
[HarmonyPatch(typeof(PreventDamage),nameof(PreventDamage.finalCooldown),MethodType.Getter)]
class AndroidShieldMinimum
{
 static void Postfix(PreventDamage __instance,ref float __result)
 {var owner=__instance.GetComponent<AndroidShieldLifecycle>();__result=owner!=null?owner.Cooldown:Mathf.Max(10,__result);}
}
[HarmonyPatch(typeof(PreventDamage),nameof(PreventDamage.ReduceCDTimer))]
class AndroidShieldRecoveryAdvance
{
 static bool Prefix(PreventDamage __instance,float reduceAmount){AndroidShieldCooldown.Advance(__instance,reduceAmount);return false;}
}

