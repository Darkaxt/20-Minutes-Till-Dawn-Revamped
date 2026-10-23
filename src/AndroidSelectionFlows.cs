using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(PowerupMenuState),"OnConfirm")]
class AndroidLevelUpConfirmation
{
 static readonly MethodInfo EndAnimation=AccessTools.Method(typeof(PowerupMenuState),"EndLevelUpAnimationCR");
 static bool Prefix(PowerupMenuState __instance,Powerup __1)
 {
  // Both versions start the native closing animation before applying the
  // choice. Source's SDK analytics call is outside the gameplay scope.
  __instance.StartCoroutine((IEnumerator)EndAnimation.Invoke(__instance,null));
  PlayerController.Instance.playerPerks.Equip(__1);
  if(!FallbackPowerups.IsFallback(__1))PowerupGenerator.Instance.RemoveFromPool(__1);
  return false;
 }
}

[HarmonyPatch(typeof(DevilDealState),"OnConfirm")]
class AndroidDevilConfirmation
{
 static bool Prefix(Powerup __1,GameController ___owner)
 {
  var player=PlayerController.Instance;
  player.playerPerks.Equip(__1);
  // The six source isRepeatable assets are exactly the existing fallback set.
  // This condition belongs to confirmation, not to every pool-removal caller.
  if(!FallbackPowerups.IsFallback(__1))PowerupGenerator.Instance.RemoveFromDevilPool(__1);
  // Source routes dead players through its reward revival menu. That menu is
  // excluded by M7; its no-reward path retains the native death transition.
  if(player.playerHealth.hp<1)___owner.ChangeState<PlayerDeadState>();
  else ___owner.ChangeState<CombatState>();
  return false;
 }
}

// Both closing iterators already branch to native death at exactly zero HP.
// Source tests below one. Keep the existing branches and change only the
// integer predicate feeding them, after their original real-time closing wait.
[HarmonyPatch]
class AndroidSelectionDeathTransition
{
 static IEnumerable<MethodBase> TargetMethods()
 {
  yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(ChestState),"WaitToLeaveMenu"));
  yield return AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(PowerupMenuState),"EndLevelUpAnimationCR"));
 }
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  foreach(var instruction in instructions)
  {
   yield return instruction;
   if(instruction.operand is MethodInfo method&&method.DeclaringType==typeof(PlayerHealth)&&method.Name=="get_hp")
   {
    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
    yield return new CodeInstruction(OpCodes.Cgt);
   }
  }
 }
}
