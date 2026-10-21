using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using flanne;
using flanne.Core;
using flanne.UI;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;
public class StatsOverlay:MonoBehaviour
{
 public static StatsOverlay Instance;
 public static bool Enabled{get=>Plugin.Instance.StatsEnabled.Value;set=>Plugin.Instance.StatsEnabled.Value=value;}
 public static readonly Color Ink=new Color(.13f,.12f,.18f,.94f),Cream=new Color(.96f,.84f,.76f),Coral=new Color(.99f,.32f,.38f),Green=new Color(.34f,.45f,.40f);
 public TMP_FontAsset Font;
 public GameObject Panel; public TMP_Text Body,Skills,Heading;
 public Button SettingButton;
 public string LastStatsText,LastSkillsText;
 PlayerController bound;bool dirty=true;
 readonly Dictionary<string,SkillEntry> collected=new Dictionary<string,SkillEntry>();
 public class SkillEntry{public string Name,Tier;public int Count;}
 public IReadOnlyDictionary<string,SkillEntry> Collected=>collected;
 void Awake(){Instance=this;}
 public static RectTransform Rect(GameObject o,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size)
 {var r=o.GetComponent<RectTransform>() ?? o.AddComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;return r;}
 public static TMP_Text Text(Transform parent,string name,TMP_FontAsset font,string value,int size,Vector2 pos,Vector2 bounds)
 {
  var o=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));Rect(o,parent,new Vector2(0,1),new Vector2(0,1),pos,bounds);
  var t=o.GetComponent<TextMeshProUGUI>();t.font=font;t.text=value;t.fontSize=size;t.color=Cream;t.raycastTarget=false;t.enableWordWrapping=true;t.overflowMode=TextOverflowModes.Overflow;return t;
 }
 public static GameObject Box(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size,Color color)
 {var o=new GameObject(name,typeof(RectTransform),typeof(Image));Rect(o,parent,anchor,pivot,pos,size);o.GetComponent<Image>().color=color;return o;}
 void Build()
 {
  Font=Resources.FindObjectsOfTypeAll<TMP_Text>().First(x=>x.font!=null && x.gameObject.scene.IsValid()).font;
  var canvas=new GameObject("NativeStatsCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));canvas.transform.SetParent(transform,false);
  var c=canvas.GetComponent<Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=20;
  var scale=canvas.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);scale.matchWidthOrHeight=.5f;
  Panel=Box(canvas.transform,"LiveStatsPanel",new Vector2(0,1),new Vector2(0,1),new Vector2(16,-176),new Vector2(302,494),Ink);Panel.GetComponent<Image>().raycastTarget=false;
  var edge=Box(Panel.transform,"Accent",new Vector2(0,1),new Vector2(0,1),Vector2.zero,new Vector2(3,494),Green);edge.GetComponent<Image>().raycastTarget=false;
  Heading=Text(Panel.transform,"StatsHeading",Font,"RUN STATS  <size=16>F2</size>",23,new Vector2(14,-12),new Vector2(274,28));Heading.color=Coral;
  Text(Panel.transform,"StatsLegend",Font,"Effective  (base + bonus)",16,new Vector2(14,-44),new Vector2(274,22));
  Body=Text(Panel.transform,"StatsValues",Font,"",17,new Vector2(14,-72),new Vector2(274,260));
  Text(Panel.transform,"DamageScope",Font,"Before charge / conditional effects",16,new Vector2(14,-336),new Vector2(274,36)).color=Green;
  Text(Panel.transform,"SkillsHeading",Font,"SKILLS  <size=16>PgUp / PgDn</size>",19,new Vector2(14,-374),new Vector2(274,24)).color=Coral;
  var viewport=new GameObject("SkillsViewport",typeof(RectTransform),typeof(RectMask2D));Rect(viewport,Panel.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(14,-406),new Vector2(274,78));
  Skills=Text(viewport.transform,"SkillRows",Font,"None collected",16,Vector2.zero,new Vector2(274,78));Skills.lineSpacing=4;
  var scroll=viewport.AddComponent<ScrollRect>();scroll.viewport=viewport.GetComponent<RectTransform>();scroll.content=Skills.rectTransform;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=24;
  // No GraphicRaycaster on this canvas: it cannot swallow a Fire/aim pointer.
 }
 void Update()
 {
  var p=PlayerController.Instance;
  if(p!=bound){Unbind();bound=p;collected.Clear();dirty=true;if(p!=null)foreach(StatType s in Enum.GetValues(typeof(StatType)))if(s<StatType.Count)p.stats[s].ChangedEvent+=Changed;}
  if(Panel==null && p!=null && p.gun.gunData!=null && p.loadedCharacter!=null)Build();
  if(Panel==null)return;
  if(Keyboard.current!=null && Keyboard.current.f2Key.wasPressedThisFrame)Toggle();
  Panel.SetActive(Enabled && p!=null && !PauseController.isPaused);
  if(dirty && p!=null && p.gun.gunData!=null){Refresh();dirty=false;}
  // Page the clipped skill list with native keyboard input without raycasting.
  if(Panel.activeSelf && Keyboard.current!=null)
  {
   var scroll=Skills.GetComponentInParent<ScrollRect>();
   if(Keyboard.current.pageDownKey.wasPressedThisFrame)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition-.5f);
   if(Keyboard.current.pageUpKey.wasPressedThisFrame)scroll.verticalNormalizedPosition=Mathf.Clamp01(scroll.verticalNormalizedPosition+.5f);
  }
 }
 void Changed(object sender,EventArgs args){dirty=true;}
 void Unbind(){if(bound!=null && bound.stats!=null)foreach(StatType s in Enum.GetValues(typeof(StatType)))if(s<StatType.Count)bound.stats[s].ChangedEvent-=Changed;}
 void OnDestroy(){Unbind();}
 public void Toggle(){Enabled=!Enabled;dirty=true;UpdateSettingsLabels();}
 public void Dirty(){dirty=true;}
 static string N(float v)=>v.ToString("0.##",CultureInfo.InvariantCulture);
 static string Row(string name,float basic,float final,string unit="")=>name+"  "+N(final)+unit+"  <color=#87988b>("+N(basic)+(final-basic>=0?" +":" ")+N(final-basic)+")</color>";
 public void Refresh()
 {
  var p=bound;var g=p.gun;var d=g.gunData;var recipe=g.GetProjectileRecipe();
  var rows=new[]{Row("Damage",d.damage,recipe.damage),Row("Fire rate",1f/d.shotCooldown,1f/g.shotCooldown,"/s"),Row("Reload",d.reloadDuration,g.reloadDuration,"s"),Row("Magazine",d.maxAmmo,g.maxAmmo),Row("Projectiles",d.numOfProjectiles,g.numOfProjectiles),Row("Bullet speed",d.projectileSpeed,recipe.projectileSpeed),Row("Bullet size",100,recipe.size*100,"%"),Row("Pierce / bounce",d.piercing,recipe.piercing)+" / "+N(recipe.bounce),Row("Move speed",p.movementSpeed,p.finalMoveSpeed),Row("Summon damage",100,p.stats[StatType.SummonDamage].Modify(1)*100,"%"),Row("Summon speed",100,p.stats[StatType.SummonAttackSpeed].Modify(1)*100,"%")};
  float dodge=Mathf.Clamp(p.stats[StatType.Dodge].Modify(1)-1,0,p.stats[StatType.DodgeCapMod].Modify(60)/100);
  LastStatsText="<line-height=20>"+Row("Max HP",p.loadedCharacter.startHP,p.playerHealth.maxHP)+"\n"+string.Join("\n",rows)+"\n"+Row("Dodge",0,dodge*100,"%");if(Body.text!=LastStatsText)Body.text=LastStatsText;
  LastSkillsText=collected.Count==0?"None collected":string.Join("\n",collected.Values.OrderBy(x=>x.Name).Select(x=>x.Name+"  "+x.Tier+"  x"+x.Count));
  if(Skills.text!=LastSkillsText){Skills.text=LastSkillsText;Skills.rectTransform.sizeDelta=new Vector2(274,Mathf.Max(78,Skills.preferredHeight));}
 }
 static string Key(Powerup p)=>((LocalizedString)AccessTools.Field(typeof(Powerup),"nameStrID").GetValue(p)).key;
 public void Record(Powerup perk,PlayerController player)
 {
  if(bound!=player){Unbind();bound=player;collected.Clear();foreach(StatType s in Enum.GetValues(typeof(StatType)))if(s<StatType.Count)bound.stats[s].ChangedEvent+=Changed;}
  string key=Key(perk);var tree=perk.powerupTreeUIData;string tier="Special";
  if(tree!=null)tier=tree.startingPowerup!=null && Key(tree.startingPowerup)==key?"Tier 1":tree.finalPowerup!=null && Key(tree.finalPowerup)==key?"Tier 3":"Tier 2";
  if(!collected.TryGetValue(key,out var entry))collected[key]=entry=new SkillEntry{Name=perk.nameString,Tier=tier};entry.Count++;dirty=true;
 }
 public static void UpdateSettingsLabels()
 {foreach(var t in Resources.FindObjectsOfTypeAll<TMP_Text>().Where(x=>x.name=="StatsSettingLabel"))t.text="Stats overlay: "+(Enabled?"On":"Off")+"  [F2]";}
 public void AddSetting(OptionsSetter options)
 {
  if(Resources.FindObjectsOfTypeAll<SettingsVisibility>().Any(x=>x.Owner==options))return;
  var font=options.GetComponentsInChildren<TMP_Text>(true).First(x=>x.font!=null).font;
  var root=new GameObject("EnhancementSettingsCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
  var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
  var panel=Box(root.transform,"EnhancementSettings",new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-18,0),new Vector2(300,140),Ink);
  var visibility=root.AddComponent<SettingsVisibility>();visibility.Owner=options;visibility.Panel=panel;
  Text(panel.transform,"EnhancementsHeading",font,"ENHANCEMENTS",21,new Vector2(14,-14),new Vector2(272,28)).color=Coral;
  var row=Box(panel.transform,"StatsSetting",new Vector2(0,1),new Vector2(0,1),new Vector2(12,-58),new Vector2(276,58),Green);var button=row.AddComponent<Button>();button.targetGraphic=row.GetComponent<Image>();
  var label=Text(row.transform,"StatsSettingLabel",font,"",18,new Vector2(12,-12),new Vector2(252,40));button.onClick.AddListener(Toggle);SettingButton=button;UpdateSettingsLabels();
  ColorfulStyle.Instance.AddSetting(panel.transform,font);
 }
}
public class SettingsVisibility:MonoBehaviour
{
 public OptionsSetter Owner;public GameObject Panel;
 void LateUpdate()
 {
  if(Owner==null){Object.Destroy(gameObject);return;}
  var menu=Owner.GetComponent<flanne.UIExtensions.Menu>();
  Panel.SetActive(menu!=null && (bool)AccessTools.Field(typeof(flanne.UIExtensions.Menu),"isActive").GetValue(menu));
 }
}
[HarmonyPatch(typeof(Powerup),nameof(Powerup.Apply))]
class CollectedPowerup{static void Postfix(Powerup __instance,PlayerController player){StatsOverlay.Instance?.Record(__instance,player);Plugin.Trace("powerup-application",__instance.name+" Apply");}}
[HarmonyPatch(typeof(Powerup),nameof(Powerup.ApplyStack))]
class CollectedPowerupStack{static void Postfix(Powerup __instance,PlayerController player){StatsOverlay.Instance?.Record(__instance,player);Plugin.Trace("powerup-application",__instance.name+" ApplyStack");}}
[HarmonyPatch(typeof(OptionsSetter),"Start")]
class OverlaySetting{static void Postfix(OptionsSetter __instance){StatsOverlay.Instance.AddSetting(__instance);}}
