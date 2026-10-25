using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using HarmonyLib;
using UnityEngine;
using UnityEngine.U2D;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization;

public static class AndroidCameraHeight
{
 public class MapHeight{public string name;public int height;}
 public class Contract{public MapHeight[] maps;}
 static Contract data;
 internal static void Configure(GameController owner)
 {
  if(data==null)
   using(var stream=File.OpenRead(Path.Combine(Plugin.AssetRoot,"camera-height.json")))
    data=(Contract)new DataContractJsonSerializer(typeof(Contract)).ReadObject(stream);
  int height=data.maps.Single(m=>m.name==SelectedMap.MapData.name).height;
  var cameras=Resources.FindObjectsOfTypeAll<PixelPerfectCamera>().Where(c=>c.gameObject.scene==owner.gameObject.scene).ToArray();
  // Keep the three original native UI/main/fog roles. Source's separate
  // damage-only camera renders the popup layer; A227 maps those producers to
  // native popup/font presentation rather than adding a mobile render pass.
  foreach(string name in new[]{"UICam","Main Camera","VisibleInFogCamera"})
  {
   var camera=cameras.Single(c=>c.name==name);
   camera.refResolutionY=height;
   camera.refResolutionX=(int)Math.Floor((float)Screen.width/Screen.height*camera.refResolutionY)&~1;
  }
  owner.gameObject.AddComponent<GameplayView>().Initialize(height,cameras);
 }
}

// Keep native reference heights and pixel-perfect calculation; zoom only the
// final world projection. This scene owner follows actual viewport transitions.
public sealed class GameplayView:MonoBehaviour
{
 internal static GameplayView Instance;
 internal PixelPerfectCamera[] Cameras;int height,width,pixelHeight;
 internal void Initialize(int sourceHeight,PixelPerfectCamera[] cameras)
 {Instance=this;height=sourceHeight;Cameras=cameras.Where(c=>new[]{"UICam","Main Camera","VisibleInFogCamera"}.Contains(c.name)).ToArray();Resize();}
 void Update(){if(width!=Screen.width||pixelHeight!=Screen.height)Resize();}
 void Resize()
 {
  width=Screen.width;pixelHeight=Screen.height;
  foreach(var camera in Cameras){camera.refResolutionY=height;camera.refResolutionX=(int)Math.Floor((float)width/pixelHeight*height)&~1;}
 }
 void OnDestroy(){if(Instance==this)Instance=null;}
}
[HarmonyPatch(typeof(PixelPerfectCamera),"OnPreCull")]
class GameplayCameraZoom
{
 static void Postfix(PixelPerfectCamera __instance)
 {
  var view=GameplayView.Instance;
  if(view!=null&&view.Cameras.Contains(__instance))__instance.GetComponent<Camera>().orthographicSize*=1+Plugin.Instance.ZoomOut.Value/100f;
 }
}
[HarmonyPatch(typeof(GameController),"Awake")]
class AndroidCameraHeightOwner
{static void Postfix(GameController __instance)=>AndroidCameraHeight.Configure(__instance);}

// Source CameraRig.Update is empty. The unchanged Awake/Start and zero-offset
// child hierarchy already provide player follow/shake; ShootingCursor and the
// native mouse/controller actions independently own aiming. Windows' additional
// cursor-driven camera offset is not part of that source view behavior.
[HarmonyPatch(typeof(CameraRig),"Update")]
class AndroidCameraFollow
{static bool Prefix()=>false;}
