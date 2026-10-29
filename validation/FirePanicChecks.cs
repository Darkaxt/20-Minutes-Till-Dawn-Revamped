using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 Vector2 FireVelocity(Health actor,float dt)=>(Vector2)AccessTools.Method(typeof(ElementalControl),"Movement").Invoke(Control(actor),new object[]{actor.GetComponent<MoveComponent2D>(),dt});
 IEnumerator CheckFirePanic(string mode)
 {
  if(mode=="fire-panic-continue"||mode=="fire-panic-prior")yield return CombatBuffContinue();
  else{RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());}
  var game=Object.FindObjectOfType<GameController>();var player=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  ElementClear();var origin=(Vector2)player.transform.position+Vector2.right*4;
  var actor=ElementEnemy(origin);var move=actor.GetComponent<MoveComponent2D>();move.enabled=false;move.vector=Vector2.left*2;
  BurnSystem.SharedInstance.Burn(actor.gameObject,3);var control=Control(actor);float fear=control.Fear;var stored=move.vector;
  FireVelocity(actor,.02f);Require(move.vector==stored,"fire panic leaves stored native pursuit velocity unchanged");
  if(mode=="fire-panic-baseline"){SetPhase("PASS");Application.Quit(0);yield break;}
  BurnSystem.SharedInstance.Burn(actor.gameObject,3);Require(control.Fear==fear,"repeat burn strengthens damage without restarting the panic loop");
  var partner=ElementEnemy(origin+Vector2.up*2);partner.GetComponent<MoveComponent2D>().enabled=false;BurnSystem.SharedInstance.Burn(partner.gameObject,3);
  Require((float)WindField(control,"panicTurn")==-(float)WindField(Control(partner),"panicTurn"),"simultaneous ignitions alternate their turn direction without consuming gameplay RNG");
  ReturnFixtureActor(partner.gameObject);
  float paused=control.Fear;PauseController.SharedInstance.Pause();yield return null;yield return new WaitForEndOfFrame();Require(control.Fear==paused,"native pause freezes panic phase");PauseController.SharedInstance.UnPause();
  var samples=new List<string>{"speed,time,x,y,fear,vx,vy"};
  foreach(float speed in new[]{2f,8f})
  {
   ElementClear();yield return null;actor=ElementEnemy(origin);move=actor.GetComponent<MoveComponent2D>();move.enabled=true;move.drag=0;move.vector=Vector2.left*speed;actor.GetComponent<AIComponent>().maxMoveSpeed=speed;
   BurnSystem.SharedInstance.Burn(actor.gameObject,3);control=Control(actor);float start=Time.time,maxDistance=0,sideMin=0,sideMax=0;Vector2 last=actor.transform.position;float length=0;bool inward=false,outward=false;
   while(control.Fear>0)
   {
    yield return new WaitForFixedUpdate();var position=(Vector2)actor.transform.position;Vector2 delta=position-last;length+=delta.magnitude;last=position;
    maxDistance=Mathf.Max(maxDistance,Vector2.Distance(position,origin));sideMin=Mathf.Min(sideMin,position.y-origin.y);sideMax=Mathf.Max(sideMax,position.y-origin.y);outward|=delta.x>.001f;inward|=delta.x<-.001f;
    samples.Add(string.Join(",",speed,Time.time-start,position.x-origin.x,position.y-origin.y,control.Fear,move.vector.x,move.vector.y));
   }
   Require(outward&&inward&&sideMax-sideMin>.02f,"actual native burning movement turns through a local loop at speed "+speed);
   Require(maxDistance<speed*.3f&&length>maxDistance*1.7f,"panic travels locally instead of running straight off screen at speed "+speed+" max="+maxDistance+" length="+length);
   Require(move.vector.x<0,"panic expiry retains native pursuit rather than outward momentum at speed "+speed);
   Vector2 resumed=FireVelocity(actor,.02f);Require(resumed.x<0,"normal pursuit resumes immediately while the remaining fire slow applies");
   yield return PoiCapture("fire-panic-speed-"+speed+".png");
  }
  File.WriteAllLines(Path.Combine(ResultDir,"fire-panic-paths.csv"),samples);
  ElementClear();yield return null;actor=ElementEnemy(origin);move=actor.GetComponent<MoveComponent2D>();move.enabled=false;move.vector=Vector2.left*3;BurnSystem.SharedInstance.Burn(actor.gameObject,3);control=Control(actor);
  ElementCall(control,"Electrocute");Require(FireVelocity(actor,.02f)==Vector2.zero,"electrical stun stops fire panic movement");
  actor.GetComponent<AIComponent>().frozen=true;Require(FireVelocity(actor,.02f)==Vector2.zero,"native freeze stops fire panic movement");actor.GetComponent<AIComponent>().frozen=false;
  ElementClear();yield return null;var boss=ElementEnemy(origin,tag:"EnemyChampion");BurnSystem.SharedInstance.Burn(boss.gameObject,3);Require(Control(boss).Fear==0&&Control(boss).SlowFraction==.1f,"champions retain slow-only fire response");ElementClear();yield return null;
  var reused=ElementEnemy(origin);Require(Control(reused)==null||(Control(reused).Fear==0&&Control(reused).Slow==0),"native pool reuse clears fire panic and slow");ElementClear();
  if(mode=="fire-panic-fresh")
  {yield return AndroidPick(game,"PyroMage",game.playerXP.level);yield return DragonSuspend(game,"Fire panic checkpoint: acquisitions persist; enemy-local panic remains transient");yield break;}
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Local fire panic native paths/pursuit, pause, damage refresh, stun/freeze/boss/pool ownership and Continue");Application.Quit(0);
 }
}
