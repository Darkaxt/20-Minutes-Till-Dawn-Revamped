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
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool SporeMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-spores-");
 internal sealed class SporeLaunch{internal AreaProjectile Actor;internal Vector2 Target;internal float Duration;}
 internal static readonly List<SporeLaunch> SporeLaunches=new List<SporeLaunch>();
 internal static readonly List<int> SporeHarmKinds=new List<int>(),SporeHarmValues=new List<int>();
 internal static bool SporeLaunching,SporeCapturePrimary;
 internal static Projectile SporePrimary;
 internal static int SporeCalls,SporeObserved;
 static SporeFlower Flower()=>PlayerController.Instance.GetComponentInChildren<SporeFlower>();
 static GameObject[] SporeActive(string field)
 {var source=(Component)WindField(Flower(),field);return ObjectPooler.SharedInstance.pooledObjectsDictionary[source.name].Where(o=>o.activeSelf).ToArray();}
 IEnumerator SporeQuiet(){while(SporeActive("sporePrefab").Length>0||SporeActive("sporeExplosionPrefab").Length>0)yield return null;}
 IEnumerator SporeAutomatic(SporeFlower flower,bool bulletScale,int expected)
 {
  var p=PlayerController.Instance;flower.multiplyByBulletDamage=bulletScale;var target=StatusEnemy();target.transform.position=flower.transform.position+Vector3.up*2;
  var boundary=StatusEnemy();boundary.transform.position=flower.transform.position+Vector3.up*5;Physics2D.SyncTransforms();
  int before=SporeLaunches.Count,hp=target.HP;SporeObserved=target.gameObject.GetInstanceID();SporeHarmKinds.Clear();SporeHarmValues.Clear();
  Plugin.SetField(flower,"_timer",bulletScale?4.99f:0f);flower.enabled=true;while(SporeLaunches.Count==before)yield return null;flower.enabled=false;
  var launch=SporeLaunches[before];Require(SporeLaunches.Count==before+1&&Vector2.Distance(launch.Target,target.transform.position)<.01f&&launch.Duration==.7f,"actual Spore Flower Update selects the strict-radius source-pooled target and launches its native0.7-second arc");
  while(target.HP==hp)yield return null;yield return SporeQuiet();
  Require(hp-target.HP==expected&&boundary.HP==100000&&SporeHarmKinds.SequenceEqual(new[]{11})&&SporeHarmValues.SequenceEqual(new[]{-expected}),"actual spore landing/explosion applies source "+expected+" Powerup damage with strict boundary exclusion");
  Log("spore-automatic","bullet-scale="+bulletScale+" damage="+(hp-target.HP)+" kind="+SporeHarmKinds[0]+" target="+launch.Target+" duration="+launch.Duration);
  SporeObserved=0;yield return StatusReturn(target);yield return StatusReturn(boundary);
 }
 IEnumerator SporeNativeProjectile(GameController game,SporeFlower flower)
 {
  var p=PlayerController.Instance;while(!(p.CurrentState is flanne.Player.IdleState))yield return null;p.ammo.Reload();
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  SporePrimary=null;SporeCapturePrimary=true;var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*8);
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(SporePrimary==null)yield return null;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});SporeCapturePrimary=false;
  Require(SporePrimary.gameObject.activeSelf&&SporePrimary.GetComponent<AndroidProjectile>()!=null,"actual native Spore Gun input emits a source-sampled primary projectile");
  var movement=SporePrimary.GetComponent<MoveComponent2D>();SporePrimary.transform.position=flower.transform.position;movement.vector=Vector2.zero;movement.enabled=true;Physics2D.SyncTransforms();
  int calls=SporeCalls;while(SporeCalls==calls&&SporePrimary.gameObject.activeSelf)yield return null;
  Require(SporeCalls>calls,"actual emitted gun projectile reaches the source Spore Flower trigger through its physical sampler");ReturnPlayerShots();while(!(p.CurrentState is flanne.Player.IdleState))yield return null;
 }
 IEnumerator SporeSpray(GameController game,SporeFlower flower)
 {
  var offsets=new[]{new Vector3(0,2),new Vector3(0,-2),new Vector3(-2,2),new Vector3(-2,-2),new Vector3(2,2),new Vector3(2,-2)};
  var targets=offsets.Select(v=>{var h=StatusEnemy();h.transform.position=flower.transform.position+v;return h;}).ToArray();Physics2D.SyncTransforms();
  int before=SporeLaunches.Count,calls=SporeCalls;Plugin.SetField(flower,"_hitCtr",4);yield return SporeNativeProjectile(game,flower);
  var launches=SporeLaunches.Skip(before).ToArray();Require(launches.Length==7&&SporeCalls>calls&&(int)WindField(flower,"_hitCtr")==4+SporeCalls-calls-5,"a real projectile dispatch crosses the explicitly seeded fifth-hit threshold and produces seven source spray arcs");
  foreach(var target in targets)Require(launches.Take(6).Any(l=>Vector2.Distance(l.Target,target.transform.position)<.01f),"source spray tracks all six pooled candidates despite serialized amountSporeTrack5");
  Require(launches.Take(6).All(l=>l.Duration>=.3f&&l.Duration<=.7f)&&launches[6].Duration>=.5f&&launches[6].Duration<=.9f&&Vector2.Distance(launches[6].Target,flower.transform.position)>=.99f&&Vector2.Distance(launches[6].Target,flower.transform.position)<=5.01f,"source spray retains tracked-arc timing then its remaining random-radius/random-duration arc");
  yield return SporeQuiet();Require(targets.All(t=>t.HP<100000),"all six actual source-tracked arcs land and physically damage their pooled NPC targets");
  Log("spore-spray","arcs="+launches.Length+" tracked=6 unused-limit=5 projectile-dispatches="+(SporeCalls-calls)+" remainder="+WindField(flower,"_hitCtr"));
  foreach(var target in targets)yield return StatusReturn(target);
 }
 IEnumerator CheckAndroidSpores(bool continuation)
 {
  GameController game;if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("weapon:SporeGunData");Progression.Refresh();yield return AchievementRun("Shana","SporeGunData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();
  }
  var p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();p.SetPosition(new Vector3(100,100,0));SporeFlower flower;
  while((flower=Flower())==null||flower.GetComponent<AndroidSporeTargets>()==null)yield return null;flower.enabled=false;flower.transform.position=p.transform.position+Vector3.right*3;
  Require(p.gun.gunData.name=="SporeGunData"&&p.GetComponentsInChildren<SporeFlower>().Length==1&&flower.GetComponent<AndroidSummonPulse>()==null,"actual native Spore Gun attachment/source Continue creates one initialized flower without the source-omitted base pulse");
  Require((int)WindField(flower,"baseDamage")==40&&(int)WindField(flower,"sprayAmount")==7&&(int)WindField(flower,"sprayThreshold")==5&&(int)WindField(flower,"amountSporeTrack")==5&&(float)WindField(flower,"range")==5,"actual flower serialized values match source40-damage/seven-arc/five-hit/range-five fields");
  Require(!flower.multiplyByBulletDamage&&!flower.procsOnHit,"base source Spore Gun does not enable Bullet Damage scaling or on-hit procs");
  if(continuation)Require((int)WindField(flower,"_hitCtr")==0&&(float)WindField(flower,"_timer")<.25f&&SporeLaunches.Count==0,"source Continue omits the saved partial hit count/attack clock and active world arc");
  Log("fixture-precondition","Owned Spore Gun/no runes, native gun attachment/input, stopped source scheduler/clock, stationary source-pooled targets and relocated flower. Explicit temporary SummonDamage+.123/BulletDamage+.27 and conditional scaling flag exercise two-stage ceiling; all are restored before save. Spray counter4, saved counter3/clock2.3 and active-world arc are declared preconditions, not natural hit-count/full-run proof.");
  p.stats[StatType.SummonDamage].AddMultiplierBonus(.123f);p.stats[StatType.BulletDamage].AddMultiplierBonus(.27f);yield return SporeAutomatic(flower,false,45);yield return SporeAutomatic(flower,true,58);flower.multiplyByBulletDamage=false;p.stats[StatType.SummonDamage].AddMultiplierBonus(-.123f);p.stats[StatType.BulletDamage].AddMultiplierBonus(-.27f);
  yield return SporeSpray(game,flower);
  if(!continuation)
  {
   var target=StatusEnemy();target.transform.position=flower.transform.position+Vector3.up*2;int before=SporeLaunches.Count;Plugin.SetField(flower,"_timer",4.99f);flower.enabled=true;while(SporeLaunches.Count==before)yield return null;flower.enabled=false;Plugin.SetField(flower,"_hitCtr",3);Plugin.SetField(flower,"_timer",2.3f);
   Require(SporeActive("sporePrefab").Length>0,"explicit source checkpoint contains an actual unfinished native spore arc");yield return DragonSuspend(game,"Spore Flower native attachment/automatic source targeting/Powerup ceiling damage/real projectile-triggered seven-spore spray and active-arc partial-counter source checkpoint");yield break;
  }
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();Require(Flower()==null,"ordinary native restart removes the Spore Gun flower");int launchesBefore=SporeLaunches.Count;yield return CheckRevolver(game,p,"android-spore-original-restart.png");Require(SporeLaunches.Count==launchesBefore,"actual original gun input after restart produces no old spore arc");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Spore Flower native attachment/actual automatic and projectile producers/physical source explosions/spray/source Continue/ordinary restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(SporeFlower),"LaunchSpore")]
class SporeLaunchScope
{static void Prefix(){if(RuntimeChecks.SporeMode)RuntimeChecks.SporeLaunching=true;}static void Postfix(){RuntimeChecks.SporeLaunching=false;}}
[HarmonyPatch(typeof(AreaProjectile),nameof(AreaProjectile.TargetPos))]
class SporeArcProbe
{static void Prefix(AreaProjectile __instance,Vector2 __0,float __1){if(RuntimeChecks.SporeLaunching)RuntimeChecks.SporeLaunches.Add(new RuntimeChecks.SporeLaunch{Actor=__instance,Target=__0,Duration=__1});}}
[HarmonyPatch(typeof(AndroidHealthEvents),"Change")]
class SporeDamageProbe
{static void Prefix(AndroidHealthEvents __instance,int requested,DamageType kind){if(RuntimeChecks.SporeMode&&__instance.gameObject.GetInstanceID()==RuntimeChecks.SporeObserved){RuntimeChecks.SporeHarmKinds.Add((int)kind);RuntimeChecks.SporeHarmValues.Add(requested);}}}
[HarmonyPatch]
class SporeTriggerProbe
{static System.Reflection.MethodBase TargetMethod()=>AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidSpores"),"OnProjectile");static void Prefix(){if(RuntimeChecks.SporeMode)RuntimeChecks.SporeCalls++;}}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class SporePrimaryProbe
{static void Postfix(bool __4,Projectile __result){if(!RuntimeChecks.SporeMode||!RuntimeChecks.SporeCapturePrimary||__4)return;if(RuntimeChecks.SporePrimary==null)RuntimeChecks.SporePrimary=__result;RuntimeChecks.EmitterPark(__result);}}
