using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal sealed class CannonFlight
 {
  internal Projectile Shot;internal Vector3 Origin;internal Vector2 Velocity;
  internal float Born,ReturnedAt;internal bool Returned,Expired;internal int Steps;
 }
 internal static bool CannonCapture;
 internal static readonly List<CannonFlight> CannonFlights=new List<CannonFlight>();
 internal static CannonFlight CannonLive(GameObject actor)=>CannonFlights.LastOrDefault(f=>!f.Returned&&f.Shot!=null&&ReferenceEquals(f.Shot.gameObject,actor));
 void CannonTemplate(GunData data,bool cannon)
 {
  var prefab=data.bullet;CombatClose(prefab.GetComponent<MoveComponent2D>().drag,cannon?3:1,"source movement drag on actual gun template");
  CombatClose((float)WindField(prefab.GetComponent<TimeToLive>(),"lifetime"),cannon?.8f:2,"source template lifetime");
  if(!cannon)return;
  Require(prefab.name=="PF_ChargeCannonProjectile","native gun recipe owns the dedicated source Cannon pool identity");
  CombatClose(prefab.GetComponent<CircleCollider2D>().radius,.15f,"source Cannon radius");
  var body=prefab.GetComponent<Rigidbody2D>();Require(body.bodyType==RigidbodyType2D.Kinematic&&body.useFullKinematicContacts,"source Cannon body/contact configuration");
  Require(WindField(prefab.GetComponent<Projectile>(),"trail")==null&&prefab.GetComponentsInChildren<TrailRenderer>(true).Length==0&&prefab.GetComponent<SpawnOnCollision>()==null,"source Cannon template has no trail or impact-spawn component");
 }
 IEnumerator CannonInput(GameController game,float hold,int count,int damage,bool contact=false)
 {
  var player=PlayerController.Instance;
  while(PauseController.isPaused||!(player.CurrentState is flanne.Player.IdleState)||!player.ammo.fullOnAmmo)yield return null;
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  int before=CannonFlights.Count,ammo=player.ammo.amount;var aim=(Vector2)Camera.main.WorldToScreenPoint(player.transform.position+Vector3.right*8);
  var charged=player.gun.shooters[0] as PortedShooter;CannonCapture=true;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));
  if(charged!=null){while(!charged.Charging||charged.ChargeTime<hold)yield return null;}
  else{while(CannonFlights.Count==before)yield return null;}
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});
  while(CannonFlights.Count==before)yield return null;CannonCapture=false;
  var emitted=CannonFlights.Skip(before).ToArray();Require(emitted.Length==count,"actual native Fire/release emits expected projectile count");
  Require(player.ammo.amount==ammo-1,"actual shot consumes exactly one ammo before source automatic reload");
  foreach(var flight in emitted)
  {
   var shot=flight.Shot;CombatClose(shot.damage,damage,"actual emitted source recipe damage");
   if(charged!=null)
   {
    CombatClose((float)WindField(shot.GetComponent<AndroidProjectile>(),"Radius"),.15f,"source sampler consumes owned Cannon radius");
    float size=charged.Mode==Evolution.OverloadShell&&hold>=1.5f?3:1;
    CombatClose((float)WindField(shot.GetComponent<AndroidProjectile>(),"Size"),size,"source charged/evolved sampler size");
    Require((shot.piercing>=999)==(hold>=1.5f||charged.Mode==Evolution.ArmorPierce),"source charge/evolution piercing on actual emitted actor");
   }
  }
  if(contact)
  {
   var target=StatusEnemy();target.transform.position=player.transform.position+Vector3.right*3;int hp=target.HP;
   foreach(var flight in emitted){flight.Shot.transform.position=target.transform.position;var move=flight.Shot.GetComponent<MoveComponent2D>();move.vector=Vector2.zero;move.enabled=true;}
   Physics2D.SyncTransforms();while(target.HP==hp)yield return null;
   // Both Dual projectiles participate in the same actual fixed-step sampler.
   Require(hp-target.HP==damage*count,"actual source projectile contacts apply the complete charged/evolved damage");StatusReturn(target);
  }
  else
  {
   if(charged==null)
   {
    while(emitted[0].Steps<5)yield return null;
    emitted[0].Shot.GetComponent<MoveComponent2D>().enabled=false;
    Log("fixture-precondition","Revolver moves for five real fixed steps, then movement is parked to isolate its full two-second lifetime from offscreen disposal.");
   }
   foreach(var flight in emitted)
   {
    while(!flight.Returned)yield return null;
    Require(flight.Expired,"actual projectile return reaches native TimeToLive.Deactivate rather than offscreen/contact disposal");
    EnemyClose(flight.ReturnedAt-flight.Born,charged==null?2:.8f,"actual source lifetime and pooled release",.06f);
    Require(flight.Steps>=3,"actual emitted projectile participates in multiple source fixed-step movements");
    Log("source-projectile-flight",new{recipe=player.gun.gunData.name,actor=flight.Shot.GetInstanceID(),seconds=flight.ReturnedAt-flight.Born,steps=flight.Steps,origin=flight.Origin,velocity=flight.Velocity});
   }
  }
  while(emitted.Any(f=>!f.Returned)||!(player.CurrentState is flanne.Player.IdleState)||!player.ammo.fullOnAmmo)yield return null;
 }
 IEnumerator CheckAndroidCannon(bool continuation)
 {
  GameController game;
  if(continuation)
  {
   yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
   var shooter=(PortedShooter)PlayerController.Instance.gun.shooters[0];
   Require(shooter.Mode==Evolution.DualBarrage&&!shooter.Charging&&shooter.ChargeTime==0,"exact source Continue restores evolved Cannon and omits the unsaved held-charge phase");
   CannonTemplate(PlayerController.Instance.gun.gunData,true);yield return CannonInput(game,1.6f,2,64);yield return CannonInput(game,1.6f,2,64,true);
   yield return AchievementExit(game,false);
   yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();Require(!(PlayerController.Instance.gun.shooters[0] is PortedShooter),"ordinary native restart removes the charge owner");CannonTemplate(PlayerController.Instance.gun.gunData,false);yield return CannonInput(game,0,1,20);yield return AchievementExit(game,false);
   SetPhase("PASS");Log("complete","Source Cannon template/input/expiry/reuse/evolutions, exact held-charge Continue and original restart");Application.Quit(0);yield break;
  }
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();CannonTemplate(PlayerController.Instance.gun.gunData,false);Require(PlayerController.Instance.gun.gunData.damage==20,"unchanged native Revolver base damage is20; no old menu screenshot operand is used");yield return CannonInput(game,0,1,20);
  var revolver=CannonFlights.Last().Shot;yield return AchievementExit(game,false);
  Progression.State.earned.Add("weapon:"+Plugin.BaseGun.name);Progression.Refresh();yield return AchievementRun("Shana",Plugin.BaseGun.name,"20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();var player=PlayerController.Instance;CannonTemplate(player.gun.gunData,true);
  var recipes=Resources.FindObjectsOfTypeAll<GunData>().Where(d=>d.name.StartsWith("ModernizationChargeCannon_")).ToArray();
  Require(recipes.Length==4&&recipes.All(d=>ReferenceEquals(d.bullet,Plugin.BaseGun.bullet))&&!ReferenceEquals(Plugin.BaseGun.bullet,Resources.FindObjectsOfTypeAll<GunData>().First(d=>d.name=="RevolverData").bullet),"all four Cannon recipes share one dedicated template distinct from Revolver");
  yield return CannonInput(game,.1f,1,20);var reused=CannonFlights.Last().Shot;Require(!ReferenceEquals(revolver,reused),"native Revolver and Cannon do not reuse the same projectile actor");
  yield return CannonInput(game,.9f,1,44);Require(ReferenceEquals(reused,CannonFlights.Last().Shot),"actual next Cannon input reuses the naturally expired pooled actor");yield return CannonInput(game,1.6f,1,80,true);
  foreach(var mode in new[]{Evolution.OverloadShell,Evolution.ArmorPierce,Evolution.DualBarrage})
  {
   player.gun.LoadGun(Plugin.BaseGun);player.ammo.Reload();yield return ArrowEvolution(game,"Modernization_"+mode);
   while(!((PortedShooter)player.gun.shooters[0]).Ready)yield return null;CannonTemplate(player.gun.gunData,true);
   int damage=mode==Evolution.ArmorPierce?92:mode==Evolution.DualBarrage?64:80,count=mode==Evolution.DualBarrage?2:1;
   if(mode==Evolution.ArmorPierce)yield return CannonInput(game,.1f,1,20,true);
   yield return CannonInput(game,1.6f,count,damage);Require(CannonFlights.Skip(CannonFlights.Count-count).Any(f=>ReferenceEquals(f.Shot,reused)),"actual evolved input retains the shared Cannon pool identity: "+mode);yield return CannonInput(game,1.6f,count,damage,true);
  }
  var aim=(Vector2)Camera.main.WorldToScreenPoint(player.transform.position+Vector3.right*8);InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));var held=(PortedShooter)player.gun.shooters[0];while(!held.Charging||held.ChargeTime<.3f)yield return null;
  File.WriteAllText(System.IO.Path.Combine(ResultDir,"cannon-checkpoint-phase.txt"),held.Mode+":"+held.ChargeTime);
  yield return DragonSuspend(game,"Native Revolver/Cannon templates, real flight/expiry/reuse, charge tiers, all three level20 evolutions and physical contact; save during an actual unsaved held charge");
 }
}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class CannonFactoryProbe
{
 static void Postfix(bool __4,Projectile __result)
 {if(!RuntimeChecks.CannonCapture||__4)return;RuntimeChecks.CannonFlights.Add(new RuntimeChecks.CannonFlight{Shot=__result,Origin=__result.transform.position,Velocity=__result.GetComponent<MoveComponent2D>().vector,Born=Time.time});}
}
[HarmonyPatch(typeof(TimeToLive),"Deactivate")]
class CannonExpiryProbe
{[HarmonyPriority(Priority.First)]static void Prefix(TimeToLive __instance){var flight=RuntimeChecks.CannonLive(__instance.gameObject);if(flight!=null)flight.Expired=true;}}
[HarmonyPatch(typeof(AndroidPools),"Release")]
class CannonReturnProbe
{
 static void Prefix(GameObject value,out RuntimeChecks.CannonFlight __state)=>__state=RuntimeChecks.CannonLive(value);
 static void Postfix(GameObject value,RuntimeChecks.CannonFlight __state){if(__state!=null&&!value.activeSelf){__state.Returned=true;__state.ReturnedAt=Time.time;}}
}
[HarmonyPatch(typeof(MoveSystem2D),"FixedUpdate")]
class CannonMovementProbe
{
 sealed class Step{internal RuntimeChecks.CannonFlight Flight;internal Vector2 Before;internal Vector3 Position;}
 [HarmonyPriority(Priority.First)]static void Prefix(out List<Step> __state)
 {__state=RuntimeChecks.CannonFlights.Where(f=>!f.Returned&&f.Shot.gameObject.activeInHierarchy&&f.Shot.GetComponent<MoveComponent2D>().enabled&&MoveSystem2D.moveComponents.Contains(f.Shot.GetComponent<MoveComponent2D>())).Select(f=>new Step{Flight=f,Before=f.Shot.GetComponent<MoveComponent2D>().vector,Position=f.Shot.transform.position}).ToList();}
 static void Postfix(List<Step> __state)
 {
  foreach(var step in __state)
  {if(step.Flight.Returned)continue;var move=step.Flight.Shot.GetComponent<MoveComponent2D>();var expected=step.Before*Mathf.Exp(-move.drag*Time.fixedDeltaTime);if(Vector2.Distance(move.vector,expected)>=.002f)RuntimeChecks.EnemyCheck(false,"actual source projectile exponential drag: before="+step.Before+", expected="+expected+", actual="+move.vector+", registrations="+MoveSystem2D.moveComponents.Count(m=>m==move));if(Vector3.Distance(move.transform.position,step.Position+(Vector3)(expected*Time.fixedDeltaTime))>=.002f)RuntimeChecks.EnemyCheck(false,"actual source projectile displacement");step.Flight.Steps++;}
 }
}
