using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
namespace TwentyMinutes.Modernization;

// Android Health.HPChange posts a typed notification only after a valid
// operation leaves the target alive. Windows' producer-only SummonOnHit event
// includes lethal/protected hits and misses other Summon-classified damage.
[HarmonyPatch(typeof(Health),nameof(Health.TakeDamage))]
class AndroidSurvivingHurt
{
 internal const string Event="Revamped.AndroidSurvivingHurt";
 static void Prefix(Health __instance,int damage,out bool __state)=>__state=!__instance.isDead&&!__instance.isInvincible.value&&damage>=0;
 static void Postfix(Health __instance,DamageType damageType,bool __state)
 {if(__state&&!__instance.isDead)__instance.PostNotification(Event,damageType);}
}

// Native Champion tags also describe minibosses. Bind the source BossMonster
// distinction to actual boss-spawner ownership, persisting on that pooled actor
// independently of checkpoint encounter lists being reset at an Endless cycle.
public sealed class AndroidBossIdentity:MonoBehaviour{public bool Boss;}
[HarmonyPatch(typeof(ObjectPooler),nameof(ObjectPooler.GetPooledObject))]
class AndroidBossSpawnIdentity
{
 static void Postfix(GameObject __result)
 {
  if(__result==null||__result.GetComponent<Health>()==null)return;
  bool boss=BossSpawnAchievementBoundary.Spawning||RunResume.SpawnContext?.boss==true;
  var identity=__result.GetComponent<AndroidBossIdentity>();
  if(identity==null&&boss)identity=__result.AddComponent<AndroidBossIdentity>();
  if(identity!=null)identity.Boss=boss;
 }
}
[Serializable]
public sealed class AndroidSummonHurtTrigger:Trigger
{
 public bool Thunder;
 public override void OnEquip(PlayerController player)=>this.AddObserver(OnHurt,AndroidSurvivingHurt.Event);
 public override void OnUnEquip(PlayerController player)=>this.RemoveObserver(OnHurt,AndroidSurvivingHurt.Event);
 void OnHurt(object sender,object args)
 {
  if((DamageType)args!=DamageType.Summon)return;
  var health=(Health)sender;
  if(Thunder){if(UnityEngine.Random.Range(0f,1f)>=.15f)return;}
  else if(health.GetComponent<AndroidBossIdentity>()?.Boss==true)return;
  RaiseTrigger(health.gameObject);
 }
}
[Serializable]
public sealed class AndroidRandomThunder:Trigger
{
 public override void OnEquip(PlayerController player)=>this.AddObserver(OnThunder,ThunderGenerator.ThunderHitEvent);
 public override void OnUnEquip(PlayerController player)=>this.RemoveObserver(OnThunder,ThunderGenerator.ThunderHitEvent);
 void OnThunder(object sender,object args)
 {if(UnityEngine.Random.Range(0f,1f)<.2f)RaiseTrigger(PlayerController.Instance.gameObject);}
}
[Serializable]
public sealed class AndroidEnergizedActions:flanne.PerkSystem.Action
{
 public flanne.PerkSystem.Action[] Actions;
 public override void Init(){foreach(var action in Actions)action.Init();}
 public override void Activate(GameObject target){foreach(var action in Actions)action.Activate(target);}
}
[HarmonyPatch]
class AndroidHitEffectRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);if(key!="hellspawns_name"&&key!="thunderspawns_name"&&key!="energized_name")return;
  var effects=(PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(__instance);
  if(key=="energized_name")
  {
   if(AccessTools.Field(typeof(PerkEffect),"trigger").GetValue(effects[0]) is AndroidRandomThunder)return;
   var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new AndroidRandomThunder());
   Plugin.SetField(effect,"action",new AndroidEnergizedActions{Actions=effects.Select(e=>(flanne.PerkSystem.Action)AccessTools.Field(typeof(PerkEffect),"action").GetValue(e)).ToArray()});
   Plugin.SetField(__instance,"effects",new[]{effect});
  }
  else foreach(var effect in effects)Plugin.SetField(effect,"trigger",new AndroidSummonHurtTrigger{Thunder=key=="thunderspawns_name"});
 }
}
