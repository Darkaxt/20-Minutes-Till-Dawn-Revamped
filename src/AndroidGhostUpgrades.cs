using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Actions;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

// Windows Activate clones an existing matching PrefabReference when available.
// Android AttachPrefabPowerup always instantiates the original serialized prefab.
// Use the native attachment helper with that original, preserving its placement.
[HarmonyPatch(typeof(AttachPrefabToPlayerAction),nameof(AttachPrefabToPlayerAction.Activate))]
class AndroidGhostAttachment
{
 static readonly MethodInfo Attach=AccessTools.Method(typeof(AttachPrefabToPlayerAction),"AttachPrefab");
 static bool Prefix(AttachPrefabToPlayerAction __instance,GameObject ___prefab,int ___amountToAttach)
 {
  var summon=___prefab.GetComponentInChildren<ShootingSummon>(true);if(summon==null||summon.SummonTypeID!="GhostFriend")return true;
  for(int i=0;i<___amountToAttach;i++)Attach.Invoke(__instance,new object[]{___prefab});return false;
 }
}

// Recovered SummonAttackCDEffect subscribes in Start and assigns its effect only
// on this named notification. Its shipped Best Friends caller selects FireRate
// once; repeated acquisitions do not add that same selected type again.
public sealed class AndroidBestFriends:MonoBehaviour
{
 public float Factor=1;
 internal bool Subscribed;
 void Start(){this.AddObserver(OnStateChange,"EventStatChange");Subscribed=true;}
 void OnStateChange(object sender,object args)=>Factor=1+PlayerController.Instance.stats[StatType.FireRate].Modify(1);
 void OnDestroy(){if(Subscribed)this.RemoveObserver(OnStateChange,"EventStatChange");Subscribed=false;}
}
[Serializable]
public sealed class AndroidGhostUpgrade:flanne.PerkSystem.Action
{
 public bool BestFriends;
 public override void Activate(GameObject target)
 {
  foreach(var ghost in PlayerController.Instance.GetComponentsInChildren<ShootingSummon>())
  {
   if(ghost.SummonTypeID!="GhostFriend")continue;
   if(BestFriends){if(ghost.GetComponent<AndroidBestFriends>()==null)ghost.gameObject.AddComponent<AndroidBestFriends>();}
   else{ghost.numProjectiles++;ghost.targetMouse=false;}
  }
 }
}
[HarmonyPatch]
class AndroidGhostRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);if(key!="best_friends_name"&&key!="vengeful_ghost_name"&&key!="ghost_wizard_name")return;
  flanne.PerkSystem.Action action=key=="ghost_wizard_name"?(flanne.PerkSystem.Action)new AndroidGhostWizardAction():new AndroidGhostUpgrade{BestFriends=key=="best_friends_name"};
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",action);
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
