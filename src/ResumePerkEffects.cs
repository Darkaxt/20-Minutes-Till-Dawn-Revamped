using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

public static partial class ResumeRuntime
{
 [Serializable]public class EffectState{public string id,trigger,action;public int activations,counter;public float periodicRemaining=-1;}
 internal sealed class EffectScope{public string id;public int index;}
 static EffectScope CurrentEffectScope;
 static readonly Dictionary<string,int> Applications=new Dictionary<string,int>();
 static readonly Dictionary<string,PerkEffect> NativeEffects=new Dictionary<string,PerkEffect>();
 static readonly Dictionary<PeriodicTrigger,IEnumerator> PeriodicRoutines=new Dictionary<PeriodicTrigger,IEnumerator>();
 static readonly Dictionary<PeriodicTrigger,float> PeriodicDeadlines=new Dictionary<PeriodicTrigger,float>(),PeriodicInitialDelay=new Dictionary<PeriodicTrigger,float>();
 static readonly Dictionary<Type,string> TriggerCounters=new Dictionary<Type,string>{{typeof(OnKillTrigger),"_killCounter"},{typeof(OnShootTrigger),"_shotCounter"},{typeof(OnThunderTrigger),"_thunderCounter"}};
 internal static EffectScope BeginEffectScope(string key)
 {
  var previous=CurrentEffectScope;Applications.TryGetValue(key,out int count);Applications[key]=count+1;
  CurrentEffectScope=new EffectScope{id=key+"#"+count};return previous;
 }
 internal static void EndEffectScope(EffectScope previous){CurrentEffectScope=previous;}
 internal static void RegisterEffect(PerkEffect effect)
 {
  if(NativeEffects.Values.Any(e=>ReferenceEquals(e,effect)))return;
  if(CurrentEffectScope==null)throw new InvalidOperationException("Native perk effect has no owning acquisition");
  NativeEffects.Add(CurrentEffectScope.id+"@"+CurrentEffectScope.index++,effect);
 }
 internal static IEnumerator ObservePeriodic(PeriodicTrigger trigger,IEnumerator native)
 {
  float? initial=PeriodicInitialDelay.TryGetValue(trigger,out float delay)?delay:(float?)null;PeriodicInitialDelay.Remove(trigger);
  var observed=DrivePeriodic(trigger,native,initial);PeriodicRoutines[trigger]=observed;return observed;
 }
 static IEnumerator DrivePeriodic(PeriodicTrigger trigger,IEnumerator native,float? initial)
 {
  bool first=true;
  while(native.MoveNext())
  {
   if(!(native.Current is WaitForSeconds))throw new InvalidOperationException("Unsupported native periodic-trigger contract");
   float duration=first&&initial.HasValue?initial.Value:Number(trigger,"triggerIntervalSeconds");
   PeriodicDeadlines[trigger]=Time.time+duration;
   yield return first&&initial.HasValue?new WaitForSeconds(duration):native.Current;first=false;
  }
 }
 static void ResetPerkEffects(){CurrentEffectScope=null;Applications.Clear();NativeEffects.Clear();PeriodicRoutines.Clear();PeriodicDeadlines.Clear();PeriodicInitialDelay.Clear();}
 static void CapturePerkEffects(State state)
 {
  foreach(var pair in NativeEffects)
  {
   var effect=pair.Value;var trigger=(Trigger)Field(effect,"trigger");var action=(flanne.PerkSystem.Action)Field(effect,"action");
   var saved=new EffectState{id=pair.Key,trigger=trigger.GetType().FullName,action=action.GetType().FullName,activations=(int)Field(effect,"_activations")};
   if(TriggerCounters.TryGetValue(trigger.GetType(),out string counter))saved.counter=(int)Field(trigger,counter);
   if(trigger is PeriodicTrigger periodic)
   {if(!PeriodicDeadlines.TryGetValue(periodic,out float end))throw new InvalidOperationException("Native periodic trigger has no observed clock");saved.periodicRemaining=Mathf.Max(0,end-Time.time);}
   state.effects.Add(saved);
  }
 }
 static void RestorePerkEffects(PlayerController player,State state)
 {
  if(NativeEffects.Count!=state.effects.Count||state.effects.Any(s=>!NativeEffects.ContainsKey(s.id)))throw new InvalidOperationException("Native perk effect owners were not reconstructed");
  foreach(var saved in state.effects)
  {
   var effect=NativeEffects[saved.id];var trigger=(Trigger)Field(effect,"trigger");var action=(flanne.PerkSystem.Action)Field(effect,"action");
   if(trigger.GetType().FullName!=saved.trigger||action.GetType().FullName!=saved.action)throw new InvalidOperationException("Native perk effect contract changed: "+saved.id);
   Plugin.SetField(effect,"_activations",saved.activations);
   if(TriggerCounters.TryGetValue(trigger.GetType(),out string counter))Plugin.SetField(trigger,counter,saved.counter);
   if(trigger is PeriodicTrigger periodic)
   {
    if(!PeriodicRoutines.TryGetValue(periodic,out var running)||saved.periodicRemaining<0)throw new InvalidOperationException("Native periodic trigger was not reconstructed");
    player.StopCoroutine(running);PeriodicInitialDelay[periodic]=saved.periodicRemaining;periodic.OnEquip(player);
   }
  }
 }
}
[HarmonyPatch]
class ResumeEffectOwner
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack)),AccessTools.Method(typeof(GunEvolution),nameof(GunEvolution.Apply))};
 static void Prefix(object __instance,out ResumeRuntime.EffectScope __state){__state=ResumeRuntime.BeginEffectScope(__instance is Powerup p?RunResume.Key(p):"evo:"+RunResume.Key((GunEvolution)__instance));}
 static Exception Finalizer(Exception __exception,ResumeRuntime.EffectScope __state){ResumeRuntime.EndEffectScope(__state);return __exception;}
}
[HarmonyPatch(typeof(PerkEffect),nameof(PerkEffect.Equip))]
class ResumeNativeEffectIdentity{static void Prefix(PerkEffect __instance)=>ResumeRuntime.RegisterEffect(__instance);}
[HarmonyPatch(typeof(PeriodicTrigger),"PeriodicTriggerCR")]
class ResumePeriodicClock{static void Postfix(PeriodicTrigger __instance,ref IEnumerator __result){__result=ResumeRuntime.ObservePeriodic(__instance,__result);}}
