using System;
using System.Linq;
using UnityEngine;
using flanne;
using flanne.PerkSystem.Actions;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

public static partial class ResumeRuntime
{
 // Read-time provenance for the earlier Boolean adapter, never written back
 // into an owned checkpoint. Its Stalwart action subtracted from the native
 // base; the source owner keeps that same reduction in a separate field.
 internal static void ConvertLegacyShieldEffects(State state)
 {
  bool boolean=state.shields.Any(s=>!s.source);
  foreach(var effect in state.effects)
  {
   bool stalwart=effect.id.StartsWith("stalwart_shield_name#");
   if(boolean&&stalwart&&effect.action==typeof(AndroidStalwartAction).FullName)state.LegacyShieldBaseReductions++;
   if(effect.trigger!=typeof(InstantTrigger).FullName||effect.action!=typeof(HolyShieldCDRAction).FullName)continue;
   if(effect.id.StartsWith("holy_shield_name#"))effect.action=typeof(AttachPrefabToPlayerAction).FullName;
   else if(stalwart)effect.action=typeof(AndroidStalwartAction).FullName;
   else continue;
   effect.activations=0;effect.counter=0;
  }
 }
 static void RestoreLegacyShieldData(State state,PreventDamage[] shields)
 {
  if(state.shields.Count==0&&shields.Length==0)return;
  bool boolean=state.shields.Any(s=>!s.source);
  if(!boolean&&state.protectionFlips.HasValue)return;
  if(boolean&&state.protectionFlips.HasValue)throw new InvalidOperationException("Checkpoint mixes Boolean shield data with a counted protection owner");
  int stalwarts=RunResume.Pending.acquired.Count(a=>!a.evolution&&a.key=="stalwart_shield_name");
  foreach(var saved in state.shields.Where(s=>!s.source))
  {
   // This is the published native StatMod.ModifyInverse expression, not the
   // source cooldown formula. It recovers the old recorded remaining delay.
   float previous=(saved.cooldown+saved.rate.flat)/((1+saved.rate.bonus)*saved.rate.reduction);
   if(state.LegacyShieldBaseReductions>0)previous=Mathf.Max(10,previous);
   float remaining=Mathf.Max(0,previous-saved.timer);
   if(float.IsNaN(remaining)||float.IsInfinity(remaining))throw new InvalidOperationException("Checkpoint has an unsupported legacy shield recovery value");
   bool flashing=saved.flashRemaining>0;
   saved.source=true;saved.count=1;saved.lastCount=saved.active&&!flashing?1:0;
   saved.active=saved.active||flashing;saved.contributes=saved.active;
   saved.applyAt=saved.active?-1:GameTimer.SharedInstance.timer+remaining;
   if(saved.ordinal==0)
   {saved.cooldown+=60f*state.LegacyShieldBaseReductions;saved.reduction=60f*stalwarts;}
  }
  // Original Windows repeats increased one shield's rate. Source repeats
  // create independent owners; newly reconstructed extra roots retain their
  // actual source Start state rather than borrowing the old first root's hit.
  for(int i=0;i<shields.Length;i++)
   if(!state.shields.Any(s=>s.ordinal==i))state.shields.Add(AndroidShieldLifecycle.Get(shields[i]).Capture(i));
  state.protectionFlips=state.shields.Count(s=>s.contributes);
 }
}
