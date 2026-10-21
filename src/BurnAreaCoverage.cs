using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Keep the native Update timer and Burn loop. Replace only its bounded physics
// query, retaining collider geometry and the component's own reusable buffer.
[HarmonyPatch(typeof(PeriodicallyBurnArea),"Update")]
class BurnAreaCoverage
{
 static readonly AccessTools.FieldRef<PeriodicallyBurnArea,Collider2D[]> Buffer=AccessTools.FieldRefAccess<PeriodicallyBurnArea,Collider2D[]>("_colliders");
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  var native=AccessTools.Method(typeof(Physics2D),nameof(Physics2D.OverlapCircleNonAlloc),new[]{typeof(Vector2),typeof(float),typeof(Collider2D[]),typeof(int)});
  int replaced=0;
  foreach(var instruction in instructions)
  {
   if(!instruction.Calls(native)){yield return instruction;continue;}
   var owner=new CodeInstruction(instruction){opcode=OpCodes.Ldarg_0,operand=null};
   yield return owner;
   yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(BurnAreaCoverage),nameof(Query)));
   replaced++;
  }
  if(replaced!=1)throw new InvalidOperationException("Unsupported PeriodicallyBurnArea physics query: expected exactly one native call");
 }
 static int Query(Vector2 position,float radius,Collider2D[] results,int layer,PeriodicallyBurnArea owner)
 {
  int count;
  while((count=Physics2D.OverlapCircleNonAlloc(position,radius,results,layer))==results.Length)
   Array.Resize(ref results,checked(results.Length*2));
  Buffer(owner)=results;
  return count;
 }
}
