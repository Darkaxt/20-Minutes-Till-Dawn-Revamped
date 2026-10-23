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
 static void ValidateAccumulated(State state)
 {
  if(state.rituals!=null&&(state.rituals.Any(s=>s==null||s.ordinal<0||float.IsNaN(s.damage)||float.IsInfinity(s.damage))||state.rituals.Select(s=>s.ordinal).Distinct().Count()!=state.rituals.Count))throw new InvalidOperationException("Unsupported Ritual runtime identity");
  if(state.unharmed!=null&&(state.unharmed.Any(s=>s==null||s.ordinal<0||new[]{s.timer,s.damage,s.movement}.Any(v=>float.IsNaN(v)||float.IsInfinity(v)))||state.unharmed.Select(s=>s.ordinal).Distinct().Count()!=state.unharmed.Count))throw new InvalidOperationException("Unsupported unharmed-bonus runtime identity");
  if(state.justice!=null&&(state.justice.Any(s=>s==null||s.ordinal<0)||state.justice.Select(s=>s.ordinal).Distinct().Count()!=state.justice.Count))throw new InvalidOperationException("Unsupported Justice runtime identity");
 }
 static int EffectOrdinal(EffectState effect)
 {int start=effect.id.IndexOf('#')+1,end=effect.id.IndexOf('@',start);return int.Parse(effect.id.Substring(start,end-start),CultureInfo.InvariantCulture);}
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
    // Unlimited native effects did not count accepted actions. Their
    // historical damage is not imported. Source reconstruction starts its
    // accrual at zero rather than inventing historical curse kills.
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
