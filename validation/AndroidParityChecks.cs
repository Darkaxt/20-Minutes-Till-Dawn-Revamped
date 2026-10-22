using System;
using System.Linq;
using System.Collections;
using UnityEngine;
using HarmonyLib;
using flanne;
using flanne.Core;
using flanne.PerkSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckAndroidRefresh()
 {
  RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.State.earned.Add("character:Abby");Progression.Refresh();
  yield return AchievementRun("Shana","RevolverData","Endless_Forest");
  var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;
  yield return AndroidPick(game,"QuickHands",game.playerXP.level);yield return AndroidPick(game,"FreshClip",game.playerXP.level);yield return null;
  float damage=Bonus(p.stats[StatType.BulletDamage]);p.ammo.Reload();
  Require(Mathf.Abs(Bonus(p.stats[StatType.BulletDamage])-damage-.3f)<.001f,"Android Fresh Clip adds thirty percent on native reload");
  float first=Time.time;while(Time.time-first<.6f)yield return null;p.ammo.Reload();float refreshed=Time.time;
  Require(Mathf.Abs(Bonus(p.stats[StatType.BulletDamage])-damage-.3f)<.001f,"Fresh Clip refreshes without overlapping bonuses");
  while(Time.time-first<1.1f)yield return null;
  Require(Mathf.Abs(Bonus(p.stats[StatType.BulletDamage])-damage-.3f)<.001f,"Fresh Clip survives its original expiration after a reload refresh");
  while(Time.time-refreshed<1.1f)yield return null;
  Require(Mathf.Abs(Bonus(p.stats[StatType.BulletDamage])-damage)<.001f,"Fresh Clip expires once after the last reload");
  yield return AndroidPick(game,"Vitality",game.playerXP.level);yield return AndroidPick(game,"AngerPoint",game.playerXP.level);yield return null;
  damage=Bonus(p.stats[StatType.BulletDamage]);float rate=Bonus(p.stats[StatType.FireRate]),reload=Bonus(p.stats[StatType.ReloadRate]);
  AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(p.playerHealth,new object[]{1});
  AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(p.playerHealth,new object[]{1});
  Require(Mathf.Abs(Bonus(p.stats[StatType.BulletDamage])-damage-.7f)<.001f&&Mathf.Abs(Bonus(p.stats[StatType.FireRate])-rate-.7f)<.001f&&Bonus(p.stats[StatType.ReloadRate])==reload,"Android Anger Point repeated native hurt refreshes one damage/fire-rate buff");
  Time.timeScale=10;while(Bonus(p.stats[StatType.BulletDamage])>damage+.001f)yield return null;Time.timeScale=1;
  Require(Mathf.Abs(Bonus(p.stats[StatType.FireRate])-rate)<.001f,"Anger Point removes its bonus once at thirty seconds");
  yield return AchievementExit(game,false);
  yield return AchievementRun("Abby","RevolverData","Endless_Forest");game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;
  var field=AccessTools.Field(typeof(PowerupGenerator),"characterPool");var pool=(System.Collections.Generic.List<PowerupPoolItem>)field.GetValue(game.powerupGenerator);
  var whirlwind=pool.Single(x=>PerkKey(x.powerup).key=="abby_perk_whirlwind_name");field.SetValue(game.powerupGenerator,new System.Collections.Generic.List<PowerupPoolItem>{whirlwind});
  this.PostNotification(flanne.Pickups.ChestPickup.ChestPickupEvent);while(!(game.CurrentState is ChestState))yield return null;
  var chest=(flanne.UI.ChestUIController)AccessTools.Property(typeof(GameState),"chestUIController").GetValue(game.CurrentState);var take=(UnityEngine.UI.Button)AccessTools.Field(typeof(flanne.UI.ChestUIController),"takeButton").GetValue(chest);
  while(!take.IsInteractable())yield return null;Click(take);while(!(game.CurrentState is CombatState))yield return null;yield return null;
  rate=Bonus(p.stats[StatType.FireRate]);float move=Bonus(p.stats[StatType.MoveSpeed]);p.ammo.Reload();p.ammo.Reload();
  Require(Mathf.Abs(Bonus(p.stats[StatType.FireRate])-rate-.35f)<.001f&&Mathf.Abs(Bonus(p.stats[StatType.MoveSpeed])-move-.35f)<.001f,"Android Abby Whirlwind native exclusive reloads refresh one two-second buff");
  first=Time.time;while(Time.time-first<2.1f)yield return null;
  Require(Mathf.Abs(Bonus(p.stats[StatType.FireRate])-rate)<.001f&&Mathf.Abs(Bonus(p.stats[StatType.MoveSpeed])-move)<.001f,"Whirlwind expires to the prior attributes");
  yield return Capture("android-refresh-native.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Android Fresh Clip/Anger Point/Abby Whirlwind strength, refresh, expiry and native scene lifecycle");Application.Quit(0);
 }
 IEnumerator CheckAndroidShield()
 {
  RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
  yield return AchievementRun("Shana","RevolverData","Endless_Forest");
  var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;
  yield return AndroidPick(game,"HolyShield",game.playerXP.level);
  yield return AndroidPick(game,"DivineBlessing",game.playerXP.level);
  var shield=p.GetComponentInChildren<PreventDamage>();while(shield.cooldownRate==null)yield return null;
  Require(shield.finalCooldown==120,"source shield starts with two-minute cooldown");
  foreach(float expected in new[]{60f,10f,10f})
  {
   yield return AndroidPick(game,"StalwartShield",game.playerXP.level);
   Require(Mathf.Abs(shield.finalCooldown-expected)<.001f,"Android flat shield reduction and ten-second minimum: "+expected);
  }
  p.playerHealth.isInvincible.UnFlip();int health=p.playerHealth.hp;
  var enemy=new GameObject("ShieldCollisionFixture");enemy.tag="Enemy";
  AccessTools.Method(typeof(PlayerHealth),"CheckCollision").Invoke(p.playerHealth,new object[]{enemy});Object.Destroy(enemy);
  while(shield.isActive)yield return null;
  Require(p.playerHealth.hp==health,"native shield actually prevents the incoming damage");
  GameTimer.SharedInstance.Start();Time.timeScale=10;
  while((float)AccessTools.Field(typeof(PreventDamage),"_timer").GetValue(shield)<9.5f)yield return null;
  shield.ReduceCDTimer(1);
  Require(Mathf.Abs((float)AccessTools.Field(typeof(PreventDamage),"_timer").GetValue(shield)-9)<.1f,"Android overshooting a recovering shield defers completion by one game second");
  while(!shield.isActive)yield return null;Time.timeScale=1;
  yield return Capture("android-native-shield-recovered.png");yield return AchievementExit(game,false);
  yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();
  Require(PlayerController.Instance.GetComponentsInChildren<PreventDamage>().Length==0,"shield owners do not carry across native restart");
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Android Stalwart flat cooldown/cap, native protection/recovery, completion clamp and scene restart");Application.Quit(0);
 }
 IEnumerator AndroidPick(GameController game,string name,int level)
 {
  var pool=FallbackPool(game.powerupGenerator);var target=pool.Single(x=>x.powerup.name==name).powerup;
  var equipped=(System.Collections.Generic.List<Powerup>)AccessTools.Field(typeof(PlayerPerks),"_equippedPerks").GetValue(PlayerController.Instance.playerPerks);
  foreach(var prerequisite in target.prereqs)
  {
   if((bool)AccessTools.Method(typeof(PowerupGenerator),"PrereqsMet").Invoke(game.powerupGenerator,new object[]{target}))break;
   if(!equipped.Any(p=>PerkKey(p).key==PerkKey(prerequisite).key))yield return AndroidPick(game,prerequisite.name,game.playerXP.level);
  }
  yield return AchievementPick(game,name,game.playerXP.level);
 }
 IEnumerator CheckAndroidNumeric()
 {
  RunResume.Invalidate();
  Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
  Log("fixture-precondition","Earned Endless access seeded in the isolated profile for finite repeated-pool checks; no production gate bypass");
  yield return AchievementRun("Shana","RevolverData","Endless_Forest");
  var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;
  Object.FindObjectOfType<HordeSpawner>().enabled=false;
  yield return AndroidPick(game,"Frenzy",game.playerXP.level);
  yield return AndroidPick(game,"Hellspawns",game.playerXP.level);
  float before=Bonus(p.stats[StatType.SummonAttackSpeed]);
  yield return AndroidPick(game,"Culling",game.playerXP.level);
  Require(Mathf.Abs(Bonus(p.stats[StatType.SummonAttackSpeed])-before-.3f)<.0001f,"Android Culling adds thirty percent through native level-up choice");
  yield return AndroidPick(game,"QuickHands",game.playerXP.level);
  yield return AndroidPick(game,"FreshClip",game.playerXP.level);
  int ammo=p.stats[StatType.MaxAmmo].FlatBonus;float damage=Bonus(p.stats[StatType.BulletDamage]),reload=Bonus(p.stats[StatType.ReloadRate]);
  yield return AndroidPick(game,"MiniClip",game.playerXP.level);
  Require(p.stats[StatType.MaxAmmo].FlatBonus==ammo-4&&Mathf.Abs(Bonus(p.stats[StatType.BulletDamage])-damage-.5f)<.0001f&&Mathf.Abs(Bonus(p.stats[StatType.ReloadRate])-reload-.75f)<.0001f,"Android Mini Clip -4 ammo and retained damage/reload bonuses");
  yield return AndroidPick(game,"Vitality",game.playerXP.level);
  int hp=p.playerHealth.maxHP;float size=Bonus(p.stats[StatType.CharacterSize]);
  yield return AndroidPick(game,"Giant",game.playerXP.level);
  Require(p.playerHealth.maxHP==hp+2&&Mathf.Abs(Bonus(p.stats[StatType.CharacterSize])-size-.5f)<.0001f,"Android Giant native two hearts and fifty percent size");
  foreach(var recipe in new[]{new{name="TomeOfPower",stat=StatType.FireRate,factor=.95f},new{name="TomeOfElasticity",stat=StatType.BulletDamage,factor=.95f},new{name="TomeOfRage",stat=StatType.BulletDamage,factor=.9f},new{name="TomeOfSummoning",stat=StatType.ReloadRate,factor=.9f}})
  {
   float reduction=Reduction(p.stats[recipe.stat]);int previousHP=p.playerHealth.maxHP;
   float knockback=Reduction(p.stats[StatType.Knockback]);
   yield return NativeTomePick(game,false,recipe.name);
   Require(Mathf.Abs(Reduction(p.stats[recipe.stat])-reduction*recipe.factor)<.0001f,"Android tome penalty: "+recipe.name);
   if(recipe.name=="TomeOfElasticity")Require(p.playerHealth.maxHP==previousHP-1,"Android Elasticity adds its missing one-heart penalty");
   if(recipe.name=="TomeOfRage")Require(Mathf.Abs(Reduction(p.stats[StatType.Knockback])-knockback*.8f)<.0001f,"Android Rage knockback penalty is twenty percent");
  }
  float moving=Reduction(p.stats[StatType.MoveSpeed]),walking=Reduction(p.stats[StatType.WalkSpeed]);
  yield return NativeTomePick(game,false,"TomeOfStillness");
  Require(Reduction(p.stats[StatType.MoveSpeed])==0&&Reduction(p.stats[StatType.WalkSpeed])==walking,"Android Stillness affects MoveSpeed rather than only shooting WalkSpeed");
  yield return Capture("android-numeric-native-build.png");yield return AchievementExit(game,false);
  yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  Require(p.playerHealth.maxHP==4&&p.gun.gunData.damage==20&&p.gun.gunData.maxAmmo==6&&p.stats[StatType.MaxAmmo].FlatBonus==0&&Bonus(p.stats[StatType.SummonAttackSpeed])==0,"native restart retains original Revolver and no carried build modifiers");
  yield return CheckRevolver(game,p,"android-numeric-revolver-restart.png");yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete","Android Culling, Mini Clip, Giant and five tome recipes through native menus, stats and original restart");Application.Quit(0);
 }
}
