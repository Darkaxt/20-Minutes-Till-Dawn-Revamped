using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Steamworks;
using flanne;
using flanne.TitleScreen;
using flanne.UIExtensions;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

public sealed class SourceAchievementMenu:MonoBehaviour
{
 const string EntryName="RevampedAchievements";
 static SourceAchievementMenu instance;
 static int entryIndex;
 class CatalogRow{internal GameObject Root;internal Image Icon;internal TMP_Text Name,Description;}
 readonly List<Button> controls=new List<Button>();
 readonly CatalogRow[] rows=new CatalogRow[8];
 readonly List<Button> claims=new List<Button>();
 TitleScreenController title;Transform box;TMP_FontAsset font;TMP_Text summary,balance,pageLabel,message;
 InputAction cancel;Button back,previous,next;int page;

 internal static void Append(TitleScreenController owner)
 {
  var entries=NativeSelection.GunEntriesFor(owner.mainMenu);
  int existing=entries.FindIndex(e=>e.name==EntryName);
  if(existing>=0){entryIndex=existing;return;}
  var template=entries.Last();var copy=Object.Instantiate(template.gameObject,template.transform.parent);copy.name=EntryName;
  foreach(var localizer in copy.GetComponentsInChildren<flanne.UI.TextLocalizerUI>(true))localizer.enabled=false;
  copy.GetComponentInChildren<TMP_Text>(true).text="Achievements";
  if(copy.transform.parent.GetComponent<LayoutGroup>()==null)
  {
   var last=template.GetComponent<RectTransform>();var prior=entries[entries.Count-2].GetComponent<RectTransform>();
   copy.GetComponent<RectTransform>().anchoredPosition=last.anchoredPosition+(last.anchoredPosition-prior.anchoredPosition);
  }
  copy.transform.SetAsLastSibling();entryIndex=entries.Count;entries.Add(copy.GetComponent<ButtonExtension>());
 }
 internal static bool Open(int index)
 {
  if(index!=entryIndex)return false;
  var owner=Progression.Title;
  if(instance==null)instance=Build(owner);
  owner.mainMenu.interactable=false;
  SourceAchievementRewards.ImportSteam();
  instance.gameObject.SetActive(true);instance.Refresh();instance.back.Select();
  return true;
 }
 static SourceAchievementMenu Build(TitleScreenController owner)
 {
  var root=new GameObject("AchievementRewards",typeof(RectTransform));root.SetActive(false);root.transform.SetParent(owner.transform,false);
  var canvas=root.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=85;
  var scaler=root.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
  root.AddComponent<GraphicRaycaster>();var view=root.AddComponent<SourceAchievementMenu>();view.title=owner;view.cancel=owner.input.FindAction("UI/Cancel");
  view.font=owner.modeSelectStartButton.GetComponentInChildren<TMP_Text>(true).font;
  view.box=StatsOverlay.Box(root.transform,"AchievementOpaquePanel",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(1060,600),new Color(.15f,.13f,.19f,1)).transform;
  StatsOverlay.Text(view.box,"AchievementHeading",view.font,"ACHIEVEMENTS",28,new Vector2(24,-18),new Vector2(1012,36)).color=StatsOverlay.Coral;
  view.summary=StatsOverlay.Text(view.box,"AchievementSummary",view.font,"",19,new Vector2(24,-60),new Vector2(1012,26));
  for(int i=0;i<SourceAchievementRewards.Data.rewards.Length;i++)
  {
   var reward=SourceAchievementRewards.Data.rewards[i];float x=24+i*520;
   view.Icon(view.box,"RewardIcon"+reward.id,reward.Icon,new Vector2(x,-104),40);
   StatsOverlay.Text(view.box,"RewardAmount"+reward.id,view.font,reward.amount.ToString("N0")+" credits",22,new Vector2(x+50,-101),new Vector2(250,30));
   StatsOverlay.Text(view.box,"RewardThreshold"+reward.id,view.font,reward.count+" achievements",17,new Vector2(x+50,-132),new Vector2(250,26));
   view.claims.Add(view.Button("Claim"+reward.id,"Claim",new Vector2(x+308,-110),new Vector2(180,44),()=>
   {
    SourceAchievementRewards.Claim(reward);view.message.text="Collected "+reward.amount.ToString("N0")+" credits.";view.Refresh();view.back.Select();
   }));
  }
  for(int i=0;i<view.rows.Length;i++)
  {
   float x=24+(i%2)*520,y=-194-(i/2)*78;
   var row=new CatalogRow();var obj=new GameObject("AchievementRow"+i,typeof(RectTransform));obj.transform.SetParent(view.box,false);row.Root=obj;
   var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=new Vector2(x,y);rect.sizeDelta=new Vector2(492,74);
   row.Icon=view.Icon(obj.transform,"Icon",null,Vector2.zero,40);
   row.Name=StatsOverlay.Text(obj.transform,"Name",view.font,"",19,new Vector2(50,0),new Vector2(442,26));
   row.Description=StatsOverlay.Text(obj.transform,"Description",view.font,"",15,new Vector2(50,-28),new Vector2(442,44));view.rows[i]=row;
  }
  view.message=StatsOverlay.Text(view.box,"RewardMessage",view.font,"",16,new Vector2(24,-510),new Vector2(1012,26));view.message.color=StatsOverlay.Green;
  view.back=view.Button("AchievementBack","Back",new Vector2(24,-542),new Vector2(180,40),view.Close);
  view.previous=view.Button("AchievementPrevious","<",new Vector2(430,-542),new Vector2(60,40),()=>{view.page--;view.Refresh();(view.page==0?view.back:view.previous).Select();});
  view.pageLabel=StatsOverlay.Text(view.box,"AchievementPage",view.font,"",19,new Vector2(500,-542),new Vector2(80,40));view.pageLabel.alignment=TextAlignmentOptions.Center;
  view.next=view.Button("AchievementNext",">",new Vector2(590,-542),new Vector2(60,40),()=>{view.page++;view.Refresh();(view.page==view.Pages-1?view.back:view.next).Select();});
  view.balance=StatsOverlay.Text(view.box,"AchievementCredits",view.font,"",18,new Vector2(760,-548),new Vector2(276,32));view.balance.alignment=TextAlignmentOptions.Right;
  return view;
 }
 int Pages=>(SourceAchievementRewards.Data.entries.Length+7)/8;
 Image Icon(Transform parent,string name,Sprite sprite,Vector2 position,float size)
 {
  var obj=new GameObject(name,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);var rect=obj.GetComponent<RectTransform>();
  rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=new Vector2(size,size);
  var image=obj.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=true;image.raycastTarget=false;return image;
 }
 Button Button(string name,string label,Vector2 position,Vector2 size,Action action)
 {
  var obj=StatsOverlay.Box(box,name,new Vector2(0,1),new Vector2(0,1),position,size,new Color(.16f,.2f,.28f,1));
  var button=obj.AddComponent<Button>();button.targetGraphic=obj.GetComponent<Image>();var colors=button.colors;colors.highlightedColor=colors.selectedColor=new Color(.65f,.85f,.75f,1);button.colors=colors;
  var text=StatsOverlay.Text(obj.transform,name+"Label",font,label,20,Vector2.zero,size);text.alignment=TextAlignmentOptions.Center;button.onClick.AddListener(()=>action());controls.Add(button);return button;
 }
 void Refresh()
 {
  summary.text=SourceAchievementRewards.Count+" / "+SourceAchievementRewards.Data.entries.Length+" completed";
  for(int i=0;i<claims.Count;i++)
  {
   var reward=SourceAchievementRewards.Data.rewards[i];claims[i].interactable=SourceAchievementRewards.CanClaim(reward);
   claims[i].GetComponentInChildren<TMP_Text>().text=SourceAchievementRewards.Claimed(reward)?"Claimed":"Claim";
  }
  for(int i=0;i<rows.Length;i++)
  {
   int index=page*8+i;rows[i].Root.SetActive(index<SourceAchievementRewards.Data.entries.Length);
   if(index>=SourceAchievementRewards.Data.entries.Length)continue;
   var entry=SourceAchievementRewards.Data.entries[index];bool finished=SourceAchievementRewards.Finished(entry);
   rows[i].Icon.sprite=finished?entry.Earned:entry.Locked;rows[i].Name.text=entry.name;rows[i].Name.color=finished?StatsOverlay.Coral:StatsOverlay.Green;rows[i].Description.text=entry.description;
  }
  previous.interactable=page>0;next.interactable=page<Pages-1;pageLabel.text=(page+1)+" / "+Pages;UpdateBalance(null,0);
  var active=controls.Where(b=>b.interactable).ToArray();
  for(int i=0;i<active.Length;i++)
  {
   var nav=active[i].navigation;nav.mode=Navigation.Mode.Explicit;nav.selectOnLeft=nav.selectOnUp=active[(i+active.Length-1)%active.Length];nav.selectOnRight=nav.selectOnDown=active[(i+1)%active.Length];active[i].navigation=nav;
  }
 }
 void UpdateBalance(object sender,int value){balance.text="Credits "+PointsTracker.pts.ToString("N0");}
 void ReceivedStats(SteamId account,Result result){if(result==Result.OK){SourceAchievementRewards.ImportSteam();Refresh();}}
 void Cancel(InputAction.CallbackContext context)=>Close();
 void Close(){gameObject.SetActive(false);title.mainMenu.interactable=true;NativeSelection.GunEntriesFor(title.mainMenu)[entryIndex].button.Select();}
 void OnEnable(){cancel.canceled+=Cancel;SteamUserStats.OnUserStatsReceived+=ReceivedStats;PointsTracker.PointsChangedEvent+=UpdateBalance;}
 void OnDisable(){cancel.canceled-=Cancel;SteamUserStats.OnUserStatsReceived-=ReceivedStats;PointsTracker.PointsChangedEvent-=UpdateBalance;}
 void OnDestroy(){if(instance==this)instance=null;}
}
[HarmonyPatch(typeof(TitleMainMenuState),nameof(TitleMainMenuState.Enter))]
class AppendAchievementEntry{static void Prefix()=>SourceAchievementMenu.Append(Progression.Title);}
[HarmonyPatch(typeof(TitleMainMenuState),nameof(TitleMainMenuState.OnClick))]
class OpenAchievementEntry{static bool Prefix(int i)=>!SourceAchievementMenu.Open(i);}
