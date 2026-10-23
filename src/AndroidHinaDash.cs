using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using flanne;
using flanne.CharacterPassives;
namespace TwentyMinutes.Modernization;

// Keep native desktop skill input/cooldown handling. Android's derived Hina
// callbacks resolve the current player rather than a parent-derived cache.
[HarmonyPatch(typeof(ShadowClonePassive),"PerformSkill")]
class AndroidHinaUse
{
 static readonly MethodInfo Dash=AccessTools.Method(typeof(ShadowClonePassive),"DashCR");
 static bool Prefix(ShadowClonePassive __instance,GameObject ___shadowClone,TimeToLive ___shadowCloneLifetime,UnityEvent ___onUse)
 {
  __instance.StartCoroutine((IEnumerator)Dash.Invoke(__instance,new object[]{PlayerController.Instance}));
  AndroidHinaSummoning.Spawn(___shadowClone,___shadowCloneLifetime);___onUse?.Invoke();return false;
 }
}
[HarmonyPatch(typeof(ShadowClonePassive),"SummonShadowClone")]
class AndroidHinaSummoning
{
 static bool Prefix(GameObject ___shadowClone,TimeToLive ___shadowCloneLifetime)
 {Spawn(___shadowClone,___shadowCloneLifetime);return false;}
 internal static void Spawn(GameObject clone,TimeToLive lifetime)
 {
  // The original source method has this player check and exception boundary.
  // Its conditional GameLog maps to the existing optional native diagnostic.
  try
  {
   if(PlayerController.Instance==null)return;
   clone.transform.position=PlayerController.Instance.transform.position;
   clone.SetActive(true);lifetime.Refresh();
  }
  catch(Exception error){Plugin.Trace("shadow-clone-spawn-error",new{message=error.Message});}
 }
}
