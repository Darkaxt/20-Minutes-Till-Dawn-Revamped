using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.UI;
using flanne.TitleScreen;
using flanne.PowerupSystems;
using flanne.PowerupSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator AndroidWhirlwindChest(GameController game)
 {
  var field=AccessTools.Field(typeof(PowerupGenerator),"characterPool");var pool=(List<PowerupPoolItem>)field.GetValue(game.powerupGenerator);
  var wanted=pool.Single(x=>PerkKey(x.powerup).key=="abby_perk_whirlwind_name");field.SetValue(game.powerupGenerator,new List<PowerupPoolItem>{wanted});
  this.PostNotification(flanne.Pickups.ChestPickup.ChestPickupEvent);while(!(game.CurrentState is ChestState))yield return null;
  var chest=(ChestUIController)AccessTools.Property(typeof(GameState),"chestUIController").GetValue(game.CurrentState);var take=(Button)AccessTools.Field(typeof(ChestUIController),"takeButton").GetValue(chest);
  while(!take.IsInteractable())yield return null;Click(take);while(!(game.CurrentState is CombatState))yield return null;
  field.SetValue(game.powerupGenerator,pool.Where(x=>x.numTimeRepeatable!=0).ToList());yield return null;
 }
 IEnumerator CheckAndroidResume(bool continuation)
 {
  GameController game;PlayerController player;
  if(!continuation)
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.State.earned.Add("character:Abby");Progression.Refresh();
   yield return AchievementRun("Abby","RevolverData","Endless_Forest");game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;
   foreach(string name in new[]{"QuickHands","FreshClip","Vitality","AngerPoint","HolyShield","DivineWrath","StalwartShield","StalwartShield"})yield return AndroidPick(game,name,game.playerXP.level);
   yield return AndroidWhirlwindChest(game);
   player.playerHealth.isInvincible.UnFlip();var enemy=new GameObject("ShieldResumeFixture");enemy.tag="Enemy";AccessTools.Method(typeof(PlayerHealth),"CheckCollision").Invoke(player.playerHealth,new object[]{enemy});Object.Destroy(enemy);
   while(player.GetComponentInChildren<PreventDamage>().isActive)yield return null;
   player.ammo.Reload();AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(player.playerHealth,new object[]{1});
   PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();
   Require(Mathf.Abs(Bonus(player.stats[StatType.BulletDamage])-1)<.001f&&Mathf.Abs(Bonus(player.stats[StatType.FireRate])-1.1f)<.001f,"all three Android refresh helpers are active at the native save boundary");
   var save=Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy);Click(save);
   while(Object.FindObjectOfType<TitleScreenController>()==null||!Active(Object.FindObjectOfType<TitleScreenController>().mainMenu))yield return null;
   var snapshot=RunResume.Read();Require(snapshot.runtime.timed.Count(x=>x.type==typeof(TemporaryRefreshingBuff).FullName)==1,"native checkpoint owns the active refreshing Whirlwind timer");
   File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Native Android refresh/shield build acquisition, active-effect checkpoint and Save & return");Application.Quit(0);yield break;
  }
  var title=Object.FindObjectOfType<TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;
  Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
  while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))
  {
   var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));
   if(error!=null)throw new InvalidOperationException(error.text);yield return null;
  }
  player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;GameTimer.SharedInstance.Stop();
  Require(player.GetComponentsInChildren<DamageUpOnReload>().Length==1&&player.GetComponentsInChildren<FireRateOnHurt>().Length==1&&player.GetComponentsInChildren<TemporaryRefreshingBuff>().Length==1,"fresh-process Continue reconstructs exactly one owner for each Android buff");
  Require(Mathf.Abs(Bonus(player.stats[StatType.BulletDamage])-1)<.001f&&Mathf.Abs(Bonus(player.stats[StatType.FireRate])-1.1f)<.001f&&Mathf.Abs(Bonus(player.stats[StatType.MoveSpeed])-.35f)<.001f,"Continue restores exact active Android buff contributions without duplication");
  var shield=player.GetComponentInChildren<PreventDamage>();Require(shield.finalCooldown==10&&!shield.isActive,"Continue preserves the reduced recovering shield");
  Time.timeScale=10;while(Bonus(player.stats[StatType.BulletDamage])>.001f)yield return null;Time.timeScale=1;
  Require(Mathf.Abs(Bonus(player.stats[StatType.FireRate])-.05f)<.001f&&Mathf.Abs(Bonus(player.stats[StatType.MoveSpeed]))<.001f&&shield.isActive,"restored buffs expire once and the native shield recovers");
  player.ammo.Reload();Require(Mathf.Abs(Bonus(player.stats[StatType.BulletDamage])-.3f)<.001f&&Mathf.Abs(Bonus(player.stats[StatType.FireRate])-.4f)<.001f,"restored reload subscriptions reactivate Fresh Clip and Whirlwind");
  yield return Capture("android-buffs-after-continue.png");yield return AchievementExit(game,false);
  yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;
  Require(player.GetComponentsInChildren<DamageUpOnReload>().Length==0&&player.GetComponentsInChildren<FireRateOnHurt>().Length==0&&player.GetComponentsInChildren<TemporaryRefreshingBuff>().Length==0,"new normal run has no reconstructed buff owners");
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Fresh-process Android buff/shield Continue: exact owners/contributions, expiry, reactivation and original restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(flanne.PerkSystem.PerkEffect),nameof(flanne.PerkSystem.PerkEffect.Equip))]
class AndroidRestoreEffectProbe
{
 static void Prefix(flanne.PerkSystem.PerkEffect __instance)
 {
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")!="resume-android-continue")return;
  var trigger=AccessTools.Field(__instance.GetType(),"trigger").GetValue(__instance);var action=AccessTools.Field(__instance.GetType(),"action").GetValue(__instance);
  RuntimeChecks.Log("restore-effect-probe",(trigger?.GetType().FullName??"null trigger")+" / "+(action?.GetType().FullName??"null action"));
 }
}
[HarmonyPatch(typeof(flanne.PerkSystem.Actions.AttachPrefabToPlayerAction),nameof(flanne.PerkSystem.Actions.AttachPrefabToPlayerAction.Activate))]
class AndroidRestorePrefabProbe
{
 static void Prefix(flanne.PerkSystem.Actions.AttachPrefabToPlayerAction __instance)
 {
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")!="resume-android-continue")return;
  var prefab=(GameObject)AccessTools.Field(__instance.GetType(),"prefab").GetValue(__instance);RuntimeChecks.Log("restore-prefab-probe",prefab==null?"null prefab":prefab.name+" #"+prefab.GetInstanceID());
 }
}
