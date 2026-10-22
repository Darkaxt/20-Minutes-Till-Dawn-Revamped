using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

public sealed class AndroidReflex:MonoBehaviour
{
 public float LastMove,Granted;
 StatsHolder stats;
 public void Bind(StatsHolder owner)
 {stats=owner;LastMove=stats[StatType.MoveSpeed].Modify(1);stats[StatType.MoveSpeed].ChangedEvent+=Changed;}
 void Changed(object sender,EventArgs args)
 {
  if(Granted>=.1f)return;
  float current=stats[StatType.MoveSpeed].Modify(1),change=current-LastMove;
  float next=Granted+change;
  if(next>.1f){change=.1f-Granted;next=.1f;}
  Granted=next;LastMove=current;stats[StatType.Dodge].AddMultiplierBonus(change);
 }
 void OnDestroy(){if(stats!=null)stats[StatType.MoveSpeed].ChangedEvent-=Changed;}
}
[Serializable]
public sealed class AndroidReflexAction:flanne.PerkSystem.Action
{
 public override void Activate(GameObject target)
 {var player=PlayerController.Instance;if(player.GetComponent<AndroidReflex>()==null)player.gameObject.AddComponent<AndroidReflex>().Bind(player.stats);}
}
[HarmonyPatch]
class AndroidReflexRecipe
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  if(RunResume.Key(__instance)!="reflex_name")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidReflexAction());
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
[HarmonyPatch(typeof(DodgeRoller),nameof(DodgeRoller.Roll))]
class AndroidDodgeRoll
{
 static bool Prefix(PlayerController ___player,ref bool __result)
 {
  float roll=UnityEngine.Random.Range(0f,1f),chance=___player.stats[StatType.Dodge].Modify(1)-1;
  float cap=___player.stats[StatType.DodgeCapMod].Modify(60)/100;
  __result=roll<Mathf.Clamp(chance,0,cap);return false;
 }
}
