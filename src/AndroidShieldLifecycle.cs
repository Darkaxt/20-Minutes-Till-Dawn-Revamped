using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Actions;
using flanne.PowerupSystem;
namespace TwentyMinutes.Modernization;

// Source shields flip Health.isInvincible itself. Value records only this
// owner's shield contributions for continuation; the native health counter is
// shared with hit, skill and rune invulnerability, as it is in Android.
public sealed class AndroidProtection:MonoBehaviour
{
 internal readonly BoolToggle Value=new BoolToggle(false);
 PlayerHealth health;
 void Awake(){health=GetComponent<PlayerHealth>();Value.ToggleEvent+=(sender,value)=>health.isProtected=value;}
 internal static AndroidProtection Get(PlayerHealth health)
 {var owner=health.GetComponent<AndroidProtection>();return owner!=null?owner:health.gameObject.AddComponent<AndroidProtection>();}
 internal void Add(){Value.Flip();health.isInvincible.Flip();}
 internal void Remove(){Value.UnFlip();health.isInvincible.UnFlip();}
 internal void RestoreCount(int count)
 {
  int delta=count-Value.flips;
  for(int i=0;i<delta;i++)health.isInvincible.Flip();
  for(int i=0;i>delta;i--)health.isInvincible.UnFlip();
  Plugin.SetField(Value,"_flip",count);health.isProtected=Value.value;
 }
}
public sealed class AndroidShieldLifecycle:MonoBehaviour
{
 internal int Count=1,LastCount=1;
 internal float Reduction,ApplyAt=-1,BreakUntil=-1;
 internal bool Contributes;
 PreventDamage shield;
 AndroidProtection Protection=>AndroidProtection.Get(PlayerController.Instance.playerHealth);
 internal float Cooldown=>Mathf.Max(10,shield.cooldownTime-Reduction);
 internal void Bind(PreventDamage owner){shield=owner;}
 internal static AndroidShieldLifecycle Get(PreventDamage shield)
 {
  var owner=shield.GetComponent<AndroidShieldLifecycle>();
  if(owner==null){owner=shield.gameObject.AddComponent<AndroidShieldLifecycle>();owner.Bind(shield);}
  return owner;
 }
 internal void Apply()
 {
  Plugin.SetField(shield,"_waitCooldownCoroutine",null);ApplyAt=-1;BreakUntil=-1;Plugin.SetField(shield,"<isActive>k__BackingField",true);
  Protection.Add();Contributes=true;shield.OnCooldownDone.Invoke();LastCount=Count;
 }
 internal void Hit()
 {
  LastCount--;if(LastCount>0||AccessTools.Field(typeof(PreventDamage),"_waitCooldownCoroutine").GetValue(shield)!=null)return;
  var routine=Break(.5f,true);Plugin.SetField(shield,"_waitCooldownCoroutine",routine);StartCoroutine(routine);
 }
 IEnumerator Break(float remaining,bool notify)
 {
  BreakUntil=Time.time+remaining;if(notify)shield.OnDamagePrevented.Invoke();
  yield return new WaitForSeconds(remaining);
  Plugin.SetField(shield,"<isActive>k__BackingField",false);Protection.Remove();Contributes=false;BreakUntil=-1;
  ApplyAt=GameTimer.SharedInstance.timer+Cooldown;
 }
 void Update()
 {
  if(ApplyAt==-1)return;
  float now=GameTimer.SharedInstance.timer;Plugin.SetField(shield,"_timer",Cooldown-(ApplyAt-now));
  if(now>ApplyAt)Apply();
 }
 internal void Advance(float seconds)
 {
  if(ApplyAt<=0)return;
  ApplyAt-=seconds;if(ApplyAt<GameTimer.SharedInstance.timer)ApplyAt=GameTimer.SharedInstance.timer+1;
  Plugin.SetField(shield,"_timer",Cooldown-(ApplyAt-GameTimer.SharedInstance.timer));
 }
 internal void Reduce(float seconds){Advance(seconds);Reduction+=seconds;}
 internal ResumeRuntime.ShieldState Capture(int ordinal)=>new ResumeRuntime.ShieldState
 {ordinal=ordinal,source=true,active=shield.isActive,cooldown=shield.cooldownTime,rate=ResumeRuntime.Capture(shield.cooldownRate),
  flashRemaining=BreakUntil<0?0:Mathf.Max(0,BreakUntil-Time.time),reduction=Reduction,applyAt=ApplyAt,count=Count,lastCount=LastCount,contributes=Contributes};
 internal void Restore(ResumeRuntime.ShieldState saved)
 {
  StopAllCoroutines();Reduction=saved.reduction;ApplyAt=saved.applyAt;Count=saved.count;LastCount=saved.lastCount;
  Plugin.SetField(shield,"<isActive>k__BackingField",saved.active);Contributes=saved.contributes;BreakUntil=-1;
  if(saved.flashRemaining>0||!saved.active)shield.OnDamagePrevented.Invoke();
  if(saved.flashRemaining>0)
  {var routine=Break(saved.flashRemaining,false);Plugin.SetField(shield,"_waitCooldownCoroutine",routine);StartCoroutine(routine);}
  else Plugin.SetField(shield,"_waitCooldownCoroutine",saved.active?null:(IEnumerator)Ended());
 }
 static IEnumerator Ended(){yield break;}
}
[HarmonyPatch]
class AndroidShieldStartLifetime
{
 static IEnumerable<MethodBase> TargetMethods()
 {yield return AccessTools.Method(typeof(PreventDamage),"Start");yield return AccessTools.Method(typeof(PreventDamage),"OnDestroy");}
 static bool Prefix(PreventDamage __instance,MethodBase __originalMethod)
 {
  var handler=(UnityAction)Delegate.CreateDelegate(typeof(UnityAction),__instance,AccessTools.Method(typeof(PreventDamage),"OnPreventDamage"));
  if(__originalMethod.Name=="OnDestroy")PlayerController.Instance.playerHealth.onDamagePrevented.RemoveListener(handler);
  else
  {
   // Native-only storage remains for owned legacy snapshots; source cooldown
   // arithmetic does not read it. Do not reset the source reduction/deadline
   // already changed by an upgrade applied before this first Start.
   __instance.cooldownRate=new StatMod();Plugin.SetField(__instance,"_waitCooldownCoroutine",null);
   PlayerController.Instance.playerHealth.onDamagePrevented.AddListener(handler);
   AccessTools.Method(typeof(PreventDamage),"ApplyInvincibility").Invoke(__instance,null);
  }
  return false;
 }
}
[HarmonyPatch(typeof(PreventDamage),"ApplyInvincibility")]
class AndroidShieldApply
{
 static bool Prefix(PreventDamage __instance)
 {AndroidShieldLifecycle.Get(__instance).Apply();return false;}
}
[HarmonyPatch(typeof(PreventDamage),"OnPreventDamage")]
class AndroidShieldHit{static bool Prefix(PreventDamage __instance){AndroidShieldLifecycle.Get(__instance).Hit();return false;}}
[HarmonyPatch]
class AndroidRepeatedShieldRecipe
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  if(RunResume.Key(__instance)!="holy_shield_name")return;
  // The first native effect already attaches the original complete prefab.
  // Source repeats attach it again; discard only the Windows rate-bonus recipe.
  Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
[HarmonyPatch(typeof(ThunderDuringHolyShield),"Update")]
class AndroidShieldThunder
{
 static bool Prefix(PreventDamage ___holyShield,ThunderGenerator ___TGen,int ___baseDamage,float ___cooldown,int ___thundersPerWave,Vector2 ___range,ref float ____timer)
 {
  if(___holyShield.isActive)____timer+=Time.deltaTime;if(____timer<=___cooldown)return false;____timer-=___cooldown;
  for(int i=0;i<___thundersPerWave;i++)
  {
   var target=AndroidSummonTargets.Random(___range.x,___range.y);
   if(target!=null)___TGen.GenerateAt(target,___baseDamage);
  }
  return false;
 }
}
