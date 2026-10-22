using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

[Serializable]
public sealed class AndroidJusticeAcquire:flanne.PerkSystem.Action
{
 public override void Activate(GameObject target)
 {
  // The shipped Android prefab contains only this native-compatible helper.
  var helper=new GameObject("PF_GainMaxHPOnSmiteKill");helper.transform.SetParent(PlayerController.Instance.transform,false);
  var growth=helper.AddComponent<GainMaxHPOnSmiteKill>();Plugin.SetField(growth,"killsToGainMHP",500);Plugin.SetField(growth,"mhpGainCap",3);
 }
}
[HarmonyPatch]
class AndroidJusticeRecipe
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  if(RunResume.Key(__instance)!="justice_name")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidJusticeAcquire());
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
[HarmonyPatch(typeof(GainMaxHPOnSmiteKill),"OnSmiteKill")]
class AndroidJusticeExplicitHealth
{
 // Android has no StatModHealth listener. Its helper owns the single explicit
 // min(20,floor(...)) assignment, including the resulting native health events.
 static void Prefix(out bool __state){__state=AndroidHealthStat.ExplicitMaximum;AndroidHealthStat.ExplicitMaximum=true;}
 static Exception Finalizer(Exception __exception,bool __state){AndroidHealthStat.ExplicitMaximum=__state;return __exception;}
}
