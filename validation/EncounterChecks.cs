using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using flanne.PerkSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
using TwentyMinutes.Modernization;
public partial class RuntimeChecks
{
 Progression.Gate Gate(string name)=>Progression.Gates.Single(g=>g.Data.name==name);
 void Earned(string name,bool expected=true)=>Require(Progression.Allowed(Gate(name).Data)==expected,"encounter eligibility "+name+" = "+expected);
 IEnumerator Mini(GameController game)
 {
  var source=SelectedMap.MapData.spawnSessions.First(s=>s.isElite);
  var spawner=Object.FindObjectOfType<HordeSpawner>();
  // Register the actual native elite pool without introducing a repeating
  // fixture spawner. Spawn once through the native spawning implementation.
  spawner.LoadSpawners(new List<SpawnSession>{new SpawnSession{monsterPrefab=source.monsterPrefab,isElite=true,maximum=0,numPerSpawn=1,spawnCooldown=1000,startTime=0,duration=0,HP=10}});
  AccessTools.Method(typeof(HordeSpawner),"Spawn").Invoke(spawner,new object[]{source.monsterPrefab.name,10,true});
  Health target;while((target=Object.FindObjectsOfType<Health>().FirstOrDefault(h=>!h.isDead&&h.gameObject.name.Replace("(Clone)","").Trim()==source.monsterPrefab.name))==null)yield return null;
  yield return null;target.HPChange(-100000);yield return null;
  Require(target.isDead,"native elite spawn and Health death: "+source.monsterPrefab.name);
  target.gameObject.SetActive(false);
 }
 IEnumerator Picks(GameController game,params string[] names)
 {
  foreach(string name in names)yield return AchievementPick(game,name,game.playerXP.level);
 }
 void Attribute(StatType type,float value)
 {
  var p=PlayerController.Instance;
  float current=type==StatType.MoveSpeed?p.finalMoveSpeed:type==StatType.BulletDamage?p.gun.damage:1/p.gun.shotCooldown;
  p.stats[type].AddMultiplierReduction(value/current);
  float actual=type==StatType.MoveSpeed?p.finalMoveSpeed:type==StatType.BulletDamage?p.gun.damage:1/p.gun.shotCooldown;
  Require(Mathf.Abs(actual-value)<.0001f,"native effective attribute precondition: "+type+" = "+actual);
 }
 IEnumerator CheckEncounterAchievements(TitleScreenController title,string mode)
 {
  Require(Progression.State.version==2&&Progression.Gates.Count==29,"version two encounter rules register all twenty-nine gates");
  if(mode=="achievements-v2-migrate")
  {
   Require(Progression.State.earned.Count==28&&Progression.State.attempts.Count==0,"v1 ownership preserved without invented encounter attempts");
   Earned("Revamped_Dimond_Portrait");Earned("ModernizationChargeCannon_None",false);
   while(UnlockNotifications.Instance.Showing!="character:Revamped_Dimond_Portrait")yield return null;
   Require(UnlockNotifications.Instance.Label.text.Contains("previous unlock requirements"),"old queued award does not claim revised conditions were fulfilled");
   yield return Capture("achievement-v1-migration.png");title.Save();
   SetPhase("PASS");Log("complete","V1 ownership and queued notice migrate honestly to revised encounter requirements");Application.Quit(0);yield break;
  }
  if(mode=="achievements-v2-fresh")
  {
   Require(Progression.State.earned.Count==0,"fresh encounter curve has no pre-earned gates");
   var diamond=Gate("Diamond");int points=PointsTracker.pts;
   diamond.Unlock.UnlockWithPoints();Earned("Diamond",false);Require(PointsTracker.pts==points,"currency cannot buy a gated character");
   Click(Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
   int index=Characters.Items(title.characterMenu).FindIndex(i=>i.data==diamond.Data);Entries(title.characterMenu)[index].button.Select();yield return null;
   Require(Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().Any(t=>t.gameObject.activeInHierarchy&&t.text.Contains(diamond.Goal)),"native locked description shows revised heart/miniboss condition");
   Click(Entries(title.characterMenu)[index].button);Require(title.CurrentState is CharacterSelectState,"locked character cannot confirm");
   yield return Capture("achievement-locked-character-v2.png");Click(title.loadoutBackButton);while(!Active(title.mainMenu))yield return null;
   yield return AchievementRun("Shana","RevolverData","20M_Forest");var game=Object.FindObjectOfType<GameController>();
   AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(GameTimer.SharedInstance,120f);yield return null;
   Require(!Progression.State.earned.Any(),"two minutes alone earns nothing");
   yield return Mini(game);Earned("Diamond",false);Earned("Scarlett",false);Earned("ShotgunData",false);
   AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(PlayerController.Instance.playerHealth,new object[]{1});PlayerController.Instance.playerHealth.Heal(1);
   Require(!game.hitlessDetector.hitless,"native healing does not erase prior damage");
   yield return AchievementBoss(game,0);Earned("ShotgunData");Earned("10M_Forest");Earned("CrossbowData",false);
   yield return Picks(game,"PyroMage","FireStarter");yield return Mini(game);Earned("Scarlett",false);
   while(UnlockNotifications.Instance.Showing!="weapon:ShotgunData")yield return null;
   Require(Progression.State.pending.Contains("scenario:10M_Forest"),"simultaneous scenario notice remains queued");
   Require(Time.timeScale>0&&!PauseController.isPaused&&UnlockNotifications.Instance.Root.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>()==null,"notification preserves combat and cannot intercept clicks");
   yield return Capture("achievement-encounter-earned.png");SaveSystem.Save();
   SetPhase("PASS");Log("complete","Passive survival rejected, miniboss conditions enforced, healed damage rejects hitless boss, native partial fire tree rejected; quit during notice");Application.Quit(0);yield break;
  }
  if(mode!="achievements-v2-events")throw new InvalidOperationException("Unknown encounter test "+mode);
  Require(Progression.State.earned.Count==2,"fresh-process ownership contains only actual prior boss awards");
  while(UnlockNotifications.Instance.Showing!="scenario:10M_Forest")yield return null;
  Require(Progression.State.notified.Count(x=>x=="weapon:ShotgunData")==1&&!Progression.State.pending.Any(),"interrupted award deduplicated, remaining scenario notice delivered");
  yield return AchievementRun("Shana","RevolverData","20M_Forest");var g=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;
  Require(g.hitlessDetector.hitless,"new native run resets hitless history");yield return AchievementBoss(g,0);Earned("CrossbowData");
  yield return Picks(g,"Vitality");Require(p.playerHealth.maxHP==5,"Vitality produces five native max red hearts");yield return Mini(g);Earned("Diamond",false);
  yield return Picks(g,"Giant");yield return Mini(g);Earned("Diamond");
  yield return Picks(g,"PyroMage","IntenseFlame","SoothingWarmth");yield return Mini(g);Earned("Scarlett",false);
  Require(Progression.State.attempts.Single(a=>a.id=="character:Scarlett").text.Contains("3 / 4"),"fire progress uses only current run, never previous run's Fire Starter");
  yield return Picks(g,"FireStarter");yield return Mini(g);Earned("Scarlett");
  while(UnlockNotifications.Instance.Showing!="character:Scarlett")yield return null;
  Require(UnlockNotifications.Instance.Label.text.Contains(Gate("Scarlett").Goal),"fire mastery notice includes actual completed requirement");yield return Capture("achievement-fire-mastery.png");
  yield return Picks(g,"Haste","RunAndGun","InTheWind");Attribute(StatType.MoveSpeed,3.99f);yield return Mini(g);Earned("Hina",false);Attribute(StatType.MoveSpeed,4);yield return Mini(g);Earned("Hina");
  yield return Picks(g,"ElectroMage","Energized");yield return Mini(g);Earned("Spark");
  p.playerHealth.shp=1;yield return Mini(g);Earned("Lilith",false);p.playerHealth.shp=2;yield return Mini(g);Earned("Lilith");
  Attribute(StatType.BulletDamage,34.99f);Attribute(StatType.FireRate,5.99f);yield return AchievementBoss(g,0);Earned("GrenadeLauncherData",false);Earned("DualSMGsData",false);Earned("FlameCannon");
  Attribute(StatType.BulletDamage,35);Attribute(StatType.FireRate,6);yield return AchievementBoss(g,0);Earned("GrenadeLauncherData");Earned("DualSMGsData");yield return AchievementWin(g);
  yield return AchievementRun("Hina","CrossbowData","20M_Forest",1);g=Object.FindObjectOfType<GameController>();
  yield return Picks(g,"FrostMage","Frostbite","IceShard");yield return Mini(g);Earned("Yuki",false);yield return Picks(g,"Shatter");yield return Mini(g);Earned("Yuki");
  yield return Picks(g,"GhostFriend","DragonEgg");yield return AchievementBoss(g,0);Earned("BatGunData");Earned("MagicBowData");Earned("SalvoKnivesData");
  yield return AchievementBoss(g,1);Earned("ModernizationChargeCannon_None",false);yield return AchievementWin(g);Earned("20M_Temple");
  yield return AchievementRun("Shana","ShotgunData","20M_Forest",1);g=Object.FindObjectOfType<GameController>();yield return Mini(g);Earned("Abby");yield return AchievementExit(g,false);
  yield return AchievementRun("Hina","GrenadeLauncherData","20M_Forest",2);g=Object.FindObjectOfType<GameController>();
  Attribute(StatType.MoveSpeed,4.49f);yield return Mini(g);Earned("Revamped_Haruka",false);Attribute(StatType.MoveSpeed,4.5f);yield return Mini(g);Earned("Revamped_Haruka");
  Attribute(StatType.MoveSpeed,4.99f);yield return Mini(g);Earned("Dasher",false);Attribute(StatType.MoveSpeed,5);yield return Mini(g);Earned("Dasher");
  yield return AchievementBoss(g,0);Earned("ModernizationChargeCannon_None",false);yield return AchievementBoss(g,1);Earned("ModernizationChargeCannon_None");yield return AchievementWin(g);
  yield return AchievementRun("Diamond","MagicBowData","20M_Temple",2);g=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  p.playerHealth.shp=2;yield return Mini(g);Earned("Luna");Earned("Hastur",false);Earned("Revamped_Dimond_Portrait",false);
  p.playerHealth.shp=3;yield return Mini(g);Earned("Hastur");Earned("Revamped_Dimond_Portrait");
  Attribute(StatType.BulletDamage,59.99f);yield return Mini(g);Earned("Katana",false);Attribute(StatType.BulletDamage,60);yield return Mini(g);Earned("Katana");
  yield return AchievementWin(g);Earned("20M_PumpkinPatch");Earned("Endless_Forest",false);
  yield return AchievementRun("Katana","RevolverData","20M_Temple",3);g=Object.FindObjectOfType<GameController>();yield return AchievementBoss(g,0);Earned("SwordData",false);yield return AchievementBoss(g,1);Earned("SwordData");yield return AchievementWin(g);Earned("Endless_Forest");
  yield return AchievementRun("Shana","RevolverData","20M_PumpkinPatch",2);g=Object.FindObjectOfType<GameController>();yield return Picks(g,"DarkArts");yield return Mini(g);Earned("Raven",false);yield return Picks(g,"Wither");yield return Mini(g);Earned("Raven");yield return AchievementExit(g,false);
  yield return AchievementRun("Shana","RevolverData","20M_Forest");g=Object.FindObjectOfType<GameController>();
  var quest=Object.FindObjectOfType<ForestMapRhogogSpawner>();var secret=(GameObject)AccessTools.Field(typeof(ForestMapRhogogSpawner),"rhogogPrefab").GetValue(quest);
  AccessTools.Field(typeof(ForestMapRhogogSpawner),"treesActivated").SetValue(quest,true);AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(GameTimer.SharedInstance,1199f);GameTimer.SharedInstance.Start();
  Health rhogog;while((rhogog=Object.FindObjectsOfType<Health>().FirstOrDefault(h=>!h.isDead&&h.gameObject.name.Replace("(Clone)","").Trim()==secret.name))==null)yield return null;GameTimer.SharedInstance.Stop();
  yield return null;while(rhogog.isInvincible.value)yield return null;rhogog.HPChange(-1000000);GameTimer.SharedInstance.Stop();Earned("SporeGunData");
  yield return AchievementExit(g,false);while(Progression.State.pending.Count>0||UnlockNotifications.Instance.Showing!=null)yield return null;
  Require(Progression.Gates.All(x=>Progression.Allowed(x.Data))&&Progression.State.earned.Count==29,"all twenty-nine encounter gates earned through native choices, spawns, defeats and clears");
  title=Object.FindObjectOfType<TitleScreenController>();title.Save();SetPhase("PASS");Log("complete","Revised encounter curve: native full trees, nonmerged run snapshots, attribute boundaries, scenario/Darkness/loadout and ordinal restrictions, queue persistence; explicit fixture preconditions, not full-length challenge play");Application.Quit(0);
 }
}
