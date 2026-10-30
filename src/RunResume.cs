using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
using flanne.PerkSystem;
using PlayerXP=flanne.Player.PlayerXP;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

public static partial class RunResume
{
 [Serializable]public class Acquisition{public bool evolution;public string key;}
 [Serializable]public class RuneRank{public string name;public int rank;}
 [Serializable]public class PoolEntry{public string key;public int remaining;}
 [Serializable]public class OfferPool{public string field;public List<PoolEntry> entries=new List<PoolEntry>();}
 // Optional wire members let the existing version-aware validator/migrator
 // read older schemas. Serializable alone makes every field required in Mono.
 [Serializable,DataContract]public class Snapshot
 {
  [DataMember(IsRequired=true)]public int version=7;[DataMember(IsRequired=true)]public bool valid=true;[DataMember(IsRequired=true)]public string savedUtc,character,weapon,map;
  [DataMember(IsRequired=true)]public int darkness,hp,maxHP,soulHP,maxSoulHP,level,ammo,kills;
  [DataMember(IsRequired=true)]public float xp,seconds,x,y,roundStart,roundElapsed;[DataMember(IsRequired=true)]public int endlessCycle;[DataMember(IsRequired=true)]public bool hitless,oneSecondWarning,usedShooting,usedManualShooting;
  [DataMember(IsRequired=true)]public List<RuneRank> runes=new List<RuneRank>();[DataMember(IsRequired=true)]public List<Acquisition> acquired=new List<Acquisition>();
  [DataMember(IsRequired=true)]public List<OfferPool> pools=new List<OfferPool>();[DataMember(IsRequired=true)]public List<string> taken=new List<string>();
  [DataMember]public List<PoolEntry> sourcePowerupCounts;
  [DataMember(IsRequired=true)]public List<int> killedBosses=new List<int>(),killedElites=new List<int>();
  [DataMember(IsRequired=true)]public int challengeEnemies,challengeBosses;[DataMember(IsRequired=true)]public int chests,devilDeals;
  [DataMember(IsRequired=true)]public ResumeRuntime.State runtime;
  [DataMember]public CustomMode.Rules custom;
  [DataMember]public bool? blessingPending;[DataMember]public float blessingRemaining;
  [DataMember]public bool? unlockHitless;
  [DataMember(IsRequired=true)]public float[] hordeModifiers,bossModifiers;
  [DataMember]public AndroidEndlessState sourceEndless;
  [DataMember]public ExplorationState exploration;
  [DataMember]public int? eliteClaims;
  [DataMember]public EliteTierProgress eliteTiers;
  [DataMember]public RuneRewardStateData runeRewards;
  [DataMember]public int? weaponFamiliarity;
  [DataMember]public bool? characterWeaponFamiliarity;
 }
 public static Snapshot Pending;
 public static bool Restoring=>Pending!=null;
 public static string PathName=>Path.Combine(Application.persistentDataPath,"revamped-run.json");
 public static string Key(Powerup p)=>((LocalizedString)AccessTools.Field(typeof(Powerup),"nameStrID").GetValue(p)).key;
 public static string Key(GunEvolution e)=>((LocalizedString)AccessTools.Field(typeof(GunEvolution),"nameStrID").GetValue(e)).key;
 static Dictionary<string,Powerup> Powers()=>Resources.FindObjectsOfTypeAll<Powerup>().Where(p=>!string.IsNullOrEmpty(Key(p))).GroupBy(Key).ToDictionary(g=>g.Key,g=>g.First());
 static Dictionary<string,GunEvolution> Evolutions()=>Resources.FindObjectsOfTypeAll<GunEvolution>().Where(e=>!string.IsNullOrEmpty(Key(e))).GroupBy(Key).ToDictionary(g=>g.Key,g=>g.First());
 static readonly List<RuneData> RunLocalRunes=new List<RuneData>();
 internal static RuneData CopyRune(RuneData original){var copy=Object.Instantiate(original);copy.name=original.name;copy.level=original.level;RunLocalRunes.Add(copy);return copy;}
 internal static void ReleaseRunAssets(){foreach(var rune in RunLocalRunes)Object.Destroy(rune);RunLocalRunes.Clear();ReleaseSpawnIdentities();}
 public static Snapshot Read()
 {
  if(!File.Exists(PathName))return null;
  Snapshot saved;using(var stream=File.OpenRead(PathName))saved=(Snapshot)new DataContractJsonSerializer(typeof(Snapshot)).ReadObject(stream);
  if(saved==null||(saved.version<3||saved.version>7))throw new InvalidOperationException("This run checkpoint uses an unsupported version; it has been preserved.");
  if(!saved.valid)return null;
  if(string.IsNullOrEmpty(saved.character)||string.IsNullOrEmpty(saved.weapon)||string.IsNullOrEmpty(saved.map)||saved.hp<=0||saved.maxHP<saved.hp||saved.soulHP<0||saved.maxSoulHP<saved.soulHP||saved.level<1||saved.ammo<0||saved.kills<0||saved.endlessCycle<0||saved.roundElapsed<0||
   new[]{saved.xp,saved.seconds,saved.x,saved.y,saved.roundStart,saved.roundElapsed,saved.blessingRemaining}.Any(v=>float.IsNaN(v)||float.IsInfinity(v))||saved.blessingRemaining<0||
   saved.runes==null||saved.acquired==null||saved.pools==null||saved.taken==null||saved.killedBosses==null||saved.killedElites==null||saved.hordeModifiers==null||saved.bossModifiers==null||
   saved.hordeModifiers.Length!=HordeFields.Length||saved.bossModifiers.Length!=BossFields.Length||saved.hordeModifiers.Concat(saved.bossModifiers).Any(v=>float.IsNaN(v)||float.IsInfinity(v))||
   saved.runes.Any(r=>r==null||string.IsNullOrEmpty(r.name)||r.rank<1||r.rank>5)||saved.runes.Select(r=>r.name).Distinct().Count()!=saved.runes.Count||saved.acquired.Any(a=>a==null||string.IsNullOrEmpty(a.key))||saved.pools.Any(p=>p==null||p.entries==null||p.entries.Any(e=>e==null||string.IsNullOrEmpty(e.key)))||saved.taken.Any(string.IsNullOrEmpty))
   throw new InvalidOperationException("This run checkpoint is incomplete or invalid; it has been preserved.");
  if(saved.weaponFamiliarity.HasValue&&(saved.weaponFamiliarity<0||saved.weaponFamiliarity>15))throw new InvalidOperationException("The checkpoint's weapon familiarity rank is invalid; it has been preserved.");
  ResumeRuntime.Validate(saved.runtime);CustomMode.Validate(saved.custom);
  ExplorationRun.Validate(saved.exploration);
  RuneRewardRun.Validate(saved.runeRewards);
  if(saved.eliteClaims.HasValue&&(saved.eliteClaims.Value<0||saved.eliteClaims.Value>saved.kills/200))throw new InvalidOperationException("The checkpoint's elite cadence is invalid; it has been preserved.");
  var tiers=saved.eliteTiers;
  if(tiers!=null&&(tiers.eliteKills<0||tiers.plusClaims<0||tiers.plusKills<0||tiers.doubleClaims<0||tiers.eliteKills>(saved.eliteClaims??saved.kills/200)||tiers.plusClaims>tiers.eliteKills/10||tiers.plusKills>tiers.plusClaims||tiers.doubleClaims>tiers.plusKills/5))throw new InvalidOperationException("The checkpoint's Endless elite tiers are invalid; it has been preserved.");
  if(saved.version==7&&saved.runtime.sourcePowerups==null)throw new InvalidOperationException("This checkpoint omitted its ordered source runtime contract; it has been preserved.");
  if(saved.version<7&&saved.runtime.sourcePowerups!=null||saved.version==7&&saved.runtime.sourcePowerups.Any(p=>!saved.acquired.Any(a=>!a.evolution&&a.key==p.key)))throw new InvalidOperationException("This checkpoint has an unsupported source runtime owner; it has been preserved.");
  if(saved.version>=5&&saved.version<7&&(saved.runtime.generators==null||saved.runtime.growth==null||saved.runtime.rituals==null||saved.runtime.unharmed==null||saved.runtime.justice==null))throw new InvalidOperationException("This source checkpoint omitted its attachment runtime contract; it has been preserved.");
  if(saved.sourcePowerupCounts!=null&&(saved.sourcePowerupCounts.Any(p=>p==null||string.IsNullOrEmpty(p.key))||saved.sourcePowerupCounts.Select(p=>p.key).Distinct().Count()!=saved.sourcePowerupCounts.Count))throw new InvalidOperationException("The checkpoint's source upgrade counts are invalid; it has been preserved.");
  if(saved.custom!=null&&saved.custom.map!=saved.map)throw new InvalidOperationException("The Custom scenario does not match its checkpoint.");
  if(saved.version<7){ResumeRuntime.RebuildLegacySource(saved);ResumeRuntime.Validate(saved.runtime);}
  return saved;
 }
 static void Write(Snapshot saved)
 {
  string temp=PathName+".writing";
  using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None)){new DataContractJsonSerializer(typeof(Snapshot)).WriteObject(stream,saved);stream.Flush(true);}
  if(File.Exists(PathName))File.Replace(temp,PathName,null);else File.Move(temp,PathName);
 }
 public static void Invalidate()
 {if(File.Exists(PathName))Write(new Snapshot{valid=false,savedUtc=DateTime.UtcNow.ToString("O")});}
 public static bool CanSave()
 {
  var r=RunRecorder.Instance;var game=r?.GetComponent<GameController>();var p=PlayerController.Instance;
  return !Restoring&&!ResumeRuntime.RevivalInProgress&&RuneRewardRun.Instance?.Applying!=true&&r!=null&&!r.Finished&&p!=null&&p.playerHealth.hp>0&&SelectedMap.MapData!=null&&
   (game.CurrentState is CombatState||game.CurrentState is PauseState||game.CurrentState is OptionsState||game.CurrentState is SynergyUIState||game.CurrentState is RuneRewardState);
 }
 public static void Save()
 {
  if(!CanSave())return;
  var r=RunRecorder.Instance;var game=r.GetComponent<GameController>();var p=PlayerController.Instance;var hp=p.playerHealth;
  var state=new Snapshot{version=7,savedUtc=DateTime.UtcNow.ToString("O"),character=Loadout.CharacterSelection.name,weapon=Loadout.GunSelection.name,map=SelectedMap.MapData.name,
   darkness=Loadout.difficultyLevel,hp=hp.hp,maxHP=hp.maxHP,soulHP=hp.shp,maxSoulHP=hp.maxSHP,level=game.playerXP.level,
   xp=(float)AccessTools.Field(typeof(PlayerXP),"xp").GetValue(game.playerXP),ammo=p.ammo.amount,seconds=GameTimer.SharedInstance.timer,
   x=p.transform.position.x,y=p.transform.position.y,hitless=game.hitlessDetector.hitless,unlockHitless=r.UnlockHitless,kills=ScoreCalculator.SharedInstance.GetScore().enemiesKilled,
   acquired=r.Acquired.Select(a=>new Acquisition{key=a.key,evolution=a.evolution}).ToList(),runtime=ResumeRuntime.Capture(p),roundStart=r.RoundStart,roundElapsed=Time.time-r.RoundClock,endlessCycle=r.EndlessCycle,
   custom=CustomMode.Active?CustomMode.Current:null,blessingPending=r.Blessings.Any(b=>b.Waiting),blessingRemaining=r.Blessings.Where(b=>b.Waiting).Select(b=>Mathf.Max(0,b.Deadline-Time.time)).DefaultIfEmpty(0).Max(),usedShooting=game.shootDetector.usedShooting,usedManualShooting=game.shootDetector.usedManualShooting,
   killedBosses=r.KilledBosses.OrderBy(i=>i).ToList(),killedElites=r.KilledElites.OrderBy(i=>i).ToList(),oneSecondWarning=(bool)AccessTools.Field(typeof(GameTimer),"oneSecondWarningSent").GetValue(GameTimer.SharedInstance)};
  foreach(var rune in Loadout.RuneSelection??new List<RuneData>())state.runes.Add(new RuneRank{name=rune.name,rank=rune.level});
  var generator=PowerupGenerator.Instance;
  foreach(string field in new[]{"powerupPool","characterPool","devilPool"})
  {
   var values=(List<PowerupPoolItem>)AccessTools.Field(typeof(PowerupGenerator),field).GetValue(generator);var pool=new OfferPool{field=field};
   foreach(var value in values??new List<PowerupPoolItem>())pool.entries.Add(new PoolEntry{key=Key(value.powerup),remaining=value.numTimeRepeatable});state.pools.Add(pool);
  }
  state.taken=((List<Powerup>)AccessTools.Field(typeof(PowerupGenerator),"takenPowerups").GetValue(generator)).Select(Key).ToList();
  state.sourcePowerupCounts=generator.GetComponent<AndroidPowerupCounts>().Capture();
  CaptureSpawners(state);
  var endless=Object.FindObjectOfType<AndroidEndlessRun>();if(endless!=null)
  {state.sourceEndless=endless.Capture();state.roundStart=state.sourceEndless.roundStart;state.roundElapsed=state.seconds-state.roundStart;}
  var challenge=ChallengeRun.Instance;if(challenge!=null)
  {state.challengeEnemies=challenge.EnemyKills;state.challengeBosses=challenge.BossKills;}
  state.exploration=r.GetComponent<ExplorationRun>().State==null?null:r.GetComponent<ExplorationRun>().Capture();
  RuneRewardRun.Instance.Ensure();state.runeRewards=RuneRewardRun.Instance.State;
  state.weaponFamiliarity=p.GetComponent<WeaponFamiliarity>().Rank;
  state.characterWeaponFamiliarity=true;
  state.eliteClaims=EliteRun.Instance.Claimed;state.eliteTiers=EliteRun.Instance.Progress;CaptureDrops(state);Write(state);Plugin.Trace("run-checkpoint-saved",new{state.map,state.seconds,state.level,actions=state.acquired.Count});
 }
 public static void Continue(Snapshot saved)
 {
  var title=Progression.Title;var character=Characters.Items(title.characterMenu).FindIndex(c=>c.data.name==saved.character);
  var weapon=NativeSelection.GunItems(title.gunMenu).FindIndex(g=>g.data.name==saved.weapon);
  var map=Resources.FindObjectsOfTypeAll<MapData>().FirstOrDefault(m=>m.name==saved.map);
  if(character<0||weapon<0||map==null)throw new InvalidOperationException("The checkpoint's character, weapon or scenario is unavailable; it has been preserved.");
  if(!Progression.Allowed(title.characterMenu[character])||!Progression.Allowed(title.gunMenu[weapon])||!Progression.Allowed(map))throw new InvalidOperationException("The checkpoint uses content this save has not obtained; it has been preserved.");
  var runes=Resources.FindObjectsOfTypeAll<RuneData>();var selection=new List<RuneData>();
  if(saved.runes.Any(rank=>rank.rank<1||rank.rank>5||!runes.Any(r=>r.name==rank.name)))throw new InvalidOperationException("The checkpoint's rune rank is unavailable");
  if(saved.custom!=null&&saved.runes.Any(rank=>RuneProgression.Rank(saved.character,rank.name)<rank.rank))throw new InvalidOperationException("The Custom checkpoint uses rune ranks this character has not earned.");
  ReleaseRunAssets();
  foreach(var rank in saved.runes)
  {
   var original=runes.First(r=>r.name==rank.name);
   var copy=Object.Instantiate(original);copy.name=original.name;copy.level=rank.rank;selection.Add(copy);RunLocalRunes.Add(copy);
  }
  Loadout.CharacterSelection=title.characterMenu[character];Loadout.CharacterIndex=character;
  Loadout.GunSelection=title.gunMenu[weapon];Loadout.GunIndex=weapon;Loadout.RuneSelection=selection;Loadout.difficultyLevel=saved.darkness;SelectedMap.MapData=map;
  if(saved.custom!=null)CustomMode.Prepare(saved.custom,map);
  Pending=saved;title.ChangeState<flanne.TitleScreen.WaitToLoadIntoBattleState>();
 }
 public static IEnumerator Restore(GameController game)
 {
  var saved=Pending;var p=PlayerController.Instance;var r=RunRecorder.Instance;
  ResumeRuntime.PrepareCustomBaseHistory(saved);
  // First frame boundary: all scene Start methods, including native loadout and
  // pool initialization, have completed before rebuilding acquired effects.
  yield return null;
  var powers=Powers();var evolutions=Evolutions();
  // Battle-scene dependencies are not loaded on a fresh title screen. Resolve
  // every saved content reference here before applying any saved acquisition.
  foreach(var acquisition in saved.acquired)
   if(acquisition.evolution?!evolutions.ContainsKey(acquisition.key):!powers.ContainsKey(acquisition.key))throw new InvalidOperationException("The checkpoint's upgrade is unavailable: "+acquisition.key);
  if(saved.pools.Count!=3||!saved.pools.Select(pool=>pool.field).OrderBy(f=>f).SequenceEqual(new[]{"characterPool","devilPool","powerupPool"}))throw new InvalidOperationException("The checkpoint's upgrade pool contract is unsupported");
  foreach(var pool in saved.pools)if(pool.entries.Any(e=>!powers.ContainsKey(e.key)))throw new InvalidOperationException("The checkpoint's upgrade pool is unavailable");
  if(saved.taken.Any(key=>!powers.ContainsKey(key)))throw new InvalidOperationException("The checkpoint's collected upgrade is unavailable");
  if(saved.sourcePowerupCounts!=null&&saved.sourcePowerupCounts.Any(item=>!powers.ContainsKey(item.key)))throw new InvalidOperationException("The checkpoint's source upgrade count is unavailable");
  if(saved.custom!=null)foreach(var value in saved.custom.values)
  {
   if(value.key.StartsWith("perk:")&&!saved.acquired.Any(a=>!a.evolution&&value.key=="perk:"+a.key+":scale"))throw new InvalidOperationException("The Custom checkpoint edits an uncollected upgrade.");
   if(value.key.StartsWith("summon:")&&!Resources.FindObjectsOfTypeAll<AttackingSummon>().Where(s=>s is ShootingSummon||s is StabbingSummon).Any(s=>value.key=="summon:"+s.SummonTypeID+":damage"||value.key=="summon:"+s.SummonTypeID+":speed"))throw new InvalidOperationException("The Custom checkpoint edits an unavailable summon type.");
  }
  // Source Restore restores its game clock before powerups and their runtime
  // values. New helper deadlines created during acquisition replay use it too.
  Plugin.SetField(GameTimer.SharedInstance,"<timer>k__BackingField",saved.seconds);
  Plugin.SetField(GameTimer.SharedInstance,"oneSecondWarningSent",saved.oneSecondWarning);
  foreach(var bootstrap in r.Blessings)
  {
   var shield=(Powerup)AccessTools.Field(typeof(BlessingRune),"holyShieldPowerup").GetValue(bootstrap.Owner);
   if(!saved.blessingPending.HasValue&&!saved.acquired.Any(a=>!a.evolution&&a.key==Key(shield)))throw new InvalidOperationException("This older checkpoint omitted its unfinished starting rune state; it has been preserved.");
   if(!bootstrap.Native.MoveNext()||!(bootstrap.Native.Current is WaitForSeconds))throw new InvalidOperationException("Unsupported native Blessing initialization contract");
   if(saved.blessingPending==true)continue;
   if(bootstrap.Native.MoveNext())throw new InvalidOperationException("Unsupported native Blessing completion contract");
  }
  var bootstrapAcquisitions=r.Acquired.Where(a=>!a.evolution).GroupBy(a=>a.key).ToDictionary(g=>g.Key,g=>g.Count());
  r.Recording=false;
  r.Rebuilding=true;
  var groups=saved.acquired.Where(a=>!a.evolution).GroupBy(a=>a.key);
  try{foreach(var group in groups)
  {
   foreach(var acquisition in group)
   {
   if(bootstrapAcquisitions.TryGetValue(acquisition.key,out int count)&&count>0){bootstrapAcquisitions[acquisition.key]=count-1;continue;}
   p.playerPerks.Equip(powers[acquisition.key]);
   }
   ResumeRuntime.RestoreSourcePowerup(p,saved.runtime,group.Key);
  }}finally{r.Rebuilding=false;}
  // Android restores maximum/red HP before assigning soul capacity and
  // invoking its soul-HP setter. Keep the real health-change callbacks.
  var hp=p.playerHealth;
  hp.maxHP=saved.maxHP;hp.hp=saved.hp;hp.maxSHP=saved.maxSoulHP;hp.shp=saved.soulHP;
  p.transform.position=new Vector3(saved.x,saved.y,p.transform.position.z);ResumeRuntime.Restore(p,saved.runtime);
  foreach(var evolution in saved.acquired.Where(a=>a.evolution))p.playerPerks.Equip(evolutions[evolution.key]);
  AndroidAmmo.Restore(p.ammo,saved.ammo);
  Plugin.SetField(game.playerXP,"<level>k__BackingField",saved.level);Plugin.SetField(game.playerXP,"xp",saved.xp);
  game.playerXP.OnLevelChanged.Invoke(saved.level);
  game.playerXP.OnXPToLevelChanged.Invoke((float)AccessTools.Property(typeof(PlayerXP),"xpToLevel").GetValue(game.playerXP));game.playerXP.OnXPChanged.Invoke(saved.xp);
  p.transform.position=new Vector3(saved.x,saved.y,p.transform.position.z);
  Plugin.SetField(game.hitlessDetector,"<hitless>k__BackingField",saved.hitless);Plugin.SetField(ScoreCalculator.SharedInstance,"_enemiesKilled",saved.kills);
  // Earlier checkpoints used Windows' onHurt for both red and soul damage.
  // Their saved hitless flag therefore retains the user's original gate meaning.
  r.UnlockHitless=saved.unlockHitless??saved.hitless;
  Plugin.SetField(game.shootDetector,"<usedShooting>k__BackingField",saved.usedShooting);Plugin.SetField(game.shootDetector,"<usedManualShooting>k__BackingField",saved.usedManualShooting);
  foreach(var pool in saved.pools)
   AccessTools.Field(typeof(PowerupGenerator),pool.field).SetValue(PowerupGenerator.Instance,pool.entries.Select(e=>new PowerupPoolItem{powerup=powers[e.key],numTimeRepeatable=e.remaining}).ToList());
  Plugin.SetField(PowerupGenerator.Instance,"takenPowerups",saved.taken.Select(k=>powers[k]).ToList());
  PowerupGenerator.Instance.GetComponent<AndroidPowerupCounts>().Restore(PowerupGenerator.Instance,saved.sourcePowerupCounts,powers);
  RestoreSpawners(saved);RestoreDrops(saved);
  r.Acquired=saved.acquired.Select(a=>new Acquisition{key=a.key,evolution=a.evolution}).ToList();r.Recording=true;r.LastSaveMinute=Mathf.FloorToInt(saved.seconds/60);
  if(saved.blessingPending==true)foreach(var bootstrap in r.Blessings)r.StartCoroutine(ResumeBootstrapBlessing.Finish(bootstrap,saved.blessingRemaining));
  // Source attachment restoration precedes Unity Start. Growth's explicit
  // source initialization is idempotent when Unity later calls Start;
  // Generator still must not receive the restored ammo notification above.
  ResumeRuntime.StartRestoredAttachments(p);
  ResumeRuntime.EnableSourceStarts();
  game.GetComponent<ExplorationRun>().Restore(saved.exploration);
  game.GetComponent<RuneRewardRun>().Restore(saved.runeRewards);
  Pending=null;StatsOverlay.Instance?.Dirty();Plugin.Trace("run-checkpoint-restored",new{saved.map,saved.seconds,saved.level,saved.hitless});
 }
}

