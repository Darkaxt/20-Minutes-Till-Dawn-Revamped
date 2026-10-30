using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;
using flanne.UI;
using flanne.UIExtensions;
using Object=UnityEngine.Object;
using Menu=flanne.UIExtensions.Menu;
namespace TwentyMinutes.Modernization;

// Extend the actual Options owner in both scenes. Original indices and option
// callbacks stay intact; added commands are handled before the native Back route.
public sealed class NativeOptionsSettings:MonoBehaviour
{
 Menu menu;RectTransform column,background;Button stats,style,zoom,numbers,projectiles,effects,hud;int statsIndex,styleIndex,zoomIndex,numbersIndex,projectilesIndex,effectsIndex,hudIndex;
 void OnEnable(){InputUser.onChange+=ControlsChanged;}
 void OnDisable(){InputUser.onChange-=ControlsChanged;}
 void ControlsChanged(InputUser user,InputUserChange change,InputDevice device)
 {if(change==InputUserChange.ControlSchemeChanged)RefreshNavigation();}
 public static void Attach(OptionsSetter owner)
 {
  var view=owner.gameObject.AddComponent<NativeOptionsSettings>();view.menu=owner.GetComponent<Menu>();
 }
 void Build()
 {
  var view=this;var owner=GetComponent<OptionsSetter>();
  var template=owner.GetComponentsInChildren<Button>(true).Single(b=>b.name=="Outline");
  view.column=(RectTransform)template.transform.parent;
  view.stats=view.Row(template,"StatsSetting","StatsSettingLabel",out view.statsIndex);
  view.style=view.Row(template,"ColorfulSetting","ColorfulSettingLabel",out view.styleIndex);
  view.zoom=view.Row(template,"ZoomSetting","ZoomSettingLabel",out view.zoomIndex);
  view.numbers=view.Row(template,"DamageNumbersSetting","DamageNumbersSettingLabel",out view.numbersIndex);
  view.projectiles=view.Row(template,"ProjectileOpacitySetting","ProjectileOpacityLabel",out view.projectilesIndex);
  view.effects=view.Row(template,"EffectsOpacitySetting","EffectsOpacityLabel",out view.effectsIndex);
  view.hud=view.Row(template,"HudOpacitySetting","HudOpacityLabel",out view.hudIndex);
  int position=template.transform.GetSiblingIndex()+1;view.stats.transform.SetSiblingIndex(position);view.style.transform.SetSiblingIndex(position+1);view.zoom.transform.SetSiblingIndex(position+2);view.numbers.transform.SetSiblingIndex(position+3);
  view.projectiles.transform.SetSiblingIndex(position+4);view.effects.transform.SetSiblingIndex(position+5);
  view.hud.transform.SetSiblingIndex(position+6);
  var box=StatsOverlay.Box(owner.transform,"NativeOptionsOpaquePanel",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,Vector2.zero,new Color(.15f,.13f,.19f,1));
  view.background=box.GetComponent<RectTransform>();box.transform.SetAsFirstSibling();
 }
 Button Row(Button template,string name,string labelName,out int index)
 {
  var copy=Object.Instantiate(template.gameObject,column);copy.name=name;
  foreach(var localizer in copy.GetComponentsInChildren<TextLocalizerUI>(true))localizer.enabled=false;
  copy.GetComponentInChildren<TMP_Text>(true).name=labelName;
  var button=copy.GetComponent<Button>();
  // Remove the cloned Outline command, retaining its native click sound.
  for(int i=0;i<button.onClick.GetPersistentEventCount();i++)
   if(button.onClick.GetPersistentTarget(i) is OptionsSetter)button.onClick.SetPersistentListenerState(i,UnityEventCallState.Off);
  var extension=copy.GetComponent<ButtonExtension>()??copy.AddComponent<ButtonExtension>();
  button.onClick.RemoveAllListeners();button.onClick.AddListener(extension.OnClick);
  extension.onClick=new UnityEvent();extension.onCancel=new UnityEvent();
  if(extension.onSelect==null)extension.onSelect=new UnityEvent();
  var entries=NativeSelection.GunEntriesFor(menu);index=entries.Count;entries.Add(extension);return button;
 }
 internal bool Click(int index)
 {
  if(index==statsIndex){StatsOverlay.Instance.Toggle();return true;}
  if(index==styleIndex){ColorfulStyle.Instance.Toggle();return true;}
  if(index==zoomIndex){Plugin.Instance.ZoomOut.Value=(Plugin.Instance.ZoomOut.Value+25)%125;Plugin.Instance.Config.Save();ZoomLabel();return true;}
  if(index==numbersIndex){Plugin.Instance.DamageNumbers.Value=!Plugin.Instance.DamageNumbers.Value;Plugin.Instance.Config.Save();NumbersLabel();return true;}
  if(index==projectilesIndex){Plugin.Instance.ProjectileOpacity.Value=Plugin.Instance.ProjectileOpacity.Value%100+25;Plugin.Instance.Config.Save();OpacityLabels();return true;}
  if(index==effectsIndex){Plugin.Instance.EffectsOpacity.Value=Plugin.Instance.EffectsOpacity.Value%100+25;Plugin.Instance.Config.Save();OpacityLabels();return true;}
  if(index==hudIndex){Plugin.Instance.HudOpacity.Value=Plugin.Instance.HudOpacity.Value==90?80:Plugin.Instance.HudOpacity.Value+5;Plugin.Instance.Config.Save();OpacityLabels();return true;}
  return false;
 }
 void ZoomLabel(){zoom.GetComponentInChildren<TMP_Text>(true).text="Zoom out: "+Plugin.Instance.ZoomOut.Value+"%";}
 void NumbersLabel(){numbers.GetComponentInChildren<TMP_Text>(true).text="Damage numbers: "+(Plugin.Instance.DamageNumbers.Value?"On":"Off");}
 void OpacityLabels(){projectiles.GetComponentInChildren<TMP_Text>(true).text="Player projectiles opacity: "+Plugin.Instance.ProjectileOpacity.Value+"%";effects.GetComponentInChildren<TMP_Text>(true).text="Player effects opacity: "+Plugin.Instance.EffectsOpacity.Value+"%";hud.GetComponentInChildren<TMP_Text>(true).text="HUD opacity: "+Plugin.Instance.HudOpacity.Value+"%";}
 internal void Prepare()
 {
  // Create controls at the actual Show boundary. Clones made after the native
  // hidden CanvasGroup was disabled otherwise retain an interactive group cache
  // and steal Automatic navigation from the title menu before their first Show.
  if(stats==null)Build();
  StatsOverlay.Instance.SettingButton=stats;ColorfulStyle.Instance.SettingButton=style;
  StatsOverlay.UpdateSettingsLabels();ColorfulStyle.Instance.Labels();ZoomLabel();NumbersLabel();OpacityLabels();
  // Native Menu.Hide fails to remove its Show-created lambdas. Keep each new
  // toggle's registration owned by the next Show, so reopening toggles once.
  foreach(var button in new[]{stats,style,zoom,numbers,projectiles,effects,hud})
  {var entry=button.GetComponent<ButtonExtension>();entry.onClick.RemoveAllListeners();entry.onSelect.RemoveAllListeners();entry.onCancel.RemoveAllListeners();}
  var controls=column.GetComponentsInChildren<Button>(true).Where(b=>b.gameObject.activeSelf).ToArray();
  var layout=column.GetComponent<VerticalLayoutGroup>();layout.childAlignment=TextAnchor.MiddleCenter;
  layout.padding=new RectOffset(30,30,18,18);layout.spacing=0;layout.childControlWidth=layout.childControlHeight=true;layout.childForceExpandWidth=layout.childForceExpandHeight=false;
  foreach(var button in controls)
  {
   var element=button.GetComponent<LayoutElement>()??button.gameObject.AddComponent<LayoutElement>();
   element.minWidth=element.preferredWidth=300;element.minHeight=element.preferredHeight=24;element.flexibleWidth=element.flexibleHeight=0;
  }
  var size=new Vector2(360,controls.Length*24+36);
  column.anchorMin=column.anchorMax=column.pivot=new Vector2(.5f,.5f);column.sizeDelta=size;
  var canvas=GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>();
  column.position=canvas.TransformPoint(canvas.rect.center);background.position=column.position;background.sizeDelta=size+new Vector2(0,50);
  LayoutRebuilder.ForceRebuildLayoutImmediate(column);
  RefreshNavigation();
 }
 internal void RefreshNavigation()
 {
  if(column==null)return;
  var buttons=column.GetComponentsInChildren<Button>(true).Where(b=>b.gameObject.activeSelf).ToArray();
  var enabled=buttons.Where(b=>b.interactable).ToArray();
  foreach(var button in buttons)button.navigation=new Navigation{mode=Navigation.Mode.None};
  for(int i=0;i<enabled.Length;i++)enabled[i].navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnUp=i==0?null:enabled[i-1],selectOnDown=i+1==enabled.Length?null:enabled[i+1]};
 }
}
[HarmonyPatch(typeof(Menu),nameof(Menu.Show))]
class PrepareNativeOptions
{
 static void Prefix(Menu __instance){__instance.GetComponent<NativeOptionsSettings>()?.Prepare();}
 static void Postfix(Menu __instance){__instance.GetComponent<NativeOptionsSettings>()?.RefreshNavigation();}
}
[HarmonyPatch(typeof(OptionsSetter),nameof(OptionsSetter.Refresh))]
class RefreshNativeOptionsNavigation{static void Postfix(OptionsSetter __instance){__instance.GetComponent<NativeOptionsSettings>()?.RefreshNavigation();}}
[HarmonyPatch(typeof(OptionsSetter),nameof(OptionsSetter.OnClickFullscreen))]
class RefreshNativeResolutionNavigation{static void Postfix(OptionsSetter __instance){__instance.GetComponent<NativeOptionsSettings>()?.RefreshNavigation();}}
[HarmonyPatch(typeof(Menu),"OnClick")]
class NativeOptionsCommand{static bool Prefix(Menu __instance,int index)=>!(__instance.GetComponent<NativeOptionsSettings>()?.Click(index)??false);}
