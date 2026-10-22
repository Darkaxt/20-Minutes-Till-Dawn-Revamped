using System;
using System.Collections;
using System.Linq;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 public static bool ShootingExitProbe;
 public static int ExitNativeSaves,ExitLiveStops,ExitCanceledCharges;
 IEnumerator CheckShootingExit(bool baseline)
 {
  ShootingExitProbe=true;bool continuation=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").EndsWith("continue");
  if(continuation)
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   GameController restored;while((restored=Object.FindObjectOfType<GameController>())==null||!(restored.CurrentState is CombatState))yield return null;
   Require(PlayerController.Instance.CurrentState is flanne.Player.IdleState&&!PlayerController.Instance.gun.isShooting,"fresh Continue restores the held-shot checkpoint through normal inactive input without resuming a held fire action");
   Require(PlayerController.Instance.gun.gunData.name=="RevolverData"&&restored.playerXP.level==2,"fresh Continue retains the saved native weapon and actual acquired level");
   yield return AndroidExitShot(restored,"continued-held",false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());var again=Object.FindObjectOfType<GameController>();yield return CheckRevolver(again,PlayerController.Instance,"shooting-exit-original-restart.png");yield return AchievementExit(again,false);SetPhase("PASS");Log("complete","Held-shot checkpoint, fresh Continue, live native stop, actual title and original gun restart pass");Application.Quit(0);yield break;
  }
  RunResume.Invalidate();Progression.State.earned.Add("weapon:CrossbowData");Progression.State.earned.Add("weapon:ModernizationChargeCannon_None");Progression.Refresh();
  bool padOnly=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").EndsWith("pad");
  bool chargeOnly=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").EndsWith("charge");
  foreach(var test in baseline?new[]{"RevolverData|held"}:padOnly?new[]{"RevolverData|pad"}:chargeOnly?new[]{"CrossbowData|held","ModernizationChargeCannon_None|live-release","ModernizationChargeCannon_None|held"}:new[]{"RevolverData|held","RevolverData|released","CrossbowData|held","ModernizationChargeCannon_None|held","RevolverData|save"})
  {
   var parts=test.Split('|');string recipe=parts[0],kind=parts[1];
   yield return AchievementRun("Shana",recipe,"20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
   var game=Object.FindObjectOfType<GameController>();var player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;
   if(kind=="save")yield return AndroidPick(game,"PowerShot",game.playerXP.level);
   yield return AndroidExitShot(game,kind,kind=="save");
   if(kind=="save"){SetPhase("PASS");Log("complete","Held/released native Revolver, charged Crossbow/Charge Cannon Give Up and held-shot checkpoint pass before dependent teardown");Application.Quit(0);yield break;}
  }
  yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());var restart=Object.FindObjectOfType<GameController>();yield return CheckRevolver(restart,PlayerController.Instance,"shooting-exit-original-restart.png");yield return AchievementExit(restart,false);SetPhase("PASS");Log("complete","Native held shooting Give Up and ordinary gun restart pass");ShootingExitProbe=false;Application.Quit(0);
 }
 IEnumerator AndroidExitShot(GameController game,string kind,bool suspend)
 {
  var player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;
  Gamepad pad=null;GamepadState padState=new GamepadState{rightStick=Vector2.right};
  if(kind=="pad")
  {
   pad=InputSystem.AddDevice<Gamepad>("RevampedExitGamepad");game.playerInput.SwitchCurrentControlScheme("Gamepad",pad);
   var bindings=game.playerInput.actions["Fire"].bindings.Where(b=>b.effectivePath?.StartsWith("<Gamepad>/")==true).Select(b=>b.effectivePath).ToArray();Log("native-controller-fire-bindings",string.Join(";",bindings));
   Require(bindings.Any(b=>b.EndsWith("/rightTrigger"))||bindings.Any(b=>b.EndsWith("/rightShoulder")),"fixture resolves the real native controller Fire control");
   if(bindings.Any(b=>b.EndsWith("/rightTrigger")))padState.rightTrigger=1;else padState=padState.WithButton(GamepadButton.RightShoulder);
  }
  else game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  bool fired=false;UnityEngine.Events.UnityAction onShot=()=>fired=true;player.gun.OnShoot.AddListener(onShot);var aim=(Vector2)Camera.main.WorldToScreenPoint(player.transform.position+Vector3.right*5);
  if(pad!=null)InputSystem.QueueStateEvent(pad,padState);else InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(!player.gun.isShooting||!(player.CurrentState is flanne.Player.ShootingState)||(!player.gun.shooters[0].fireOnStop&&!fired))yield return null;player.gun.OnShoot.RemoveListener(onShot);
  if(player.gun.shooters[0] is PortedShooter charge){while(!charge.Charging)yield return null;Log("exit-charge-precondition",new{charge.Charging,charge.ChargeTime});}
  Require(player.gun.isShooting,"actual native/ported gun is shooting before exit: "+player.gun.gunData.name+" "+kind);
  bool released=kind=="released"||kind=="live-release";
  if(released)
  {
   int before=Projectiles,shots=PortedShooter.Shots;InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});while(!(player.CurrentState is flanne.Player.IdleState))yield return null;
   if(kind=="live-release")Require(Projectiles>before&&PortedShooter.Shots==shots+1,"actual live Charge Cannon release still creates its native projectile volley exactly once");
  }
  int stops=ExitLiveStops,saves=ExitNativeSaves,points=PointsTracker.pts,projectiles=Projectiles,canceled=ExitCanceledCharges;bool charged=player.gun.shooters[0].fireOnStop,ported=player.gun.shooters[0] is PortedShooter;string weapon=player.gun.gunData.name;SetPhase("native-shooting-exit-"+weapon+"-"+kind);
  if(suspend)
  {
   PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
   while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
   Require(RunResume.Read()!=null&&RunResume.Read().weapon==weapon&&PointsTracker.pts==points&&ExitNativeSaves==saves,"held-shot Save & return preserves its actual checkpoint without a native completed-run credit deposit");File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));
  }
  else{yield return AchievementExit(game,false);Require(ExitNativeSaves==saves+1,"completed run still invokes the original native score deposit exactly once");}
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});Require(Object.FindObjectsOfType<PlayerController>().Length==0,"shooting exit reaches actual title with the old player removed");
  if(pad!=null){InputSystem.QueueStateEvent(pad,new GamepadState());InputSystem.RemoveDevice(pad);}
  Require(released?ExitLiveStops==stops:ExitLiveStops==stops+1,"shooting stop runs exactly once with live aim dependencies, and a released gun is not stopped twice");
  if(charged&&!released)
  {
   if(ported)Require(ExitCanceledCharges==canceled+1&&Projectiles==projectiles,"Charge Cannon preserves the recorded disable cancellation without a phantom release at title exit");
   else Require(Projectiles>projectiles,"charged Crossbow exit retains actual native projectile creation before scene teardown");
  }
 }
}
[HarmonyPatch(typeof(Gun),nameof(Gun.StopShooting))]
class ShootingExitReferenceProbe
{
 static void Prefix(Gun __instance)
 {
  if(!RuntimeChecks.ShootingExitProbe||!__instance.isShooting)return;
  var cursor=(UnityEngine.Object)AccessTools.Field(typeof(Gun),"SC").GetValue(__instance);
  if(Camera.main!=null&&cursor!=null)RuntimeChecks.ExitLiveStops++;
  RuntimeChecks.Log("shooting-stop-references",new{camera=Camera.main!=null,cursorAlive=cursor!=null,cursorReferenceNull=ReferenceEquals(cursor,null),gunObject=__instance.gunObj!=null,recipe=__instance.gunData!=null,shooters=__instance.shooters.Count,playerState=PlayerController.Instance?.CurrentState?.GetType().Name,portedCharging=(__instance.shooters[0] as PortedShooter)?.Charging});
 }
}
[HarmonyPatch(typeof(TransitionToTitleState),"Save")]
class ShootingExitSaveProbe
{static void Prefix(){if(RuntimeChecks.ShootingExitProbe)RuntimeChecks.ExitNativeSaves++;}}
[HarmonyPatch(typeof(PortedShooter),"OnDisable")]
class ShootingExitChargeCancelProbe
{
 static void Prefix(PortedShooter __instance)
 {
  if(!RuntimeChecks.ShootingExitProbe||!__instance.Charging)return;
  RuntimeChecks.ExitCanceledCharges++;RuntimeChecks.Log("native-charge-disable",new{__instance.Charging,active=__instance.gameObject.activeInHierarchy,state=Object.FindObjectOfType<GameController>()?.CurrentState?.GetType().Name});
 }
}
