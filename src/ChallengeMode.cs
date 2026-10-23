using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using flanne;
using flanne.UI;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

public static class ChallengeMode
{
 public static MapData Map;
 public static readonly int[] EnemyTargets={5000,8000,10000,15000,20000},BossTargets={5,7,9,12,15};
 static readonly float[] AdditionalSpawns={0,.15f,.3f,.5f,.7f},AliveMultipliers={1,1.2f,1.4f,1.6f,1.8f};
 public static bool Active=>SelectedMap.MapData!=null&&SelectedMap.MapData.name=="Challenge_Forest";
 public static bool Selected=>Progression.Title!=null&&Progression.Title.modeSelectMenu.toggledData==Map&&Map!=null;
 public static Toggle[] Toggles(GameModeMenu menu)=>(Toggle[])AccessTools.Field(typeof(ToggleGroupMenu<MapData>),"toggles").GetValue(menu);
 public static float SpawnMultiplier(int tier)=>1+AdditionalSpawns.Take(tier+1).Sum();
 public static float AliveMultiplier(int tier)=>AliveMultipliers.Take(tier+1).Aggregate(1f,(a,b)=>a*b);
 public static void Append(GameModeMenu menu)
 {
  var toggles=Toggles(menu);var existing=toggles.FirstOrDefault(t=>t.GetComponent<MapEntry>().data.name=="Challenge_Forest");
  if(existing!=null){Map=existing.GetComponent<MapEntry>().data;return;}
  if(Map==null)
  {
  var forest=Resources.FindObjectsOfTypeAll<MapData>().First(m=>m.name=="20M_Forest");
  Map=Object.Instantiate(forest);Map.name="Challenge_Forest";Object.DontDestroyOnLoad(Map);
  Map.nameStringID="revamped_challenge_name";Map.descriptionStringID="revamped_challenge_description";
  Plugin.Text["revamped_challenge_name"]="Challenge";
  Plugin.Text["revamped_challenge_description"]="Defeat the enemy and boss quotas. Five tiers; no time limit. Clear a tier to unlock the next.";
  Map.timeLimit=float.MaxValue;Map.endless=false;Map.darknessDisabled=false;Map.achievementsDisabled=false;Map.runesDisabled=false;Map.numPowerupsRepeat=1;
  var prefabs=Resources.FindObjectsOfTypeAll<Health>().Where(h=>!h.gameObject.scene.IsValid()).GroupBy(h=>h.gameObject.name).ToDictionary(g=>g.Key,g=>g.First().gameObject);
  Map.spawnSessions=ChallengeWaves.Hordes(n=>prefabs[n]);Map.bossSpawns=ChallengeWaves.Bosses(n=>prefabs[n]);
  }
  wasChallenge=false;
  var template=toggles.Last();var copy=Object.Instantiate(template.gameObject,template.transform.parent);copy.name="RevampedChallengeMode";
  var added=copy.GetComponent<Toggle>();added.isOn=false;copy.GetComponent<MapEntry>().data=Map;
  var all=toggles.Concat(new[]{added}).ToArray();Plugin.SetField(menu,"toggles",all);
  var row=template.transform.parent.GetComponent<HorizontalLayoutGroup>();
  if(row!=null)
  {
   row.padding.left=row.padding.right=24;row.spacing=12;
   row.childControlWidth=true;row.childForceExpandWidth=true;
   foreach(var toggle in all)
   {var element=toggle.GetComponent<LayoutElement>()??toggle.gameObject.AddComponent<LayoutElement>();element.minWidth=0;element.preferredWidth=1;element.flexibleWidth=1;}
  }
  else if(template.transform.parent.GetComponent<LayoutGroup>()==null)
  {
   // Keep all four options within the same native row.
   float left=((RectTransform)toggles.First().transform).anchoredPosition.x;
   float right=((RectTransform)toggles.Last().transform).anchoredPosition.x;
   for(int i=0;i<all.Length;i++){var rect=(RectTransform)all[i].transform;rect.anchoredPosition=new Vector2(Mathf.Lerp(left,right,i/(float)(all.Length-1)),rect.anchoredPosition.y);rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,Mathf.Abs(right-left)/3f-8);}
  }
  foreach(var t in all)t.onValueChanged.AddListener(_=>RefreshDifficulty());
 }
 static bool wasChallenge;static int normalDifficulty;
 public static void RefreshDifficulty()
 {
  var title=Progression.Title;if(title==null||Progression.State==null)return;
  var c=title.difficultyController;bool challenge=Selected;
  if(challenge&&!wasChallenge)normalDifficulty=c.difficulty;
  if(challenge)AccessTools.Method(typeof(DifficultyController),"SetDifficulty").Invoke(c,new object[]{wasChallenge?c.difficulty:Progression.State.challengeDifficultyUnlocked});
  else if(wasChallenge)AccessTools.Method(typeof(DifficultyController),"SetDifficulty").Invoke(c,new object[]{normalDifficulty});
  wasChallenge=challenge;
 }
 public static bool SetDifficulty(DifficultyController c,int requested)
 {
  if(!Selected||Progression.State==null)return true;
  int tier=Mathf.Clamp(requested,0,Mathf.Clamp(Progression.State.challengeDifficultyUnlocked,0,4));
  Plugin.SetField(c,"<difficulty>k__BackingField",tier);Loadout.difficultyLevel=tier;
  ((TMP_Text)AccessTools.Field(typeof(DifficultyController),"labelTMP").GetValue(c)).text="Challenge "+(tier+1);
  ((TMP_Text)AccessTools.Field(typeof(DifficultyController),"descriptionTMP").GetValue(c)).text=EnemyTargets[tier]+" enemies + "+BossTargets[tier]+" bosses";
  ((TMP_Text)AccessTools.Field(typeof(DifficultyController),"unlockTMP").GetValue(c)).text="Unlocked "+(Progression.State.challengeDifficultyUnlocked+1)+"/5";
  return false;
 }
 public static void Clear()
 {
  int tier=Loadout.difficultyLevel;
  Progression.State.challengeDifficultyUnlocked=Math.Max(Progression.State.challengeDifficultyUnlocked,Math.Min(4,tier+1));Progression.Save();
  Plugin.Trace("challenge-cleared",tier);
 }
}

