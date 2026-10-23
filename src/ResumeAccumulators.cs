using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

public static partial class ResumeRuntime
{
 [Serializable]public class RitualState{public int ordinal;public float damage;}
 [Serializable]public class UnharmedState{public int ordinal,ticks;public float timer,damage,movement;}
 [Serializable]public class JusticeState{public int ordinal,gainedHealth;}
 static bool AccumulatedProvider(Type type)=>type==typeof(DamageBuffOnCurseKill)||type==typeof(DmgAndMSOnNotHurt)||type==typeof(GainMaxHPOnSmiteKill);
 static void CaptureAccumulated(PlayerController player,State state)
 {
  state.rituals=new List<RitualState>();state.unharmed=new List<UnharmedState>();state.justice=new List<JusticeState>();
  var rituals=player.GetComponentsInChildren<DamageBuffOnCurseKill>(true);
  for(int i=0;i<rituals.Length;i++)state.rituals.Add(new RitualState{ordinal=i,damage=AndroidAccumulatedBonuses.Get(rituals[i]).Damage});
  var unharmed=player.GetComponentsInChildren<DmgAndMSOnNotHurt>(true);
  for(int i=0;i<unharmed.Length;i++)
  {var value=AndroidAccumulatedBonuses.Get(unharmed[i]);state.unharmed.Add(new UnharmedState{ordinal=i,ticks=AndroidAccumulatedBonuses.Ticks(unharmed[i]),timer=AndroidAccumulatedBonuses.Timer(unharmed[i]),damage=value.Damage,movement=value.Movement});}
  var justice=player.GetComponentsInChildren<GainMaxHPOnSmiteKill>(true);
  for(int i=0;i<justice.Length;i++)state.justice.Add(new JusticeState{ordinal=i,gainedHealth=AndroidAccumulatedBonuses.GainedHealth(justice[i])});
  if(RunRecorder.Instance.Acquired.Any(a=>!a.evolution&&a.key=="feed_the_beasts_name"))
  {
   var feed=AndroidKillEffectRecipes.Existing("feed_the_beasts_name");
   if(!Mathf.Approximately(feed.Accumulated,0))state.feedTheBeastsDamage=feed.Accumulated;
  }
 }
 static void ValidateAccumulated(State state)
 {
  if(state.rituals!=null&&(state.rituals.Any(s=>s==null||s.ordinal<0||float.IsNaN(s.damage)||float.IsInfinity(s.damage))||state.rituals.Select(s=>s.ordinal).Distinct().Count()!=state.rituals.Count))throw new InvalidOperationException("Unsupported Ritual runtime identity");
  if(state.unharmed!=null&&(state.unharmed.Any(s=>s==null||s.ordinal<0||new[]{s.timer,s.damage,s.movement}.Any(v=>float.IsNaN(v)||float.IsInfinity(v)))||state.unharmed.Select(s=>s.ordinal).Distinct().Count()!=state.unharmed.Count))throw new InvalidOperationException("Unsupported unharmed-bonus runtime identity");
  if(state.justice!=null&&(state.justice.Any(s=>s==null||s.ordinal<0)||state.justice.Select(s=>s.ordinal).Distinct().Count()!=state.justice.Count))throw new InvalidOperationException("Unsupported Justice runtime identity");
 }
 static void RestoreAccumulatedData(PlayerController player,State state)
 {
  // The enclosing Windows checkpoint still restores aggregate stat values.
  // Translate the source helper data without adding the same bonuses twice.
  // Full source stat-mutation/observer replay remains the P2-G3 reconciliation.
  if(state.rituals!=null)
  {
   var owners=player.GetComponentsInChildren<DamageBuffOnCurseKill>(true);
   foreach(var owner in owners){AndroidAccumulatedBonuses.CurseKills(owner)=0;AndroidAccumulatedBonuses.Get(owner).Damage=0;}
   foreach(var saved in state.rituals)
   {if(saved.ordinal>=owners.Length)throw new InvalidOperationException("Source Ritual was not reconstructed");AndroidAccumulatedBonuses.Get(owners[saved.ordinal]).Damage=saved.damage;}
  }
  if(state.unharmed!=null)
  {
   var owners=player.GetComponentsInChildren<DmgAndMSOnNotHurt>(true);
   foreach(var saved in state.unharmed)
   {
    if(saved.ordinal>=owners.Length)throw new InvalidOperationException("Source unharmed bonus was not reconstructed");
    var owner=owners[saved.ordinal];int maximum=(int)AccessTools.Field(typeof(DmgAndMSOnNotHurt),"maxTicks").GetValue(owner);
    AndroidAccumulatedBonuses.Ticks(owner)=Mathf.Clamp(saved.ticks,0,maximum);AndroidAccumulatedBonuses.Timer(owner)=Mathf.Max(0,saved.timer);
    var accumulated=AndroidAccumulatedBonuses.Get(owner);accumulated.Damage=saved.damage;accumulated.Movement=saved.movement;AndroidAccumulatedBonuses.RestoreTrail(owner);
   }
  }
  if(state.justice!=null)
  {
   var owners=player.GetComponentsInChildren<GainMaxHPOnSmiteKill>(true);
   foreach(var owner in owners){AndroidAccumulatedBonuses.SmiteKills(owner)=0;AndroidAccumulatedBonuses.GainedHealth(owner)=0;}
   foreach(var saved in state.justice)
   {
    if(saved.ordinal>=owners.Length)throw new InvalidOperationException("Source Justice was not reconstructed");
    var owner=owners[saved.ordinal];int cap=(int)AccessTools.Field(typeof(GainMaxHPOnSmiteKill),"mhpGainCap").GetValue(owner);
    AndroidAccumulatedBonuses.GainedHealth(owner)=Mathf.Clamp(saved.gainedHealth,0,cap);
   }
  }
 }
 static int EffectOrdinal(EffectState effect)
 {int start=effect.id.IndexOf('#')+1,end=effect.id.IndexOf('@',start);return int.Parse(effect.id.Substring(start,end-start),CultureInfo.InvariantCulture);}
 internal static void ConvertLegacyKillEffects(State state)
 {
  foreach(var provider in state.providers.Where(p=>p.type==typeof(AndroidBodySoul).FullName))provider.values.RemoveAll(v=>v.field=="Remaining");
  foreach(var effect in state.effects)
  {
   string key=effect.id.Substring(0,effect.id.IndexOf('#'));
   if((key=="diamond_perk_berserk_name"&&effect.trigger=="flanne.PerkSystem.Triggers.OnPlayerHurtTrigger"||key=="diamond_perk_bulk_up_name"&&effect.trigger=="flanne.PerkSystem.Triggers.OnPlayerHealTrigger")&&effect.action=="flanne.PerkSystem.Actions.ModStatAction")
   {effect.trigger=typeof(InstantTrigger).FullName;effect.action=typeof(AndroidCombatAttachment).FullName;effect.counter=0;effect.activations=0;continue;}
   if(!AndroidKillEffectRecipes.Keys.Contains(key))continue;
   bool native=effect.trigger==typeof(OnKillTrigger).FullName&&(effect.action=="flanne.PerkSystem.Actions.ModStatAction"||effect.action=="flanne.PerkSystem.Actions.SpawnPrefabAction");
   bool direct=effect.trigger==typeof(AndroidSummonKillTrigger).FullName&&effect.action==typeof(AndroidGrantHeart).FullName;
   if(!native&&!direct)continue;
   // Android does not save these partial kill counters. Native unlimited
   // Feed effects also lack a total accepted-action count: retain the known
   // legacy aggregate bonus, without inventing an accumulated source history.
   effect.trigger=typeof(InstantTrigger).FullName;effect.action=typeof(AndroidKillEffectApplication).FullName;effect.counter=0;effect.activations=0;
  }
 }
 internal static void ConvertLegacyAccumulated(State state)
 {
  if(state.unharmed==null)
  {
   state.unharmed=new List<UnharmedState>();
   foreach(var provider in state.providers.Where(p=>p.type==typeof(DmgAndMSOnNotHurt).FullName))
   {
    int ticks=int.Parse(provider.values.Single(v=>v.field=="_ticks").value,CultureInfo.InvariantCulture);float amount=0;
    for(int i=0;i<ticks;i++)amount+=.1f;
    state.unharmed.Add(new UnharmedState{ordinal=provider.ordinal,ticks=ticks,timer=float.Parse(provider.values.Single(v=>v.field=="_timer").value,CultureInfo.InvariantCulture),damage=amount,movement=amount});
   }
  }
  bool ritualMissing=state.rituals==null,justiceMissing=state.justice==null;
  if(ritualMissing)state.rituals=new List<RitualState>();
  if(justiceMissing)
  {
   state.justice=state.providers.Where(p=>p.type==typeof(GainMaxHPOnSmiteKill).FullName).Select(p=>new JusticeState{ordinal=p.ordinal,
    gainedHealth=int.Parse(p.values.Single(v=>v.field=="_mhpGainCounter").value,CultureInfo.InvariantCulture)}).ToList();
  }
  foreach(var effect in state.effects)
  {
   if(ritualMissing&&effect.id.StartsWith("ritual_name#")&&effect.action=="flanne.PerkSystem.Actions.ModStatAction"&&effect.trigger==typeof(OnKillTrigger).FullName)
   {
    // Unlimited native effects did not count accepted actions. Keep their
    // existing permanent damage in the aggregate snapshot; start tracking new
    // source accrual at zero rather than inventing historical curse kills.
    state.rituals.Add(new RitualState{ordinal=EffectOrdinal(effect),damage=0});
    effect.trigger=typeof(InstantTrigger).FullName;effect.action=typeof(AndroidCombatAttachment).FullName;effect.activations=0;effect.counter=0;
   }
   else if(justiceMissing&&effect.id.StartsWith("justice_name#")&&effect.action=="flanne.PerkSystem.Actions.ModStatAction"&&effect.trigger==typeof(OnKillTrigger).FullName)
   {
    state.justice.Add(new JusticeState{ordinal=EffectOrdinal(effect),gainedHealth=effect.activations});
    effect.trigger=typeof(InstantTrigger).FullName;effect.action=typeof(AndroidJusticeAcquire).FullName;effect.activations=0;effect.counter=0;
   }
   else if(effect.id.StartsWith("in_the_wind_name#")&&effect.action=="flanne.PerkSystem.Actions.AttachPrefabToPlayerAction")effect.action=typeof(AndroidCombatAttachment).FullName;
  }
 }
}
