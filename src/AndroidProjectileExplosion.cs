using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source Start/OnDestroy are empty. It uses the scene-owned explosion pool,
// without Windows' per-projectile registration or scene/quit suppression.
[HarmonyPatch]
class AndroidExplosionLifecycle
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(ExplosiveProjectile),"Start"),AccessTools.Method(typeof(ExplosiveProjectile),"OnDestroy"),AccessTools.Method(typeof(ExplosiveProjectile),"OnApplicationQuit"),AccessTools.Method(typeof(ExplosiveProjectile),"ChangedActiveScene")};
 static bool Prefix()=>false;
}
[HarmonyPatch(typeof(ExplosiveProjectile),"OnDisable")]
class AndroidExplosionDisable
{
 static bool Prefix(ExplosiveProjectile __instance){__instance.SpawnExplosive();return false;}
}
[HarmonyPatch(typeof(ExplosiveProjectile),"SpawnExplosive")]
class AndroidExplosionSpawn
{
 static bool Prefix(ExplosiveProjectile __instance,Vector3 ____explosionSize,ExplosionShake2D ___cameraShaker,SoundEffectSO ___soundFX)
 {
  // All three original source grenade variants use GrenadeExplosion. Its
  // native prefab contact category/proc flag is set by AndroidContacts.
  var explosion=ObjectPooler.SharedInstance.GetPooledObject("GrenadeExplosion");
  explosion.GetComponent<Harmful>().damageAmount=(int)__instance.damage;
  explosion.transform.position=__instance.transform.position;
  explosion.transform.localScale=____explosionSize;explosion.SetActive(true);
  ___cameraShaker?.Shake();___soundFX?.Play();
  if(!__instance.isSecondary)__instance.PostNotification(ExplosiveProjectile.ProjExplodeEvent);
  return false;
 }
}
