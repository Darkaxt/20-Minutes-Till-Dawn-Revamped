using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 static void HoneyRefresh(ExplorationRun run)=>AccessTools.Method(typeof(ExplorationRun),"RefreshLayout").Invoke(run,null);
 static void HoneyReset(ExplorationRun run,ExplorationState state)
 {foreach(var chest in run.Chests.ToArray()){chest.gameObject.SetActive(false);Object.Destroy(chest.gameObject);}run.Chests.Clear();AccessTools.Method(typeof(ExplorationRun),"Restore").Invoke(run,new object[]{state});}
 void HoneyGeometry()
 {
  int cells=0;bool balanced=true,located=true,shared=true;
  foreach(uint seed in new uint[]{1,2,3,36000,918273})for(int q=-5;q<=5;q++)for(int r=-5;r<=5;r++)
  {
   var cell=new HoneycombLayout.Cell(q,r);var corners=HoneycombLayout.Corners(cell).ToArray();var rewards=corners.Select(v=>HoneycombLayout.Reward(seed,v)).Select(v=>v==ExplorationReward.CharacterUpgrade?ExplorationReward.Upgrade:v).ToArray();
   balanced&=corners.Select(v=>v.Slot).Distinct().Count()==6&&rewards.Distinct().Count()==6;
   var center=corners.Select(v=>HoneycombLayout.Position(v,new Vector2(13,-7),37)).Aggregate(Vector2.zero,(a,b)=>a+b)/6;
   located&=HoneycombLayout.Locate(center,new Vector2(13,-7),37).Equals(cell);
   shared&=corners.Select(v=>v.Key).Intersect(HoneycombLayout.Corners(new HoneycombLayout.Cell(q+1,r)).Select(v=>v.Key)).Count()==2;cells++;
  }
  Require(balanced&&located&&shared,"all "+cells+" sampled hex cells have six balanced classes, correct inverse positions and two shared edge vertices");
  var edge=new Rect(56,84,1168,552);var reserved=new List<Rect>{new Rect(56,590,1168,120)};
  var oldSides=new HashSet<bool>();
  foreach(float x in new[]{-1f,1f})
  {
   var direction=new Vector2(x,1000);var desired=edge.center+direction*(edge.height*.5f/1000);var old=new List<Vector2>{desired,new Vector2(edge.xMin,edge.center.y),new Vector2(edge.xMax,edge.center.y)};
   var chosen=old.OrderBy(p=>reserved.Count(rect=>rect.Overlaps(new Rect(p.x-36,p.y-52,72,88)))*10000000+(p-desired).sqrMagnitude).First();oldSides.Add(chosen.x==edge.xMin);
   var fixedPoint=PickupDirectionLayout.EdgePoint(direction,edge,reserved,new List<Vector2>(),1);Require(fixedPoint.y==edge.yMax,"north axis keeps northern edge despite a fully reserved top band: "+x);
  }
  Require(oldSides.Count==2,"old global edge ranking reproduces left/right flip around north");
 }
 IEnumerator HoneyArrow(Gamepad pad,Transform scope,Selectable button)
 {
  yield return MenuMove(pad,scope,button);Canvas.ForceUpdateCanvases();yield return null;yield return new WaitForEndOfFrame();
  var arrow=Plugin.Instance.GetComponent<MenuFocusArrow>();Require(arrow.Target==button.gameObject&&arrow.Arrow.gameObject.activeInHierarchy,"visible focus arrow follows actual controller selection: "+button.name);
  arrow.Arrow.ForceMeshUpdate();var bounds=arrow.Arrow.textBounds;var corners=new Vector3[4];arrow.Arrow.rectTransform.GetWorldCorners(corners);var canvas=arrow.Arrow.canvas.rootCanvas;var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
  var pixels=new[]{bounds.min,new Vector3(bounds.max.x,bounds.min.y,0),bounds.max,new Vector3(bounds.min.x,bounds.max.y,0)}.Select(c=>RectTransformUtility.WorldToScreenPoint(camera,arrow.Arrow.transform.TransformPoint(c))).ToArray();
  Log("focus-render-bounds",button.name+" "+string.Join(";",pixels.Select(v=>v.ToString())));
  Require(pixels.All(c=>c.x>=0&&c.x<=Screen.width&&c.y>=0&&c.y<=Screen.height),"focus arrow ink remains inside viewport: "+button.name);
  float height=Vector2.Distance(RectTransformUtility.WorldToScreenPoint(camera,arrow.Arrow.transform.TransformPoint(bounds.min)),RectTransformUtility.WorldToScreenPoint(camera,arrow.Arrow.transform.TransformPoint(new Vector3(bounds.min.x,bounds.max.y,0))));
  Require(height>=4,"native rendered focus chevron is visibly sized: "+button.name+" height="+height);
 }
 IEnumerator CheckHoneycomb(string mode)
 {
  if(mode=="honeycomb-mirror"){yield return CheckMirrorPotency();yield break;}
  if(mode=="honeycomb-opacity"){yield return CheckPlayerOpacity();yield break;}
  if(mode=="honeycomb-focus")
  {
   var iconTitle=Progression.Title;var iconPad=InputSystem.AddDevice<Gamepad>("HoneycombIconController");InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});RunResume.Invalidate();
   while(iconTitle.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;Click(Entries(iconTitle.mainMenu)[0].button);while(!Active(iconTitle.characterMenu)||iconTitle.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
   foreach(var entry in Entries(iconTitle.characterMenu).Take(6))yield return HoneyArrow(iconPad,iconTitle.characterMenu.transform,entry.button);
   yield return Capture("honeycomb-character-focus.png");SetPhase("PASS");Log("complete","Native icon-only roster buttons use an inside-left chevron, with visible ink and viewport fit across the first row.");Application.Quit(0);yield break;
  }
  if(mode=="honeycomb-continue")
  {
   var focusTitle=Progression.Title;var focusPad=InputSystem.AddDevice<Gamepad>("HoneycombContinueController");InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
   while(focusTitle.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
   var focusOptions=Entries(focusTitle.mainMenu).Single(e=>e.button.GetComponentInChildren<TMP_Text>().text=="Options").button;
   yield return HoneyArrow(focusPad,focusTitle.mainMenu.transform,focusOptions);yield return SelectorPad(focusPad,GamepadButton.South);while(!Active(focusTitle.optionsMenu)||focusTitle.optionsMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
   foreach(var button in Entries(focusTitle.optionsMenu).Select(e=>e.button).Where(b=>b.IsInteractable()).Take(3))yield return HoneyArrow(focusPad,focusTitle.optionsMenu.transform,button);
   yield return Capture("honeycomb-title-focus.png");yield return SelectorPad(focusPad,GamepadButton.East);while(!Active(focusTitle.mainMenu))yield return null;
   var expected=RunResume.Read().exploration;yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var run=ExplorationRun.Instance;
   Require(run.State.seed==expected.seed&&run.State.originX==expected.originX&&run.State.originY==expected.originY&&run.State.collected.SequenceEqual(expected.collected),"actual Continue preserves grid seed/origin/collected vertices");
   Require(expected.drops.All(d=>run.State.drops.Any(actual=>actual.id==d.id&&actual.vertex==d.vertex&&actual.reward==d.reward&&actual.power==d.power&&actual.x==d.x&&actual.y==d.y)),"actual Continue preserves every discovered reward without reroll");
   Require(run.Chests.All(c=>c.Drop.vertex==null||!run.State.collected.Contains(c.Drop.vertex))&&run.Chests.Count(c=>c.Physical)<=6&&run.Chests.Count(c=>c.GetComponent<ChestDirectionIndicator>().Radar)==3,"Continue preserves collection, physical cell bound and nearest-three radar");
   var boundary=new GameObject("HoneycombMapBoundsFixture",typeof(BoxCollider2D));boundary.tag="MapBounds";boundary.transform.position=run.GridOrigin+Vector2.right*6;boundary.GetComponent<BoxCollider2D>().isTrigger=true;boundary.GetComponent<BoxCollider2D>().size=new Vector2(1,1000);Physics2D.SyncTransforms();
   Require(!(bool)AccessTools.Method(typeof(ExplorationRun),"Reachable").Invoke(run,new object[]{run.GridOrigin+Vector2.right*36}),"native MapBounds physics excludes a vertex across the arena wall");boundary.SetActive(false);Object.Destroy(boundary);
   using(var stream=File.OpenRead(Environment.GetEnvironmentVariable("MODERNIZATION_HONEY_LEGACY")))
   {
    var old=(RunResume.Snapshot)new DataContractJsonSerializer(typeof(RunResume.Snapshot)).ReadObject(stream);Require(old.exploration.layout==0&&old.exploration.collected==null,"real public1.3.8 wire checkpoint reads missing optional grid fields");
    var legacy=old.exploration;var p=(Vector2)PlayerController.Instance.transform.position;legacy.drops=new List<ExplorationDrop>{new ExplorationDrop{id=0,reward=ExplorationReward.Magnet,x=p.x+8,y=p.y},new ExplorationDrop{id=1,reward=ExplorationReward.Freeze,x=p.x-8,y=p.y}};legacy.nextId=2;legacy.credits=1234;legacy.frenzy=3;
    AccessTools.Method(typeof(ExplorationRun),"Validate").Invoke(null,new object[]{legacy});HoneyReset(run,legacy);yield return null;
    Require(run.State.layout==1&&run.State.credits==1234&&run.State.frenzy>0&&run.State.drops.Any(d=>d.id==0&&d.reward==ExplorationReward.Magnet&&Mathf.Abs(d.x-(p.x+8))<.0001f)&&run.State.drops.Any(d=>d.id==1&&d.reward==ExplorationReward.Freeze&&Mathf.Abs(d.x-(p.x-8))<.0001f),"old-format transition keeps unclaimed contents/positions, earned Score and frenzy without a reset");
   }
   yield return PoiCapture("honeycomb-continued.png");PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return null;game.playerInput.SwitchCurrentControlScheme("Gamepad",focusPad);
   foreach(var button in game.pauseMenu.GetComponentsInChildren<Button>().Where(b=>b.IsInteractable()))yield return HoneyArrow(focusPad,game.pauseMenu.transform,button);
   yield return Capture("honeycomb-pause-focus.png");yield return PadClick(focusPad,game.pauseMenu.transform,game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;
   yield return AchievementExit(game,false);var roster=Progression.Title;Click(Entries(roster.mainMenu)[0].button);while(!Active(roster.characterMenu)||roster.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
   yield return HoneyArrow(focusPad,roster.characterMenu.transform,Entries(roster.characterMenu)[0].button);yield return Capture("honeycomb-character-focus.png");
   SetPhase("PASS");Log("complete","Byte-bound Continue, collected/shared reward identity, native old-wire migration, native-scaled title/pause/icon focus and scene lifetime preserve state.");Application.Quit(0);yield break;
  }
  HoneyGeometry();RunResume.Invalidate();var title=Progression.Title;var pad=InputSystem.AddDevice<Gamepad>("HoneycombController");InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
  while(title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  var options=Entries(title.mainMenu).Single(e=>e.button.GetComponentInChildren<TMP_Text>().text=="Options").button;
  yield return HoneyArrow(pad,title.mainMenu.transform,options);yield return SelectorPad(pad,GamepadButton.South);while(!Active(title.optionsMenu)||title.optionsMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  foreach(var button in Entries(title.optionsMenu).Select(e=>e.button).Where(b=>b.IsInteractable()).Take(3))yield return HoneyArrow(pad,title.optionsMenu.transform,button);
  yield return Capture("honeycomb-title-focus.png");yield return SelectorPad(pad,GamepadButton.East);while(!Active(title.mainMenu))yield return null;
  yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  var current=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var owner=ExplorationRun.Instance;var player=PlayerController.Instance;
  while(owner.State==null)yield return null;HoneyReset(owner,new ExplorationState{random=36000});yield return null;
  var origin=owner.GridOrigin;Require(owner.Chests.Count(c=>c.Physical)==6&&owner.Chests.Count(c=>c.GetComponent<ChestDirectionIndicator>().Radar)==3,"fresh cell immediately materializes six corners and only three radar targets");
  Require(owner.State.drops.Where(d=>HoneycombLayout.Corners(new HoneycombLayout.Cell(0,0)).Any(v=>v.Key==d.vertex)).Select(d=>d.reward==ExplorationReward.CharacterUpgrade?ExplorationReward.Upgrade:d.reward).Distinct().Count()==6,"actual unobstructed cell has the complete balanced reward mix");
  var ids=owner.State.drops.Select(d=>d.id).ToArray();owner.State.remaining=0;HoneyRefresh(owner);yield return null;Require(ids.SequenceEqual(owner.State.drops.Select(d=>d.id)),"standing still at an expired legacy timer does not spawn or reroll any POI");
  var north=new GameObject("NorthEdgeReproduction",typeof(SpriteRenderer),typeof(ChestDirectionIndicator));var marker=north.GetComponent<ChestDirectionIndicator>();var source=Resources.FindObjectsOfTypeAll<flanne.Pickups.ChestPickup>().First(c=>!c.gameObject.scene.IsValid());marker.Icon=(Sprite)WindField(source,"chestClosed");north.GetComponent<SpriteRenderer>().sprite=marker.Icon;
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(720,720)})
  {
   Screen.SetResolution(size.x,size.y,false);while(Screen.width!=size.x||Screen.height!=size.y)yield return null;Canvas.ForceUpdateCanvases();
   foreach(float x in new[]{-1f,-.01f,0f,.01f,1f})
   {
    north.transform.position=player.transform.position+new Vector3(x,100,0);FeedbackCameras();yield return null;yield return new WaitForEndOfFrame();
    Require(marker.Marker!=null&&marker.Marker.gameObject.activeInHierarchy,"north marker remains visible around the axis");var screen=RectTransformUtility.WorldToScreenPoint(null,marker.Marker.position);Require(screen.y>Screen.height*.7f,"north marker remains on top rather than blinking left/right: "+size.x+"/"+x);
   }
   yield return PoiCapture("honeycomb-north-"+size.x+".png");
  }
  north.SetActive(false);Object.Destroy(north);
  var xpChest=owner.Chests.Single(c=>c.Physical&&c.Drop.reward==ExplorationReward.Experience);string collected=xpChest.Drop.vertex;yield return ExplorationContact(xpChest,current);
  Require(owner.State.collected.Contains(collected)&&owner.Chests.All(c=>c.Drop.vertex!=collected),"real physics contact records the vertex once and removes its chest");
  player.SetPosition(origin);FeedbackCameras();yield return null;HoneyRefresh(owner);Require(owner.State.drops.All(d=>d.vertex!=collected),"returning to the original cell does not regenerate its collected vertex");
  var shared=owner.State.drops.Where(d=>HoneycombLayout.Corners(new HoneycombLayout.Cell(1,0)).Any(v=>v.Key==d.vertex)).ToDictionary(d=>d.vertex,d=>d.id+":"+d.reward+":"+d.power);
  var next=HoneycombLayout.Corners(new HoneycombLayout.Cell(1,0)).Select(v=>HoneycombLayout.Position(v,origin,owner.GridRotation)).Aggregate(Vector2.zero,(a,b)=>a+b)/6;player.SetPosition(next);FeedbackCameras();yield return null;HoneyRefresh(owner);
  Require(owner.Chests.Count(c=>c.Physical)<=6&&owner.Chests.Count(c=>c.GetComponent<ChestDirectionIndicator>().Radar)==3&&shared.All(p=>owner.State.drops.Any(d=>d.vertex==p.Key&&d.id+":"+d.reward+":"+d.power==p.Value)),"cell transition streams bounded physical objects, keeps nearest three and reuses shared reward identities");
  File.WriteAllText(Path.Combine(ResultDir,"grid-before-save.json"),JsonUtility.ToJson(owner.State,true));yield return PoiCapture("honeycomb-cell.png");
  PauseController.SharedInstance.Pause();current.ChangeState<PauseState>();yield return null;current.playerInput.SwitchCurrentControlScheme("Gamepad",pad);
  foreach(var button in current.pauseMenu.GetComponentsInChildren<Button>().Where(b=>b.IsInteractable()))yield return HoneyArrow(pad,current.pauseMenu.transform,button);
  yield return Capture("honeycomb-pause-focus.png");yield return PadClick(pad,current.pauseMenu.transform,current.pauseResumeButton);while(!(current.CurrentState is CombatState))yield return null;
  yield return DragonSuspend(current,"Native menu focus, balanced honeycomb, north-axis stability, real collection, revisit and shared-cell streaming; saved for exact Continue.");
 }
}
