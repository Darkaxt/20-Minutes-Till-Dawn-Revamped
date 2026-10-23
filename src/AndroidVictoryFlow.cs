using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(PlayerSurvivedState),nameof(PlayerSurvivedState.Enter))]
class AndroidVictoryFlow
{
 static readonly AccessTools.FieldRef<GameState,GameController> Owner=AccessTools.FieldRefAccess<GameState,GameController>("owner");
 static readonly MethodInfo Retry=AccessTools.Method(typeof(PlayerSurvivedState),"OnClickRetry"),Quit=AccessTools.Method(typeof(PlayerSurvivedState),"OnClickQuit");
 static readonly Action<PlayerSurvivedState> Difficulty=(Action<PlayerSurvivedState>)Delegate.CreateDelegate(typeof(Action<PlayerSurvivedState>),AccessTools.Method(typeof(PlayerSurvivedState),"CheckDifficultyUnlock"));
 static readonly Action<PlayerSurvivedState> Achievements=(Action<PlayerSurvivedState>)Delegate.CreateDelegate(typeof(Action<PlayerSurvivedState>),AccessTools.Method(typeof(PlayerSurvivedState),"CheckAchievmentUnlocks"));
 static bool Prefix(PlayerSurvivedState __instance)
 {
  ResumeEndOfRun.Finish();
  var owner=Owner(__instance);
  // Native Stop preserves elapsed time, matching source Stop(false).
  GameTimer.SharedInstance.Stop();
  // Normal runs retain retry. Android's rental/trial restriction is excluded.
  owner.retryRunButton.gameObject.SetActive(true);
  owner.retryRunButton.onClick.AddListener((UnityAction)Delegate.CreateDelegate(typeof(UnityAction),__instance,Retry));
  owner.quitToTitleButton.onClick.AddListener((UnityAction)Delegate.CreateDelegate(typeof(UnityAction),__instance,Quit));
  AudioManager.Instance.SetLowPassFilter(true);
  __instance.StartCoroutine(Present(__instance,owner));
  return false;
 }
 static IEnumerator Present(PlayerSurvivedState state,GameController owner)
 {
  // The source's misleadingly named KillEnemiesCR only presents results.
  // Enemy cleanup belongs to the preceding KillEnemiesState.
  owner.youSurvivedSFX.Play();
  owner.hud.Hide();
  yield return new WaitForSecondsRealtime(.1f);
  owner.endScreenUIC.SetScores(ScoreCalculator.SharedInstance.GetScore());
  owner.endScreenUIC.Show(true);
  owner.powerupListUI.Show();
  // Keep the native Windows end-screen loadout presentation.
  owner.loadoutUI.Show();
  if(!SelectedMap.MapData.achievementsDisabled){Difficulty(state);Achievements(state);}
  Progression.Survived();
 }
}
