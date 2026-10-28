using TMPro;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization;

// The native state owns pause/input. Outcomes already belong to the journal;
// this popup only reveals them and delivers their XP once.
public sealed class RuneRewardState:GameState
{
 GameObject root;RuneBossReward reward;
 public override void Enter()
 {
  owner.pauseController.Pause();reward=RuneRewardRun.Instance.Pending;RunResume.Save();
  root=new GameObject("RuneRewardWindow",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(owner.transform,false);
  root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=90;
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
  Canvas.ForceUpdateCanvases();float width=Mathf.Min(760,root.GetComponent<RectTransform>().rect.width-64);
  var box=StatsOverlay.Box(root.transform,"OpaqueRuneReward",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(width,360),new Color(.15f,.13f,.19f,1));
  var font=StatsOverlay.Instance.Font;
  var heading=StatsOverlay.Text(box.transform,"RuneRewardHeading",font,"RUNES",30,new Vector2(24,-24),new Vector2(width-48,44));heading.alignment=TextAlignmentOptions.Center;heading.color=StatsOverlay.Coral;
  var body=StatsOverlay.Text(box.transform,"RuneRewardBody",font,Loadout.CharacterSelection.nameString,19,new Vector2(24,-70),new Vector2(width-48,30));body.alignment=TextAlignmentOptions.Center;
  var buttonObject=StatsOverlay.Box(box.transform,"RuneRewardContinue",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,22),new Vector2(240,44),new Color(.16f,.2f,.28f,1));
  var button=buttonObject.AddComponent<Button>();button.targetGraphic=buttonObject.GetComponent<Image>();var colors=button.colors;colors.selectedColor=colors.highlightedColor=new Color(.65f,.85f,.75f);button.colors=colors;
  var text=StatsOverlay.Text(buttonObject.transform,"ContinueLabel",font,"Continue",24,Vector2.zero,new Vector2(240,44));text.alignment=TextAlignmentOptions.Center;
  button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(()=>{RuneRewardRun.Instance.GrantXP(reward);reward.phase=2;RunResume.Save();owner.ChangeState<CombatState>();});
  buttonObject.SetActive(false);
  box.AddComponent<RuneConversionVisual>().Initialize(reward,()=>RunResume.Save(),()=>{buttonObject.SetActive(true);button.Select();NativeMenuFocus.Open(root.GetComponent<Canvas>(),button);});
 }
 public override void Exit(){if(root!=null){root.SetActive(false);Object.Destroy(root);}owner.pauseController.UnPause();}
}
