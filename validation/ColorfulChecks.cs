using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization.Validation;
using TwentyMinutes.Modernization;
public partial class RuntimeChecks
{
 IEnumerator CheckColorful(GameController game,PlayerController player,Mouse mouse,Keyboard keyboard)
 {
  SetPhase("colorful-combat");var style=ColorfulStyle.Instance;
  Require(style.Enabled && style.AppliedCount>0,"Colorful follows transition from title into actual combat");
  foreach(var name in new[]{"T_Death_Sword_Spin","T_Katana_SS","T_Chest","T_ForestTiles"})
  {
   var texture=Resources.FindObjectsOfTypeAll<Texture2D>().Single(t=>t.name==name);
   var copy=ColorfulStyle.Sample(texture);try{File.WriteAllBytes(Path.Combine(ResultDir,name+".gpu.png"),copy.EncodeToPNG());}finally{UnityEngine.Object.Destroy(copy);}
   Require(texture.width>0,"extended artwork loaded through native texture: "+name);
  }
  while(game.hud.GetComponentsInChildren<Transform>(true).Any(x=>LeanTween.isTweening(x.gameObject)))yield return null;
  yield return Capture("combat-colorful.png");
  Log("diagnostic-action","Enter native pause state directly, then use native Options and visual setting buttons");
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.optionsButton);
  while(!(game.CurrentState is OptionsState) || !Active(game.optionsMenu) || game.optionsMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  Click(style.SettingButton);Require(!style.Enabled && style.LastRestored>0 && style.LastRestoreFailures==0,"disabling restores exact original RGBA pixels for every changed loaded sheet");
  int restored=style.LastRestored;
  Click(style.SettingButton);Require(style.Enabled && style.AppliedCount==restored,"visual style can be enabled again after restoration");
  Click(style.SettingButton);Require(!style.Enabled && style.LastRestored==restored && style.LastRestoreFailures==0,"second toggle cycle also restores exact original sampled pixels");
  yield return Capture("combat-options-original.png");
  // Resume through the game's native Options Back and Pause Resume controls.
  Click(Entries(game.optionsMenu)[0].button);while(!(game.CurrentState is PauseState))yield return null;
  Click(game.pauseResumeButton);while(!(game.CurrentState is CombatState) || PauseController.isPaused)yield return null;
  yield return Capture("combat-original-restored.png");
  yield return Shot(mouse,.1f,Evolution.None,20,1,null);
  Require(style.AppliedCount==0 && !style.Enabled,"Original remains active after normal gameplay input");
  File.WriteAllText(Path.Combine(ResultDir,"colorful-restoration.txt"),"Exact original RGBA SHA-256 matches for "+restored+" loaded texture sheets.\nSetting returned to Original; underlying sprites/animators retained.\n");
 }
}
