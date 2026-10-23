using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne.Core;
namespace TwentyMinutes.Modernization;

// Android exposes one Boolean, not Windows' nested BoolToggle counter. Keep
// the native property ABI for the existing Windows UI and gameplay callers.
internal static class AndroidPauseState
{internal static bool IsPaused;}
[HarmonyPatch(typeof(PauseController),"Awake")]
class AndroidPauseAwake
{
 static bool Prefix(PauseController __instance)
 {AndroidPauseState.IsPaused=false;PauseController.SharedInstance=__instance;return false;}
}
[HarmonyPatch(typeof(PauseController),"get_isPaused")]
class AndroidPauseValue
{
 static bool Prefix(ref bool __result)
 {__result=AndroidPauseState.IsPaused;return false;}
}
[HarmonyPatch(typeof(PauseController),nameof(PauseController.Pause),new Type[]{})]
class AndroidPauseBegin
{
 static bool Prefix(){Time.timeScale=0;AndroidPauseState.IsPaused=true;return false;}
}
[HarmonyPatch(typeof(PauseController),nameof(PauseController.UnPause))]
class AndroidPauseEnd
{
 static bool Prefix(){Time.timeScale=1;AndroidPauseState.IsPaused=false;return false;}
}
[HarmonyPatch(typeof(PauseController),nameof(PauseController.Pause),new[]{typeof(float)})]
class AndroidTimedPause
{
 static readonly MethodInfo Routine=AccessTools.Method(typeof(PauseController),"PauseCR");
 static bool Prefix(PauseController __instance,float duration,ref IEnumerator ___pauseCoroutine)
 {
  if(___pauseCoroutine!=null)__instance.StopCoroutine(___pauseCoroutine);
  ___pauseCoroutine=(IEnumerator)Routine.Invoke(__instance,new object[]{duration});
  __instance.StartCoroutine(___pauseCoroutine);return false;
 }
}
// Source Minipause has neither a cached controller nor a cooldown. Native
// serialized callbacks still enter here, using the current global owner.
[HarmonyPatch(typeof(Minipause),nameof(Minipause.Pause))]
class AndroidMinipause
{
 static bool Prefix(float duration){PauseController.SharedInstance.Pause(duration);return false;}
}
[HarmonyPatch]
class AndroidMinipauseLifecycle
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Minipause),"Start"),AccessTools.Method(typeof(Minipause),"Update")};
 static bool Prefix()=>false;
}
