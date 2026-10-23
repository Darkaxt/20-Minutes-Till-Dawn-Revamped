using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
using flanne.PerkSystem.Actions;
namespace TwentyMinutes.Modernization;

// GaleOnLastAmmo's name does not describe a reload subscription. Its actual
// source Start/Update owns a countdown, with no carry or saved runtime phase.
[Serializable]
public sealed class AndroidGaleAcquire:flanne.PerkSystem.Action
{
 public AndroidGaleCast Cast;
 public override void Init()=>Cast.Init();
 public override void Activate(GameObject target)
 {
  var root=new GameObject("GaleOnLastAmmo");root.transform.SetParent(PlayerController.Instance.transform,false);
  var owner=root.AddComponent<AndroidGaleProducer>();owner.Cast=Cast;
  if(RunResume.Restoring){owner.enabled=false;RunRecorder.Instance.SourceStarts.Add(owner);}
 }
}
public sealed class AndroidGaleProducer:MonoBehaviour
{
 public float Interval=2,Remaining;
 internal AndroidGaleCast Cast;
 void Start(){Remaining=Interval;}
 void Update()
 {
  Remaining-=Time.deltaTime;
  if(Remaining<=0){Cast.Activate(PlayerController.Instance.gameObject);Remaining=Interval;}
 }
}

[Serializable]
public sealed class AndroidRegenerationAcquire:flanne.PerkSystem.Action
{
 public GameObject Prefab;
 public override void Activate(GameObject target)
 {
  var root=new GameObject("PF_DropHPOverTime");root.transform.SetParent(PlayerController.Instance.transform,false);
  var owner=root.AddComponent<DropPrefabOverTime>();Plugin.SetField(owner,"timeToDrop",90f);Plugin.SetField(owner,"prefab",Prefab);
  if(RunResume.Restoring){owner.enabled=false;RunRecorder.Instance.SourceStarts.Add(owner);}
 }
}
[HarmonyPatch]
class AndroidRegenerationRecipe
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  if(RunResume.Key(__instance)!="regeneration_name")return;
  var effects=(PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(__instance);
  if(effects.Any(e=>AccessTools.Field(typeof(PerkEffect),"action").GetValue(e) is AndroidRegenerationAcquire))return;
  var spawn=effects.Select(e=>AccessTools.Field(typeof(PerkEffect),"action").GetValue(e)).OfType<SpawnPrefabAction>().First();
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());
  Plugin.SetField(effect,"action",new AndroidRegenerationAcquire{Prefab=(GameObject)AccessTools.Field(typeof(SpawnPrefabAction),"prefab").GetValue(spawn)});
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
// Retain native DropPrefabOverTime and its matching CancelInvoke teardown.
// Only the reconstructed source Regeneration owner changes the native initial
// delay and uses the source position-before-activation pool overload.
[HarmonyPatch(typeof(DropPrefabOverTime),"Start")]
class AndroidRegenerationStart
{
 static bool Prefix(DropPrefabOverTime __instance,GameObject ___prefab,float ___timeToDrop,ref ObjectPooler ___OP)
 {
  if(__instance.gameObject.name!="PF_DropHPOverTime")return true;
  ___OP=ObjectPooler.SharedInstance;___OP.GetComponent<AndroidPools>().Register(___prefab.name,___prefab,-1);
  __instance.InvokeRepeating("DropHP",___timeToDrop,___timeToDrop);return false;
 }
}
[HarmonyPatch(typeof(DropPrefabOverTime),"DropHP")]
class AndroidRegenerationDrop
{
 static bool Prefix(DropPrefabOverTime __instance,GameObject ___prefab,ObjectPooler ___OP)
 {
  if(__instance.gameObject.name!="PF_DropHPOverTime")return true;
  ___OP.GetComponent<AndroidPools>().Get(___prefab.name,__instance.transform.position);return false;
 }
}
