using HarmonyLib;
using System.Reflection;
using System.Collections.Generic;
using flanne;
using flanne.RuneSystem;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(GrowthRune),"Init")]
class GrowthExperienceBonus
{
 // Runtime clone only: keep the native XP modifier, heal-on-level callback,
 // rune level, spending and subscriptions. The current tooltip promises 4%.
 static void Prefix(ref float ___xpMultiplierPerLevel)=>___xpMultiplierPerLevel=.04f;
}
[HarmonyPatch(typeof(GrowthRune),"OnLevelChanged")]
class GrowthRestoreReward
{static bool Prefix()=>!RunResume.Restoring;}
[HarmonyPatch]
class ElementalBarrageStats
{
 static IEnumerable<MethodBase> TargetMethods()
 {yield return AccessTools.Method(typeof(ElementalBarrageRune),"ActivateBonus");yield return AccessTools.Method(typeof(ElementalBarrageRune),"DeactivateBonus");}
 static bool Prefix(MethodBase __originalMethod,float ___bonusStatMulti)
 {
  float amount=__originalMethod.Name=="ActivateBonus"?___bonusStatMulti:-___bonusStatMulti;
  var stats=PlayerController.Instance.stats;stats[StatType.FireRate].AddMultiplierBonus(amount);stats[StatType.ReloadRate].AddMultiplierBonus(amount);return false;
 }
}
