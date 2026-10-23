using System;
using System.Globalization;
using System.Collections.Generic;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

// Read-only legacy effect DTOs and the identified dragon-growth conversion.
// Current source saves do not track native effect slots or coroutine phases.
public static partial class ResumeRuntime
{
 [Serializable]public class EffectState{public string id,trigger,action;public int activations,counter;public float periodicRemaining=-1;}
 internal static void ConvertLegacyDragonGrowth(State state)
 {
  if(state.growth!=null)return;state.growth=new List<GrowthState>();
  foreach(var effect in state.effects)
  {
   bool damage=effect.id.StartsWith("aged_dragon#")&&effect.action=="flanne.PerkSystem.Actions.ModShootingSummonDamageAction";
   bool speed=effect.id.StartsWith("trained_dragon_name#")&&effect.action=="flanne.PerkSystem.Actions.ModShootingSummonAttackSpeed";
   if((!damage&&!speed)||effect.trigger!=typeof(PeriodicTrigger).FullName)continue;
   int start=effect.id.IndexOf('#')+1,end=effect.id.IndexOf('@',start);
   // The native capped effect increments only for actual accepted calls. Its
   // saved count and observed next tick therefore supply the source replay;
   // no acquisition timestamps or elapsed-run estimate are substituted.
   state.growth.Add(new GrowthState{type=AndroidSummonGrowth.Types[damage?0:1].FullName,
    ordinal=int.Parse(effect.id.Substring(start,end-start),CultureInfo.InvariantCulture),count=effect.activations,remaining=effect.periodicRemaining});
   effect.trigger=typeof(InstantTrigger).FullName;effect.action=typeof(AndroidCombatAttachment).FullName;
   effect.activations=0;effect.counter=0;effect.periodicRemaining=-1;
  }
 }
}
