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
 }
}
[HarmonyPatch(typeof(GameController),"Awake")]
class AndroidCameraHeightOwner
{static void Postfix(GameController __instance)=>AndroidCameraHeight.Configure(__instance);}
