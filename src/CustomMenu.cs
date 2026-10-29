using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// A native UI screen, reached through native states. No desktop hotkeys,
// IMGUI window, alternate input stack or edits to shared content assets.
public class CustomMenu:MonoBehaviour
{
 public static CustomMenu Instance;
 static readonly string[] Tabs={"Run","Weapon","Attributes","Enemies","Summons","Upgrades","Objects"};
 class Row{public string label;public Func<float> get;public System.Action<float> set;public System.Action action;public float step=1,min=0,max=100;public bool integer;}
 readonly List<Row> rows=new List<Row>();readonly List<Selectable> controls=new List<Selectable>();
 Transform box,content;TMP_FontAsset font;TMP_Text status,pageLabel;InputAction cancel;
 System.Action back,start;int tab,page;bool presets;MapData pendingPreset;
 public static void Show(Transform parent,InputActionAsset input,System.Action back,System.Action start)
 {
  Close();var root=new GameObject("CustomGameMenu",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CustomMenu));root.transform.SetParent(parent,false);
  var menu=root.GetComponent<CustomMenu>();Instance=menu;menu.back=back;menu.start=start;
  root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=85;
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
  menu.font=Resources.FindObjectsOfTypeAll<TMP_Text>().First(t=>t.font!=null&&t.gameObject.scene.IsValid()).font;
  menu.box=StatsOverlay.Box(root.transform,"CustomOpaquePanel",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(1060,600),new Color(.15f,.13f,.19f,1)).transform;
  StatsOverlay.Text(menu.box,"CustomHeading",menu.font,"CUSTOM GAME",28,new Vector2(24,-18),new Vector2(1012,36)).color=StatsOverlay.Coral;
  StatsOverlay.Text(menu.box,"CustomRules",menu.font,"Score only · No unlocks or achievements · Obtained content",18,new Vector2(24,-56),new Vector2(1012,26));
  menu.status=StatsOverlay.Text(menu.box,"CustomStatus",menu.font,"",16,new Vector2(24,-482),new Vector2(1012,40));menu.status.color=StatsOverlay.Green;
  menu.cancel=input.FindAction("UI/Cancel");menu.cancel.canceled+=menu.OnCancel;menu.Refresh();
 }
 public static void Close(){if(Instance!=null){Instance.gameObject.SetActive(false);Object.Destroy(Instance.gameObject);Instance=null;}}
 void OnCancel(InputAction.CallbackContext ctx)
 {
  var input=EventSystem.current.currentSelectedGameObject?.GetComponent<TMP_InputField>();
  if(input!=null&&input.transform.IsChildOf(transform))
  {input.DeactivateInputField();input.navigation.selectOnRight.Select();return;}
  back();
 }
 void OnDestroy(){if(cancel!=null)cancel.canceled-=OnCancel;if(Instance==this)Instance=null;}
 void Add(string label,string key,float original,float step=.1f,float min=0,float max=100,bool integer=false)
 {rows.Add(new Row{label=label,get=()=>CustomMode.Get(key,original),set=v=>CustomMode.Set(key,v),step=step,min=min,max=max,integer=integer});}
 void Command(string label,System.Action action){rows.Add(new Row{label=label,action=action});}
 void Inventory()
 {
  rows.Clear();var p=PlayerController.Instance;
  if(presets)
  {
   if(pendingPreset!=null)
   {
    var target=pendingPreset;
    Command("Start a new Custom run: "+CustomMode.ModeName(target),()=>CustomMode.RestartWithDefaults(target));
    Command("Keep playing this run",()=>{pendingPreset=null;presets=false;status.text="";Refresh();});return;
   }
   foreach(var map in CustomMode.Maps())
   {
    var target=map;
    Command("Load "+CustomMode.ModeName(map)+" defaults",()=>
    {
     if(CustomMode.Active&&CustomMode.Current.map!=target.name)
     {pendingPreset=target;status.text="Starts a new run at Darkness0 / Challenge1. Current run score is retained; the current build ends.";page=0;Refresh();return;}
     CustomMode.LoadDefaults(target);presets=false;page=0;status.text=CustomMode.Active?"Defaults restored. Your current build, difficulty and clock are retained.":CustomMode.ModeName(target)+" defaults loaded. Darkness0 / Challenge1; no Custom overrides.";Refresh();
    });
   }
   Command("Back to Custom settings",()=>{presets=false;status.text="";Refresh();});return;
  }
  if(tab==0)
  {
   var maps=CustomMode.Maps();int selected=Array.FindIndex(maps,m=>m.name==CustomMode.Current.map);if(selected<0)selected=0;
   if(!CustomMode.Active)Command("Scenario: "+maps[selected].nameString,()=>{CustomMode.Current.map=maps[(selected+1)%maps.Length].name;Refresh();});
   else Command("Scenario: "+SelectedMap.MapData.nameString,()=>status.text="The scenario stays fixed for this run.");
   bool challenge=CustomMode.Current.map=="Challenge_Forest";int offset=challenge?1:0;
   rows.Add(new Row{label=challenge?"Challenge tier":"Darkness",get=()=>Loadout.difficultyLevel+offset,set=v=>Loadout.difficultyLevel=(int)v-offset,integer=true,min=offset,max=challenge?Progression.State.challengeDifficultyUnlocked+1:SaveSystem.data.difficultyUnlocked});
   if(CustomMode.Active)rows.RemoveAt(rows.Count-1);
   Add("Experience multiplier","xp",1,.25f,0,100);
   Command("Reset experience multiplier",()=>{CustomMode.Reset("xp");Refresh();});
  }
  if(tab==1)
  {
   var gun=p!=null?p.gun.gunData:Loadout.GunSelection;
   string[] names={"Base damage","Shot cooldown (seconds)","Magazine","Reload time (seconds)","Projectiles","Spread","Knockback","Projectile speed","Bounce","Piercing","Inaccuracy"};
   for(int i=0;i<CustomMode.WeaponFields.Length;i++)
   {
    string field=CustomMode.WeaponFields[i];var member=AccessTools.Field(typeof(GunData),field);bool integer=member.FieldType==typeof(int);bool interval=field=="shotCooldown"||field=="reloadDuration";
    Add(names[i],"weapon:"+field,Convert.ToSingle(member.GetValue(gun)),interval?.05f:integer?1:.5f,interval?.05f:0,integer?1000:10000,integer);
   }
  }
  if(tab==2)foreach(StatType type in Enum.GetValues(typeof(StatType)))if(type<StatType.Count)Add(LocalizationSystem.GetLocalizedValue(StatLabels.Labels[type])+" multiplier","stat:"+type,1,.1f,0,100);
  if(tab==3)
  {
   string[] names={"Enemy spawn amount","Enemy health","Elite health","Enemy speed","Elite speed","Enemy contact damage","Elite contact damage","Boss health","Boss contact damage","Boss attack rate"};
   for(int i=0;i<CustomMode.SpawnFields.Length;i++)Add(names[i]+" multiplier","spawn:"+CustomMode.SpawnFields[i],1,.1f,0,100);
  }
  if(tab==4)
  {
   var summons=Object.FindObjectsOfType<AttackingSummon>().Where(s=>s is ShootingSummon||s is StabbingSummon).GroupBy(s=>s.SummonTypeID).OrderBy(g=>g.Key).ToArray();
   foreach(var group in summons){string name=Resources.FindObjectsOfTypeAll<Powerup>().FirstOrDefault(power=>power.name==group.Key)?.nameString??DisplayName(group.Key);Add(name+" damage multiplier","summon:"+group.Key+":damage",1,.1f,0,100);Add(name+" attack speed","summon:"+group.Key+":speed",1,.1f,0,100);}
   if(summons.Length==0)Command("Collect a summon upgrade, then reopen this screen.",()=>{});
  }
  if(tab==5)
  {
   if(!CustomMode.Active){Command("Upgrade controls are available during the run.",()=>{});return;}
   var acquired=RunRecorder.Instance.Acquired.Where(a=>!a.evolution).Select(a=>a.key).Distinct().ToArray();
   foreach(string key in acquired)
   {
    var power=Resources.FindObjectsOfTypeAll<Powerup>().First(x=>RunResume.Key(x)==key);
    if(CustomPerkStats.Values(power).Length>0)Add(power.nameString+" stat contribution","perk:"+key+":scale",1,.1f,0,10);
   }
   foreach(var pair in CustomMode.AvailablePerks().OrderBy(x=>x.Item2.nameString))
   {string pool=pair.Item1,key=RunResume.Key(pair.Item2);Command("Obtain: "+pair.Item2.nameString,()=>{CustomMode.Grant(pool,key);Refresh();});}
   if(rows.Count==0)Command("No remaining upgrades with met prerequisites.",()=>{});
  }
  if(tab==6)
  {
   if(!CustomMode.Active){Command("Object controls are available during the run.",()=>{});return;}
   var map=SelectedMap.MapData;
   var pickups=Resources.FindObjectsOfTypeAll<flanne.Pickups.HPPickup>().Cast<Component>().Concat(Resources.FindObjectsOfTypeAll<flanne.Pickups.SHPPickup>().Cast<Component>()).Where(c=>!c.gameObject.scene.IsValid());
   foreach(string name in pickups.Select(c=>c.name).Concat(map.spawnSessions.Select(w=>w.monsterPrefab.name)).Concat(map.bossSpawns.Select(b=>b.bossPrefab.name)).Distinct())
   {string target=name;var pickup=pickups.FirstOrDefault(c=>c.name==target);string label=pickup is flanne.Pickups.HPPickup?"Red heart":pickup is flanne.Pickups.SHPPickup?"Soul heart":DisplayName(name);Command("Spawn: "+label,()=>{CustomMode.Spawn(target);status.text="Spawn requested. Bosses appear when play resumes.";});}
  }
  if(tab>=1&&tab<=5)Command("Reset "+Tabs[tab].ToLowerInvariant()+" adjustments",()=>{CustomMode.Reset(new[]{"","weapon:","stat:","spawn:","summon:","perk:"}[tab]);Refresh();});
 }
 static string DisplayName(string key)=>System.Text.RegularExpressions.Regex.Replace(key.Replace("PF_","").Replace("(Clone)",""),"([a-z0-9])([A-Z])","$1 $2").Replace('_',' ').Trim();
 void Refresh()
 {
  Inventory();page=Mathf.Clamp(page,0,Math.Max(0,(rows.Count-1)/8));controls.Clear();
  if(content!=null){content.gameObject.SetActive(false);Object.Destroy(content.gameObject);}
  var area=new GameObject("CustomControls",typeof(RectTransform));StatsOverlay.Rect(area,box,new Vector2(0,1),new Vector2(0,1),Vector2.zero,new Vector2(1060,600));content=area.transform;
  for(int i=0;i<Tabs.Length;i++){int index=i;var b=Button("CustomTab"+i,Tabs[i],new Vector2(24+i*146,-96),new Vector2(136,38),()=>{tab=index;page=0;presets=false;pendingPreset=null;status.text="";Refresh();});if(i==tab&&!presets)b.GetComponent<Image>().color=StatsOverlay.Green;}
  Button("CustomLoadDefaults","Load mode defaults",new Vector2(758,-18),new Vector2(278,36),()=>{presets=true;pendingPreset=null;page=0;status.text="Choose a mode you have obtained. Defaults use its original rules and remove Custom overrides.";Refresh();});
  int rowIndex=0;
  foreach(var row in rows.Skip(page*8).Take(8))
  {
   int index=page*8+rowIndex;float y=-150-rowIndex*40;rowIndex++;
   StatsOverlay.Text(content,"CustomRow"+index,font,row.label,19,new Vector2(24,y),new Vector2(710,34));
   if(row.action!=null){Button("CustomAction"+index,"Select",new Vector2(758,y),new Vector2(278,32),()=>Run(row.action));continue;}
   TMP_InputField input=null;
   System.Action<float> update=v=>{v=Mathf.Clamp(v,row.min,row.max);if(row.integer)v=Mathf.Round(v);Run(()=>row.set(v));input.SetTextWithoutNotify(row.get().ToString("0.##",CultureInfo.InvariantCulture));};
   var minus=Button("CustomMinus"+index,"−",new Vector2(758,y),new Vector2(48,32),()=>update(row.get()-row.step));
   var inputObj=StatsOverlay.Box(content,"CustomValue"+index,new Vector2(0,1),new Vector2(0,1),new Vector2(816,y),new Vector2(162,32),new Color(.16f,.2f,.28f,1));
   var label=StatsOverlay.Text(inputObj.transform,"ValueText",font,"",19,Vector2.zero,new Vector2(162,32));label.alignment=TextAlignmentOptions.Center;
   input=inputObj.AddComponent<TMP_InputField>();input.textViewport=inputObj.GetComponent<RectTransform>();input.textComponent=(TextMeshProUGUI)label;input.targetGraphic=inputObj.GetComponent<Image>();input.contentType=TMP_InputField.ContentType.DecimalNumber;
   input.SetTextWithoutNotify(row.get().ToString("0.##",CultureInfo.InvariantCulture));input.onEndEdit.AddListener(text=>{if(float.TryParse(text,NumberStyles.Float,CultureInfo.InvariantCulture,out float value)&&!float.IsNaN(value)&&!float.IsInfinity(value))update(value);else input.SetTextWithoutNotify(row.get().ToString("0.##",CultureInfo.InvariantCulture));});
   var plus=Button("CustomPlus"+index,"+",new Vector2(988,y),new Vector2(48,32),()=>update(row.get()+row.step));
   // Controller traversal changes numbers with the existing Minus/Plus buttons.
   // Pointer/keyboard retain direct entry without trapping the controller ring.
   input.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=minus,selectOnUp=minus,selectOnRight=plus,selectOnDown=plus};
  }
  pageLabel=StatsOverlay.Text(content,"CustomPage",font,(page+1)+" / "+Math.Max(1,(rows.Count+7)/8),18,new Vector2(480,-529),new Vector2(100,40));pageLabel.alignment=TextAlignmentOptions.Center;
  Button("CustomPrevious","<",new Vector2(410,-530),new Vector2(52,38),()=>{page=Math.Max(0,page-1);Refresh();});
  Button("CustomNext",">",new Vector2(598,-530),new Vector2(52,38),()=>{page=Math.Min((rows.Count-1)/8,page+1);Refresh();});
  Button("CustomBack",start==null?"Back to pause":"Back",new Vector2(24,-530),new Vector2(230,42),back);
  if(start!=null)Button("CustomStart","Start Custom",new Vector2(806,-530),new Vector2(230,42),()=>Run(start));
  else StatsOverlay.Text(content,"CustomTriggerScope",font,"Stats change; abilities keep their normal behavior.",15,new Vector2(700,-529),new Vector2(336,48));
  for(int i=0;i<controls.Count;i++){var nav=controls[i].navigation;nav.mode=Navigation.Mode.Explicit;nav.selectOnUp=nav.selectOnLeft=controls[(i+controls.Count-1)%controls.Count];nav.selectOnDown=nav.selectOnRight=controls[(i+1)%controls.Count];controls[i].navigation=nav;}
  var first=presets?content.Find(pendingPreset!=null?"CustomAction1":"CustomAction0").GetComponent<Button>():controls[tab];first.Select();NativeMenuFocus.Open(this,first);
 }
 void Run(System.Action action){try{action();}catch(InvalidOperationException ex){status.text=ex.Message;}}
 Button Button(string name,string text,Vector2 pos,Vector2 size,System.Action action)
 {
  var obj=StatsOverlay.Box(content,name,new Vector2(0,1),new Vector2(0,1),pos,size,new Color(.16f,.2f,.28f,1));var button=obj.AddComponent<Button>();button.targetGraphic=obj.GetComponent<Image>();
  var colors=button.colors;colors.highlightedColor=colors.selectedColor=new Color(.65f,.85f,.75f,1);button.colors=colors;
  var label=StatsOverlay.Text(obj.transform,name+"Label",font,text,19,Vector2.zero,size);label.alignment=TextAlignmentOptions.Center;button.onClick.AddListener(()=>action());controls.Add(button);return button;
 }
 internal static void AddPauseControl(GameController game)
 {
  if(!CustomMode.Active||game.pauseMenu.GetComponentsInChildren<Button>(true).Any(b=>b.name=="CustomSettings"))return;
  var template=game.giveupButton;var copy=Object.Instantiate(template.gameObject,template.transform.parent);copy.name="CustomSettings";
  foreach(var localizer in copy.GetComponentsInChildren<flanne.UI.TextLocalizerUI>(true))localizer.enabled=false;
  copy.GetComponentInChildren<TMP_Text>(true).text="Custom rules";var button=copy.GetComponent<Button>();button.onClick=new Button.ButtonClickedEvent();button.onClick.AddListener(()=>game.ChangeState<CustomSettingsState>());
  var panel=game.pauseMenu.GetComponent<RectTransform>();panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,panel.rect.height+template.GetComponent<RectTransform>().rect.height);
  var all=game.pauseMenu.GetComponentsInChildren<Button>(true).Where(b=>b.gameObject.activeSelf).ToArray();
  for(int i=0;i<all.Length;i++){var nav=all[i].navigation;nav.mode=Navigation.Mode.Explicit;nav.selectOnUp=all[(i+all.Length-1)%all.Length];nav.selectOnDown=all[(i+1)%all.Length];all[i].navigation=nav;}
 }
}
