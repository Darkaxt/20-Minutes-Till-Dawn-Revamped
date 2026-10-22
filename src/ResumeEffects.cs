using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using flanne;
using flanne.Player;
using flanne.Player.Buffs;
using flanne.RuneSystem;
using flanne.PowerupSystem;
using flanne.PerkSystem.Actions;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

public static partial class ResumeRuntime
{
 [Serializable]public class TimedEffect{public string type,method;public int ordinal;public float remaining;}
 [Serializable]public class SavedChange{public string type;public bool flat;public int flatValue;public float value;}
 [Serializable]public class TemporaryEffect{public float remaining;public List<SavedChange> changes=new List<SavedChange>();}
 [Serializable]public class ShieldState{public int ordinal;public bool active;public float timer,flashRemaining,cooldown;public ModState rate;}
 static readonly Dictionary<MonoBehaviour,Tuple<string,float>> Deadlines=new Dictionary<MonoBehaviour,Tuple<string,float>>();
 static readonly Dictionary<TemporaryStatBuff,float> TemporaryDeadlines=new Dictionary<TemporaryStatBuff,float>();
 static readonly HashSet<TemporaryStatBuff> InertReplayedBuffs=new HashSet<TemporaryStatBuff>();
 static readonly HashSet<MonoBehaviour> SpentOneUse=new HashSet<MonoBehaviour>(),Reviving=new HashSet<MonoBehaviour>();
 static readonly List<SingleShotModStatAction> ShotActions=new List<SingleShotModStatAction>();
 internal static bool RevivalInProgress=>Reviving.Count>0;
 static readonly Dictionary<Type,string[]> FlowMethods=new Dictionary<Type,string[]>
 {
  {typeof(MomentumRune),new[]{"StartBuffCR"}},{typeof(BuffOnXP),new[]{"StartBuffCR"}},
  {typeof(FireRateBuffOnGainAmmo),new[]{"BuffCR"}},{typeof(TemporaryRefreshingBuff),new[]{"BuffCR"}},{typeof(FireRateOnHurt),new[]{"StatBoostCR"}},
  {typeof(ElementalBarrageRune),new[]{"StartBonusCR","WaitForCooldownCR"}},{typeof(LastStandRune),new[]{"InvulnCR"}}
 };
 static object Field(object owner,string name)=>AccessTools.Field(owner.GetType(),name).GetValue(owner);
 static float Number(object owner,string name)=>Convert.ToSingle(Field(owner,name));
 internal static void NativeFlowStarted(MonoBehaviour owner,string method)
 {
  float duration=method=="StartBonusCR"?Number(owner,"secondsPerLevel")*Number(owner,"level"):
   method=="WaitForCooldownCR"?Number(owner,"cooldown"):
   method=="InvulnCR"?Number(owner,"invulnDurationPerLevel")*Number(owner,"level"):
   method=="InvincibilityCooldownCR"?.5f:Number(owner,"duration");
  Deadlines[owner]=Tuple.Create(method,Time.time+duration);
  if(method=="InvulnCR")SpentOneUse.Add(owner);
 }
 internal static bool AttachTemporary(TemporaryStatBuff buff)
 {
  if(RunRecorder.Instance?.Rebuilding==true){InertReplayedBuffs.Add(buff);return false;}
  TemporaryDeadlines[buff]=Time.time+Number(buff,"duration");return true;
 }
 internal static void DetachTemporary(TemporaryStatBuff buff){TemporaryDeadlines.Remove(buff);}
 internal static void RegisterShot(SingleShotModStatAction action){if(!ShotActions.Contains(action))ShotActions.Add(action);}
 internal static IEnumerator TrackRevival(MonoBehaviour owner,IEnumerator native)
 {
  SpentOneUse.Add(owner);Reviving.Add(owner);
  try{while(native.MoveNext())yield return native.Current;}finally{Reviving.Remove(owner);}
 }
 static void ForgetEffects(){Deadlines.Clear();TemporaryDeadlines.Clear();InertReplayedBuffs.Clear();SpentOneUse.Clear();Reviving.Clear();ShotActions.Clear();ResetPerkEffects();}
 static void CaptureEffects(PlayerController player,State state)
 {
  CapturePerkEffects(state);
  foreach(var pair in FlowMethods)
  {
   var components=Providers(player,pair.Key);
   for(int i=0;i<components.Length;i++)
   {
    var c=components[i];string method=null;float remaining=0;
    if(c is MomentumRune||c is BuffOnXP){remaining=Number(c,"_timer");if(remaining>0)method="StartBuffCR";}
    else if(c is FireRateBuffOnGainAmmo){if(Field(c,"_buffCoroutine")!=null){method="BuffCR";remaining=Number(c,"duration")-Number(c,"_timer");}}
    else if(c is TemporaryRefreshingBuff){if(Field(c,"buffCoroutine")!=null){method="BuffCR";remaining=Number(c,"duration")-Number(c,"_timer");}}
    else
    {
     bool active=c is FireRateOnHurt?Field(c,"statBoostCoroutine")!=null:c is ElementalBarrageRune?(bool)Field(c,"disableInflictGain"):(bool)Field(c,"active");
     if(active)
     {if(!Deadlines.TryGetValue(c,out var end))throw new InvalidOperationException("Native effect clock was not observed: "+c.GetType().Name);method=end.Item1;remaining=end.Item2-Time.time;}
    }
    if(method!=null)state.timed.Add(new TimedEffect{type=pair.Key.FullName,ordinal=i,method=method,remaining=Mathf.Max(0,remaining)});
   }
  }
  var buffs=(List<Buff>)Field(player.GetComponentInChildren<PlayerBuffs>(),"buffs");
  foreach(var buff in buffs.OfType<TemporaryStatBuff>())
  {
   if(!TemporaryDeadlines.TryGetValue(buff,out float deadline))throw new InvalidOperationException("Native temporary effect clock was not observed");
   state.temporary.Add(new TemporaryEffect{remaining=Mathf.Max(0,deadline-Time.time),changes=((StatChange[])Field(buff,"statChanges")).Select(s=>new SavedChange{type=s.type.ToString(),flat=s.isFlatMod,flatValue=s.flatValue,value=s.value}).ToList()});
  }
  foreach(var rune in player.GetComponentsInChildren<Rune>(true).Where(r=>r is LastStandRune||r is SecondWindRune))
   if(!SpentOneUse.Contains(rune)||(rune is LastStandRune&&(bool)Field(rune,"active")))state.liveOneUseRunes.Add(rune.GetType().FullName);
  state.singleShotActions=ShotActions.Select(a=>(bool)Field(a,"_isActive")).ToList();
  var shields=player.GetComponentsInChildren<PreventDamage>(true);
  for(int i=0;i<shields.Length;i++)
  {
   var s=shields[i];bool flash=s.isActive&&Field(s,"_waitCooldownCoroutine")!=null;
   float flashRemaining=flash&&Deadlines.TryGetValue(s,out var deadline)?Mathf.Max(0,deadline.Item2-Time.time):0;
   state.shields.Add(new ShieldState{ordinal=i,active=s.isActive&&!flash,timer=flash?0:Number(s,"_timer"),flashRemaining=flashRemaining,cooldown=s.cooldownTime,rate=Capture(s.cooldownRate)});
  }
 }
 static void ValidateEffects(State state)
 {
  if(state.effects==null||state.effects.Select(s=>s.id).Distinct().Count()!=state.effects.Count)throw new InvalidOperationException("Unsupported native perk effect checkpoint");
  foreach(var flow in state.timed)
  {var type=FlowMethods.Keys.SingleOrDefault(t=>t.FullName==flow.type);if(type==null||flow.ordinal<0||!FlowMethods[type].Contains(flow.method)||float.IsNaN(flow.remaining)||float.IsInfinity(flow.remaining)||flow.remaining<0)throw new InvalidOperationException("Unsupported native effect checkpoint");}
  foreach(var temp in state.temporary)
   if(temp.remaining<0||float.IsNaN(temp.remaining)||float.IsInfinity(temp.remaining)||temp.changes.Any(s=>!Enum.TryParse<StatType>(s.type,out var type)||type<0||type>=StatType.Count))throw new InvalidOperationException("Unsupported native temporary effect checkpoint");
 }
 static void RestoreEffects(PlayerController player,State state)
 {
  RestorePerkEffects(player,state);
  var buffs=player.GetComponentInChildren<PlayerBuffs>();var list=(List<Buff>)Field(buffs,"buffs");
  // Replayed instant actions registered these native buff instances, but their
  // OnAttach was suppressed. Remove them without an unmatched OnUnattach.
  list.RemoveAll(b=>b is TemporaryStatBuff temporary&&InertReplayedBuffs.Contains(temporary));InertReplayedBuffs.Clear();
  foreach(var saved in state.temporary)
  {
   var buff=new TemporaryStatBuff();Plugin.SetField(buff,"duration",saved.remaining);
   Plugin.SetField(buff,"statChanges",saved.changes.Select(s=>new StatChange{type=(StatType)Enum.Parse(typeof(StatType),s.type),isFlatMod=s.flat,flatValue=s.flatValue,value=s.value}).ToArray());buffs.Add(buff);
  }
  foreach(var rune in player.GetComponentsInChildren<Rune>(true).Where(r=>r is LastStandRune||r is SecondWindRune))
   if(!state.liveOneUseRunes.Contains(rune.GetType().FullName))
   {var handler=(UnityAction<int>)Delegate.CreateDelegate(typeof(UnityAction<int>),rune,AccessTools.Method(rune.GetType(),"OnHealthChange"));player.playerHealth.onHealthChangedTo.RemoveListener(handler);Object.Destroy(rune.gameObject);}
  foreach(var pair in FlowMethods)
  {
   var components=Providers(player,pair.Key);
   for(int i=0;i<components.Length;i++)
   {
    var c=components[i];c.StopAllCoroutines();var saved=state.timed.SingleOrDefault(s=>s.type==pair.Key.FullName&&s.ordinal==i);
    if(c is FireRateBuffOnGainAmmo)Plugin.SetField(c,"_buffCoroutine",null);
    if(c is TemporaryRefreshingBuff)Plugin.SetField(c,"buffCoroutine",null);
    if(c is FireRateOnHurt)Plugin.SetField(c,"statBoostCoroutine",null);
    if(saved==null)continue;
    string duration=c is LastStandRune?"invulnDurationPerLevel":c is ElementalBarrageRune?(saved.method=="StartBonusCR"?"secondsPerLevel":"cooldown"):"duration";
    object originalValue=Field(c,duration);float original=Convert.ToSingle(originalValue);float factor=(c is LastStandRune||(c is ElementalBarrageRune&&saved.method=="StartBonusCR"))?Number(c,"level"):1;
    float timer=(c is MomentumRune||c is BuffOnXP||c is FireRateBuffOnGainAmmo||c is TemporaryRefreshingBuff)?Number(c,"_timer"):0;
    Plugin.SetField(c,duration,c is FireRateBuffOnGainAmmo||c is TemporaryRefreshingBuff?originalValue:(object)(saved.remaining/factor));
    try
    {
     var routine=(IEnumerator)AccessTools.Method(pair.Key,saved.method).Invoke(c,null);
     if(c is FireRateBuffOnGainAmmo)Plugin.SetField(c,"_buffCoroutine",routine);
     if(c is TemporaryRefreshingBuff)Plugin.SetField(c,"buffCoroutine",routine);
     if(c is FireRateOnHurt)Plugin.SetField(c,"statBoostCoroutine",routine);
     c.StartCoroutine(routine);
     if(c is MomentumRune||c is BuffOnXP||c is FireRateBuffOnGainAmmo||c is TemporaryRefreshingBuff)Plugin.SetField(c,"_timer",timer);
    }
    finally{Plugin.SetField(c,duration,originalValue);}
   }
  }
  foreach(var standing in player.GetComponentsInChildren<BuffWhenStanding>(true))Plugin.SetField(standing,"_lastFramePos",player.transform.position);
  if(state.singleShotActions.Count!=ShotActions.Count)throw new InvalidOperationException("Native one-shot action contract was not reconstructed");
  for(int i=0;i<ShotActions.Count;i++)
   if(state.singleShotActions[i]!=(bool)Field(ShotActions[i],"_isActive"))
   {if(state.singleShotActions[i])ShotActions[i].Activate(player.gameObject);else ShotActions[i].DeActivate();}
  var shields=player.GetComponentsInChildren<PreventDamage>(true);
  foreach(var saved in state.shields)
  {
   if(saved.ordinal>=shields.Length)throw new InvalidOperationException("Native shield was not reconstructed");
   var shield=shields[saved.ordinal];shield.cooldownTime=saved.cooldown;Restore(shield.cooldownRate,saved.rate);
   if(!saved.active)
   {var routine=RestoreShield(shield,saved);Plugin.SetField(shield,"_waitCooldownCoroutine",routine);shield.StartCoroutine(routine);}
  }
 }
 static IEnumerator RestoreShield(PreventDamage shield,ShieldState saved)
 {
  var native=(IEnumerator)AccessTools.Method(typeof(PreventDamage),"InvincibilityCooldownCR").Invoke(shield,null);
  if(!native.MoveNext()||!(native.Current is WaitForSeconds))throw new InvalidOperationException("Unsupported native shield cooldown contract");
  PlayerController.Instance.playerHealth.isProtected=false;
  if(saved.flashRemaining>0)yield return new WaitForSeconds(saved.flashRemaining);
  if(!native.MoveNext())yield break;
  Plugin.SetField(shield,"_timer",saved.timer);
  do{yield return native.Current;}while(native.MoveNext());
 }
}
[HarmonyPatch(typeof(TemporaryStatBuff),nameof(TemporaryStatBuff.OnAttach))]
class ResumeTemporaryAttach{static bool Prefix(TemporaryStatBuff __instance)=>ResumeRuntime.AttachTemporary(__instance);}
[HarmonyPatch(typeof(TemporaryStatBuff),nameof(TemporaryStatBuff.OnUnattach))]
class ResumeTemporaryDetach{static void Postfix(TemporaryStatBuff __instance)=>ResumeRuntime.DetachTemporary(__instance);}
[HarmonyPatch(typeof(SingleShotModStatAction),nameof(SingleShotModStatAction.Init))]
class ResumeSingleShotIdentity{static void Postfix(SingleShotModStatAction __instance)=>ResumeRuntime.RegisterShot(__instance);}
[HarmonyPatch]
class ResumeEffectClock
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(FireRateOnHurt),"StatBoostCR"),AccessTools.Method(typeof(ElementalBarrageRune),"StartBonusCR"),AccessTools.Method(typeof(ElementalBarrageRune),"WaitForCooldownCR"),AccessTools.Method(typeof(LastStandRune),"InvulnCR"),AccessTools.Method(typeof(PreventDamage),"InvincibilityCooldownCR")};
 static void Postfix(MonoBehaviour __instance,MethodBase __originalMethod)=>ResumeRuntime.NativeFlowStarted(__instance,__originalMethod.Name);
}
[HarmonyPatch(typeof(SecondWindRune),"HealAnimationCR")]
class ResumeRevivalBoundary{static void Postfix(SecondWindRune __instance,ref IEnumerator __result){__result=ResumeRuntime.TrackRevival(__instance,__result);}}
[HarmonyPatch]
class ResumeHealthIsNotAnEvent
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(LastStandRune),"OnHealthChange"),AccessTools.Method(typeof(SecondWindRune),"OnHealthChange"),AccessTools.Method(typeof(HPThresholdStatRune),"OnHpChange")};
 static bool Prefix()=>!RunResume.Restoring;
}
