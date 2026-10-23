using System;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using flanne;
using flanne.UI;
using flanne.TitleScreen;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 static AndroidEndlessState ChallengeSchedule()=>(AndroidEndlessState)AccessTools.Method(typeof(AndroidEndlessRun),"Capture").Invoke(Object.FindObjectOfType<AndroidEndlessRun>(),null);
 static System.Collections.Generic.HashSet<int> DefeatedEncounters(string field)
 {
  var type=typeof(Plugin).Assembly.GetType("TwentyMinutes.Modernization.RunRecorder",true);
  var recorder=AccessTools.Field(type,"Instance").GetValue(null);
  return (System.Collections.Generic.HashSet<int>)AccessTools.Field(type,field).GetValue(recorder);
 }
 public static bool GunBackReady;
 IEnumerator CheckChallenge(TitleScreenController title,bool relaunch)
 {
  SetPhase("native-challenge-entry");
  RunResume.Invalidate();
  Require(Progression.Gates.Count==30&&ChallengeMode.Map!=null,"one native Challenge mode and its explicit progression gate are registered");
  var gate=Progression.Gates.Single(g=>g.Id=="scenario:Challenge_Forest");int points=PointsTracker.pts;
  var originalDiff=SaveSystem.data.difficultyUnlocked;
  if(!relaunch)
  {
   Progression.State.earned.Remove(gate.Id);Progression.State.pending.Remove(gate.Id);Progression.State.notified.Remove(gate.Id);Progression.State.notices.RemoveAll(n=>n.id==gate.Id);Progression.State.challengeDifficultyUnlocked=0;Progression.Save();Progression.Refresh();
   Log("fixture-precondition","Only the isolated profile's Challenge gate/tier/notice is reset for repeatable native gate and quota validation; other ownership is preserved.");
   Require(!Progression.Allowed(gate.Data),"new Challenge is not retroactively unlocked by a legacy profile");
   yield return AchievementRun("Shana","RevolverData","20M_Forest",0);
   var game=Object.FindObjectOfType<GameController>();yield return AchievementWin(game);
   var currentGate=Progression.Gates.Single(g=>g.Id=="scenario:Challenge_Forest");Log("challenge-map-identity",new{registered=currentGate.Data.GetInstanceID(),current=ChallengeMode.Map.GetInstanceID(),earned=Progression.State.earned.Contains(currentGate.Id)});
   Require(currentGate.Data==ChallengeMode.Map&&!Progression.Allowed(ChallengeMode.Map),"Forest Darkness0 clear does not earn Challenge and the title binds the current map identity");
   yield return AchievementRun("Shana","RevolverData","20M_Forest",1);
   game=Object.FindObjectOfType<GameController>();yield return AchievementWin(game);
   Require(Progression.Allowed(ChallengeMode.Map)&&Progression.State.pending.Concat(Progression.State.notified).Contains(gate.Id),"actual native Forest Darkness1 clear earns Challenge with a queued notice");
  }
  else Require(Progression.Allowed(ChallengeMode.Map)&&Progression.State.challengeDifficultyUnlocked==1,"earned scenario and next Challenge tier survive a fresh process");
  title=Object.FindObjectOfType<TitleScreenController>();
  Click(Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  Click(Entries(title.characterMenu)[0].button);while(!Active(title.gunMenu)||title.gunMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  Click(Entries(title.gunMenu)[0].button);while(!(title.CurrentState is ModeSelectState))yield return null;
  Require(title.modeSelectPanel.GetComponent<CanvasGroup>().alpha==1,"native mode dialog is opaque immediately when Show returns");
  ChallengeMode.Toggles(title.modeSelectMenu).Single(t=>t.GetComponent<MapEntry>().data==ChallengeMode.Map).isOn=true;
  yield return Capture("challenge-opening-opaque.png");
  while(title.modeSelectPanel.GetComponentsInChildren<flanne.UIExtensions.ScaleTransition>(true).Any(t=>(t.transform.localScale-Vector3.one).sqrMagnitude>.0000001f))
  {Require(title.modeSelectPanel.GetComponent<CanvasGroup>().alpha==1,"mode dialog stays opaque during its native scale transition");yield return null;}
  var group=ChallengeMode.Toggles(title.modeSelectMenu)[0].transform.parent;
  Log("challenge-layout",new{group=group.name,type=group.GetComponent<LayoutGroup>()?.GetType().Name,rect=((RectTransform)group).rect.ToString(),panel=((RectTransform)title.modeSelectPanel.transform).rect.ToString()});
  Canvas.ForceUpdateCanvases();var bounds=new Vector3[4];((RectTransform)group).GetWorldCorners(bounds);float left=bounds[0].x,right=bounds[2].x;
  foreach(var toggle in ChallengeMode.Toggles(title.modeSelectMenu)){((RectTransform)toggle.transform).GetWorldCorners(bounds);Require(bounds[0].x>=left-.1f&&bounds[2].x<=right+.1f,"native mode fits within the menu row: "+toggle.name);}
  var row=group.GetComponent<HorizontalLayoutGroup>();Require(row.padding.left==24&&row.padding.right==24&&row.spacing==12,"mode row has24-unit side margins and12-unit gaps");
  var children=ChallengeMode.Toggles(title.modeSelectMenu).Select(t=>(RectTransform)t.transform).ToArray();
  for(int i=0;i<children.Length;i++)
  {
   var corners=new Vector3[4];children[i].GetWorldCorners(corners);float localLeft=group.InverseTransformPoint(corners[0]).x,localRight=group.InverseTransformPoint(corners[2]).x;
   var parent=((RectTransform)group).rect;
   Require(localLeft>=parent.xMin+23.9f&&localRight<=parent.xMax-23.9f,"mode button stays visibly inset from both popup edges: "+children[i].name);
   if(i>0){var prior=new Vector3[4];children[i-1].GetWorldCorners(prior);Require(Mathf.Abs(localLeft-group.InverseTransformPoint(prior[2]).x-12)<.1f,"native mode buttons have the measured12-unit gap");}
   Require(Mathf.Abs(children[i].rect.width-(((RectTransform)group).rect.width-48-12*(children.Length-1))/children.Length)<.1f,"every current mode shares the available width while preserving padding/gaps");
  }
  title.difficultyController.RefreshText();
  Require(title.difficultyController.difficulty==(relaunch?1:0),"Challenge has its separate clamped tier selection");
  int normalPreference=PlayerPrefs.GetInt("LastSelectedDifficulty",-1);
  for(int i=0;i<8;i++)title.difficultyController.IncreaseDifficulty();
  Require(title.difficultyController.difficulty==(relaunch?1:0),"native increase cannot bypass the earned Challenge tier");
  Require(PlayerPrefs.GetInt("LastSelectedDifficulty",-1)==normalPreference,"Challenge tier controls preserve the saved normal Darkness preference");
  yield return Capture("challenge-native-menu.png");
  if(relaunch)
  {
   ChallengeMode.Toggles(title.modeSelectMenu)[0].isOn=true;
   Require(title.difficultyController.difficulty<=SaveSystem.data.difficultyUnlocked,"leaving Challenge restores a valid native Darkness selection");
   Click(title.modeSelectBackButton);while(!(title.CurrentState is GunSelectState)||!GunBackReady)yield return null;
   Require(title.modeSelectPanel.GetComponent<CanvasGroup>().alpha==0,"closed mode dialog has no translucent fade-out interval");
   Click(title.loadoutBackButton);while(!(title.CurrentState is CharacterSelectState)||!Active(title.characterMenu))yield return null;
   Click(title.loadoutBackButton);while(!Active(title.mainMenu))yield return null;
   yield return AchievementRun("Shana","RevolverData","20M_Forest");var ordinary=Object.FindObjectOfType<GameController>();
   Require(ChallengeRun.Instance==null&&GameTimer.SharedInstance.timeLimit==1200,"original Forest restart has no Challenge controller or altered timer");
   yield return CheckRevolver(ordinary,PlayerController.Instance,"challenge-original-restart.png");yield return AchievementExit(ordinary,false);
   SetPhase("PASS");Log("complete","Fresh-process Challenge ownership/tier persistence, native tier cap and original-mode restart verified");Application.Quit(0);yield break;
  }
  Click(title.modeSelectStartButton);GameController current;
  while((current=Object.FindObjectOfType<GameController>())==null||!(current.CurrentState is CombatState))yield return null;
  PlayerController.Instance.playerHealth.isInvincible.Flip();GameTimer.SharedInstance.Stop();
  var run=ChallengeRun.Instance;
  Require(run!=null&&run.Tier==0&&ChallengeSchedule().waveTimers.Length==36&&SelectedMap.MapData.bossSpawns.Count==10,"native menu loads the complete recovered Challenge schedule");
  Require(GameTimer.SharedInstance.timeLimit==float.MaxValue&&GameTimer.SharedInstance.endless,"Challenge has no20-minute survival win");
  Require(Mathf.Abs(ChallengeMode.SpawnMultiplier(4)-2.65f)<.00001&&Mathf.Abs(ChallengeMode.AliveMultiplier(4)-4.8384f)<.00001,"recovered five-tier modifiers accumulate in source order");
  AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(GameTimer.SharedInstance,180f);
  Health elite;while((elite=Object.FindObjectsOfType<Health>().FirstOrDefault(h=>h.name=="ElderBrain(Clone)"&&!h.isDead))==null)yield return null;
  Require(elite.maxHP==1000&&ChallengeSchedule().spawnedElites.Contains(5),"actual challenge clock spawns the source elite with native Health and recovered HP");
  elite.TakeDamage(DamageType.Bullet,100000);yield return null;
  Require(run.BossKills==1&&DefeatedEncounters("KilledElites").Contains(5),"source boss classification includes the real elite and records its exact session");
  AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(GameTimer.SharedInstance,300f);
  Health boss;while((boss=Object.FindObjectsOfType<Health>().FirstOrDefault(h=>h.name=="ShubNiggurath(Clone)"&&!h.isDead))==null)yield return null;
  Require(boss.maxHP==4000&&!ChallengeSchedule().pendingBosses.Contains(0),"actual challenge clock creates the first native boss at its recovered HP");
  while(BossArenas().Length==0)yield return null;
  boss.TakeDamage(DamageType.Bullet,100000);yield return null;
  Require(run.BossKills==2&&DefeatedEncounters("KilledBosses").Contains(0),"native boss defeat increments the quota and exact boss index");
  run.enabled=false;Object.FindObjectOfType<AndroidEndlessRun>().enabled=false;
  var brain=ObjectPooler.SharedInstance.GetPooledObject("BrainMonster");brain.transform.position=PlayerController.Instance.transform.position+Vector3.right*10;brain.SetActive(true);
  int before=run.EnemyKills;brain.GetComponent<Health>().TakeDamage(DamageType.Bullet,100000);
  Require(run.EnemyKills==before+1&&run.BossKills==2,"ordinary native enemy death increments only the enemy quota");
  yield return Capture("challenge-native-objectives.png");
  // Explicit boundary preconditions, not a claim that thousands of kills were played.
  run.EnemyKills=4999;run.BossKills=5;
  Require(current.CurrentState is CombatState,"boss target alone cannot complete the enemy quota");
  var last=ObjectPooler.SharedInstance.GetPooledObject("BrainMonster");last.SetActive(true);last.GetComponent<Health>().TakeDamage(DamageType.Bullet,100000);
  Require(run.Completed&&current.CurrentState is KillEnemiesState,"actual final enemy death with both quotas met uses the native win transition");
  while(!(current.CurrentState is PlayerSurvivedState))yield return null;
  Require(Progression.State.challengeDifficultyUnlocked==1&&SaveSystem.data.difficultyUnlocked==originalDiff,"Challenge victory unlocks its next tier without advancing native Darkness");
  yield return Capture("challenge-native-win.png");yield return AchievementExit(current,true);Object.FindObjectOfType<TitleScreenController>().Save();
  SetPhase("PASS");Log("complete","Native Forest gate, recovered Challenge spawns/elite/boss identities, real death quotas, native win and separate tier save verified; elapsed times and final quota boundary explicitly seeded");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(GunSelectState),"Enter")]
class ChallengeGunReadinessReset{static void Prefix(){RuntimeChecks.GunBackReady=false;}}
[HarmonyPatch]
class ChallengeGunReadiness
{
 static System.Reflection.MethodBase TargetMethod()=>AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(GunSelectState),"WaitToShowCR"));
 static void Postfix(bool __result){if(!__result)RuntimeChecks.GunBackReady=true;}
}
