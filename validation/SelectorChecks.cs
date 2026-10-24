using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using TMPro;
using flanne;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator SelectorPad(Gamepad pad,GamepadButton button)
 {
  InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(button));yield return null;yield return null;
  InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;yield return null;
 }
 static string SelectorText()=>string.Join("\n",Object.FindObjectsOfType<TMP_Text>().Where(t=>t.isActiveAndEnabled).Select(t=>t.name+"="+t.text));
 static string SelectorFocus()=>EventSystem.current.currentSelectedGameObject?.name??"none";
 IEnumerator SelectorMove(Gamepad pad,flanne.UIExtensions.Menu menu,Button target)
 {
  var start=EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>();
  var allowed=new HashSet<Selectable>(Entries(menu).Select(e=>(Selectable)e.button));
  var routes=new Dictionary<Selectable,List<GamepadButton>>{{start,new List<GamepadButton>()}};
  var queue=new Queue<Selectable>();queue.Enqueue(start);
  while(queue.Count>0&&!routes.ContainsKey(target))
  {
   var current=queue.Dequeue();
   var neighbors=new[]{current.FindSelectableOnLeft(),current.FindSelectableOnRight(),current.FindSelectableOnUp(),current.FindSelectableOnDown()};
   var directions=new[]{GamepadButton.DpadLeft,GamepadButton.DpadRight,GamepadButton.DpadUp,GamepadButton.DpadDown};
   for(int i=0;i<neighbors.Length;i++)
   {
    var next=neighbors[i];if(next==null||!allowed.Contains(next)||routes.ContainsKey(next))continue;
    routes.Add(next,routes[current].Concat(new[]{directions[i]}).ToList());queue.Enqueue(next);
   }
  }
  Require(routes.ContainsKey(target),"controller graph reaches roster entry "+target.name);
  foreach(var direction in routes[target])yield return SelectorPad(pad,direction);
  Require(EventSystem.current.currentSelectedGameObject==target.gameObject,"real native controller focus reaches "+target.name);
 }
 void RequireCharacterPreview(TitleScreenController title,int index)
 {
  var data=title.characterMenu[index];var text=SelectorText();
  Require(text.Contains(data.nameString)&&text.Contains(data.description),"character name and description match focused "+data.name);
  Require(Object.FindObjectsOfType<Image>().Any(image=>image.isActiveAndEnabled&&image.sprite==data.portrait),"native portrait matches focused "+data.name);
  Require(Object.FindObjectsOfType<TMP_Text>().Any(label=>label.name=="HPValue"&&label.text==data.startHP.ToString()),"native starting HP matches focused "+data.name);
 }
 void SelectorSnapshot(TitleScreenController title,string label)
 {
  var lines=new List<string>{"focus="+SelectorFocus(),"state="+title.CurrentState.GetType().Name,"module="+EventSystem.current.currentInputModule.GetType().Name,SelectorText()};
  foreach(var menu in new flanne.UIExtensions.Menu[]{title.characterMenu,title.gunMenu})
  foreach(var entry in Entries(menu))
  {
   var b=entry.button;var n=b.navigation;
   lines.Add(menu.name+"/"+b.name+" active="+b.gameObject.activeInHierarchy+" interactable="+b.IsInteractable()+" nav="+n.mode+" left="+n.selectOnLeft?.name+" right="+n.selectOnRight?.name+" up="+n.selectOnUp?.name+" down="+n.selectOnDown?.name);
  }
  File.WriteAllLines(Path.Combine(ResultDir,label+".txt"),lines);
 }
 IEnumerator CheckSelectors(TitleScreenController title)
 {
  SetPhase("controller-selector-regression");
  // Only this copied player's isolated save is changed. Start with obtained
  // Shana/Revolver, as on the phone; never grant locked content for this test.
  Progression.State=new Progression.Journal();
  Array.Clear(SaveSystem.data.characterUnlocks.unlocks,0,SaveSystem.data.characterUnlocks.unlocks.Length);
  Array.Clear(SaveSystem.data.gunUnlocks.unlocks,0,SaveSystem.data.gunUnlocks.unlocks.Length);
  Progression.Refresh();
  var pad=InputSystem.AddDevice<Gamepad>("RevampedSelectorController");
  Log("selector-pref-keys",AccessTools.Field(typeof(flanne.UIExtensions.DataListMenu<CharacterData>),"playerPrefsKey").GetValue(title.characterMenu)+" / "+AccessTools.Field(typeof(flanne.UIExtensions.DataListMenu<GunData>),"playerPrefsKey").GetValue(title.gunMenu));
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
  var failures=new List<string>();
  while(title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  Log("selector-controller","initial focus="+SelectorFocus()+" UI map="+title.input.FindActionMap("UI").enabled);
  yield return SelectorPad(pad,GamepadButton.South);
  while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f||title.selectPanel.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  SelectorSnapshot(title,"character-initial-controller");
  int index=Entries(title.characterMenu).FindIndex(e=>e.gameObject==EventSystem.current.currentSelectedGameObject);
  Require(index>=0,"native controller opens character selection with a roster slot focused");
  if(Environment.GetEnvironmentVariable("MODERNIZATION_SELECTOR_REMEMBERED")=="1")Require(index==14,"appended remembered character survives stock pre-append Awake clamp");
  var character=title.characterMenu[index];
  bool preview=SelectorText().Contains(character.nameString)&&SelectorText().Contains(character.description);
  if(!preview)failures.Add("initial character preview does not match controller focus");
  yield return Capture("character-initial-controller.png");
  if(Environment.GetEnvironmentVariable("MODERNIZATION_SELECTOR_BASELINE")!="1")
  {
   RequireCharacterPreview(title,index);
   for(int i=0;i<Entries(title.characterMenu).Count;i++)
   {
    yield return SelectorMove(pad,title.characterMenu,Entries(title.characterMenu)[i].button);
    RequireCharacterPreview(title,i);
    if(!Progression.Allowed(title.characterMenu[i]))
    {
     yield return SelectorPad(pad,GamepadButton.South);
     Require(title.CurrentState is CharacterSelectState,"locked character confirm stays on roster "+i);
    }
   }
   // Re-enter from the main menu with a remembered, locked added character.
   yield return SelectorPad(pad,GamepadButton.East);
   while(!Active(title.mainMenu))yield return null;
   yield return SelectorPad(pad,GamepadButton.South);
   while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
   RequireCharacterPreview(title,Entries(title.characterMenu).FindIndex(e=>e.gameObject==EventSystem.current.currentSelectedGameObject));
   yield return SelectorMove(pad,title.characterMenu,Entries(title.characterMenu)[0].button);
  }
  // Initial valid native selection is Shana; confirm through the input module.
  yield return SelectorPad(pad,GamepadButton.South);
  while(!Active(title.gunMenu)||title.gunMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  SelectorSnapshot(title,"weapon-controller-before");
  var guns=Entries(title.gunMenu);var cannon=guns[NativeSelection.GunIndex].button;
  if(Environment.GetEnvironmentVariable("MODERNIZATION_SELECTOR_REMEMBERED")=="1")
  {
   bool remembers=(bool)AccessTools.Field(typeof(flanne.UIExtensions.DataListMenu<GunData>),"saveLastSelectionToPlayerPrefs").GetValue(title.gunMenu);
   int expected=remembers?NativeSelection.GunIndex:0;
   Require(EventSystem.current.currentSelectedGameObject==guns[expected].gameObject&&SelectorText().Contains(title.gunMenu[expected].description),"initial weapon preview respects native saved-selection policy; persistence="+remembers);
  }
  if(Environment.GetEnvironmentVariable("MODERNIZATION_SELECTOR_BASELINE")!="1")yield return SelectorMove(pad,title.gunMenu,guns[0].button);
  var seen=new HashSet<GameObject>();var queue=new Queue<Selectable>();queue.Enqueue(guns[0].button);
  while(queue.Count>0)
  {
   var button=queue.Dequeue();if(button==null||!seen.Add(button.gameObject))continue;
   foreach(var next in new[]{button.FindSelectableOnLeft(),button.FindSelectableOnRight(),button.FindSelectableOnUp(),button.FindSelectableOnDown()})if(next!=null)queue.Enqueue(next);
  }
  bool reachable=seen.Contains(cannon.gameObject);
  if(!reachable)failures.Add("Charge Cannon is absent from the controller navigation graph");
  // The native grid is six columns. Walk right across row one, then down to
  // its last column. Real device states exercise the event module/navigation.
  for(int i=0;i<5;i++)yield return SelectorPad(pad,GamepadButton.DpadRight);
  yield return SelectorPad(pad,GamepadButton.DpadDown);
  bool focused=EventSystem.current.currentSelectedGameObject==cannon.gameObject;
  if(!focused)failures.Add("native controller cannot reach row two's final weapon");
  var gate=Progression.Gates.Single(g=>g.Data==Plugin.BaseGun);
  if(focused&&!SelectorText().Contains(gate.Goal))failures.Add("focused Charge Cannon does not expose its unlock condition");
  SelectorSnapshot(title,"weapon-controller-final");yield return Capture("weapon-controller-final.png");
  int points=PointsTracker.pts;
  if(focused)
  {
   yield return SelectorPad(pad,GamepadButton.South);
   Require(title.CurrentState is GunSelectState&&!Progression.Allowed(Plugin.BaseGun)&&PointsTracker.pts==points,"native locked weapon confirm preserves ownership, credits and current menu");
  }
  if(Environment.GetEnvironmentVariable("MODERNIZATION_SELECTOR_BASELINE")!="1")
  {
   foreach(var item in NativeSelection.GunItems(title.gunMenu))
   {
    yield return SelectorMove(pad,title.gunMenu,item.GetComponent<Button>());
    Require(SelectorText().Contains(item.data.nameString)&&SelectorText().Contains(item.data.description),"native controller previews weapon and unlock condition "+item.data.name);
   }
   yield return SelectorPad(pad,GamepadButton.East);
   while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
   RequireCharacterPreview(title,0);
   yield return SelectorPad(pad,GamepadButton.South);
   while(!Active(title.gunMenu)||title.gunMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
   Require(EventSystem.current.currentSelectedGameObject==cannon.gameObject&&SelectorText().Contains(gate.Goal),"remembered locked weapon initializes its preview on native re-entry");
   yield return SelectorMove(pad,title.gunMenu,guns[0].button);
   Canvas.ForceUpdateCanvases();
   var rect=(RectTransform)cannon.transform;var canvas=cannon.GetComponentInParent<Canvas>().rootCanvas;
   Vector2 position=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,rect.TransformPoint(rect.rect.center));
   var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},hits);
   Log("selector-pointer",position+" canvas="+canvas.renderMode+" camera="+canvas.worldCamera?.name+" hits="+string.Join(",",hits.Select(hit=>hit.gameObject.name+"/"+hit.gameObject.GetComponentInParent<Button>()?.name)));
   Require(hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==cannon,"pointer raycast reaches the actual added weapon slot");
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=position});yield return null;yield return null;
   Require(SelectorText().Contains(gate.Goal),"actual mouse hover previews Charge Cannon's unlock condition");
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=position}.WithButton(MouseButton.Left));yield return null;
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=position});yield return null;
   Require(title.CurrentState is GunSelectState&&!Progression.Allowed(Plugin.BaseGun)&&PointsTracker.pts==points,"actual pointer click cannot confirm locked Charge Cannon or spend credits");
   yield return Capture("weapon-pointer-locked.png");
   yield return SelectorMove(pad,title.gunMenu,guns[0].button);
   yield return SelectorPad(pad,GamepadButton.South);
   Require(title.CurrentState is ModeSelectState&&Loadout.GunSelection==title.gunMenu[0],"native controller still confirms obtained Revolver into mode selection");
  }
  File.WriteAllLines(Path.Combine(ResultDir,"selector-failures.txt"),failures);
  Require(failures.Count==0,string.Join("; ",failures));
  SetPhase("PASS");Log("complete","Initial/remembered/re-entry character preview, controller browsing all15 characters/12 weapons, native locked guards and obtained confirmation, pointer raycast/hover/click");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(PlayerPrefs),nameof(PlayerPrefs.GetInt),new[]{typeof(string),typeof(int)})]
class SelectorRememberedPreferences
{
 static bool Prefix(string key,ref int __result)
 {
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")!="selectors"||Environment.GetEnvironmentVariable("MODERNIZATION_SELECTOR_REMEMBERED")!="1")return true;
  // Model this isolated profile's persisted added indices before native Awake.
  // The existing fixture suppresses preference writes; the real registry is untouched.
  if(key=="LastCharacterSelected"){__result=14;return false;}
  if(key=="LastGunSelected"){__result=11;return false;}
  return true;
 }
}
