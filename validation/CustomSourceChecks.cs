using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool CustomTickPending,CustomInputProbe;
 internal static int CustomTicks;
 internal static float CustomEmittedDamage;
 [Serializable]class CustomSourceExpected{public float damage,size,speed,move;public int magazine,points;public string ownership;}
 CustomSourceExpected CustomSourceValues()
 {var p=PlayerController.Instance;var r=p.gun.GetProjectileRecipe();return new CustomSourceExpected{damage=r.damage,size=r.size,speed=r.projectileSpeed,move=p.movementSpeed,magazine=p.gun.maxAmmo,points=PointsTracker.pts,ownership=Ownership()};}
 IEnumerator CustomSourceSuspend(GameController game)
 {
  File.WriteAllText(Path.Combine(ResultDir,"custom-source-expected.json"),JsonUtility.ToJson(CustomSourceValues(),true));
  yield return DragonSuspend(game,"Actual native Custom controls, source spawning/input/current perks and global XP settings saved through normal Save & return");
 }
 IEnumerator CheckCustomSourceContinue()
 {
  string fixture=Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR");var expected=JsonUtility.FromJson<CustomSourceExpected>(File.ReadAllText(Path.Combine(fixture,"custom-source-expected.json")));
  byte[] original=File.ReadAllBytes(RunCheckpoint);var decoded=RunResume.Read();Require(decoded.version==7&&decoded.custom!=null,"explicit Custom checkpoint uses current source schema");Require(original.SequenceEqual(File.ReadAllBytes(RunCheckpoint)),"reading the actual Custom checkpoint leaves its bytes unchanged");
  yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var actual=CustomSourceValues();
  Require(CustomMode.Active&&SelectedMap.MapData.achievementsDisabled&&CustomMode.Get("stat:BulletDamage")==3&&CustomMode.Get("xp")==2,"fresh native Continue restores global Custom rules and the no-achievement map boundary");
  CombatClose(actual.damage,expected.damage,"exact source Continue retains the effective Custom weapon/perk/stat damage");CombatClose(actual.size,expected.size,"exact source Continue retains Custom projectile size");CombatClose(actual.speed,expected.speed,"exact source Continue retains Custom projectile speed");CombatClose(actual.move,expected.move,"exact source Continue retains base character movement");
  Require(actual.magazine==expected.magazine&&actual.points==expected.points&&actual.ownership==expected.ownership,"Custom Save & return/Continue preserves magazine, credits and all existing ownership/rune/clear/encounter records");
  yield return CustomSourceSpawns(game);
  float before=(float)WindField(game.playerXP,"xp");var xp=ResumeRuntime.Capture(game.playerXP.xpMultiplier);game.playerXP.GainXP(1);float after=(float)WindField(game.playerXP,"xp");CombatClose(after-before,2*(1+xp.flat)*(1+xp.bonus)*xp.reduction,"actual XP gain after source Continue retains the configured factor2");
  yield return CheckRevolver(game,PlayerController.Instance,"custom-source-continued-shooting.png");
  int points=PointsTracker.pts;yield return AchievementWin(game);Require(Ownership()==expected.ownership&&PointsTracker.pts>points,"continued Custom victory grants credits and preserves all unlock/achievement/rune/clear records");
  yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();
  Require(!CustomMode.Active&&!SelectedMap.MapData.achievementsDisabled&&PlayerController.Instance.gun.gunData.damage==20,"ordinary native restart removes Custom weapon/scenario rules");yield return CheckRevolver(game,PlayerController.Instance,"custom-source-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Exact current Custom Continue, effective edited stats/perks, source spawn factors/XP, real input, credits-only victory and ordinary restart");Application.Quit(0);
 }
 IEnumerator CustomCommand(string label)
 {
  var pagination=CustomMenu.Instance.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="CustomPage").text.Split('/');int current=int.Parse(pagination[0]),pages=int.Parse(pagination[1]);
  for(int page=1;page<current;page++)Click(CustomButton("CustomPrevious"));
  TMP_Text row=null;
  for(int page=0;page<pages;page++)
  {row=CustomMenu.Instance.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t=>t.name.StartsWith("CustomRow")&&t.text==label);if(row!=null)break;Click(CustomButton("CustomNext"));}
  Require(row!=null,"actual Custom menu exposes command: "+label);Click(CustomButton(row.name.Replace("CustomRow","CustomAction")));yield return null;
 }
 IEnumerator CustomSourceSpawns(GameController game)
 {
  SetPhase("custom-source-menu-spawning");StopFixtureSpawning();var run=Object.FindObjectOfType<AndroidEndlessRun>();var horde=Object.FindObjectOfType<HordeSpawner>();var boss=Object.FindObjectOfType<BossSpawner>();
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.pauseMenu.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="CustomSettings"));Click(CustomButton("CustomTab6"));
  var map=SelectedMap.MapData;
  foreach(bool elite in new[]{false,true})
  {
   var wave=map.spawnSessions.First(w=>w.isElite==elite);var before=Object.FindObjectsOfType<Health>().Select(h=>h.GetInstanceID()).ToArray();
   string label=System.Text.RegularExpressions.Regex.Replace(wave.monsterPrefab.name.Replace("PF_","").Replace("(Clone)",""),"([a-z0-9])([A-Z])","$1 $2").Replace('_',' ').Trim();
   yield return CustomCommand("Spawn: "+label);
   var actor=Object.FindObjectsOfType<Health>().Single(h=>!before.Contains(h.GetInstanceID())&&h.name==wave.monsterPrefab.name);var ai=actor.GetComponent<AIComponent>();
   float hp=elite?horde.eliteHealthMultiplier:horde.healthMultiplier,speed=elite?horde.eliteSpeedMultiplier:horde.speedMultiplier;int damage=elite?horde.eliteDamage:horde.enemyDamage;
   Require(actor.maxHP==Mathf.FloorToInt(wave.HP*hp*2),"actual Custom object-menu source spawn applies health factor once: elite="+elite);
   CombatClose(ai.maxMoveSpeed,ai.baseMaxMoveSpeed*speed*CustomMode.Get("spawn:"+(elite?"eliteSpeedMultiplier":"speedMultiplier")),"actual Custom menu speed factor reaches source spawning once");Require(ai.damageToPlayer==(int)(damage*CustomMode.Get("spawn:"+(elite?"eliteDamage":"enemyDamage"))),"actual Custom menu contact-damage factor reaches source spawning once");ReturnFixtureActor(actor.gameObject);
  }
  var original=map.bossSpawns[0];var ids=Object.FindObjectsOfType<Health>().Select(h=>h.GetInstanceID()).ToArray();
  string bossLabel=System.Text.RegularExpressions.Regex.Replace(original.bossPrefab.name.Replace("PF_","").Replace("(Clone)",""),"([a-z0-9])([A-Z])","$1 $2").Replace('_',' ').Trim();yield return CustomCommand("Spawn: "+bossLabel);
  var spawned=Object.FindObjectsOfType<Health>().Single(h=>!ids.Contains(h.GetInstanceID())&&h.name==original.bossPrefab.name);
  Require(spawned.maxHP==Mathf.FloorToInt(original.maxHP*boss.healthMultiplier*2),"actual Custom object-menu boss uses source boss-health factor once");
  Require(spawned.GetComponent<AIComponent>().damageToPlayer==(int)(boss.enemyDamage*CustomMode.Get("spawn:bossDamage")),"actual Custom menu boss contact-damage factor reaches source spawning once");
  CombatClose((float)WindField(spawned.GetComponent<AndroidAIActor>(),"SpecialCooldownMultiplier"),boss.cooldownRate*CustomMode.Get("spawn:bossCooldown"),"actual Custom menu boss cooldown factor reaches the source AI owner once");ReturnFixtureActor(spawned.gameObject);
  Click(CustomButton("CustomBack"));Click(game.pauseResumeButton);while(!(game.CurrentState is CombatState)||PauseController.isPaused)yield return null;
  // One real Unity Update, with explicit wave eligibility/clock operands, proves
  // the Custom density setting at the source scheduler rather than native Horde.Update.
  WorldClearSchedule(run);WorldTime(0);var session=WorldSession(run,0);float start=session.startTime,duration=session.duration,timer=session.timer;var maximum=session.maximum;int amount=session.numPerSpawn;
  session.startTime=0;session.duration=100;session.timer=-1;session.maximum=1000;((List<int>)WindField(run,"active")).Add(0);
  int expected=Mathf.FloorToInt(horde.spawnRateMulitplier*amount*CustomMode.Get("spawn:spawnRateMulitplier")),beforeCount=Object.FindObjectsOfType<Health>().Count(h=>h.gameObject.layer==LayerMask.NameToLayer("Enemy"));
  CustomTickPending=true;int tick=CustomTicks;run.enabled=true;while(CustomTicks==tick)yield return null;
  int afterCount=Object.FindObjectsOfType<Health>().Count(h=>h.gameObject.layer==LayerMask.NameToLayer("Enemy"));Require(afterCount-beforeCount==Math.Min(5,expected)&&WorldQueue(run).Count==Math.Max(0,expected-5),"actual source scheduler applies Custom spawn amount once before its five-actor batch");
  session.startTime=start;session.duration=duration;session.timer=timer;session.maximum=maximum;WorldClearSchedule(run);WorldReturnEnemies();
  Log("fixture-precondition","Source scheduler receives one explicitly eligible wave; native menu spawns and this real Unity tick are controlled integration checks, not natural encounter progression.");
 }
 IEnumerator CustomNativeInput(GameController game)
 {
  SetPhase("custom-native-input-transitions");var player=PlayerController.Instance;var gun=player.gun;var mouse=fixtureMouse;var aim=(Vector2)Camera.main.WorldToScreenPoint(player.transform.position+Vector3.right*5);
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,mouse);game.playerInput.neverAutoSwitchControlSchemes=true;CustomInputProbe=true;
  while(!(player.CurrentState is flanne.Player.IdleState)||!player.ammo.fullOnAmmo)yield return null;
  int before=Projectiles;InputSystem.QueueStateEvent(mouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(Projectiles==before)yield return null;
  CombatClose(CustomEmittedDamage,66,"actual emitted Custom shot combines isolated base33 and BulletDamage factor2");
  InputSystem.QueueStateEvent(mouse,new MouseState{position=aim});
  while(player.CurrentState is flanne.Player.ShootingState&&WindField(player.CurrentState,"_changeStateCoroutine")==null)yield return null;
  Require(player.CurrentState is flanne.Player.ShootingState&&!gun.shotReady,"native release enters the source pending state transition during the configured two-second shot cooldown");
  InputSystem.QueueStateEvent(mouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(Projectiles==before+1)yield return null;
  Require(player.CurrentState is flanne.Player.ShootingState&&Projectiles==before+2,"native re-fire cancels the pending transition and emits the next real shot without an intervening reload");
  InputSystem.QueueStateEvent(mouse,new MouseState{position=aim});while(!(player.CurrentState is flanne.Player.IdleState)||!player.ammo.fullOnAmmo)yield return null;
  before=Projectiles;InputSystem.QueueStateEvent(mouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(Projectiles==before)yield return null;
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();int paused=Projectiles;InputSystem.QueueStateEvent(mouse,new MouseState{position=aim});yield return Capture("custom-input-native-pause.png");
  Require(PauseController.isPaused&&Projectiles==paused,"actual pause stops source firing while native release input is queued");Click(game.pauseResumeButton);
  while(!(player.CurrentState is flanne.Player.IdleState)||!player.ammo.fullOnAmmo)yield return null;Require(Projectiles==paused,"native Resume completes source release/reload without another held-fire shot");
  var pad=InputSystem.AddDevice<Gamepad>("RevampedCustomInput");game.playerInput.SwitchCurrentControlScheme("Gamepad",pad);var state=new GamepadState{rightStick=Vector2.right};
  var bindings=game.playerInput.actions["Fire"].bindings.Where(b=>b.effectivePath!=null&&b.effectivePath.StartsWith("<Gamepad>/")).Select(b=>b.effectivePath).ToArray();
  Require(bindings.Any(b=>b.EndsWith("/rightTrigger")||b.EndsWith("/rightShoulder")),"actual native controller Fire binding is resolved");if(bindings.Any(b=>b.EndsWith("/rightTrigger")))state.rightTrigger=1;else state=state.WithButton(GamepadButton.RightShoulder);
  before=Projectiles;InputSystem.QueueStateEvent(pad,state);while(Projectiles==before)yield return null;Require(gun.isShooting,"native controller aim/Fire enters source shooting");InputSystem.QueueStateEvent(pad,new GamepadState());
  while(!(player.CurrentState is flanne.Player.IdleState)||!player.ammo.fullOnAmmo)yield return null;Require(!gun.isShooting,"native controller release returns through source reload to idle");InputSystem.RemoveDevice(pad);game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,mouse);CustomInputProbe=false;
 }
}
[HarmonyPatch(typeof(AndroidEndlessRun),"Update")]
class CustomSourceTickProbe
{static void Postfix(AndroidEndlessRun __instance){if(!RuntimeChecks.CustomTickPending)return;RuntimeChecks.CustomTickPending=false;RuntimeChecks.CustomTicks++;__instance.enabled=false;}}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class CustomInputShotProbe
{static void Postfix(bool __4,Projectile __result){if(RuntimeChecks.CustomInputProbe&&!__4)RuntimeChecks.CustomEmittedDamage=__result.damage;}}
