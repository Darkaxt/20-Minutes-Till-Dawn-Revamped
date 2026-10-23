using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source event helpers resolve the current player Health, rather than a
// parent-derived cached Health. Keep the native events, comparisons and their
// one-frame initial-check coroutines; only translate that owner lookup.
[HarmonyPatch]
class AndroidPlayerHealthEventOwner
{
 static readonly Type[] Owners={typeof(OnPlayerHPAboveHalf),typeof(OnPlayerHPBelowHalf),typeof(OnPlayerHealedEvent),typeof(OnPlayerHurtEvent)};
 static readonly Type[] Thresholds={typeof(OnPlayerHPAboveHalf),typeof(OnPlayerHPBelowHalf)};
 static IEnumerable<MethodBase> TargetMethods()
 {
  foreach(var type in Owners)
  {yield return AccessTools.Method(type,"Start");yield return AccessTools.Method(type,"OnDestroy");}
  foreach(var type in Thresholds)
  {yield return AccessTools.Method(type,"CheckHP");yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(type,"WaitToCheckHPCR"));}
 }
 static PlayerController Player(Component owner)=>PlayerController.Instance;
 static PlayerHealth Health(Component owner)=>PlayerController.Instance.playerHealth;
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,MethodBase __originalMethod)
 {
  int parent=0,health=0;
  foreach(var instruction in instructions)
  {
   if(instruction.operand is MethodInfo method&&method.DeclaringType==typeof(Component)&&
    method.Name=="GetComponentInParent"&&method.IsGenericMethod&&method.GetGenericArguments().SequenceEqual(new[]{typeof(PlayerController)}))
   {instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(AndroidPlayerHealthEventOwner),nameof(Player));parent++;}
   else if(instruction.opcode==OpCodes.Ldfld&&instruction.operand is FieldInfo field&&
    field.Name=="health"&&field.FieldType==typeof(PlayerHealth)&&Owners.Contains(field.DeclaringType))
   {instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(AndroidPlayerHealthEventOwner),nameof(Health));health++;}
   yield return instruction;
  }
  int expected=__originalMethod.Name=="CheckHP"?2:1;
  if(health!=expected||parent!=(__originalMethod.Name=="Start"?1:0))
   throw new InvalidOperationException("Unsupported native player-health event owner: "+__originalMethod.DeclaringType+"."+__originalMethod.Name);
 }
}
