using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
using flanne.PerkSystem.Actions;
using flanne.Player.Buffs;
namespace TwentyMinutes.Modernization;

[Serializable]
public sealed class AndroidSummonKillTrigger:Trigger
{
 public int Count;
 const string KillEvent="Health.SummonDamageKill";
 public override void OnEquip(PlayerController player)=>this.AddObserver(OnDeath,KillEvent);
 public override void OnUnEquip(PlayerController player)=>this.RemoveObserver(OnDeath,KillEvent);
 void OnDeath(object sender,object args)
 {
  if(++Count<500)return;Count=0;RaiseTrigger(PlayerController.Instance.gameObject);
 }
}
[Serializable]
public sealed class AndroidGrantHeart:flanne.PerkSystem.Action
{
 public bool Soul;public int Capacity;
 public override void Activate(GameObject target)
 {var health=PlayerController.Instance.playerHealth;if(Soul){health.maxSHP+=Capacity;health.shp++;}else health.Heal(1);}
}
[Serializable]
public sealed class AndroidElementStats:flanne.PerkSystem.Action
{
 public override void Activate(GameObject target)
 {
  BurnSystem.SharedInstance.burnDamageMultiplier.AddMultiplierBonus(.35f);
  ThunderGenerator.SharedInstance.damageMod.AddMultiplierBonus(.35f);
  FreezeSystem.SharedInstance.durationMod.AddMultiplierBonus(.35f);
 }
}
[Serializable]
public sealed class AndroidMightDamage:IDamageModifier
{
 // Android adds +2 to a signed negative HP change. Windows passes positive
 // damage to modifiers, so the equivalent operation subtracts two damage.
 public ValueModifier GetMod()=>new AddValueModifier(0,AndroidDamageRounding.PositiveDamageStage?-2:2);
}
[HarmonyPatch]
class AndroidHealthPerkRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);Trigger trigger=null;flanne.PerkSystem.Action action=null;
  switch(key)
  {
   case "bloodsuckers_name":trigger=new AndroidSummonKillTrigger();action=new AndroidGrantHeart();break;
   case "soul_drain_name":trigger=new AndroidSummonKillTrigger();action=new AndroidGrantHeart{Soul=true};break;
   case "soul_expand_name":trigger=new InstantTrigger();action=new AndroidGrantHeart{Soul=true,Capacity=2};break;
   case "tome_of_elements_name":trigger=new InstantTrigger();action=new AndroidElementStats();break;
   case "electro_mastery_name":trigger=new InstantTrigger();action=new AndroidElectroMastery();break;
   case "tome_of_might_name":
    var buff=new DamageBuff();Plugin.SetField(buff,"modAllDamageType",true);Plugin.SetField(buff,"modifier",new AndroidMightDamage());
    action=new ApplyPlayerBuffAction();Plugin.SetField(action,"buff",buff);trigger=new InstantTrigger();break;
   case "death_rounds_name":Plugin.SetField(__instance,"effects",new PerkEffect[0]);Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);return;
   default:return;
  }
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",trigger);Plugin.SetField(effect,"action",action);
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
