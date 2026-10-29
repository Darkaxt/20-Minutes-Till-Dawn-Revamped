using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.Pickups;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 void PoiRenderFacts(GameObject root,string role)
 {
  foreach(var tr in root.GetComponentsInChildren<Transform>(true))
  {
   Log("poi-transform",role+"/"+tr.name+" local="+tr.localPosition+" scale="+tr.localScale+" world="+tr.position+" layer="+tr.gameObject.layer+" active="+tr.gameObject.activeSelf+" components="+string.Join(",",tr.GetComponents<Component>().Select(c=>c.GetType().Name)));
   foreach(var renderer in tr.GetComponents<Renderer>())Log("poi-renderer",role+"/"+tr.name+" shader="+renderer.sharedMaterial?.shader?.name+" material="+renderer.sharedMaterial?.name+" sort="+renderer.sortingLayerName+":"+renderer.sortingOrder+" sprite="+(renderer as SpriteRenderer)?.sprite?.name+" color="+(renderer as SpriteRenderer)?.color);
  }
 }
 IEnumerator CheckPoiGlow(string mode)
 {
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;var run=game.GetComponent<ExplorationRun>();while(run.State==null)yield return null;
  CameraZoom.Value=0;FeedbackCameras();
  if(mode!="poi-glow-inspect"){yield return CheckPoiGlowPresentation(game,run);yield break;}
  var chest=ExplorationFixture(run,ExplorationReward.Credits,(Vector2)p.transform.position+Vector2.right*5);
  var template=Resources.FindObjectsOfTypeAll<ChestPickup>().First(c=>!c.gameObject.scene.IsValid());PoiRenderFacts(template.gameObject,"native-template");PoiRenderFacts(chest.gameObject,"exploration");
  var boss=ObjectPooler.SharedInstance.GetPooledObject("Chest");boss.transform.position=p.transform.position+new Vector3(5,2,0);PoiRenderFacts(boss,"native-live");
  foreach(var sprite in Resources.FindObjectsOfTypeAll<Sprite>().Where(s=>new[]{"glow","circle","light","halo"}.Any(key=>s.name.ToLowerInvariant().Contains(key))).GroupBy(s=>s.name).Select(g=>g.First()))Log("poi-light-art",sprite.name+" texture="+sprite.texture.name+" bounds="+sprite.bounds);
  foreach(var c in Camera.allCameras)Log("poi-camera",c.name+" depth="+c.depth+" layers="+c.cullingMask+" target="+c.targetTexture?.name);
  yield return null;foreach(var c in Camera.allCameras.Where(c=>c.enabled&&c.targetTexture!=null).OrderBy(c=>c.depth))c.Render();yield return Capture("poi-visibility-before.png");
  if(mode=="poi-glow-inspect")
  {Require(chest.GetComponentsInChildren<SpriteRenderer>().Length>=boss.GetComponentsInChildren<SpriteRenderer>().Length,"exploration chest retains native reveal artwork while its arrow is hidden");}
  SetPhase("PASS");Log("complete","Exploration visibility native comparison");Application.Quit(0);
 }
 void PoiVisualContract(ExplorationChest chest)
 {
  var template=Resources.FindObjectsOfTypeAll<ChestPickup>().First(c=>!c.gameObject.scene.IsValid());
  foreach(string name in new[]{"FogCircleRed","FogCircleBlue"})
  {
   var original=template.transform.Find(name);var copy=chest.transform.Find(name);Require(copy!=null,"exploration retains native shadow reveal: "+name);
   var a=original.GetComponent<SpriteRenderer>();var b=copy.GetComponent<SpriteRenderer>();
   Require(a.sprite==b.sprite&&a.sharedMaterial==b.sharedMaterial&&a.color==b.color&&original.gameObject.layer==copy.gameObject.layer&&a.sortingOrder==b.sortingOrder&&a.sortingLayerID==b.sortingLayerID,"native reveal sprite/material/channels/layer/order are exact: "+name);
   Require(original.localPosition==copy.localPosition&&original.localScale==copy.localScale&&Quaternion.Angle(original.localRotation,copy.localRotation)<.01f,"native fog-reveal pose is exact: "+name);
  }
  var visual=chest.GetComponent<ExplorationChestVisual>();Require(visual!=null&&chest.GetComponent<TimeToLive>()==null&&chest.GetComponent<ChestPickup>()==null,"owned exploration presentation adds no expiry or boss reward trigger");
  float expected=Mathf.Clamp01((8-Vector2.Distance(chest.transform.position,PlayerController.Instance.transform.position))/6);
  CombatClose(visual.Strength,expected,"glow follows actual player distance");
  var glow=chest.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name.StartsWith("RewardGlow")).ToArray();Require(glow.Length==3,"three restrained native-circle layers form the glow");
  var color=ExplorationChestVisual.RewardColor(RewardShown(chest));
  foreach(var renderer in glow)Require(renderer.enabled==(expected>0)&&renderer.color.r==color.r&&renderer.color.g==color.g&&renderer.color.b==color.b&&renderer.color.a<=.1201f&&renderer.gameObject.layer==template.transform.Find("Arrow").gameObject.layer,"reward glow uses the native visible-in-fog layer without changing native fog channels");
  var marker=chest.GetComponent<ChestDirectionIndicator>();if(marker.Marker!=null)Require(marker.Marker.GetComponent<UnityEngine.UI.Image>().color==marker.IconTint&&marker.Marker.GetComponent<UnityEngine.UI.Image>().sprite==chest.Owner.ContentIcon(chest.Drop),"edge pointer shows its actual reward artwork and native pickup tint");
 }
 IEnumerator PoiCapture(string name)
 {
  FeedbackCameras();
  foreach(var camera in Camera.allCameras.Where(c=>c.enabled&&c.targetTexture!=null).OrderBy(c=>c.depth))camera.Render();
  yield return Capture(name);
 }
 void RemovePoiFixture(ExplorationRun run)
 {
  foreach(var chest in run.Chests.ToArray()){run.State.drops.Remove(chest.Drop);run.Chests.Remove(chest);chest.gameObject.SetActive(false);Object.Destroy(chest.gameObject);}
 }
 IEnumerator CheckPoiGlowPresentation(GameController game,ExplorationRun run)
 {
  var player=PlayerController.Instance;var origin=player.transform.position;
  Log("fixture-precondition","Eight reward categories are created in batches of at most three for native visual checks; visual-only chests are removed without claiming rewards. The separate full exploration workflow exercises every actual pickup/reward/menu and current/prior checkpoints. FogOfWarCamera renders its actual mask texture before batchmode screenshots, as a normal player does automatically.");
  var palette=Enum.GetValues(typeof(ExplorationReward)).Cast<ExplorationReward>().Where(r=>r!=ExplorationReward.Credits).ToArray();Require(palette.Select(ExplorationChestVisual.RewardColor).Distinct().Count()==7,"seven current reward types have distinct palette entries; credit rewards are excluded");
  for(int batch=0;batch<3;batch++)
  {
   foreach(var reward in palette.Skip(batch*3).Take(3))
   {
    string power=null;if(reward==ExplorationReward.Upgrade||reward==ExplorationReward.CharacterUpgrade)power=RunResume.Key(((List<Powerup>)AccessTools.Method(typeof(ExplorationRun),"Eligible").Invoke(run,new object[]{reward==ExplorationReward.CharacterUpgrade})).First());
    ExplorationFixture(run,reward,(Vector2)origin+new Vector2(3,1.6f*(run.Chests.Count-1)),power);
   }
   foreach(bool reborn in new[]{false,true})
   {
    if(ColorfulStyle.Instance.Enabled!=reborn)ColorfulStyle.Instance.Toggle();
    player.SetPosition(origin+Vector3.left*20);yield return null;yield return null;
    foreach(var chest in run.Chests){PoiVisualContract(chest);Require(chest.GetComponent<ExplorationChestVisual>().Strength==0&&chest.GetComponent<SpriteRenderer>().color==ExplorationRun.Mint,"far chest conceals type in either style");}
    player.SetPosition(origin);yield return null;yield return null;
    foreach(var chest in run.Chests){PoiVisualContract(chest);Require(chest.GetComponent<ExplorationChestVisual>().Strength>0,"nearby fixed reward reveals its color");var pointer=chest.GetComponent<ChestDirectionIndicator>().Marker;Require(pointer==null||!pointer.gameObject.activeSelf,"nearby in-view chest remains discoverable without an edge arrow");}
    yield return PoiCapture("poi-colors-"+batch+"-"+(reborn?"reborn":"original")+".png");
   }
   RemovePoiFixture(run);yield return null;
  }
  // Keep a full set while the actual scheduler attempts repeated new waves.
  for(int i=0;i<3;i++)ExplorationFixture(run,ExplorationReward.Experience,(Vector2)origin+new Vector2(4,2*(i-1)));
  var ids=run.Chests.Select(c=>c.Drop.id).ToArray();for(int i=0;i<4;i++){run.State.remaining=0;yield return null;yield return null;Require(run.Chests.Select(c=>c.Drop.id).SequenceEqual(ids)&&run.Chests.All(c=>c.gameObject.activeSelf),"unclaimed POIs survive later wave boundaries without expiring");}
  var selected=run.Chests[1];var near=origin+new Vector3(2.5f,0,0);player.SetPosition(near);yield return null;yield return null;Require(selected.GetComponent<ExplorationChestVisual>().Strength==1,"full glow is reached within two world units");
  game.pauseController.Pause();game.ChangeState<PauseState>();yield return null;float strength=selected.GetComponent<ExplorationChestVisual>().Strength;yield return null;CombatClose(selected.GetComponent<ExplorationChestVisual>().Strength,strength,"pause introduces no timed glow or expiry");
  yield return MenuPointer(game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;
  foreach(var size in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})
  {
   Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);
   foreach(int zoom in new[]{0,100}){CameraZoom.Value=zoom;FeedbackCameras();yield return null;yield return null;foreach(var chest in run.Chests)PoiVisualContract(chest);yield return PoiCapture("poi-near-"+size.x+"-zoom"+zoom+".png");}
  }
  CameraZoom.Value=0;FeedbackCameras();int credits=run.State.credits;var marker=selected.GetComponent<ChestDirectionIndicator>();
  int beforeLevel=game.playerXP.level;float beforeXP=(float)WindField(game.playerXP,"xp");
  yield return ExplorationContact(selected,game);Require(run.State.credits==credits&&run.Chests.Count==2&&(game.playerXP.level>beforeLevel||(float)WindField(game.playerXP,"xp")>beforeXP),"actual visible chest contact grants XP once without credits");yield return null;Require(marker==null||marker.Marker==null||!marker.Marker.gameObject.activeSelf,"collection clears the nearby glow and pointer together");
  yield return AchievementExit(game,false);Require(ExplorationRun.Instance==null&&!Object.FindObjectsOfType<ExplorationChestVisual>().Any(),"scene exit removes all chest visuals");
  SetPhase("PASS");Log("complete","Native exploration reveal, all8 reward colors/near-far/pause/non-expiry/zoom/viewports/real collection and teardown");Application.Quit(0);
 }
}
