using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.UI;
namespace TwentyMinutes.Modernization.Validation;
[BepInPlugin("local.20mtd.modernization.validation","Modernization native validation","1.0.0")]
[BepInDependency(Plugin.Id)]
public class ValidationPlugin:BaseUnityPlugin
{
 void Awake()
 {
  if(Application.companyName!="CodexModernization" || Application.productName!="20MTD-Modernization-Validation" || string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MODERNIZATION_RESULT_DIR")))throw new InvalidOperationException("Validation fixture refuses the non-isolated player");
  Application.runInBackground=true;
  AudioListener.volume=0f;
  Plugin.Diagnostic+=RuntimeChecks.Log;
  gameObject.AddComponent<RuntimeChecks>();
  // Install error/cancellation observation before fixture patches, so a
  // rejected probe terminates the owned test instead of leaving a silent game.
  new Harmony("local.20mtd.modernization.validation").PatchAll(typeof(ValidationPlugin).Assembly);
 }
 void OnDestroy(){Plugin.Diagnostic-=RuntimeChecks.Log;}
}
// These suppression/probe patches belong only to the separate validation DLL.
// SaveSystem is intentionally untouched: the isolated native save path is tested.
[HarmonyPatch]
class NoSteam
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{"Awake","Update","OnApplicationQuit","UnlockAchievement","ClearAchievement","ClearAll","SubmitScore","ReplaceScore"}.Select(x=>AccessTools.Method(typeof(SteamIntegration),x));
 static bool Prefix()=>false;
}
[HarmonyPatch(typeof(LeaderboardUI),nameof(LeaderboardUI.SubmitAndShowsync))]
class NoValidationLeaderboard
{
 static bool Prefix(){RuntimeChecks.Log("fixture-steam-boundary","Endless leaderboard submission suppressed in isolated offline player only");return false;}
}
[HarmonyPatch]
class NoPreferenceWrites
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{"SetInt","SetFloat","SetString"}.Select(x=>AccessTools.Method(typeof(PlayerPrefs),x));
 static bool Prefix(string key)
 {
  string mode=Environment.GetEnvironmentVariable("MODERNIZATION_TEST");
  return mode.StartsWith("display-")&&(key=="Fullscreen"||key=="ResolutionIndex")||
   (mode=="damage-auto-hide-probe"||mode.StartsWith("combat-visibility-"))&&key=="ShowDamagePopup";
 }
}
[HarmonyPatch(typeof(OptionsSetter),nameof(OptionsSetter.Refresh))]
class TestViewport{static void Postfix(){string mode=Environment.GetEnvironmentVariable("MODERNIZATION_TEST");if(mode.StartsWith("display-")||mode.StartsWith("hud-presentation"))return;bool large=mode.EndsWith("1080")||Environment.GetEnvironmentVariable("MODERNIZATION_VIEWPORT")=="1080";Screen.SetResolution(large?1920:1280,large?1080:720,false);}}
[HarmonyPatch(typeof(GunEvolution),nameof(GunEvolution.Apply))]
class EvolutionProbe{static void Prefix(GunEvolution __instance)=>RuntimeChecks.Log("evolution-apply",__instance.name);}
[HarmonyPatch(typeof(Summon),"Start")]
class SummonInitializationObservation
{
 static void Postfix(Summon __instance)
 {
  int id=__instance.GetInstanceID();RuntimeChecks.StartedSummons.Add(id);
  if(__instance is AttackingSummon)RuntimeChecks.StartedSummonTimers[id]=(float)AccessTools.Field(typeof(AttackingSummon),"_timer").GetValue(__instance);
 }
}
// Android's NPC Hurt performs feedback directly; its serialized onHurt event
// is not the damage publisher. Observe the real source body, without publishing
// an invented gameplay event or changing any production callback.
[HarmonyPatch(typeof(AndroidHealthEvents),"Hurt")]
class SourceDamageObservation
{static void Prefix(AndroidHealthEvents __instance,int change)=>RuntimeChecks.ObservedSourceDamage(__instance.GetComponent<Health>(),Mathf.Abs(change));}
