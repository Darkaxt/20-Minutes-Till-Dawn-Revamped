using System.Collections;
using System.IO;
using System.Runtime.Serialization.Json;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using flanne.Core;
namespace TwentyMinutes.Modernization;

// This is the original controller's reached damage-display behavior, not a
// generic performance monitor. Sampling state is static; the low-FPS run and
// once-disabled bit belong to each controller, as in the source.
public sealed class AndroidDamageDisplay:MonoBehaviour
{
 public class Contract{public float threshold,duration,interval,tipDuration;public string tip;}
 static Contract data;
 static int frames;
 static double lastInterval;
 static float fps;
 float lowFpsTimer;
 bool hasAutoDisabled;
 GameController owner;
 internal void Bind(GameController controller)
 {
  owner=controller;
  if(data==null)
   using(var stream=File.OpenRead(Path.Combine(Plugin.AssetRoot,"damage-display.json")))
    data=(Contract)new DataContractJsonSerializer(typeof(Contract)).ReadObject(stream);
 }
 void Update()
 {
  // Source Update is on this native controller itself.
  if(!owner.enabled)return;
  frames++;
  float now=Time.realtimeSinceStartup;
  if(lastInterval+data.interval<now)
  {
   // Source deliberately round-trips the previous double through current-
   // culture text before subtracting it from the current float clock.
   fps=frames/(now-float.Parse(lastInterval.ToString()));
   frames=0;lastInterval=now;
   Check();
  }
 }
 void Check()
 {
  if(hasAutoDisabled)return;
  if(PlayerPrefs.GetInt("ShowDamagePopup",1)==1&&fps>0&&fps<data.threshold)
  {
   lowFpsTimer+=data.interval;
   if(lowFpsTimer<data.duration)return;
   hasAutoDisabled=true;lowFpsTimer=0;
   PlayerPrefs.SetInt("ShowDamagePopup",0);
   StartCoroutine(ShowTip());
  }
  else lowFpsTimer=0;
 }
 IEnumerator ShowTip()
 {
  // Native, transparent presentation; no raycaster or gameplay input owner.
  var canvasObject=new GameObject("DamageDisplayNotice",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));canvasObject.transform.SetParent(transform,false);
  canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvasObject.GetComponent<Canvas>().sortingOrder=23;
  var scale=canvasObject.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);scale.matchWidthOrHeight=.5f;
  var label=StatsOverlay.Text(canvasObject.transform,"DamageDisplayNoticeText",owner.hud.GetComponentInChildren<TMP_Text>(true).font,data.tip,18,Vector2.zero,new Vector2(1100,48));
  StatsOverlay.Rect(label.gameObject,canvasObject.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,32),new Vector2(1100,48));label.alignment=TextAlignmentOptions.Center;
  yield return new WaitForSeconds(data.tipDuration);
  canvasObject.SetActive(false);
 }
}
[HarmonyPatch(typeof(GameController),"Awake")]
class AndroidDamageDisplayOwner
{static void Postfix(GameController __instance)=>__instance.gameObject.AddComponent<AndroidDamageDisplay>().Bind(__instance);}
