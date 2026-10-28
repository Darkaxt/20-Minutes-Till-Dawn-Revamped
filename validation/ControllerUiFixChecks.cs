using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using flanne.UI;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator PadClick(Gamepad pad,Transform root,Button button)
 {yield return MenuMove(pad,root,button);yield return SelectorPad(pad,GamepadButton.South);}
 IEnumerator MenuPointer(Selectable control)
 {
  Canvas.ForceUpdateCanvases();var rect=control.GetComponent<RectTransform>();var canvas=control.GetComponentInParent<Canvas>().rootCanvas;
  Vector2 point=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
  var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=point},hits);
  Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Selectable>()==control,"actual pointer raycast reaches "+control.name);
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=point});yield return null;yield return null;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=point}.WithButton(MouseButton.Left));yield return null;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=point});yield return null;yield return null;
 }
 IEnumerator TypeCustomValue(TMP_InputField input,string value)
 {
  yield return MenuPointer(input);
  Require(input.isFocused,"actual pointer starts numeric editing");
  // This shipped TMP uses legacy Unity key events, not InputSystem text input.
  // Feed its public event processor, then commit via an actual pointer deselect.
  input.ProcessEvent(Event.KeyboardEvent("^a"));
  foreach(char c in value)input.ProcessEvent(new Event{type=EventType.KeyDown,character=c});
  Log("native-tmp-edit",input.name+" text="+input.text);
  yield return MenuPointer(input.navigation.selectOnRight);
 }
 IEnumerator CheckNativeOptions(Gamepad pad,flanne.UIExtensions.Menu menu,string label)
 {
  while(!Active(menu)||menu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  var buttons=menu.GetComponentsInChildren<Button>(true);var stats=buttons.Single(b=>b.name=="StatsSetting");var style=buttons.Single(b=>b.name=="ColorfulSetting");
  Require(!stats.GetComponentInChildren<TMP_Text>().text.Contains("F2"),"native Metrics setting has no shortcut suffix: "+label);
  var backdrop=menu.transform.Find("NativeOptionsOpaquePanel").GetComponent<Image>();
  Require(backdrop.color.a==1&&menu.GetComponent<CanvasGroup>().alpha==1,"Options has an opaque native panel immediately on Show: "+label);
  Require(!Resources.FindObjectsOfTypeAll<Canvas>().Any(c=>c.name=="EnhancementSettingsCanvas"),"floating enhancement canvas is absent");
  Require(Entries(menu).Any(e=>e.button==stats)&&Entries(menu).Any(e=>e.button==style),"both enhancements are registered native Options entries");
  var original=buttons.Single(b=>b.name=="Outline").GetComponentInChildren<TMP_Text>(true);
  Require(new[]{stats,style}.All(b=>b.GetComponentInChildren<TMP_Text>(true).font==original.font&&b.GetComponentInChildren<TMP_Text>(true).fontSize==original.fontSize),"settings retain native typography");
  string outline=original.text;
  foreach(var button in buttons.Where(b=>b.IsInteractable()))yield return MenuMove(pad,menu.transform,button);
  var before=StatsOverlay.Mode;yield return PadClick(pad,menu.transform,stats);
  Require(StatsOverlay.Mode==(MetricsView)(((int)before+1)%3)&&stats.GetComponentInChildren<TMP_Text>().text.Contains(StatsOverlay.Mode.ToString()),"one controller Submit cycles metrics once: "+label);
  bool art=ColorfulStyle.Instance.Enabled;yield return PadClick(pad,menu.transform,style);
  Require(ColorfulStyle.Instance.Enabled!=art&&ColorfulStyle.Instance.LastRestoreFailures==0,"controller changes artwork with exact restoration: "+label);
  yield return MenuPointer(stats);Require(StatsOverlay.Mode==(MetricsView)(((int)before+2)%3),"actual pointer cycles metrics once");
  yield return MenuPointer(style);Require(ColorfulStyle.Instance.Enabled==art,"actual pointer toggles artwork once");
  yield return PadClick(pad,menu.transform,stats);Require(StatsOverlay.Mode==before,"three native metric activations return to the original mode");
  Require(original.text==outline,"enhancement activation does not invoke cloned Outline command");
  if(!StatsOverlay.Enabled)yield return PadClick(pad,menu.transform,stats);
  if(!ColorfulStyle.Instance.Enabled)yield return PadClick(pad,menu.transform,style);
  foreach(var button in buttons.Where(b=>b.IsInteractable()))yield return MenuMove(pad,menu.transform,button);
  MenuSnapshot(menu.transform,label);yield return Capture(label+".png");
  Canvas.ForceUpdateCanvases();var camera=backdrop.GetComponentInParent<Canvas>().rootCanvas;var corners=new Vector3[4];backdrop.rectTransform.GetWorldCorners(corners);
  var min=RectTransformUtility.WorldToScreenPoint(camera.renderMode==RenderMode.ScreenSpaceOverlay?null:camera.worldCamera,corners[0]);var max=RectTransformUtility.WorldToScreenPoint(camera.renderMode==RenderMode.ScreenSpaceOverlay?null:camera.worldCamera,corners[2]);
  Require(min.x>=0&&min.y>=0&&max.x<=Screen.width&&max.y<=Screen.height,"opaque Options panel fits actual viewport: "+Screen.width+"x"+Screen.height);
  foreach(var button in buttons)
  {
   // Native labels have100-unit layout boxes around24-unit rows. Check the
   // actual hit rectangle and rendered glyphs, rather than that empty padding.
   var points=new Vector3[4];button.GetComponent<RectTransform>().GetWorldCorners(points);
   Require(points.All(point=>backdrop.rectTransform.rect.Contains(backdrop.transform.InverseTransformPoint(point))),"native Options hit rectangle fits opaque panel: "+button.name);
   var text=button.GetComponentInChildren<TMP_Text>();text.ForceMeshUpdate();
   Require(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(c=>c.isVisible).All(c=>backdrop.rectTransform.rect.Contains(backdrop.transform.InverseTransformPoint(text.transform.TransformPoint(c.bottomLeft)))&&backdrop.rectTransform.rect.Contains(backdrop.transform.InverseTransformPoint(text.transform.TransformPoint(c.topRight)))),"rendered native option glyphs fit opaque panel: "+button.name);
  }
  // Leave from an added row, exercising the native Menu Cancel registration.
  yield return MenuMove(pad,menu.transform,stats);yield return SelectorPad(pad,GamepadButton.East);
 }
 IEnumerator CheckControllerUiFixed(TitleScreenController title,bool relaunch)
 {
  SetPhase("controller-ui-complete-workflows");var pad=InputSystem.AddDevice<Gamepad>("RevampedCorrectedUiController");
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
  while(title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  MenuSnapshot(title.mainMenu.transform,"controller-ui-initial");MenuSnapshot(title.optionsMenu.transform,"controller-ui-initial-options");
  Log("options-native-owners","main="+title.mainMenu.name+" entries="+string.Join(",",Entries(title.mainMenu).Select(e=>e.name))+"; options="+title.optionsMenu.name+" entries="+string.Join(",",Entries(title.optionsMenu).Select(e=>e.name)));
  File.WriteAllLines(Path.Combine(ResultDir,"initial-canvas-groups.txt"),Resources.FindObjectsOfTypeAll<CanvasGroup>().Where(g=>g.gameObject.scene.IsValid()).Select(g=>g.name+" parent="+g.transform.parent?.name+" alpha="+g.alpha+" interactable="+g.interactable+" blocks="+g.blocksRaycasts));
  if(relaunch)Require(StatsOverlay.Enabled&&ColorfulStyle.Instance.Enabled,"both native settings persisted into another process");
  for(int entry=0;entry<2;entry++)
  {
   var options=Entries(title.mainMenu).Single(e=>e.button.GetComponentInChildren<TMP_Text>().text=="Options").button;
   yield return PadClick(pad,title.mainMenu.transform,options);yield return CheckNativeOptions(pad,title.optionsMenu,"native-title-options-"+entry);
   while(!Active(title.mainMenu))yield return null;
  }
  if(relaunch){SetPhase("PASS");Log("complete","Fresh-process setting persistence, native Options re-entry/controller/pointer/Cancel and opacity verified");Application.Quit(0);yield break;}
  var achievements=Entries(title.mainMenu).Single(e=>e.name=="RevampedAchievements").button;
  yield return PadClick(pad,title.mainMenu.transform,achievements);var achievementView=Object.FindObjectOfType<SourceAchievementMenu>();
  Require(achievementView!=null,"Achievements controller entry remains working");
  var next=achievementView.GetComponentsInChildren<Button>().Single(b=>b.name=="AchievementNext");yield return PadClick(pad,achievementView.transform,next);yield return SelectorPad(pad,GamepadButton.East);
  RunResume.Invalidate();
  yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);
  while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  RequireCharacterPreview(title,0);
  yield return MenuMove(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,title.runesButton);yield return SelectorPad(pad,GamepadButton.South);
  while(!(title.CurrentState is RuneMenuCharacterState))yield return null;
  foreach(var rune in AllRunes(title))yield return MenuMove(pad,title.runeMenuPanel.transform,(Button)AccessTools.Field(rune.GetType(),"button").GetValue(rune));
  yield return PadClick(pad,title.runeMenuPanel.transform,title.runeConfirmButton);
  Log("rune-controller-return",title.CurrentState.GetType().Name+" focus="+SelectorFocus());
  while(!(title.CurrentState is CharacterSelectState)||!title.characterMenu.interactable)yield return null;
  yield return PadClick(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,Entries(title.characterMenu)[0].button);
  while(!Active(title.gunMenu)||title.gunMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  yield return PadClick(pad,title.gunMenu.transform,Entries(title.gunMenu)[0].button);
  while(!(title.CurrentState is ModeSelectState))yield return null;
  var toggles=ChallengeMode.Toggles(title.modeSelectMenu);int credits=PointsTracker.pts;
  var challengeGate=Progression.Gates.Single(g=>g.Data==ChallengeMode.Map);Progression.State.earned.RemoveAll(id=>id==challengeGate.Id);
  var lockedGates=Progression.Gates.Where(g=>g.Data is MapData&&toggles.Any(t=>t.GetComponent<MapEntry>().data==g.Data)).ToArray();
  var earnedModes=lockedGates.Where(g=>Progression.State.earned.Contains(g.Id)).Select(g=>g.Id).ToArray();
  foreach(var gate in lockedGates)Progression.State.earned.RemoveAll(id=>id==gate.Id);Progression.Refresh();
  var nativeLock=(Image)AccessTools.Field(typeof(Unlockable),"lockSprite").GetValue(NativeSelection.Unlocks(title.characterUnlocker).First());
  foreach(var toggle in toggles){bool locked=!Progression.Allowed(toggle.GetComponent<MapEntry>().data);Require(toggle.GetComponent<ModeLockVisual>().Locked==locked,"campaign lock artwork reflects actual ownership: "+toggle.name);if(locked)Require(toggle.transform.Find("NativeModeLock").GetComponent<Image>().sprite==nativeLock.sprite,"campaign uses exact native character/weapon lock artwork");}
  foreach(var toggle in toggles)
  {
   yield return MenuMove(pad,title.modeSelectPanel.transform,toggle);yield return SelectorPad(pad,GamepadButton.South);
   Require(title.modeSelectMenu.toggledData==toggle.GetComponent<MapEntry>().data,"actual controller selects campaign "+toggle.name);
   Require(SelectorText().Contains(title.modeSelectMenu.toggledData.description),"selected campaign shows its concise description or unlock condition");
  }
  yield return MenuMove(pad,title.modeSelectPanel.transform,toggles.Single(t=>t.GetComponent<MapEntry>().data==ChallengeMode.Map));yield return SelectorPad(pad,GamepadButton.South);
  yield return PadClick(pad,title.modeSelectPanel.transform,title.modeSelectStartButton);
  Require(title.CurrentState is ModeSelectState&&PointsTracker.pts==credits,"controller cannot start unearned Challenge or spend credits");
  yield return Capture("campaign-native-locks.png");
  foreach(string id in earnedModes)Progression.State.earned.Add(id);
  Progression.State.earned.Add(challengeGate.Id);Progression.State.challengeDifficultyUnlocked=1;
  Progression.Refresh();Require(!toggles.Single(t=>t.GetComponent<MapEntry>().data==ChallengeMode.Map).GetComponent<ModeLockVisual>().Locked,"earned campaign clears its lock artwork immediately");
  yield return MenuMove(pad,title.modeSelectPanel.transform,title.difficultyController.GetComponent<Selectable>());yield return SelectorPad(pad,GamepadButton.DpadRight);
  Require(title.difficultyController.difficulty==1,"controller Right selects the earned next Challenge tier");
  yield return SelectorPad(pad,GamepadButton.DpadRight);Require(title.difficultyController.difficulty==1,"controller cannot exceed earned Challenge tier");
  yield return SelectorPad(pad,GamepadButton.DpadLeft);Require(title.difficultyController.difficulty==0,"controller Left returns to the first Challenge tier");
  yield return PadClick(pad,title.modeSelectPanel.transform,title.modeSelectBackButton);while(!(title.CurrentState is GunSelectState)||!GunBackReady)yield return null;
  yield return PadClick(pad,title.gunMenu.GetComponentInParent<Canvas>().rootCanvas.transform,Entries(title.gunMenu)[0].button);while(!(title.CurrentState is ModeSelectState))yield return null;
  yield return MenuMove(pad,title.modeSelectPanel.transform,ChallengeMode.Toggles(title.modeSelectMenu).Single(t=>t.GetComponent<MapEntry>().data==CustomMode.MenuMap));yield return SelectorPad(pad,GamepadButton.South);
  yield return Capture("campaign-controller-custom-selected.png");
  yield return PadClick(pad,title.modeSelectPanel.transform,title.modeSelectStartButton);while(CustomMenu.Instance==null)yield return null;
  string ownership=Ownership();
  for(int tab=0;tab<7;tab++)
  {
   yield return PadClick(pad,CustomMenu.Instance.transform,CustomButton("CustomTab"+tab));
   var rows=(IList)AccessTools.Field(typeof(CustomMenu),"rows").GetValue(CustomMenu.Instance);int pages=Math.Max(1,(rows.Count+7)/8);
   for(int page=0;page<pages;page++)
   {
    var view=CustomMenu.Instance;
    foreach(var button in view.GetComponentsInChildren<Button>().Where(b=>b.IsInteractable()).ToArray())yield return MenuMove(pad,view.transform,button);
    Require(!(EventSystem.current.currentSelectedGameObject.GetComponent<TMP_InputField>() is TMP_InputField),"controller ring excludes text editing on Custom tab "+tab+" page "+page);
    if(page+1<pages)yield return PadClick(pad,view.transform,CustomButton("CustomNext"));
   }
  }
  yield return PadClick(pad,CustomMenu.Instance.transform,CustomButton("CustomTab1"));var plus=CustomButton("CustomPlus0");
  float damage=CustomMode.Get("weapon:damage",Loadout.GunSelection.damage);yield return PadClick(pad,CustomMenu.Instance.transform,plus);
  Require(CustomMode.Get("weapon:damage")==damage+.5f,"controller Plus changes weapon damage");
  yield return PadClick(pad,CustomMenu.Instance.transform,CustomButton("CustomMinus0"));Require(CustomMode.Get("weapon:damage")==damage,"controller Minus restores weapon damage");
  var input=CustomMenu.Instance.GetComponentsInChildren<TMP_InputField>().Single(f=>f.name=="CustomValue0");yield return TypeCustomValue(input,"21");
  Log("native-tmp-commit",CustomMode.Get("weapon:damage"));
  Require(CustomMode.Get("weapon:damage")==21.5f,"native TMP entry commits21 before the actual pointer Plus adds0.5");
  yield return MenuPointer(input);yield return SelectorPad(pad,GamepadButton.East);
  Require(CustomMenu.Instance!=null&&SelectorFocus()=="CustomPlus0"&&!input.isFocused,"Cancel exits a pointer edit to Plus without closing Custom");
  Require(Ownership()==ownership&&PointsTracker.pts==credits,"Custom browsing/value edits preserve content ownership and credits");
  yield return Capture("custom-controller-values.png");
  yield return SelectorPad(pad,GamepadButton.East);Require(CustomMenu.Instance==null&&title.CurrentState is ModeSelectState,"second Cancel returns from Custom to mode selection");
  yield return PadClick(pad,title.modeSelectPanel.transform,title.modeSelectStartButton);while(CustomMenu.Instance==null)yield return null;
  yield return PadClick(pad,CustomMenu.Instance.transform,CustomButton("CustomStart"));GameController game;
  while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
  Require(Loadout.RuneSelection.All(r=>r.level>0&&r.level==RuneProgression.Rank(Loadout.CharacterSelection.name,r.name)),"actual battle rune clones retain their character-owned ranks");
  GameTimer.SharedInstance.Stop();StopFixtureSpawning();PlayerController.Instance.playerHealth.isInvincible.Flip();
  for(int entry=0;entry<2;entry++)
  {
   PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return null;
   yield return PadClick(pad,game.pauseMenu.transform,game.optionsButton);yield return CheckNativeOptions(pad,game.optionsMenu,"native-pause-options-"+entry);
   while(!(game.CurrentState is PauseState))yield return null;
   yield return PadClick(pad,game.pauseMenu.transform,game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;
  }
  Require(StatsOverlay.Enabled&&ColorfulStyle.Instance.Enabled,"both settings remain enabled through native Options");
  yield return null;Require(StatsOverlay.Instance.Panel.activeSelf&&ColorfulStyle.Instance.AppliedCount>0,"native settings control the live HUD and artwork");
  Plugin.Instance.Config.Save();File.Copy(Plugin.Instance.Config.ConfigFilePath,Path.Combine(ResultDir,"saved-native-settings.cfg"));
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return null;
  yield return PadClick(pad,game.pauseMenu.transform,game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="ResumeSaveAndReturn"));
  while((title=Object.FindObjectOfType<TitleScreenController>())==null||!Active(title.mainMenu))yield return null;
  byte[] checkpoint=File.ReadAllBytes(RunCheckpoint);File.WriteAllBytes(Path.Combine(ResultDir,"controller-checkpoint.json"),checkpoint);
  for(int attempt=0;attempt<2;attempt++)
  {
   yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);
   var dialogType=AccessTools.TypeByName("TwentyMinutes.Modernization.ResumeDialog");var dialog=(Component)AccessTools.Field(dialogType,"Instance").GetValue(null);
   Require(dialog!=null,"ordinary Continue prompt opens with controller");
   if(attempt==0)
   {
    yield return SelectorPad(pad,GamepadButton.East);Require(title.mainMenu.interactable,"ordinary Continue Cancel returns to title");
    yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);dialog=(Component)AccessTools.Field(dialogType,"Instance").GetValue(null);
   }
   ControllerRestoreRejection.Enabled=true;
   yield return PadClick(pad,dialog.transform,(Button)AccessTools.Field(dialogType,"ContinueButton").GetValue(dialog));
   Component failure;
   while((failure=(Component)AccessTools.Field(dialogType,"Instance").GetValue(null))==null||!failure.GetComponentInChildren<TMP_Text>().text.Contains("Could not restore"))yield return null;
   var failureCancel=(InputAction)AccessTools.Field(dialogType,"cancel").GetValue(failure);
   Log("restore-error-controller-map",Object.FindObjectOfType<GameController>().playerInput.currentActionMap?.name+" cancelEnabled="+(failureCancel!=null&&failureCancel.enabled));
   Require(failureCancel!=null&&failureCancel.enabled,"failed Continue owns an enabled native Cancel action");
   Require(SelectorFocus()=="ResumeBack","failed Continue focuses its usable Back button");yield return Capture("continue-controller-rejection-"+attempt+".png");
   yield return SelectorPad(pad,attempt==0?GamepadButton.East:GamepadButton.South);
   while((title=Object.FindObjectOfType<TitleScreenController>())==null||!Active(title.mainMenu))yield return null;
   Require(File.ReadAllBytes(RunCheckpoint).SequenceEqual(checkpoint),"failed Continue "+(attempt==0?"Cancel":"Back Submit")+" preserves checkpoint bytes");
  }
  ControllerRestoreRejection.Enabled=false;SetPhase("PASS");Log("complete","Complete native controller workflows, opaque Options/controller/pointer/re-entry, all Custom tabs/pages/value paths, runes, locked modes, save/Continue error Cancel/Back and unchanged checkpoint");Application.Quit(0);
 }
}
[HarmonyPatch]
class ControllerRestoreRejection
{
 internal static bool Enabled;
 static System.Reflection.MethodBase TargetMethod()=>AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.RunResume"),"Restore");
 static void Postfix(ref IEnumerator __result){if(Enabled)__result=Reject();}
 static IEnumerator Reject(){yield return null;throw new InvalidOperationException("Controlled validation-only restore rejection");}
}
