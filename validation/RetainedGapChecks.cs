using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.AISpecials;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool GapMode=>(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")??"").StartsWith("resume-retained-gaps-")||ClosureMode;
 internal static bool GapCaptureShots;
 internal static int GapChargeTicks,GapAutoEvents,GapLaserContacts;
 internal static readonly List<Projectile> GapShots=new List<Projectile>();
 internal sealed class GapSpecial
 {
  internal AIComponent AI;internal AISpecial Special;internal float Started;
  internal readonly List<float> Sounds=new List<float>(),Spawns=new List<float>();
  internal float ArmedSound=-1;internal Collider2D[] Children;internal bool DisabledAtSound;
 }
 internal static GapSpecial GapUse;
 internal static GameObject GapContact;
 void GapShootEvent(object sender,object args)
 {if(sender is ChargedShooter)GapAutoEvents++;}
 IEnumerator GapCamera(GameController game)
 {
  var p=PlayerController.Instance;var rig=Object.FindObjectOfType<CameraRig>();var parent=rig.transform.parent;var before=rig.transform.position-parent.position;
  Require(parent==p.transform||parent.IsChildOf(p.transform),"source camera rig remains in the actual player hierarchy");
  var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*8);
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});yield return null;yield return null;
  CombatClose(rig.transform.localPosition.magnitude,0,"source camera rig ignores actual far mouse input");
  var start=p.transform.position;InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.W));
  while((p.transform.position-start).sqrMagnitude<.05f)yield return null;
  InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());yield return null;
  CombatClose((rig.transform.position-parent.position-before).magnitude,0,"actual native keyboard movement retains the rig offset from its authored player parent");
 }
 IEnumerator GapChargedInput(GameController game,bool physical,bool controller=false)
 {
  var p=PlayerController.Instance;while(!(p.CurrentState is flanne.Player.IdleState)||!p.ammo.fullOnAmmo)yield return null;
  var shooter=(ChargedShooter)p.gun.shooters[0];int maximum=(int)WindField(shooter,"maxCharge");
  Require((int)WindField(shooter,"_charge")==-1,"fresh serialized charged shooter begins at source unsaved phase -1");
  GapShots.Clear();GapChargeTicks=0;GapAutoEvents=0;GapCaptureShots=true;
  this.AddObserver(GapShootEvent,Gun.ShootEvent);
  var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*8);
  Gamepad pad=null;
  if(controller)
  {
   pad=InputSystem.AddDevice<Gamepad>("RetainedChargeController");game.playerInput.SwitchCurrentControlScheme("Gamepad",pad);
   var state=new GamepadState{rightStick=Vector2.right};var paths=game.playerInput.actions["Fire"].bindings.Select(b=>b.effectivePath).Where(path=>path!=null&&path.StartsWith("<Gamepad>/")).ToArray();
   Require(paths.Any(path=>path.EndsWith("/rightTrigger")||path.EndsWith("/rightShoulder")),"native charged controller Fire binding exists");
   if(paths.Any(path=>path.EndsWith("/rightTrigger")))state.rightTrigger=1;else state=state.WithButton(GamepadButton.RightShoulder);
   InputSystem.QueueStateEvent(pad,state);
  }
  else{game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));}
  // This observes the actual next native shot tick, so the old max-charge
  // no-op fails deterministically instead of waiting for a timeout/shot.
  while(GapChargeTicks<maximum+2)yield return null;
  Require(GapShots.Count==p.gun.numOfProjectiles&&GapAutoEvents==1,"native held fire posts one shooter-owned full-charge event and emits before release: "+p.gun.gunData.name);
  Require((int)WindField(shooter,"_charge")==-1,"source auto-release resets the actual serialized charge state");
  Require(p.ammo.amount<p.gun.maxAmmo,"actual full-charge emission reaches native ammo callbacks");
  GapCaptureShots=false;this.RemoveObserver(GapShootEvent,Gun.ShootEvent);
  if(controller)InputSystem.QueueStateEvent(pad,new GamepadState());else InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});
  if(physical)
  {
   var shot=GapShots.First();var target=StatusEnemy();target.transform.position=p.transform.position+Vector3.right*3;int hp=target.HP;
   shot.transform.position=target.transform.position;shot.GetComponent<MoveComponent2D>().vector=Vector2.zero;Physics2D.SyncTransforms();
   while(target.HP==hp)yield return null;
   Require(hp-target.HP==(int)shot.damage,"actual full-charge projectile applies its emitted Bullet damage through physical source contact");yield return StatusReturn(target);
  }
  while(!(p.CurrentState is flanne.Player.IdleState)||!p.ammo.fullOnAmmo)yield return null;
  Require(!p.gun.isShooting,"native held/released charged input completes original reload/idle flow");
  if(controller){game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);InputSystem.RemoveDevice(pad);}
 }
 void GapReleaseEdges()
 {
  var p=PlayerController.Instance;var shooter=(ChargedShooter)p.gun.shooters[0];GapShots.Clear();GapCaptureShots=true;
  Log("fixture-precondition","Directed native release edge calls explicitly set initial charge/ammo; ordinary held input and real projectile/health contact are verified separately.");
  Plugin.SetField(shooter,"_charge",5);Plugin.SetField(p.ammo,"<amount>k__BackingField",0);
  var empty=p.gun.GetProjectileRecipe();float damage=empty.damage,size=empty.size;
  shooter.OnStopShoot(empty,Vector2.right,1,0,0);
  Require(GapShots.Count==0&&(int)WindField(shooter,"_charge")==5&&empty.damage==damage&&empty.size==size,"source empty-ammo release emits nothing and preserves charge/recipe");
  p.ammo.Reload();Plugin.SetField(shooter,"_charge",-1);var initial=p.gun.GetProjectileRecipe();damage=initial.damage;size=initial.size;int pierce=initial.piercing;
  shooter.OnStopShoot(initial,Vector2.right,1,0,0);
  Require(GapShots.Count==1&&initial.damage==damage&&initial.size==size&&initial.piercing==pierce,"source initial negative charge clamps to zero before real base emission");
  GapCaptureShots=false;p.ammo.Reload();
 }
 IEnumerator GapLaser(bool continuation,string actorName="Shoggoth")
 {
  SetPhase("retained-laser-rearm-"+actorName);GapUse=null;var actor=EnemyActor(actorName,Vector3.right*2);var ai=actor.GetComponent<AIComponent>();ai.specialTimer=0;
  while(GapUse==null||GapUse.AI!=ai)yield return null;
  var special=(AILasersSpecial)GapUse.Special;
  for(int attack=0;attack<2;attack++)
  {
   if(attack!=0){GapUse=null;ai.specialTimer=0;ai.frozen=false;while(GapUse==null||GapUse.AI!=ai)yield return null;}
   var use=GapUse;while(use.ArmedSound<0)yield return null;
   Require(use.DisabledAtSound&&use.Children.Length>0,"actual source AI laser sound follows direct-child collider disable pass");
   while(!use.Children.All(c=>c.enabled&&c.gameObject.activeInHierarchy))yield return null;
   Require(Time.time-use.ArmedSound>=.1f-Time.deltaTime,"actual AI child colliders arm only after the source additional0.1-second scaled wait");
   EnemyClose(Time.time-use.ArmedSound,.1f,"source laser arming cadence",.06f);
   if(actorName!="Shoggoth")continue;
   var laser=use.Children.First(c=>c.name.Contains("PF_Laser"));int contacts=GapLaserContacts;GapContact=laser.gameObject;
   laser.transform.position+=PlayerController.Instance.playerHealth.transform.position-laser.bounds.center;Physics2D.SyncTransforms();
   while(GapLaserContacts==contacts)yield return null;GapContact=null;
   Require(!laser.enabled&&laser.gameObject.activeInHierarchy,"actual protected laser contact disables only the collider");
   Log("retained-laser-contact",new{attack,continuation,actor=actor.GetInstanceID(),collider=laser.GetInstanceID()});
  }
  ReturnFixtureActor(actor);GapUse=null;
 }
 IEnumerator GapSpawnSounds()
 {
  foreach(string name in new[]{"PF_Yog","PF_Reaper"})
  {
   SetPhase("retained-spawn-cue-"+name);GapUse=null;var actor=EnemyActor(name,Vector3.right*3);var ai=actor.GetComponent<AIComponent>();ai.specialTimer=0;
   while(GapUse==null||GapUse.AI!=ai)yield return null;
   var use=GapUse;int expected=(int)WindField(use.Special,"numSpawns");float windup=(float)WindField(use.Special,"spawnWinduptime");
   Require(expected==(name=="PF_Yog"?3:2)&&use.Sounds.Count==1&&use.Spawns.Count==0,"actual spawn special plays one immediate original cue before any delayed spawn");
   CombatClose(use.Sounds[0]-use.Started,0,"source spawn cue is owned by the actual Use activation");
   while(use.Spawns.Count<expected)yield return null;
   Require(use.Sounds.Count==1&&use.Spawns.Count==expected,"complete real spawn iterator emits all source batches without repeating the warning");
   for(int i=0;i<expected;i++)Require(use.Spawns[i]-use.Started>=windup*(i+1)-.01f,"source spawn windups remain ordered");
   Log("retained-spawn-cue",new{name,calls=use.Sounds.Count,batches=use.Spawns.Count,windup});WorldReturnEnemies();GapUse=null;
  }
 }
 IEnumerator CheckRetainedGaps(bool continuation)
 {
  Log("fixture-precondition","Real keyboard/mouse held charge, original level20 evolution menus, source-pooled AI producers and protected physical beam contact. Encounter/game clocks stopped, empty runes, explicit level/charge/ammo edge operands. No natural full-run/frequency/Android-device claim.");
  GameController game;
  if(!continuation)
  {
   RunResume.Invalidate();Progression.State.earned.Add("weapon:CrossbowData");Progression.Refresh();
   foreach(string evolution in new[]{"", "QuantumBoltGun", "QuickLoadGun", "WindBoltGun"})
   {
    yield return AchievementRun("Shana","CrossbowData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
    game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
    if(evolution!="")yield return ArrowEvolution(game,PlayerController.Instance.gun.gunData.gunEvolutions.Single(e=>((GunData)WindField(e,"gunData")).name==evolution).name);
    yield return GapCamera(game);yield return GapChargedInput(game,evolution=="");
    if(evolution=="")GapReleaseEdges();
    if(evolution!="WindBoltGun")yield return AchievementExit(game,false);
   }
  }
  else
  {
   yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
   Require(PlayerController.Instance.gun.gunData.name=="WindBoltGun"&&(int)WindField(PlayerController.Instance.gun.shooters[0],"_charge")==-1,"exact fresh-process Continue restores Wind Bolt without the source-unsaved charge phase");
   yield return GapCamera(game);yield return GapChargedInput(game,false);yield return GapChargedInput(game,false,true);
  }
  game=Object.FindObjectOfType<GameController>();yield return GapLaser(continuation);yield return GapLaser(continuation,"PF_TentacleEnemy");yield return GapSpawnSounds();
  if(!continuation)
  {
   var p=PlayerController.Instance;var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*8);
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));
   while((int)WindField(p.gun.shooters[0],"_charge")<2)yield return null;
   yield return DragonSuspend(game,"Four actual Crossbow recipes auto-fire/release, source release edges, camera follow/aim, real laser rearm and complete spawn cue ownership; save an actual held unsaved charge");
  }
  else
  {
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();yield return CheckRevolver(game,PlayerController.Instance,"retained-gaps-original-restart.png");yield return AchievementExit(game,false);
   RunResume.Invalidate();yield return OpenCustomConfiguration("CrossbowData");string ownership=Ownership();
   Click(CustomButton("CustomTab1"));CustomValue(0,"27");Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>();Click(CustomButton("CustomStart"));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
   StopFixtureSpawning();GameTimer.SharedInstance.Stop();PlayerController.Instance.playerHealth.isInvincible.Flip();
   Require(CustomMode.Active&&SelectedMap.MapData.achievementsDisabled&&PlayerController.Instance.gun.gunData.damage==27,"actual obtained Custom Crossbow uses the edited recipe and existing no-achievement boundary");
   yield return GapCamera(game);yield return GapChargedInput(game,true);yield return GapSpawnSounds();yield return AchievementExit(game,false);
   Require(Ownership()==ownership,"affected Custom charged/spawn workflows preserve all ownership and achievement records");
   SetPhase("PASS");Log("complete","Exact charged source Continue, mouse/controller input, both actual AI laser recipes, spawn workflows, source camera follow, ordinary restart and obtained Custom Crossbow");Application.Quit(0);
  }
 }
}
[HarmonyPatch(typeof(ChargedShooter),nameof(ChargedShooter.Shoot))]
class GapChargeProbe{static void Postfix(){if(RuntimeChecks.GapMode&&RuntimeChecks.GapCaptureShots)RuntimeChecks.GapChargeTicks++;}}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class GapProjectileProbe{static void Postfix(bool __4,Projectile __result){if(RuntimeChecks.GapMode&&RuntimeChecks.GapCaptureShots&&!__4)RuntimeChecks.GapShots.Add(__result);}}
[HarmonyPatch(typeof(AILasersSpecial),nameof(AILasersSpecial.Use))]
class GapLaserUseProbe
{
 [HarmonyPriority(Priority.First)]static void Prefix(AILasersSpecial __instance,AIComponent ai){if(RuntimeChecks.GapMode)RuntimeChecks.GapUse=new RuntimeChecks.GapSpecial{AI=ai,Special=__instance,Started=Time.time};}
 static void Postfix(AIComponent ai){if(RuntimeChecks.GapMode){ai.frozen=true;ai.moveComponent.vector=Vector2.zero;}}
}
[HarmonyPatch(typeof(AISpawnSpecial),nameof(AISpawnSpecial.Use))]
class GapSpawnUseProbe
{
 [HarmonyPriority(Priority.First)]static void Prefix(AISpawnSpecial __instance,AIComponent ai){if(RuntimeChecks.GapMode)RuntimeChecks.GapUse=new RuntimeChecks.GapSpecial{AI=ai,Special=__instance,Started=Time.time};}
 static void Postfix(AIComponent ai){if(RuntimeChecks.GapMode){ai.frozen=true;ai.moveComponent.vector=Vector2.zero;}}
}
[HarmonyPatch(typeof(SoundEffectSO),nameof(SoundEffectSO.Play))]
class GapCueProbe
{
 [HarmonyPriority(Priority.First)]static void Prefix(SoundEffectSO __instance)
 {
  var use=RuntimeChecks.GapUse;if(!RuntimeChecks.GapMode||use==null)return;
  if(use.Special is AILasersSpecial laser&&ReferenceEquals(RuntimeChecks.EnemyField(laser,"laserSFX"),__instance))
  {
   use.ArmedSound=Time.time;string tag=(string)RuntimeChecks.EnemyField(laser,"laserTag");
   var group=Enumerable.Range(0,use.AI.transform.childCount).Select(i=>use.AI.transform.GetChild(i)).Single(t=>t.tag==tag);
   use.Children=Enumerable.Range(0,group.childCount).Select(i=>group.GetChild(i).GetComponent<Collider2D>()).Where(c=>c!=null).ToArray();
   use.DisabledAtSound=use.Children.Length>0&&use.Children.All(c=>!c.enabled&&c.gameObject.activeInHierarchy);
  }
  if(use.Special is AISpawnSpecial spawn&&ReferenceEquals(RuntimeChecks.EnemyField(spawn,"soundFX"),__instance))use.Sounds.Add(Time.time);
 }
}
[HarmonyPatch(typeof(Spawner),nameof(Spawner.Spawn))]
class GapSpawnBatchProbe{static void Postfix(Spawner __instance){var use=RuntimeChecks.GapUse;if(RuntimeChecks.GapMode&&use?.Special is AISpawnSpecial&&__instance.transform.IsChildOf(use.AI.transform))use.Spawns.Add(Time.time);}}
[HarmonyPatch(typeof(AndroidPlayerCollision),"Enter")]
class GapLaserContactProbe{static void Postfix(Collision2D collision){if(RuntimeChecks.GapMode&&collision.gameObject==RuntimeChecks.GapContact)RuntimeChecks.GapLaserContacts++;}}
