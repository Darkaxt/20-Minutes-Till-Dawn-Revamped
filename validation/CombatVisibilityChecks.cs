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
using flanne.Pickups;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckChestHandoff()
 {
  yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;var run=ExplorationRun.Instance;while(run.State==null)yield return null;RemovePoiFixture(run);
  Screen.SetResolution(900,900,false);yield return WaitDisplay(900,900);CameraZoom.Value=100;if(!ColorfulStyle.Instance.Enabled)ColorfulStyle.Instance.Toggle();yield return null;yield return null;
  var origin=p.transform.position;var chest=ExplorationFixture(run,ExplorationReward.Experience,(Vector2)origin+Vector2.right*50);var point=chest.transform.position;var camera=Camera.main;
  foreach(float distance in new[]{camera.orthographicSize*camera.aspect+2,camera.orthographicSize*camera.aspect-.5f,8,5,2,.8f})
  {
   p.SetPosition(point-Vector3.right*distance);Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;yield return null;
   var marker=chest.GetComponent<ChestDirectionIndicator>();Log("natural-chest-handoff","distance="+distance+" recorded="+run.State.drops.Count+" active="+chest.gameObject.activeInHierarchy+" viewport="+camera.WorldToViewportPoint(point)+" marker="+(marker.Marker!=null&&marker.Marker.gameObject.activeInHierarchy));yield return Capture("natural-handoff-"+distance.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+".png");
  }
  var scenery=Object.FindObjectsOfType<SpriteRenderer>().Where(r=>r.sprite!=null&&r.GetComponentInParent<Health>()==null&&r.GetComponentInParent<PlayerController>()==null&&!r.transform.IsChildOf(game.transform)&&r.bounds.size.x>1).OrderBy(r=>Vector2.Distance(r.transform.position,p.transform.position)).Take(15).ToArray();
  foreach(var sprite in scenery)Log("chest-scenery",sprite.name+" art="+sprite.sprite.name+" pos="+sprite.transform.position+" layer="+sprite.gameObject.layer+" sort="+sprite.sortingLayerName+":"+sprite.sortingOrder+" bounds="+sprite.bounds);
  if(scenery.Length>0)
  {
   point=scenery[0].bounds.center;point.z=0;chest.transform.position=point;chest.Drop.x=point.x;chest.Drop.y=point.y;p.SetPosition(point-Vector3.right*2);Physics2D.SyncTransforms();yield return null;yield return null;yield return Capture("natural-chest-scenery.png");
  }
  SetPhase("PASS");Log("complete","Diagnostic natural native camera/fog handoff and scenery; no premature-destruction or fix claim");Application.Quit(0);
 }
 IEnumerator CheckChestVisibility()
 {
  yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;var run=ExplorationRun.Instance;while(run.State==null)yield return null;
  RemovePoiFixture(run);var origin=p.transform.position;
  foreach(var size in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})foreach(bool reborn in new[]{false,true})foreach(int zoom in new[]{0,100})
  {
   Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);CameraZoom.Value=zoom;if(ColorfulStyle.Instance.Enabled!=reborn)ColorfulStyle.Instance.Toggle();
   p.SetPosition(origin);var chest=ExplorationFixture(run,ExplorationReward.Magnet,(Vector2)origin+Vector2.right*50);int id=chest.Drop.id;yield return null;yield return null;
   var marker=chest.GetComponent<ChestDirectionIndicator>();Require(marker.Marker!=null&&marker.Marker.gameObject.activeInHierarchy,"uncollected chest has an offscreen content marker");
   p.SetPosition(chest.transform.position-Vector3.right*.8f);Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;yield return null;
   Require(chest!=null&&chest.isActiveAndEnabled&&run.State.drops.Any(d=>d.id==id)&&!marker.Marker.gameObject.activeSelf,"hiding the edge marker does not remove or collect the onscreen chest");PoiVisualContract(chest);
   var body=chest.GetComponent<SpriteRenderer>();Require(body.enabled&&body.sprite!=null&&body.color.a==1,"near chest retains opaque world artwork");yield return PoiCapture("chest-visible-"+size.x+"-"+(reborn?"reborn":"original")+"-"+zoom+".png");
   p.SetPosition(origin);Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;yield return null;Require(marker.Marker.gameObject.activeSelf,"walking away restores the same unclaimed chest's indicator");
   yield return ExplorationContact(chest,game);Require(!run.State.drops.Any(d=>d.id==id),"only actual body contact removes that chest and dispatches its reward");
   p.SetPosition(origin);yield return null;yield return null;var skill=StatsOverlay.Instance.SkillIcons.First(i=>i.gameObject.activeInHierarchy);var screen=RectTransformUtility.WorldToScreenPoint(null,skill.rectTransform.position);var covered=Camera.main.ScreenToWorldPoint(new Vector3(screen.x,screen.y,10));covered.z=0;
   var hidden=ExplorationFixture(run,ExplorationReward.Experience,covered);yield return null;yield return null;var pointer=hidden.GetComponent<ChestDirectionIndicator>();Require(pointer.Marker!=null&&pointer.Marker.gameObject.activeSelf,"chest hidden by the real skills HUD keeps a safely arranged content indicator");
   hidden.transform.position=Camera.main.ViewportToWorldPoint(new Vector3(.999f,.6f,10));var clipped=hidden.transform.position;clipped.z=0;hidden.transform.position=clipped;yield return null;yield return null;Require(pointer.Marker.gameObject.activeSelf,"partially clipped chest keeps its indicator until the whole world artwork is visible");
   yield return Capture("chest-handoff-"+size.x+"-"+(reborn?"reborn":"original")+"-"+zoom+".png");RemovePoiFixture(run);
  }
  SetPhase("PASS");Log("complete","Continued game: exploration approach/retreat/contact, native fog/world sprite and edge lifecycle across three viewports, both palettes and both zoom extremes. No timeout or premature disappearance reproduced; no speculative production change.");Application.Quit(0);
 }
 IEnumerator CheckCombatVisibilityProbe()
 {
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;
  Log("damage-preference",PlayerPrefs.GetInt("ShowDamagePopup",1));
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="damage-auto-hide-probe")
  {
   var type=AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidDamageDisplay");var display=game.GetComponent(type);Log("auto-hide-initial-state","disabled="+WindField(display,"hasAutoDisabled")+" timer="+WindField(display,"lowFpsTimer")+" pref="+PlayerPrefs.GetInt("ShowDamagePopup",1));Plugin.SetField(display,"hasAutoDisabled",false);Plugin.SetField(display,"lowFpsTimer",7f);AccessTools.Field(type,"fps").SetValue(null,19f);PlayerPrefs.SetInt("ShowDamagePopup",1);ElementCall(display,"Check");
   Require(PlayerPrefs.GetInt("ShowDamagePopup",1)==0,"actual Android controller persistently disables numbers after its final low-FPS sample");
   var enemy=ElementEnemy((Vector2)p.transform.position+Vector2.right*3);enemy.TakeDamage(DamageType.Bullet,13);yield return null;
   Require(!Object.FindObjectsOfType<AndroidDamagePopup>().Any(n=>n.GetComponent<TextMeshPro>().text=="13"),"a real hit is suppressed by that persisted automatic flag");
   SetPhase("PASS");Log("complete","Diagnostic reproduction of Android persistent damage-number hiding; not acceptance");Application.Quit(0);yield break;
  }
  foreach(var collider in p.GetComponentsInChildren<Collider2D>(true))Log("player-contact-shape",collider.name+" tag="+collider.tag+" layer="+collider.gameObject.layer+" trigger="+collider.isTrigger+" radius="+collider.bounds.extents+" path="+collider.transform.parent?.name);
  foreach(var shader in Resources.FindObjectsOfTypeAll<Shader>())Log("loaded-shader",shader.name+" supported="+shader.isSupported+" properties="+string.Join(",",Enumerable.Range(0,shader.GetPropertyCount()).Select(shader.GetPropertyName)));
  var target=ElementEnemy((Vector2)p.transform.position+Vector2.right*3);target.GetComponent<MoveComponent2D>().enabled=false;int before=Object.FindObjectsOfType<AndroidDamagePopup>().Length;
  target.TakeDamage(DamageType.Bullet,7);yield return null;
  Log("actual-hit-popups","before="+before+" after="+Object.FindObjectsOfType<AndroidDamagePopup>().Length);
  PlayerPrefs.SetInt("ShowDamagePopup",1);target.TakeDamage(DamageType.Bullet,7);yield return null;
  foreach(var popup in Object.FindObjectsOfType<AndroidDamagePopup>())
  {var text=popup.GetComponent<TextMeshPro>();Log("damage-live-render",text.text+" active="+popup.isActiveAndEnabled+" font="+text.fontSize+" scale="+popup.transform.lossyScale+" material="+text.fontSharedMaterial?.name+" shader="+text.fontSharedMaterial?.shader?.name+" layer="+popup.gameObject.layer+" sorting="+text.renderer.sortingLayerName+":"+text.renderer.sortingOrder);}
  yield return PoiCapture("damage-probe.png");ElementClear();
  var run=ExplorationRun.Instance;while(run.State==null)yield return null;var point=(Vector2)p.transform.position+Vector2.right*5;
  var chest=ExplorationFixture(run,ExplorationReward.Magnet,point);chest.gameObject.AddComponent<CombatChestContactProbe>();
  PoiRenderFacts(chest.gameObject,"exploration-probe");var original=Resources.FindObjectsOfTypeAll<ChestPickup>().First(c=>!c.gameObject.scene.IsValid());PoiRenderFacts(original.gameObject,"native-chest-probe");
  foreach(var camera in Camera.allCameras)Log("visibility-camera",camera.name+" layers="+camera.cullingMask+" depth="+camera.depth+" enabled="+camera.enabled);
  foreach(float distance in new[]{15f,8f,5f,3f,1.5f,.8f,.4f})
  {
   p.SetPosition(new Vector3(point.x-distance,point.y,0));Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
   Log("approach-chest","distance="+distance+" exists="+(chest!=null)+" active="+(chest!=null&&chest.gameObject.activeSelf)+" recorded="+run.State.drops.Count);
   yield return PoiCapture("chest-distance-"+distance.ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+".png");
   if(chest==null)break;
  }
  SetPhase("PASS");Log("complete","Diagnostic native hit/preference, contact, material, camera and shader inventory; not acceptance");Application.Quit(0);
 }
 IEnumerator CheckCombatVisibility(bool continued)
 {
  if(continued)yield return CombatBuffContinue();
  else{RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());}
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;
  Require(AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidDamageDisplay")==null,"automatic damage hiding owner has been removed");
  Require(Plugin.Instance.DamageNumbers.Value==!continued,continued?"manual Off persists in fresh-process Continue":"new explicit damage-number setting defaults On");
  var pad=InputSystem.AddDevice<Gamepad>("DamageVisibilityController");game.playerInput.SwitchCurrentControlScheme("Gamepad",pad);game.playerInput.neverAutoSwitchControlSchemes=true;
  if(continued)
  {
   game.pauseController.Pause();game.ChangeState<PauseState>();yield return null;yield return PadClick(pad,game.pauseMenu.transform,game.optionsButton);while(!Active(game.optionsMenu))yield return null;
   var setting=game.optionsMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="DamageNumbersSetting");yield return PadClick(pad,game.optionsMenu.transform,setting);Require(Plugin.Instance.DamageNumbers.Value,"controller enables persisted Off after Continue");
   yield return SelectorPad(pad,GamepadButton.East);while(!(game.CurrentState is PauseState))yield return null;yield return PadClick(pad,game.pauseMenu.transform,game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;
  }
  PlayerPrefs.SetInt("ShowDamagePopup",0);
  foreach(var size in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})foreach(bool reborn in new[]{false,true})foreach(int zoom in new[]{0,100})
  {
   Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);CameraZoom.Value=zoom;if(ColorfulStyle.Instance.Enabled!=reborn)ColorfulStyle.Instance.Toggle();
   foreach(var popup in Object.FindObjectsOfType<AndroidDamagePopup>())ReturnFixtureActor(popup.gameObject);
   var victim=ElementEnemy((Vector2)p.transform.position+Vector2.right*3);victim.GetComponent<MoveComponent2D>().enabled=false;int hp=victim.HP;victim.TakeDamage(DamageType.Bullet,17);yield return null;
   var number=Object.FindObjectsOfType<AndroidDamagePopup>().Single(n=>n.GetComponent<TextMeshPro>().text=="17");var text=number.GetComponent<TextMeshPro>();
   Require(hp-victim.HP==17&&text.renderer.enabled&&text.color.a>0&&text.gameObject.activeInHierarchy,"actual damage still displays despite legacy automatic Off; damage is unchanged");CombatClose(text.fontSize,2*(1+zoom/100f),"number glyphs retain camera zoom compensation");
   yield return PoiCapture("damage-visible-"+size.x+"-"+(reborn?"reborn":"original")+"-"+zoom+".png");ElementClear();
  }
  game.pauseController.Pause();game.ChangeState<PauseState>();yield return null;yield return PadClick(pad,game.pauseMenu.transform,game.optionsButton);while(!Active(game.optionsMenu))yield return null;
  var toggle=game.optionsMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="DamageNumbersSetting");yield return PadClick(pad,game.optionsMenu.transform,toggle);Require(!Plugin.Instance.DamageNumbers.Value&&toggle.GetComponentInChildren<TMP_Text>().text=="Damage numbers: Off","native controller switches numbers Off explicitly");
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);yield return MenuPointer(toggle);Require(Plugin.Instance.DamageNumbers.Value,"native pointer switches numbers On");yield return MenuPointer(toggle);Require(!Plugin.Instance.DamageNumbers.Value,"native pointer switches numbers Off once");yield return PoiCapture("damage-numbers-options.png");
  game.playerInput.SwitchCurrentControlScheme("Gamepad",pad);yield return SelectorPad(pad,GamepadButton.East);while(!(game.CurrentState is PauseState))yield return null;yield return PadClick(pad,game.pauseMenu.transform,game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;
  foreach(var popup in Object.FindObjectsOfType<AndroidDamagePopup>())ReturnFixtureActor(popup.gameObject);var off=ElementEnemy((Vector2)p.transform.position+Vector2.right*3);off.TakeDamage(DamageType.Bullet,19);yield return null;Require(!Object.FindObjectsOfType<AndroidDamagePopup>().Any(n=>n.GetComponent<TextMeshPro>().text=="19"),"explicit Off suppresses real damage numbers");ElementClear();
  Plugin.Instance.Config.Save();RunResume.Save();File.Copy(RunResume.PathName,Path.Combine(ResultDir,"saved-run.json"),true);SetPhase("PASS");Log("complete","Actual hit numbers, legacy automatic Off recovery, native controller/pointer toggle, both styles/three viewports/zoom and exact manual preference/Continue checkpoint");Application.Quit(0);
 }
}
public sealed class CombatChestContactProbe:MonoBehaviour
{
 void OnTriggerEnter2D(Collider2D other)=>RuntimeChecks.Log("chest-trigger-evidence",other.name+" tag="+other.tag+" trigger="+other.isTrigger+" layer="+other.gameObject.layer+" playerDistance="+Vector2.Distance(transform.position,PlayerController.Instance.transform.position));
}
