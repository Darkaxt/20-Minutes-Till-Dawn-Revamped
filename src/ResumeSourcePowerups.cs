using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PowerupSystem;
using flanne.RuneSystem;
namespace TwentyMinutes.Modernization;

public static partial class ResumeRuntime
{
 // The eight attachment save families dispatched by Android, plus the
 // KillEffectPowerup accumulator. Values retain source root/list order;
 // omitted zero buff counts consume no position in the source reader.
 [Serializable]public class SourceValue{public string kind;public int count;public float a,b,c;}
 [Serializable]public class SourcePowerup{public string key;public List<SourceValue> values=new List<SourceValue>();}
 static readonly Dictionary<string,Type> SourceOwners=new Dictionary<string,Type>
 {
  ["dragon_egg_name"]=typeof(SummonEgg),["aged_dragon"]=typeof(BuffSummonDamageOverTime),["trained_dragon_name"]=typeof(BuffSummonAttackSpeedOverTime),
  ["ritual_name"]=typeof(DamageBuffOnCurseKill),["in_the_wind_name"]=typeof(DmgAndMSOnNotHurt),["justice_name"]=typeof(GainMaxHPOnSmiteKill),
  ["shana_perk_specialize_name"]=typeof(TripleNextPowerup),["generator_name"]=typeof(BuffPlayerStats),
  ["diamond_perk_berserk_name"]=typeof(BuffPlayerStats),["diamond_perk_bulk_up_name"]=typeof(BuffPlayerStats),["scarlett_perk_pyromaniac_name"]=typeof(BuffPlayerStats)
 };
 static readonly Dictionary<string,string> BuffRoots=new Dictionary<string,string>
 { ["generator_name"]="PF_FireRateBuffOnAmmoGain",["diamond_perk_berserk_name"]="PF_BuffOnHurt",["diamond_perk_bulk_up_name"]="PF_BuffOnHeal",["scarlett_perk_pyromaniac_name"]="BuffOnBurn" };
 static MonoBehaviour[] SourceHelpers(PlayerController player,string key)
 {
  if(!SourceOwners.TryGetValue(key,out var type))return new MonoBehaviour[0];
  return Providers(player,type).Where(c=>type!=typeof(BuffPlayerStats)||c.gameObject.name.Replace("(Clone)","")==BuffRoots[key]).ToArray();
 }
 static State CaptureSource(PlayerController player)
 {
  var state=new State{sourcePowerups=new List<SourcePowerup>(),
   customRageAmmo=CustomMode.Active?RunRecorder.Instance.CustomRageAmmo?.ToList():null,
   customFusilladeProjectiles=CustomMode.Active?RunRecorder.Instance.CustomFusilladeProjectiles?.ToList():null};
  foreach(string key in RunRecorder.Instance.Acquired.Where(a=>!a.evolution).Select(a=>a.key).Distinct())
  {
   var saved=new SourcePowerup{key=key};
   foreach(var owner in SourceHelpers(player,key))
   {
    SourceValue value;
    if(owner is SummonEgg egg)value=new SourceValue{kind="egg",a=AndroidDragonEgg.Get(egg).GetRemainingHatchSeconds()};
    else if(AndroidSummonGrowth.IsGrowth(owner.GetType()))
    {var growth=AndroidSummonGrowth.Capture(owner,0);value=new SourceValue{kind="growth",count=growth.count,a=growth.remaining};}
    else if(owner is DamageBuffOnCurseKill)value=new SourceValue{kind="ritual",a=AndroidAccumulatedBonuses.Get(owner).Damage};
    else if(owner is DmgAndMSOnNotHurt wind)
    {var total=AndroidAccumulatedBonuses.Get(wind);value=new SourceValue{kind="wind",count=AndroidAccumulatedBonuses.Ticks(wind),a=AndroidAccumulatedBonuses.Timer(wind),b=total.Damage,c=total.Movement};}
    else if(owner is GainMaxHPOnSmiteKill justice)value=new SourceValue{kind="justice",count=AndroidAccumulatedBonuses.GainedHealth(justice)};
    else if(owner is TripleNextPowerup triple)value=new SourceValue{kind="specialize",count=(bool)AndroidSpecialize.Used.GetValue(triple)?0:1};
    else
    {int count=AndroidPlayerBuffCounters.Count((BuffPlayerStats)owner);if(count<=0)continue;value=new SourceValue{kind="buff",count=count};}
    saved.values.Add(value);
   }
   if(key=="feed_the_beasts_name")
   {float amount=AndroidKillEffectRecipes.Existing(key).Accumulated;if(!Mathf.Approximately(amount,0))saved.values.Add(new SourceValue{kind="feed",a=amount});}
   if(saved.values.Count>0)state.sourcePowerups.Add(saved);
  }
  var healing=player.GetComponentInChildren<DropHPOnKillRune>();
  if(healing!=null)state.sourceHealing=(int)AccessTools.Field(typeof(DropHPOnKillRune),"_counter").GetValue(healing);
  var soul=player.GetComponentInChildren<AndroidBodySoul>();if(soul!=null)state.sourceBodySoul=soul.Grants;
  return state;
 }
 static void ValidateSource(State state)
 {
  if(state.sourcePowerups.Any(p=>p==null||string.IsNullOrEmpty(p.key)||p.values==null)||state.sourcePowerups.Select(p=>p.key).Distinct().Count()!=state.sourcePowerups.Count)
   throw new InvalidOperationException("Unsupported source powerup runtime identity");
  foreach(var power in state.sourcePowerups)
  {
   string expected=power.key=="feed_the_beasts_name"?"feed":!SourceOwners.TryGetValue(power.key,out var type)?null:
    type==typeof(SummonEgg)?"egg":AndroidSummonGrowth.IsGrowth(type)?"growth":type==typeof(DamageBuffOnCurseKill)?"ritual":
    type==typeof(DmgAndMSOnNotHurt)?"wind":type==typeof(GainMaxHPOnSmiteKill)?"justice":type==typeof(TripleNextPowerup)?"specialize":"buff";
   foreach(var value in power.values)
    if(value==null||expected==null||value.kind!=expected||new[]{value.a,value.b,value.c}.Any(v=>float.IsNaN(v)||float.IsInfinity(v)))
     throw new InvalidOperationException("Unsupported source powerup runtime value");
  }
 }
 internal static void RestoreSourcePowerup(PlayerController player,State state,string key)
 {
  var saved=state.sourcePowerups.SingleOrDefault(p=>p.key==key);if(saved==null)return;
  var owners=SourceHelpers(player,key);int index=0;
  foreach(var value in saved.values)
  {
   if(value.kind=="feed")
   {var feed=AndroidKillEffectRecipes.Existing(key);feed.Accumulated=value.a;if(!Mathf.Approximately(value.a,0))player.stats[StatType.SummonDamage].AddMultiplierBonus(value.a);continue;}
   var owner=owners[index++];
   if(value.kind=="egg")AndroidDragonEgg.Get((SummonEgg)owner).RestoreRemainingHatchSeconds(value.a);
   else if(value.kind=="growth")AndroidSummonGrowth.Restore(owner,value.a,value.count);
   else if(value.kind=="ritual")
   {AndroidAccumulatedBonuses.CurseKills((DamageBuffOnCurseKill)owner)=0;AndroidAccumulatedBonuses.Get(owner).Damage=value.a;if(value.a!=0)player.stats[StatType.BulletDamage].AddMultiplierBonus(value.a);}
   else if(value.kind=="wind")
   {
    var wind=(DmgAndMSOnNotHurt)owner;int maximum=(int)AccessTools.Field(typeof(DmgAndMSOnNotHurt),"maxTicks").GetValue(wind);
    AndroidAccumulatedBonuses.Ticks(wind)=Mathf.Clamp(value.count,0,maximum);AndroidAccumulatedBonuses.Timer(wind)=Mathf.Max(0,value.a);
    var total=AndroidAccumulatedBonuses.Get(wind);total.Damage=value.b;total.Movement=value.c;
    if(value.b!=0)player.stats[StatType.BulletDamage].AddMultiplierBonus(value.b);
    if(value.c!=0)player.stats[StatType.MoveSpeed].AddMultiplierBonus(value.c);
    AndroidAccumulatedBonuses.RestoreTrail(wind);
   }
   else if(value.kind=="justice")
   {var justice=(GainMaxHPOnSmiteKill)owner;AndroidAccumulatedBonuses.SmiteKills(justice)=0;AndroidAccumulatedBonuses.GainedHealth(justice)=Mathf.Clamp(value.count,0,(int)AccessTools.Field(typeof(GainMaxHPOnSmiteKill),"mhpGainCap").GetValue(justice));}
   else if(value.kind=="specialize")
   {AndroidSpecialize.Used.SetValue(owner,value.count<=0);if(value.count<=0)AndroidSpecialize.Count=0;}
   else AndroidPlayerBuffCounters.Restore((BuffPlayerStats)owner,value.count);
  }
 }
 internal static void RestoreSourceRunes(PlayerController player,State state)
 {
  if(state.sourceHealing.HasValue)Plugin.SetField(player.GetComponentInChildren<DropHPOnKillRune>(),"_counter",Mathf.Max(0,state.sourceHealing.Value));
  if(state.sourceBodySoul.HasValue)player.GetComponentInChildren<AndroidBodySoul>().Grants=Mathf.Max(0,state.sourceBodySoul.Value);
 }
 internal static void DeferSourceStarts(PlayerController player)
 {
  foreach(var owner in SourceOwners.Keys.SelectMany(key=>SourceHelpers(player,key)).Concat(player.GetComponentsInChildren<AndroidAmmoOnThunder>(true)).Distinct())
  {
   var components=owner is BuffPlayerStats?owner.GetComponents<MonoBehaviour>():new[]{owner};
   foreach(var component in components)
    if(component.enabled){component.enabled=false;RunRecorder.Instance.SourceStarts.Add(component);}
  }
 }
 internal static void EnableSourceStarts()
 {foreach(var owner in RunRecorder.Instance.SourceStarts.Where(o=>o!=null))owner.enabled=true;RunRecorder.Instance.SourceStarts.Clear();}
 internal static void DeferLegacyAttachmentStarts(PlayerController player)
 {
  foreach(var owner in SourceHelpers(player,"shana_perk_specialize_name").Concat(player.GetComponentsInChildren<AndroidAmmoOnThunder>(true)))
   if(owner.enabled){owner.enabled=false;RunRecorder.Instance.SourceStarts.Add(owner);}
 }
}
[HarmonyPatch]
class ResumeSourceAttachmentStarts
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Postfix()
 {
  if(!RunResume.Restoring||!RunRecorder.Instance.Rebuilding)return;
  if(RunResume.Pending.runtime.sourcePowerups!=null)ResumeRuntime.DeferSourceStarts(PlayerController.Instance);
  else ResumeRuntime.DeferLegacyAttachmentStarts(PlayerController.Instance);
 }
}