class RunRecorder:MonoBehaviour
{
 public readonly List<MonoBehaviour> SourceStarts=new List<MonoBehaviour>();
 public List<int> CustomRageAmmo=new List<int>(),CustomFusilladeProjectiles=new List<int>();
 public static RunRecorder Instance;
 public bool Recording=true,Finished,Rebuilding,UnlockHitless=true;public int LastSaveMinute=-1,EndlessCycle;public float RoundStart,RoundClock;MapData customMap;
 public bool ModeClearRecorded;
 public bool SourceRecklessSkillUsed;
 public List<RunResume.Acquisition> Acquired=new List<RunResume.Acquisition>();
 public readonly HashSet<int> KilledElites=new HashSet<int>(),KilledBosses=new HashSet<int>();
 public readonly Dictionary<Health,Tuple<bool,int>> Encounters=new Dictionary<Health,Tuple<bool,int>>();
 internal class BlessingBootstrap{public BlessingRune Owner;public IEnumerator Native;public bool Waiting;public float Deadline;}
 public readonly List<BlessingBootstrap> Blessings=new List<BlessingBootstrap>();
 void Awake(){Instance=this;customMap=CustomMode.Active?CustomMode.RunMap:null;this.AddObserver(OnDeath,Health.DeathEvent);}
 void OnDestroy(){this.RemoveObserver(OnDeath,Health.DeathEvent);ResumeRuntime.ForgetRuntime();RunResume.ReleaseRunAssets();if(customMap!=null)CustomMode.End(customMap);if(Instance==this)Instance=null;}
 void OnDeath(object sender,object args)
 {
  // Android clears notifications at scene transitions. The persistent toast
  // canvas cannot own this battle subscription; the run recorder is created
  // after that clear and removed with the actual battle scene.
  Progression.EnemyDeath(sender,args);
  if(sender is Health defeated)EliteRun.Instance.Defeated(defeated);
  if(!BossSpawnAchievementBoundary.Spawning&&sender is Health h&&Encounters.TryGetValue(h,out var encounter)){(encounter.Item1?KilledBosses:KilledElites).Add(encounter.Item2);Encounters.Remove(h);if(encounter.Item1)GetComponent<RuneRewardRun>().BossKilled(encounter.Item2);}
 }
 void Update()
 {if(RunResume.CanSave()){int minute=Mathf.FloorToInt(GameTimer.SharedInstance.timer/60);if(minute!=LastSaveMinute){RunResume.Save();LastSaveMinute=minute;}}}
 void OnApplicationQuit(){RunResume.Save();}
 public IEnumerator SaveAfterNativeStarts(){yield return null;RunResume.Save();}
}
class RunRestoreState:GameState
{
 public override void Enter(){pauseController.Pause();AudioManager.Instance.SetLowPassFilter(true);AudioManager.Instance.PlayMusic(battleMusic);AudioManager.Instance.FadeInMusic(.5f);StartCoroutine(RestoreCR());}
 IEnumerator RestoreCR()
 {
  var routine=RunResume.Restore(owner);
  while(true)
  {
   bool next;object current=null;Exception error=null;
   try{next=routine.MoveNext();if(next)current=routine.Current;}catch(Exception ex){next=false;error=ex;}
   if(error!=null){Plugin.Trace("run-restore-failed",error.ToString());ResumeDialog.RestoreFailed(error);yield break;}
   if(!next)break;yield return current;
  }
  // Continue bypasses the opening animation that grows this scene-authored
  // zero-sized reveal to unit scale. Its children already own VisionRange.
  owner.playerFogRevealer.transform.localScale=Vector3.one;
  hud.Show();pauseController.UnPause();owner.ChangeState<CombatState>();
 }
 public override void Exit(){AudioManager.Instance.SetLowPassFilter(false);}
}
[HarmonyPatch(typeof(GameController),"Awake")]
class ResumeRunLifetime{static void Postfix(GameController __instance){__instance.gameObject.AddComponent<RunRecorder>();__instance.gameObject.AddComponent<ExplorationRun>();__instance.gameObject.AddComponent<ElementalRun>();__instance.gameObject.AddComponent<EliteRun>();__instance.gameObject.AddComponent<RuneRewardRun>();}}
[HarmonyPatch(typeof(GameController),"Start")]
class ResumeInitialState{static bool Prefix(GameController __instance){if(!RunResume.Restoring)return true;__instance.ChangeState<RunRestoreState>();return false;}}
[HarmonyPatch(typeof(PlayerPerks),nameof(PlayerPerks.Equip),typeof(Powerup))]
class RecordResumePowerup{static void Prefix(Powerup perk){var r=RunRecorder.Instance;if(r!=null&&r.Recording)r.Acquired.Add(new RunResume.Acquisition{key=RunResume.Key(perk)});}}
[HarmonyPatch(typeof(PlayerPerks),nameof(PlayerPerks.Equip),typeof(GunEvolution))]
class RecordResumeEvolution{static void Prefix(GunEvolution gunEvo){var r=RunRecorder.Instance;if(r!=null&&r.Recording)r.Acquired.Add(new RunResume.Acquisition{key=RunResume.Key(gunEvo),evolution=true});}}
[HarmonyPatch(typeof(CombatState),nameof(CombatState.Enter))]
class ResumeStableChoice{static void Postfix(){var r=RunRecorder.Instance;if(r!=null)r.StartCoroutine(r.SaveAfterNativeStarts());}}
[HarmonyPatch(typeof(PauseState),nameof(PauseState.Enter))]
class ResumePauseCheckpoint{static void Postfix(){RunResume.Save();}}
[HarmonyPatch]
class ResumeEndOfRun
{
 static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(PlayerDeadState),"Enter"),AccessTools.Method(typeof(KillEnemiesState),"Enter")};
 [HarmonyPrefix]internal static void Finish(){var r=RunRecorder.Instance;if(r!=null)r.Finished=true;RunResume.Invalidate();}
}
[HarmonyPatch(typeof(BlessingRune),"WaitToApplyCR")]
class ResumeBootstrapBlessing
{
 static void Postfix(BlessingRune __instance,ref IEnumerator __result)
 {
  var r=RunRecorder.Instance;var bootstrap=new RunRecorder.BlessingBootstrap{Owner=__instance,Native=__result};r.Blessings.Add(bootstrap);
  __result=RunResume.Restoring?Deferred():Observe(bootstrap);
 }
 static IEnumerator Observe(RunRecorder.BlessingBootstrap bootstrap)
 {
  if(!bootstrap.Native.MoveNext()||!(bootstrap.Native.Current is WaitForSeconds wait))throw new InvalidOperationException("Unsupported native starting rune yield");
  bootstrap.Waiting=true;bootstrap.Deadline=Time.time+(float)AccessTools.Field(typeof(WaitForSeconds),"m_Seconds").GetValue(wait);
  yield return wait;
  if(bootstrap.Native.MoveNext())throw new InvalidOperationException("Unsupported native starting rune completion");bootstrap.Waiting=false;
 }
 internal static IEnumerator Finish(RunRecorder.BlessingBootstrap bootstrap,float remaining)
 {
  bootstrap.Waiting=true;bootstrap.Deadline=Time.time+remaining;yield return new WaitForSeconds(remaining);
  if(bootstrap.Native.MoveNext())throw new InvalidOperationException("Unsupported native restored rune completion");bootstrap.Waiting=false;
 }
 static IEnumerator Deferred(){yield break;}
}
