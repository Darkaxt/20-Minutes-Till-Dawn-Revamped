using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using flanne;
using flanne.RuneSystem;
namespace TwentyMinutes.Modernization;

// The native tooltip and source Android Init both subtract one second per
// rank. Preserve native spawning and stat composition; correct the two signs
// at the actual flat-modifier calls rather than changing the entire cooldown.
[HarmonyPatch(typeof(SeismicWardRune),"Init")]
class SeismicWardCooldown
{
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  var add=AccessTools.Method(typeof(StatMod),nameof(StatMod.AddFlatBonus),new[]{typeof(int)});int corrected=0;
  foreach(var instruction in instructions)
  {
   if(instruction.Calls(add))
   {
    // Copy the original entry labels/blocks to the inserted operation, then
    // emit the call without a second copy of those labels.
    yield return new CodeInstruction(instruction){opcode=OpCodes.Neg,operand=null};
    yield return new CodeInstruction(instruction.opcode,instruction.operand);
    corrected++;continue;
   }
   yield return instruction;
  }
  if(corrected!=2)throw new InvalidOperationException("Unsupported Seismic Ward Init: expected two native flat cooldown calls.");
 }
}

[HarmonyPatch(typeof(GrowthRune),"Init")]
class GrowthExperienceBonus
{
 // Runtime clone only: keep the native XP modifier, heal-on-level callback,
 // rune level, spending and subscriptions. The current tooltip promises 4%.
 static void Prefix(ref float ___xpMultiplierPerLevel)=>___xpMultiplierPerLevel=.04f;
}
