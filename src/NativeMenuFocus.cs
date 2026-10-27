using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.Users;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using Panel=flanne.UI.Panel;
using Menu=flanne.UIExtensions.Menu;
namespace TwentyMinutes.Modernization;

// Focus belongs to the menu which opened it, not to unrelated Selectables in
// the same Canvas. Restore on controller input/lifecycle events, never a timer
// or a frame-by-frame override of valid pointer/controller selection.
public sealed class NativeMenuFocus:MonoBehaviour
{
 static readonly List<NativeMenuFocus> open=new List<NativeMenuFocus>();
 static InputAction move,submit,cancel;
 Selectable first,last;Transform scope;
 static NativeMenuFocus(){InputUser.onChange+=Changed;}
 internal static void Open(Component owner,Selectable first,Transform scope=null)
 {
  if(first==null)return;
  var focus=owner.GetComponent<NativeMenuFocus>()??owner.gameObject.AddComponent<NativeMenuFocus>();
  focus.first=first;focus.last=first;focus.scope=scope??owner.transform;open.Remove(focus);open.Add(focus);BindInput();
  if(Gamepad.current!=null)focus.Select();
 }
 internal static void Close(Component owner){var focus=owner.GetComponent<NativeMenuFocus>();if(focus!=null)open.Remove(focus);}
 void OnDisable(){open.Remove(this);}
 void OnDestroy(){open.Remove(this);}
 // Some native menus are composed of sibling roots: the roster grid and the
 // shared action/background panel. Both belong to the same navigation owner.
 IEnumerable<Selectable> Controls()=>GetComponentsInChildren<Selectable>().Concat(scope.GetComponentsInChildren<Selectable>()).Distinct();
 bool Valid(Selectable control)=>control!=null&&control.gameObject.activeInHierarchy&&control.IsInteractable()&&(control.transform.IsChildOf(transform)||control.transform.IsChildOf(scope));
 void Select()
 {
  var selected=EventSystem.current?.currentSelectedGameObject?.GetComponent<Selectable>();
  if(Valid(selected)){last=selected;return;}
  var target=Valid(last)?last:Valid(first)?first:Controls().FirstOrDefault(Valid);
  if(target!=null){Plugin.Trace("menu-focus-restored",new{owner=name,scope=scope.name,from=selected?.name,to=target.name});target.Select();}
 }
 static void SelectTop()
 {
  var focus=open.LastOrDefault(f=>f!=null&&f.gameObject.activeInHierarchy&&f.scope!=null&&f.Controls().Any(f.Valid));focus?.Select();
 }
 static void Changed(InputUser user,InputUserChange change,InputDevice device)
 {if(change==InputUserChange.ControlSchemeChanged&&user.controlScheme?.name=="Gamepad")SelectTop();}
 static void ControllerInput(InputAction.CallbackContext context){if(context.control?.device is Gamepad)SelectTop();}
 static void BindInput()
 {
  var module=EventSystem.current?.currentInputModule as InputSystemUIInputModule;if(module==null||module.move?.action==move)return;
  foreach(var action in new[]{move,submit,cancel})if(action!=null)action.performed-=ControllerInput;
  move=module.move?.action;submit=module.submit?.action;cancel=module.cancel?.action;
  foreach(var action in new[]{move,submit,cancel})if(action!=null)action.performed+=ControllerInput;
 }
}
[HarmonyPatch(typeof(Panel),nameof(Panel.Show))]
class NativePanelFocus
{
 static bool Prefix(Panel __instance)
 {
  var game=RunRecorder.Instance?.GetComponent<GameController>();
  // Both original death and ported victory show this summary. It is redundant
  // with the in-run HUD and offers no useful end-of-run navigation.
  return game==null||__instance!=game.powerupListUI||!(game.CurrentState is PlayerDeadState||game.CurrentState is PlayerSurvivedState);
 }
 static void Postfix(Panel __instance)
 {
  if(!__instance.interactable)return;
  Transform scope=__instance.transform;var title=Progression.Title;var game=RunRecorder.Instance?.GetComponent<GameController>();
  // Choice grids and their action buttons can be siblings. Their visible
  // panel owns navigation; the grid component only supplies the first choice.
  if(title!=null&&__instance.gameObject==title.modeSelectMenu.gameObject)scope=title.modeSelectPanel.transform;
  if(game!=null&&__instance.gameObject==game.powerupMenu.gameObject)scope=game.powerupMenuPanel.transform;
  if(game!=null&&__instance.gameObject==game.devilDealMenu.gameObject)scope=game.devilDealMenuPanel.transform;
  NativeMenuFocus.Open(__instance,(Selectable)AccessTools.Field(typeof(Panel),"gamepadDefaultSelectable").GetValue(__instance),scope);
 }
}
[HarmonyPatch(typeof(Panel),nameof(Panel.Hide))]
class NativePanelFocusClose{static void Prefix(Panel __instance)=>NativeMenuFocus.Close(__instance);}
[HarmonyPatch(typeof(Menu),nameof(Menu.Show))]
class NativeTitleMenuFocus
{
 static void Postfix(Menu __instance)
 {
  var entries=NativeSelection.GunEntriesFor(__instance);int index=(int)AccessTools.Field(typeof(Menu),"index").GetValue(__instance);
  var first=index<entries.Count&&entries[index].button.IsInteractable()?entries[index].button:entries.Select(e=>e.button).FirstOrDefault(b=>b.gameObject.activeInHierarchy&&b.IsInteractable());
  var title=Progression.Title;var scope=title!=null&&(__instance==title.characterMenu||__instance==title.gunMenu)?title.selectPanel.transform:__instance.transform;
  NativeMenuFocus.Open(__instance,first,scope);
 }
}
[HarmonyPatch(typeof(Menu),nameof(Menu.Hide))]
class NativeTitleMenuFocusClose{static void Prefix(Menu __instance)=>NativeMenuFocus.Close(__instance);}
[HarmonyPatch(typeof(flanne.UI.EndScreenUIC),nameof(flanne.UI.EndScreenUIC.Show))]
class NativeResultsNavigation
{
 static void Prefix()
 {
  var game=RunRecorder.Instance.GetComponent<GameController>();var controls=new[]{game.retryRunButton,game.quitToTitleButton}.Where(b=>b.gameObject.activeSelf).ToArray();
  foreach(var button in controls){var other=controls.FirstOrDefault(b=>b!=button);button.navigation=new Navigation{mode=Navigation.Mode.Explicit,selectOnLeft=other,selectOnRight=other,selectOnUp=other,selectOnDown=other};}
 }
}
