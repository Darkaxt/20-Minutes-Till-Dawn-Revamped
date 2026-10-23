using System;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using flanne;
using flanne.Player;
namespace TwentyMinutes.Modernization;

// Own the complete source firing/release flow. Existing Shooter subclasses,
// recipes, ammo callbacks and desktop cursor projection remain its boundaries.
public sealed class AndroidGunRuntime:MonoBehaviour
{
 static readonly AccessTools.FieldRef<Gun,float> Timer=AccessTools.FieldRefAccess<Gun,float>("_shotTimer");
 static readonly AccessTools.FieldRef<Gun,bool> Shooting=AccessTools.FieldRefAccess<Gun,bool>("<isShooting>k__BackingField");
 static readonly AccessTools.FieldRef<Gun,ShootingCursor> Cursor=AccessTools.FieldRefAccess<Gun,ShootingCursor>("SC");
 public BoolToggle ShootEnable=new BoolToggle(true);
 internal static AndroidGunRuntime Get(Gun gun)
 {var owner=gun.GetComponent<AndroidGunRuntime>();return owner!=null?owner:gun.gameObject.AddComponent<AndroidGunRuntime>();}
 internal static void StartShooting(Gun gun)=>Shooting(gun)=true;
 static Vector2 Direction(Gun gun)=>(Vector2)Camera.main.ScreenToWorldPoint(Cursor(gun).cursorPosition)-(Vector2)gun.transform.position;
 static bool Sword(GunData data)=>data.name=="SwordData"||data.name=="SwordOfDeathGun"||data.name=="SwordOfLightGun"||data.name=="SwordOfWindGun";
 internal void Tick(Gun gun)
 {
  if(flanne.Core.PauseController.isPaused||!ShootEnable.value)return;
  if(Timer(gun)>0){Timer(gun)-=Time.deltaTime;return;}
  if(!gun.isShooting)return;
  // The original ID211 family alone uses this animation trigger per shot.
  if(Sword(gun.gunData))gun.SetAnimationTrigger("Attack");
  Timer(gun)+=gun.gunData.isSummonGun?gun.stats[StatType.SummonAttackSpeed].ModifyInverse(gun.shotCooldown):gun.shotCooldown;
  var shooters=gun.shooters;
  for(int i=0;i<shooters.Count;i++)
  {
   var shooter=shooters[i];var recipe=gun.GetProjectileRecipe();
   if(!shooter.fireOnStop)gun.PostNotification(Gun.ShootEvent,recipe);
   var direction=Direction(gun);
   shooter.Shoot(recipe,direction,gun.numOfProjectiles,gun.spread,gun.gunData.inaccuracy);
  }
  if(!gun.shooters[0].fireOnStop)gun.gunData.gunshotSFX?.Play();
 }
 internal static void StopShooting(Gun gun)
 {
  if(!gun.isShooting)return;
  Shooting(gun)=false;
  var shooters=gun.shooters;
  for(int i=0;i<shooters.Count;i++)
  {
   var shooter=shooters[i];var direction=Direction(gun);var recipe=gun.GetProjectileRecipe();
   if(shooter.fireOnStop)gun.PostNotification(Gun.ShootEvent,recipe);
   shooter.OnStopShoot(recipe,direction,gun.numOfProjectiles,gun.spread,gun.gunData.inaccuracy);
  }
  // Source tests !fireOnStop here too: ordinary guns replay their sound at
  // release; charged shooters do not use this sound branch. Preserve it.
  if(!gun.shooters[0].fireOnStop)gun.gunData.gunshotSFX?.Play();
 }
}
[HarmonyPatch(typeof(Gun),"Start")]
class AndroidGunStart{static void Postfix(Gun __instance)=>AndroidGunRuntime.Get(__instance);}
[HarmonyPatch(typeof(Gun),"Update")]
class AndroidGunTick
{static bool Prefix(Gun __instance){AndroidGunRuntime.Get(__instance).Tick(__instance);return false;}}
[HarmonyPatch(typeof(Gun),nameof(Gun.StartShooting))]
class AndroidGunStartShooting
{static bool Prefix(Gun __instance){AndroidGunRuntime.StartShooting(__instance);return false;}}
[HarmonyPatch(typeof(Gun),nameof(Gun.StopShooting))]
class AndroidGunStopShooting
{static bool Prefix(Gun __instance){AndroidGunRuntime.StopShooting(__instance);return false;}}

