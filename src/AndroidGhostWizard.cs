using System;
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
internal class AndroidProjectileRecipe:ProjectileRecipe
{
 internal bool BulletEffect=true;
 internal int BurnDamage;
 internal float? Radius;
}
internal sealed class AndroidSummonRecipe:AndroidProjectileRecipe{}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class AndroidSummonShotFlags
{
 static void Postfix(ProjectileRecipe __0,Projectile __result)
 {
  if(__result.gameObject.layer==LayerMask.NameToLayer("EnemyProjectile"))return;
  var state=__result.GetComponent<AndroidProjectile>();if(state==null)state=__result.gameObject.AddComponent<AndroidProjectile>();
  state.Configure(__0);
 }
}
