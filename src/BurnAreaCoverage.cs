using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Android queries monster-pool transforms, once per actor, at a strict radius.
// A large collider or two colliders on one actor must not change burn delivery.
[HarmonyPatch(typeof(PeriodicallyBurnArea),"Update")]
class BurnAreaCoverage
{
 static bool Prefix(PeriodicallyBurnArea __instance,ref float ____timer,float ___timePerTick,float ___rangeRadius,int ___burnDamage,BurnSystem ___burnSys)
 {
  ____timer+=Time.deltaTime;
  if(____timer<=___timePerTick)return false;
  ____timer-=___timePerTick;
  Vector2 position=__instance.transform.position;
  foreach(var actors in ObjectPooler.SharedInstance.pooledObjectsDictionary.Values)
   foreach(var actor in actors)
    if(actor!=null&&actor.activeInHierarchy&&actor.layer==(int)TagLayerUtil.Enemy&&actor.GetComponent<Health>()!=null&&Vector2.Distance(actor.transform.position,position)<___rangeRadius)
     ___burnSys.Burn(actor,___burnDamage);
  return false;
 }
}
