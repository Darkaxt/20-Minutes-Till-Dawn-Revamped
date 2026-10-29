using System;
using System.Linq;
using System.Collections;
using System.IO;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 void StatusGpu(SpriteRenderer body,MeshRenderer overlay,int index)
 {
  var go=new GameObject("StatusGpuCamera",typeof(Camera));var camera=go.GetComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=Mathf.Max(body.bounds.extents.x,body.bounds.extents.y)*1.2f;camera.aspect=1;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.cullingMask=1<<31;camera.nearClipPlane=.1f;camera.farClipPlane=20;camera.transform.position=body.bounds.center+Vector3.back*10;
  var target=new RenderTexture(256,256,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;var copy=new GameObject("ActualStatusGpuProbe",typeof(MeshFilter),typeof(MeshRenderer));copy.layer=31;copy.transform.position=overlay.transform.position;copy.transform.rotation=overlay.transform.rotation;copy.transform.localScale=overlay.transform.lossyScale;copy.GetComponent<MeshFilter>().sharedMesh=overlay.GetComponent<MeshFilter>().sharedMesh;
  var renderer=copy.GetComponent<MeshRenderer>();renderer.sharedMaterial=overlay.sharedMaterial;var block=new MaterialPropertyBlock();overlay.GetPropertyBlock(block);renderer.SetPropertyBlock(block);
  camera.Render();var prior=RenderTexture.active;RenderTexture.active=target;var pixels=new Texture2D(256,256,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,256,256),0,0);pixels.Apply();var actual=pixels.GetPixels32();File.WriteAllBytes(Path.Combine(ResultDir,"gpu-status-"+index+".png"),pixels.EncodeToPNG());
  int colored=actual.Count(c=>c.a>8);Log("status-gpu",overlay.name+" colored="+colored+" queue="+renderer.sharedMaterial.renderQueue+" shader="+renderer.sharedMaterial.shader.name+" keywords="+string.Join(",",renderer.sharedMaterial.shaderKeywords)+" faceScale="+renderer.sharedMaterial.GetTextureScale("_FaceTex"));
  renderer.enabled=false;var reference=new GameObject("NativeAlphaReference",typeof(SpriteRenderer));reference.layer=31;reference.transform.position=body.transform.position;reference.transform.rotation=body.transform.rotation;reference.transform.localScale=body.transform.lossyScale;var sprite=reference.GetComponent<SpriteRenderer>();sprite.sprite=body.sprite;sprite.flipX=body.flipX;sprite.flipY=body.flipY;camera.Render();pixels.ReadPixels(new Rect(0,0,256,256),0,0);pixels.Apply();var alpha=pixels.GetPixels32();int outside=Enumerable.Range(0,actual.Length).Count(i=>actual[i].a>8&&alpha[i].a<8);RenderTexture.active=prior;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(pixels);Object.Destroy(copy);Object.Destroy(reference);Object.Destroy(go);
  Require(colored>8&&outside==0,"actual GPU output has colored pattern pixels and none outside the native sprite alpha: "+index);
 }
 IEnumerator CheckElementVisuals()
 {
  yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;
  var origin=p.transform.position;var victims=new[]{ElementEnemy((Vector2)origin+new Vector2(-3,2)),ElementEnemy((Vector2)origin+new Vector2(0,3),"EyeMonster"),ElementEnemy((Vector2)origin+new Vector2(3,2),"Boomer"),ElementEnemy((Vector2)origin+new Vector2(0,-2))};
  foreach(var h in victims)
  {
   // Boomer's pooled prefab has no idle sprite until its native AI starts the
   // move animation. Let the real actor initialize before freezing the fixture.
   var ai=h.GetComponent<AIComponent>();var move=h.GetComponent<MoveComponent2D>();ai.enabled=true;move.enabled=true;while(h.GetComponent<SpriteRenderer>().sprite==null)yield return null;ai.enabled=false;move.enabled=false;
  }
  var enhanced=victims[3].gameObject.AddComponent<EnhancedEnemy>();ElementCall(enhanced,"Promote",2);
  foreach(var size in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})foreach(bool reborn in new[]{false,true})foreach(int zoom in new[]{0,100})
  {
   Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);CameraZoom.Value=zoom;if(ColorfulStyle.Instance.Enabled!=reborn)ColorfulStyle.Instance.Toggle();
   for(int i=0;i<victims.Length;i++)
   {
    var h=victims[i];var source=h.GetComponent<SpriteRenderer>();source.flipX=i%2==0;var material=source.sharedMaterial;
    if(i==0||i==3)BurnSystem.SharedInstance.Burn(h.gameObject,1);
    var c=(ElementalControl)AccessTools.Method(typeof(ElementalControl),"Get").Invoke(null,new object[]{h.gameObject});
    if(i==1||i==3)ElementCall(c,"Electrocute");if(i==2||i==3)ElementCall(c,"Disorient");
    yield return null;yield return null;
    var visual=h.GetComponent<ElementalStatusVisual>();Require(visual!=null,"actual elemental owner creates the sprite visual");
    var overlays=h.GetComponentsInChildren<MeshRenderer>().Where(r=>r.name.StartsWith("ElementalSprite_")&&r.enabled).ToArray();Log("status-render-facts",h.name+" sprite="+source.sprite?.name+" enabled="+source.enabled+" alpha="+source.color.a+" hp="+h.HP+" dead="+h.isDead+" stun="+c.Stun+" dizzy="+c.Dizzy+" burn="+BurnSystem.SharedInstance.IsBurning(h.gameObject)+" overlays="+string.Join(",",overlays.Select(r=>r.name)));if(overlays.Length!=(i==3?3:1))yield return PoiCapture("status-failure.png");Require(overlays.Length==(i==3?3:1),"concurrent statuses keep their independent masked layers");
    foreach(var overlay in overlays)
    {
     var mesh=overlay.GetComponent<MeshFilter>().sharedMesh;Require(overlay.sharedMaterial.shader.name=="TextMeshPro/Distance Field"&&overlay.sharedMaterial.shader.isSupported,"the actual supported GPU mask/flow shader renders each status");
     Require(mesh.uv.SequenceEqual(source.sprite.uv)&&mesh.vertices.Length==source.sprite.vertices.Length,"effect uses the native animated sprite mesh and atlas UVs");
     Require(overlay.gameObject.layer==source.gameObject.layer&&overlay.sortingLayerID==source.sortingLayerID&&overlay.transform.localScale.x==(source.flipX?-1:1),"effect preserves fog layer, sorting and native facing");
    }
    Require(!source.sharedMaterial.name.StartsWith("Revamped_")&&!h.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.name=="ElementalStatus"),"overlay leaves the native material/real burn-hit flash owner intact and removes the status disk");
    if(i==3)Require(h.transform.Find("EliteContour").GetComponent<SpriteRenderer>().enabled,"mixed sprite effects coexist with the elite's independent outside contour and enlarged body");
    if(size.x==900&&!reborn&&zoom==0&&i<3)StatusGpu(source,overlays.Single(),i);
   }
   yield return PoiCapture("sprite-status-"+size.x+"-"+(reborn?"reborn":"original")+"-"+zoom+".png");
  }
  var fire=victims[0];var control=fire.GetComponent<ElementalControl>();BurnSystem.SharedInstance.Burn(fire.gameObject,1);
  while(control.Slow>0)yield return null;Require(BurnSystem.SharedInstance.IsBurning(fire.gameObject),"burn continues after panic and slow finish");yield return null;Require(fire.GetComponentsInChildren<MeshRenderer>().Any(r=>r.name=="ElementalSprite_0"&&r.enabled),"fire remains visible for the actual burn lifetime");
  game.pauseController.Pause();float clock=Time.time;yield return null;yield return null;Require(Time.time==clock,"the native pause freezes the shader's combat clock");game.pauseController.UnPause();
  while(BurnSystem.SharedInstance.IsBurning(fire.gameObject))yield return null;yield return null;Require(!fire.GetComponentsInChildren<MeshRenderer>().Any(r=>r.enabled),"effect expiry disables its visual");
  foreach(var h in victims){ReturnFixtureActor(h.gameObject);Require(!h.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.enabled),"pool return hides every status layer");}
  var reused=ObjectPooler.SharedInstance.GetPooledObject("BrainMonster");Require(reused==victims[3].gameObject&&!reused.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.enabled)&&reused.GetComponent<ElementalControl>().Stun==0,"actual LIFO pool reuses the last returned mixed-status actor without previous status");ReturnFixtureActor(reused);
  yield return AchievementExit(game,false);Require(Object.FindObjectsOfType<ElementalStatusVisual>().Length==0,"scene teardown releases actors and status overlays");SetPhase("PASS");Log("complete","Actual burn/stun/disorientation/mixed shaders, native mesh/atlas/facing/fog, both styles/three viewports/zoom, burn lifetime, native pause, expiry, pool return and scene teardown");Application.Quit(0);
 }
}
