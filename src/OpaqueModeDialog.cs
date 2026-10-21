using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne.UIExtensions;
namespace TwentyMinutes.Modernization;

// AlphaTransition owns the native tween. Give Show/Hide their final alpha before
// it starts so a visible dialog never exposes the loadout through its dark fill.
// Its native scale transition, input blocking and lifecycle remain unchanged.
[HarmonyPatch]
class OpaqueModeDialog
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(AlphaTransition),nameof(AlphaTransition.Show)),AccessTools.Method(typeof(AlphaTransition),nameof(AlphaTransition.Hide))};
 static void Prefix(AlphaTransition __instance,MethodBase __originalMethod)
 {
  var panel=Progression.Title?.modeSelectPanel;if(panel==null||!__instance.transform.IsChildOf(panel.transform))return;
  __instance.GetComponent<CanvasGroup>().alpha=__originalMethod.Name==nameof(AlphaTransition.Show)?1:0;
 }
}
