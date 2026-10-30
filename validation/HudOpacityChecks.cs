using System;
using System.Linq;
using System.Collections;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator ProbeHudRoots()
 {
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());yield return null;
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  foreach(var view in game.hud.GetComponentInParent<Canvas>().rootCanvas.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="FollowMouseAmmo"||t.name=="MoveControls"))
   Log("hud-extra-root",string.Join("/",view.GetComponentsInParent<Transform>(true).Reverse().Select(t=>t.name))+" active="+view.gameObject.activeInHierarchy+" scripts="+string.Join(",",view.GetComponents<MonoBehaviour>().Select(b=>b.GetType().Name)));
  SetPhase("DIAGNOSTIC_COMPLETE");Application.Quit(0);
 }
 IEnumerator CheckHudOpacity(bool skipMenu)
 {
  RunResume.Invalidate();if(!skipMenu)yield return PoiOpacityMenu();
  yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return AndroidPick(game,"MagicLens",game.playerXP.level);
  while(StatsOverlay.Instance.Panel==null)yield return null;
  var panel=StatsOverlay.Instance.Panel;var enemy=ElementEnemy((Vector2)PlayerController.Instance.transform.position+Vector2.right*3);enemy.GetComponent<MoveComponent2D>().enabled=false;var body=enemy.GetComponent<Rigidbody2D>();if(body!=null)body.constraints=RigidbodyConstraints2D.FreezeAll;
  var native=game.hud.GetComponentInParent<Canvas>().rootCanvas.GetComponentsInChildren<RectTransform>(true);var cursorAmmo=native.Single(t=>t.name=="ShootingCursor").gameObject;var hints=native.Single(t=>t.name=="ControlsDisplay").gameObject;
  var sprite=enemy.GetComponentInChildren<SpriteRenderer>();var nativeColor=sprite.color;var nativeMaterial=sprite.sharedMaterial;
  // Exercise the real badge component's UI owner without changing the run's
  // mode, progression or eligibility; natural Custom rules are unchanged.
  game.gameObject.AddComponent(AccessTools.TypeByName("TwentyMinutes.Modernization.CustomRunLabel"));yield return null;
  if(UnlockNotifications.Instance.Root==null)AccessTools.Method(typeof(UnlockNotifications),"Build").Invoke(UnlockNotifications.Instance,null);
  Require(UnlockNotifications.Instance.Root.GetComponent<GameplayHudOpacity>()!=null,"achievement notification has its own HUD opacity owner");
  var badge=GameObject.Find("CustomRunBadge");Require(badge!=null&&badge.GetComponent<GameplayHudOpacity>()!=null,"actual Custom badge UI has its own HUD opacity owner");
  foreach(var text in Object.FindObjectsOfType<TMP_Text>().Where(t=>t.gameObject.activeInHierarchy&&t.canvasRenderer.GetInheritedAlpha()>.01f))Log("visible-hud-inventory",text.name+" type="+text.GetType().Name+" parent="+text.transform.parent.name+" nativeHUD="+text.transform.IsChildOf(game.hud.transform));
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(720,720)})
  {
   Screen.SetResolution(size.x,size.y,false);while(Screen.width!=size.x||Screen.height!=size.y)yield return null;FeedbackCameras();
   var position=Camera.main.ViewportToWorldPoint(new Vector3(.05f,.5f,10));position.z=0;enemy.transform.position=position;
   foreach(int value in new[]{80,85,90})
   {
    Plugin.Instance.HudOpacity.Value=value;yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();float factor=value/100f;
    foreach(var root in new[]{game.hud.gameObject,panel,badge,cursorAmmo,hints})
    {
     Require(root.GetComponentsInParent<GameplayHudOpacity>().Length==1,"HUD root has exactly one opacity owner: "+root.name);
     var graphics=root.GetComponentsInChildren<Graphic>().Where(g=>g.isActiveAndEnabled&&g.color.a>.01f&&g.canvasRenderer.GetInheritedAlpha()>.01f).ToArray();
     if(root==hints)
     {
      Require(Mathf.Abs(root.GetComponentInParent<GameplayHudOpacity>().GetComponent<CanvasGroup>().alpha-factor)<.002f,"control hints retain their native visibility beneath the shared opacity factor");
      Log("controls-native-visibility","active="+root.activeInHierarchy+" nativeGroups="+string.Join(",",root.GetComponents<CanvasGroup>().Select(g=>g.alpha))+" drawnGraphics="+graphics.Length);
     }
     Require(graphics.Length>0||root==hints,"visible HUD graphics exist, or native control hints are intentionally hidden: "+root.name);
     foreach(var graphic in graphics){float alpha=graphic.canvasRenderer.GetInheritedAlpha();Log("hud-inherited-alpha",size+" "+value+" "+graphic.name+"="+alpha);Require(alpha<=factor+.002f,"rendered HUD alpha stays within setting: "+graphic.name);}
    }
    var timer=game.hud.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Timetext");var score=game.hud.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="RunCredits");
    Require(Mathf.Abs(timer.canvasRenderer.GetInheritedAlpha()-factor)<.002f&&Mathf.Abs(score.canvasRenderer.GetInheritedAlpha()-factor)<.002f&&Mathf.Abs(StatsOverlay.Instance.Body.canvasRenderer.GetInheritedAlpha()-factor)<.002f,"timer, Score and stats inherit the factor once, without double fading");
    Require(StatsOverlay.Instance.SkillIcons.Any(i=>i.gameObject.activeInHierarchy&&Mathf.Abs(i.canvasRenderer.GetInheritedAlpha()-factor)<.002f),"newly acquired skill icon inherits unified opacity");
    Require(Object.FindObjectsOfType<ChestDirectionIndicator>().Any(i=>i.Marker!=null&&i.Marker.gameObject.activeInHierarchy&&Mathf.Abs(i.Marker.GetComponent<Image>().canvasRenderer.GetInheritedAlpha()-factor)<.002f),"actual POI content marker inherits unified opacity");
    Require(sprite.color==nativeColor&&sprite.sharedMaterial==nativeMaterial&&sprite.GetComponentsInParent<GameplayHudOpacity>().Length==0,"enemy appearance and material remain unchanged under the HUD");
    if(value==85)yield return PoiCapture("hud85-mob-under-metrics-"+size.x+".png");
   }
  }
  Plugin.Instance.HudOpacity.Value=85;yield return null;PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();
  while(game.pauseMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  var background=game.pauseMenu.GetComponentsInChildren<Image>().First(i=>i.color.a>.99f);Require(background.GetComponentsInParent<GameplayHudOpacity>().Length==0&&background.canvasRenderer.GetInheritedAlpha()>.99f,"native pause menu background stays opaque and outside HUD opacity");
  yield return Capture("hud85-opaque-pause.png");Click(game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;yield return null;
  Require(Mathf.Abs(game.hud.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="Timetext").canvasRenderer.GetInheritedAlpha()-.85f)<.001f&&game.hud.GetComponent<CanvasGroup>().alpha>.99f,"resume restores85% HUD without taking the native visibility group's alpha");
  ReturnFixtureActor(enemy.gameObject);yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Native HUD opacity controller/pointer/persistence, all three levels across square/16:9, new skills/POIs/badge/notification owners, unchanged mobs and opaque pause menus.");Application.Quit(0);
 }
}
