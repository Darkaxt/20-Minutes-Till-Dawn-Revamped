using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization;

// Reposition the original timer/objective components; their native update and
// visibility owners remain intact. Credits only read the existing run score.
public sealed class GameplayHudLayout:MonoBehaviour
{
 TMP_Text credits;int previousCredits=-1;GameController owner;CanvasGroup[] clock;
 internal void Initialize(GameController game)
 {
  owner=game;
  // Native gameplay menus share this screen-space canvas. PixelPerfectCamera
  // changes its render viewport after Canvas layout, leaving stale dimensions
  // on square resizes/zoom. Screen UI must use the actual display directly.
  var canvas=game.hud.GetComponentInParent<Canvas>().rootCanvas;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;
  var texts=game.hud.GetComponentsInChildren<TMP_Text>(true);var time=texts.Single(t=>t.name=="Timetext");var objective=texts.Single(t=>t.name=="SurviveText");
  StatsOverlay.Rect(time.gameObject,game.hud.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-48),new Vector2(300,42));
  time.fontSize=32;time.alignment=TextAlignmentOptions.Center;
  StatsOverlay.Rect(objective.gameObject,game.hud.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-116),new Vector2(400,26));
  objective.fontSize=18;objective.alignment=TextAlignmentOptions.Center;
  credits=StatsOverlay.Text(game.hud.transform,"RunCredits",time.font,"Credits: 0",20,Vector2.zero,new Vector2(400,28));
  StatsOverlay.Rect(credits.gameObject,game.hud.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-88),new Vector2(400,28));credits.alignment=TextAlignmentOptions.Center;
  clock=new[]{time,credits,objective}.Select(t=>t.gameObject.AddComponent<CanvasGroup>()).ToArray();
 }
 void LateUpdate()
 {
  int value=ScoreCalculator.SharedInstance.GetScore().totalScore;
  if(value!=previousCredits){previousCredits=value;credits.text="Credits: "+value.ToString("N0");}
  bool choosing=owner.CurrentState is PowerupMenuState||owner.CurrentState is DevilDealState||owner.CurrentState is ChestState||owner.CurrentState is ExplorationRewardState;
  foreach(var group in clock)group.alpha=choosing?0:1;
 }
}
[HarmonyPatch(typeof(GameController),"Awake")]
class CenterGameplayTimer
{static void Postfix(GameController __instance){__instance.hud.gameObject.AddComponent<GameplayHudLayout>().Initialize(__instance);}}