// The recovered challenge clock drives its waves. Native Windows spawn bodies,
// pools, enemy AI, Health, boss arenas and end-of-run states remain the owners.
public class ChallengeRun:MonoBehaviour
{
 public static ChallengeRun Instance;
 public int EnemyKills,BossKills;public bool Completed;
 public readonly HashSet<int> SpawnedElites=new HashSet<int>(),KilledElites=new HashSet<int>(),SpawnedBosses=new HashSet<int>(),KilledBosses=new HashSet<int>();
 public float RoundStart;public float[] WaveTimers;
 public int Tier;public HordeSpawner Horde;public BossSpawner Boss;
 readonly Queue<int> pending=new Queue<int>();
 readonly Dictionary<Health,Tuple<bool,int>> encounters=new Dictionary<Health,Tuple<bool,int>>();
 TMP_Text counts;GameObject hud;
 static readonly System.Reflection.MethodInfo Spawn=AccessTools.Method(typeof(HordeSpawner),"Spawn"),BossRoutine=AccessTools.Method(typeof(BossSpawner),"WaitToSpawnCR");
 internal static ChallengeRun Spawning;internal static int WaveIndex=-1,BossIndex=-1;
 void Awake(){Instance=this;this.AddObserver(OnDeath,Health.DeathEvent);}
 void OnDestroy(){this.RemoveObserver(OnDeath,Health.DeathEvent);if(Instance==this)Instance=null;}
 public void Initialize(MapInitializer initializer)
 {
  Tier=Mathf.Clamp(Loadout.difficultyLevel,0,4);var map=SelectedMap.MapData;
  Horde=(HordeSpawner)AccessTools.Field(typeof(MapInitializer),"hordeSpawner").GetValue(initializer);
  Boss=(BossSpawner)AccessTools.Field(typeof(MapInitializer),"bossSpawner").GetValue(initializer);
  Horde.LoadSpawners(new List<SpawnSession>());Horde.enabled=false;Boss.LoadSpawners(new List<BossSpawn>());
  foreach(var p in map.spawnSessions.Select(s=>s.monsterPrefab).Distinct())ObjectPooler.SharedInstance.AddObject(p.name,p);
  foreach(var p in map.bossSpawns.Select(s=>s.bossPrefab).Distinct())ObjectPooler.SharedInstance.AddObject(p.name,p,1);
  GameTimer.SharedInstance.timeLimit=float.MaxValue;GameTimer.SharedInstance.endless=true;
  AndroidRuneStartup.Apply();PowerupGenerator.Instance.InitPowerupPool(map.numPowerupsRepeat);AndroidAIRecipes.SpawnMap(map.mapPrefab);
  WaveTimers=new float[map.spawnSessions.Count];
  Plugin.Trace("challenge-start",new{Tier,waves=WaveTimers.Length,bosses=map.bossSpawns.Count,spawn=ChallengeMode.SpawnMultiplier(Tier),alive=ChallengeMode.AliveMultiplier(Tier)});
 }
 internal void Bind(Health health)
 {
  if(health==null)return;
  if(BossIndex>=0)encounters[health]=Tuple.Create(true,BossIndex);
  else if(WaveIndex>=0)encounters[health]=Tuple.Create(false,WaveIndex);
 }
 void OnDeath(object sender,object args)
 {
  var health=sender as Health;var game=Object.FindObjectOfType<GameController>();
  if(health==null||health.GetComponent<PlayerHealth>()!=null||Completed||!(game?.CurrentState is CombatState)||BossSpawnAchievementBoundary.Spawning)return;
  string name=health.name.Replace("(Clone)","").Trim();var map=SelectedMap.MapData;
  bool isBoss=map.bossSpawns.Any(b=>b.bossPrefab.name==name)||map.spawnSessions.Any(w=>w.isElite&&w.monsterPrefab.name==name);
  if(isBoss)BossKills++;else if(health.CompareTag("Enemy"))EnemyKills++;else return;
  if(encounters.TryGetValue(health,out var encounter))
  {if(encounter.Item1)KilledBosses.Add(encounter.Item2);else if(map.spawnSessions[encounter.Item2].isElite)KilledElites.Add(encounter.Item2);encounters.Remove(health);}
  Plugin.Trace("challenge-kills",new{EnemyKills,BossKills});
  CheckComplete(game);
 }
 void CheckComplete(GameController game)
 {
  if(Completed||EnemyKills<ChallengeMode.EnemyTargets[Tier]||BossKills<ChallengeMode.BossTargets[Tier]||!(game.CurrentState is CombatState))return;
  Completed=true;pending.Clear();game.ChangeState<KillEnemiesState>();
 }
 void Update()
 {
  var game=Object.FindObjectOfType<GameController>();if(game==null||WaveTimers==null)return;
  if(hud==null)BuildHUD();if(hud!=null){hud.SetActive(game.CurrentState is CombatState&&!PauseController.isPaused);counts.text=EnemyKills+" / "+ChallengeMode.EnemyTargets[Tier]+"     "+BossKills+" / "+ChallengeMode.BossTargets[Tier];}
  if(Completed||PauseController.isPaused||!(game.CurrentState is CombatState))return;
  CheckComplete(game);if(Completed)return;
  var map=SelectedMap.MapData;float now=GameTimer.SharedInstance.timer;
  float roundLength=map.spawnSessions.Max(s=>s.startTime+s.duration);if(now-RoundStart>=roundLength){RoundStart+=roundLength;Array.Clear(WaveTimers,0,WaveTimers.Length);}
  for(int i=0;i<map.spawnSessions.Count;i++)
  {
   var wave=map.spawnSessions[i];float time=now-RoundStart;
   if(time<wave.startTime||time>=wave.startTime+wave.duration||wave.isElite&&SpawnedElites.Contains(i))continue;
   WaveTimers[i]-=Time.deltaTime;if(WaveTimers[i]>=0)continue;
   int alive=ObjectPooler.SharedInstance.GetAllPooledObjects(wave.monsterPrefab.name).Count(o=>o.activeInHierarchy);
   if(alive<Mathf.FloorToInt(wave.maximum*ChallengeMode.AliveMultiplier(Tier)))
    for(int n=Mathf.FloorToInt(wave.numPerSpawn*ChallengeMode.SpawnMultiplier(Tier));n>0;n--)pending.Enqueue(i);
   WaveTimers[i]+=wave.spawnCooldown;
  }
  // Android CheckSpawn drains at most five entries per frame.
  for(int n=0;n<5&&pending.Count>0;n++)
  {
   int index=pending.Dequeue();var wave=map.spawnSessions[index];Spawning=this;WaveIndex=index;
   try{Spawn.Invoke(Horde,new object[]{wave.monsterPrefab.name,wave.HP,wave.isElite});if(wave.isElite)SpawnedElites.Add(index);}
   finally{Spawning=null;WaveIndex=-1;}
  }
  for(int i=0;i<map.bossSpawns.Count;i++)if(now>=map.bossSpawns[i].timeToSpawn&&SpawnedBosses.Add(i))StartCoroutine(SpawnBoss(i));
 }
 IEnumerator SpawnBoss(int index)
 {
  var b=SelectedMap.MapData.bossSpawns[index];var immediate=new BossSpawn{bossPrefab=b.bossPrefab,maxHP=b.maxHP,spawnPosition=b.spawnPosition,timeToSpawn=0,dontSpawnArena=b.dontSpawnArena,killAllOnSpawn=b.killAllOnSpawn};
  var routine=(IEnumerator)BossRoutine.Invoke(Boss,new object[]{immediate});
  while(true)
  {
   bool next;Spawning=this;BossIndex=index;
   try{next=routine.MoveNext();}finally{Spawning=null;BossIndex=-1;}
   if(!next)yield break;yield return routine.Current;
  }
 }
 void BuildHUD()
 {
  var font=Resources.FindObjectsOfTypeAll<TMP_Text>().FirstOrDefault(t=>t.font!=null&&t.gameObject.scene.IsValid())?.font;if(font==null)return;
  hud=new GameObject("ChallengeObjectives",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));hud.transform.SetParent(transform,false);
  hud.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;hud.GetComponent<Canvas>().sortingOrder=19;
  var scaler=hud.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
  counts=StatsOverlay.Text(hud.transform,"EnemyAndBossQuotas",font,"",20,Vector2.zero,new Vector2(400,30));
  StatsOverlay.Rect(counts.gameObject,hud.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-34),new Vector2(400,30));counts.alignment=TextAlignmentOptions.Center;
  // Native enemy artwork makes the two counters recognizable without a panel.
  var data=SelectedMap.MapData;int j=0;foreach(var prefab in new[]{data.spawnSessions[0].monsterPrefab,data.bossSpawns[0].bossPrefab})
  {var icon=new GameObject(j==0?"EnemyIcon":"BossIcon",typeof(RectTransform),typeof(Image));StatsOverlay.Rect(icon,hud.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(j==0?-216:216,-34),new Vector2(28,28));var image=icon.GetComponent<Image>();image.sprite=prefab.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.sprite!=null).sprite;image.preserveAspect=true;image.raycastTarget=false;j++;}
 }
}
// Mono shares generic method bodies across reference-type instantiations.
// Patching ToggleGroupMenu<MapData>.Start specializes its native body for every
// menu, corrupting PowerupMenu's entries. All scene Awake calls precede Start;
// append through the concrete title owner before any native menu initializes.
[HarmonyPatch(typeof(flanne.TitleScreen.TitleScreenController),"Awake")]
class AppendChallengeBeforeMenu{static void Postfix(flanne.TitleScreen.TitleScreenController __instance){AndroidMapTables.ConfigureLoaded();ChallengeMode.Append(__instance.modeSelectMenu);CustomMode.Append(__instance.modeSelectMenu);}}
[HarmonyPatch(typeof(DifficultyController),"SetDifficulty")]
class ChallengeDifficultySelection{static bool Prefix(DifficultyController __instance,int diff)=>ChallengeMode.SetDifficulty(__instance,diff);}
[HarmonyPatch]
class ChallengeDifficultyButtons
{
 static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(DifficultyController),"IncreaseDifficulty"),AccessTools.Method(typeof(DifficultyController),"DecreaseDifficulty")};
 static bool Prefix(DifficultyController __instance,System.Reflection.MethodBase __originalMethod)
 {if(!ChallengeMode.Selected)return true;ChallengeMode.SetDifficulty(__instance,__instance.difficulty+(__originalMethod.Name=="IncreaseDifficulty"?1:-1));return false;}
}
[HarmonyPatch(typeof(MapInitializer),"Start")]
class ChallengeInitialize{static bool Prefix(MapInitializer __instance){if(AndroidEndlessRun.Active){__instance.gameObject.AddComponent<AndroidEndlessRun>().Initialize(__instance,RunResume.Pending);return false;}if(RunResume.Restoring){RunResume.InitializeMap(__instance);return false;}if(!ChallengeMode.Active)return true;__instance.gameObject.AddComponent<ChallengeRun>().Initialize(__instance);return false;}}
[HarmonyPatch(typeof(DifficultyInitializer),"ApplyDifficulty")]
class ChallengeDifficultyGameplay{static bool Prefix()=>!RunResume.Restoring&&!ChallengeMode.Active;}
[HarmonyPatch(typeof(ObjectPooler),nameof(ObjectPooler.GetPooledObject))]
class ChallengeEncounterIdentity{static void Postfix(GameObject __result){if(ChallengeRun.Spawning!=null&&__result!=null)ChallengeRun.Spawning.Bind(__result.GetComponent<Health>());}}
[HarmonyPatch(typeof(GameTimer),nameof(GameTimer.TimeRemainingToString))]
class ChallengeElapsedClock{static bool Prefix(GameTimer __instance,ref string __result){if(!ChallengeMode.Active)return true;__result=__instance.TimeToString();return false;}}
[HarmonyPatch(typeof(PlayerSurvivedState),"CheckDifficultyUnlock")]
class ChallengeTierClear{static bool Prefix(){if(!ChallengeMode.Active)return true;ChallengeMode.Clear();return false;}}
