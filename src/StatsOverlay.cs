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
public enum MetricsView{Off,Little,Extended}
public class StatsOverlay:MonoBehaviour
{
 public static StatsOverlay Instance;
 public static MetricsView Mode{get=>Plugin.Instance.HudMode.Value;set{Plugin.Instance.HudMode.Value=value;if(Instance!=null)Instance.dirty=true;UpdateSettingsLabels();}}
 public static bool Enabled{get=>Mode!=MetricsView.Off;set=>Mode=value?MetricsView.Extended:MetricsView.Off;}
 public static readonly Color Ink=new Color(.13f,.12f,.18f,.94f),Cream=new Color(.96f,.84f,.76f),Coral=new Color(.99f,.32f,.38f),Green=new Color(.34f,.45f,.40f);
 public TMP_FontAsset Font;
 public GameObject Panel; public TMP_Text Body;
 public RectTransform SkillContent;
 RectTransform attributes;Rect previousSafe;Vector2 previousSize;
 public readonly List<Image> AttributeIcons=new List<Image>();
 public readonly List<Image> SkillIcons=new List<Image>();
 public Button SettingButton;
 public string LastStatsText,LastSkillsText;
 PlayerController bound;bool dirty=true;
 readonly Dictionary<string,SkillEntry> collected=new Dictionary<string,SkillEntry>();
 public class SkillEntry{public string Name,Tier,Path;public int Count,Order,PathOrder;public Sprite Icon;public Powerup Power;public Image View;}
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
  Panel=new GameObject("LiveStatsHUD",typeof(RectTransform));Rect(Panel,canvas.transform,Vector2.zero,Vector2.zero,Vector2.zero,new Vector2(1280,720));
  var root=Panel.GetComponent<RectTransform>();root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
  attributes=Rect(new GameObject("Attributes",typeof(RectTransform)),Panel.transform,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(16,0),new Vector2(302,360));
  foreach(string key in new[]{"Haste","Evasive","SummonMastery","Trainer","PowerShot","RapidFire","QuickHands","DoubleShot","Sniper","BigShot","Penetration","RubberBullets"})
  {
   var icon=Icon(attributes,"Attribute_"+key,Vector2.zero,new Vector2(26,26));icon.rectTransform.anchoredPosition=new Vector2(0,-AttributeIcons.Count*30);AttributeIcons.Add(icon);
  }
  Body=Text(attributes,"StatsValues",Font,"",18,new Vector2(36,-2),new Vector2(266,360));Body.enableWordWrapping=false;
  var viewport=new GameObject("SkillsViewport",typeof(RectTransform));Rect(viewport,Panel.transform,new Vector2(1,.5f),new Vector2(1,.5f),new Vector2(-16,0),new Vector2(40,420));
  SkillContent=Rect(new GameObject("CollectedSkills",typeof(RectTransform)),viewport.transform,new Vector2(1,.5f),new Vector2(1,.5f),Vector2.zero,new Vector2(40,420));
  // No GraphicRaycaster on this canvas: it cannot swallow a Fire/aim pointer.
 }
 void Update()
 {
  var p=PlayerController.Instance;if(p==null)p=null;
  if(!ReferenceEquals(p,bound)){Unbind();bound=p;collected.Clear();dirty=true;if(p!=null)foreach(StatType s in Enum.GetValues(typeof(StatType)))if(s<StatType.Count)p.stats[s].ChangedEvent+=Changed;}
  if(Panel==null && p!=null && p.gun.gunData!=null && p.loadedCharacter!=null)Build();
  if(Panel==null)return;
  if(Keyboard.current!=null && Keyboard.current.f2Key.wasPressedThisFrame)Toggle();
  Panel.SetActive(p!=null && !PauseController.isPaused);
  Body.transform.parent.gameObject.SetActive(Enabled);
  if(dirty && p!=null && p.gun.gunData!=null){Refresh();dirty=false;}
  var size=Panel.GetComponent<RectTransform>().rect.size;
  if(previousSize!=size||previousSafe!=Screen.safeArea){previousSize=size;previousSafe=Screen.safeArea;Layout();}
 }
 void Changed(object sender,EventArgs args){dirty=true;}
 void Unbind(){if(bound!=null && bound.stats!=null)foreach(StatType s in Enum.GetValues(typeof(StatType)))if(s<StatType.Count)bound.stats[s].ChangedEvent-=Changed;}
 void OnDestroy(){Unbind();}
 public void Toggle(){Mode=(MetricsView)(((int)Mode+1)%3);Plugin.Instance.Config.Save();dirty=true;}
 public void Dirty(){dirty=true;}
 static string N(float v)=>v.ToString("0.##",CultureInfo.InvariantCulture);
 static Image Icon(Transform parent,string name,Vector2 position,Vector2 size)
 {
  var o=new GameObject(name,typeof(RectTransform),typeof(Image));Rect(o,parent,new Vector2(0,1),new Vector2(0,1),position,size);var i=o.GetComponent<Image>();i.preserveAspect=true;i.raycastTarget=false;return i;
 }
 static string Row(string name,float basic,float final,string unit="",bool lowerIsBetter=false)
 {
  string value=N(final)+unit;
  if(final!=basic)value="<color="+((final>basic)!=lowerIsBetter?"#A8D5BA":"#E8A3A8")+">"+value+"</color>";
  return name+"  "+value+(Mode==MetricsView.Little?"":"  <color=#87988b>("+N(basic)+(final-basic>=0?" +":" ")+N(final-basic)+")</color>");
 }
 public void Refresh()
 {
  var p=bound;var g=p.gun;var d=g.gunData;var recipe=g.GetProjectileRecipe();
  var rows=new[]{Row("Damage",d.damage,recipe.damage),Row("Fire rate",1f/d.shotCooldown,1f/g.shotCooldown,"/s"),Row("Reload",d.reloadDuration,g.reloadDuration,"s",true),Row("Projectiles",d.numOfProjectiles,g.numOfProjectiles),Row("Bullet speed",d.projectileSpeed,recipe.projectileSpeed),Row("Bullet size",100,recipe.size*100,"%"),Row("Pierce",d.piercing,recipe.piercing),Row("Bounce",d.bounce,recipe.bounce)};
  float dodge=Mathf.Clamp(p.stats[StatType.Dodge].Modify(1)-1,0,p.stats[StatType.DodgeCapMod].Modify(60)/100);
  var all=new[]{Row("Move speed",p.movementSpeed,p.finalMoveSpeed),Row("Dodge",0,dodge*100,"%"),Row("Summon damage",100,p.stats[StatType.SummonDamage].Modify(1)*100,"%"),Row("Summon speed",100,p.stats[StatType.SummonAttackSpeed].Modify(1)*100,"%")}.Concat(rows).ToArray();
  LastStatsText=string.Join("\n",all);Body.text="<line-height=30>"+string.Join("\n",all.Select(r=>r.Substring(r.IndexOf("  ",StringComparison.Ordinal)+2)));
  var powers=Resources.FindObjectsOfTypeAll<Powerup>();
  foreach(var icon in AttributeIcons){string key=icon.name.Substring("Attribute_".Length);icon.sprite=powers.First(x=>x.name==key).icon;}
  string skills=string.Join("\n",collected.Values.OrderBy(x=>x.Name).Select(x=>x.Name+"  "+x.Tier+"  x"+x.Count));
  if(LastSkillsText!=skills)
  {
   LastSkillsText=skills;foreach(Transform child in SkillContent)Object.Destroy(child.gameObject);SkillIcons.Clear();int index=0;
   foreach(var entry in collected.Values.OrderBy(x=>x.Order))
   {
    float y=-index++*42;var icon=Icon(SkillContent,entry.Name,new Vector2(0,y),new Vector2(32,32));icon.sprite=entry.Icon;entry.View=icon;SkillIcons.Add(icon);
    if(entry.Count>=3){var glow=icon.gameObject.AddComponent<Outline>();glow.effectColor=new Color(.66f,.84f,.73f,.65f);glow.effectDistance=new Vector2(2,2);glow.useGraphicAlpha=true;}
   }
   Layout();
  }
 }
 static string Key(Powerup p)=>((LocalizedString)AccessTools.Field(typeof(Powerup),"nameStrID").GetValue(p)).key;
 public void Record(Powerup perk,PlayerController player)
 {
  if(bound!=player){Unbind();bound=player;collected.Clear();foreach(StatType s in Enum.GetValues(typeof(StatType)))if(s<StatType.Count)bound.stats[s].ChangedEvent+=Changed;}
  string key=Key(perk);var tree=perk.powerupTreeUIData;string tier="Special";
  if(tree!=null)tier=tree.startingPowerup!=null && Key(tree.startingPowerup)==key?"Tier 1":tree.finalPowerup!=null && Key(tree.finalPowerup)==key?"Tier 3":"Tier 2";
  string path=tree!=null?tree.name:key.Contains("tome")?"Tomes":key.Contains("_perk_")||key.StartsWith("revamped_power_")?"Character":perk.prereqs!=null&&perk.prereqs.Count>1?"Synergies":"Other";
  int pathOrder=tree==null?collected.Count:Key(tree.startingPowerup)==key?0:Key(tree.leftPowerup)==key?1:Key(tree.rightPowerup)==key?2:3;
  if(!collected.TryGetValue(key,out var entry))collected[key]=entry=new SkillEntry{Name=perk.nameString,Tier=tier,Path=path,PathOrder=pathOrder,Icon=perk.icon,Power=perk,Order=collected.Count};entry.Count++;dirty=true;
 }
 void Layout()
 {
  var root=Panel.GetComponent<RectTransform>().rect;var safe=Screen.safeArea;
  float left=safe.xMin/Screen.width*root.width,right=(Screen.width-safe.xMax)/Screen.width*root.width;
  float bottom=safe.yMin/Screen.height*root.height,top=(Screen.height-safe.yMax)/Screen.height*root.height;
  float height=root.height-top-bottom,center=(bottom-top)*.5f;
  attributes.anchoredPosition=new Vector2(left+16,center);
  // Keep the established text size; shrink the group uniformly only if the
  // safe viewport is shorter than its complete twelve-row content.
  attributes.localScale=Vector3.one*Mathf.Min(1,(height-32)/360);
  var groups=collected.Values.OrderBy(e=>e.Order).GroupBy(e=>e.Path).ToArray();
  float cell=Mathf.Min(36,(height-200)/Mathf.Max(1,groups.Length));
  int columns=groups.Length==0?0:groups.Max(g=>g.Count());
  cell=Mathf.Min(cell,(root.width*.45f-right)/Mathf.Max(1,columns));
  var viewport=(RectTransform)SkillContent.parent;viewport.anchoredPosition=new Vector2(-right-16,center);viewport.sizeDelta=new Vector2(columns*cell,groups.Length*cell);
  SkillContent.sizeDelta=viewport.sizeDelta;
  for(int row=0;row<groups.Length;row++)
  {
   var entries=groups[row].OrderBy(e=>e.PathOrder).ToArray();
   for(int col=0;col<entries.Length;col++)
   {
    var icon=entries[col].View.rectTransform;icon.anchorMin=icon.anchorMax=new Vector2(1,1);icon.pivot=new Vector2(1,1);
    icon.sizeDelta=Vector2.one*(cell-4);icon.anchoredPosition=new Vector2(-(entries.Length-1-col)*cell,-row*cell-2);
   }
  }
 }
 public static void UpdateSettingsLabels()
  {foreach(var t in Resources.FindObjectsOfTypeAll<TMP_Text>().Where(x=>x.name=="StatsSettingLabel"))t.text="Metrics: "+Mode;}
}
[HarmonyPatch(typeof(Powerup),nameof(Powerup.Apply))]
class CollectedPowerup{static void Postfix(Powerup __instance,PlayerController player){StatsOverlay.Instance?.Record(__instance,player);Plugin.Trace("powerup-application",__instance.name+" Apply");}}
[HarmonyPatch(typeof(Powerup),nameof(Powerup.ApplyStack))]
class CollectedPowerupStack{static void Postfix(Powerup __instance,PlayerController player){StatsOverlay.Instance?.Record(__instance,player);Plugin.Trace("powerup-application",__instance.name+" ApplyStack");}}
[HarmonyPatch(typeof(OptionsSetter),"Start")]
class OverlaySetting{static void Postfix(OptionsSetter __instance){NativeOptionsSettings.Attach(__instance);}}
