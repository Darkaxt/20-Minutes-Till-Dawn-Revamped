using System;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using flanne.UI;
using flanne.PerkSystem;
using PlayerXP=flanne.Player.PlayerXP;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
using TwentyMinutes.Modernization;
public partial class RuntimeChecks
{
 IEnumerator CheckAchievements(TitleScreenController title,bool relaunch)
 {
  Require(Progression.Gates.Count==29,"all native gates registered with separate ownership indices");
  var diamond=Progression.Gates.Single(g=>g.Data.name=="Diamond");
  var shotgun=Progression.Gates.Single(g=>g.Data.name=="ShotgunData");
  var crossbow=Progression.Gates.Single(g=>g.Data.name=="CrossbowData");
  var quickplay=Progression.Gates.Single(g=>g.Data.name=="10M_Forest");
  if(relaunch)
  {
   Require(Progression.Allowed(diamond.Data)&&Progression.Allowed(crossbow.Data)&&Progression.Allowed(quickplay.Data),"earned character, weapon and scenario survive a fresh process");
   Require(!Progression.Allowed(shotgun.Data),"unearned level gate remains locked after relaunch");
   while(UnlockNotifications.Instance.Showing!=quickplay.Id)yield return null;
   Require(Progression.State.notified.Count(x=>x==diamond.Id)==1&&Progression.State.notified.Count(x=>x==crossbow.Id)==1,"earlier notices deduplicated, including the notice interrupted by quit");
   Require(!Progression.State.pending.Any(),"remaining queued scenario notice delivered on relaunch");
   yield return Capture("achievement-pending-relaunch.png");
   Click(Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu))yield return null;
   int index=Characters.Items(title.characterMenu).FindIndex(x=>x.data==diamond.Data);Click(Entries(title.characterMenu)[index].button);
   while(!Active(title.gunMenu))yield return null;
   Require(Loadout.CharacterSelection==diamond.Data,"earned character selectable through the ordinary native menu after relaunch");
   SetPhase("PASS");Log("complete","Journal ownership, interrupted notice deduplication and remaining queue survive relaunch");Application.Quit(0);yield break;
  }
  Require(Progression.State.earned.Count==0&&Progression.State.pending.Count==0,"fresh profile starts with Shana, Revolver and standard Forest only");
  Require(!Progression.Allowed(Plugin.BaseGun)&&!Progression.Allowed(Characters.Data["Haruka"]),"experimental content now requires its earned gates");
  int points=PointsTracker.pts;diamond.Unlock.UnlockWithPoints();Require(!Progression.Allowed(diamond.Data)&&PointsTracker.pts==points,"native currency purchase cannot bypass an achievement gate");
  foreach(var gate in Progression.Gates.Where(g=>g.Unlock!=null))
   Require(gate.Unlock.IsLocked,"fresh native locked state: "+gate.Id);
  while(title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  Click(Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f||title.selectPanel.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  int diamondIndex=Characters.Items(title.characterMenu).FindIndex(x=>x.data==diamond.Data);
  Entries(title.characterMenu)[diamondIndex].button.Select();yield return null;
  Require(title.characterMenu.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t=>t.text.Contains(diamond.Goal))||Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().Any(t=>t.gameObject.activeInHierarchy&&t.text.Contains(diamond.Goal)),"native selection description exposes the locked character's explicit goal");
  var purchase=(Button)AccessTools.Field(typeof(Unlockable),"unlockButton").GetValue(diamond.Unlock);
  Require(purchase==null||!purchase.gameObject.activeInHierarchy,"native focus does not restore a currency purchase control for gated content");
  Click(Entries(title.characterMenu)[diamondIndex].button);Require(title.CurrentState is CharacterSelectState,"locked character remains focusable but cannot confirm a loadout");
  yield return Capture("achievement-locked-character.png");
  Click(Entries(title.characterMenu)[0].button);while(!Active(title.gunMenu)||title.gunMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  int shotgunIndex=NativeSelection.GunItems(title.gunMenu).FindIndex(x=>x.data==shotgun.Data);
  Click(Entries(title.gunMenu)[shotgunIndex].button);Require(title.CurrentState is GunSelectState,"locked weapon cannot confirm a loadout");
  Click(Entries(title.gunMenu)[0].button);while(!(title.CurrentState is ModeSelectState))yield return null;
  var modes=(Toggle[])AccessTools.Field(typeof(flanne.UI.ToggleGroupMenu<MapData>),"toggles").GetValue(title.modeSelectMenu);
  var quick=modes.Single(t=>t.GetComponent<DataUIBinding<MapData>>().data==quickplay.Data);quick.isOn=true;Click(title.modeSelectStartButton);
  Require(title.CurrentState is ModeSelectState,"unearned Quickplay cannot start");
  modes[0].isOn=true;Click(title.modeSelectStartButton);while(!(title.CurrentState is MapSelectState))yield return null;
  Click(title.mapSelectStartButton);
  GameController game;while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
  var player=PlayerController.Instance;player.playerHealth.isInvincible.Flip();var timer=GameTimer.SharedInstance;timer.Stop();
  // Explicit native clock preconditions; this does not claim two/five minutes
  // were played. Actual native timer updates and production observers earn it.
  AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(timer,119.5f);yield return null;yield return null;
  Require(!Progression.Allowed(diamond.Data),"survival gate remains locked below 120 seconds");
  AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(timer,120f);timer.Start();
  while(UnlockNotifications.Instance.Showing!=diamond.Id)yield return null;timer.Stop();
  Require(Progression.Allowed(diamond.Data)&&SaveSystem.data.characterUnlocks.unlocks[diamond.Index],"real native survival observation earns and applies ownership");
  Require(Time.timeScale>0&&!PauseController.isPaused&&UnlockNotifications.Instance.Root.GetComponentInParent<GraphicRaycaster>()==null,"notice neither pauses gameplay nor intercepts UI input");
  Require(UnlockNotifications.Instance.Icon.sprite==diamond.Icon&&UnlockNotifications.Instance.Label.text.Contains(diamond.Goal),"notice uses native content icon, name and completed goal");
  yield return Capture("achievement-earned-diamond.png");yield return CheckRevolver(game,player,"achievement-input.png");
  AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(timer,300f);timer.Start();
  while(!Progression.Allowed(quickplay.Data))yield return null;timer.Stop();
  while(UnlockNotifications.Instance.Showing!=crossbow.Id)yield return null;
  Require(Progression.State.pending.SequenceEqual(new[]{quickplay.Id}),"simultaneous weapon and scenario unlock notices queue deterministically");
  Require(PointsTracker.pts==points,"earned unlocks spend no currency");
  yield return Capture("achievement-queued-crossbow.png");SaveSystem.Save();
  SetPhase("PASS");Log("complete","Native survival -> ownership -> transparent notice, uninterrupted shooting and interrupted queue save; clock thresholds explicitly seeded");Application.Quit(0);
 }
 IEnumerator AchievementRun(string character,string weapon,string map,int darkness=0,System.Action prepareRun=null)
 {
  TitleScreenController title;while((title=Object.FindObjectOfType<TitleScreenController>())==null||!Active(title.mainMenu))yield return null;
  Click(Entries(title.mainMenu)[0].button);
  while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)
  {
   var fresh=Resources.FindObjectsOfTypeAll<Button>().SingleOrDefault(b=>b.name=="ResumeNewRun"&&b.gameObject.activeInHierarchy);
   if(fresh!=null&&fresh.IsInteractable())Click(fresh);yield return null;
  }
  int c=Characters.Items(title.characterMenu).FindIndex(x=>x.data.name==character);Require(c>=0&&Progression.Allowed(title.characterMenu[c]),"earned native character for next run: "+character);Click(Entries(title.characterMenu)[c].button);
  while(!Active(title.gunMenu)||title.gunMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  int w=NativeSelection.GunItems(title.gunMenu).FindIndex(x=>x.data.name==weapon);Require(w>=0&&Progression.Allowed(title.gunMenu[w]),"earned native weapon for next run: "+weapon);Click(Entries(title.gunMenu)[w].button);
  while(!(title.CurrentState is ModeSelectState))yield return null;
  var modes=(Toggle[])AccessTools.Field(typeof(ToggleGroupMenu<MapData>),"toggles").GetValue(title.modeSelectMenu);
  bool direct=map=="Endless_Forest"||map=="10M_Forest"||map=="Challenge_Forest";
  prepareRun?.Invoke();
  (direct?modes.Single(t=>t.GetComponent<DataUIBinding<MapData>>().data.name==map):modes[0]).isOn=true;if(direct)Loadout.difficultyLevel=darkness;Click(title.modeSelectStartButton);
  if(!direct)
  {
  while(!(title.CurrentState is MapSelectState))yield return null;
  var maps=(Toggle[])AccessTools.Field(typeof(ToggleGroupMenu<MapData>),"toggles").GetValue(title.mapSelectMenu);var entry=maps.Single(x=>x.GetComponent<DataUIBinding<MapData>>().data.name==map);
  Require(Progression.Allowed(entry.GetComponent<DataUIBinding<MapData>>().data),"earned native scenario for next run: "+map);entry.isOn=true;
  // Darkness record is a controlled fixture precondition, not a claim that the
  // intervening Darkness clears were played. Native loadout/clear paths follow.
  Loadout.difficultyLevel=darkness;Click(title.mapSelectStartButton);
  }
  GameController game;while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
  PlayerController.Instance.playerHealth.isInvincible.Flip();GameTimer.SharedInstance.Stop();
 }
 IEnumerator AchievementPick(GameController game,string name,int seedLevel)
 {
  var generator=game.powerupGenerator;var poolField=AccessTools.Field(typeof(PowerupGenerator),"powerupPool");var pool=(System.Collections.Generic.List<PowerupPoolItem>)poolField.GetValue(generator);
  var desired=pool.Single(x=>x.powerup.name==name);
  Require((bool)AccessTools.Method(typeof(PowerupGenerator),"PrereqsMet").Invoke(generator,new object[]{desired.powerup}),"native prerequisites satisfied before requesting offer: "+name);
  var choices=pool.Where(x=>x!=desired&&x.powerup.prereqs.Count==0).Take(4).Concat(new[]{desired}).ToList();poolField.SetValue(generator,choices);
  var xp=game.playerXP;AccessTools.Field(typeof(PlayerXP),"<level>k__BackingField").SetValue(xp,seedLevel);xp.GainXP((float)AccessTools.Property(typeof(PlayerXP),"xpToLevel").GetValue(xp)+1f);
  while(!(game.CurrentState is PowerupMenuState)||AccessTools.Field(typeof(ToggleGroupMenu<Powerup>),"ConfirmEvent").GetValue(game.powerupMenu)==null)
  {
   if((Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-combat-buffs-")||Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-bugfix-"))&&game.CurrentState is GunEvoMenuState&&AccessTools.Field(typeof(flanne.UI.Menu),"ClickEvent").GetValue(game.gunEvoMenu)!=null&&game.gunEvoMenu.GetEntry(0).IsInteractable())
   {
    Log("fixture-evolution","Select the actual native level20 evolution, then earn the next level for the pending perk; later baseline includes this evolution.");Click(game.gunEvoMenu.GetEntry(0));
    while(!(game.CurrentState is CombatState))yield return null;
    xp.GainXP((float)AccessTools.Property(typeof(PlayerXP),"xpToLevel").GetValue(xp)+1f);
   }
   yield return null;
  }
  var toggles=(Toggle[])AccessTools.Field(typeof(ToggleGroupMenu<Powerup>),"toggles").GetValue(game.powerupMenu);
  var target=toggles.Take(game.numPowerupChoices).Single(t=>t.GetComponent<DataUIBinding<Powerup>>().data==desired.powerup);target.isOn=true;
  Button confirm=null;while(confirm==null||!confirm.IsInteractable())
  {confirm=Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(b=>b.gameObject.activeInHierarchy&&Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentTarget(i)==game.powerupMenu&&b.onClick.GetPersistentMethodName(i)=="OnConfirmClicked"));yield return null;}
  Require(UnlockNotifications.Instance.Root==null||!UnlockNotifications.Instance.Root.activeInHierarchy,"achievement notice does not cover native level-up choices");Click(confirm);
  while(!(game.CurrentState is CombatState))yield return null;
  poolField.SetValue(generator,pool.Where(p=>p.numTimeRepeatable!=0).ToList());Require(xp.level>=seedLevel+1,"native XP event completes the actual level-up choice: "+name);
 }
 IEnumerator AchievementBoss(GameController game,int index,bool cleanupOnly=false)
 {
  var source=SelectedMap.MapData.bossSpawns[index];var spawner=Object.FindObjectOfType<BossSpawner>();
  spawner.LoadSpawners(new System.Collections.Generic.List<BossSpawn>{new BossSpawn{bossPrefab=source.bossPrefab,timeToSpawn=0,maxHP=10,spawnPosition=new Vector2(15,0),dontSpawnArena=true,killAllOnSpawn=cleanupOnly}});
  Health target;while((target=Object.FindObjectsOfType<Health>().FirstOrDefault(h=>!h.isDead&&h.gameObject.name.Replace("(Clone)","").Trim()==source.bossPrefab.name&&h.gameObject.activeInHierarchy))==null)yield return null;
  yield return null;
  if(cleanupOnly){target.AutoKill();yield break;}
  target.HPChange(-100000);yield return null;Require(target.isDead,"real spawned source boss defeated through native Health damage: "+source.bossPrefab.name);
 }
 IEnumerator AchievementExit(GameController game,bool survived)
 {
  if(!survived){PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.giveupButton);}
  while(!(game.CurrentState is PlayerSurvivedState)&&!(game.CurrentState is PlayerDeadState))yield return null;
  while(!game.quitToTitleButton.gameObject.activeInHierarchy||!game.quitToTitleButton.IsInteractable())yield return null;Click(game.quitToTitleButton);
  while(Object.FindObjectOfType<TitleScreenController>()==null||!Active(Object.FindObjectOfType<TitleScreenController>().mainMenu))yield return null;
 }
 IEnumerator AchievementWin(GameController game)
 {
  SetPhase("native-win-"+SelectedMap.MapData.name+"-"+Loadout.CharacterSelection.name+"-"+Loadout.GunSelection.name);
  AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(GameTimer.SharedInstance,1200f);GameTimer.SharedInstance.Start();
  while(!(game.CurrentState is PlayerSurvivedState))yield return null;
  yield return AchievementExit(game,true);
 }
 IEnumerator CheckAchievementEvents(TitleScreenController title)
 {
  foreach(var gate in Progression.Gates)Require(gate.Icon!=null,"native content artwork for unlock notice: "+gate.Id);
  SetPhase("native-rune-spending");
  PointsTracker.pts=10000;Click(Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  ((CharacterSelectState)title.CurrentState).OnClickRunes();while(!title.runeMenuPanel.gameObject.activeInHierarchy)yield return null;
  var rune=Object.FindObjectsOfType<RuneUnlocker>().First(x=>!x.locked&&x.level==0);var runeButton=(Button)AccessTools.Field(typeof(RuneUnlocker),"button").GetValue(rune);runeButton.Select();int cost=rune.costPerLevel;
  AccessTools.Method(typeof(RuneUnlocker),"StartPress").Invoke(rune,null);while(rune.level==0)yield return null;AccessTools.Method(typeof(RuneUnlocker),"ReleasePress").Invoke(rune,null);
  Require(rune.level==1&&PointsTracker.pts==10000-cost,"unchanged native hold-to-upgrade rune path spends its exact currency cost");
  Click(title.runeConfirmButton);while(!(title.CurrentState is CharacterSelectState)||!title.characterMenu.interactable)yield return null;
  Click(title.loadoutBackButton);while(!Active(title.mainMenu))yield return null;title.Save();
  yield return AchievementRun("Shana","CrossbowData","20M_Forest");var game=Object.FindObjectOfType<GameController>();
  foreach(var step in new[]{new{name="PyroMage",level=1},new{name="IntenseFlame",level=2},new{name="ElectroMage",level=3},new{name="FrostMage",level=4},new{name="GhostFriend",level=5},new{name="Haste",level=7},new{name="RunAndGun",level=9}})
   yield return AchievementPick(game,step.name,step.level);
  Require(Progression.Has("perk:pyro_mage_name")&&Progression.Has("perk:intense_burn_name")&&Progression.State.bestLevel==10,"native perk and level events award build/level gates");
  AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(GameTimer.SharedInstance,600f);GameTimer.SharedInstance.Start();while(!Progression.Has("haste-run"))yield return null;GameTimer.SharedInstance.Stop();
  yield return AchievementBoss(game,0);
  foreach(var fact in new[]{"boss:first","boss:CrossbowData","boss:electro","boss:frost","boss:summon"})Require(Progression.Has(fact),"actual source boss death records current build: "+fact);
  yield return AchievementBoss(game,1);Require(Progression.Has("boss:second"),"second source Forest boss awards Charge Cannon");
  yield return AchievementExit(game,false);
  Require(Progression.State.bestLevel==10&&Progression.Has("boss:first"),"failed native run retains level, perk and boss accomplishments");
  yield return AchievementRun("Hina","ShotgunData","20M_Forest",3);game=Object.FindObjectOfType<GameController>();yield return AchievementBoss(game,0);
  Require(Progression.Has("boss:ShotgunData"),"Shotgun-specific first boss records the actual current weapon");
  yield return AchievementWin(game);
  foreach(string name in new[]{"Katana","Revamped_Haruka","SalvoKnivesData","20M_Temple","Endless_Forest"})Require(Progression.Allowed(Progression.Gates.Single(g=>g.Data.name==name).Data),"native Hina/Darkness/Forest clear gate earned: "+name);
  yield return AchievementRun("Diamond","RevolverData","20M_Forest",1);game=Object.FindObjectOfType<GameController>();yield return AchievementWin(game);
  Require(Progression.Allowed(Characters.Data["Dimond_Portrait"]),"actual Diamond Darkness clear earns Bulwark");
  yield return AchievementRun("Katana","MagicBowData","20M_Forest");game=Object.FindObjectOfType<GameController>();yield return AchievementWin(game);
  Require(Progression.Has("forest-gun:MagicBowData")&&Progression.Has("forest-character:Katana"),"Magic Bow and Katana clear facts retain exact native identities");
  yield return AchievementRun("Shana","RevolverData","20M_Temple");game=Object.FindObjectOfType<GameController>();yield return AchievementWin(game);
  Require(Progression.Has("clear:20M_Temple"),"native Temple win reaches unified clear path without a duplicate unlock modal");
  yield return AchievementRun("Shana","RevolverData","20M_PumpkinPatch");game=Object.FindObjectOfType<GameController>();yield return AchievementWin(game);
  Require(Progression.Has("clear:20M_PumpkinPatch"),"native Pumpkin win reaches unified clear path");
  yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();
  var quest=Object.FindObjectOfType<ForestMapRhogogSpawner>();Require(quest!=null,"secret-boss quest belongs to the reachable native Forest map");
  var secret=(GameObject)AccessTools.Field(typeof(ForestMapRhogogSpawner),"rhogogPrefab").GetValue(quest);
  AccessTools.Field(typeof(ForestMapRhogogSpawner),"treesActivated").SetValue(quest,true);
  AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(GameTimer.SharedInstance,1199f);GameTimer.SharedInstance.Start();
  Health rhogog;while((rhogog=Object.FindObjectsOfType<Health>().FirstOrDefault(h=>h.gameObject.name.Replace("(Clone)","").Trim()==secret.name))==null)yield return null;GameTimer.SharedInstance.Stop();
  yield return null;while(rhogog.isInvincible.value)yield return null;
  rhogog.HPChange(-1000000);GameTimer.SharedInstance.Stop();
  Require(rhogog.isDead&&Progression.Has("rhogog")&&Progression.Allowed(Progression.Gates.Single(g=>g.Data.name=="SporeGunData").Data),"actual native secret-boss quest spawn/death awards Spore Gun without its old confirmation modal");
  while(UnlockNotifications.Instance.Showing!="weapon:SporeGunData")yield return null;yield return Capture("achievement-secret-boss.png");
  yield return AchievementExit(game,false);
  Require(Progression.Gates.All(g=>Progression.Allowed(g.Data)),"all twenty-nine varied gates are now earned and persisted through their native production events");
  SetPhase("PASS");Log("complete","Native perk offers, XP, boss deaths, failure retention, exact rune spending and scenario/character/weapon clears; level/time/Darkness preconditions explicitly seeded");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(SeekerWeapon),"OnDestroy")]
class SeekerLifecycleProbe
{
 static void Prefix(SeekerWeapon __instance)
 {
  var stats=AccessTools.Field(typeof(Summon),"stats").GetValue(__instance);
  RuntimeChecks.Log("native-seeker-destroy",__instance.name+" initialized="+(stats!=null)+" active="+__instance.gameObject.activeSelf);
 }
}
