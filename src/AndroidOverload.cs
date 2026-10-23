using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PowerupSystem;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(ExplosionOnBurningThunder),"Start")]
class AndroidOverloadSound
{
 // Native Start already caches the source systems, registers through the
 // source pool adapter and owns the matching global thunder observer.
 static void Prefix(ref SoundEffectSO ___soundFX)=>___soundFX=AndroidPerkSounds.Overload();
}
[HarmonyPatch(typeof(ExplosionOnBurningThunder),"OnThunderHit")]
class AndroidOverloadProc
{
 static bool Prefix(BurnSystem ___BurnSys,GameObject ___explosionPrefab,SoundEffectSO ___soundFX,object args)
 {
  var target=args as GameObject;
  if(___BurnSys.IsBurning(target))
  {
   // Source acquisition supplies position before activation. Its return is
   // unused, and source audio still plays when the effect pool is exhausted.
   ObjectPooler.SharedInstance.GetComponent<AndroidPools>().Get(___explosionPrefab.name,target.transform.position);
   ___soundFX?.Play();
  }
  // Source retains the serialized cameraShaker field but never invokes it.
  return false;
 }
}
