using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 static BuffPlayerStats CharacterBuff(string root)=>PlayerController.Instance.GetComponentsInChildren<BuffPlayerStats>(true).Single(b=>b.name.Replace("(Clone)","")==root);
 static void CharacterHurt(int amount)
 {
  var health=PlayerController.Instance.playerHealth;if(health.isInvincible.value)health.isInvincible.UnFlip();
  AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(health,new object[]{amount});
 }
 IEnumerator CharacterBuffRestart(GameController game,string capture)
 {
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;
  Require(p.GetComponentsInChildren<BuffPlayerStats>(true).Length==0,"ordinary restart clears all acquired source character-buff owners");
  foreach(var type in new[]{StatType.BulletDamage,StatType.FireRate,StatType.ReloadRate,StatType.ProjectileSize,StatType.MoveSpeed,StatType.Knockback})CombatClose(Bonus(p.stats[type]),0,"ordinary restart clears source "+type+" contributions");
  Require(p.stats[StatType.Piercing].FlatBonus==0,"ordinary restart clears source counted piercing");yield return CheckRevolver(game,p,capture);yield return AchievementExit(game,false);
 }
 IEnumerator CheckAndroidCharacterBuffs(bool continuation)
 {
  string mode=Environment.GetEnvironmentVariable("MODERNIZATION_TEST");string character=mode.Contains("-diamond-")?"Diamond":mode.Contains("-necrophile-")?"Lilith":"Scarlett";
  GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.State.earned.Add("character:"+character);Progression.Refresh();
   yield return AchievementRun(character,"RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
   string[] keys=character=="Diamond"?new[]{"diamond_perk_strong_will_name","diamond_perk_berserk_name","diamond_perk_bulk_up_name"}:new[]{character=="Lilith"?"lilith_perk_necrophile_name":"scarlett_perk_pyromaniac_name"};
   foreach(string key in keys)yield return AndroidWhirlwindChest(game,key);
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));yield return null;yield return null;
  Require(p.loadedCharacter.name==character,"actual native character matches its source exclusive-buff workflow: "+character);
  Log("fixture-precondition","Actual native character/chest acquisitions and health/death/burn/reload producers. Encounter/game clock stopped, no synthetic buff applications or imposed counters. Current source Continue and ordinary restart use the real native menus; no legacy/package/phone claim.");
  if(character=="Diamond"){yield return CheckDiamondBuffs(game,continuation);yield break;}
  if(character=="Lilith"){yield return CheckNecrophileBuff(game,continuation);yield break;}
  yield return CheckPyromaniacBuff(game,continuation);
 }
 static void DiamondStats(int hurt,int heal,int pierce,float knockback)
 {
  var p=PlayerController.Instance;
  CombatClose(Bonus(p.stats[StatType.FireRate]),hurt*.1f,"actual counted Berserk Fire Rate");CombatClose(Bonus(p.stats[StatType.ReloadRate]),hurt*.1f,"actual counted Berserk reload rate");
  CombatClose(Bonus(p.stats[StatType.BulletDamage]),heal*.15f,"actual counted Bulk Up damage");CombatClose(Bonus(p.stats[StatType.ProjectileSize]),heal*.15f,"actual counted Bulk Up bullet size");
  Require(p.stats[StatType.Piercing].FlatBonus==pierce,"actual source Strong Will piercing: "+pierce);CombatClose(Bonus(p.stats[StatType.Knockback]),knockback,"actual source Strong Will knockback");
 }
 IEnumerator CheckDiamondBuffs(GameController game,bool continuation)
 {
  var p=PlayerController.Instance;var will=(NonStackingBuff)CharacterBuff("PF_BuffOnBelowHalfHP");var hurt=CharacterBuff("PF_BuffOnHurt");var heal=CharacterBuff("PF_BuffOnHeal");
  Require(p.playerHealth.maxHP==7&&p.playerHealth.hp==7,"actual Diamond starts/continues at seven maximum/current hearts");
  Require(!(bool)WindField(will,"_isActive"),"above-half Strong Will has no active flag, including source-unsaved flag after Continue");
  if(continuation)
  {
   Require(CombatCount(will)==2&&CombatCount(hurt)==8&&CombatCount(heal)==3,"source Continue restores independent Strong Will2/Berserk8/Bulk Up3 counts");DiamondStats(8,3,4,.5f);
   CharacterHurt(4);Require(p.playerHealth.hp==3&&(bool)WindField(will,"_isActive")&&CombatCount(will)==3&&CombatCount(hurt)==12,"actual continued four red hits activate Strong Will once and add four Berserk applications");DiamondStats(12,3,6,.75f);
   p.playerHealth.Heal(0);Require(CombatCount(will)==3&&CombatCount(heal)==3,"zero healing neither duplicates active Strong Will nor raises Bulk Up");
   p.playerHealth.Heal(4);Require(p.playerHealth.hp==7&&!(bool)WindField(will,"_isActive")&&CombatCount(will)==3&&CombatCount(heal)==4,"actual continued healing clears only the new Strong Will phase and grants Bulk Up once");DiamondStats(12,4,4,.5f);
   RunResume.Save();File.Copy(RunCheckpoint,Path.Combine(ResultDir,"resaved-character-run.json"));
   Require(CombatValues("diamond_perk_strong_will_name").Single().count==3&&CombatValues("diamond_perk_berserk_name").Single().count==12&&CombatValues("diamond_perk_bulk_up_name").Single().count==4,"next actual source capture keeps each updated count without normalizing Strong Will's expired applications");
   yield return CharacterBuffRestart(game,"android-diamond-buff-original-restart.png");SetPhase("PASS");Log("complete","Source Diamond Strong Will/Berserk/Bulk Up count replay, unsaved active flag, actual threshold/hurt/heal/next save and ordinary restart");Application.Quit(0);yield break;
  }
  Require(CombatCount(will)==0&&CombatCount(hurt)==0&&CombatCount(heal)==0,"fresh Diamond exclusive helpers start with zero counted applications");DiamondStats(0,0,0,0);
  for(int cycle=1;cycle<=2;cycle++)
  {
   CharacterHurt(4);Require(p.playerHealth.hp==3&&CombatCount(will)==cycle&&(bool)WindField(will,"_isActive")&&CombatCount(hurt)==cycle*4,"four actual red-health hits activate Strong Will once and grant four Berserk applications in cycle"+cycle);
   int priorHeals=cycle==1?0:2;DiamondStats(cycle*4,priorHeals,2,.25f);
   p.playerHealth.Heal(0);Require(CombatCount(will)==cycle&&CombatCount(heal)==priorHeals,"zero HPChange emits no heal buff and does not restack active Strong Will");
   p.playerHealth.Heal(4);Require(p.playerHealth.hp==7&&!(bool)WindField(will,"_isActive")&&CombatCount(will)==cycle,"above-half actual healing removes one Strong Will contribution without decrementing its source count");DiamondStats(cycle*4,cycle==1?1:3,0,0);
   if(cycle==1)
   {p.playerHealth.Heal(1);Require(p.playerHealth.hp==7&&CombatCount(heal)==2,"source positive healing at full HP still grants Bulk Up through its actual heal event");DiamondStats(4,2,0,0);}
  }
  var captured=ResumeRuntime.Capture(p);Require(captured.stats.Count==0&&captured.timed.Count==0&&captured.providers.Count==0&&CombatValues("diamond_perk_strong_will_name").Single().count==2&&CombatValues("diamond_perk_berserk_name").Single().count==8&&CombatValues("diamond_perk_bulk_up_name").Single().count==3,"source checkpoint captures all three independent positive counts, omitting Strong Will's inactive flag and raw stats");
  yield return DragonSuspend(game,"Native Diamond exclusive chest acquisitions, red-hurt/heal/full-HP/zero-heal/threshold cycles and three independent source count checkpoint");
 }
 void NecrophileKill(DamageType kind)
 {var health=HitEffectTarget(maximum:1,tag:"Untagged");health.TakeDamage(kind,100);Require(health.isDead,"actual pooled untagged Health dies through "+kind);}
 IEnumerator CheckNecrophileBuff(GameController game,bool continuation)
 {
  var p=PlayerController.Instance;var buff=(TemporaryRefreshingBuff)CharacterBuff("PF_BuffOnDeath");Require((int)WindField(buff,"duration")==1,"actual Necrophile helper uses source one-second duration");
  float baseline=continuation?.7f:0;Require(CombatCount(buff)==(continuation?2:0)&&WindField(buff,"buffCoroutine")==null,"source Necrophile starts without a restored old coroutine and with the expected application count");CombatClose(Bonus(p.stats[StatType.MoveSpeed]),baseline,"Necrophile source replay keeps counted contributions from expired phases");
  if(continuation)
  {float began=Time.time;while(Time.time-began<1.1f)yield return null;CombatClose(Bonus(p.stats[StatType.MoveSpeed]),.7f,"count-replayed Necrophile contributions have no invented expiry");}
  int cycles=continuation?1:2;
  for(int cycle=1;cycle<=cycles;cycle++)
  {
   NecrophileKill(DamageType.Bullet);float first=Time.time;int expected=continuation?3:cycle;Require(CombatCount(buff)==expected,"real untagged death starts one new Necrophile application");CombatClose(Bonus(p.stats[StatType.MoveSpeed]),baseline+.35f,"actual death applies source35-percent movement above counted replay");
   while(Time.time-first<.6f)yield return null;NecrophileKill(DamageType.Summon);Require(CombatCount(buff)==expected,"another untagged Summon death refreshes the existing phase without another counted application");
   while(Time.time-first<1.1f)yield return null;CombatClose(Bonus(p.stats[StatType.MoveSpeed]),baseline+.35f,"Necrophile refreshed phase survives its old deadline");
   while(WindField(buff,"buffCoroutine")!=null)yield return null;Require(CombatCount(buff)==expected,"source Necrophile expiry does not decrement its application count");CombatClose(Bonus(p.stats[StatType.MoveSpeed]),baseline,"Necrophile expiry removes one active contribution only");
  }
  if(!continuation)
  {Require(CombatValues("lilith_perk_necrophile_name").Single().count==2,"source checkpoint includes positive inherited Necrophile count2 after expiry");yield return DragonSuspend(game,"Native Lilith exclusive, untagged Bullet/Summon death refresh/expiry, inherited count2 source checkpoint");yield break;}
  Require(CombatValues("lilith_perk_necrophile_name").Single().count==3,"source continued capture retains count3 despite expired coroutine");yield return CharacterBuffRestart(game,"android-necrophile-original-restart.png");SetPhase("PASS");Log("complete","Native Necrophile untagged death/refresh/expiry, inherited count replay and resumed producers/ordinary restart");Application.Quit(0);
 }
 IEnumerator CheckPyromaniacBuff(GameController game,bool continuation)
 {
  var p=PlayerController.Instance;var buff=CharacterBuff("BuffOnBurn");Require(CombatCount(buff)==(continuation?2:0),"source Pyromaniac starts with its fresh or restored counted applications");CombatClose(Bonus(p.stats[StatType.FireRate]),continuation?.2f:0,"source Pyromaniac count replay restores exact Fire Rate");
  var target=HitEffectTarget(maximum:100000,tag:"Untagged");
  for(int i=0;i<3;i++){BurnSystem.SharedInstance.Burn(target.gameObject,1);Require(CombatCount(buff)==i+1+(continuation?2:0),"each actual burn/repeated-burn notification grants exactly one Pyromaniac count");CombatClose(Bonus(p.stats[StatType.FireRate]),(i+1+(continuation?2:0))*.1f,"actual Pyromaniac ten-percent per-burn contribution");}
  p.ammo.Reload();Require(CombatCount(buff)==0,"actual native reload removes and clears all Pyromaniac counts");CombatClose(Bonus(p.stats[StatType.FireRate]),0,"source RemoveStacks reverses every restored and new burn contribution exactly");
  if(!continuation)
  {
   BurnSystem.SharedInstance.Burn(target.gameObject,1);BurnSystem.SharedInstance.Burn(target.gameObject,1);Require(CombatValues("scarlett_perk_pyromaniac_name").Single().count==2,"source checkpoint retains two applications after native reload/reset and new burn events");
   var captured=ResumeRuntime.Capture(p);Require(captured.stats.Count==0&&captured.timed.Count==0&&captured.providers.Count==0,"source Pyromaniac uses its count rather than a raw Fire Rate or generic phase snapshot");yield return DragonSuspend(game,"Native Scarlett exclusive, repeated original burn events, exact reload removal and positive Pyromaniac source checkpoint");yield break;
  }
  ReturnFixtureActor(target.gameObject);yield return CharacterBuffRestart(game,"android-pyromaniac-original-restart.png");SetPhase("PASS");Log("complete","Source Pyromaniac burn/repeated-burn counts, exact restored/new removal through actual reload, current Continue and ordinary restart");Application.Quit(0);
 }
}
