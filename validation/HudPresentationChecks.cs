using System;
using System.Collections;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckHudChallenge()
 {
  SetPhase("challenge-hud-spacing");RunResume.Invalidate();
  Progression.State.earned.Add("scenario:Challenge_Forest");Progression.Refresh();
  Log("fixture-precondition","Grant Challenge ownership only in the isolated UI fixture; no natural unlock claim.");
  yield return AchievementRun("Shana","RevolverData","Challenge_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();
  foreach(var size in new[]{new Vector2Int(1024,768),new Vector2Int(1280,720)})
  {
   Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);
   CheckHudBounds(StatsOverlay.Instance,game);
   var quota=Object.FindObjectsOfType<TMP_Text>().Single(t=>t.name=="EnemyAndBossQuotas");
   var objective=game.hud.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="SurviveText");
   Require(HudPoint(quota.rectTransform).y<HudPoint(objective.rectTransform).y-25,"Challenge quotas sit below timer/credits/objective without overlap");
   yield return Capture("challenge-hud-"+size.x+".png");
  }
  SetPhase("PASS");Log("complete","Actual Challenge HUD timer/credits/quotas fit4:3/16:9");Application.Quit(0);
 }
 static string MetricsMode=>((ConfigEntryBase)AccessTools.Field(typeof(Plugin),"HudMode").GetValue(Plugin.Instance)).BoxedValue.ToString();
 IEnumerator CycleMetrics(string expected)
 {
  InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.F2));yield return null;yield return null;
  InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());yield return null;
  Require(MetricsMode==expected,"actual F2 cycles metrics to "+expected);
 }
 static Vector2 HudPoint(RectTransform rect)
 {var c=rect.GetComponentInParent<Canvas>().rootCanvas;return RectTransformUtility.WorldToScreenPoint(c.renderMode==RenderMode.ScreenSpaceOverlay?null:c.worldCamera,rect.TransformPoint(rect.rect.center));}
 void CheckHudBounds(StatsOverlay hud,GameController game)
 {
  Canvas.ForceUpdateCanvases();var viewport=hud.SkillContent.parent.GetComponent<RectTransform>();var corners=new Vector3[4];viewport.GetWorldCorners(corners);
  var canvas=viewport.GetComponentInParent<Canvas>().rootCanvas;
  Require(corners.Select(p=>RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,p)).All(p=>p.x>=0&&p.x<=Screen.width&&p.y>=0&&p.y<=Screen.height),"right selected-skill column fits actual "+Screen.width+"x"+Screen.height);
  var time=game.hud.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Timetext");
  Log("native-timer-layout",time.name+" parent="+time.transform.parent.name+" grandparent="+time.transform.parent.parent.name+" point="+HudPoint(time.rectTransform));
  Require(Mathf.Abs(HudPoint(time.rectTransform).x-Screen.width*.5f)<1,"native timer is centered below the level bar");
  var credits=game.hud.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="RunCredits");
  Require(Mathf.Abs(HudPoint(credits.rectTransform).x-Screen.width*.5f)<1&&HudPoint(credits.rectTransform).y<HudPoint(time.rectTransform).y,"run credits sit below the timer");
 }
 IEnumerator CheckHudPresentation(TitleScreenController title,bool continued)
 {
  SetPhase("hud-modes-and-skills");var pad=InputSystem.AddDevice<Gamepad>("RevampedHudController");
  if(continued)
  {
   Require(MetricsMode=="Little","Little metrics persist into another process");
   yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);yield return null;
   var next=Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy);
   yield return PadClick(pad,next.transform.parent,next);
  }
  else
  {RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<RuneData>());}
  GameController game;while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var player=PlayerController.Instance;var hud=StatsOverlay.Instance;
  while(hud.Panel==null)yield return null;
  var powers=Resources.FindObjectsOfTypeAll<Powerup>();var big=powers.First(p=>p.name=="BigShot");var haste=powers.First(p=>p.name=="Haste");
  if(!continued){player.playerPerks.Equip(big);player.playerPerks.Equip(big);player.playerPerks.Equip(haste);}
  hud.Dirty();yield return null;
  Require(hud.Collected.Values.Single(p=>p.Name==big.nameString).Count==2&&hud.Collected.Values.Single(p=>p.Name==haste.nameString).Count==1,"real acquired and continued skills retain counts");
  Require(hud.SkillIcons.Count==2&&hud.Collected.Values.Any(p=>p.Tier=="Tier 1"),"right column shows real skill icons and tiers");
  Screen.SetResolution(1024,768,false);yield return WaitDisplay(1024,768);CheckHudBounds(hud,game);
  if(!continued)
  {
   StatsOverlay.Enabled=true;hud.Dirty();yield return null;Require(MetricsMode=="Extended"&&hud.Body.text.Contains("("),"legacy enabled HUD retains Extended breakdown");
   yield return CycleMetrics("Off");Require(!hud.Body.transform.parent.gameObject.activeSelf&&hud.SkillContent.gameObject.activeInHierarchy&&hud.Panel.activeSelf,"Off hides only metrics and retains selected skills");
   yield return CycleMetrics("Little");Require(!hud.Body.text.Contains("(")&&!hud.Body.text.Contains(" +"),"Little metrics show only calculated values");
   player.stats[StatType.MoveSpeed].AddMultiplierBonus(-.4f);hud.Dirty();yield return null;
   Require(hud.Body.text.Contains("#A8D5BA")&&hud.Body.text.Contains("#E8A3A8"),"Little values use pastel green above base and pastel red below base");
   yield return Capture("hud-little-4x3.png");
   yield return CycleMetrics("Extended");Require(hud.Body.text.Contains("(")&&hud.Body.text.Contains(" +"),"Extended preserves effective/base/bonus notation");yield return Capture("hud-extended-4x3.png");
   yield return CycleMetrics("Off");yield return Capture("hud-off-with-skills.png");yield return CycleMetrics("Little");
  }
  Screen.SetResolution(1280,720,false);yield return WaitDisplay(1280,720);CheckHudBounds(hud,game);
  int wallet=PointsTracker.pts;Plugin.SetField(ScoreCalculator.SharedInstance,"_enemiesKilled",117);yield return null;
  var score=game.hud.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="RunCredits");
  Require(score.text=="Score: "+ScoreCalculator.SharedInstance.GetScore().totalScore.ToString("N0"),"live Score uses the game's current run score calculation");Require(PointsTracker.pts==wallet,"displaying score never changes the stored balance");
  yield return Capture(continued?"hud-little-continued.png":"hud-little-16x9.png");
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return null;yield return PadClick(pad,game.pauseMenu.transform,game.optionsButton);while(!Active(game.optionsMenu))yield return null;
  var setting=DisplayButton(game.optionsMenu,"StatsSetting");
  if(!continued)
  {
   yield return PadClick(pad,game.optionsMenu.transform,setting);Require(MetricsMode=="Extended","controller selects Extended metrics");
   yield return MenuPointer(setting);Require(MetricsMode=="Off","pointer selects Off metrics");
   yield return PadClick(pad,game.optionsMenu.transform,setting);Require(MetricsMode=="Little","controller selects Little metrics");
  }
  Require(setting.GetComponentInChildren<TMP_Text>().text.Contains("Little"),"native Options shows the selected metrics mode");
  yield return MenuMove(pad,game.optionsMenu.transform,DisplayButton(game.optionsMenu,"ZoomSetting"));yield return SelectorPad(pad,GamepadButton.East);while(!(game.CurrentState is PauseState))yield return null;
  yield return PadClick(pad,game.pauseMenu.transform,game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="ResumeSaveAndReturn"));
  while((title=Object.FindObjectOfType<TitleScreenController>())==null||!Active(title.mainMenu))yield return null;
  Plugin.Instance.Config.Save();SetPhase("PASS");Log("complete","Native three-mode metrics/F2/controller/pointer, independent skills, pastel comparisons, centered timer/run credits, actual4:3/16:9 and source Continue");Application.Quit(0);
 }
}
