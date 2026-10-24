using System.Collections;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.AISpecials;
namespace TwentyMinutes.Modernization;

// Keep the original special's tagged-child selection and actual AI coroutine
// owner. The complete source sequence includes both child collider passes.
[HarmonyPatch(typeof(AILasersSpecial),"LaserCR")]
class AndroidLaserSpecial
{
 static readonly AccessTools.FieldRef<AILasersSpecial,float> Windup=AccessTools.FieldRefAccess<AILasersSpecial,float>("windupTime");
 static readonly AccessTools.FieldRef<AILasersSpecial,SoundEffectSO> WindupSound=AccessTools.FieldRefAccess<AILasersSpecial,SoundEffectSO>("windupSFX"),LaserSound=AccessTools.FieldRefAccess<AILasersSpecial,SoundEffectSO>("laserSFX");
 static bool Prefix(AILasersSpecial __instance,GameObject windupObj,GameObject laserObj,AIComponent ai,ref IEnumerator __result)
 {__result=Run(__instance,windupObj,laserObj,ai);return false;}
 static IEnumerator Run(AILasersSpecial special,GameObject windup,GameObject lasers,AIComponent ai)
 {
  windup.SetActive(true);
  ai.animator?.SetTrigger("Windup");
  WindupSound(special)?.Play();
  yield return new WaitForSeconds(Windup(special));
  lasers.SetActive(true);
  SetChildren(lasers,false);
  ai.animator?.SetTrigger("Special");
  LaserSound(special)?.Play();
  yield return new WaitForSeconds(.1f);
  SetChildren(lasers,true);
 }
 static void SetChildren(GameObject lasers,bool enabled)
 {
  for(int i=0;i<lasers.transform.childCount;i++)
  {
   var child=lasers.transform.GetChild(i).gameObject;
   child.SetActive(true);
   var collider=child.GetComponent<Collider2D>();
   if(collider!=null)collider.enabled=enabled;
  }
 }
}

// Source Use owns the one sound before the animation trigger; its iterator
// only owns the individual waits/spawns. Each invocation has independent state.
[HarmonyPatch(typeof(AISpawnSpecial),nameof(AISpawnSpecial.Use))]
class AndroidSpawnSpecial
{
 static readonly AccessTools.FieldRef<AISpawnSpecial,float> Windup=AccessTools.FieldRefAccess<AISpawnSpecial,float>("spawnWinduptime");
 static bool Prefix(AISpawnSpecial __instance,AIComponent ai,Transform target,int ___numSpawns,SoundEffectSO ___soundFX)
 {
  var spawner=ai.GetComponentInChildren<Spawner>();
  ai.StartCoroutine(Run(__instance,___numSpawns,spawner));
  ___soundFX?.Play();
  ai.animator?.SetTrigger("Special");
  return false;
 }
 static IEnumerator Run(AISpawnSpecial special,int count,Spawner spawner)
 {
  for(int i=0;i<count;i++)
  {
   yield return new WaitForSeconds(Windup(special));
   spawner?.Spawn();
  }
 }
}
