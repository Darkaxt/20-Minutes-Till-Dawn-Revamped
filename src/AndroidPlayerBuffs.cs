using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using flanne;
namespace TwentyMinutes.Modernization;

internal static class AndroidPlayerBuffCounters
{
 static readonly AccessTools.FieldRef<BuffPlayerStats,int> Stacks=AccessTools.FieldRefAccess<BuffPlayerStats,int>("_stacks");
 internal static int Count(BuffPlayerStats buff)=>Stacks(buff);
 internal static void Restore(BuffPlayerStats buff,int requested)
 {
  int target=Math.Max(0,requested);
  if(target==Stacks(buff))return;
  Clear(buff);
  for(int i=0;i<target;i++)buff.ApplyBuff();
 }
 internal static void Clear(BuffPlayerStats buff){while(Stacks(buff)>0)buff.RemoveBuff();}
}

[HarmonyPatch(typeof(BuffPlayerStats),"Start")]
class AndroidPlayerBuffInitialization
{static bool Prefix()=>false;}
[HarmonyPatch(typeof(BuffPlayerStats),nameof(BuffPlayerStats.ApplyBuff))]
class AndroidPlayerBuffApplication
{
 static void Prefix(ref int ____stacks)=>____stacks++;
}
[HarmonyPatch(typeof(BuffPlayerStats),nameof(BuffPlayerStats.RemoveBuff))]
class AndroidPlayerBuffRemoval
{static bool Prefix(ref int ____stacks){if(____stacks<=0)return false;____stacks--;return true;}}
[HarmonyPatch(typeof(BuffPlayerStats),nameof(BuffPlayerStats.RemoveStacks))]
class AndroidPlayerBuffClear
{static bool Prefix(BuffPlayerStats __instance){AndroidPlayerBuffCounters.Clear(__instance);return false;}}
[HarmonyPatch(typeof(BuffPlayerStats),nameof(BuffPlayerStats.AddStack))]
class AndroidPlayerBuffAddStack
{static bool Prefix(BuffPlayerStats __instance){__instance.ApplyBuff();return false;}}
[HarmonyPatch]
class AndroidPlayerBuffOwner
{
 static IEnumerable<MethodBase> TargetMethods()
 {yield return AccessTools.Method(typeof(BuffPlayerStats),nameof(BuffPlayerStats.ApplyBuff));yield return AccessTools.Method(typeof(BuffPlayerStats),nameof(BuffPlayerStats.RemoveBuff));}
 static PlayerController Player(BuffPlayerStats owner)=>PlayerController.Instance;
 static StatsHolder Stats(BuffPlayerStats owner)=>PlayerController.Instance.stats;
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  int players=0,stats=0;
  foreach(var instruction in instructions)
  {
   if(instruction.opcode==OpCodes.Ldfld&&instruction.operand is FieldInfo field&&field.DeclaringType==typeof(BuffPlayerStats))
   {
    if(field.Name=="player"){instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(AndroidPlayerBuffOwner),nameof(Player));players++;}
    else if(field.Name=="stats"){instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(AndroidPlayerBuffOwner),nameof(Stats));stats++;}
   }
   yield return instruction;
  }
  if(players!=3||stats!=6)throw new InvalidOperationException("Unsupported native player-buff owner boundary");
 }
}
