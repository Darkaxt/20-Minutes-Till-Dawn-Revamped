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
using flanne.UI;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 List<GamepadButton> MenuRoute(Transform root,Selectable target)
 {
  var start=EventSystem.current.currentSelectedGameObject?.GetComponent<Selectable>();if(start==null)return null;
  var allowed=new HashSet<Selectable>(root.GetComponentsInChildren<Selectable>(true).Where(x=>x.gameObject.activeInHierarchy&&x.IsInteractable()));
  var paths=new Dictionary<Selectable,List<GamepadButton>>{{start,new List<GamepadButton>()}};var queue=new Queue<Selectable>();queue.Enqueue(start);
  while(queue.Count>0&&!paths.ContainsKey(target))
  {
   var node=queue.Dequeue();var neighbors=new[]{node.FindSelectableOnLeft(),node.FindSelectableOnRight(),node.FindSelectableOnUp(),node.FindSelectableOnDown()};
   var directions=new[]{GamepadButton.DpadLeft,GamepadButton.DpadRight,GamepadButton.DpadUp,GamepadButton.DpadDown};
   for(int i=0;i<4;i++)if(neighbors[i]!=null&&allowed.Contains(neighbors[i])&&!paths.ContainsKey(neighbors[i])){paths[neighbors[i]]=paths[node].Concat(new[]{directions[i]}).ToList();queue.Enqueue(neighbors[i]);}
  }
  return paths.TryGetValue(target,out var result)?result:null;
 }
 IEnumerator MenuMove(Gamepad pad,Transform root,Selectable target)
 {
  var path=MenuRoute(root,target);Require(path!=null,"controller graph reaches "+target.name);
  foreach(var direction in path)yield return SelectorPad(pad,direction);
  Require(EventSystem.current.currentSelectedGameObject==target.gameObject,"real controller reaches "+target.name);
 }
 void MenuSnapshot(Transform root,string label)
 {
  Canvas.ForceUpdateCanvases();var lines=new List<string>{"focus="+SelectorFocus(),SelectorText()};
  foreach(var control in root.GetComponentsInChildren<Selectable>(true))
  {
   var n=control.navigation;var r=control.GetComponent<RectTransform>();
   lines.Add(control.name+" type="+control.GetType().Name+" parent="+control.transform.parent.name+" active="+control.gameObject.activeInHierarchy+" interactable="+control.IsInteractable()+" rect="+r.rect+" pos="+r.anchoredPosition+" navigation="+n.mode+" left="+control.FindSelectableOnLeft()?.name+" right="+control.FindSelectableOnRight()?.name+" up="+control.FindSelectableOnUp()?.name+" down="+control.FindSelectableOnDown()?.name);
   var button=control as Button;if(button!=null)for(int i=0;i<button.onClick.GetPersistentEventCount();i++)lines.Add("  persistent="+button.onClick.GetPersistentTarget(i)?.GetType().Name+"."+button.onClick.GetPersistentMethodName(i));
  }
  foreach(var rect in root.GetComponentsInChildren<RectTransform>(true).Where(x=>x.GetComponent<Selectable>()==null&&x.parent==root))lines.Add("child "+rect.name+" pos="+rect.anchoredPosition+" size="+rect.rect+" components="+string.Join(",",rect.GetComponents<Component>().Select(c=>c.GetType().Name)));
  File.WriteAllLines(Path.Combine(ResultDir,label+".txt"),lines);
 }
 IEnumerator CheckControllerMenus(TitleScreenController title)
 {
  SetPhase("controller-modes-and-native-options");var pad=InputSystem.AddDevice<Gamepad>("RevampedMenuController");
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
  while(title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  var options=Entries(title.mainMenu).Single(x=>x.GetComponentInChildren<TMP_Text>(true).text=="Options").button;
  yield return SelectorMove(pad,title.mainMenu,options);yield return SelectorPad(pad,GamepadButton.South);
  while(!Active(title.optionsMenu)||title.optionsMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  MenuSnapshot(title.optionsMenu.transform,"title-options-controller");yield return Capture("title-options-controller.png");
  Log("reproduced-floating-settings",!StatsOverlay.Instance.SettingButton.transform.IsChildOf(title.optionsMenu.transform));
  yield return SelectorPad(pad,GamepadButton.East);while(!Active(title.mainMenu))yield return null;
  MenuSnapshot(title.mainMenu.transform,"main-menu-controller");
  var achievements=Entries(title.mainMenu).Single(x=>x.name=="RevampedAchievements").button;
  bool achievementsReachable=MenuRoute(title.mainMenu.transform,achievements)!=null;
  Log("audit-achievements-route",achievementsReachable);
  if(achievementsReachable)
  {
   yield return MenuMove(pad,title.mainMenu.transform,achievements);yield return SelectorPad(pad,GamepadButton.South);
   var view=Object.FindObjectOfType<SourceAchievementMenu>();Require(view!=null&&view.gameObject.activeInHierarchy,"controller opens Achievements");
   MenuSnapshot(view.transform,"achievements-controller");
   foreach(var control in view.GetComponentsInChildren<Button>(true).Where(x=>x.gameObject.activeInHierarchy&&x.IsInteractable()))Log("audit-achievement-control",control.name+" reachable="+(MenuRoute(view.transform,control)!=null));
   var next=view.GetComponentsInChildren<Button>(true).Single(x=>x.name=="AchievementNext");
   if(next.IsInteractable()){yield return MenuMove(pad,view.transform,next);yield return SelectorPad(pad,GamepadButton.South);Require(view.GetComponentsInChildren<TMP_Text>().Any(x=>x.name=="AchievementPage"&&x.text.StartsWith("2")),"controller advances achievement page");}
   yield return SelectorPad(pad,GamepadButton.East);Require(!view.gameObject.activeSelf,"controller cancels Achievements");
  }
  yield return SelectorMove(pad,title.mainMenu,Entries(title.mainMenu)[0].button);yield return SelectorPad(pad,GamepadButton.South);
  while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  yield return SelectorMove(pad,title.characterMenu,Entries(title.characterMenu)[0].button);yield return SelectorPad(pad,GamepadButton.South);
  while(!Active(title.gunMenu)||title.gunMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  yield return SelectorMove(pad,title.gunMenu,Entries(title.gunMenu)[0].button);yield return SelectorPad(pad,GamepadButton.South);
  while(!(title.CurrentState is ModeSelectState)||title.modeSelectPanel.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  MenuSnapshot(title.modeSelectPanel.transform,"campaign-modes-controller");
  foreach(var toggle in ChallengeMode.Toggles(title.modeSelectMenu))Log("mode-controller-route",toggle.name+" data="+toggle.GetComponent<MapEntry>().data.name+" reachable="+(MenuRoute(title.modeSelectPanel.transform,toggle)!=null));
  yield return Capture("campaign-modes-controller.png");
  // The normal route to Custom is the reproduced defect above. Select only
  // this fixture's toggle to inspect its downstream controls separately.
  Log("fixture-precondition","Set the isolated Custom toggle to audit downstream input; this does not prove its normal entry route works.");
  ChallengeMode.Toggles(title.modeSelectMenu).Single(t=>t.GetComponent<MapEntry>().data==CustomMode.MenuMap).isOn=true;
  yield return MenuMove(pad,title.modeSelectPanel.transform,title.modeSelectStartButton);yield return SelectorPad(pad,GamepadButton.South);
  while(CustomMenu.Instance==null)yield return null;
  var custom=CustomMenu.Instance;MenuSnapshot(custom.transform,"custom-controls-controller");
  var controls=(List<Selectable>)AccessTools.Field(typeof(CustomMenu),"controls").GetValue(custom);
  TMP_InputField field=null;
  // Follow the actual finite traversal ring forward. A field consuming input
  // is an audit result, rather than an assertion that aborts the inventory.
  foreach(var control in controls)
  {
   field=EventSystem.current.currentSelectedGameObject?.GetComponent<TMP_InputField>();if(field!=null)break;
   string before=SelectorFocus();yield return SelectorPad(pad,GamepadButton.DpadRight);
   Log("audit-custom-traversal",before+" -> "+SelectorFocus());
   if(SelectorFocus()==before)break;
  }
  field=EventSystem.current.currentSelectedGameObject?.GetComponent<TMP_InputField>();
  Log("audit-custom-field","selected="+SelectorFocus()+" field="+(field!=null)+" editing="+(field!=null&&field.isFocused));
  if(field!=null)
  {
   yield return SelectorPad(pad,GamepadButton.DpadRight);
   Log("audit-custom-after-right","selected="+SelectorFocus()+" editing="+field.isFocused);
   yield return SelectorPad(pad,GamepadButton.South);yield return SelectorPad(pad,GamepadButton.DpadDown);
   Log("audit-custom-after-submit-down","selected="+SelectorFocus()+" editing="+field.isFocused);
  }
  MenuSnapshot(custom.transform,"custom-after-field-controller");
  yield return SelectorPad(pad,GamepadButton.East);
  Log("audit-custom-after-cancel","menuOpen="+(CustomMenu.Instance!=null)+" selected="+SelectorFocus());
  Log("diagnostic-complete","Read-only controller audit: no production correction or fixed-feature acceptance claim.");Application.Quit(0);
 }
}
