using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool EliteMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("elites-");
 internal static readonly Dictionary<AIComponent,List<float>> EliteShots=new Dictionary<AIComponent,List<float>>();
 int EliteClaims=>(int)WindField(Object.FindObjectOfType<EliteRun>(),"Claimed");
 int NativeKills=>ScoreCalculator.SharedInstance.GetScore().enemiesKilled;
 Health EliteSpawn(string tag="BrainMonster",int hp=24,bool scheduled=false,int index=-1)
 {
  var before=new HashSet<Health>(Object.FindObjectsOfType<Health>());
  var run=Object.FindObjectOfType<AndroidEndlessRun>();ElementCall(run,"SpawnActor",tag,hp,scheduled,index);
  return Object.FindObjectsOfType<Health>().Single(h=>!before.Contains(h)&&h.name==tag);
 }
 static bool Enhanced(Health h)=>h.GetComponent<EnhancedEnemy>()!=null&&(bool)WindField(h.GetComponent<EnhancedEnemy>(),"Enhanced");
 GameObject[] LiveXP()=>ObjectPooler.SharedInstance.pooledObjectsDictionary["SmallXP"].Where(o=>o.activeSelf).ToArray();
 void ClearEliteXP(){foreach(var xp in LiveXP())ReturnFixtureActor(xp);}
 IEnumerator EliteCollect(GameController game,GameObject[] drops,float expected)
 {
  float before=(float)WindField(game.playerXP,"xp");foreach(var drop in drops)ElementCall(drop.GetComponent<AndroidPickup>(),"TargetPick",PlayerController.Instance.gameObject);
  while(drops.Any(o=>o.activeSelf))yield return null;
  CombatClose((float)WindField(game.playerXP,"xp")-before,expected,"actual dropped XP travel and UsePickup award proportional experience");
 }
 IEnumerator EliteCadence(GameController game)
 {
  Plugin.SetField(game.playerXP,"<level>k__BackingField",10);ClearEliteXP();
  var reference=EliteSpawn("EyeMonster");Vector3 ordinaryScale=reference.transform.localScale,ordinaryArtScale=reference.GetComponent<SpriteRenderer>().transform.lossyScale;
  Physics2D.SyncTransforms();Vector3 ordinaryCollider=reference.GetComponent<Collider2D>().bounds.size;int referenceId=reference.GetInstanceID();ReturnFixtureActor(reference.gameObject);
  for(int i=0;i<200;i++)
  {
   var ordinary=EliteSpawn();Require(!Enhanced(ordinary),"no promotion before the earned 200-kill boundary at "+i);ordinary.TakeDamage(DamageType.Bullet,100000);
   Require(NativeKills==i+1,"native death advances actual kill cadence once");
   if(i==0){Require(LiveXP().Length==1,"ordinary death retains one native XP drop");yield return EliteCollect(game,LiveXP(),1);}
   yield return null;
  }
  ClearEliteXP();var elite=EliteSpawn("EyeMonster");int baseHP=Mathf.FloorToInt(Object.FindObjectOfType<HordeSpawner>().healthMultiplier*24);
  Require(Enhanced(elite)&&elite.maxHP==baseHP*3&&elite.HP==baseHP*3&&EliteClaims==1,"first ordinary spawn after200 real kills gains triple actual health and consumes exactly one opportunity");
  Vector3 largerScale=new Vector3(ordinaryScale.x*1.25f,ordinaryScale.y*1.25f,ordinaryScale.z);
  Require(elite.GetInstanceID()==referenceId&&elite.transform.localScale==largerScale,"elite uses 125 percent of its actual spawned XY scale and preserves Z");
  Physics2D.SyncTransforms();var collider=elite.GetComponent<Collider2D>().bounds.size;
  CombatClose(collider.x,ordinaryCollider.x*1.25f,"elite collider width grows proportionally with the artwork");CombatClose(collider.y,ordinaryCollider.y*1.25f,"elite collider height grows proportionally with the artwork");
  var artScale=elite.GetComponent<SpriteRenderer>().transform.lossyScale;
  CombatClose(artScale.x,ordinaryArtScale.x*1.25f,"elite rendered sprite width scale is 125 percent");CombatClose(artScale.y,ordinaryArtScale.y*1.25f,"elite rendered sprite height scale is 125 percent");
  var normal=EliteSpawn("EyeMonster");Require(!Enhanced(normal)&&normal.maxHP==baseHP&&normal.transform.localScale==ordinaryScale,"one opportunity cannot promote or resize a second regular spawn");
  normal.transform.position=PlayerController.Instance.transform.position+new Vector3(2,1.6f);normal.GetComponent<MoveComponent2D>().enabled=false;normal.GetComponent<AIComponent>().enabled=false;
  var ai=elite.GetComponent<AIComponent>();elite.transform.position=PlayerController.Instance.transform.position+Vector3.right*2;elite.GetComponent<MoveComponent2D>().enabled=false;ai.specialTimer=0;EliteShots[ai]=new List<float>();
  while(EliteShots[ai].Count<3)yield return null;
  float enhancedInterval=(EliteShots[ai][2]-EliteShots[ai][0])/2;Require(Mathf.Abs(enhancedInterval-ai.specialCooldown*2/3)<.12f,"actual enhanced Eye fires at two-thirds ordinary cooldown");
  foreach(bool style in new[]{false,true}){if(ColorfulStyle.Instance.Enabled!=style)ColorfulStyle.Instance.Toggle();yield return PoiCapture("elite-aura-"+(style?"reborn":"original")+".png");}
  var aura=elite.transform.Find("EliteAura").GetComponent<SpriteRenderer>();Require(aura.enabled&&aura.color.r>.9f&&aura.color.g<.3f,"elite has a visible red native-art aura in both artwork modes");
  normal.GetComponent<AIComponent>().enabled=true;ReturnFixtureActor(normal.gameObject);
  var move=elite.GetComponent<MoveComponent2D>();move.enabled=true;ai.specialTimer=0;
  elite.transform.position=PlayerController.Instance.transform.position+Vector3.right*(Mathf.Sqrt(ai.specialRangeSqr)+2);Vector3 beforeMove=elite.transform.position;
  while(elite.transform.position==beforeMove)yield return null;
  Require(elite.transform.localScale==largerScale,"actual native Eye movement and repeated attacks preserve the elite size");move.enabled=false;
  ClearEliteXP();var recorder=Object.FindObjectOfType<GameController>().GetComponent(AccessTools.TypeByName("TwentyMinutes.Modernization.RunRecorder"));int minis=((HashSet<int>)WindField(recorder,"KilledElites")).Count;int bosses=((HashSet<int>)WindField(recorder,"KilledBosses")).Count;
  elite.TakeDamage(DamageType.Bullet,100000);Require(LiveXP().Length==3&&NativeKills==201,"enhanced death drops triple XP but counts as one kill");
  Require(elite.transform.localScale==ordinaryScale,"death returns the exact original scale before pool reuse");
  Require(((HashSet<int>)WindField(recorder,"KilledElites")).Count==minis&&((HashSet<int>)WindField(recorder,"KilledBosses")).Count==bosses,"enhanced ordinary death does not count as a scheduled miniboss or boss");yield return EliteCollect(game,LiveXP(),3);
  var reused=EliteSpawn("EyeMonster");Require(reused==elite&&!Enhanced(reused)&&reused.maxHP==baseHP&&!reused.transform.Find("EliteAura").GetComponent<SpriteRenderer>().enabled&&reused.transform.localScale==ordinaryScale,"pool reuse restores ordinary size/health and hides the old aura");
  Physics2D.SyncTransforms();CombatClose(reused.GetComponent<Collider2D>().bounds.size.x,ordinaryCollider.x,"ordinary reuse restores the original collider width");
  ai=reused.GetComponent<AIComponent>();reused.transform.position=PlayerController.Instance.transform.position+Vector3.right*2;reused.GetComponent<MoveComponent2D>().enabled=false;ai.specialTimer=0;EliteShots[ai]=new List<float>();
  while(EliteShots[ai].Count<3)yield return null;
  float ordinaryInterval=(EliteShots[ai][2]-EliteShots[ai][0])/2;Require(Mathf.Abs(ordinaryInterval-ai.specialCooldown)<.12f&&Mathf.Abs(ordinaryInterval/enhancedInterval-1.5f)<.12f,"ordinary reused Eye restores its actual firing cadence without a retained elite modifier");ElementClear();
  Plugin.SetField(ScoreCalculator.SharedInstance,"_enemiesKilled",400);var source=Object.FindObjectOfType<AndroidEndlessRun>();
  var scheduled=EliteSpawn("ElderBrain",1000,true);Require(!Enhanced(scheduled)&&EliteClaims==1,"scheduled miniboss does not consume a pending regular-enemy opportunity");ReturnFixtureActor(scheduled.gameObject);
  int index=SelectedMap.MapData.bossSpawns.Count>0?0:-1;Require(index>=0,"scenario has its real scheduled boss for identity check");ElementCall(source,"SpawnScheduledBoss",index,false);var boss=Object.FindObjectsOfType<Health>().First(h=>h.GetComponent<AndroidHealthEvents>()!=null&&h.GetComponent<AndroidHealthEvents>().CreatureType==2);Require(!Enhanced(boss)&&EliteClaims==1,"real boss spawn retains identity and does not consume a regular opportunity");ReturnFixtureActor(boss.gameObject);
  // The pool also permits signed/nonuniform prefab scales. Exercise the same
  // actual promotion path a second time, then restore this controlled fixture.
  var pose=new Vector3(-.8f,1.4f,2);elite.transform.localScale=pose;var repromoted=EliteSpawn("EyeMonster");
  Require(repromoted==elite&&Enhanced(repromoted)&&repromoted.transform.localScale==new Vector3(-1,1.75f,2),"second promotion scales its current signed nonuniform pose once without compounding");
  ReturnFixtureActor(repromoted.gameObject);Require(repromoted.transform.localScale==pose,"explicit pool return restores signed nonuniform XY and untouched Z");repromoted.transform.localScale=ordinaryScale;
  Plugin.SetField(ScoreCalculator.SharedInstance,"_enemiesKilled",600);
  Log("fixture-precondition","Real first200 deaths/native kill count; later native score seeded400 for a second promotion with a signed/nonuniform pose, then600 to save a pending third opportunity. Source scheduler stopped; NPC shot positions/movement isolated; actual AI Use cadence/movement and native XP grants measured.");
  yield return DragonSuspend(game,"Enhanced regular enemies: real200-kill cadence, exact health/XP, native Eye intervals, aura, boss separation, pool reuse and pending opportunity checkpoint");
 }
 IEnumerator CheckElites(string mode)
 {
  if(mode=="elites-maps"){yield return EliteMaps();yield break;}
  if(mode=="elites-continue"||mode=="elites-prior")yield return CombatBuffContinue();
  else{RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());}
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  if(mode=="elites-fresh"){yield return EliteCadence(game);yield break;}
  if(mode=="elites-prior")
  {
   Require(NativeKills==450&&EliteClaims==2&&!Enhanced(EliteSpawn()),"older checkpoint initializes at its next200-kill boundary without retrospective elite burst");ElementClear();Plugin.SetField(ScoreCalculator.SharedInstance,"_enemiesKilled",600);Require(Enhanced(EliteSpawn())&&EliteClaims==3,"legacy cadence keeps its next eligible boundary");ElementClear();yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Derived released checkpoint with450 kills and absent elite member retains forward cadence");Application.Quit(0);yield break;
  }
  Require(NativeKills==600&&EliteClaims==2,"fresh-process Continue preserves pending third opportunity exactly");
  var next=EliteSpawn();Require(Enhanced(next)&&EliteClaims==3,"resumed pending opportunity promotes the next ordinary actor once");var normal=EliteSpawn();Require(!Enhanced(normal),"restored opportunity cannot be claimed twice");ElementClear();
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();Require(NativeKills==0&&EliteClaims==0&&!Enhanced(EliteSpawn()),"fresh run resets kills/cadence and spawns ordinary enemies");ElementClear();yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Enhanced enemies exact pending Continue, one-time claim and fresh-run reset");Application.Quit(0);
 }
 IEnumerator EliteMaps()
 {
  foreach(string map in new[]{"20M_Forest","20M_Temple","20M_PumpkinPatch","10M_Forest","Endless_Forest","Challenge_Forest"})
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:"+map);Progression.Refresh();yield return AchievementRun("Shana","RevolverData",map,prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();var source=Object.FindObjectOfType<AndroidEndlessRun>();WorldClearSchedule(source);var session=WorldSession(source,0);string tag=((string[])WindField(WorldWaves(source)[0],"Pools"))[0];
   var baseline=EliteSpawn(tag,session.HP);int normalMaximum=baseline.maxHP;Vector3 normalScale=baseline.transform.localScale;ReturnFixtureActor(baseline.gameObject);Plugin.SetField(ScoreCalculator.SharedInstance,"_enemiesKilled",200);
   WorldQueue(source).Enqueue(0);source.enabled=true;EnhancedEnemy enhanced;
   while((enhanced=Object.FindObjectsOfType<EnhancedEnemy>().FirstOrDefault(e=>(bool)WindField(e,"Enhanced")))==null)yield return null;source.enabled=false;
   Require(enhanced.GetComponent<Health>().maxHP==normalMaximum*3&&EliteClaims==1,"real source Update queue promotes an ordinary wave with final health scaling in "+map);
   Require(enhanced.transform.localScale==new Vector3(normalScale.x*1.25f,normalScale.y*1.25f,normalScale.z),"source wave keeps the 125 percent elite size in "+map);enhanced.transform.position=PlayerController.Instance.transform.position+Vector3.right*2;yield return PoiCapture("elite-map-"+map+".png");
   enhanced.GetComponent<Health>().TakeDamage(DamageType.Bullet,1000000);if(ChallengeRun.Instance!=null)Require(ChallengeRun.Instance.EnemyKills==1&&ChallengeRun.Instance.BossKills==0,"Challenge counts promoted ordinary enemy against enemy quota, not boss quota");
   ElementClear();yield return AchievementExit(game,false);
  }
  RunResume.Invalidate();yield return OpenCustomConfiguration();Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>();CustomMode.Set("spawn:healthMultiplier",2);yield return MenuPointer(CustomButton("CustomStart"));GameController custom;
  while((custom=Object.FindObjectOfType<GameController>())==null||!(custom.CurrentState is CombatState))yield return null;StopFixtureSpawning();string ownership=Ownership();
  var ordinary=EliteSpawn();int hp=ordinary.maxHP;ReturnFixtureActor(ordinary.gameObject);Plugin.SetField(ScoreCalculator.SharedInstance,"_enemiesKilled",200);var promoted=EliteSpawn();Require(CustomMode.Active&&Enhanced(promoted)&&promoted.maxHP==hp*3,"Custom health factor applies once before elite health multiplier");promoted.TakeDamage(DamageType.Bullet,1000000);Require(Ownership()==ownership,"Custom elite death preserves its no-unlocks/no-achievements rule");ElementClear();yield return AchievementExit(custom,false);SetPhase("PASS");Log("complete","All six source mode schedules and actual Update queue, Challenge accounting, Custom configured health and progression isolation");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(flanne.AISpecials.AIShootSpecial),nameof(flanne.AISpecials.AIShootSpecial.Use))]
class EliteEyeProbe
{static void Prefix(AIComponent ai){if(RuntimeChecks.EliteMode&&RuntimeChecks.EliteShots.TryGetValue(ai,out var times))times.Add(Time.time);}}
[HarmonyPatch(typeof(CombatState),nameof(CombatState.Enter))]
class EliteCheckpointFixture
{static void Postfix(){if(RuntimeChecks.EliteMode)Object.FindObjectOfType<AndroidEndlessRun>().enabled=false;}}
