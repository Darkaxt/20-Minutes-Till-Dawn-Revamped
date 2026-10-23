using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using flanne;
using flanne.Pickups;
using flanne.PowerupSystem;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(BuffOnXP),"Start")]
class AndroidXPBuffInitialization
{
 static bool Prefix(BuffOnXP __instance)
 {
  // Source Start only registers this observer; it does not cache parent stats.
  var handler=(Action<object,object>)Delegate.CreateDelegate(typeof(Action<object,object>),__instance,AccessTools.Method(typeof(BuffOnXP),"OnXPPickup"));
  __instance.AddObserver(handler,XPPickup.XPPickupEvent);
  return false;
 }
}

[HarmonyPatch]
class AndroidXPBuffStatOwner
{
 static MethodBase TargetMethod()=>AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(BuffOnXP),"StartBuffCR"));
 static StatsHolder Stats(BuffOnXP owner)=>PlayerController.Instance.stats;
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  foreach(var instruction in instructions)
  {
   if(instruction.opcode==OpCodes.Ldfld&&instruction.operand is FieldInfo field&&field.DeclaringType==typeof(BuffOnXP)&&field.Name=="stats")
   {
    instruction.opcode=OpCodes.Call;
    instruction.operand=AccessTools.Method(typeof(AndroidXPBuffStatOwner),nameof(Stats));
   }
   yield return instruction;
  }
 }
}
