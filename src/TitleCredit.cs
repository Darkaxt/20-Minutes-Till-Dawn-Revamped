using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using flanne.TitleScreen;
namespace TwentyMinutes.Modernization;

// Original native-font credit treatment; no third-party title image or scaling.
public class TitleCredit:MonoBehaviour
{
 public const string Credit="A game by Flanne, maintained by Darkaxt";
 public TMP_Text Label;TitleScreenController owner;
 public static void Attach(TitleScreenController title)
 {
  if(title.GetComponent<TitleCredit>()!=null)return;
  var credit=title.gameObject.AddComponent<TitleCredit>();credit.owner=title;
  var root=new GameObject("RevampedTitleCredit",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));root.transform.SetParent(title.transform,false);
  root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=19;
  var scale=root.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1280,720);scale.matchWidthOrHeight=.5f;
  credit.Label=StatsOverlay.Text(root.transform,"Authors",title.modeSelectStartButton.GetComponentInChildren<TMP_Text>(true).font,Credit,18,Vector2.zero,new Vector2(600,28));
  StatsOverlay.Rect(credit.Label.gameObject,root.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,22),new Vector2(600,28));credit.Label.alignment=TextAlignmentOptions.Center;
 }
 void LateUpdate(){if(Label!=null)Label.gameObject.SetActive(owner.CurrentState is TitleMainMenuState);}
}
[HarmonyPatch(typeof(TitleScreenController),"Start")]
class AddTitleCredit{static void Postfix(TitleScreenController __instance)=>TitleCredit.Attach(__instance);}
