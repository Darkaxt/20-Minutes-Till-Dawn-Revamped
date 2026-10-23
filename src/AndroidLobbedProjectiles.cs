using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.AISpecials;
namespace TwentyMinutes.Modernization;

// The native scatter/windup and player-spore arc already match. Enemy flight
// shares that arc but owns three source pools rather than disposable objects.
internal static class AndroidLobbedProjectiles
{
 internal static readonly MethodInfo Land=AccessTools.Method(typeof(EnemyAreaProjectile),"OnTargetReached");
 internal static bool OwnsTemplate(GameObject prefab)=>prefab.name=="RhogogSporeExplosion";
 internal static void ConfigureTemplate(GameObject owned)
 {
  if(!OwnsTemplate(owned))return;
  Plugin.SetField(owned.GetComponent<TimeToLive>(),"lifetime",.3f);
  UnityEngine.Object.DestroyImmediate(owned.transform.Find("Damage").GetComponent<TimeToLive>());
  // No enable-time reset: the source child has no script to reactivate it
  // after PlayerCollider's name-based release of the collision child.
 }
}
[HarmonyPatch(typeof(AIRandomAreaProjectileSpecial),"InitProjectile")]
class AndroidLobbedEnemyProducer
{
 static bool Prefix(Vector2 spawnPos,Vector2 hitPos,float duration,EnemyAreaProjectile ___projectilePrefab)
 {
  var prefab=___projectilePrefab.gameObject;var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();
  pool.Register(prefab.name,prefab);
  var projectile=pool.Get(prefab.name).GetComponent<EnemyAreaProjectile>();
  projectile.transform.position=spawnPos;projectile.TargetPos(hitPos,duration);return false;
 }
}
[HarmonyPatch(typeof(EnemyAreaProjectile),nameof(EnemyAreaProjectile.TargetPos))]
class AndroidLobbedEnemyFlight
{
 static bool Prefix(EnemyAreaProjectile __instance,Vector2 pos,float duration,Transform ___lobTransform,GameObject ___indicatorPrefab,ref GameObject ____indicator)
 {
  ___lobTransform.LeanMoveLocalY(2,duration*.5f).setLoopPingPong(1).setEase((LeanTweenType)6);
  ___lobTransform.eulerAngles=new Vector3(0,0,UnityEngine.Random.Range(0f,360f));
  LeanTween.move(__instance.gameObject,pos,duration).setOnComplete((Action)Delegate.CreateDelegate(typeof(Action),__instance,AndroidLobbedProjectiles.Land));
  var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();pool.Register(___indicatorPrefab.name,___indicatorPrefab);
  ____indicator=pool.Get(___indicatorPrefab.name,pos);return false;
 }
}
[HarmonyPatch(typeof(EnemyAreaProjectile),"OnTargetReached")]
class AndroidLobbedEnemyLanding
{
 static bool Prefix(EnemyAreaProjectile __instance,GameObject ___damagePrefab,GameObject ____indicator,SoundEffectSO ___hitSFX)
 {
  ___hitSFX?.Play();
  var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();pool.Register(___damagePrefab.name,___damagePrefab);
  pool.Get(___damagePrefab.name,____indicator.transform.position);
  if(____indicator)AndroidPools.Return(____indicator);
  AndroidPools.Return(__instance.gameObject);return false;
 }
}
