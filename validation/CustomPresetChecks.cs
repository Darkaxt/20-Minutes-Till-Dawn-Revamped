using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckPresetContinue(flanne.TitleScreen.TitleScreenController title)
 {
  SetPhase("fresh-preset-continue");
  var expected=RunResume.Read();
  Click(Entries(title.mainMenu)[0].button);yield return null;
  Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
  GameController game;while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))
  {
   var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));
   if(error!=null)throw new InvalidOperationException(error.text);yield return null;
  }
  Require(CustomMode.Active&&CustomMode.Current.map==expected.map&&CustomMode.Current.values.Count==expected.custom.values.Count&&SelectedMap.MapData.achievementsDisabled,"fresh process continues the saved Custom mode and overrides");
  if(expected.blessingPending==true)
  {
   float began=Time.time;Require(PlayerController.Instance.GetComponentsInChildren<PreventDamage>().Length==0,"pending starting rune does not grant a shield during reconstruction");
   while(PlayerController.Instance.GetComponentsInChildren<PreventDamage>().Length==0)yield return null;
   RunResume.Save();var restored=RunResume.Read();
   Require(restored.blessingPending==false&&restored.acquired.Count(a=>!a.evolution&&a.key=="holy_shield_name")==1&&Time.time-began>=expected.blessingRemaining-.02f,"the original starting rune completes once at its saved remaining native delay");
   Require(restored.pools.Single(p=>p.field=="powerupPool").entries.All(e=>e.key!="holy_shield_name"),"delayed starting rune consumes its native finite offer after restoration");
   var shield=restored.runtime.shields.Single();Require(shield.source&&shield.active&&shield.contributes&&shield.count==1&&shield.applyAt==-1&&restored.runtime.protectionFlips==1&&PlayerController.Instance.playerHealth.isProtected,"the resumed native Blessing grants one initialized Android shield owner and saves its counted active protection contract");
  }
  yield return CheckCustomNavigation(game);
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Fresh-process imported preset continuation and native exit verified");Application.Quit(0);
 }
 IEnumerator SaveEarlyCustom()
 {
  RunResume.Invalidate();yield return OpenCustomConfiguration();
  var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();AllRunes(title).Single(r=>r.rune.data.name=="BlessingRuneData").level=1;title.Save();
  var blessing=Object.Instantiate(Resources.FindObjectsOfTypeAll<flanne.RuneSystem.RuneData>().First(r=>r.name=="BlessingRuneData"));blessing.name="BlessingRuneData";blessing.level=1;Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>{blessing};
  Log("fixture-precondition","Isolated native profile seeded with owned rank-one Blessing and a run-local selection to isolate pending startup; no claim of testing its purchase path");Click(CustomButton("CustomStart"));
  GameController game;while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();RunResume.Save();var saved=RunResume.Read();
  Require(saved.blessingPending==true&&saved.blessingRemaining>0&&saved.acquired.All(a=>a.key!="holy_shield_name"),"actual immediate native pause saves the unfinished starting rune and its remaining delay");
  Click(game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="ResumeSaveAndReturn"));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"preset-saved-run.json"));SetPhase("PASS");Log("complete","Immediate native Custom pause/suspend captures the real unfinished Blessing startup");Application.Quit(0);
 }
 IEnumerator CheckCustomNavigation(GameController game)
 {
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="CustomSettings"));
  Log("custom-input-state","cancel-enabled="+game.playerInput.actions.FindAction("UI/Cancel").enabled+" module="+EventSystem.current.currentInputModule.GetType().FullName);
  InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.Escape));yield return null;
  InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());yield return null;
  Require(CustomMenu.Instance==null&&game.CurrentState is PauseState,"actual native keyboard Cancel returns from Custom settings to pause");
  Click(game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="CustomSettings"));
  var pad=InputSystem.AddDevice<Gamepad>("CustomMenuGamepad");game.playerInput.SwitchCurrentControlScheme("Gamepad",pad);
  CustomButton("CustomTab0").Select();
  InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.DpadRight));yield return null;
  InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
  Require(EventSystem.current.currentSelectedGameObject==CustomButton("CustomTab1").gameObject,"actual native gamepad navigation moves between Custom categories");
  InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.South));yield return null;
  InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
  Require(CustomMenu.Instance.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.name.StartsWith("CustomRow")&&t.text=="Base damage"),"actual native gamepad Submit opens the selected category");
  InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.East));yield return null;
  InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
  Require(CustomMenu.Instance==null&&game.CurrentState is PauseState,"actual native gamepad Cancel returns to pause");
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);InputSystem.RemoveDevice(pad);
  Click(game.pauseResumeButton);Require(!PauseController.isPaused,"native Resume after keyboard/controller Custom navigation releases its pause hold");
 }
 IEnumerator CheckCustomPresets()
 {
  RunResume.Invalidate();yield return OpenCustomConfiguration();SetPhase("native-custom-default-presets");
  var challenge=CustomMode.Maps().Single(m=>m.name=="Challenge_Forest");
  Click(CustomButton("CustomLoadDefaults"));Click(CustomButton("CustomAction"+Array.IndexOf(CustomMode.Maps(),challenge)));
  Require(Loadout.difficultyLevel==0&&CustomMenu.Instance.GetComponentsInChildren<TMPro.TMP_InputField>().Single(f=>f.name=="CustomValue1").text=="1","Custom displays Challenge tier1 while preserving native zero-based difficulty");
  CustomValue(1,"2");Require(Loadout.difficultyLevel==1,"displayed Challenge tier2 selects native tier index1");yield return Capture("custom-challenge-defaults.png");
  Click(CustomButton("CustomTab1"));CustomValue(0,"77");
  var quick=CustomMode.Maps().Single(m=>m.name=="10M_Forest");
  Click(CustomButton("CustomLoadDefaults"));Click(CustomButton("CustomAction"+Array.IndexOf(CustomMode.Maps(),quick)));
  Require(CustomMode.Current.map==quick.name&&CustomMode.Current.values.Count==0&&Loadout.difficultyLevel==0,"native Quickplay preset selects its real mode and clears prior Custom overrides");
  yield return Capture("custom-quickplay-defaults.png");Click(CustomButton("CustomStart"));
  GameController game;while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
  Require(SelectedMap.MapData.name==quick.name&&SelectedMap.MapData.timeLimit==quick.timeLimit&&SelectedMap.MapData.runesDisabled==quick.runesDisabled&&SelectedMap.MapData.numPowerupsRepeat==quick.numPowerupsRepeat&&GameTimer.SharedInstance.timeLimit==quick.timeLimit,"preset starts the actual Quickplay timer, rune policy and native repeat limit");
  GameTimer.SharedInstance.Stop();CustomMode.Set("weapon:damage",77);CustomMode.Set("stat:BulletDamage",3);float nativeDamage=Loadout.GunSelection.damage;
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="CustomSettings"));
  Click(CustomButton("CustomLoadDefaults"));Click(CustomButton("CustomAction"+Array.IndexOf(CustomMode.Maps(),quick)));
  Require(CustomMode.Current.values.Count==0&&PlayerController.Instance.gun.gunData.damage==nativeDamage&&SelectedMap.MapData.name==quick.name,"loading current-mode defaults removes live overrides without changing mode/build/clock");
  var endless=CustomMode.Maps().Single(m=>m.name=="Endless_Forest");int credits=PointsTracker.pts;
  Click(CustomButton("CustomLoadDefaults"));Click(CustomButton("CustomAction"+Array.IndexOf(CustomMode.Maps(),endless)));
  Require(SelectedMap.MapData.name==quick.name&&CustomMode.Current.map==quick.name,"choosing another preset leaves the current run unchanged until explicit restart");
  yield return Capture("custom-preset-restart-choice.png");Click(CustomButton("CustomAction1"));
  Require(CustomMode.Current.map==quick.name,"keeping the current run cancels a mode switch without changes");
  Click(CustomButton("CustomLoadDefaults"));Click(CustomButton("CustomAction"+Array.IndexOf(CustomMode.Maps(),endless)));Click(CustomButton("CustomAction0"));
  var previous=game;while((game=Object.FindObjectOfType<GameController>())==null||game==previous||!(game.CurrentState is CombatState))yield return null;
  GameTimer.SharedInstance.Stop();
  Require(CustomMode.Active&&SelectedMap.MapData.name==endless.name&&SelectedMap.MapData.endless&&SelectedMap.MapData.spawnSessions.Count==endless.spawnSessions.Count&&SelectedMap.MapData.endlessSpawnSessions.Count==endless.endlessSpawnSessions.Count&&CustomMode.Current.values.Count==0&&Loadout.difficultyLevel==0,"explicit preset restart starts a fresh native Endless Custom run with its original schedules");
  Require(PointsTracker.pts>=credits&&SaveSystem.data.points==PointsTracker.pts,"preset restart retains the prior run's native credits");
  Require(Ownership()==File.ReadAllText(Path.Combine(ResultDir,"custom-ownership.txt")),"preset imports/restart preserve all ownership, rune purchases and achievement records");
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();
  var save=game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="ResumeSaveAndReturn");
  while(!save.IsInteractable())yield return null;
  Click(save);while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  var checkpoint=RunResume.Read();Require(checkpoint.custom!=null&&checkpoint.map==endless.name&&checkpoint.custom.values.Count==0,"an imported native preset is saved in the normal Custom checkpoint");
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"preset-saved-run.json"));RunResume.Invalidate();
  SetPhase("PASS");Log("complete","Native owned-mode defaults, actual Quickplay start, live reset, cancelled/explicit Endless restart, retained credits and checkpoint payload verified");Application.Quit(0);
 }
}
