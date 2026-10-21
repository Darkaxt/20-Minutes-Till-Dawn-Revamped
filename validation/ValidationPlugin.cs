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
  new Harmony("local.20mtd.modernization.validation").PatchAll(typeof(ValidationPlugin).Assembly);
  Plugin.Diagnostic+=RuntimeChecks.Log;
  gameObject.AddComponent<RuntimeChecks>();
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
 static bool Prefix()=>false;
}
[HarmonyPatch(typeof(OptionsSetter),nameof(OptionsSetter.Refresh))]
class TestViewport{static void Postfix(){Screen.SetResolution(1280,720,false);}}
[HarmonyPatch(typeof(GunEvolution),nameof(GunEvolution.Apply))]
class EvolutionProbe{static void Prefix(GunEvolution __instance)=>RuntimeChecks.Log("evolution-apply",__instance.name);}
