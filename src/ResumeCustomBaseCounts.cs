using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using flanne;
using flanne.PerkSystem.Actions;
namespace TwentyMinutes.Modernization;

public static partial class ResumeRuntime
{
 internal static void PrepareCustomBaseHistory(RunResume.Snapshot snapshot)
 {
  if(snapshot.custom==null)return;
  var recorder=RunRecorder.Instance;
  // Missing older history is rebuilt from the saved current settings under
  // the user's source-reconstruction policy. New replayed operands then form
  // known history; existing saved operands are substituted at the mutation.
  recorder.CustomRageAmmo=new List<int>();
  recorder.CustomFusilladeProjectiles=new List<int>();
 }
}

// Custom permits changing base weapon values after a pick. Preserve each
// actual native action operand; totals alone lose the pickup-time inputs.
// Override that same operand during Continue, without an extra stat mutation
// or notification. Ordinary runs retain the native operand unchanged.
[HarmonyPatch]
class CustomBaseCountHistory
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{
  AccessTools.Method(typeof(TripleBaseAmmoAction),nameof(TripleBaseAmmoAction.Activate)),
  AccessTools.Method(typeof(DoubleBaseNumProjectilesAction),nameof(DoubleBaseNumProjectilesAction.Activate))};
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,MethodBase __originalMethod)
 {
  var code=instructions.ToList();
  var mutation=code.Single(i=>i.Calls(AccessTools.Method(typeof(StatMod),nameof(StatMod.AddFlatBonus))));
  mutation.opcode=OpCodes.Call;
  mutation.operand=AccessTools.Method(typeof(CustomBaseCountHistory),__originalMethod.DeclaringType==typeof(TripleBaseAmmoAction)?nameof(Ammo):nameof(Projectiles));
  return code;
 }
 static void Ammo(StatMod stat,int amount)
 {
  if(CustomMode.Active)
  {
   var recorder=RunRecorder.Instance;
   var saved=recorder.Rebuilding?RunResume.Pending.runtime.customRageAmmo:null;
   if(saved!=null)amount=saved[recorder.CustomRageAmmo.Count];
   recorder.CustomRageAmmo?.Add(amount);
  }
  stat.AddFlatBonus(amount);
 }
 static void Projectiles(StatMod stat,int amount)
 {
  if(CustomMode.Active)
  {
   var recorder=RunRecorder.Instance;
   var saved=recorder.Rebuilding?RunResume.Pending.runtime.customFusilladeProjectiles:null;
   if(saved!=null)amount=saved[recorder.CustomFusilladeProjectiles.Count];
   recorder.CustomFusilladeProjectiles?.Add(amount);
  }
  stat.AddFlatBonus(amount);
 }
}
