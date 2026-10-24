using System;
using System.IO;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using TMPro;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

class ResumeDialog:MonoBehaviour
{
 public static ResumeDialog Instance;
 public GameObject Root;public TMP_Text Description;public Button ContinueButton,NewButton,BackButton;
 TitleScreenController title;Action back;InputAction cancel;
 public static void Show(TitleMainMenuState state,RunResume.Snapshot saved,string error)
 {
  var title=Progression.Title;var dialog=Build(title.transform,title.modeSelectStartButton.GetComponentInChildren<TMP_Text>(true).font);
  dialog.title=title;title.mainMenu.interactable=false;
  dialog.Description.text=error??"Continue your "+(saved.map=="Challenge_Forest"?"Challenge":"run")+" at "+Mathf.FloorToInt(saved.seconds/60)+":"+Mathf.FloorToInt(saved.seconds%60).ToString("00")+"?";
  dialog.ContinueButton.interactable=saved!=null;
  dialog.back=()=>{title.mainMenu.interactable=true;NativeSelection.GunEntriesFor(title.mainMenu)[0].button.Select();Object.Destroy(dialog.gameObject);};
  dialog.BackButton.onClick.AddListener(()=>dialog.back());
  dialog.NewButton.onClick.AddListener(()=>{RunResume.Invalidate();dialog.back();state.OnClick(0);});
  dialog.ContinueButton.onClick.AddListener(()=>
  {
   try{RunResume.Continue(saved);Object.Destroy(dialog.gameObject);}
   catch(Exception ex){Plugin.Trace("run-continue-rejected",ex.ToString());dialog.Description.text=ex.Message;dialog.ContinueButton.interactable=false;dialog.NewButton.Select();}
  });
  dialog.cancel=title.input.FindAction("UI/Cancel");dialog.cancel.canceled+=dialog.OnCancel;
  (saved!=null?dialog.ContinueButton:dialog.BackButton).Select();
 }
 public static void RestoreFailed(Exception error)
 {
  var font=Resources.FindObjectsOfTypeAll<TMP_Text>().First(t=>t.font!=null&&t.gameObject.scene.IsValid()).font;
  var dialog=Build(RunRecorder.Instance.transform,font);dialog.Description.text="Could not restore this run. Your checkpoint is preserved.\n"+error.Message;
  dialog.ContinueButton.gameObject.SetActive(false);dialog.NewButton.gameObject.SetActive(false);
  dialog.BackButton.onClick.AddListener(()=>{RunResume.Pending=null;RunRecorder.Instance.Finished=true;RunRecorder.Instance.GetComponent<GameController>().ChangeState<SuspendToTitleState>();});
  dialog.BackButton.Select();
 }
 static ResumeDialog Build(Transform parent,TMP_FontAsset font)
 {
  var root=new GameObject("RunResumeDialog",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(ResumeDialog));root.transform.SetParent(parent,false);
  var dialog=root.GetComponent<ResumeDialog>();Instance=dialog;dialog.Root=root;
  var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=80;
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
  var box=StatsOverlay.Box(root.transform,"OpaqueRunPrompt",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(720,260),new Color(.15f,.13f,.19f,1));
  dialog.Description=StatsOverlay.Text(box.transform,"CheckpointDescription",font,"",25,new Vector2(28,-30),new Vector2(664,126));dialog.Description.alignment=TextAlignmentOptions.Center;
  dialog.ContinueButton=Button(box.transform,font,"ResumeContinue","Continue",new Vector2(24,-184));
  dialog.NewButton=Button(box.transform,font,"ResumeNewRun","New run",new Vector2(256,-184));
  dialog.BackButton=Button(box.transform,font,"ResumeBack","Back",new Vector2(488,-184));
  var buttons=new[]{dialog.ContinueButton,dialog.NewButton,dialog.BackButton};
  for(int i=0;i<buttons.Length;i++){var navigation=buttons[i].navigation;navigation.mode=Navigation.Mode.Explicit;navigation.selectOnLeft=buttons[(i+2)%3];navigation.selectOnRight=buttons[(i+1)%3];buttons[i].navigation=navigation;}
  return dialog;
 }
 static Button Button(Transform parent,TMP_FontAsset font,string name,string text,Vector2 pos)
 {
  var box=StatsOverlay.Box(parent,name,new Vector2(0,1),new Vector2(0,1),pos,new Vector2(208,50),new Color(.16f,.2f,.28f,1));
  var button=box.AddComponent<Button>();button.targetGraphic=box.GetComponent<Image>();
  var colors=button.colors;colors.highlightedColor=new Color(.65f,.85f,.75f,1);colors.selectedColor=colors.highlightedColor;button.colors=colors;
  var label=StatsOverlay.Text(box.transform,name+"Label",font,text,24,Vector2.zero,new Vector2(208,50));label.alignment=TextAlignmentOptions.Center;return button;
 }
 void OnCancel(InputAction.CallbackContext context){back?.Invoke();}
 void OnDestroy(){if(cancel!=null)cancel.canceled-=OnCancel;if(Instance==this)Instance=null;}
}
[HarmonyPatch(typeof(TitleMainMenuState),nameof(TitleMainMenuState.OnClick))]
class ResumePlayPrompt
{
 static bool Prefix(TitleMainMenuState __instance,int i)
 {
  if(i!=0||!File.Exists(RunResume.PathName))return true;
  RunResume.Snapshot saved=null;string error=null;
  try{saved=RunResume.Read();}catch(Exception ex){error=ex.Message;}
  if(saved==null&&error==null)return true;
  if(ResumeDialog.Instance==null)ResumeDialog.Show(__instance,saved,error);return false;
 }
}
class SuspendToTitleState:GameState
{
 public override void Enter()
 {
  RunExitPreparation.Prepare();
  GameTimer.SharedInstance.Stop();pauseController.UnPause();
  // The original TransitionToTitleState deposits the run score. A suspended run
  // must not receive that reward, so use only the normal Unity scene boundary.
  AndroidNotifications.Clear();
  AndroidBurnState.Clear();
  AndroidPools.Clear();
  AndroidCrowCollision.Clear();
  AndroidSummonPulse.Clear();
  SceneManager.LoadSceneAsync("TitleScreen",LoadSceneMode.Single);
 }
 public override void Exit(){}
}
[HarmonyPatch(typeof(PauseState),nameof(PauseState.Enter))]
class ResumeSuspendControl
{
 static void Postfix()
 {
  var game=Object.FindObjectOfType<GameController>();
  var existing=game.pauseMenu.GetComponentsInChildren<Button>(true).FirstOrDefault(b=>b.name=="ResumeSaveAndReturn");
  if(existing!=null){existing.interactable=RunResume.CanSave();return;}
  var template=game.giveupButton;var copy=Object.Instantiate(template.gameObject,template.transform.parent);copy.name="ResumeSaveAndReturn";
  foreach(var script in copy.GetComponentsInChildren<flanne.UI.TextLocalizerUI>(true))script.enabled=false;
  copy.GetComponentInChildren<TMP_Text>(true).text="Save & return";
  var button=copy.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();
  button.onClick.AddListener(()=>
  {
   if(!RunResume.CanSave())return;
   RunResume.Save();RunRecorder.Instance.Finished=true;game.ChangeState<SuspendToTitleState>();
  });
  if(copy.transform.parent.GetComponent<LayoutGroup>()==null)
  {
   var rect=copy.GetComponent<RectTransform>();rect.anchoredPosition=template.GetComponent<RectTransform>().anchoredPosition+new Vector2(0,-60);
  }
  var panel=game.pauseMenu.GetComponent<RectTransform>();
  panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,panel.rect.height+template.GetComponent<RectTransform>().rect.height);
  var controls=new[]{game.pauseResumeButton,game.optionsButton,game.synergiesButton,template,button};
  for(int i=0;i<controls.Length;i++)
  {var navigation=controls[i].navigation;navigation.mode=Navigation.Mode.Explicit;navigation.selectOnUp=controls[(i+controls.Length-1)%controls.Length];navigation.selectOnDown=controls[(i+1)%controls.Length];controls[i].navigation=navigation;}
  button.interactable=RunResume.CanSave();CustomMenu.AddPauseControl(game);
 }
}
