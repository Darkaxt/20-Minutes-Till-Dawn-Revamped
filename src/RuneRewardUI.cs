using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization;

// The state owns pause, input and explicit current-run activation decisions.
// Permanent draws already belong to the journal; revealing never rerolls them.
public sealed class RuneRewardState:GameState
{
 GameObject root;RuneBossReward reward;RuneConversionVisual visual;RuneRewardDraw shown;
 TMP_Text resultName,description,status;Button confirm,enable,decline;
 public override void Enter()
 {
  owner.pauseController.Pause();reward=RuneRewardRun.Instance.Pending;RunResume.Save();EventSystem.current.SetSelectedGameObject(null);
  root=new GameObject("RuneRewardWindow",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(owner.transform,false);
  root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=90;
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
  Canvas.ForceUpdateCanvases();var screen=root.GetComponent<RectTransform>().rect;float width=Mathf.Min(760,screen.width-64);
  var box=StatsOverlay.Box(root.transform,"OpaqueRuneReward",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(width,560),new Color(.15f,.13f,.19f,1));
  box.transform.localScale=Vector3.one*Mathf.Min(1,(screen.height-48)/560);
  var font=StatsOverlay.Instance.Font;
  var heading=StatsOverlay.Text(box.transform,"RuneRewardHeading",font,"RUNE REWARD",30,new Vector2(24,-20),new Vector2(width-48,40));heading.alignment=TextAlignmentOptions.Center;heading.color=StatsOverlay.Coral;
  var body=StatsOverlay.Text(box.transform,"RuneRewardBody",font,Loadout.CharacterSelection.nameString+" · Arena reward",18,new Vector2(24,-62),new Vector2(width-48,28));body.alignment=TextAlignmentOptions.Center;
  float top=reward.draws.Count>5?300:260;
  resultName=StatsOverlay.Text(box.transform,"RuneResultName",font,"",24,new Vector2(28,-top),new Vector2(width-56,30));resultName.alignment=TextAlignmentOptions.Center;
  description=StatsOverlay.Text(box.transform,"RuneResultDescription",font,"",18,new Vector2(28,-top-38),new Vector2(width-56,80));description.alignment=TextAlignmentOptions.Center;description.enableAutoSizing=true;description.fontSizeMin=14;description.fontSizeMax=18;
  status=StatsOverlay.Text(box.transform,"RuneActivationStatus",font,"",17,new Vector2(28,-430),new Vector2(width-56,34));status.alignment=TextAlignmentOptions.Center;status.color=new Color(.65f,.85f,.75f);
  confirm=Button(box.transform,"RuneRewardContinue","Continue",0,240,()=>{RuneRewardRun.Instance.GrantXP(reward);reward.phase=2;RunResume.Save();owner.ChangeState<CombatState>();});
  float choiceWidth=Mathf.Min(270,(width-96)/2);
  enable=Button(box.transform,"RuneRewardEnable","Enable this run",-choiceWidth/2-12,choiceWidth,()=>Choose(true));
  decline=Button(box.transform,"RuneRewardDecline","Not now",choiceWidth/2+12,choiceWidth,()=>Choose(false));
  confirm.gameObject.SetActive(false);enable.gameObject.SetActive(false);decline.gameObject.SetActive(false);
  visual=box.AddComponent<RuneConversionVisual>();visual.Initialize(reward,()=>RunResume.Save(),()=>owner.StartCoroutine(NextDecision()),ShowResult);
 }
 Button Button(Transform parent,string name,string label,float x,float width,UnityEngine.Events.UnityAction click)
 {
  var go=StatsOverlay.Box(parent,name,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(x,22),new Vector2(width,44),new Color(.16f,.2f,.28f,1));
  var button=go.AddComponent<Button>();button.targetGraphic=go.GetComponent<Image>();var colors=button.colors;colors.selectedColor=colors.highlightedColor=new Color(.65f,.85f,.75f);button.colors=colors;
  var text=StatsOverlay.Text(go.transform,name+"Label",StatsOverlay.Instance.Font,label,22,Vector2.zero,new Vector2(width,44));text.alignment=TextAlignmentOptions.Center;
  button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(click);return button;
 }
 void ShowResult(RuneRewardDraw draw)
 {
  shown=draw;var data=RuneProgression.Definition(draw.rune);resultName.text=data.nameString+" · Rank "+draw.rank+" / 5";description.text=data.description;
  var choice=reward.choices?.FirstOrDefault(c=>c.rune==draw.rune);int active=RuneRewardRun.Instance.ActiveRank(draw.rune);
  status.text=draw.capped?"Converted to experience":active>0?"Active this run at rank "+active:choice!=null?"Saved for future runs":"Earned for "+Loadout.CharacterSelection.nameString;
 }
 IEnumerator NextDecision()
 {
  foreach(var slot in visual.Buttons)slot.interactable=false;
  enable.gameObject.SetActive(false);decline.gameObject.SetActive(false);confirm.gameObject.SetActive(false);
  // Input which ended the roll must finish before it can accept a new choice.
  // Wait for the native actions' release/dispatch state, not an elapsed delay.
  var module=(InputSystemUIInputModule)EventSystem.current.currentInputModule;
  while(module.submit.action.ReadValue<float>()>0||module.submit.action.triggered||module.leftClick.action.ReadValue<float>()>0||module.leftClick.action.triggered)yield return null;
  var pending=RuneRewardRun.Instance.PendingChoice(reward);
  if(pending!=null)
  {
   ShowResult(pending);status.text="Enable this rune for the current run?";
   enable.gameObject.SetActive(true);decline.gameObject.SetActive(true);enable.interactable=decline.interactable=true;
   enable.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=decline,selectOnRight=decline,selectOnUp=decline,selectOnDown=decline};
   decline.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=enable,selectOnRight=enable,selectOnUp=enable,selectOnDown=enable};
   enable.Select();NativeMenuFocus.Open(root.GetComponent<Canvas>(),enable);yield break;
  }
  ShowResult(shown);var slots=visual.Buttons;int columns=Mathf.Min(5,slots.Count);
  for(int i=0;i<slots.Count;i++)
  {
   slots[i].interactable=true;
   slots[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=i%columns>0?slots[i-1]:confirm,selectOnRight=i%columns<columns-1&&i+1<slots.Count?slots[i+1]:confirm,selectOnUp=i>=columns?slots[i-columns]:confirm,selectOnDown=i+columns<slots.Count?slots[i+columns]:confirm};
  }
  confirm.gameObject.SetActive(true);confirm.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=slots.Last(),selectOnDown=slots.First(),selectOnLeft=slots.First(),selectOnRight=slots.Last()};
  confirm.Select();NativeMenuFocus.Open(root.GetComponent<Canvas>(),confirm);
 }
 void Choose(bool enabled)
 {
  var pending=RuneRewardRun.Instance.PendingChoice(reward);enable.interactable=decline.interactable=false;
  owner.StartCoroutine(RuneRewardRun.Instance.Choose(reward,pending,enabled,()=>owner.StartCoroutine(NextDecision())));
 }
 public override void Exit(){if(root!=null){NativeMenuFocus.Close(root.GetComponent<Canvas>());root.SetActive(false);Object.Destroy(root);}owner.pauseController.UnPause();}
}
