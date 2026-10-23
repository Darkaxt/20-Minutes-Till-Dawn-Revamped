using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using flanne;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

internal sealed class AndroidEndlessCurve
{
 readonly float initial,end,minimum,maximum;readonly int ease;
 internal AndroidEndlessCurve(float initial,float end,float minimum,float maximum,int ease){this.initial=initial;this.end=end;this.minimum=minimum;this.maximum=maximum;this.ease=ease;}
 internal float Value(float level)
 {
  float current=Mathf.Clamp(level,minimum,maximum),delta=end-initial;
  // Source DataCurve calls Curve with (level, initial, delta, maximum).
  if(ease==0)return current*delta/maximum+initial;
  if(ease==34){float t=current/maximum;return initial-(Mathf.Sqrt(1-t*t)-1)*delta;}
  throw new InvalidOperationException("Unimplemented source Endless curve: "+ease);
 }
}
[Serializable]
public sealed class AndroidEndlessState
{
 public float roundStart,nextBoss;public int bossLevel;
 public float[] waveTimers;
 public List<int> active=new List<int>(),pending=new List<int>(),queue=new List<int>(),spawnedElites=new List<int>();
 public List<int> pendingBosses=new List<int>();
 public List<AndroidEndlessBossState> bosses=new List<AndroidEndlessBossState>();
}
[Serializable]
public sealed class AndroidEndlessBossState
{public string tag;public int level;public List<int> affixes=new List<int>();}
public sealed class AndroidEndlessRun:MonoBehaviour
{
 internal static bool Active=>SelectedMap.MapData!=null&&new[]{"Endless_Forest","10M_Forest","20M_Forest","20M_Temple","20M_PumpkinPatch","Challenge_Forest"}.Contains(SelectedMap.MapData.name);
 bool endless;MapData map;
 internal int? PreviousWindowsCycle;
 HordeSpawner horde;BossSpawner boss;
 List<AndroidEndlessTables.Wave> waves;
 readonly List<int> active=new List<int>(),pending=new List<int>();readonly Queue<int> queue=new Queue<int>();readonly HashSet<int> spawnedElites=new HashSet<int>();
 readonly List<int> pendingBosses=new List<int>();
 readonly Dictionary<Health,AndroidEndlessBossState> liveBosses=new Dictionary<Health,AndroidEndlessBossState>();
 Dictionary<string,GameObject> prefabs;float roundStart,roundDuration,nextBoss;int bossLevel;
 static string PrefabName(string pool)=>AndroidPoolTables.PrefabName(pool);
 float Modifier(string field,float value)=>value*(CustomMode.Active?CustomMode.Get("spawn:"+field):1);
 void Awake()=>this.AddObserver(OnDeath,Health.DeathEvent);
 void OnDestroy()=>this.RemoveObserver(OnDeath,Health.DeathEvent);
 void OnDeath(object sender,object args){if(sender is Health health)liveBosses.Remove(health);}
 internal void Initialize(MapInitializer initializer,RunResume.Snapshot saved)
 {
  map=SelectedMap.MapData;endless=map.endless;
  horde=(HordeSpawner)AccessTools.Field(typeof(MapInitializer),"hordeSpawner").GetValue(initializer);boss=(BossSpawner)AccessTools.Field(typeof(MapInitializer),"bossSpawner").GetValue(initializer);
  horde.LoadSpawners(new List<SpawnSession>());horde.enabled=false;boss.LoadSpawners(new List<BossSpawn>());
  if(ChallengeMode.Active)initializer.gameObject.AddComponent<ChallengeRun>().Initialize();
  prefabs=Resources.FindObjectsOfTypeAll<Health>().Where(h=>!h.gameObject.scene.IsValid()).GroupBy(h=>h.gameObject.name).ToDictionary(g=>g.Key,g=>g.First().gameObject);
  waves=endless?AndroidEndlessTables.Waves():map.spawnSessions.Select((s,i)=>new AndroidEndlessTables.Wave{Session=s,Pools=new[]{ChallengeMode.Active?s.monsterPrefab.name:AndroidMapTables.PoolTags(map)[i]}}).ToList();roundDuration=waves.Max(w=>w.Session.startTime+w.Session.duration);
  foreach(var tag in waves.SelectMany(w=>w.Pools).Concat(endless?AndroidEndlessTables.BossPools:map.bossSpawns.Select(b=>b.bossPrefab.name)).Distinct())ObjectPooler.SharedInstance.AddObject(tag,prefabs[PrefabName(tag)],1);
  nextBoss=AndroidEndlessTables.FirstBoss;
  for(int i=0;i<waves.Count;i++)pending.Add(i);
  if(!endless)for(int i=0;i<map.bossSpawns.Count;i++)pendingBosses.Add(i);
  GameTimer.SharedInstance.timeLimit=endless?float.MaxValue:map.timeLimit;GameTimer.SharedInstance.endless=endless;
  AndroidRuneStartup.Apply();PowerupGenerator.Instance.InitPowerupPool(SelectedMap.MapData.numPowerupsRepeat);AndroidAIRecipes.SpawnMap(SelectedMap.MapData.mapPrefab);
  if(saved!=null)
  {
   var state=saved.sourceEndless??TranslateWindowsSchedule(saved);saved.sourceEndless=state;
   roundStart=state.roundStart;nextBoss=state.nextBoss;bossLevel=state.bossLevel;active.AddRange(state.active);pending.Clear();pending.AddRange(state.pending);foreach(int i in state.queue)queue.Enqueue(i);spawnedElites.UnionWith(state.spawnedElites);
   for(int i=0;i<waves.Count;i++)waves[i].Session.timer=state.waveTimers[i];
   pendingBosses.Clear();pendingBosses.AddRange(state.pendingBosses);
  }
 }
 internal void RestoreEncounters(RunResume.Snapshot saved)
 {
  foreach(int i in saved.sourceEndless.spawnedElites.Where(i=>!saved.killedElites.Contains(i)))Spawn(i);
  if(endless)foreach(var encounter in saved.sourceEndless.bosses)SpawnBoss(encounter);
  else for(int i=0;i<map.bossSpawns.Count;i++)if(!pendingBosses.Contains(i)&&!saved.killedBosses.Contains(i))SpawnScheduledBoss(i);
 }
 AndroidEndlessState TranslateWindowsSchedule(RunResume.Snapshot saved)
 {
  // Version3 saved the semantic build and elapsed map clock, not a frozen
  // spawn queue. Translate encounter indices by the original prefab identity.
  // Native Endless's initial negative display clock (A70) is not elapsed
  // source time. Its recorded first-round encounter clock owns that position.
  if(endless&&saved.version<7&&saved.endlessCycle==0&&saved.seconds<0)
   saved.seconds=saved.roundStart+saved.roundElapsed;
  var oldWaves=AndroidMapTables.LegacyWaves(map,saved.endlessCycle);var oldBosses=AndroidMapTables.LegacyBosses(map,saved.endlessCycle);
  saved.killedElites=saved.killedElites.SelectMany(index=>waves.Select((w,i)=>new{w,i}).Where(x=>x.w.Session.isElite&&x.w.Pools.Any(tag=>PrefabName(tag)==oldWaves[index].monsterPrefab.name)).Select(x=>x.i)).Distinct().ToList();
  if(!endless)saved.killedBosses=saved.killedBosses.SelectMany(index=>map.bossSpawns.Select((b,i)=>new{b,i}).Where(x=>x.b.bossPrefab.name==oldBosses[index].bossPrefab.name).Select(x=>x.i)).Distinct().ToList();
  var state=new AndroidEndlessState{roundStart=ChallengeMode.Active?saved.roundStart:Mathf.Floor(saved.seconds/roundDuration)*roundDuration,nextBoss=AndroidEndlessTables.FirstBoss,waveTimers=new float[waves.Count]};
  float relative=saved.seconds-state.roundStart;
  for(int i=0;i<waves.Count;i++)
  {
   var wave=waves[i].Session;
   if(relative<wave.startTime)state.pending.Add(i);else if(relative<wave.startTime+wave.duration)state.active.Add(i);
   if(wave.isElite&&(saved.killedElites.Contains(i)||relative>=wave.startTime+wave.duration))state.spawnedElites.Add(i);
  }
  if(endless)
  {
   while(state.nextBoss<=saved.seconds){state.nextBoss+=AndroidEndlessTables.BossSpawnCdUp.Value(state.bossLevel);state.bossLevel++;}
   // The old snapshot records scheduled identities and defeated indices,
   // not source random-boss records. Keep overdue undefeated encounters by
   // their original identity instead of silently dropping them at migration.
   // They had no affixes; only subsequent newly spawned source bosses roll
   // those. Source scaling uses the encounter's original elapsed spawn time.
   for(int i=0;i<oldBosses.Count;i++)
   {
    var old=oldBosses[i];if(saved.killedBosses.Contains(i)||old.timeToSpawn>saved.roundElapsed)continue;
    string tag=AndroidEndlessTables.BossPools.Single(pool=>PrefabName(pool)==old.bossPrefab.name);
    state.bosses.Add(new AndroidEndlessBossState{tag=tag,level=BossLevelAt(saved.roundStart+old.timeToSpawn)});
   }
  }
  else for(int i=0;i<map.bossSpawns.Count;i++)if(map.bossSpawns[i].timeToSpawn>saved.seconds)state.pendingBosses.Add(i);
  PreviousWindowsCycle=saved.endlessCycle;saved.endlessCycle=Mathf.FloorToInt(saved.seconds/roundDuration);
  saved.roundStart=state.roundStart;saved.roundElapsed=relative;
  return state;
 }
 static int BossLevelAt(float elapsed)
 {
  int level=0;float spawn=AndroidEndlessTables.FirstBoss;
  while(spawn+AndroidEndlessTables.BossSpawnCdUp.Value(level)<=elapsed)
  {spawn+=AndroidEndlessTables.BossSpawnCdUp.Value(level);level++;}
  return level;
 }
 internal AndroidEndlessState Capture()=>new AndroidEndlessState{roundStart=roundStart,nextBoss=nextBoss,bossLevel=bossLevel,waveTimers=waves.Select(w=>w.Session.timer).ToArray(),active=active.ToList(),pending=pending.ToList(),queue=queue.ToList(),spawnedElites=spawnedElites.OrderBy(i=>i).ToList(),pendingBosses=pendingBosses.ToList(),bosses=liveBosses.Where(p=>p.Key.gameObject.activeSelf&&!p.Key.isDead).Select(p=>p.Value).ToList()};
 void Update()
 {
  if(RunResume.Restoring)return;
  float now=GameTimer.SharedInstance.timer,relative=now-roundStart,level=Mathf.Floor(now/AndroidEndlessTables.LevelSeconds);
  for(int n=pending.Count-1;n>=0;n--)
  {int i=pending[n];if(relative<waves[i].Session.startTime)continue;active.Add(i);waves[i].Session.timer=0;pending.RemoveAt(n);}
  for(int n=active.Count-1;n>=0;n--)
  {int i=active[n];var wave=waves[i].Session;if(relative<wave.startTime+wave.duration)continue;if(wave.isElite&&!spawnedElites.Contains(i))Spawn(i);active.RemoveAt(n);}
  foreach(int i in active)
  {
   var wave=waves[i];var session=wave.Session;session.timer-=Time.deltaTime;if(session.timer>=0)continue;
   int count=0;var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();
   foreach(string tag in wave.Pools)count=unchecked(count+pool.CountAllocated(tag));
   int maximum=Mathf.FloorToInt(session.maximum*(endless?AndroidEndlessTables.HordeMaxNumsUp.Value(level):1)*(ChallengeMode.Active?ChallengeMode.AliveMultiplier(Loadout.difficultyLevel):1));
   if(count<maximum)
   {float rate=ChallengeMode.Active?ChallengeMode.SpawnRate(horde.spawnRateMulitplier,Loadout.difficultyLevel):horde.spawnRateMulitplier;int amount=Mathf.FloorToInt(Modifier("spawnRateMulitplier",rate)*session.numPerSpawn*(endless?AndroidEndlessTables.HordeNumPerWpawnUp.Value(level):1));for(int j=0;j<amount;j++)queue.Enqueue(i);}
   session.timer+=session.spawnCooldown*(endless?AndroidEndlessTables.HordeSpawnCdUp.Value(level):1);
  }
  int batch=Mathf.Min(5,queue.Count);for(int n=0;n<batch;n++)Spawn(queue.Dequeue());
  if(relative>roundDuration)
  {roundStart+=roundDuration;for(int i=0;i<waves.Count;i++){waves[i].Session.timer=0;pending.Add(i);}RunRecorder.Instance.RoundStart=roundStart;RunRecorder.Instance.RoundClock=Time.time-(now-roundStart);RunRecorder.Instance.EndlessCycle++;}
  for(int n=pendingBosses.Count-1;n>=0;n--){int i=pendingBosses[n];if(now<map.bossSpawns[i].timeToSpawn)continue;SpawnScheduledBoss(i);pendingBosses.RemoveAt(n);}
  if(endless&&now>=nextBoss)
  {
   nextBoss+=AndroidEndlessTables.BossSpawnCdUp.Value(bossLevel);SpawnBoss();bossLevel++;
  }
 }
 void Spawn(int index)
 {
  var wave=waves[index];string tag=wave.Pools[UnityEngine.Random.Range(0,wave.Pools.Length)];
  SpawnActor(tag,wave.Session.HP,wave.Session.isElite,index);
 }
 internal void SpawnActor(string tag,int baseHP,bool elite,int index=-1)
 {
  var previous=RunResume.SpawnContext;
  if(elite&&index>=0)RunResume.SpawnContext=new RunResume.SpawnIdentity{boss=false,index=index,tag=tag};
  GameObject actor;try{actor=ObjectPooler.SharedInstance.GetPooledObject(tag);}finally{RunResume.SpawnContext=previous;}
  float radius=(float)AccessTools.Field(typeof(HordeSpawner),"spawnRadius").GetValue(horde);
  Vector2 point=UnityEngine.Random.insideUnitCircle.normalized*radius;
  if(actor==null){Debug.LogError("Monster spawn failed: "+tag);return;}
  actor.transform.position=PlayerController.Instance.transform.position+(Vector3)point;
  float level=Mathf.Floor(GameTimer.SharedInstance.timer/AndroidEndlessTables.LevelSeconds),growth=endless?AndroidEndlessTables.HordehealthUp.Value(level):1;
  var health=actor.GetComponent<Health>();
  if(health!=null)
  {
   health.maxHP=Mathf.FloorToInt((elite?Modifier("eliteHealthMultiplier",horde.eliteHealthMultiplier):Modifier("healthMultiplier",horde.healthMultiplier))*baseHP*growth);
   if(elite&&index>=0)spawnedElites.Add(index);
  }
  var ai=actor.GetComponent<AIComponent>();
  if(ai!=null)
  {
   ai.damageToPlayer=(int)(elite?Modifier("eliteDamage",horde.eliteDamage):Modifier("enemyDamage",horde.enemyDamage));
   float speed=elite?Modifier("eliteSpeedMultiplier",horde.eliteSpeedMultiplier):Modifier("speedMultiplier",horde.speedMultiplier),speedGrowth=endless?AndroidEndlessTables.HordeMovespeedUp.Value(level):1;
   ai.maxMoveSpeed=ai.baseMaxMoveSpeed*speed*speedGrowth;ai.acceleration=ai.baseAcceleration*speed*speedGrowth;
  }
 }
 void SpawnBoss(AndroidEndlessBossState saved=null)
 {
  string tag=saved!=null?saved.tag:AndroidEndlessTables.BossPools[UnityEngine.Random.Range(0,AndroidEndlessTables.BossPools.Length)];int level=saved!=null?saved.level:bossLevel;
  var record=new AndroidEndlessBossState{tag=tag,level=level};
  bool previous=BossSpawnAchievementBoundary.Spawning;BossSpawnAchievementBoundary.Spawning=true;GameObject actor;
  try{actor=ObjectPooler.SharedInstance.GetPooledObject(tag);}finally{BossSpawnAchievementBoundary.Spawning=previous;}
  float radians=UnityEngine.Random.Range(0,360)*Mathf.PI/180f;
  actor.transform.position=PlayerController.Instance.transform.position+new Vector3(Mathf.Sin(radians),Mathf.Cos(radians))*AndroidEndlessTables.BossRadius;
  var health=actor.GetComponent<Health>();if(health!=null)health.maxHP=Mathf.FloorToInt((AndroidEndlessTables.BossHP+AndroidEndlessTables.BosshealthUp.Value(level))*Modifier("bossHealth",boss.healthMultiplier));
  var ai=actor.GetComponent<AIComponent>();if(ai!=null){ai.damageToPlayer=(int)Modifier("bossDamage",boss.enemyDamage);ai.GetComponent<AndroidAIActor>().SpecialCooldownMultiplier=Modifier("bossCooldown",boss.cooldownRate)*AndroidEndlessTables.BossspecialcooldownUp.Value(level);}
  var affixes=AndroidAffixArt.Attach(actor,PrefabName(tag));
  if(affixes!=null)
  {
   if(saved!=null){foreach(int value in saved.affixes){affixes.Add(value);record.affixes.Add(value);}}
   else
   {var remaining=new List<int>{0,1,2,3};int amount=Mathf.FloorToInt(AndroidEndlessTables.BossAffixUp.Value(level));
   for(int i=0;i<amount&&remaining.Count>0;i++){int pick=UnityEngine.Random.Range(0,remaining.Count);int value=remaining[pick];remaining.RemoveAt(pick);affixes.Add(value);record.affixes.Add(value);}}
  }
  liveBosses[health]=record;
  Arena(health);
 }
 internal void SpawnScheduledBoss(int index,bool scheduled=true)
 {
  var spawn=map.bossSpawns[index];string tag=spawn.bossPrefab.name;var previous=RunResume.SpawnContext;bool previousSpawning=BossSpawnAchievementBoundary.Spawning;
  if(scheduled)RunResume.SpawnContext=new RunResume.SpawnIdentity{boss=true,index=index,tag=tag};BossSpawnAchievementBoundary.Spawning=true;GameObject actor;
  try{actor=ObjectPooler.SharedInstance.GetPooledObject(tag);}finally{RunResume.SpawnContext=previous;BossSpawnAchievementBoundary.Spawning=previousSpawning;}
  actor.transform.position=PlayerController.Instance.transform.position+spawn.spawnPosition;
  var health=actor.GetComponent<Health>();if(health!=null)health.maxHP=Mathf.FloorToInt(spawn.maxHP*Modifier("bossHealth",boss.healthMultiplier));
  var ai=actor.GetComponent<AIComponent>();if(ai!=null){ai.damageToPlayer=(int)Modifier("bossDamage",boss.enemyDamage);ai.GetComponent<AndroidAIActor>().SpecialCooldownMultiplier=Modifier("bossCooldown",boss.cooldownRate);}
  Arena(health);
 }
 void Arena(Health health)
 {var prefab=(GameObject)AccessTools.Field(typeof(BossSpawner),"arenaMonsterPrefab").GetValue(boss);var arena=BossArenaLifetime.Acquire(prefab,health);arena.transform.position=PlayerController.Instance.transform.position;}
}
