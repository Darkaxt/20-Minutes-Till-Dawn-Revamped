using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using flanne.Core;
using flanne.TitleScreen;
using UnityEngine.SceneManagement;
namespace TwentyMinutes.Modernization;

internal static class AndroidNotifications
{
 internal static void Clear()
 {
  var table=(Dictionary<string,Dictionary<object,List<Action<object,object>>>>)AccessTools.Field(typeof(NotificationCenter),"_table").GetValue(NotificationCenter.instance);
  // Source clears the existing lists and nested dictionaries as well as the
  // root table. Do not replace the table or clear the invoking-list set.
  foreach(string name in table.Keys.ToArray())
  {
   var senders=table[name];
   foreach(object sender in senders.Keys.ToArray())senders[sender].Clear();
   senders.Clear();
  }
  table.Clear();
 }
}

// These are the three original source Clear call sites. Inject at the native
// scene-load instruction, after its existing wait/unpause rather than at Enter.
[HarmonyPatch]
class AndroidTransitionNotifications
{
 static IEnumerable<MethodBase> TargetMethods()
 {
  yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(WaitToLoadIntoBattleState),"WaitToLoadCR"));
  yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(TransitionToRetryState),"WaitToReload"));
  yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(TransitionToTitleState),"WaitToExitToTitleScreen"));
 }
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,MethodBase __originalMethod)
 {
  int calls=0;
  foreach(var instruction in instructions)
  {
   if(instruction.operand is MethodInfo method&&method.DeclaringType==typeof(SceneManager)&&
    (method.Name==nameof(SceneManager.LoadScene)||method.Name==nameof(SceneManager.LoadSceneAsync)))
   {
    var clear=new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(AndroidNotifications),nameof(AndroidNotifications.Clear)));
    instruction.MoveLabelsTo(clear);yield return clear;
    var owner=__originalMethod.DeclaringType.DeclaringType;
    if(owner==typeof(TransitionToRetryState)||owner==typeof(TransitionToTitleState))
    {
     yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(AndroidBurnState),nameof(AndroidBurnState.Clear)));
     yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(AndroidPools),nameof(AndroidPools.Clear)));
     yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(AndroidCrowCollision),nameof(AndroidCrowCollision.Clear)));
     yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(AndroidSummonPulse),nameof(AndroidSummonPulse.Clear)));
    }
    calls++;
   }
   yield return instruction;
  }
  if(calls!=1)throw new InvalidOperationException("Unsupported native transition notification boundary");
 }
}
