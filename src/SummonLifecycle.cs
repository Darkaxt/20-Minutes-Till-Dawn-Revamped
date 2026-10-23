using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Android's getters resolve the current player even before this actor's Start.
// Native Orbital/Seeker Init and OnDestroy therefore subscribe/unsubscribe via
// the same source lookup, including unused pool members. No Start-state guard.
[HarmonyPatch(typeof(Summon),"get_summonAtkSpdMod")]
class AndroidSummonAttackStat
{static bool Prefix(ref StatMod __result){__result=PlayerController.Instance.stats[StatType.SummonAttackSpeed];return false;}}
[HarmonyPatch(typeof(Summon),"get_summonDamageMod")]
class AndroidSummonDamageStat
{static bool Prefix(ref StatMod __result){__result=PlayerController.Instance.stats[StatType.SummonDamage];return false;}}

[HarmonyPatch(typeof(Summon),"Start")]
class AndroidSummonParent
{
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  // The source Start has no player/stats cache writes. The retained native
  // consumers below resolve their player live, as do both stat getters.
  var code=instructions.ToList();var prefix=new[]{OpCodes.Ldarg_0,OpCodes.Ldsfld,OpCodes.Stfld,OpCodes.Ldarg_0,OpCodes.Ldarg_0,OpCodes.Ldfld,OpCodes.Ldfld,OpCodes.Stfld};
  if(!code.Take(8).Select(c=>c.opcode).SequenceEqual(prefix)||
   !Equals(code[2].operand,AccessTools.Field(typeof(Summon),"player"))||
   !Equals(code[7].operand,AccessTools.Field(typeof(Summon),"stats")))
   throw new InvalidOperationException("Unsupported native summon initialization boundary");
  code[8].labels.AddRange(code.Take(8).SelectMany(c=>c.labels));
  var original=AccessTools.Method(typeof(Transform),nameof(Transform.SetParent),new[]{typeof(Transform)});
  var source=AccessTools.Method(typeof(Transform),nameof(Transform.SetParent),new[]{typeof(Transform),typeof(bool)});int calls=0;
  foreach(var instruction in code.Skip(8))
  {
   if(instruction.Calls(original))
   {
    var localPose=new CodeInstruction(OpCodes.Ldc_I4_0);localPose.labels.AddRange(instruction.labels);instruction.labels.Clear();
    yield return localPose;instruction.operand=source;calls++;
   }
   yield return instruction;
  }
  if(calls!=1)throw new InvalidOperationException("Unsupported native summon parenting boundary");
 }
}

[HarmonyPatch]
class AndroidSummonPlayerLookup
{
 static IEnumerable<MethodBase> TargetMethods()
 {yield return AccessTools.Method(typeof(BulletEnhanceSummon),"ModifyProjectile");yield return AccessTools.Method(typeof(SporeFlower),"OnSporeHit");}
 static PlayerController Player(Summon owner)=>PlayerController.Instance;
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  var player=AccessTools.Field(typeof(Summon),"player");int reads=0;
  foreach(var instruction in instructions)
  {
   if(instruction.opcode==OpCodes.Ldfld&&Equals(instruction.operand,player))
   {instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(AndroidSummonPlayerLookup),nameof(Player));reads++;}
   yield return instruction;
  }
  if(reads!=1)throw new InvalidOperationException("Unsupported native summon player consumer");
 }
}

// These native overrides omit a base call which Android explicitly executes.
// StabbingSummon/BulletEnhanceSummon use native Summon.Init itself. SporeFlower
// and ThunderAttackSummon omit the source base call too: do not add their pulse.
[HarmonyPatch]
class AndroidSummonBaseInitialization
{
 static IEnumerable<MethodBase> TargetMethods()
 {yield return AccessTools.Method(typeof(Summon),"Init");yield return AccessTools.Method(typeof(OrbitalWeapon),"Init");yield return AccessTools.Method(typeof(SeekerWeapon),"Init");}
 static void Prefix(Summon __instance)=>AndroidSummonPulse.Initialize(__instance);
}
[HarmonyPatch(typeof(ProjectileSummon),"Init")]
class AndroidProjectileSummonInitialization
{
 static bool Prefix(ProjectileSummon __instance)
 {AndroidSummonPulse.Initialize(__instance);__instance.GetComponent<SeekEnemy>();return false;}
}
[HarmonyPatch(typeof(SetHitboxActiveSummon),"Init")]
class AndroidHitboxSummonInitialization
{
 // Source inherits Summon.Init; native's extra producer-hit subscription is
 // superseded by the source typed Health event, not forwarded a second time.
 static bool Prefix(SetHitboxActiveSummon __instance)
 {AndroidSummonPulse.Initialize(__instance);return false;}
}
[HarmonyPatch(typeof(ShootingSummon),"Init")]
class AndroidShootingSummonInitialization
{
 static bool Prefix(ShootingSummon __instance,GameObject ___projectilePrefab,ref ObjectPooler ___OP,ref ShootingCursor ___SC)
 {
  AndroidSummonPulse.Initialize(__instance);___OP=ObjectPooler.SharedInstance;
  ___OP.GetComponent<AndroidPools>().Register(___projectilePrefab.name,___projectilePrefab,-1);
  ___SC=ShootingCursor.Instance;return false;
 }
}
