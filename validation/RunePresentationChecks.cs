using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.RuneSystem;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 Button RuneControl(GameController game,string name)=>game.GetComponentsInChildren<Button>().Single(b=>b.name==name&&b.IsInteractable());
 int RewardActive(string name)=>(int)AccessTools.Method(typeof(RuneRewardRun),"ActiveRank").Invoke(RuneRewardRun.Instance,new object[]{name});
 IEnumerator RuneChoice(GameController game,string name,bool accept,bool controller=true)
 {
  while(!game.GetComponentsInChildren<Button>().Any(b=>b.name=="RuneRewardEnable"&&b.IsInteractable()))yield return null;
  var reward=RuneRewardRun.Instance.State.rewards.Last();var pending=(RuneRewardDraw)AccessTools.Method(typeof(RuneRewardRun),"PendingChoice").Invoke(RuneRewardRun.Instance,new object[]{reward});
  Require(pending.rune==name,"activation is offered once per inactive rune at its last awarded rank: "+name);
  var definition=Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.name==name);
  Require(game.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="RuneResultDescription").text==definition.description,"activation shows the actual rune description");
  var button=RuneControl(game,accept?"RuneRewardEnable":"RuneRewardDecline");
  if(controller){game.playerInput.SwitchCurrentControlScheme("Gamepad",runePad);yield return MenuMove(runePad,button.transform.parent,button);yield return SelectorPad(runePad,GamepadButton.South);}
  else{game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);yield return MenuPointer(button);}
  while(reward.choices==null||!reward.choices.Any(c=>c.rune==name))yield return null;
  Require(reward.choices.Single(c=>c.rune==name).enabled==accept,"actual input saves the requested activation decision");
 }
 IEnumerator RunePresentationImages(GameController game,string prefix)
 {
  bool original=ColorfulStyle.Instance.Enabled;
  foreach(var size in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})
  {
   Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);
   foreach(bool style in new[]{false,true})
   {
    if(ColorfulStyle.Instance.Enabled!=style)ColorfulStyle.Instance.Toggle();Canvas.ForceUpdateCanvases();yield return null;
    var box=Object.FindObjectOfType<RuneConversionVisual>().GetComponent<RectTransform>();var bounds=new Vector3[4];box.GetWorldCorners(bounds);
    Require(bounds.All(c=>c.x>=0&&c.x<=Screen.width&&c.y>=0&&c.y<=Screen.height),"reward panel fits "+size);
    foreach(var rect in box.GetComponentsInChildren<RectTransform>().Where(r=>r.name.StartsWith("RuneSlot")||r.name=="RuneResultName"||r.name=="RuneResultDescription"||r.name=="RuneActivationStatus"||r.name=="RuneRewardEnable"||r.name=="RuneRewardDecline"))
    {var c=new Vector3[4];rect.GetWorldCorners(c);Require(c.All(p=>p.x>=bounds[0].x-.1f&&p.x<=bounds[2].x+.1f&&p.y>=bounds[0].y-.1f&&p.y<=bounds[2].y+.1f),rect.name+" fits opaque popup at "+size);}
    yield return PoiCapture(prefix+"-"+size.x+"-"+(style?"reborn":"original")+".png");
   }
  }
  if(ColorfulStyle.Instance.Enabled!=original)ColorfulStyle.Instance.Toggle();
 }
 IEnumerator CheckRuneFanfare(string mode)
 {
  bool continued=mode.EndsWith("continue"),endless=mode.Contains("endless");
  RunResume.Snapshot input=continued?RunResume.Read():null;
  if(continued)yield return CombatBuffContinue();
  else
  {
   RunResume.Invalidate();if(endless&&!Progression.State.earned.Contains("scenario:Endless_Forest"))Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData",endless?"Endless_Forest":"20M_Forest",8,prepareRun:()=>
   {var rune=Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.name=="VigilanceRuneData");rune.level=2;Loadout.RuneSelection=new List<RuneData>{rune};});
  }
  var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  runePad=InputSystem.AddDevice<Gamepad>("RuneFanfareController");game.playerInput.SwitchCurrentControlScheme("Gamepad",runePad);game.playerInput.neverAutoSwitchControlSchemes=true;
  var run=RuneRewardRun.Instance;
  var startingRune=Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.name=="VigilanceRuneData");
  Require(p.GetComponentsInChildren<Rune>().Any(r=>r.name==startingRune.runePrefab.name+"(Clone)"),"native starting rune component actually exists, including Endless and Continue");
  Log("fixture-precondition","Controlled journaled rune outcomes isolate real reward rendering/input/native Apply/Continue. Boss scheduling/lottery distribution belongs to existing native reward regression. No natural reward-frequency claim.");
  if(continued)
  {
   Require(run.State.rewards.Single().choices.Count==2&&run.State.rewards[0].phase==1,"Continue retains accepted stat/effect choices and unfinished activation popup");
   Require(RewardActive("PowerRuneData")==2&&RewardActive("SeismicRuneData")==1&&RewardActive("AlacrityRuneData")==0,"Continue applies only accepted rewards");
   CombatClose(Bonus(p.stats[StatType.BulletDamage]),.04f,"accepted native Power rank two restored exactly once");
   Require(p.GetComponentsInChildren<SetGameObjectActiveSummon>().Count(s=>s.SummonTypeID=="SeismicWard")==2,"accepted effect rune rebuilt exactly once");
   yield return RunePopup(game);while(!Object.FindObjectOfType<RuneConversionVisual>().Ready)yield return null;
   yield return RuneChoice(game,"AlacrityRuneData",false,false);
  }
  else
  {
   ElementCall(run,"Ensure");Require(RewardActive("VigilanceRuneData")==2,"enabled starting rune loadout applies in Standard and Endless");
   var reward=new RuneBossReward{boss=0,draws=new List<RuneRewardDraw>{
    new RuneRewardDraw{rune="PowerRuneData",tier=1,rank=1},new RuneRewardDraw{rune="PowerRuneData",tier=1,rank=2},
    new RuneRewardDraw{rune="SeismicRuneData",tier=2,rank=1},new RuneRewardDraw{rune="AlacrityRuneData",tier=1,rank=1},
    new RuneRewardDraw{rune="VigilanceRuneData",tier=1,rank=3},new RuneRewardDraw{rune="TenacityRuneData",tier=1,rank=5,capped=true}}};
   run.State.rewards.Add(reward);var profile=RuneProgression.Profile("Shana");
   foreach(var group in reward.draws.GroupBy(d=>d.rune)){profile.ranks.Single(r=>r.name==group.Key).rank=group.Max(d=>d.rank);if(!profile.enabled.Contains(group.Key))profile.enabled.Add(group.Key);}Progression.Save();
   yield return RunePopup(game);var visual=Object.FindObjectOfType<RuneConversionVisual>();float started=Time.unscaledTime;int previous=0;float lastReveal=started;
   while(!visual.Ready)
   {
    if(reward.revealed>previous){Require(Time.unscaledTime-lastReveal>=RuneConversionVisual.RollSeconds-.15f,"each actual slot spins for four seconds before settling");lastReveal=Time.unscaledTime;previous=reward.revealed;}
    if(reward.revealed==reward.draws.Count-1)InputSystem.QueueStateEvent(runePad,new GamepadState().WithButton(GamepadButton.South));
    yield return null;
   }
   Require(Time.unscaledTime-started>=reward.draws.Count*RuneConversionVisual.RollSeconds,"full sequential celebration completes before activation");
   for(int i=0;i<8;i++)yield return null;
   Require(reward.choices==null&&!game.GetComponentsInChildren<Button>().Any(b=>b.name=="RuneRewardEnable"),"held Submit cannot enable a newly opened activation choice");
   InputSystem.QueueStateEvent(runePad,new GamepadState());yield return null;
   while(!game.GetComponentsInChildren<Button>().Any(b=>b.name=="RuneRewardEnable"))yield return null;
   Require(reward.revealed==6&&visual.GetComponentsInChildren<Image>().Any(i=>i.name=="ConvertedXP")&&!game.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("levels of XP")),"all slots reveal before capped runes burn into in-popup native XP");
   yield return RunePresentationImages(game,"rune-activation");
   yield return RuneChoice(game,"PowerRuneData",true);
   CombatClose(Bonus(p.stats[StatType.BulletDamage]),.04f,"duplicate Power awards apply their final rank once");
   yield return RuneChoice(game,"SeismicRuneData",true);
   Require(p.GetComponentsInChildren<SetGameObjectActiveSummon>().Count(s=>s.SummonTypeID=="SeismicWard")==2,"accepted Seismic actually creates the native paired ward actors");
   while(!game.GetComponentsInChildren<Button>().Any(b=>b.name=="RuneRewardEnable"))yield return null;
   RunResume.Save();var saved=RunResume.Read();Require(saved.runeRewards.rewards[0].choices.Count==2&&saved.runes.Any(r=>r.name=="SeismicRuneData"&&r.rank==1),"stable checkpoint includes real new effects and both completed choices");
   File.Copy(RunResume.PathName,Path.Combine(ResultDir,"saved-run.json"),true);
   SetPhase("PASS");Log("complete","Long sequential fanfare, descriptions, capped burn, all layouts/styles, held-input exclusion and actual stat/effect acceptance checkpoint");Application.Quit(0);yield break;
  }
  Require(RewardActive("VigilanceRuneData")==2,"already active rank two remains at its battle-start rank despite a rank-three permanent reward");
  while(!game.GetComponentsInChildren<Button>().Any(b=>b.name=="RuneRewardContinue"))yield return null;
  Require(run.State.rewards[0].choices.Count==3,"duplicates, active and capped runes do not generate extra decisions");
  Require(RewardActive("AlacrityRuneData")==0,"declined rune never mutates battle stats/loadout");
  game.playerInput.SwitchCurrentControlScheme("Gamepad",runePad);var slot=Object.FindObjectOfType<RuneConversionVisual>().Buttons[0];yield return MenuMove(runePad,slot.transform.parent,slot);Require(game.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="RuneResultName").text.Contains("Power"),"controller browses revealed rune descriptions");
  yield return RunePresentationImages(game,"rune-results");
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);yield return MenuPointer(RuneControl(game,"RuneRewardContinue"));while(game.CurrentState is RuneRewardState)yield return null;
  Require(run.State.rewards[0].phase==2&&run.State.xpLevels<=.5f,"Continue commits capped conversion once");
  SetPhase("PASS");Log("complete","Byte-bound fresh-process Continue, accepted effects exactly once, decline, duplicate/active/capped exclusion and controller/pointer descriptions");Application.Quit(0);
 }
 IEnumerator CheckScorePresentation()
 {
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();Plugin.SetField(ScoreCalculator.SharedInstance,"_enemiesKilled",117);yield return null;
  var score=ScoreCalculator.SharedInstance.GetScore();var hud=game.hud.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="RunCredits");Require(hud.text=="Score: "+score.totalScore.ToString("N0"),"HUD uses Score and actual live total");yield return PoiCapture("score-hud.png");
  game.ChangeState<PlayerSurvivedState>();while(!game.retryRunButton.gameObject.activeInHierarchy||!game.retryRunButton.IsInteractable())yield return null;
  var end=Object.FindObjectOfType<flanne.UI.EndScreenUIC>();var total=(TMP_Text)WindField(end,"totalScoreTMP");
  Require(int.Parse(total.text.Replace(",",""))==score.totalScore,"native Total Score value is the completed run total");
  var summary=Resources.FindObjectsOfTypeAll<flanne.UI.TextLocalizerUI>().Single(t=>t.gameObject.scene.IsValid()&&((LocalizedString)WindField(t,"localizedString")).key=="currency_earned_label").GetComponent<TMP_Text>();
  Log("native-score-summary",summary.text+" owner="+summary.transform.parent.name+" localized="+LocalizationSystem.GetLocalizedValue("currency_earned_label"));
  Require(summary.text=="Total Score","actual native result summary localizes to Total Score without obsolete Earned");
  yield return PoiCapture("score-results.png");SetPhase("PASS");Log("complete","Real HUD and native completed-run Total Score label/value, unchanged stored scoring arithmetic");Application.Quit(0);
 }
 IEnumerator CheckScorePresentationAudit()
 {
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest");
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  foreach(var field in game.playerXP.GetType().GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Where(f=>typeof(SoundEffectSO).IsAssignableFrom(f.FieldType)||f.FieldType==typeof(AudioClip)))Log("xp-native-sound",field.Name+":"+field.FieldType.Name+"="+field.GetValue(game.playerXP));
  foreach(var sound in Resources.FindObjectsOfTypeAll<SoundEffectSO>())Log("battle-reward-sound",sound.name);
  foreach(var field in typeof(flanne.UI.EndScreenUIC).GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance))Log("native-result-field",field.Name+":"+field.FieldType.FullName);
  foreach(var method in typeof(flanne.UI.EndScreenUIC).GetMethods(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly))Log("native-result-method",method.Name+"("+string.Join(",",method.GetParameters().Select(p=>p.ParameterType.Name+" "+p.Name))+")");
  game.ChangeState<PlayerSurvivedState>();while(!game.retryRunButton.gameObject.activeInHierarchy||!game.retryRunButton.IsInteractable())yield return null;
  foreach(var label in Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().Where(t=>t.gameObject.scene.IsValid()&&t.gameObject.activeInHierarchy))
  {
   Log("native-result-label",label.name+"="+label.text);
   foreach(var component in label.GetComponents<MonoBehaviour>())foreach(var field in component.GetType().GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Where(f=>f.FieldType==typeof(LocalizedString)))Log("native-result-localized",component.GetType().Name+"/"+field.Name+"="+((LocalizedString)field.GetValue(component)).key);
  }
  yield return Capture("native-results-before.png");SetPhase("PASS");Log("complete","Native results and battle reward audio inventory; diagnostic only");Application.Quit(0);
 }
 IEnumerator CheckRunePresentationAudit()
 {
  var rune=Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.name=="PowerRuneData");
  foreach(var field in typeof(RuneData).GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance))Log("rune-data-field",field.Name+":"+field.FieldType.FullName+"="+field.GetValue(rune));
  foreach(var prop in typeof(RuneData).GetProperties(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Where(p=>p.PropertyType==typeof(string)))Log("rune-data-text",prop.Name+"="+prop.GetValue(rune));
  foreach(var entry in AllRunes(Progression.Title))foreach(var field in entry.GetType().GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).Where(f=>typeof(SoundEffectSO).IsAssignableFrom(f.FieldType)))Log("rune-native-sound",field.Name+"="+((SoundEffectSO)field.GetValue(entry))?.name);
  foreach(var sound in Resources.FindObjectsOfTypeAll<SoundEffectSO>().Where(s=>System.Text.RegularExpressions.Regex.IsMatch(s.name,"rune|power|level|chest|confirm|select",System.Text.RegularExpressions.RegexOptions.IgnoreCase)))Log("available-reward-sound",sound.name);
  foreach(var map in Resources.FindObjectsOfTypeAll<MapData>())Log("map-rune-rule",map.name+" disabled="+map.runesDisabled);
  foreach(var label in Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().Where(t=>t.gameObject.scene.IsValid()&&System.Text.RegularExpressions.Regex.IsMatch(t.text??"","earned|credits|currency|score",System.Text.RegularExpressions.RegexOptions.IgnoreCase)))Log("native-score-label",label.name+"="+label.text);
  SetPhase("PASS");Log("complete","Native rune text, sound and map rule inventory; diagnostic only");Application.Quit(0);yield break;
 }
}
