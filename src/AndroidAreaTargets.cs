using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem.Actions;
namespace TwentyMinutes.Modernization;

// Native serialized actions lack Android's per-action integer target list.
// Its identity/lifetime follows that action; do not share or copy the list.
[HarmonyPatch(typeof(AreaTargetAction),nameof(AreaTargetAction.Activate))]
class AndroidAreaTargets
{
 static readonly ConditionalWeakTable<AreaTargetAction,List<int>> Targets=new ConditionalWeakTable<AreaTargetAction,List<int>>();
 static bool Prefix(AreaTargetAction __instance,GameObject target,float ___range,flanne.PerkSystem.Action ___action)
 {
  Vector2 position=target.transform.position;
  var targets=Targets.GetOrCreateValue(__instance);targets.Clear();
  ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetMonsterInRange(targets,___range,position);
  foreach(int id in targets)
  {
   var actor=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetFromID(id);
   if(actor!=null)___action.Activate(actor);
  }
  return false;
 }
}
