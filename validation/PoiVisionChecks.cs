using System;
using System.IO;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 [Serializable]class VisionFacts
 {public bool active,hierarchy;public Vector3 scale;public float range;public string[] renderers;}
 IEnumerator CheckPoiVision(bool restore)
 {
  if(restore)yield return CombatBuffContinue();
  else{RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest");}
  var game=Object.FindObjectOfType<GameController>();var player=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  while(LeanTween.isTweening(game.playerFogRevealer))yield return null;
  if(!restore){yield return AndroidPick(game,"Revamped_NightVision",game.playerXP.level);while(LeanTween.isTweening(game.playerFogRevealer))yield return null;}
  yield return null;CameraZoom.Value=0;FeedbackCameras();
  var root=game.playerFogRevealer;PoiRenderFacts(root,restore?"continued-player-vision":"fresh-player-vision");
  var facts=new VisionFacts{active=root.activeSelf,hierarchy=root.activeInHierarchy,scale=root.transform.localScale,range=player.stats[StatType.VisionRange].Modify(2),renderers=root.GetComponentsInChildren<SpriteRenderer>(true).Select(r=>r.name+"|"+r.enabled+"|"+r.gameObject.activeSelf+"|"+r.transform.localScale+"|"+r.color).ToArray()};
  File.WriteAllText(Path.Combine(ResultDir,"vision-facts.json"),JsonUtility.ToJson(facts,true));yield return PoiCapture(restore?"vision-continued-idle.png":"vision-fresh-idle.png");
  if(restore)
  {
   var before=JsonUtility.FromJson<VisionFacts>(File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR"),"vision-facts.json")));
   Require(facts.active==before.active&&facts.hierarchy==before.hierarchy,"Continue restores native idle vision activation");
   Require(facts.scale==before.scale&&facts.range==before.range&&facts.renderers.SequenceEqual(before.renderers),"Continue restores native idle reveal scale, renderers and earned vision range");
   yield return AchievementExit(game,false);SetPhase("PASS");Application.Quit(0);
  }
  else{RunResume.Save();yield return DragonSuspend(game,"Native fresh idle player vision and checkpoint for fresh-process Continue");}
 }
}
