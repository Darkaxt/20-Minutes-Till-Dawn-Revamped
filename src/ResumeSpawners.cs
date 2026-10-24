using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.Pickups;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;
static partial class RunResume
{
 static readonly string[] HordeFields={"spawnRateMulitplier","healthMultiplier","eliteHealthMultiplier","enemyDamage","eliteDamage","speedMultiplier","eliteSpeedMultiplier"};
 static readonly string[] BossFields={"healthMultiplier","enemyDamage","cooldownRate"};
 internal class SpawnIdentity{public bool boss;public int index;public string tag;}
 internal static SpawnIdentity SpawnContext;
 static readonly Dictionary<BossSpawn,int> BossIndices=new Dictionary<BossSpawn,int>();
 static readonly Dictionary<SpawnSession,int> WaveIndices=new Dictionary<SpawnSession,int>();
 internal static void ReleaseSpawnIdentities(){BossIndices.Clear();WaveIndices.Clear();SpawnContext=null;}
 static List<SpawnSession> Waves(MapData map,int cycle)=>map.endless&&cycle>0?map.endlessSpawnSessions:map.spawnSessions;
 static List<BossSpawn> Bosses(MapData map,int cycle)=>map.endless&&cycle>0?map.endlessBossSpawn:map.bossSpawns;
 static float[] Values(object owner,string[] fields)=>fields.Select(f=>Convert.ToSingle(AccessTools.Field(owner.GetType(),f).GetValue(owner))).ToArray();
 static void RestoreValues(object owner,string[] fields,float[] values)
 {if(values==null||values.Length!=fields.Length)throw new InvalidOperationException("Unsupported saved spawner state");for(int i=0;i<fields.Length;i++){var f=AccessTools.Field(owner.GetType(),fields[i]);f.SetValue(owner,Convert.ChangeType(values[i],f.FieldType));}}
 static void CaptureSpawners(Snapshot saved)
 {saved.hordeModifiers=Values(Object.FindObjectOfType<HordeSpawner>(),HordeFields);saved.bossModifiers=Values(Object.FindObjectOfType<BossSpawner>(),BossFields);}
 public static void InitializeMap(MapInitializer initializer)
 {
  var saved=Pending;var map=SelectedMap.MapData;
  var horde=(HordeSpawner)AccessTools.Field(typeof(MapInitializer),"hordeSpawner").GetValue(initializer);
  var boss=(BossSpawner)AccessTools.Field(typeof(MapInitializer),"bossSpawner").GetValue(initializer);
  GameTimer.SharedInstance.timeLimit=map.timeLimit;GameTimer.SharedInstance.endless=map.endless;
  RestoreValues(horde,HordeFields,saved.hordeModifiers);RestoreValues(boss,BossFields,saved.bossModifiers);
  RunRecorder.Instance.EndlessCycle=saved.endlessCycle;float relative=saved.roundElapsed;
  var original=Waves(map,saved.endlessCycle);var adjusted=new List<SpawnSession>();WaveIndices.Clear();
  for(int i=0;i<original.Count;i++)
  {
   var wave=original[i];if(wave.isElite&&saved.killedElites.Contains(i))continue;
   float end=wave.startTime+wave.duration;
   if(relative>=end&&!wave.isElite)continue;
   var copy=new SpawnSession{monsterPrefab=wave.monsterPrefab,HP=wave.HP,maximum=wave.maximum,numPerSpawn=wave.numPerSpawn,
    spawnCooldown=wave.spawnCooldown,isElite=wave.isElite,startTime=Mathf.Max(0,wave.startTime-relative),duration=relative>=end?wave.duration:end-Mathf.Max(relative,wave.startTime)};
   adjusted.Add(copy);WaveIndices.Add(copy,i);
  }
  horde.LoadSpawners(adjusted);BossIndices.Clear();var scheduled=new List<BossSpawn>();var originalBosses=Bosses(map,saved.endlessCycle);
  for(int i=0;i<originalBosses.Count;i++)
  {
   if(saved.killedBosses.Contains(i))continue;var b=originalBosses[i];
   var copy=new BossSpawn{bossPrefab=b.bossPrefab,maxHP=b.maxHP,spawnPosition=b.spawnPosition,timeToSpawn=Mathf.Max(0,b.timeToSpawn-relative),dontSpawnArena=b.dontSpawnArena,killAllOnSpawn=b.killAllOnSpawn};
   BossIndices.Add(copy,i);scheduled.Add(copy);
  }
  boss.LoadSpawners(scheduled);AndroidRuneStartup.Apply();PowerupGenerator.Instance.InitPowerupPool(map.numPowerupsRepeat);AndroidAIRecipes.SpawnMap(map.mapPrefab);
  if(map.endless)
  {
   float duration=saved.endlessCycle==0?map.timeLimit:map.endlessLoopTime;
   initializer.StartCoroutine(ResumeEndlessLoop(map,horde,boss,Mathf.Max(0,duration-relative),saved.endlessCycle+1));
  }
 }
 static IEnumerator ResumeEndlessLoop(MapData map,HordeSpawner horde,BossSpawner boss,float remaining,int cycle)
 {
  yield return new WaitForSeconds(remaining);
  while(true)
  {
   horde.healthMultiplier=1+Mathf.Pow(cycle,3);horde.eliteHealthMultiplier=1+Mathf.Pow(cycle,2);horde.speedMultiplier+=.2f;
   boss.healthMultiplier+=3;boss.cooldownRate+=.25f;
   horde.LoadSpawners(map.endlessSpawnSessions);boss.LoadSpawners(map.endlessBossSpawn);cycle++;
   yield return new WaitForSeconds(map.endlessLoopTime);
  }
 }
 static void RestoreSpawners(Snapshot saved)
 {
  var r=RunRecorder.Instance;r.RoundStart=saved.roundStart;r.RoundClock=Time.time-saved.roundElapsed;r.EndlessCycle=saved.endlessCycle;r.KilledBosses.UnionWith(saved.killedBosses);r.KilledElites.UnionWith(saved.killedElites);
  var horde=Object.FindObjectOfType<HordeSpawner>();var boss=Object.FindObjectOfType<BossSpawner>();
  var source=Object.FindObjectOfType<AndroidEndlessRun>();
  if(!SelectedMap.MapData.darknessDisabled&&!ChallengeMode.Active&&saved.darkness>=10)AndroidTimedEnemyDamage.RestoreOwner(horde);
  var hordeValues=(float[])saved.hordeModifiers.Clone();var bossValues=(float[])saved.bossModifiers.Clone();
  int previousCycle=source?.PreviousWindowsCycle??saved.endlessCycle;
  if(saved.version==3&&SelectedMap.MapData.endless&&previousCycle>0)
  {
   // Remove only the previous Windows loop's documented growth. Health fields
   // were overwritten by that loop, so recover their fresh native difficulty
   // values after all scene Start initializers have completed.
   hordeValues[1]=horde.healthMultiplier;hordeValues[2]=horde.eliteHealthMultiplier;hordeValues[5]-=.2f*previousCycle;
   bossValues[0]-=3*previousCycle;bossValues[2]-=.25f*previousCycle;
  }
  // Legacy Windows D10 may have saved its one-shot late enemy-damage
  // addition. These map
  // owners have not loaded saved modifiers before this post-Start boundary;
  // use their actual fresh damage, without guessing the old timer history.
  // Custom spawn factors remain separately owned and applied at spawn time.
  if(saved.version<7&&source!=null&&!SelectedMap.MapData.darknessDisabled&&saved.darkness>=10)
   hordeValues[3]=horde.enemyDamage;
  // D15 is a source assignment, not accumulated run history. The old native
  // snapshot must not overwrite it with1.8. Custom scales are saved separately.
  if(!SelectedMap.MapData.endless&&!ChallengeMode.Active&&saved.darkness==15)
   bossValues[2]=AndroidDarknessBossCooldown.SourceReduction;
  RestoreValues(horde,HordeFields,hordeValues);RestoreValues(boss,BossFields,bossValues);
  horde.GetComponent<AndroidLateEnemyDamage>()?.Sync();
  if(source!=null)source.RestoreEncounters(saved);
  ChallengeRun.Instance?.RestoreKills(saved.challengeEnemies,saved.challengeBosses);
 }
 static void CaptureDrops(Snapshot saved)
 {
  var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();
  saved.chests=pool.CountCollectableDrops("Chest");saved.devilDeals=pool.CountCollectableDrops("DevilDeal");
 }
 static void RestoreDrops(Snapshot saved)
 {
  var pool=ObjectPooler.SharedInstance;if(pool==null)return;
  var player=PlayerController.Instance;var center=player!=null?player.transform.position:Vector3.zero;
  foreach(var pair in new[]{Tuple.Create(typeof(ChestPickup),saved.chests),Tuple.Create(typeof(DevilDealPickup),saved.devilDeals)})
  {
   string tag=pair.Item1==typeof(ChestPickup)?"Chest":"DevilDeal";
   for(int i=0;i<pair.Item2;i++)
   {var offset=UnityEngine.Random.insideUnitCircle;var drop=pool.GetPooledObject(tag);if(drop!=null)drop.transform.position=center+new Vector3(offset.x*2,offset.y*2,0);}
  }
 }
 internal static SpawnIdentity BossIdentity(object routine)
 {
  var b=(BossSpawn)AccessTools.Field(routine.GetType(),"spawner").GetValue(routine);if(b==null)return null;
  int index=BossIndices.TryGetValue(b,out int stored)?stored:Bosses(SelectedMap.MapData,RunRecorder.Instance.EndlessCycle).IndexOf(b);
  return index<0?null:new SpawnIdentity{boss=true,index=index,tag=b.bossPrefab.name};
 }
 internal static void SpawnFromSession(HordeSpawner owner,string tag,int hp,bool elite,int activeIndex)
 {
  var previous=SpawnContext;
  if(elite&&RunRecorder.Instance!=null)
  {
   var session=((List<SpawnSession>)AccessTools.Field(typeof(HordeSpawner),"activeSpawners").GetValue(owner))[activeIndex];
   int index=WaveIndices.TryGetValue(session,out int stored)?stored:Waves(SelectedMap.MapData,RunRecorder.Instance.EndlessCycle).IndexOf(session);
   if(index<0)throw new InvalidOperationException("Native elite session has no source identity");
   SpawnContext=new SpawnIdentity{boss=false,index=index,tag=tag};
  }
  try{AccessTools.Method(typeof(HordeSpawner),"Spawn").Invoke(owner,new object[]{tag,hp,elite});}finally{SpawnContext=previous;}
 }
}
[HarmonyPatch(typeof(HordeSpawner),nameof(HordeSpawner.LoadSpawners))]
class ResumeRoundBoundary
{
 static void Prefix(List<SpawnSession> spawnSessions)
 {
  var r=RunRecorder.Instance;var map=SelectedMap.MapData;if(r==null||map==null||RunResume.Restoring)return;
  if(ReferenceEquals(spawnSessions,map.spawnSessions)){r.RoundStart=0;r.RoundClock=Time.time;r.EndlessCycle=0;}
  else if(map.endless&&ReferenceEquals(spawnSessions,map.endlessSpawnSessions)){r.RoundStart=GameTimer.SharedInstance.timer;r.RoundClock=Time.time;r.EndlessCycle++;r.KilledBosses.Clear();r.KilledElites.Clear();r.Encounters.Clear();}
 }
}
[HarmonyPatch]
class ResumeBossIdentity
{
 static System.Reflection.MethodBase TargetMethod()=>AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(BossSpawner),"WaitToSpawnCR"));
 static void Prefix(object __instance,out RunResume.SpawnIdentity __state){__state=RunResume.SpawnContext;if(RunRecorder.Instance!=null)RunResume.SpawnContext=RunResume.BossIdentity(__instance);}
 static Exception Finalizer(Exception __exception,RunResume.SpawnIdentity __state){RunResume.SpawnContext=__state;return __exception;}
}
[HarmonyPatch(typeof(HordeSpawner),"Spawn")]
class ResumeEliteIdentity
{
 static void Prefix(string objectPoolTag,int HP,bool isElite,out RunResume.SpawnIdentity __state)
 {
  __state=RunResume.SpawnContext;var r=RunRecorder.Instance;var map=SelectedMap.MapData;if(!isElite||r==null||map==null||__state!=null)return;
  var waves=map.endless&&r.EndlessCycle>0?map.endlessSpawnSessions:map.spawnSessions;
  var matching=waves.Select((w,i)=>new{w,i}).Where(x=>x.w.isElite&&x.w.monsterPrefab.name==objectPoolTag&&x.w.HP==HP).ToArray();int index=matching.Length==1?matching[0].i:-1;
  if(index>=0)RunResume.SpawnContext=new RunResume.SpawnIdentity{boss=false,index=index,tag=objectPoolTag};
 }
 static Exception Finalizer(Exception __exception,RunResume.SpawnIdentity __state){RunResume.SpawnContext=__state;return __exception;}
}
[HarmonyPatch(typeof(HordeSpawner),"Update")]
class ResumeExactEliteSession
{
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,MethodBase __originalMethod)
 {
  var locals=__originalMethod.GetMethodBody().LocalVariables;
  if(locals.Count!=3||locals.Any(l=>l.LocalType!=typeof(int)))throw new InvalidOperationException("Unsupported native horde iteration layout");
  var spawn=AccessTools.Method(typeof(HordeSpawner),"Spawn");int bound=0;
  foreach(var instruction in instructions)
  {
   if(!instruction.Calls(spawn)){yield return instruction;continue;}
   // Keep the original instruction object, including its branch/region metadata,
   // at this boundary. The existing spawn arguments remain on the stack.
   instruction.opcode=OpCodes.Ldloc_0;instruction.operand=null;yield return instruction;
   yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(RunResume),nameof(RunResume.SpawnFromSession)));bound++;
  }
  if(bound!=1)throw new InvalidOperationException("Unsupported native horde spawn call layout");
 }
}
[HarmonyPatch(typeof(ObjectPooler),nameof(ObjectPooler.GetPooledObject))]
class ResumeEncounterBinding
{
 static void Postfix(string tag,GameObject __result)
 {var c=RunResume.SpawnContext;var r=RunRecorder.Instance;if(c==null||r==null||c.tag!=tag||__result==null)return;var h=__result.GetComponent<Health>();if(h!=null)r.Encounters[h]=Tuple.Create(c.boss,c.index);}
}
