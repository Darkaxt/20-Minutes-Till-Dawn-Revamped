using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

public sealed class AndroidShootingFlags:MonoBehaviour
{
 public bool BulletEffect;
 public int BurnDamage;
}
[Serializable]
public sealed class AndroidGhostWizardAction:flanne.PerkSystem.Action
{
 public override void Activate(GameObject target)
 {
  foreach(var ghost in PlayerController.Instance.GetComponentsInChildren<ShootingSummon>())
  {
   if(ghost.SummonTypeID!="GhostFriend")continue;
   var flags=ghost.GetComponent<AndroidShootingFlags>();if(flags==null)flags=ghost.gameObject.AddComponent<AndroidShootingFlags>();
   flags.BulletEffect=true;flags.BurnDamage=6;
  }
 }
}
// The Android recipe has these two fields in addition to the native payload.
// Keep the values on the shot, so later actor edits cannot alter a live shot.
internal sealed class AndroidSummonRecipe:ProjectileRecipe
{
 internal bool BulletEffect;
 internal int BurnDamage;
}
public sealed class AndroidSummonProjectile:MonoBehaviour{public int BurnDamage;}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class AndroidSummonShotFlags
{
 static void Postfix(ProjectileRecipe __0,Projectile __result)
 {
  var recipe=__0 as AndroidSummonRecipe;var state=__result.GetComponent<AndroidSummonProjectile>();
  if(state==null&&recipe!=null)state=__result.gameObject.AddComponent<AndroidSummonProjectile>();
  if(state!=null)state.BurnDamage=recipe?.BurnDamage??0;
  // Native impact triggers require the player's GameObject and target payload.
  // This is the same source-to-native adapter used by Sight Magic.
  if(recipe!=null&&recipe.BulletEffect)__result.owner=PlayerController.Instance.gameObject;
 }
}
[HarmonyPatch(typeof(Projectile),"OnCollisionEnter2D")]
class AndroidSummonShotBurn
{
 static void BeforeImpact(Projectile projectile,Collision2D other)
 {
  var state=projectile.GetComponent<AndroidSummonProjectile>();if(state==null||state.BurnDamage<=0)return;
  if(other.gameObject.layer==(int)TagLayerUtil.Enemy)BurnSystem.SharedInstance.Burn(other.gameObject,state.BurnDamage);
 }
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  var owner=AccessTools.Field(typeof(Projectile),"owner");int count=0;
  foreach(var instruction in instructions)
  {
   // Preserve the existing projectile already on the evaluation stack.
   if(instruction.LoadsField(owner)){yield return new CodeInstruction(OpCodes.Ldarg_0);yield return new CodeInstruction(OpCodes.Ldarg_1);yield return CodeInstruction.Call(typeof(AndroidSummonShotBurn),nameof(BeforeImpact));count++;}
   yield return instruction;
  }
  if(count!=1)throw new InvalidOperationException("Unsupported native projectile impact owner boundary: "+count);
 }
}
