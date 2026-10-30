using System;
using System.Linq;
using System.Collections;
using System.IO;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static void PoiCameraProbe(Camera camera,string phase)
 {
  var target=GameObject.Find("PoiTrackingFixture");if(target==null)return;var marker=target.GetComponent<ChestDirectionIndicator>();var world=target.transform.position;
  Log("camera-probe",phase+" camera="+camera.name+" rect="+camera.pixelRect+" aspect="+camera.aspect+" size="+camera.orthographicSize+" texture="+(camera.targetTexture==null?"none":camera.targetTexture.width+"x"+camera.targetTexture.height)+" screen="+camera.WorldToScreenPoint(world)+" viewport="+camera.WorldToViewportPoint(world)+" marker="+(marker.Marker==null?"none":RectTransformUtility.WorldToScreenPoint(null,marker.Marker.position).ToString()));
 }
 IEnumerator ProbePoiProjection()
 {
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  var target=new GameObject("PoiTrackingFixture",typeof(SpriteRenderer),typeof(ChestDirectionIndicator));var marker=target.GetComponent<ChestDirectionIndicator>();marker.Icon=(Sprite)WindField(Resources.FindObjectsOfTypeAll<flanne.Pickups.ChestPickup>().First(c=>!c.gameObject.scene.IsValid()),"chestClosed");target.GetComponent<SpriteRenderer>().sprite=marker.Icon;
  target.transform.position=PlayerController.Instance.transform.position+new Vector3(1,2,0);
  yield return null;yield return new WaitForEndOfFrame();PoiCameraProbe(Camera.main,"end-frame");yield return PoiCapture("projection-probe.png");
  SetPhase("DIAGNOSTIC_COMPLETE");Application.Quit(0);
 }
 IEnumerator PoiOpacityMenu()
 {
  var title=Progression.Title;var pad=InputSystem.AddDevice<Gamepad>("PoiPulseController");InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
  while(title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu).Single(e=>e.button.GetComponentInChildren<TMP_Text>().text=="Options").button);
  while(!Active(title.optionsMenu)||title.optionsMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  var clock=title.optionsMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="ClockOpacitySetting");Require((int)Plugin.Instance.ClockOpacity.DefaultValue==100,"clock opacity defaults100%");Plugin.Instance.ClockOpacity.Value=100;
  for(int i=0;i<4;i++){int before=Plugin.Instance.ClockOpacity.Value;yield return PadClick(pad,title.optionsMenu.transform,clock);Require(Plugin.Instance.ClockOpacity.Value==before%100+25,"native controller cycles clock opacity: "+Plugin.Instance.ClockOpacity.Value);}
  yield return MenuPointer(clock);Require(Plugin.Instance.ClockOpacity.Value==25,"native pointer selects clock25%");yield return Capture("timer-score-options.png");
  Plugin.Instance.Config.Save();bool save=Plugin.Instance.Config.SaveOnConfigSet;Plugin.Instance.Config.SaveOnConfigSet=false;
  try{Plugin.Instance.ClockOpacity.Value=100;Plugin.Instance.Config.Reload();Require(Plugin.Instance.ClockOpacity.Value==25,"real saved clock opacity reloads");}finally{Plugin.Instance.Config.SaveOnConfigSet=save;}
  yield return SelectorPad(pad,GamepadButton.East);while(!Active(title.mainMenu))yield return null;
 }
 IEnumerator PoiTracking(GameController game)
 {
  var player=PlayerController.Instance;var north=new GameObject("PoiTrackingFixture",typeof(SpriteRenderer),typeof(ChestDirectionIndicator));var marker=north.GetComponent<ChestDirectionIndicator>();
  var source=Resources.FindObjectsOfTypeAll<flanne.Pickups.ChestPickup>().First(c=>!c.gameObject.scene.IsValid());marker.Icon=(Sprite)WindField(source,"chestClosed");north.GetComponent<SpriteRenderer>().sprite=marker.Icon;
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(720,720)})
  {
   Screen.SetResolution(size.x,size.y,false);while(Screen.width!=size.x||Screen.height!=size.y)yield return null;Canvas.ForceUpdateCanvases();
   foreach(float x in new[]{-1f,-.01f,0f,.01f,1f})
   {
    north.transform.position=player.transform.position+new Vector3(x,100,0);FeedbackCameras();yield return null;yield return new WaitForEndOfFrame();
    var safe=Screen.safeArea;float scale=((RectTransform)StatsOverlay.Instance.Panel.transform).lossyScale.x;var edge=Rect.MinMaxRect(safe.xMin+56*scale,safe.yMin+84*scale,safe.xMax-56*scale,safe.yMax-84*scale);
    var expected=PickupDirectionLayout.EdgePoint((Vector2)Camera.main.WorldToScreenPoint(north.transform.position)-safe.center,edge);var screen=RectTransformUtility.WorldToScreenPoint(null,marker.Marker.position);
    Log("marker-position",size+"/"+x+" actual="+screen+" ray="+expected);
    // The native PixelPerfectCamera quantizes its render viewport after
    // LateUpdate. Permit one four-screen-pixel native texel, never HUD slots.
    Require(marker.Marker.gameObject.activeInHierarchy&&Vector2.Distance(screen,expected)<=4,"native north marker follows its ray within one rendered texel: "+size.x+"/"+x);
   }
   foreach(float y in new[]{20f,10f,5f,2f})
   {
    north.transform.position=player.transform.position+new Vector3(1,y,0);FeedbackCameras();yield return null;yield return new WaitForEndOfFrame();var projected=(Vector2)Camera.main.WorldToScreenPoint(north.transform.position);
    Require(marker.Marker.gameObject.activeInHierarchy,"marker persists during chest approach: "+size.x+"/"+y);
    if(y<=2)
    {
     var point=RectTransformUtility.WorldToScreenPoint(null,marker.Marker.position);bool arrow=((RectTransform)WindField(marker,"Arrow")).gameObject.activeSelf;
     Log("on-screen-marker",size+" actual="+point+" projected="+projected+" cameraZ="+Camera.main.WorldToScreenPoint(north.transform.position).z+" arrow="+arrow+" delta="+Vector2.Distance(point,projected));
     Require(Vector2.Distance(point,projected)<=1&&!arrow,"on-screen marker uses final camera projection at actual chest");
    }
   }
   foreach(var axis in new[]{Vector2.right,Vector2.left,Vector2.down,Vector2.up})
   {
    foreach(float distance in new[]{100f,10f,2f})
    {
     north.transform.position=player.transform.position+(Vector3)(axis*distance);FeedbackCameras();yield return null;yield return new WaitForEndOfFrame();
     var projected=Camera.main.WorldToScreenPoint(north.transform.position);var safe=Screen.safeArea;float scale=((RectTransform)StatsOverlay.Instance.Panel.transform).lossyScale.x;var edge=Rect.MinMaxRect(safe.xMin+56*scale,safe.yMin+84*scale,safe.xMax-56*scale,safe.yMax-84*scale);
     bool inside=edge.Contains(projected);var expected=inside?(Vector2)projected:PickupDirectionLayout.EdgePoint((Vector2)projected-safe.center,edge);var actual=RectTransformUtility.WorldToScreenPoint(null,marker.Marker.position);
     Log("all-edge-tracking",size+" "+axis+"/"+distance+" actual="+actual+" expected="+expected);
     Require(marker.Marker.gameObject.activeInHierarchy&&Vector2.Distance(actual,expected)<=1,"all-edge marker tracks real chest projection: "+size+"/"+axis+"/"+distance);
    }
   }
   yield return PoiCapture("poi-tracking-"+size.x+".png");
  }
  north.SetActive(false);yield return null;Require(!marker.Marker.gameObject.activeSelf,"target disable immediately hides its marker");Object.Destroy(north);
  Screen.SetResolution(1280,720,false);while(Screen.width!=1280||Screen.height!=720)yield return null;
  var timer=game.hud.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="Timetext");var score=game.hud.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="RunCredits");var objective=game.hud.GetComponentsInChildren<TMP_Text>(true).Single(t=>t.name=="SurviveText");
  yield return null;yield return new WaitForEndOfFrame();Require(timer.GetComponent<CanvasGroup>().alpha==.25f&&score.GetComponent<CanvasGroup>().alpha==.25f&&objective.GetComponent<CanvasGroup>().alpha==1,"only timer and Score use configured25% alpha");
 }
 IEnumerator CheckPoiPulse(string mode)
 {
  if(mode=="poi-pulse-geometry"){yield return ProbePoiProjection();yield break;}
  if(mode=="poi-pulse-tracking")
  {
   RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
   var trackingGame=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return PoiTracking(trackingGame);yield return AchievementExit(trackingGame,false);SetPhase("PASS");Log("complete","Continuous all-edge target tracking, final display projection and timer/Score alpha verified in native square and16:9 views.");Application.Quit(0);yield break;
  }
  bool continuation=mode.EndsWith("continue")||mode.EndsWith("final"),inspect=mode.EndsWith("inspect");
  if(continuation)yield return CombatBuffContinue();
  else{RunResume.Invalidate();if(!inspect&&mode!="poi-pulse-track")yield return PoiOpacityMenu();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());}
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  if(!continuation)
  {
   if(!inspect){yield return PoiTracking(game);yield return BugPickKey(game,"pulsing_summons_name");}
   foreach(string power in new[]{"MagicScythe","MagicSpear"})yield return AndroidPick(game,power,game.playerXP.level);
   if(inspect)yield return BugPickKey(game,"pulsing_summons_name");yield return null;
  }
  var summons=Object.FindObjectsOfType<Summon>().Where(s=>s is StabbingSummon||s is OrbitalWeapon).ToArray();
  Require(summons.Count(s=>s is StabbingSummon)==2&&summons.Any(s=>s is OrbitalWeapon),"actual acquired scythes and two lances exist (Pulsing prerequisites may already include Scythe)");
  SetPhase("waiting-summon-start");while(summons.Any(s=>s.GetComponent<AndroidSummonPulse>()==null))yield return null;
  // Orbit pivots move independently of the Summon component. Detach only
  // fixture actors so the in/out-of-radius samples have a fixed center.
  foreach(var s in Object.FindObjectsOfType<Summon>()){s.transform.SetParent(null,true);s.enabled=false;foreach(var collider in s.GetComponentsInChildren<Collider2D>())collider.enabled=false;}
  foreach(var s in summons)
  {
   Log("pulse-actor",s.name+" "+s.GetType().Name+" active="+s.gameObject.activeInHierarchy+" components="+string.Join(";",s.GetComponents<AndroidSummonPulse>().Select(p=>"enabled="+p.enabled+" timer="+p.Timer+" damage="+p.Damage+" interval="+p.Interval+" range="+p.Range)));
   s.enabled=false;foreach(var collider in s.GetComponentsInChildren<Collider2D>())collider.enabled=false;
  }
  foreach(var pulse in Object.FindObjectsOfType<AndroidSummonPulse>())pulse.enabled=false;
  foreach(var summon in mode.EndsWith("final")?summons.Where(s=>s is OrbitalWeapon).Take(1):summons.AsEnumerable())
  {
   var pulse=summon.GetComponent<AndroidSummonPulse>();Require(pulse!=null&&pulse.Damage==50&&pulse.Interval==2&&pulse.Range==3,"actual acquired pulse configuration: "+summon.name);
   var target=ElementEnemy((Vector2)summon.transform.position+Vector2.up*1);target.GetComponent<MoveComponent2D>().enabled=false;var body=target.GetComponent<Rigidbody2D>();if(body!=null)body.constraints=RigidbodyConstraints2D.FreezeAll;
   var outside=ElementEnemy((Vector2)summon.transform.position+Vector2.up*3.5f);outside.GetComponent<MoveComponent2D>().enabled=false;var outsideBody=outside.GetComponent<Rigidbody2D>();if(outsideBody!=null)outsideBody.constraints=RigidbodyConstraints2D.FreezeAll;
   int before=target.HP;pulse.Timer=0;pulse.enabled=true;while(pulse.Timer<1)yield return null;while(pulse.Timer>=1)yield return null;pulse.enabled=false;
   Log("pulse-damage",summon.name+" delta="+(before-target.HP)+" timer="+pulse.Timer);Require(before-target.HP==50,"actual two-second pulse damages registered enemy by50: "+summon.name);ReturnFixtureActor(target.gameObject);
   var selected=(System.Collections.Generic.List<int>)WindField(pulse,"targets");Log("pulse-range-query","inside="+selected.Contains(target.gameObject.GetInstanceID())+" outside="+selected.Contains(outside.gameObject.GetInstanceID())+" other-producer-outside-damage="+(outside.maxHP-outside.HP));
   Require(!selected.Contains(outside.gameObject.GetInstanceID()),"actual pulse target query excludes outside-radius actor irrespective of other producers");ReturnFixtureActor(outside.gameObject);
   if(!inspect)
   {
    var wave=summon.GetComponentInChildren<LineRenderer>();Require(wave!=null&&wave.enabled&&wave.loop&&wave.positionCount==48,"timed pulse has an actual rendered expanding wave: "+summon.name);
    Plugin.Instance.EffectsOpacity.Value=25;yield return null;Require(wave.GetComponent<PlayerVisualOpacity>().AppliedOpacity==.25f,"pulse uses player-effects opacity");
    PauseController.SharedInstance.Pause();yield return PoiCapture("summon-pulse-"+summon.GetType().Name+".png");PauseController.SharedInstance.UnPause();
   }
  }
  if(!inspect&&!continuation){yield return DragonSuspend(game,"Native marker tracking, clock opacity and actual scythe/lance pulses verified; save for fresh-process Continue.");yield break;}
  if(continuation)yield return PoiTracking(game);
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Actual scythe/lance Pulsing configuration and timed combat ticks, native acquisitions; direct summon colliders isolated.");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(PickupDirectionLayout),"Place")]
class ObservePoiProjection
{static void Postfix(Camera camera){if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="poi-pulse-geometry")RuntimeChecks.PoiCameraProbe(camera,"place");}}
