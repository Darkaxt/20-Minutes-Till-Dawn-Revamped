using System;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.UI;
namespace TwentyMinutes.Modernization;

// A95's finite list is also the native UI's bound. Never leave old entries
// selectable when a subsequent offer contains fewer candidates.
static class AndroidOfferMenus
{
 internal static void Bind(PowerupMenu menu,List<Powerup> choices)
 {
  var toggles=(Toggle[])AccessTools.Field(typeof(ToggleGroupMenu<Powerup>),"toggles").GetValue(menu);
  var group=(ToggleGroup)AccessTools.Field(typeof(ToggleGroupMenu<Powerup>),"toggleGroup").GetValue(menu);bool allowSwitchOff=group.allowSwitchOff;group.allowSwitchOff=true;
  for(int i=0;i<toggles.Length;i++)
  {
   // Native SetData/OnToggleChanged refresh the description immediately and
   // require a selected non-null value. Bind the new entries as one operation
   // before publishing its selection, including the valid empty-list case.
   toggles[i].SetIsOnWithoutNotify(false);
   if(i<choices.Count)toggles[i].GetComponent<DataUIBinding<Powerup>>().data=choices[i];
   menu.SetActive(i,i<choices.Count);
  }
  if(choices.Count>0)toggles[0].SetIsOnWithoutNotify(true);group.allowSwitchOff=allowSwitchOff;
  if(choices.Count>0){AccessTools.Property(typeof(ToggleGroupMenu<Powerup>),"currIndex").SetValue(menu,0);menu.RefreshDescription();}
 }
 internal static T Handler<T>(object owner,string method) where T:Delegate=>(T)Delegate.CreateDelegate(typeof(T),owner,AccessTools.Method(owner.GetType(),method));
 internal static IEnumerator End(PowerupMenuState state)=>(IEnumerator)AccessTools.Method(typeof(PowerupMenuState),"EndLevelUpAnimationCR").Invoke(state,null);
 internal static void Resume(GameController game){if(game.playerHealth.hp!=0)game.ChangeState<CombatState>();else game.ChangeState<PlayerDeadState>();}
}
[HarmonyPatch(typeof(PowerupMenuState),"GeneratePowerups")]
class AndroidFiniteLevelOffers
{
 static bool Prefix(GameController ___owner,ref List<Powerup> ___powerupChoices)
 {___powerupChoices=___owner.powerupGenerator.GetRandom(___owner.numPowerupChoices);AndroidOfferMenus.Bind(___owner.powerupMenu,___powerupChoices);return false;}
}
[HarmonyPatch(typeof(PowerupMenuState),"PlayLevelUpAnimationCR")]
class AndroidFiniteLevelMenu
{
 static bool Prefix(PowerupMenuState __instance,GameController ___owner,ref IEnumerator __result)
 {__result=Play(__instance,___owner);return false;}
 static IEnumerator Play(PowerupMenuState state,GameController game)
 {
  game.screenFlash.Flash(1);game.levelupAnimator.gameObject.SetActive(true);game.levelupAnimator.SetTrigger("Start");
  LeanTween.scale(game.playerFogRevealer,new Vector3(2,2,1),.7f).setEase(LeanTweenType.easeOutBack).setIgnoreTimeScale(true);
  yield return new WaitForSecondsRealtime(.6f);
  game.screenFlash.Flash(1);game.powerupMenuSFX.Play();yield return new WaitForSecondsRealtime(.1f);
  AccessTools.Method(typeof(PowerupMenuState),"GeneratePowerups").Invoke(state,null);
  if(((List<Powerup>)AccessTools.Field(typeof(PowerupMenuState),"powerupChoices").GetValue(state)).Count==0)
  {yield return AndroidOfferMenus.End(state);yield break;}
  game.powerupMenuPanel.Show();game.powerupMenu.ConfirmEvent+=AndroidOfferMenus.Handler<EventHandler<Powerup>>(state,"OnConfirm");
  game.powerupRerollButton.onClick.AddListener(AndroidOfferMenus.Handler<UnityAction>(state,"OnReroll"));
  if(PowerupGenerator.CanReroll)game.powerupRerollButton.gameObject.SetActive(true);
 }
}
[HarmonyPatch(typeof(PowerupMenuState),"OnReroll")]
class AndroidFiniteReroll
{
 static bool Prefix(PowerupMenuState __instance,GameController ___owner)
 {
  AccessTools.Method(typeof(PowerupMenuState),"GeneratePowerups").Invoke(__instance,null);___owner.powerupRerollButton.gameObject.SetActive(false);
  if(((List<Powerup>)AccessTools.Field(typeof(PowerupMenuState),"powerupChoices").GetValue(__instance)).Count==0)__instance.StartCoroutine(AndroidOfferMenus.End(__instance));
  else ___owner.powerupMenuPanel.SelectDefault();return false;
 }
}
[HarmonyPatch(typeof(ChestState),nameof(ChestState.Enter))]
class AndroidFiniteChestMenu
{
 static bool Prefix(ChestState __instance,GameController ___owner,ref Powerup ____currPowerup)
 {
  var choices=___owner.powerupGenerator.GetRandomCharacterProfile();
  if(choices.Count==0){____currPowerup=null;AndroidOfferMenus.Resume(___owner);return false;}
  ____currPowerup=choices[0];var menu=___owner.chestUIController;menu.SetToPowerup(____currPowerup);menu.Show();
  menu.TakeClickEvent+=AndroidOfferMenus.Handler<EventHandler>(__instance,"OnTakeClick");menu.LeaveClickEvent+=AndroidOfferMenus.Handler<EventHandler>(__instance,"OnLeaveClick");
  ___owner.pauseController.Pause();AudioManager.Instance.SetLowPassFilter(true);return false;
 }
}
[HarmonyPatch(typeof(DevilDealState),nameof(DevilDealState.Enter))]
class AndroidFiniteDevilMenu
{
 static bool Prefix(DevilDealState __instance,GameController ___owner,ref List<Powerup> ___powerupChoices)
 {
  ___owner.pauseController.Pause();___powerupChoices=___owner.powerupGenerator.GetRandomDevilProfile(3);AndroidOfferMenus.Bind(___owner.devilDealMenu,___powerupChoices);
  if(___powerupChoices.Count==0){AndroidOfferMenus.Resume(___owner);return false;}
  ___owner.devilDealMenuPanel.Show();___owner.devilDealMenu.ConfirmEvent+=AndroidOfferMenus.Handler<EventHandler<Powerup>>(__instance,"OnConfirm");
  ___owner.devilDealLeaveButton.onClick.AddListener(AndroidOfferMenus.Handler<UnityAction>(__instance,"OnLeave"));AudioManager.Instance.SetLowPassFilter(true);return false;
 }
}
