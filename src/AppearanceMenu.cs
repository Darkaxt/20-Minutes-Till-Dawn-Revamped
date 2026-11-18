using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using flanne;
using flanne.UI;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

public sealed class AppearanceTab:MonoBehaviour
{
 public Button Button;
 internal static void Build()
 {
  var title=Progression.Title;if(title==null||!Cosmetics.Available||title.selectPanel.GetComponent<AppearanceTab>()!=null)return;
  var owner=title.selectPanel.gameObject.AddComponent<AppearanceTab>();var native=title.runesButton;
  var tab=Object.Instantiate(native.transform.parent.gameObject,native.transform.parent.parent);tab.name="AppearanceTab";
  foreach(var transition in tab.GetComponentsInChildren<flanne.UIExtensions.MoveTransition>(true))transition.enabled=false;
  foreach(var localizer in tab.GetComponentsInChildren<TextLocalizerUI>(true))localizer.enabled=false;
  owner.Button=tab.GetComponentInChildren<Button>(true);owner.Button.name="AppearanceButton";owner.Button.onClick=new Button.ButtonClickedEvent();owner.Button.onClick.AddListener(()=>
  {AppearanceState.FromWeapon=title.CurrentState is GunSelectState;title.ChangeState<AppearanceState>();});
  var extension=owner.Button.GetComponent<flanne.UIExtensions.ButtonExtension>();if(extension!=null){extension.onClick=new UnityEvent();extension.onSelect=new UnityEvent();extension.onCancel=new UnityEvent();}
  foreach(var text in tab.GetComponentsInChildren<TMP_Text>(true))text.text="Appearance";
  foreach(var image in tab.GetComponentsInChildren<Image>(true))if(image.name.Contains("Arrow")||image.name.Contains("Prompt"))image.gameObject.SetActive(false);
  owner.Layout();
 }
 internal void Layout()
 {
  var title=Progression.Title;var native=(RectTransform)title.runesButton.transform.parent;var tab=(RectTransform)Button.transform.parent;
  tab.anchorMin=native.anchorMin;tab.anchorMax=native.anchorMax;tab.pivot=native.pivot;tab.anchoredPosition=native.anchoredPosition+new Vector2(native.rect.width+12,0);
  var menu=title.CurrentState is GunSelectState?(flanne.UIExtensions.Menu)title.gunMenu:title.characterMenu;
  var items=NativeSelection.GunEntriesFor(menu);int index=(int)AccessTools.Field(typeof(flanne.UIExtensions.Menu),"index").GetValue(menu);
  var navigation=title.runesButton.navigation;navigation.mode=Navigation.Mode.Explicit;navigation.selectOnRight=Button;navigation.selectOnDown=items[index].button;title.runesButton.navigation=navigation;
  Button.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=title.runesButton,selectOnDown=navigation.selectOnDown};
 }
}

public sealed class AppearanceState:TitleScreenState
{
 internal static bool FromWeapon;
 AppearanceMenu view;
 public override void Enter()
 {
  characterMenu.Hide();gunMenu.Hide();selectPanel.Hide();view=owner.gameObject.AddComponent<AppearanceMenu>();view.Build(FromWeapon);
  input.FindAction("UI/Cancel").canceled+=Cancel;
 }
 void Cancel(InputAction.CallbackContext context)=>Back();
 internal void Back(){if(FromWeapon)owner.ChangeState<GunSelectState>();else owner.ChangeState<CharacterSelectState>();}
 public override void Exit(){input.FindAction("UI/Cancel").canceled-=Cancel;if(view!=null){view.Close();Object.Destroy(view);}selectPanel.Show();selectPanel.interactable=true;characterMenu.interactable=true;gunMenu.interactable=true;}
}

