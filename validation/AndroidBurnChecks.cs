using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckAndroidBurn()
 {
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest");
  var game=Object.FindObjectOfType<GameController>();var player=PlayerController.Instance;
  StopFixtureSpawning();var pool=ObjectPooler.SharedInstance;var target=DurableEnemy(pool.GetPooledObject("BrainMonster"),player.transform.position+Vector3.right*30,false);
  var hits=new List<int>();ObserveDamage(target,d=>{hits.Add(d);Log("native-burn-hit",d);});var burn=BurnSystem.SharedInstance;
  burn.Burn(target.gameObject,3);while(hits.Count<2)yield return null;
  Require(hits.Take(2).All(d=>d==3),"native burn starts with three damage each tick");burn.Burn(target.gameObject,3);
  while(hits.Count<5)yield return null;
  Require(hits.Skip(2).Take(3).All(d=>d==6),"Android repeated fire accumulates damage and refreshes the shared lifetime rather than expiring the first application");
  while(burn.IsBurning(target.gameObject))yield return null;
  Require(hits.Count==7&&hits.Skip(2).All(d=>d==6),"Android base five-tick lifetime refresh retains all accumulated damage for five further ticks");
  hits.Clear();burn.Burn(target.gameObject,2000);while(hits.Count==0)yield return null;
  Require(hits[0]==1000,"Android shipped scene cap limits each burn tick to one thousand before native damage processing");
  target.HPChange(-1000000);Require(target.isDead,"actual source damage kills the burning pooled actor");while(burn.IsBurning(target.gameObject))yield return new WaitForFixedUpdate();Require(!burn.IsBurning(target.gameObject),"source fixed-step burn owner releases the dead actor's status and effect");
  var replacement=DurableEnemy(pool.GetPooledObject("BrainMonster"),player.transform.position+Vector3.right*30,false);Require(ReferenceEquals(replacement,target)&&replacement.gameObject.activeInHierarchy&&!burn.IsBurning(replacement.gameObject),"actual source pool reuses the dead actor without its released burn state");ReturnFixtureActor(replacement.gameObject);
  yield return Capture("android-burn-native.png");yield return AchievementExit(game,false);
  yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();
  yield return CheckRevolver(game,PlayerController.Instance,"android-burn-original-restart.png");yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete","Android shared burn accumulation/refresh, five-tick duration, scene cap, actor cleanup and native restart");Application.Quit(0);
 }
}
