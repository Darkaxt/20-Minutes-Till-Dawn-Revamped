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
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 [Serializable]class CornerNode{public string path,scripts,text,sprite,layout;public Vector2 min,max,anchorMin,anchorMax,pivot,size;public bool active;}
 [Serializable]class CornerTree{public int width,height;public string character,portrait;public List<CornerNode> nodes=new List<CornerNode>();}
 IEnumerator InspectCornerHud()
 {
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return null;Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
  var snapshot=new CornerTree{width=Screen.width,height=Screen.height,character=PlayerController.Instance.loadedCharacter.name,portrait=PlayerController.Instance.loadedCharacter.portrait.name};
  foreach(var view in game.hud.GetComponentsInChildren<RectTransform>(true))
  {
   var corners=new Vector3[4];view.GetWorldCorners(corners);var points=corners.Select(p=>RectTransformUtility.WorldToScreenPoint(null,p)).ToArray();var text=view.GetComponent<TMP_Text>();var image=view.GetComponent<Image>();
   snapshot.nodes.Add(new CornerNode{path=string.Join("/",view.GetComponentsInParent<Transform>(true).Reverse().Select(t=>t.name)),scripts=string.Join(",",view.GetComponents<MonoBehaviour>().Select(b=>b.GetType().FullName)),text=text?.text,sprite=image?.sprite?.name,layout=string.Join(",",view.GetComponents<LayoutGroup>().Select(l=>l.GetType().Name)),min=new Vector2(points.Min(p=>p.x),points.Min(p=>p.y)),max=new Vector2(points.Max(p=>p.x),points.Max(p=>p.y)),anchorMin=view.anchorMin,anchorMax=view.anchorMax,pivot=view.pivot,size=view.sizeDelta,active=view.gameObject.activeInHierarchy});
  }
  using(var stream=File.Create(Path.Combine(ResultDir,"corner-native-tree.json")))new DataContractJsonSerializer(typeof(CornerTree)).WriteObject(stream,snapshot);
  foreach(var data in Resources.FindObjectsOfTypeAll<CharacterData>().Where(d=>d.portrait!=null).OrderBy(d=>d.name))Log("corner-portrait",data.name+" "+data.portrait.name+" rect="+data.portrait.rect+" pivot="+data.portrait.pivot+" packed="+data.portrait.packed);
  foreach(var sprite in Resources.FindObjectsOfTypeAll<Image>().Where(i=>i.sprite!=null&&(i.sprite.name.ToLowerInvariant().Contains("circle")||i.sprite.name.ToLowerInvariant().Contains("ring")||i.sprite.name.ToLowerInvariant().Contains("character"))).Select(i=>i.sprite).Distinct())Log("corner-frame-art",sprite.name+" rect="+sprite.rect);
  CornerExportSprite(PlayerController.Instance.loadedCharacter.portrait,"portrait-shana-native.png");CornerExportSprite(Resources.FindObjectsOfTypeAll<Image>().First(i=>i.sprite!=null&&i.sprite.name=="T_Circle").sprite,"circle-native.png");
  yield return PoiCapture("corner-native-before.png");SetPhase("DIAGNOSTIC_COMPLETE");Application.Quit(0);
 }
 Rect CornerBounds(RectTransform view)
 {var points=new Vector3[4];view.GetWorldCorners(points);return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));}
 IEnumerator CornerReady(GameController game)
 {
  RectTransform root=null;
  while(root==null){root=game.hud.transform.Find("CornerStatusHud") as RectTransform;yield return null;}
  float scale=Mathf.Sqrt((float)Screen.width/1280*Screen.height/720);
  while(Mathf.Abs(CornerBounds(root).yMax-(Screen.safeArea.yMax-12*scale))>1||game.hud.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  Canvas.ForceUpdateCanvases();yield return new WaitForEndOfFrame();
 }
 void CornerGeometry(GameController game,string state)
 {
  var root=(RectTransform)game.hud.transform.Find("CornerStatusHud");var bounds=CornerBounds(root);var score=game.hud.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="RunCredits");score.ForceMeshUpdate();float bottom=score.transform.TransformPoint(score.textBounds.min).y;
  Require(bounds.xMin>=Screen.safeArea.xMin-1&&bounds.yMax<=Screen.safeArea.yMax+1&&bounds.yMin>=bottom-1,"entire corner respects safe area and ends above Score ink: "+state+" bounds="+bounds+" scoreBottom="+bottom);
  Require(!game.hud.transform.Find("LevelDisplay").gameObject.activeSelf&&root.Find("XPBar")!=null,"full-width level display is replaced by the native local XP widget");
  Require(CornerBounds((RectTransform)root.Find("XPBar")).width<bounds.width&&CornerBounds((RectTransform)root.Find("XPBar")).height<=8,"native XP progress is a short thin local bar");
  var face=root.GetComponentsInChildren<Image>().Single(i=>i.name=="CharacterFace");Require(face.sprite==PlayerController.Instance.loadedCharacter.portrait,"avatar shows the actual selected character portrait");
  foreach(var text in root.GetComponentsInChildren<TMP_Text>()){text.ForceMeshUpdate();var r=CornerBounds(text.rectTransform);Require(r.yMin>=bottom-1,"status text stays within the Score height limit: "+text.name);}
  var level=root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="LvlValue");Require(int.Parse(level.text)==game.playerXP.level,"native level variable still updates the caption");
  var prefix=root.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="LevelPrefix");var caption=(RectTransform)root.Find("LevelCaption");var portrait=(RectTransform)root.Find("CharacterPortrait");
  Require(prefix.text=="Lvl."&&level.transform.parent==caption&&caption.GetComponentsInChildren<Image>().Length==0,"level prefix and native value share a background-free external caption");
  Require(CornerBounds(caption).yMax<CornerBounds(portrait).yMin&&Mathf.Abs(CornerBounds(caption).center.x-CornerBounds(portrait).center.x)<.1f,"caption is centered entirely below the avatar");
  Require(Mathf.Abs(level.fontSize/root.Find("AmmoDisplay/AmmoCount").GetComponent<TMP_Text>().fontSize-.75f)<.001f&&prefix.fontSize==level.fontSize,"level caption uses75% of the native ammunition font size");
  var ammo=root.Find("AmmoDisplay");Require(int.Parse(ammo.Find("AmmoCount").GetComponent<TMP_Text>().text)==PlayerController.Instance.ammo.amount,"native ammo binding matches actual amount");
  Require(Mathf.Abs(level.canvasRenderer.GetInheritedAlpha()-Plugin.Instance.HudOpacity.Value/100f)<.002f,"compact HUD inherits opacity exactly once");
  Log("corner-layout",state+" "+Screen.width+"x"+Screen.height+" bounds="+bounds+" scoreBottom="+bottom+" portrait="+face.sprite.name);
 }
 IEnumerator CheckCornerHud(string mode)
 {
  if(mode=="corner-hud-inspect"){yield return InspectCornerHud();yield break;}
  bool continuation=mode.EndsWith("continue");
  if(continuation)yield return CombatBuffContinue();else{RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());}
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return CornerReady(game);var player=PlayerController.Instance;
  if(mode=="corner-hud-caption")
  {
   yield return AchievementPick(game,"Haste",36);yield return CornerReady(game);Require(game.playerXP.level==37,"native level-up establishes the requested level37 example");
   foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(720,720),new Vector2Int(1920,1080)})
   {Screen.SetResolution(size.x,size.y,false);while(Screen.width!=size.x||Screen.height!=size.y)yield return null;FeedbackCameras();yield return CornerReady(game);CornerGeometry(game,"external-level37");yield return PoiCapture("level-caption-"+size.x+".png");}
   yield return AchievementPick(game,"QuickHands",37);yield return CornerReady(game);CornerGeometry(game,"next-native-level-up");Require(game.playerXP.level==38,"native value updates to38 without replacing its variable binding");
   player.playerHealth.maxHP=12;player.playerHealth.hp=9;player.playerHealth.maxSHP=3;player.playerHealth.shp=3;yield return null;yield return new WaitForEndOfFrame();CornerGeometry(game,"expanded-health-caption");yield return PoiCapture("level-caption-expanded.png");
   yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","External Lvl.37 caption at three viewports,75% font ratio, Score-height limit, native update to38 and expanded hearts verified. Native portrait/frame and unrelated bindings retained.");Application.Quit(0);yield break;
  }
  if(mode=="corner-hud-frame")
  {
   foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(720,720),new Vector2Int(1920,1080)})
   {Screen.SetResolution(size.x,size.y,false);while(Screen.width!=size.x||Screen.height!=size.y)yield return null;FeedbackCameras();yield return CornerReady(game);CornerGeometry(game,"pixel-frame");yield return PoiCapture("corner-hud-"+size.x+".png");}
   var ring=game.hud.GetComponentInChildren<CornerPortraitRing>();var mesh=new Mesh();using(var helper=new VertexHelper()){AccessTools.Method(typeof(CornerPortraitRing),"OnPopulateMesh",new[]{typeof(VertexHelper)}).Invoke(ring,new object[]{helper});helper.FillMesh(mesh);}var vertices=mesh.vertices;
   Require(vertices.Length>0&&vertices.Length%4==0,"pixel frame has raster-cell quads");
   for(int i=0;i<vertices.Length;i+=4)Require(vertices[i].x==vertices[i+1].x&&vertices[i+1].y==vertices[i+2].y&&vertices[i+2].x==vertices[i+3].x&&vertices[i+3].y==vertices[i].y,"frame cell edges are horizontal/vertical");Object.Destroy(mesh);
   yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Stepped frame, unchanged native portrait and Score-height bounds at three viewports; prior live-binding/Continue acceptance reused.");Application.Quit(0);yield break;
  }
  if(!continuation)
  {
   int hp=player.playerHealth.hp;player.playerHealth.hp=hp-1;yield return null;Require(player.playerHealth.hp==hp-1&&game.hud.GetComponentsInChildren<MonoBehaviour>().Single(b=>b.GetType().FullName=="flanne.UI.HealthUI").enabled,"native health setter/binding stays live in the compact row");player.playerHealth.hp=hp;
   float required=(float)AccessTools.Property(typeof(flanne.Player.PlayerXP),"xpToLevel").GetValue(game.playerXP);game.playerXP.GainXP(required*.2f);var slider=game.hud.GetComponentInChildren<Slider>();while(slider.normalizedValue<=.01f)yield return null;Require(slider.normalizedValue<1,"actual XP gain advances the native local progress bar");
   player.ammo.UseAmmo(2);yield return null;CornerGeometry(game,"ammo-consumed");player.ammo.Reload();while(player.ammo.amount<player.ammo.max)yield return null;yield return null;CornerGeometry(game,"reload-complete");
   yield return AndroidPick(game,"Haste",game.playerXP.level);yield return CornerReady(game);CornerGeometry(game,"actual-level-up");
  }
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(720,720),new Vector2Int(1920,1080)})
  {
   Screen.SetResolution(size.x,size.y,false);while(Screen.width!=size.x||Screen.height!=size.y)yield return null;FeedbackCameras();yield return CornerReady(game);CornerGeometry(game,"fresh-or-continue");yield return PoiCapture("corner-hud-"+size.x+".png");
  }
  int max=player.playerHealth.maxHP,hpBefore=player.playerHealth.hp,capacity=player.playerHealth.maxSHP,souls=player.playerHealth.shp;
  player.playerHealth.maxHP=12;player.playerHealth.hp=9;player.playerHealth.maxSHP=3;player.playerHealth.shp=3;yield return null;yield return new WaitForEndOfFrame();CornerGeometry(game,"expanded-health-and-souls");
  var hearts=(RectTransform)game.hud.transform.Find("CornerStatusHud/HealthUI");foreach(var item in hearts.Cast<Transform>().Where(t=>t.gameObject.activeSelf)){var r=CornerBounds((RectTransform)item);Require(r.yMin>=CornerBounds((RectTransform)game.hud.transform.Find("CornerStatusHud")).yMin-1,"expanded native heart stays inside compact cluster");}
  yield return PoiCapture("corner-expanded-hearts.png");player.playerHealth.maxHP=max;player.playerHealth.hp=hpBefore;player.playerHealth.maxSHP=capacity;player.playerHealth.shp=souls;
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return null;yield return Capture("corner-pause.png");Click(game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;yield return CornerReady(game);CornerGeometry(game,"pause-resume");
  if(!continuation){yield return DragonSuspend(game,"Compact native HUD live health/XP/ammo/level, three viewports, expanded red/soul health and save-and-return verified.");yield break;}
  yield return AchievementExit(game,false);Progression.State.earned.Add("character:Revamped_Dimond_Portrait");Progression.Refresh();yield return AchievementRun("Revamped_Dimond_Portrait","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return CornerReady(game);CornerGeometry(game,"ported-character-portrait");yield return PoiCapture("corner-bulwark-portrait.png");
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Compact native corner HUD, actual live bindings/level-up, safe-area/Score-height bounds in square/16:9, expanded health, Continue and ported portrait.");Application.Quit(0);
 }
 void CornerExportSprite(Sprite sprite,string name)
 {
  var texture=sprite.texture;var target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);var previous=RenderTexture.active;var image=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
  try{Graphics.Blit(texture,target);RenderTexture.active=target;image.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(ResultDir,name),image.EncodeToPNG());}
  finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.Destroy(image);}
 }
}
