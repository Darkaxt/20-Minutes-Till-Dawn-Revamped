using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

internal static class AndroidForestEncounter
{
 internal static readonly MethodInfo TreeKilled=AccessTools.Method(typeof(ForestMapRhogogSpawner),"OnTreeKilled");
 internal static readonly MethodInfo Defeated=AccessTools.Method(typeof(ForestMapRhogogSpawner),"OnRhogogDefeated");
 internal static GameObject Spawn(GameObject prefab)=>ObjectPooler.SharedInstance.GetPooledObject(prefab.name);
 internal static void ConfigureTemplate(GameObject map)
 {
  if(map.name!="PF_ForestMap"&&map.name!="PF_EndlessForestMap")return;
  // Snapshot original identities before applying source names: two distinct
  // source trees share PF_TreeMonster, and several other suffixes also moved.
  var trees=map.GetComponentsInChildren<Health>(true).Select(h=>new{Transform=h.transform,Name=h.name}).ToArray();
  foreach(var tree in trees)
  {
   var parent=tree.Transform.parent;
   if(parent==null||parent.parent!=map.transform||!AndroidForestTables.Poses.TryGetValue(tree.Name,out var pose))continue;
   tree.Transform.name=pose.Name;tree.Transform.localPosition=pose.Position;tree.Transform.localScale=pose.Scale;tree.Transform.localRotation=pose.Rotation;
  }
 }
}
[HarmonyPatch(typeof(ForestMapRhogogSpawner),"Start")]
class AndroidForestStart
{
 static bool Prefix(ForestMapRhogogSpawner __instance,GameObject ___rhogogPrefab,GameObject ___walkingTreePrefab,ref Health[] ___treeHealths,ref GameTimer ___timer)
 {
  ___timer=GameTimer.SharedInstance;
  var oneSecond=(Action<object,object>)Delegate.CreateDelegate(typeof(Action<object,object>),__instance,AccessTools.Method(typeof(ForestMapRhogogSpawner),"OnOneSecondLeft"));
  __instance.AddObserver(oneSecond,GameTimer.OneSecondLeftNotification);
  var pool=ObjectPooler.SharedInstance;pool.AddObject(___rhogogPrefab.name,___rhogogPrefab,-1);pool.AddObject(___walkingTreePrefab.name,___walkingTreePrefab,-1);
  ___treeHealths=__instance.GetComponentsInChildren<Health>();
  Action<object,object> treeKilled=(sender,args)=>AndroidForestEncounter.TreeKilled.Invoke(__instance,null);
  foreach(var tree in ___treeHealths)__instance.AddObserver(treeKilled,Health.DeathEvent,tree);
  return false;
 }
}
[HarmonyPatch(typeof(ForestMapRhogogSpawner),"OnTreeKilled")]
class AndroidForestTrees
{
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  int replaced=0;
  foreach(var instruction in instructions)
  {
   if(instruction.operand is MethodInfo method&&method.Name=="Instantiate"&&method.DeclaringType==typeof(Object)&&method.IsGenericMethod&&method.GetGenericArguments()[0]==typeof(GameObject)&&method.GetParameters().Length==1)
   {instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(AndroidForestEncounter),nameof(AndroidForestEncounter.Spawn));replaced++;}
   yield return instruction;
  }
  if(replaced!=1)throw new InvalidOperationException("Unsupported native Forest walking-tree spawn");
 }
}
[HarmonyPatch(typeof(ForestMapRhogogSpawner),"OnOneSecondLeft")]
class AndroidForestBoss
{
 static bool Prefix(ForestMapRhogogSpawner __instance,bool ___treesActivated,GameTimer ___timer,GameObject ___rhogogPrefab)
 {
  if(!___treesActivated)return false;
  ___timer.Stop();var actor=AndroidForestEncounter.Spawn(___rhogogPrefab);
  actor.transform.position=PlayerController.Instance.transform.position+new Vector3(20,0,0);
  var health=actor.GetComponent<Health>();
  Action<object,object> defeated=(sender,args)=>AndroidForestEncounter.Defeated.Invoke(__instance,null);
  health.AddObserver(defeated,Health.DeathEvent,health);
  // The existing M10 handler owns the requested secret-boss unlock/notice.
  // Source adds no arena here and has no explicit Endless exclusion.
  return false;
 }
}
