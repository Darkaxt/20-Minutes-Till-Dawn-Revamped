using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

public sealed class EnergizedReload:MonoBehaviour
{
 float contribution;StatMod reload;
 void Start(){reload=PlayerController.Instance.stats[StatType.ReloadRate];}
 void LateUpdate(){float next=ElementalControl.VisibleStunned()*.01f;reload.AddMultiplierBonus(next-contribution);contribution=next;}
 void OnDisable(){if(reload!=null&&contribution!=0)reload.AddMultiplierBonus(-contribution);contribution=0;}
}
[System.Serializable]
public sealed class AndroidEnergizedAttach:flanne.PerkSystem.Action
{public override void Activate(GameObject target){var root=new GameObject("EnergizedReload");root.transform.SetParent(target.transform,false);root.AddComponent<EnergizedReload>();}}
[HarmonyPatch]
class AndroidEnergizedRecipe
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  if(RunResume.Key(__instance)!="energized_name")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidEnergizedAttach());Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
