using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
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
 internal static bool WorldMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-world-");
 internal sealed class WorldSpawn{internal string Map,Tag;internal int Index,Frame,HP,Damage;internal bool Elite;internal Health Actor;}
 internal static readonly List<WorldSpawn> WorldSpawns=new List<WorldSpawn>();
 internal static readonly List<int> WorldBatches=new List<int>();
 internal static WorldSpawn WorldCurrentSpawn;
 internal static bool WorldCaptureBoss,WorldCaptureBullet;
 internal static Health WorldBoss;
 internal static Projectile WorldBullet;
 internal static int WorldUpdates;
 internal static AndroidEndlessState WorldRestoredSchedule;
 int worldWarnings,worldLimits;
 void WorldWarning(object sender,object args)=>worldWarnings++;
 void WorldLimit(object sender,object args)=>worldLimits++;
 internal static AndroidEndlessState WorldState(AndroidEndlessRun run)=>(AndroidEndlessState)AccessTools.Method(typeof(AndroidEndlessRun),"Capture").Invoke(run,null);
 static int WorldMilliseconds=>(int)AccessTools.Field(typeof(AndroidGameClock),"Milliseconds").GetValue(null);
 static IList WorldWaves(AndroidEndlessRun run)=>(IList)WindField(run,"waves");
 static SpawnSession WorldSession(AndroidEndlessRun run,int index)=>(SpawnSession)WindField(WorldWaves(run)[index],"Session");
 static Queue<int> WorldQueue(AndroidEndlessRun run)=>(Queue<int>)WindField(run,"queue");
 static void WorldTime(float value)=>Plugin.SetField(GameTimer.SharedInstance,"<timer>k__BackingField",value);
 static List<AIComponent> WorldAIActors()=>(List<AIComponent>)AccessTools.Field(typeof(AIController),"aiComponents").GetValue(null);
 static void WorldReturnEnemies()
 {foreach(var h in ObjectPooler.SharedInstance.pooledObjectsDictionary.Values.SelectMany(v=>v).Where(o=>o.activeSelf&&o.layer==LayerMask.NameToLayer("Enemy")&&o.GetComponent<Health>()!=null).Distinct().ToArray())ReturnFixtureActor(h);}
 static void WorldClearSchedule(AndroidEndlessRun run)
 {run.enabled=false;foreach(string field in new[]{"active","pending","pendingBosses"})((List<int>)WindField(run,field)).Clear();WorldQueue(run).Clear();if(SelectedMap.MapData.endless)Plugin.SetField(run,"nextBoss",float.MaxValue);}
 IEnumerator WorldOneUpdate(AndroidEndlessRun run)
 {int before=WorldUpdates;run.enabled=true;while(WorldUpdates==before)yield return null;run.enabled=false;}
 IEnumerator WorldQueueSpawns(AndroidEndlessRun run,int amount)
 {
  int before=WorldSpawns.Count,batches=WorldBatches.Count;for(int i=0;i<amount;i++)WorldQueue(run).Enqueue(0);run.enabled=true;
  while(WorldQueue(run).Count>0)yield return null;run.enabled=false;
  Require(WorldSpawns.Count-before==amount&&WorldBatches.Skip(batches).Where(n=>n>0).SequenceEqual(Enumerable.Range(0,(amount+4)/5).Select(i=>Math.Min(5,amount-i*5))),"actual source Update drains the queued cohort in five-spawn batches, including its final remainder");
 }
 IEnumerator WorldClock()
 {
  var timer=GameTimer.SharedInstance;float before=timer.timer;int ms=WorldMilliseconds;timer.Start();while(timer.timer==before||WorldMilliseconds==ms)yield return null;
  Require(timer.timer>before&&WorldMilliseconds>ms,"real fixed-step owner advances both source run and independent millisecond clocks");
  AccessTools.Method(typeof(AndroidGameClock),"Stop").Invoke(null,new object[]{timer,true});float stopped=timer.timer;ms=WorldMilliseconds;while(WorldMilliseconds==ms)yield return null;
  Require(timer.timer==stopped,"source Stop(true) retains the independent clock while run time stops");timer.Stop();ms=WorldMilliseconds;yield return new WaitForFixedUpdate();Require(WorldMilliseconds==ms&&timer.timer==stopped,"ordinary native Stop freezes both source clocks");
 }
 IEnumerator WorldOpening(GameController game,AndroidEndlessRun run)
 {
  var p=PlayerController.Instance;var map=SelectedMap.MapData;var horde=Object.FindObjectOfType<HordeSpawner>();
  Require(Object.FindObjectsOfType<AndroidEndlessRun>().Length==1&&!horde.enabled,"native mode entry installs one source scheduler with the superseded horde update disabled");
  Require(GameTimer.SharedInstance.timer>=0&&GameTimer.SharedInstance.timeLimit==(map.endless?float.MaxValue:map.timeLimit),"source mode initialization uses nonnegative elapsed time and the actual mode limit");
  while(!WorldSpawns.Any(s=>s.Map==map.name&&!s.Elite))yield return null;run.enabled=false;
  var first=WorldSpawns.First(s=>s.Map==map.name&&!s.Elite);var session=WorldSession(run,first.Index);
  Require(first.HP==Mathf.FloorToInt(session.HP*horde.healthMultiplier)&&first.Damage==horde.enemyDamage,"actual opening wave imports source HP and native difficulty damage at the shared spawn entry");
  if(map.name=="10M_Forest")Require(!first.Elite&&session.startTime==0&&session.numPerSpawn==4,"actual Quickplay opening Brain wave is ordinary rather than elite");
  var ai=first.Actor.GetComponent<AIComponent>();var move=first.Actor.GetComponent<MoveComponent2D>();while(!(bool)WindField(first.Actor.GetComponent<AndroidAIActor>(),"Initialized"))yield return null;
  Require(WorldAIActors().Count(x=>x==ai)==1&&MoveSystem2D.moveComponents.Count(x=>x==move)==1,"actual spawned enemy registers once with source AI and movement owners");
  float distance=Vector2.Distance(first.Actor.transform.position,p.transform.position);while(Vector2.Distance(first.Actor.transform.position,p.transform.position)>=distance)yield return null;
  Require(move.vector.sqrMagnitude>0,"actual source AI and movement advance an opening enemy toward the live player");
  ReturnFixtureActor(first.Actor.gameObject);Require(!WorldAIActors().Contains(ai)&&!MoveSystem2D.moveComponents.Contains(move)&&move.vector==Vector2.zero,"real pool return unregisters source AI/movement and clears velocity");WorldReturnEnemies();
  yield return WorldClock();WorldClearSchedule(run);WorldTime(0);WorldBatches.Clear();yield return WorldQueueSpawns(run,12);WorldReturnEnemies();
  // Explicit schedule operands isolate the allocated-count rule; these do not
  // pretend to be a naturally populated maximum-cap encounter.
  var wave=WorldSession(run,0);var tags=(string[])WindField(WorldWaves(run)[0],"Pools");Require(tags.Length==1||map.endless,"ordinary source opening uses its original pool identity");
  int maximum=Mathf.FloorToInt(wave.maximum*(ChallengeMode.Active?ChallengeMode.AliveMultiplier(Loadout.difficultyLevel):1));var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();var allocated=new List<GameObject>();
  for(int i=0;i<maximum;i++){var actor=ObjectPooler.SharedInstance.GetPooledObject(tags[0]);actor.SetActive(false);allocated.Add(actor);}
  int counted=(int)AccessTools.Method(typeof(AndroidPools),"CountAllocated").Invoke(pool,new object[]{tags[0]});Require(counted==maximum,"source population count includes inactive allocations that were not returned to the pool");
  ((List<int>)WindField(run,"active")).Add(0);wave.timer=-.01f;int spawns=WorldSpawns.Count;yield return WorldOneUpdate(run);Require(WorldSpawns.Count==spawns&&WorldQueue(run).Count==0,"actual source scheduler suppresses a new batch at allocated maximum despite inactive actor roots");
  foreach(var actor in allocated)ReturnFixtureActor(actor);WorldClearSchedule(run);
  Log("world-opening","mode="+map.name+" pool="+first.Tag+" HP="+first.HP+" damage="+first.Damage+" allocated-cap="+maximum+" source-run-clock="+GameTimer.SharedInstance.timer);
 }
 IEnumerator WorldEliteAndRound(AndroidEndlessRun run)
 {
  int index=Enumerable.Range(0,WorldWaves(run).Count).First(i=>WorldSession(run,i).isElite);var wave=WorldSession(run,index);WorldClearSchedule(run);((List<int>)WindField(run,"active")).Add(index);WorldTime(wave.startTime+wave.duration);int before=WorldSpawns.Count;yield return WorldOneUpdate(run);
  Require(WorldSpawns.Count==before+1&&WorldSpawns.Last().Elite&&WorldSpawns.Last().Index==index&&WorldState(run).spawnedElites.Contains(index),"actual source expiry boundary spawns a not-yet-spawned elite before retiring its wave");WorldReturnEnemies();WorldClearSchedule(run);
  float duration=(float)WindField(run,"roundDuration");Plugin.SetField(run,"roundStart",0f);WorldTime(duration);yield return WorldOneUpdate(run);Require(WorldState(run).roundStart==0,"source round does not restart at exact duration equality");
  WorldTime(duration+.02f);yield return WorldOneUpdate(run);Require(WorldState(run).roundStart==duration&&WorldState(run).pending.Count==WorldWaves(run).Count,"actual source round restarts strictly after duration and requeues its original waves");WorldClearSchedule(run);
 }
 IEnumerator WorldNormalVictory(GameController game)
 {
  var timer=GameTimer.SharedInstance;worldWarnings=worldLimits=0;this.AddObserver(WorldWarning,GameTimer.OneSecondLeftNotification);this.AddObserver(WorldLimit,GameTimer.TimeLimitNotification);
  Plugin.SetField(timer,"oneSecondWarningSent",false);WorldTime(timer.timeLimit-1.01f);timer.Start();while(!(game.CurrentState is PlayerSurvivedState))yield return null;
  Require(worldWarnings==1&&worldLimits==1,"real fixed-step warning/limit events reach native victory once");this.RemoveObserver(WorldWarning,GameTimer.OneSecondLeftNotification);this.RemoveObserver(WorldLimit,GameTimer.TimeLimitNotification);yield return AchievementExit(game,true);
 }
 IEnumerator WorldChallengeVictory(GameController game,AndroidEndlessRun run)
 {
  var challenge=ChallengeRun.Instance;Require(challenge!=null&&challenge.Tier==2,"actual Challenge entry retains requested owned tier2");WorldTime(0);WorldClearSchedule(run);yield return WorldQueueSpawns(run,1);var victim=WorldSpawns.Last().Actor;
  challenge.EnemyKills=ChallengeMode.EnemyTargets[2]-1;challenge.BossKills=ChallengeMode.BossTargets[2];int normal=SaveSystem.data.difficultyUnlocked;victim.TakeDamage(DamageType.Bullet,victim.maxHP);
  while(!(game.CurrentState is PlayerSurvivedState))yield return null;
  // Source Enter starts Present; progression follows its actual results
  // presentation. State entry alone is not that completion boundary.
  while(!game.quitToTitleButton.gameObject.activeInHierarchy||!game.quitToTitleButton.IsInteractable())yield return null;
  Require(challenge.Completed&&Progression.State.challengeDifficultyUnlocked==3&&SaveSystem.data.difficultyUnlocked==normal,"real qualifying source death completes Challenge, advances only its tier and reaches native victory");
  Require(SaveSystem.data.characterHighestClear[0]==2&&SaveSystem.data.gunHighestClear[0]==2,"actual Challenge victory rejoins common highest character/gun clear writes");yield return AchievementExit(game,true);
 }
 IEnumerator WorldAffixDamage(GameController game,Health boss)
 {
  var p=PlayerController.Instance;while(boss.isInvincible.value)yield return null;boss.GetComponent<AIComponent>().frozen=true;boss.GetComponent<MoveComponent2D>().vector=Vector2.zero;boss.transform.position=p.transform.position+Vector3.right*8;
  var affix=boss.GetComponent<AndroidAffixes>();var values=(List<int>)WindField(affix,"values");Require(values.Count==4&&values.Distinct().Count()==4&&values.OrderBy(i=>i).SequenceEqual(new[]{0,1,2,3}),"late source boss owns all four distinct rolled affixes and original icon entries");
  BurnSystem.SharedInstance.Burn(boss.gameObject,3);FreezeSystem.SharedInstance.Freeze(boss.gameObject);int hp=boss.HP;ThunderGenerator.SharedInstance.GenerateAt(boss.gameObject,22);
  Require(!BurnSystem.SharedInstance.IsBurning(boss.gameObject)&&!FreezeSystem.SharedInstance.IsFrozen(boss.gameObject)&&RecoveryBolts().Length==0&&boss.HP==hp,"actual source fire/freeze/targeted-lightning applications reject the corresponding affixed boss");
  p.stats[StatType.Piercing].AddFlatBonus(2);WorldBullet=null;WorldCaptureBullet=true;game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*12);
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(WorldBullet==null)yield return null;InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});WorldCaptureBullet=false;p.stats[StatType.Piercing].AddFlatBonus(-2);int raw=(int)WorldBullet.damage;
  var move=WorldBullet.GetComponent<MoveComponent2D>();WorldBullet.transform.position=boss.transform.position;move.vector=Vector2.zero;move.enabled=true;Physics2D.SyncTransforms();while(boss.HP==hp&&WorldBullet.gameObject.activeSelf)yield return null;
  Require(hp-boss.HP==raw/2&&(int)WorldBullet.damage==raw/2&&WorldBullet.gameObject.activeSelf,"real piercing bullet contact mutates its stored damage by source affix division before harming the boss");
  var plain=StatusEnemy();plain.transform.position=p.transform.position+Vector3.right*12;WorldBullet.transform.position=plain.transform.position;move.vector=Vector2.zero;Physics2D.SyncTransforms();while(plain.HP==100000&&WorldBullet.gameObject.activeSelf)yield return null;
  Require(100000-plain.HP==raw/2,"same actual pierced bullet carries its already-halved damage to an unaffixed target");if(WorldBullet.gameObject.activeSelf)ReturnFixtureActor(WorldBullet.gameObject);yield return StatusReturn(plain);while(!(p.CurrentState is flanne.Player.IdleState))yield return null;
  Log("world-affix","boss="+boss.name+" raw="+raw+" source-first="+(raw/2)+" carried-to-plain="+(raw/2)+" affixes="+string.Join(",",values));
 }
 IEnumerator CheckAndroidWorld(bool continuation)
 {
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").EndsWith("-codec"))
  {
   var type=AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidAffixArt+Manifest");string path=Path.Combine(Plugin.AssetRoot,"affixes.json");var native=JsonUtility.FromJson(File.ReadAllText(path),type);object decoded;
   using(var stream=File.OpenRead(path))decoded=new DataContractJsonSerializer(type).ReadObject(stream);
   var actors=(Array)WindField(decoded,"actors");var icons=(Array)WindField(decoded,"icons");var pose=((Array)WindField(actors.GetValue(0),"poses")).GetValue(0);
   Log("world-affix-codec","unity-actors="+(((Array)WindField(native,"actors"))?.Length.ToString()??"null")+" reflection-actors="+actors.Length+" icons="+icons.Length+" first-scale="+WindField(pose,"scale")+" first-rotation="+WindField(pose,"rotation"));
   Require(WindField(native,"actors")==null&&actors.Length==32&&icons.Length==4&&(Vector3)WindField(pose,"scale")==Vector3.one*.5f&&((Quaternion)WindField(pose,"rotation")).w==1,"actual native codec reproduction isolates lost mod DTO arrays while established wire reader preserves arrays and Unity pose structs");
   SetPhase("PASS");Log("complete","Read-only affix codec diagnostic only, not gameplay acceptance");Application.Quit(0);yield break;
  }
  Log("fixture-precondition","Native owned-mode entries/no runes and native helper invulnerability in isolated profile. Source scheduler stays live for observed updates. Queue/population/expiry/round/boss-level and quota operands are explicitly seeded; no natural20-minute or10-boss progression claim. Source AI/movement and clocks run normally at observed transitions. Temporary piercing is removed before save; no production behavior change.");
  if(continuation)
  {
   yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();var run=Object.FindObjectOfType<AndroidEndlessRun>();run.enabled=false;GameTimer.SharedInstance.Stop();
   Require(WorldRestoredSchedule!=null&&WorldRestoredSchedule.queue.SequenceEqual(new[]{0,0,0})&&WorldRestoredSchedule.waveTimers[0]==.73f&&WorldRestoredSchedule.bossLevel==11,"exact source Continue restores saved schedule/queue/timer before its first live Update");
   var state=WorldState(run);Require(state.bosses.Count==1&&state.bosses[0].level==10&&state.bosses[0].affixes.Count==4&&WorldBoss!=null,"source encounter restoration recreates the saved level10/four-affix boss");yield return WorldAffixDamage(game,WorldBoss);WorldReturnEnemies();yield return AchievementExit(game,false);
   yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();Require(WorldState(Object.FindObjectOfType<AndroidEndlessRun>()).bosses.Count==0,"ordinary native restart has no retained Endless boss record");yield return CheckRevolver(game,PlayerController.Instance,"android-world-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Connected source world exact schedule/affix encounter Continue, real affix contacts and ordinary native restart");Application.Quit(0);yield break;
  }
  RunResume.Invalidate();foreach(string id in new[]{"scenario:10M_Forest","scenario:20M_Temple","scenario:20M_PumpkinPatch","scenario:Challenge_Forest","scenario:Endless_Forest"})Progression.State.earned.Add(id);Progression.State.challengeDifficultyUnlocked=2;Progression.Refresh();
  int ms=WorldMilliseconds;
  foreach(string map in new[]{"10M_Forest","20M_Temple","20M_PumpkinPatch","20M_Forest","Challenge_Forest","Endless_Forest"})
  {
   if(map=="Challenge_Forest"){SaveSystem.data.difficultyUnlocked=0;SaveSystem.data.characterHighestClear[0]=-1;SaveSystem.data.gunHighestClear[0]=-1;}
   yield return AchievementRun("Shana","RevolverData",map,map=="20M_Temple"?15:map=="Challenge_Forest"?2:0,()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   var game=Object.FindObjectOfType<GameController>();var run=Object.FindObjectOfType<AndroidEndlessRun>();Require(WorldMilliseconds>=ms,"source independent millisecond clock survives native scene changes");var horde=Object.FindObjectOfType<HordeSpawner>();int originalDamage=horde.enemyDamage;yield return WorldOpening(game,run);yield return WorldEliteAndRound(run);
   if(map=="20M_Temple")
   {
    var late=horde.GetComponent<AndroidLateEnemyDamage>();Require(late!=null,"D15 scene owns the approved D10 late-damage clock rule");
    while(!(bool)WindField(late,"applied"))yield return null;
   }
   Require(horde.enemyDamage==originalDamage+(map=="20M_Temple"?1:0),"elapsed schedule boundary applies the approved D10 +1 exactly once and leaves lower Darkness unchanged");
   if(map=="20M_Temple")CombatClose(Object.FindObjectOfType<BossSpawner>().cooldownRate,.55f,"actual D15 initializer assigns source boss cooldown0.55");
   if(map=="Endless_Forest")
   {
    WorldTime(0);Plugin.SetField(run,"roundStart",0f);Plugin.SetField(run,"nextBoss",0f);Plugin.SetField(run,"bossLevel",10);WorldBoss=null;WorldCaptureBoss=true;yield return WorldOneUpdate(run);WorldCaptureBoss=false;
    Require(WorldBoss!=null&&WorldState(run).bossLevel==11,"actual source scheduler emits the explicitly seeded late boss and advances its encounter level");yield return WorldAffixDamage(game,WorldBoss);
    var bossData=WorldState(run).bosses.Single();float t=10/30f;int expected=Mathf.FloorToInt((10000+(1-Mathf.Sqrt(1-t*t))*10000000)*Object.FindObjectOfType<BossSpawner>().healthMultiplier);Require(WorldBoss.maxHP==expected&&bossData.level==10,"actual late boss uses original capped circular HP curve and saved source encounter level");
    WorldTime(12.34f);Plugin.SetField(run,"roundStart",0f);WorldSession(run,0).timer=.73f;WorldQueue(run).Enqueue(0);WorldQueue(run).Enqueue(0);WorldQueue(run).Enqueue(0);
    yield return DragonSuspend(game,"All six native source mode entries/opening/queue/cap/expiry/round/clock victory, actual Challenge quota/highest-clear and seeded late Endless boss/four-affix piercing contacts; queued source encounter checkpoint");yield break;
   }
   if(map=="Challenge_Forest")yield return WorldChallengeVictory(game,run);else yield return WorldNormalVictory(game);ms=WorldMilliseconds;
  }
 }
}
[HarmonyPatch(typeof(AndroidEndlessRun),"Update")]
class WorldUpdateProbe
{static void Prefix(out int __state)=>__state=RuntimeChecks.WorldSpawns.Count;static void Postfix(int __state){if(!RuntimeChecks.WorldMode)return;RuntimeChecks.WorldUpdates++;RuntimeChecks.WorldBatches.Add(RuntimeChecks.WorldSpawns.Count-__state);}}
[HarmonyPatch(typeof(AndroidEndlessRun),"Initialize")]
class WorldRestoreScheduleProbe
{static void Postfix(AndroidEndlessRun __instance,object saved){if(RuntimeChecks.WorldMode&&saved!=null)RuntimeChecks.WorldRestoredSchedule=RuntimeChecks.WorldState(__instance);}}
[HarmonyPatch(typeof(AndroidEndlessRun),"SpawnActor")]
class WorldSpawnScope
{
 static void Prefix(string tag,bool elite,int index){if(RuntimeChecks.WorldMode)RuntimeChecks.WorldCurrentSpawn=new RuntimeChecks.WorldSpawn{Map=SelectedMap.MapData.name,Tag=tag,Index=index,Frame=Time.frameCount,Elite=elite};}
 static void Postfix(){var edge=RuntimeChecks.WorldCurrentSpawn;if(edge==null)return;edge.HP=edge.Actor.maxHP;edge.Damage=edge.Actor.GetComponent<AIComponent>().damageToPlayer;RuntimeChecks.WorldSpawns.Add(edge);RuntimeChecks.WorldCurrentSpawn=null;}
}
[HarmonyPatch(typeof(ObjectPooler),nameof(ObjectPooler.GetPooledObject))]
class WorldPoolProbe
{static void Postfix(GameObject __result){if(RuntimeChecks.WorldCurrentSpawn!=null&&__result!=null)RuntimeChecks.WorldCurrentSpawn.Actor=__result.GetComponent<Health>();if(RuntimeChecks.WorldMode&&RuntimeChecks.WorldCaptureBoss&&RuntimeChecks.WorldBoss==null&&__result!=null&&__result.GetComponent<Health>()!=null)RuntimeChecks.WorldBoss=__result.GetComponent<Health>();}}
[HarmonyPatch(typeof(AndroidEndlessRun),"SpawnBoss")]
class WorldBossProbe
{static void Prefix(){if(RuntimeChecks.WorldMode){RuntimeChecks.WorldBoss=null;RuntimeChecks.WorldCaptureBoss=true;}}static void Postfix(){RuntimeChecks.WorldCaptureBoss=false;}}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class WorldBulletProbe
{static void Postfix(bool __4,Projectile __result){if(!RuntimeChecks.WorldMode||!RuntimeChecks.WorldCaptureBullet||__4)return;if(RuntimeChecks.WorldBullet==null)RuntimeChecks.WorldBullet=__result;RuntimeChecks.EmitterPark(__result);}}