// Source state transitions remain on the real native ShootingState, so reload,
// skills, disableAction and scene-exit callers all enter this same flow. Desktop
// Aim events substitute for the mobile joystick's release/re-fire callbacks.
public sealed class AndroidShootingRuntime:MonoBehaviour
{
 static readonly AccessTools.FieldRef<ShootingState,IEnumerator> Changing=AccessTools.FieldRefAccess<ShootingState,IEnumerator>("_changeStateCoroutine");
 ShootingState state;PlayerController player;
 internal static AndroidShootingRuntime Get(ShootingState state,PlayerController player)
 {
  var owner=state.GetComponent<AndroidShootingRuntime>();
  if(owner==null)owner=state.gameObject.AddComponent<AndroidShootingRuntime>();
  owner.state=state;owner.player=player;return owner;
 }
 InputAction Fire=>player.playerInput.actions["Fire"];
 InputAction Aim=>player.playerInput.actions["Aim"];
 InputAction Reload=>player.playerInput.actions["Reload"];
 void Listen()
 {
  Fire.canceled+=Cancel;Reload.started+=ManualReload;
  player.ammo.OnAmmoChanged.AddListener(AmmoChanged);
  Aim.canceled+=Cancel;
 }
 void Unlisten()
 {
  Fire.canceled-=Cancel;Reload.started-=ManualReload;
  player.ammo.OnAmmoChanged.RemoveListener(AmmoChanged);
  Aim.canceled-=Cancel;
 }
 internal void Enter()
 {
  Listen();player.disableAction.ToggleEvent+=Disabled;
  player.gun.StartShooting();CharacterWalkSpeed.Apply(player);player.faceMouse=true;
 }
 internal void Exit()
 {
  Unlisten();player.disableAction.ToggleEvent-=Disabled;player.gun.StopShooting();
 }
 void AmmoChanged(int amount)
 {
  // Unlike Windows, source neither checks nor assigns _changeStateCoroutine
  // for this notification. Do not coalesce independently started waits.
  if(amount==0)state.StartCoroutine(Wait<ReloadState>());
 }
 void ManualReload(InputAction.CallbackContext context)
 {if(!player.gun.gunData.disableManualReload)player.ChangeState<ReloadState>();}
 void Cancel(InputAction.CallbackContext context)
 {
  Changing(state)=player.ammo.outOfAmmo||!player.gun.gunData.disableManualReload?Wait<ReloadState>():Wait<IdleState>();
  state.StartCoroutine(Changing(state));
 }
 void Disabled(object sender,bool disabled)
 {if(disabled)player.ChangeState<DisabledState>();}
 void ReFire(InputAction.CallbackContext context)
 {
  if(player.ammo.outOfAmmo)return;
  if(Changing(state)!=null)state.StopCoroutine(Changing(state));
  Listen();Fire.performed-=ReFire;Aim.performed-=ReFire;
 }
 IEnumerator Wait<T>() where T:PlayerState
 {
  Unlisten();Fire.performed+=ReFire;Aim.performed+=ReFire;
  while(!player.gun.shotReady)yield return null;
  Fire.performed-=ReFire;Aim.performed-=ReFire;
  player.ChangeState<T>();Changing(state)=null;
 }
}
[HarmonyPatch(typeof(ShootingState),nameof(ShootingState.Enter))]
class AndroidShootingEnter
{static bool Prefix(ShootingState __instance,PlayerController ___owner){AndroidShootingRuntime.Get(__instance,___owner).Enter();return false;}}
[HarmonyPatch(typeof(ShootingState),nameof(ShootingState.Exit))]
class AndroidShootingExit
{static bool Prefix(ShootingState __instance,PlayerController ___owner){AndroidShootingRuntime.Get(__instance,___owner).Exit();return false;}}

// Source release always chooses Reload when manual reload is available. Keep
// the existing localized option truthful and read-only without overwriting the
// user's stored Windows preference (which applies again without the mod).
[HarmonyPatch(typeof(flanne.UI.OptionsSetter),"SetAutoReload")]
class AndroidAutomaticReloadOption
{
 static bool Prefix(LocalizedString ___autoReloadLabel,LocalizedString ___onString,TMP_Text ___autoReloadTMP)
 {
  flanne.UI.OptionsSetter.AutoReloadEnabled=true;
  ___autoReloadTMP.text=LocalizationSystem.GetLocalizedValue(___autoReloadLabel.key)+" "+LocalizationSystem.GetLocalizedValue(___onString.key);
  ___autoReloadTMP.GetComponentInParent<Button>().interactable=false;return false;
 }
}
