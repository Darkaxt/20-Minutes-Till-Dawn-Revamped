using System;
using System.IO;
using System.Linq;
using System.Collections;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.UI;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator RecordedModeWin(string map,string mode,int difficulty)
 {
  int before=ModeClearRecords.Count(mode,difficulty);
  yield return AchievementRun("Shana","RevolverData",map,darkness:difficulty,prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  Log("fixture-precondition","Controlled native completion boundary for mode-clear accounting: source clock/quota operands, real victory presentation and saved journal; no natural full-run claim.");
  if(mode=="Challenge")
  {
   var challenge=ChallengeRun.Instance;challenge.EnemyKills=ChallengeMode.EnemyTargets[difficulty]-1;challenge.BossKills=ChallengeMode.BossTargets[difficulty];
   HitEffectTarget(maximum:1).AutoKill();
  }
  else{Plugin.SetField(GameTimer.SharedInstance,"<timer>k__BackingField",SelectedMap.MapData.timeLimit);GameTimer.SharedInstance.Start();}
  while(!(game.CurrentState is PlayerSurvivedState)||!game.quitToTitleButton.gameObject.activeInHierarchy||!game.quitToTitleButton.IsInteractable())yield return null;
  Require(ModeClearRecords.Count(mode,difficulty)==before+1,"actual victory increments exactly one "+mode+" count at difficulty "+difficulty);
  AccessTools.Method(typeof(ModeClearRecords),"Record").Invoke(null,null);Require(ModeClearRecords.Count(mode,difficulty)==before+1,"same completed run cannot count twice");
  yield return AchievementExit(game,true);
 }
 IEnumerator CheckModeClearRecords(string mode)
 {
  SetPhase("mode-clear-records");RunResume.Invalidate();
  if(mode=="mode-clear-records")
  {
   foreach(string id in new[]{"scenario:10M_Forest","scenario:Challenge_Forest"})if(!Progression.State.earned.Contains(id))Progression.State.earned.Add(id);Progression.Refresh();
   yield return RecordedModeWin("20M_Forest","Standard",2);yield return RecordedModeWin("20M_Forest","Standard",2);yield return RecordedModeWin("20M_Forest","Standard",3);
   yield return RecordedModeWin("10M_Forest","Quickplay",0);yield return RecordedModeWin("Challenge_Forest","Challenge",0);
   File.Copy(Progression.JournalPath,Path.Combine(ResultDir,"mode-clear-journal.json"));
  }
  else Require(ModeClearRecords.Count("Standard",2)>=2&&ModeClearRecords.Count("Standard",3)>=1&&ModeClearRecords.Count("Quickplay",0)>=1&&ModeClearRecords.Count("Challenge",0)>=1,"per-mode/difficulty counters persist across a fresh process");
  // Earlier UI fixtures deliberately cap unlocked Darkness. The completion
  // operands above are controlled; explicitly make their UI difficulties usable.
  SaveSystem.data.difficultyUnlocked=Mathf.Max(SaveSystem.data.difficultyUnlocked,4);
  var title=Object.FindObjectOfType<TitleScreenController>();var pad=InputSystem.AddDevice<Gamepad>("RevampedModeCrownPad");
  yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);
  while(!Active(title.characterMenu)||!title.characterMenu.interactable)yield return null;
  yield return PadClick(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,Entries(title.characterMenu)[0].button);
  while(!Active(title.gunMenu)||!title.gunMenu.interactable)yield return null;
  yield return PadClick(pad,title.gunMenu.transform,Entries(title.gunMenu)[0].button);while(!(title.CurrentState is ModeSelectState))yield return null;
  while(title.modeSelectMenu.GetComponentsInParent<CanvasGroup>().Any(g=>g.alpha<.99f))yield return null;
  var toggles=ChallengeMode.Toggles(title.modeSelectMenu);var standard=toggles[0];
  yield return MenuMove(pad,title.modeSelectPanel.transform,standard);yield return SelectorPad(pad,GamepadButton.South);
  title.difficultyController.Init(SaveSystem.data.difficultyUnlocked);yield return null;
  foreach(int difficulty in new[]{2,3})
  {
   AccessTools.Method(typeof(DifficultyController),"SetDifficulty").Invoke(title.difficultyController,new object[]{difficulty});yield return null;
   Log("mode-crown-binding","selected="+title.modeSelectMenu.toggledData.name+" difficulty="+title.difficultyController.difficulty+" normal="+ChallengeMode.NormalDifficulty+" expected="+ModeClearRecords.Count("Standard",difficulty));
   var badge=standard.transform.Find("ModeClearCrown");Require(badge.gameObject.activeSelf&&badge.GetComponentInChildren<TMP_Text>().text==ModeClearRecords.Count("Standard",difficulty).ToString(),"native crown updates to selected Darkness "+difficulty);
   Require(badge.GetComponentsInChildren<Image>().Any(i=>NativeSelection.VictoryObjects(title.gunVictories)[0].GetComponentsInChildren<Image>(true).Any(original=>original.sprite==i.sprite)),"mode crown reuses exact native victory artwork");
   var bounds=((RectTransform)standard.transform).rect;foreach(var graphic in badge.GetComponentsInChildren<Graphic>()){var corners=new Vector3[4];graphic.rectTransform.GetWorldCorners(corners);Require(corners.All(p=>bounds.Contains(standard.transform.InverseTransformPoint(p)))&&!graphic.raycastTarget,"crown/count graphics fit inside the button without taking pointer input");}
   yield return Capture("mode-crowns-darkness-"+difficulty+".png");
  }
  var challenge=toggles.Single(t=>t.GetComponent<MapEntry>().data==ChallengeMode.Map);yield return MenuMove(pad,title.modeSelectPanel.transform,challenge);yield return SelectorPad(pad,GamepadButton.South);
  foreach(int tier in new[]{0,1})
  {
   AccessTools.Method(typeof(DifficultyController),"SetDifficulty").Invoke(title.difficultyController,new object[]{tier});yield return null;int actual=title.difficultyController.difficulty,count=ModeClearRecords.Count("Challenge",actual);
   var badge=challenge.transform.Find("ModeClearCrown");Require(badge.gameObject.activeSelf==(count>0)&&badge.GetComponentInChildren<TMP_Text>(true).text==count.ToString(),"Challenge crown tracks its selected tier "+actual);
   yield return Capture("mode-crowns-challenge-"+actual+".png");
  }
  foreach(var toggle in toggles.Where(t=>t.GetComponent<MapEntry>().data.endless||t.GetComponent<MapEntry>().data==CustomMode.MenuMap))Require(toggle.transform.Find("ModeClearCrown")==null||!toggle.transform.Find("ModeClearCrown").gameObject.activeSelf,"Endless/Custom have no completion crown");
  Require(!SelectorText().Contains("No qualifying attempt")&&!SelectorText().Contains("Unlocked "),"mode panel omits attempt and unlocked-tier debug text");
  if(mode!="mode-clear-records")File.Copy(Progression.JournalPath,Path.Combine(ResultDir,"mode-clear-journal.json"));
  SetPhase("PASS");Log("complete","Actual Standard/Quickplay/Challenge wins, per-Darkness repeat accounting, native crown refresh and fresh-process persistence");Application.Quit(0);
 }
}
