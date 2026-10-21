using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.UI;
using flanne.PerkSystem;
using PlayerXP=flanne.Player.PlayerXP;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
using TwentyMinutes.Modernization;
public partial class RuntimeChecks
{
 public static int ShieldApply,ShieldStack;
 IEnumerator CheckRepeatPerk(GameController game,PlayerController player,bool baseline)
 {
  Require(SelectedMap.MapData.endless,"repeat-perk reproduction uses a real native Endless run");player.playerHealth.isInvincible.Flip();
  var generator=game.powerupGenerator;var field=AccessTools.Field(typeof(PowerupGenerator),"powerupPool");var pool=(List<PowerupPoolItem>)field.GetValue(generator);
  var shield=pool.Single(x=>x.powerup.name=="HolyShield");Require(shield.numTimeRepeatable==3,"native Endless pool permits its three Holy Shield picks");
  // Restrict which five eligible native items can be offered, keeping each
  // original native pool entry/repeat policy and the real GeneratePowerups path.
  var choices=pool.Where(x=>x.powerup.prereqs.Count==0&&x!=shield).Take(4).Concat(new[]{shield}).ToList();Require(choices.Count==5,"fixture has five distinct eligible native choices");field.SetValue(generator,choices);
  var target=shield.powerup;float originalCooldown=0;
  for(int pick=0;pick<(baseline?2:3);pick++)
  {
   SetPhase("native-repeat-choice-"+pick);var xp=game.playerXP;xp.GainXP((float)AccessTools.Property(typeof(PlayerXP),"xpToLevel").GetValue(xp)+1);
   while(!(game.CurrentState is PowerupMenuState))yield return null;
   while(AccessTools.Field(typeof(ToggleGroupMenu<Powerup>),"ConfirmEvent").GetValue(game.powerupMenu)==null)yield return null;
   var toggles=(Toggle[])AccessTools.Field(typeof(ToggleGroupMenu<Powerup>),"toggles").GetValue(game.powerupMenu);
   Log("native-repeat-offers",string.Join(";",toggles.Select(t=>t.name+":"+t.GetComponent<DataUIBinding<Powerup>>().data?.name+":"+t.GetComponent<DataUIBinding<Powerup>>().data?.GetInstanceID()+":"+t.gameObject.activeInHierarchy)));
   // The same native menu also stores four tree-preview toggles after its
   // generated choice slots. Only the GeneratePowerups-assigned slots qualify.
   var choice=toggles.Take(game.numPowerupChoices).Single(x=>x.GetComponent<DataUIBinding<Powerup>>().data==target);choice.isOn=true;
   Button confirm=null;
   while(confirm==null || !confirm.IsInteractable())
   {
    confirm=Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(b=>b.gameObject.activeInHierarchy&&Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentTarget(i)==game.powerupMenu&&b.onClick.GetPersistentMethodName(i)=="OnConfirmClicked"));yield return null;
   }
   yield return Capture("repeat-choice-"+pick+".png");Click(confirm);
   while(!(game.CurrentState is CombatState))yield return null;
   var shields=player.GetComponentsInChildren<PreventDamage>();
   if(pick==0){Require(shields.Length==1&&ShieldApply==1&&ShieldStack==0,"first ordinary level-up choice attaches exactly one native Holy Shield");originalCooldown=shields[0].finalCooldown;}
   else if(baseline)
   {
    Require(shields.Length==2&&ShieldApply==2&&ShieldStack==0&&Mathf.Abs(shields[0].finalCooldown-originalCooldown)<.001f,"ordinary second Holy Shield pick reproduces duplicate first-pick attachment instead of native stacked cooldown improvement");
    Log("reproduced-anomaly","A01: actual Endless native level-up choices invoke Apply twice, create two shields, and leave the original cooldown unchanged; not acceptance of corrected behavior");
   }
   else Require(shields.Length==1&&ShieldApply==1&&ShieldStack==pick&&Mathf.Abs(shields[0].finalCooldown-originalCooldown/(pick+1))<.001f,"ordinary Holy Shield repeat "+pick+" uses a distinct native ApplyStack, retains one shield and increases cooldown recovery rate");
  }
  yield return Capture("repeat-perk-result.png");
 }
}
[HarmonyPatch(typeof(Powerup),nameof(Powerup.Apply))]
class ShieldApplyProbe{static void Prefix(Powerup __instance){if(__instance.name=="HolyShield")RuntimeChecks.ShieldApply++;}}
[HarmonyPatch(typeof(Powerup),nameof(Powerup.ApplyStack))]
class ShieldStackProbe{static void Prefix(Powerup __instance){if(__instance.name=="HolyShield")RuntimeChecks.ShieldStack++;}}
