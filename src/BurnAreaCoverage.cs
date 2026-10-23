using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

public sealed class AndroidBurnAreaTargets:MonoBehaviour
{internal readonly List<int> Values=new List<int>();}

[HarmonyPatch(typeof(PeriodicallyBurnArea),"Start")]
class AndroidBurnAreaOwner
{
 static bool Prefix(PeriodicallyBurnArea __instance,ref BurnSystem ___burnSys)
 {___burnSys=BurnSystem.SharedInstance;__instance.gameObject.AddComponent<AndroidBurnAreaTargets>();return false;}
}

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
  var targets=__instance.GetComponent<AndroidBurnAreaTargets>().Values;targets.Clear();
  var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();
  pool.GetMonsterInRange(targets,___rangeRadius,__instance.transform.position);
  foreach(int id in targets)
  {var actor=pool.GetFromID(id);if(actor!=null)___burnSys.Burn(actor,___burnDamage);}
  return false;
 }
}
