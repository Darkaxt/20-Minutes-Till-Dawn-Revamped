using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using flanne;
using flanne.Player;
namespace TwentyMinutes.Modernization;

// The native state keeps the Windows bindings/presentation. These are the two
// source state fields missing from it, owned by the same player/state lifetime.
public sealed class AndroidReloadRuntime:MonoBehaviour
{
 static readonly AccessTools.FieldRef<ReloadState,IEnumerator> ReloadRoutine=AccessTools.FieldRefAccess<ReloadState,IEnumerator>("reloadCoroutine");
 internal bool Force;
 internal IEnumerator ReadFire;
 internal static AndroidReloadRuntime Get(ReloadState state)
 {var value=state.GetComponent<AndroidReloadRuntime>();return value!=null?value:state.gameObject.AddComponent<AndroidReloadRuntime>();}
 internal static Action<InputAction.CallbackContext> Fire(ReloadState state)=>(Action<InputAction.CallbackContext>)Delegate.CreateDelegate(typeof(Action<InputAction.CallbackContext>),state,AccessTools.Method(typeof(ReloadState),"OnFireAction"));
 internal static EventHandler<bool> Disabled(ReloadState state)=>(EventHandler<bool>)Delegate.CreateDelegate(typeof(EventHandler<bool>),state,AccessTools.Method(typeof(ReloadState),"OnDisableToggleChange"));
 internal static void Hide(PlayerController player)=>player.reloadBar.transform.parent.gameObject.SetActive(false);
 internal static IEnumerator Reload(ReloadState state,PlayerController player)
 {
  player.reloadBar.transform.parent.gameObject.SetActive(true);float timer=0,duration=player.gun.reloadDuration;
  while(timer<duration){player.reloadBar.value=timer/duration;yield return null;timer+=Time.deltaTime;}
  player.ammo.Reload();player.reloadEndSFX.Play();
  if(player.playerInput.actions["Fire"].ReadValue<float>()!=0)player.ChangeState<ShootingState>();else player.ChangeState<IdleState>();
 }
 internal static IEnumerator Read(ReloadState state,PlayerController player)
 {
  while(true)
  {
   if(player.playerInput.actions["Fire"].ReadValue<float>()==1&&!player.ammo.outOfAmmo)
   {player.ChangeState<ShootingState>();state.StopCoroutine(ReloadRoutine(state));}
   yield return null;
  }
 }
}
[HarmonyPatch(typeof(ReloadState),nameof(ReloadState.Enter))]
class AndroidReloadEnter
{
 static bool Prefix(ReloadState __instance,PlayerController ___owner,ref IEnumerator ___reloadCoroutine)
 {
  var data=AndroidReloadRuntime.Get(__instance);
  ___owner.playerInput.actions["Fire"].started+=AndroidReloadRuntime.Fire(__instance);
  ___owner.disableAction.ToggleEvent+=AndroidReloadRuntime.Disabled(__instance);
  ___owner.moveSpeedMultiplier=1;___owner.faceMouse=false;
  ___reloadCoroutine=AndroidReloadRuntime.Reload(__instance,___owner);__instance.StartCoroutine(___reloadCoroutine);
  data.ReadFire=AndroidReloadRuntime.Read(__instance,___owner);__instance.StartCoroutine(data.ReadFire);
  ___owner.gun.SetAnimationTrigger("Reload");
  // Keep the Windows gun's audio override as native presentation.
  var sound=___owner.gun.gunData.reloadSFXOverride;sound=sound==null?___owner.reloadStartSFX:sound;sound.Play();
  data.Force=___owner.ammo.outOfAmmo;return false;
 }
}
[HarmonyPatch(typeof(ReloadState),nameof(ReloadState.Exit))]
class AndroidReloadExit
{
 static bool Prefix(ReloadState __instance,PlayerController ___owner)
 {
  ___owner.playerInput.actions["Fire"].started-=AndroidReloadRuntime.Fire(__instance);
  ___owner.disableAction.ToggleEvent-=AndroidReloadRuntime.Disabled(__instance);
  ___owner.gun.SetAnimationTrigger("Still");AndroidReloadRuntime.Hide(___owner);
  __instance.StopCoroutine(AndroidReloadRuntime.Get(__instance).ReadFire);return false;
 }
}
[HarmonyPatch(typeof(ReloadState),"OnFireAction")]
class AndroidReloadFire
{
 static bool Prefix(ReloadState __instance,PlayerController ___owner,ref IEnumerator ___reloadCoroutine)
 {
  var data=AndroidReloadRuntime.Get(__instance);if(___owner.ammo.outOfAmmo||data.Force)return false;
  ___owner.ChangeState<ShootingState>();__instance.StopCoroutine(___reloadCoroutine);__instance.StopCoroutine(data.ReadFire);
  AndroidReloadRuntime.Hide(___owner);return false;
 }
}
[HarmonyPatch(typeof(ReloadState),"OnDisableToggleChange")]
class AndroidReloadDisabled
{
 static bool Prefix(ReloadState __instance,PlayerController ___owner,ref IEnumerator ___reloadCoroutine,bool isDisabled)
 {
  if(!isDisabled)return false;
  ___owner.ChangeState<DisabledState>();__instance.StopCoroutine(___reloadCoroutine);__instance.StopCoroutine(AndroidReloadRuntime.Get(__instance).ReadFire);
  AndroidReloadRuntime.Hide(___owner);return false;
 }
}
[HarmonyPatch(typeof(IdleState),nameof(IdleState.Enter))]
class AndroidIdleEntry
{
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  foreach(var instruction in instructions)
  {
   yield return instruction;
   if(instruction.opcode==OpCodes.Stfld&&Equals(instruction.operand,AccessTools.Field(typeof(PlayerController),"faceMouse")))
   {yield return new CodeInstruction(OpCodes.Ret);yield break;}
  }
  throw new InvalidOperationException("Unsupported native idle entry boundary");
 }
}
[HarmonyPatch(typeof(IdleState),"OnFireAction")]
class AndroidIdleFire
{
 static bool Prefix(PlayerController ___owner)
 {if(!___owner.ammo.outOfAmmo)___owner.ChangeState<ShootingState>();return false;}
}
[HarmonyPatch(typeof(IdleState),"OnReloadAction")]
class AndroidIdleReload
{
 static bool Prefix(PlayerController ___owner)
 {if(!___owner.ammo.fullOnAmmo&&!___owner.gun.gunData.disableManualReload)___owner.ChangeState<ReloadState>();return false;}
}
