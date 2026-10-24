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
 const string KillEvent="Health.DeathEvent";
 public override void OnEquip(PlayerController player)=>this.AddObserver(OnDeath,KillEvent);
 public override void OnUnEquip(PlayerController player)=>this.RemoveObserver(OnDeath,KillEvent);
 void OnDeath(object sender,object args)
 {
  if(!(args is DamageType kind)||kind!=DamageType.Summon)return;
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
public sealed class AndroidBurnDamageUp:flanne.PerkSystem.Action
{
 // Complete BurnDamageUp.Apply (0x1899310): the source owns one global
 // StatMod contribution per application, before BurnBuff floors each tick.
 public override void Activate(GameObject target)=>BurnSystem.SharedInstance.burnDamageMultiplier.AddMultiplierBonus(.35f);
}
[Serializable]
public sealed class AndroidMightDamage:IDamageModifier
{
 public ValueModifier GetMod()=>new DamageBonus(AndroidDamageRounding.PositiveDamageStage);
 sealed class DamageBonus:ValueModifier
 {
  readonly bool positive;
  internal DamageBonus(bool positiveDamage):base(0){positive=positiveDamage;}
  public override float Modify(float fromValue,float toValue)=>positive?(fromValue>0?toValue+2:toValue):fromValue<0?toValue-2:toValue;
 }
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
   case "soul_expand_name":trigger=new InstantTrigger();action=new AndroidGrantHeart{Soul=true,Capacity=2};break;
   case "tome_of_elements_name":trigger=new InstantTrigger();action=new AndroidElementStats();break;
   case "intense_burn_name":trigger=new InstantTrigger();action=new AndroidBurnDamageUp();break;
   case "electro_mastery_name":trigger=new InstantTrigger();action=new AndroidElectroMastery();break;
   case "death_rounds_name":Plugin.SetField(__instance,"effects",new PerkEffect[0]);Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);return;
   default:return;
  }
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",trigger);Plugin.SetField(effect,"action",action);
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
