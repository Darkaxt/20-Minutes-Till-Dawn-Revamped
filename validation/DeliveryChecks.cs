using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckDeliveredRoster(TitleScreenController title)
 {
  Require(Environment.GetEnvironmentVariable("MODERNIZATION_DELIVERED_PLAYER")=="1","roster smoke requires the actual installed package, never a runner-replaced production DLL");
  string[] characters=Characters.Items(title.characterMenu).Select(item=>item.data.name).ToArray();
  string[] guns=NativeSelection.GunItems(title.gunMenu).Select(item=>item.data.name).ToArray();
  Require(characters.Length==15&&guns.Length==12,"delivered native menus retain thirteen original characters/two additions and eleven original weapons/Charge Cannon");
  Require(Progression.Gates.All(g=>Progression.Allowed(g.Data)),"delivered roster fixture owns content earned by the complete encounter workflow");
  Log("fixture-precondition","Each delivered character starts once; each delivered base gun fires/reloads at least once against a real stationary source-pooled monster, including Salvo's required lock target. Empty rune selections, stopped encounter/game clocks and synthetic devices isolate roster/input compatibility. This is not every character/weapon permutation, evolution or natural run.");
  var rows=new List<string>();
  foreach(int i in Enumerable.Range(0,characters.Length).OrderBy(index=>guns[index%guns.Length]=="SalvoKnivesData"?0:1))
  {
   string character=characters[i],weapon=guns[i%guns.Length];SetPhase("delivered-roster-"+character+"-"+weapon);
   RunResume.Invalidate();yield return AchievementRun(character,weapon,"20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());
   var game=Object.FindObjectOfType<GameController>();var player=PlayerController.Instance;StopFixtureSpawning();
   var target=StatusEnemy("Enemy");target.transform.position=player.transform.position+Vector3.right*3;Physics2D.SyncTransforms();
   Require(player.loadedCharacter.name==character&&player.gun.gunData.name==weapon,"actual delivered native selection reaches combat: "+character+" / "+weapon);
   game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
   int shots=0;UnityAction observe=()=>shots++;player.gun.OnShoot.AddListener(observe);
   while(!(player.CurrentState is flanne.Player.IdleState)||!player.ammo.fullOnAmmo)yield return null;
   // Original Salvo UpdateDistanceMoved locks after walking, before Fire;
   // distanceToLockOn is walked distance, not cursor proximity or hold time.
   if(player.gun.shooters[0] is SalvoShooter salvo)
   {
    InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.W));
    while(((List<int>)WindField(salvo.GetComponent<AndroidSalvo>(),"Locked")).Count==0)yield return null;
    InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());yield return null;
    Require(target.gameObject.activeSelf,"delivered native walking locks an actual active source-pooled monster before Fire");
   }
   var aim=(Vector2)Camera.main.WorldToScreenPoint(target.transform.position);
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));
   while(!player.gun.isShooting)yield return null;
   // A short real hold also exercises the release-fired base weapons.
   float held=0;while(held<.15f){held+=Time.deltaTime;yield return null;}
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});
   while(shots==0||!(player.CurrentState is flanne.Player.IdleState)||(!player.gun.gunData.disableManualReload&&!player.ammo.fullOnAmmo))yield return null;
   if(player.gun.gunData.disableManualReload&&!player.ammo.fullOnAmmo)
   {
    Log("delivered-native-reload-rule","Original recipe disables manual/release reload; use real held Fire until the magazine empties: "+weapon);
    InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));
    while(!(player.CurrentState is flanne.Player.ReloadState))yield return null;
    Require(player.ammo.outOfAmmo,"delivered no-manual-reload weapon enters native automatic reload at empty: "+weapon);
    InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});
    while(!(player.CurrentState is flanne.Player.IdleState)||!player.ammo.fullOnAmmo)yield return null;
   }
   Require(!player.gun.isShooting,"delivered native mouse fire/release/reload returns full and idle: "+weapon);
   if(i==0)
   {
    var pad=InputSystem.AddDevice<Gamepad>("RevampedDeliveredInput");game.playerInput.SwitchCurrentControlScheme("Gamepad",pad);
    var state=new GamepadState{rightStick=Vector2.right};var paths=game.playerInput.actions["Fire"].bindings.Select(b=>b.effectivePath).Where(p=>p!=null&&p.StartsWith("<Gamepad>/")).ToArray();
    Require(paths.Any(p=>p.EndsWith("/rightTrigger")||p.EndsWith("/rightShoulder")),"delivered native controller Fire binding exists");
    if(paths.Any(p=>p.EndsWith("/rightTrigger")))state.rightTrigger=1;else state=state.WithButton(GamepadButton.RightShoulder);
    int before=shots;InputSystem.QueueStateEvent(pad,state);while(shots==before)yield return null;InputSystem.QueueStateEvent(pad,new GamepadState());
    while(!(player.CurrentState is flanne.Player.IdleState)||!player.ammo.fullOnAmmo)yield return null;
    Require(!player.gun.isShooting,"delivered controller Aim/Fire/release completes native reload");
    InputSystem.RemoveDevice(pad);game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);
    yield return Capture("delivered-native-input-hud.png");
   }
   rows.Add(character+"\t"+weapon+"\t"+player.gun.shooters[0].GetType().Name+"\t"+shots);
   player.gun.OnShoot.RemoveListener(observe);yield return StatusReturn(target);yield return AchievementExit(game,false);
  }
  File.WriteAllLines(Path.Combine(ResultDir,"delivered-roster.tsv"),new[]{"character\tweapon\tshooter\tactualShootEvents"}.Concat(rows));
  Require(StatsOverlay.Instance.Collected.Count==0&&!CustomMode.Active,"delivered final scene exit clears run-only HUD and Custom state");
  SetPhase("PASS");Log("complete","Installed-package native roster15/base weapons12, real mouse fire/release/reload, controller input and repeated ordinary scene exits; no cross-product/evolution/natural-run claim");Application.Quit(0);
 }
}