public sealed class AppearanceMenu:MonoBehaviour
{
 GameObject root,box,grid;Button[] tabs;Button back,previous,next;Image preview;TMP_Text itemName,detail,pageLabel;string character,weapon,kind;int page,selected;float width,height,elapsed;
 Cosmetics.Item previewItem;readonly List<Button> entries=new List<Button>();int[] visible;
 static readonly string[] kinds={"character","weapon","wing","bullet"};
 static readonly string[] headings={"Character","Weapon","Wings","Bullet colour"};
 public static AppearanceMenu Current;
 public string Kind=>kind;public int[] Visible=>visible;public int Selected=>selected;
 public void Build(bool fromWeapon)
 {
  Current=this;var title=Progression.Title;
  int ci=(int)AccessTools.Field(typeof(flanne.UIExtensions.Menu),"index").GetValue(title.characterMenu),wi=(int)AccessTools.Field(typeof(flanne.UIExtensions.Menu),"index").GetValue(title.gunMenu);
  character=title.characterMenu[ci].name;weapon=title.gunMenu[wi].name;
  root=new GameObject("AppearanceCollection",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
  root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=70;
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
  Canvas.ForceUpdateCanvases();var screen=root.GetComponent<RectTransform>().rect;width=Mathf.Min(1100,screen.width-48);height=Mathf.Min(600,screen.height-48);
  box=StatsOverlay.Box(root.transform,"AppearanceOpaquePanel",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(width,height),new Color(.15f,.13f,.19f,1));
  var nativeFrame=title.runeMenuPanel.GetComponentsInChildren<Image>(true).FirstOrDefault(i=>i.sprite!=null&&i.type==Image.Type.Sliced);
  if(nativeFrame!=null)
  {var frame=StatsOverlay.Box(box.transform,"AppearanceNativeFrame",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(width,height),nativeFrame.color);var image=frame.GetComponent<Image>();image.sprite=nativeFrame.sprite;image.type=Image.Type.Sliced;image.raycastTarget=false;}
  Label(box.transform,"AppearanceHeading","APPEARANCE",30,new Vector2(28,-20),new Vector2(width-56,40)).color=StatsOverlay.Coral;
  var subtitle=Label(box.transform,"AppearanceOwner",title.characterMenu[ci].nameString+" · "+title.gunMenu[wi].nameString,18,new Vector2(28,-64),new Vector2(width-56,28));subtitle.color=new Color(.65f,.78f,.69f);
  tabs=new Button[4];float tabWidth=(width-74)/4;
  for(int i=0;i<4;i++){int at=i;tabs[i]=Button(box.transform,"Appearance"+headings[i],headings[i],new Vector2(28+i*(tabWidth+6),-108),new Vector2(tabWidth,42),()=>Show(kinds[at],0));}
  grid=new GameObject("AppearanceChoices",typeof(RectTransform));StatsOverlay.Rect(grid,box.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(28,-172),new Vector2(width*.58f-30,height-264));
  var picture=new GameObject("AppearancePreview",typeof(RectTransform),typeof(Image));StatsOverlay.Rect(picture,box.transform,new Vector2(0,1),new Vector2(.5f,.5f),new Vector2(width*.78f,-height*.44f),new Vector2(width*.28f,height*.34f));
  preview=picture.GetComponent<Image>();preview.preserveAspect=true;preview.raycastTarget=false;
  float right=width*.6f;itemName=Label(box.transform,"AppearanceName","",22,new Vector2(right,-height*.68f),new Vector2(width-right-24,48));itemName.alignment=TextAlignmentOptions.Center;
  detail=Label(box.transform,"AppearanceRequirement","",17,new Vector2(right,-height*.68f-58),new Vector2(width-right-24,74));detail.alignment=TextAlignmentOptions.Center;
  itemName.enableAutoSizing=true;itemName.fontSizeMin=16;itemName.fontSizeMax=22;detail.enableAutoSizing=true;detail.fontSizeMin=14;detail.fontSizeMax=17;
  back=Button(box.transform,"AppearanceBack","Back",new Vector2(28,-height+70),new Vector2(180,42),()=>((AppearanceState)title.CurrentState).Back());
  previous=Button(box.transform,"AppearancePrevious","<",new Vector2(width*.42f-58,-height+70),new Vector2(46,42),()=>Show(kind,page-1));
  next=Button(box.transform,"AppearanceNext",">",new Vector2(width*.42f+58,-height+70),new Vector2(46,42),()=>Show(kind,page+1));
  pageLabel=Label(box.transform,"AppearancePage","",18,new Vector2(width*.42f-8,-height+70),new Vector2(62,42));pageLabel.alignment=TextAlignmentOptions.Center;
  Show(fromWeapon?"weapon":"character",0);
  var viewport=root.AddComponent<AppearanceViewport>();viewport.Resize=()=>{string savedKind=kind;int savedPage=page;Close();entries.Clear();Build(AppearanceState.FromWeapon);Show(savedKind,savedPage);};
 }
 TMP_Text Label(Transform parent,string name,string text,int size,Vector2 at,Vector2 dimensions)
 {var font=Progression.Title.runesButton.GetComponentInChildren<TMP_Text>(true).font;var label=StatsOverlay.Text(parent,name,font,text,size,at,dimensions);label.raycastTarget=false;return label;}
 Button Button(Transform parent,string name,string text,Vector2 at,Vector2 dimensions,UnityAction click)
 {
  var go=StatsOverlay.Box(parent,name,new Vector2(0,1),new Vector2(0,1),at,dimensions,new Color(.16f,.2f,.28f,1));var button=go.AddComponent<Button>();button.targetGraphic=go.GetComponent<Image>();
  var colors=button.colors;colors.selectedColor=colors.highlightedColor=new Color(.65f,.85f,.75f);button.colors=colors;button.onClick.AddListener(click);
  if(text.Length!=0){var label=Label(go.transform,name+"Label",text,19,Vector2.zero,dimensions);label.alignment=TextAlignmentOptions.Center;}
  return button;
 }
 string Owner=>kind=="weapon"||kind=="bullet"?weapon:character;
 internal void Show(string category,int requested)
 {
  kind=category;var catalog=Cosmetics.Items.Values.Where(i=>i.kind==kind&&(i.owner==""||i.owner==Owner)).OrderBy(i=>i.id).Select(i=>i.id).Prepend(0).ToArray();
  const int perPage=20;int pages=(catalog.Length+perPage-1)/perPage;page=Mathf.Clamp(requested,0,pages-1);visible=catalog.Skip(page*perPage).Take(perPage).ToArray();
  foreach(var entry in entries){entry.gameObject.SetActive(false);Object.Destroy(entry.gameObject);}entries.Clear();
  float cell=Mathf.Min(78,(width*.58f-40)/5),size=Mathf.Min(62,cell-10);
  for(int i=0;i<visible.Length;i++)
  {
   int id=visible[i];var button=Button(grid.transform,"AppearanceItem_"+id,"",new Vector2((i%5)*cell,-(i/5)*cell),new Vector2(size,size),()=>Choose(id));entries.Add(button);
   var art=new GameObject("AppearanceIcon",typeof(RectTransform),typeof(Image));StatsOverlay.Rect(art,button.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(size-14,size-14));var image=art.GetComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;
   image.sprite=id==0?OriginalIcon():Cosmetics.Sprite(Cosmetics.Items[id].icon);image.color=Cosmetics.Owned(id)?Color.white:new Color(.36f,.42f,.45f,1);
   if(id!=0&&!Cosmetics.Owned(id))Label(button.transform,"AppearanceLocked","Locked",12,new Vector2(0,-size+17),new Vector2(size,16)).alignment=TextAlignmentOptions.Center;
   var select=button.gameObject.AddComponent<AppearanceChoice>();select.Selected=()=>Preview(id);
  }
  for(int i=0;i<entries.Count;i++)entries[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=i%5>0?entries[i-1]:null,selectOnRight=i%5<4&&i+1<entries.Count?entries[i+1]:null,selectOnUp=i>=5?entries[i-5]:tabs[Array.IndexOf(kinds,kind)],selectOnDown=i+5<entries.Count?entries[i+5]:back};
  for(int i=0;i<tabs.Length;i++)tabs[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=i>0?tabs[i-1]:null,selectOnRight=i+1<tabs.Length?tabs[i+1]:null,selectOnDown=entries[0]};
  previous.interactable=page>0;next.interactable=page+1<pages;pageLabel.text=(page+1)+" / "+pages;
  back.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=entries.Last(),selectOnRight=previous.interactable?previous:next.interactable?next:null};
  previous.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=back,selectOnRight=next.interactable?next:null,selectOnUp=entries.Last()};next.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=previous.interactable?previous:back,selectOnUp=entries.Last()};
  int equipped=Cosmetics.Selected(Owner,kind)?.id??0;int index=Array.IndexOf(visible,equipped);var first=entries[index<0?0:index];Preview(visible[index<0?0:index]);NativeMenuFocus.Open(root.GetComponent<Canvas>(),first);first.Select();
 }
 Sprite OriginalIcon()=>Cosmetics.Original(kind=="character"||kind=="wing"?character:weapon,kind=="character"||kind=="wing"?"character":"weapon");
 void Choose(int id){if(Cosmetics.Equip(Owner,kind,id))Preview(id);}
 internal void Preview(int id)
 {
  selected=id;previewItem=id==0?null:Cosmetics.Items[id];elapsed=0;preview.sprite=previewItem==null?OriginalIcon():Cosmetics.Sprite(previewItem.portrait??previewItem.icon);preview.color=Color.white;
  itemName.text=previewItem?.name??(kind=="wing"?"No wings":kind=="bullet"?"Original colour":"Original");
  bool equipped=(Cosmetics.Selected(Owner,kind)?.id??0)==id;
  detail.text=equipped?"Equipped":Cosmetics.Owned(id)?"Select to equip":kind=="weapon"?"Win a run with this weapon to unlock a skin.":kind=="character"?"Win a run with this character to unlock a cosmetic.":"Win a run to unlock a character cosmetic or wings.";
  detail.color=Cosmetics.Owned(id)?new Color(.65f,.78f,.69f):StatsOverlay.Coral;
 }
 void Update()
 {
  if(previewItem==null)return;elapsed+=Time.unscaledDeltaTime;
  if(previewItem.kind=="wing"&&previewItem.wingFrames.Length!=0)preview.sprite=Cosmetics.Sprite(previewItem.wingFrames[(int)(elapsed/.1f)%previewItem.wingFrames.Length]);
  else if(previewItem.kind=="weapon")
  {var clip=(previewItem.previewClips??new Cosmetics.Clip[0]).FirstOrDefault(c=>c.source.IndexOf("Idle",StringComparison.OrdinalIgnoreCase)>=0||c.source.IndexOf("Still",StringComparison.OrdinalIgnoreCase)>=0);if(clip!=null){float time=clip.duration>0?elapsed%clip.duration:0;var frame=clip.frames.LastOrDefault(f=>f.time<=time);if(frame!=null)preview.sprite=Cosmetics.Sprite(frame.sprite);}}
 }
 public void Close(){if(root!=null){NativeMenuFocus.Close(root.GetComponent<Canvas>());root.SetActive(false);Object.Destroy(root);}if(Current==this)Current=null;}
}
public sealed class AppearanceChoice:MonoBehaviour,ISelectHandler,IPointerEnterHandler
{public System.Action Selected;public void OnSelect(BaseEventData data)=>Selected();public void OnPointerEnter(PointerEventData data)=>Selected();}
public sealed class AppearanceViewport:MonoBehaviour
{public System.Action Resize;Vector2 size;void Awake(){size=GetComponent<RectTransform>().rect.size;}void OnRectTransformDimensionsChange(){var current=GetComponent<RectTransform>().rect.size;if(Resize!=null&&gameObject.activeInHierarchy&&size!=current){size=current;Resize();}}}

[HarmonyPatch(typeof(flanne.UIExtensions.Panel),nameof(flanne.UIExtensions.Panel.Show))]
class AppearanceHeader
{static void Postfix(flanne.UIExtensions.Panel __instance){if(Progression.Title!=null&&__instance==Progression.Title.selectPanel){AppearanceTab.Build();__instance.GetComponent<AppearanceTab>()?.Layout();}}}
[HarmonyPatch(typeof(flanne.UIExtensions.Menu),nameof(flanne.UIExtensions.Menu.Show))]
class AppearanceHeaderNavigation
{static void Postfix(flanne.UIExtensions.Menu __instance){var title=Progression.Title;if(title!=null&&(__instance==title.characterMenu||__instance==title.gunMenu))title.selectPanel.GetComponent<AppearanceTab>()?.Layout();}}
