using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source GetRandomEnmey uses the player center even when callers pass a summon
// position. Select once from strict pooled centers, without collider extents.
internal static class AndroidSummonTargets
{
 internal static GameObject Random(float x,float y)
 {
  var center=PlayerController.Instance.transform.position;
  var targets=ObjectPooler.SharedInstance.pooledObjectsDictionary.Values.SelectMany(list=>list).Where(actor=>actor!=null&&actor.activeInHierarchy&&actor.layer==(int)TagLayerUtil.Enemy&&Mathf.Abs(actor.transform.position.x-center.x)<x&&Mathf.Abs(actor.transform.position.y-center.y)<y).Distinct().ToArray();
  return targets.Length==0?null:targets[UnityEngine.Random.Range(0,targets.Length)];
 }
}
[HarmonyPatch(typeof(AttackingSummon),"get_finalAttackCooldown")]
class AndroidAttackCooldown
{
 static bool Prefix(AttackingSummon __instance,float ___attackCooldown,ref float __result)
 {
  if(!(__instance is StabbingSummon)&&!(__instance is ShootingSummon))return true;
  var bestFriends=__instance.GetComponent<AndroidBestFriends>();float effect=bestFriends!=null?bestFriends.Factor:__instance.attackSpeedMod.Modify(1);
  __result=Mathf.Clamp(__instance.summonAtkSpdMod.ModifyInverse(___attackCooldown)/effect,.2f,float.MaxValue);return false;
 }
}
[HarmonyPatch(typeof(AttackingSummon),"Update")]
class AndroidAttackInterval
{
 static readonly MethodInfo Cooldown=AccessTools.PropertyGetter(typeof(AttackingSummon),"finalAttackCooldown"),Stab=AccessTools.Method(typeof(StabbingSummon),"Attack"),Shoot=AccessTools.Method(typeof(ShootingSummon),"Attack");
 static bool Prefix(AttackingSummon __instance,ref float ____timer,Animator ___animator,SoundEffectSO ___attackSoundFX)
 {
  if(!(__instance is StabbingSummon)&&!(__instance is ShootingSummon))return true;
  ____timer+=Time.deltaTime;float interval=(float)Cooldown.Invoke(__instance,null);if(____timer<interval)return false;
  ____timer-=interval;var attack=__instance is StabbingSummon?Stab:Shoot;
  if((bool)attack.Invoke(__instance,null)){___animator?.SetTrigger("Attack");___attackSoundFX?.Play();}return false;
 }
}
[HarmonyPatch(typeof(ShootingSummon),"Attack")]
class AndroidShootingAttack
{
 static readonly MethodInfo Shoot=AccessTools.Method(typeof(ShootingSummon),"Shoot");
 static bool Prefix(ShootingSummon __instance,ref bool __result)
 {
  if(__instance.targetMouse)return true;
  var target=AndroidSummonTargets.Random(9,6);__result=target!=null;
  if(__result)Shoot.Invoke(__instance,new object[]{(Vector2)(target.transform.position-__instance.transform.position)});
  return false;
 }
}
[HarmonyPatch(typeof(ShootingSummon),"GetProjectileRecipe")]
class AndroidShootingRecipe
{
 static bool Prefix(ShootingSummon __instance,GameObject ___projectilePrefab,ref ProjectileRecipe __result)
 {
  var flags=__instance.GetComponent<AndroidShootingFlags>();
  __result=new AndroidSummonRecipe{objectPoolTag=___projectilePrefab.name,BulletEffect=flags!=null&&flags.BulletEffect,BurnDamage=flags?.BurnDamage??0,
   damage=__instance.summonDamageMod.Modify(__instance.inheritPlayerDamage?PlayerController.Instance.gun.damage:__instance.baseDamage),
   projectileSpeed=__instance.projectileSpeed,size=1,knockback=__instance.knockback,bounce=__instance.bounce,piercing=__instance.pierce,owner=__instance.gameObject};
  return false;
 }
}
