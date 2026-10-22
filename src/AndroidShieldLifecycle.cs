using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Actions;
using flanne.PowerupSystem;
namespace TwentyMinutes.Modernization;

// Health.isProtected is a BoolToggle in Android, a bool in Windows. Keep its
// native counted implementation and project only its value into the native API.
public sealed class AndroidProtection:MonoBehaviour
{
 internal readonly BoolToggle Value=new BoolToggle(false);
 PlayerHealth health;
 void Awake(){health=GetComponent<PlayerHealth>();Value.ToggleEvent+=(sender,value)=>health.isProtected=value;}
 internal static AndroidProtection Get(PlayerHealth health)
 {var owner=health.GetComponent<AndroidProtection>();return owner!=null?owner:health.gameObject.AddComponent<AndroidProtection>();}
 internal void RestoreCount(int count)
 {Plugin.SetField(Value,"_flip",count);health.isProtected=Value.value;}
 internal static void KeepProtection(PlayerHealth health,bool value)
 {var owner=health.GetComponent<AndroidProtection>();health.isProtected=owner!=null?owner.Value.value:value;}
}
public sealed class AndroidShieldLifecycle:MonoBehaviour
{
 internal int Count=1,LastCount=1;
 internal float Reduction,ApplyAt=-1,BreakUntil=-1;
 internal bool Contributes;
 PreventDamage shield;
 AndroidProtection protection;
 internal float Cooldown=>Mathf.Max(10,shield.cooldownTime-Reduction);
 internal void Bind(PreventDamage owner){shield=owner;protection=AndroidProtection.Get(PlayerController.Instance.playerHealth);}
 internal void Apply()
 {
  Plugin.SetField(shield,"_waitCooldownCoroutine",null);ApplyAt=-1;BreakUntil=-1;Plugin.SetField(shield,"<isActive>k__BackingField",true);
  protection.Value.Flip();Contributes=true;shield.OnCooldownDone.Invoke();LastCount=Count;
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
  Plugin.SetField(shield,"<isActive>k__BackingField",false);protection.Value.UnFlip();Contributes=false;BreakUntil=-1;
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
[HarmonyPatch(typeof(PreventDamage),"ApplyInvincibility")]
class AndroidShieldApply
{
 static bool Prefix(PreventDamage __instance)
 {var owner=__instance.GetComponent<AndroidShieldLifecycle>();if(owner==null){owner=__instance.gameObject.AddComponent<AndroidShieldLifecycle>();owner.Bind(__instance);}owner.Apply();return false;}
}
[HarmonyPatch(typeof(PreventDamage),"OnPreventDamage")]
class AndroidShieldHit{static bool Prefix(PreventDamage __instance){__instance.GetComponent<AndroidShieldLifecycle>().Hit();return false;}}
[HarmonyPatch(typeof(PlayerHealth),"CheckCollision")]
class AndroidProtectionCollision
{
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  int writes=0;var field=AccessTools.Field(typeof(PlayerHealth),nameof(PlayerHealth.isProtected));
  foreach(var instruction in instructions)
  {if(instruction.opcode==OpCodes.Stfld&&Equals(instruction.operand,field)){instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(AndroidProtection),nameof(AndroidProtection.KeepProtection));writes++;}yield return instruction;}
  if(writes!=1)throw new InvalidOperationException("Unsupported native protection-consumption boundary");
 }
}
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
   Vector3 center=PlayerController.Instance.transform.position;
   var candidates=ObjectPooler.SharedInstance.pooledObjectsDictionary.Values.SelectMany(list=>list).Where(actor=>actor!=null&&actor.activeInHierarchy&&actor.layer==(int)TagLayerUtil.Enemy&&Mathf.Abs(actor.transform.position.x-center.x)<___range.x&&Mathf.Abs(actor.transform.position.y-center.y)<___range.y).Distinct().ToArray();
   if(candidates.Length>0)___TGen.GenerateAt(candidates[UnityEngine.Random.Range(0,candidates.Length)],___baseDamage);
  }
  return false;
 }
}
