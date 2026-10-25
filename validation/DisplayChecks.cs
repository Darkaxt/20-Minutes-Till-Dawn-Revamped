using System;
using System.Collections;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.U2D;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using flanne.UI;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 static ConfigEntry<int> CameraZoom=>(ConfigEntry<int>)AccessTools.Field(typeof(Plugin),"ZoomOut").GetValue(Plugin.Instance);
 static Button DisplayButton(flanne.UIExtensions.Menu menu,string name)=>menu.GetComponentsInChildren<Button>(true).Single(b=>b.name==name);
 IEnumerator WaitDisplay(int width,int height)
 {while(Screen.width!=width||Screen.height!=height)yield return null;yield return null;Log("actual-viewport",Screen.width+"x"+Screen.height);}
 IEnumerator CheckDisplay(TitleScreenController title,string mode)
 {
  SetPhase("display-native-options");var pad=InputSystem.AddDevice<Gamepad>("RevampedDisplayController");
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
  bool fresh=mode=="display-relaunch";
  if(!fresh){PlayerPrefs.DeleteKey("ResolutionIndex");PlayerPrefs.SetInt("Fullscreen",0);Screen.SetResolution(1024,768,false);yield return WaitDisplay(1024,768);}
  yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu).Single(e=>e.button.GetComponentInChildren<TMP_Text>().text=="Options").button);
  while(!Active(title.optionsMenu))yield return null;
  if(!fresh)
  {
   yield return PadClick(pad,title.optionsMenu.transform,DisplayButton(title.optionsMenu,"Fullscreen"));
   Require(DisplayModeBoundary.Fullscreen,"controller requests fullscreen without switching desktop");
   MenuSnapshot(title.optionsMenu.transform,"fullscreen-resolution");
   Require(DisplayButton(title.optionsMenu,"Resolution").IsInteractable(),"native Resolution remains usable in fullscreen");
   yield return PadClick(pad,title.optionsMenu.transform,DisplayButton(title.optionsMenu,"Resolution"));yield return WaitDisplay(1200,675);
   Require(DisplayModeBoundary.Fullscreen&&PlayerPrefs.GetInt("ResolutionIndex")==1,"controller selects1200x675 while preserving fullscreen request");
  }
  else Require(PlayerPrefs.GetInt("ResolutionIndex")==1&&PlayerPrefs.GetInt("Fullscreen")==1&&CameraZoom.Value==50,"display and zoom persist across process restart");
  var zoom=DisplayButton(title.optionsMenu,"ZoomSetting");
  if(!fresh)
  {
   while(CameraZoom.Value!=0)yield return PadClick(pad,title.optionsMenu.transform,zoom);
   yield return PadClick(pad,title.optionsMenu.transform,zoom);Require(CameraZoom.Value==25,"controller changes native zoom once");
   yield return MenuPointer(zoom);Require(CameraZoom.Value==50,"pointer changes native zoom once");
  }
  Require(zoom.GetComponentInChildren<TMP_Text>().text.Contains("50%"),"native row describes saved zoom");
  yield return Capture("display-title-options.png");yield return MenuMove(pad,title.optionsMenu.transform,zoom);yield return SelectorPad(pad,GamepadButton.East);
  while(!Active(title.mainMenu))yield return null;
  RunResume.Invalidate();yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);
  while(!Active(title.characterMenu))yield return null;
  yield return PadClick(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,Entries(title.characterMenu)[0].button);
  while(!Active(title.gunMenu))yield return null;yield return PadClick(pad,title.gunMenu.transform,Entries(title.gunMenu)[0].button);
  while(!(title.CurrentState is ModeSelectState))yield return null;
  yield return MenuMove(pad,title.modeSelectPanel.transform,ChallengeMode.Toggles(title.modeSelectMenu)[0]);yield return SelectorPad(pad,GamepadButton.South);
  yield return PadClick(pad,title.modeSelectPanel.transform,title.modeSelectStartButton);
  while(!(title.CurrentState is MapSelectState))yield return null;
  var maps=(Toggle[])AccessTools.Field(typeof(ToggleGroupMenu<MapData>),"toggles").GetValue(title.mapSelectMenu);
  var forest=maps.Single(t=>t.GetComponent<MapEntry>().data.name=="20M_Forest");
  yield return MenuMove(pad,title.mapSelectMenu.GetComponentInParent<Canvas>().rootCanvas.transform,forest);yield return SelectorPad(pad,GamepadButton.South);
  yield return PadClick(pad,title.mapSelectMenu.GetComponentInParent<Canvas>().rootCanvas.transform,title.mapSelectStartButton);
  GameController game;while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
  GameTimer.SharedInstance.Stop();StopFixtureSpawning();
  var cameras=Resources.FindObjectsOfTypeAll<PixelPerfectCamera>().Where(c=>c.gameObject.scene==game.gameObject.scene&&new[]{"Main Camera","UICam","VisibleInFogCamera"}.Contains(c.name)).ToArray();Require(cameras.Length==3,"three original world/fog/UI camera roles");
  var baseline=cameras.Select(c=>{AccessTools.Method(typeof(PixelPerfectCamera),"LateUpdate").Invoke(c,null);AccessTools.Method(typeof(PixelPerfectCamera),"OnPreCull").Invoke(c,null);return c.GetComponent<Camera>().orthographicSize/1.5f;}).ToArray();
  Require(cameras.All(c=>c.refResolutionY==350),"zoom retains the original Forest source reference height");
  var hud=StatsOverlay.Instance.Panel.GetComponent<RectTransform>();var hudSize=hud.rect.size;
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return null;
  yield return PadClick(pad,game.pauseMenu.transform,game.optionsButton);
  var options=game.optionsMenu;
  while(!Active(options))yield return null;zoom=DisplayButton(options,"ZoomSetting");
  if(!fresh)
  {
   foreach(int percent in new[]{75,100,0,25,50})
   {
    yield return PadClick(pad,options.transform,zoom);Require(CameraZoom.Value==percent,"native zoom cycles to "+percent);
    for(int i=0;i<cameras.Length;i++)
    {
     AccessTools.Method(typeof(PixelPerfectCamera),"LateUpdate").Invoke(cameras[i],null);
     AccessTools.Method(typeof(PixelPerfectCamera),"OnPreCull").Invoke(cameras[i],null);
     float expected=baseline[i]*(1+percent/100f);
     Require(Mathf.Abs(cameras[i].GetComponent<Camera>().orthographicSize-expected)<.0001f,"native camera projection scales once: "+cameras[i].name+" at"+percent);
     AccessTools.Method(typeof(PixelPerfectCamera),"OnPreCull").Invoke(cameras[i],null);
     Require(Mathf.Abs(cameras[i].GetComponent<Camera>().orthographicSize-expected)<.0001f,"repeated pre-cull never accumulates zoom");
    }
    Require(hud.rect.size==hudSize,"zoom preserves HUD dimensions");
   }
   // Exclusive fullscreen is intentionally not entered in this host fixture.
   // Exercise its windowed setter directly, then the real pointer toggle from
   // the actual windowed host state. Do not spoof Unity's inlined native getter.
   AccessTools.Method(typeof(OptionsSetter),"SetFullscreen").Invoke(options.GetComponent<OptionsSetter>(),new object[]{false});
   Require(!DisplayModeBoundary.Fullscreen,"native windowed setter retains selected resolution");yield return WaitDisplay(1200,675);
   yield return MenuPointer(DisplayButton(options,"Fullscreen"));Require(DisplayModeBoundary.Fullscreen,"pointer restores fullscreen with selected resolution");yield return WaitDisplay(1200,675);
  }
  yield return Capture("display-pause-options.png");yield return MenuMove(pad,options.transform,zoom);yield return SelectorPad(pad,GamepadButton.East);
  while(!(game.CurrentState is PauseState))yield return null;
  yield return PadClick(pad,game.pauseMenu.transform,game.pauseResumeButton);
  while(!(game.CurrentState is CombatState))yield return null;
  foreach(var size in new[]{new Vector2Int(1024,768),new Vector2Int(1280,720)})
  {
   Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);
   foreach(int percent in new[]{0,50})
   {
    CameraZoom.Value=percent;
    foreach(var pp in cameras)
    {
     Require(pp.refResolutionX==((int)Math.Floor((float)size.x/size.y*350)&~1),"camera reference width follows actual resize: "+pp.name);
     AccessTools.Method(typeof(PixelPerfectCamera),"LateUpdate").Invoke(pp,null);AccessTools.Method(typeof(PixelPerfectCamera),"OnPreCull").Invoke(pp,null);
     Log("world-projection",pp.name+" zoom="+percent+" ortho="+pp.GetComponent<Camera>().orthographicSize+" ref="+pp.refResolutionX+"x"+pp.refResolutionY);
    }
    yield return Capture("world-"+size.x+"-zoom"+percent+".png");
   }
  }
  var player=PlayerController.Instance;
  while(!(player.CurrentState is flanne.Player.IdleState)||!player.ammo.fullOnAmmo)yield return null;
  int ammo=player.ammo.amount;
  InputSystem.QueueStateEvent(pad,new GamepadState{rightStick=Vector2.right,rightTrigger=1});
  while(player.ammo.amount==ammo)yield return null;
  InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;Require(player.ammo.amount<ammo,"native controller aim and fire work while zoomed out");
  Plugin.Instance.Config.Save();PlayerPrefs.Save();
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return null;
  yield return PadClick(pad,game.pauseMenu.transform,game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="ResumeSaveAndReturn"));
  while((title=Object.FindObjectOfType<TitleScreenController>())==null||!Active(title.mainMenu))yield return null;
  yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu).Single(e=>e.button.GetComponentInChildren<TMP_Text>().text=="Options").button);
  while(!Active(title.optionsMenu))yield return null;
  Require(DisplayButton(title.optionsMenu,"ZoomSetting").GetComponentInChildren<TMP_Text>().text.Contains("50%"),"zoom survives native return to title");
  Require(DisplayButton(title.optionsMenu,"Resolution").IsInteractable(),"resolution remains controller accessible on scene re-entry");
  SetPhase("PASS");Log("complete","Native display/zoom controller and pointer, projection alignment/no accumulation, resize4:3/16:9, aim/fire, scene transitions and persistence");Application.Quit(0);
 }
}
// Simulate only the window-system fullscreen boundary: retain real rendering and
// actual resolutions, never change the user's desktop display mode.
[HarmonyPatch(typeof(Screen),nameof(Screen.SetResolution),new[]{typeof(int),typeof(int),typeof(bool)})]
class DisplayModeBoundary
{
 internal static bool Fullscreen;
 static void Prefix(ref bool fullscreen){string mode=Environment.GetEnvironmentVariable("MODERNIZATION_TEST");if(!mode.StartsWith("display-")&&!mode.StartsWith("hud-"))return;Fullscreen=fullscreen;RuntimeChecks.Log("requested-display-mode",fullscreen);fullscreen=false;}
}
