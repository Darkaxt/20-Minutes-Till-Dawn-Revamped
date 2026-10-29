using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.TitleScreen;
using flanne.UI;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 string HeaderPath(Transform value)=>value.parent==null?value.name:HeaderPath(value.parent)+"/"+value.name;
 IEnumerator HeaderReady(TitleScreenController title,flanne.UIExtensions.Menu menu)
 {
  var tab=(RectTransform)title.runesButton.transform.parent;var transition=tab.GetComponent<flanne.UIExtensions.MoveTransition>();
  while(!Active(menu)||!menu.interactable||menu.GetComponent<CanvasGroup>().alpha<.999f||title.selectPanel.GetComponent<CanvasGroup>().alpha<.999f||(title.runeMenuPanel.gameObject.activeInHierarchy&&title.runeMenuPanel.GetComponent<CanvasGroup>().alpha>.001f)||Vector3.SqrMagnitude(tab.anchoredPosition3D-(Vector3)WindField(transition,"_startPosition"))>.001f)yield return null;
  Canvas.ForceUpdateCanvases();
 }
 void HeaderContract(TitleScreenController title)
 {
  var badge=title.selectPanel.GetComponentInChildren<PointsUI>(true);Require(badge!=null,"native shared header has its exact PointsUI owner");
  Log("header-show-owner",title.selectPanel.GetType().GetMethod("Show").DeclaringType.FullName+" badgeActive="+badge.gameObject.activeSelf);
  Require(!badge.gameObject.activeSelf&&badge.GetComponentsInChildren<Graphic>(true).All(g=>!g.gameObject.activeInHierarchy),"obsolete selection currency amount/icon/background are absent");
  Require(title.runesButton.gameObject.activeInHierarchy&&title.runesButton.IsInteractable()&&title.loadoutBackButton.IsInteractable(),"shared Runes and Back controls remain visible and usable");
  var tab=(RectTransform)title.runesButton.transform.parent;var old=(RectTransform)badge.transform;
  CombatClose(tab.anchoredPosition.x+tab.rect.xMin,old.anchoredPosition.x+old.rect.xMin,"complete Runes tab aligns with the selector's original left header inset");
  Require(title.checkRunesPromptArrow.transform.IsChildOf(tab),"native rune prompt moves with the complete tab");
  var label=title.runesButton.GetComponentInChildren<TMPro.TMP_Text>(true);var corners=new Vector3[4];label.rectTransform.GetWorldCorners(corners);
  Log("header-render-state",title.CurrentState.GetType().Name+"/"+Screen.width+" root="+tab.anchoredPosition+" textColor="+label.color+" rendererColor="+label.canvasRenderer.GetColor()+" inheritedAlpha="+label.canvasRenderer.GetInheritedAlpha()+" cull="+label.canvasRenderer.cull+" corners="+string.Join(";",corners.Select(v=>v.ToString())));
 }
 IEnumerator CheckRosterHeader(TitleScreenController title,bool probe)
 {
  RunResume.Invalidate();Click(Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||!title.characterMenu.interactable)yield return null;
  if(probe)
  {
   yield return HeaderReady(title,title.characterMenu);
   foreach(var target in new[]{title.selectPanel.GetComponentInChildren<PointsUI>(true).transform,title.runesButton.transform.parent})
   {
    var rect=(RectTransform)target;Log("header-layout",HeaderPath(target)+" position="+rect.anchoredPosition+" anchor="+rect.anchorMin+"/"+rect.anchorMax+" pivot="+rect.pivot);
    foreach(var transition in target.GetComponents<MonoBehaviour>().Where(c=>c.GetType().Name.Contains("Transition")))
    foreach(var field in transition.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))Log("header-transition-field",target.name+"/"+transition.GetType().FullName+"/"+field.Name+"="+field.GetValue(transition));
   }
   foreach(var field in typeof(TitleScreenController).GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
   {var value=field.GetValue(title);if(value is Component component)Log("title-component-field",field.Name+"="+component.GetType().FullName+":"+HeaderPath(component.transform));else if(value is GameObject go)Log("title-object-field",field.Name+"="+HeaderPath(go.transform));}
   foreach(var component in Resources.FindObjectsOfTypeAll<MonoBehaviour>().Where(c=>c.gameObject.scene.IsValid()&&(c.GetType().Name.IndexOf("Point",StringComparison.OrdinalIgnoreCase)>=0||c.name.IndexOf("Point",StringComparison.OrdinalIgnoreCase)>=0)))
   {Log("currency-component",component.GetType().FullName+":"+HeaderPath(component.transform)+" active="+component.gameObject.activeInHierarchy);foreach(var child in component.GetComponentsInChildren<Transform>(true))Log("currency-child",HeaderPath(child)+" active="+child.gameObject.activeSelf);}
   yield return Capture("character-header-before.png");
   Click(Entries(title.characterMenu)[0].button);while(!Active(title.gunMenu)||!title.gunMenu.interactable)yield return null;yield return Capture("weapon-header-before.png");
   SetPhase("PASS");Log("complete","Actual selection-header ownership diagnostic; not acceptance");Application.Quit(0);yield break;
  }
  var pad=InputSystem.AddDevice<Gamepad>("RosterHeaderController");int credits=PointsTracker.pts;string ownership=Ownership();
  yield return HeaderReady(title,title.characterMenu);HeaderContract(title);
  // Cover both rune entry points, each viewport's character/weapon round-trip,
  // then leave to the title and verify a fresh selector entry.
  {
   yield return PadClick(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,title.runesButton);
   while(!(title.CurrentState is RuneMenuCharacterState)||!title.runeConfirmButton.IsInteractable()||title.runeMenuPanel.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
   Require(!title.selectPanel.GetComponentInChildren<PointsUI>(true).gameObject.activeSelf,"currency badge stays absent behind character rune menu");
   yield return PadClick(pad,title.runeMenuPanel.transform,title.runeConfirmButton);yield return HeaderReady(title,title.characterMenu);HeaderContract(title);
   yield return PadClick(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,Entries(title.characterMenu)[0].button);yield return HeaderReady(title,title.gunMenu);HeaderContract(title);
   yield return PadClick(pad,title.gunMenu.GetComponentInParent<Canvas>().rootCanvas.transform,title.runesButton);
   while(!(title.CurrentState is RuneMenuGunState)||!title.runeConfirmButton.IsInteractable()||title.runeMenuPanel.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
   yield return MenuPointer(title.runeConfirmButton);yield return HeaderReady(title,title.gunMenu);yield return SelectorPad(pad,GamepadButton.DpadUp);HeaderContract(title);
   {
    // Each aspect ratio starts a fresh native player at its actual resolution.
    // Do not mix runtime resizing with batchmode's absent automatic camera render.
    foreach(var viewport in new[]{new Vector2Int(Screen.width,Screen.height)})
    {
     yield return MenuMove(pad,title.gunMenu.GetComponentInParent<Canvas>().rootCanvas.transform,Entries(title.gunMenu)[0].button);
     yield return HeaderReady(title,title.gunMenu);
     HeaderContract(title);
     yield return Capture("weapon-header-"+viewport.x+".png");
     yield return PadClick(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,title.loadoutBackButton);yield return HeaderReady(title,title.characterMenu);HeaderContract(title);yield return Capture("character-header-"+viewport.x+".png");
     yield return PadClick(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,Entries(title.characterMenu)[0].button);yield return HeaderReady(title,title.gunMenu);
    }
   }
   yield return PadClick(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,title.loadoutBackButton);yield return HeaderReady(title,title.characterMenu);
   yield return MenuPointer(title.loadoutBackButton);while(!Active(title.mainMenu)||!title.mainMenu.interactable)yield return null;
   yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);yield return HeaderReady(title,title.characterMenu);HeaderContract(title);
  }
  Require(PointsTracker.pts==credits&&Ownership()==ownership,"header correction and navigation preserve credits and all progression ownership");
  SetPhase("PASS");Log("complete","Actual selection badge absent across character/weapon/rune menus, controller/pointer/re-entry at "+Screen.width+"x"+Screen.height+"; wallet/progression unchanged");Application.Quit(0);
 }
}
