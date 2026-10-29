using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using flanne;
using flanne.RuneSystem;
namespace TwentyMinutes.Modernization;

internal static class AndroidRuneStartup
{
 internal static void Apply()
 {
  // Revamped's character loadout applies in every mode, including Endless.
  // This explicitly replaces the shipped Endless starting-rune restriction.
  var selected=Loadout.RuneSelection;
  // This is the existing native selection contract: null means no selection.
  if(selected==null)return;
  for(int i=0;i<selected.Count;i++)
  {var rune=selected[i];if(rune.level>=1)rune.Apply(PlayerController.Instance);}
 }
}
[HarmonyPatch(typeof(LoadoutInitializer),"Start")]
class AndroidRunesLeaveLoadout
{
 static readonly MethodInfo Apply=AccessTools.Method(typeof(RuneData),nameof(RuneData.Apply));
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  int removed=0;
  foreach(var instruction in instructions)
  {
   if(instruction.Calls(Apply))
   {
    // Preserve the native selection enumeration/ABI, but consume its two
    // arguments without applying. The map owner applies them at source order.
    instruction.opcode=OpCodes.Pop;instruction.operand=null;yield return instruction;
    yield return new CodeInstruction(OpCodes.Pop);removed++;continue;
   }
   yield return instruction;
  }
  if(removed!=1)throw new InvalidOperationException("Unsupported native loadout rune application boundary");
 }
}
[HarmonyPatch(typeof(MapInitializer),"Start")]
class AndroidNativeMapRunes
{
 static readonly MethodInfo Initialize=AccessTools.Method(typeof(PowerupGenerator),nameof(PowerupGenerator.InitPowerupPool));
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  int inserted=0;
  foreach(var instruction in instructions)
  {
   if(instruction.Calls(Initialize))
   {
    // The original generator/count arguments remain on the stack. Move the
    // call-site metadata to the inserted first instruction at this boundary.
    var apply=new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(AndroidRuneStartup),nameof(AndroidRuneStartup.Apply)));
    instruction.MoveLabelsTo(apply);
    instruction.MoveBlocksTo(apply);
    yield return apply;inserted++;
   }
   yield return instruction;
  }
  if(inserted!=1)throw new InvalidOperationException("Unsupported native map rune application boundary");
 }
}
