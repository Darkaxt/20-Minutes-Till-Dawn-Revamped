using System;
using System.IO;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne.Player;
using flanne.Player.Buffs;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 [Serializable]class ResumeExpected
 {public string character,weapon,map,recipe;public int hp,maxHP,soul,maxSoul,level,ammo,points,kills;public float damage,fireRate,moveSpeed,seconds;public bool hitless,usedShooting,usedManualShooting;}
 string RunCheckpoint=>Path.Combine(Application.persistentDataPath,"revamped-run.json");
 IEnumerator CheckResume(TitleScreenController title,bool continuation)
 {
  if(!continuation)
  {
   SetPhase("resume-native-acquisition");
   // Explicit isolated-profile precondition for repeatable feature validation.
   AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.RunResume"),"Invalidate").Invoke(null,null);
   string variant=Environment.GetEnvironmentVariable("MODERNIZATION_TEST");bool runtime=variant.Contains("runtime"),cycle=variant.Contains("cycle"),challenge=variant.Contains("challenge");
   if(variant.Contains("custom"))yield return StartCustomRun();else yield return AchievementRun("Shana","RevolverData",challenge?"Challenge_Forest":runtime||cycle?"Endless_Forest":"20M_Forest",prepareRun:variant.Contains("runes")?(System.Action)PrepareRunRuneRanks:null);var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;
   var nativeEntries=(System.Collections.IList)AccessTools.Field(typeof(flanne.UI.ToggleGroupMenu<Powerup>),"entries").GetValue(game.powerupMenu);
   Log("resume-menu-initialization",nativeEntries==null?"null list":nativeEntries.GetType().FullName+" count="+nativeEntries.Count+" null="+nativeEntries.Cast<object>().Count(x=>x==null));
   yield return Picks(game,"PowerShot","BigShot","Haste","DragonEgg");
   Object.FindObjectOfType<HordeSpawner>().enabled=false;
   if(runtime)yield return PrepareResumeRuntime(game,p);
   if(cycle)PrepareEndlessCycle();
   if(variant.Contains("evolution"))yield return SelectResumeEvolution(game);
   if(variant.Contains("custom"))yield return CheckCustomLive(game);
   var waves=cycle?SelectedMap.MapData.endlessSpawnSessions:SelectedMap.MapData.spawnSessions;
   var elite=waves.First(w=>w.isElite);var horde=Object.FindObjectOfType<HordeSpawner>();
   if(challenge)
   {
    AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(GameTimer.SharedInstance,elite.startTime);
    AccessTools.Method(typeof(ChallengeRun),"Update").Invoke(ChallengeRun.Instance,null);
   }
   else
   {
   horde.LoadSpawners(new System.Collections.Generic.List<SpawnSession>{new SpawnSession{monsterPrefab=elite.monsterPrefab,HP=elite.HP,isElite=true,startTime=0,duration=0,maximum=0}});
   AccessTools.Method(typeof(HordeSpawner),"Spawn").Invoke(horde,new object[]{elite.monsterPrefab.name,elite.HP,true});
   }
   Health health;while((health=Object.FindObjectsOfType<Health>().SingleOrDefault(h=>h.name==elite.monsterPrefab.name+"(Clone)"&&!h.isDead))==null)yield return null;
   health.TakeDamage(DamageType.Bullet,1000000);Require(health.isDead,"actual native elite is defeated before suspension");
   var bosses=cycle?SelectedMap.MapData.endlessBossSpawn:SelectedMap.MapData.bossSpawns;
   var boss=bosses[0];float originalTime=boss.timeToSpawn;boss.timeToSpawn=0;
   if(challenge)
   {
    boss.timeToSpawn=originalTime;
    AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(GameTimer.SharedInstance,originalTime);
    AccessTools.Method(typeof(ChallengeRun),"Update").Invoke(ChallengeRun.Instance,null);
   }
   else
   {
   Object.FindObjectOfType<BossSpawner>().LoadSpawners(new System.Collections.Generic.List<BossSpawn>{boss});
   boss.timeToSpawn=originalTime;
   }
   Health target;while((target=Object.FindObjectsOfType<Health>().FirstOrDefault(h=>h.name==boss.bossPrefab.name+"(Clone)"&&!h.isDead))==null)yield return null;
   target.TakeDamage(DamageType.Bullet,1000000);Require(target.isDead,"actual native scheduled boss is defeated before suspension");yield return null;
   if(challenge)Require(ChallengeRun.Instance.KilledBosses.Contains(0)&&ChallengeRun.Instance.KilledElites.Contains(waves.IndexOf(elite)),"real Challenge scheduling records both exact defeated encounter identities before saving");
   AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(p.playerHealth,new object[]{1});p.playerHealth.Heal(1);
   Require(!game.hitlessDetector.hitless,"actual native hurt and healing leave a non-hitless run to preserve");
   yield return CheckRevolver(game,p,"resume-save-native-shooting.png",p.gun.gunData.name);
   AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(GameTimer.SharedInstance,420f);
   var expected=new ResumeExpected{character=Loadout.CharacterSelection.name,weapon=Loadout.GunSelection.name,map=SelectedMap.MapData.name,recipe=p.gun.gunData.name,hp=p.playerHealth.hp,maxHP=p.playerHealth.maxHP,
    soul=p.playerHealth.shp,maxSoul=p.playerHealth.maxSHP,level=game.playerXP.level,ammo=p.ammo.amount,points=PointsTracker.pts,kills=ScoreCalculator.SharedInstance.GetScore().enemiesKilled,
    damage=p.gun.damage,fireRate=1/p.gun.shotCooldown,moveSpeed=p.finalMoveSpeed,seconds=420,hitless=false,usedShooting=game.shootDetector.usedShooting,usedManualShooting=game.shootDetector.usedManualShooting};
   PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();
   var save=Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy);
   Require(File.Exists(RunCheckpoint),"native pause writes a separate run checkpoint");
   Canvas.ForceUpdateCanvases();
   Require(save.GetComponentInChildren<TMPro.TMP_Text>().text=="Save & return"&&game.pauseMenu.GetComponentsInChildren<TMPro.TMP_Text>().Count(t=>t.text=="Give Up")==1,"pause has one Give Up and a distinct Save & return label");
   var saveCorners=new Vector3[4];save.GetComponent<RectTransform>().GetWorldCorners(saveCorners);var panelCorners=new Vector3[4];game.pauseMenu.GetComponent<RectTransform>().GetWorldCorners(panelCorners);
   Require(saveCorners[0].y>panelCorners[0].y&&saveCorners[2].y<panelCorners[2].y,"Save & return lies fully inside the native opaque pause panel");
   File.WriteAllText(Path.Combine(ResultDir,"pause-hierarchy.txt"),string.Join("\n",game.pauseMenu.GetComponentsInChildren<RectTransform>(true).Select(t=>t.name+" parent="+t.parent?.name+" size="+t.rect.size+" pos="+t.anchoredPosition+" anchor="+t.anchorMin+"/"+t.anchorMax+" image="+t.GetComponent<Image>()?.sprite?.name)));
   yield return Capture("resume-save-and-return.png");File.WriteAllText(Path.Combine(ResultDir,"expected-resume.json"),JsonUtility.ToJson(expected,true));Click(save);
   while(Object.FindObjectOfType<TitleScreenController>()==null||!Active(Object.FindObjectOfType<TitleScreenController>().mainMenu))yield return null;
   Require(PointsTracker.pts==expected.points,"suspending a run does not deposit its score as spendable credits");
   File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));
   SetPhase("PASS");Log("complete","Native acquisitions, exact elite/boss defeats, hurt history, pause save and title return without score deposit verified; encounter clocks explicitly seeded");Application.Quit(0);yield break;
  }
  SetPhase("resume-fresh-process-continue");
  var fixture=Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR");
  var wanted=JsonUtility.FromJson<ResumeExpected>(File.ReadAllText(Path.Combine(fixture,"expected-resume.json")));
  string before=File.ReadAllText(RunCheckpoint);
  Click(Entries(title.mainMenu)[0].button);yield return null;
  var back=Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeBack"&&b.gameObject.activeInHierarchy);
  Require(!title.mainMenu.interactable,"resume prompt blocks the underlying native menu");Click(back);yield return null;
  Require(title.mainMenu.interactable&&File.ReadAllText(RunCheckpoint)==before,"Back preserves the exact checkpoint and restores the native title menu");
  Click(Entries(title.mainMenu)[0].button);yield return null;
  yield return Capture("resume-native-prompt.png");
  var resume=Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy);Click(resume);
  GameController current;while((current=Object.FindObjectOfType<GameController>())==null||!(current.CurrentState is CombatState))
  {
   var dialog=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy);
   if(dialog!=null&&(dialog.text.StartsWith("Could not restore")||resume==null||!resume.IsInteractable()))throw new InvalidOperationException("Continue rejected checkpoint: "+dialog.text);
   yield return null;
  }
  var player=PlayerController.Instance;GameTimer.SharedInstance.Stop();Object.FindObjectOfType<HordeSpawner>().enabled=false;player.playerHealth.isInvincible.Flip();
  Require(Loadout.CharacterSelection.name==wanted.character&&Loadout.GunSelection.name==wanted.weapon&&SelectedMap.MapData.name==wanted.map,"fresh process restores the exact semantic character, gun and scenario");
  Require(player.playerHealth.hp==wanted.hp&&player.playerHealth.maxHP==wanted.maxHP&&player.playerHealth.shp==wanted.soul&&player.playerHealth.maxSHP==wanted.maxSoul,"resume restores red/soul health and capacities without healing or duplication");
  Require(current.playerXP.level==wanted.level&&player.ammo.amount==wanted.ammo,"resume restores native level and ammo");
  Require(Mathf.Abs(player.gun.damage-wanted.damage)<.001f&&Mathf.Abs(1/player.gun.shotCooldown-wanted.fireRate)<.001f&&Mathf.Abs(player.finalMoveSpeed-wanted.moveSpeed)<.001f,"native rebuilt gun/player effective attributes match the saved build");
  Require(GameTimer.SharedInstance.timer>=wanted.seconds&&GameTimer.SharedInstance.timer<wanted.seconds+1,"resume starts at the saved gameplay clock");
  Require(!current.hitlessDetector.hitless&&ScoreCalculator.SharedInstance.GetScore().enemiesKilled==wanted.kills,"resume preserves monotonic hitless history and native kill score");
  Require(current.shootDetector.usedShooting==wanted.usedShooting&&current.shootDetector.usedManualShooting==wanted.usedManualShooting,"resume preserves native shooting history for achievement eligibility");
  Require(player.gun.gunData.name==wanted.recipe,"resume restores the exact current weapon/evolution recipe");
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("custom"))Require(CustomMode.Active&&CustomMode.Get("stat:BulletDamage")==3&&CustomMode.Get("xp")==2&&SelectedMap.MapData.achievementsDisabled,"fresh process restores Custom settings and progression isolation");
  Require(player.GetComponentsInChildren<SummonEgg>(true).Length==1,"the acquired native dragon egg is reconstructed exactly once");
  RunResume.Snapshot original;using(var input=File.OpenRead(Path.Combine(fixture,"saved-run.json")))original=(RunResume.Snapshot)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(RunResume.Snapshot)).ReadObject(input);
  var restoredBosses=original.endlessCycle>0?SelectedMap.MapData.endlessBossSpawn:SelectedMap.MapData.bossSpawns;
  Require(original.killedBosses.Count>0&&original.killedBosses.All(i=>!Object.FindObjectsOfType<Health>().Any(h=>h.name==restoredBosses[i].bossPrefab.name+"(Clone)"&&!h.isDead)),"defeated scheduled bosses do not respawn or award another reward");
  foreach(var pool in original.pools)
  {
   var actual=(System.Collections.Generic.List<PowerupPoolItem>)AccessTools.Field(typeof(PowerupGenerator),pool.field).GetValue(current.powerupGenerator);
   Require(actual.Select(x=>RunResume.Key(x.powerup)+"="+x.numTimeRepeatable).SequenceEqual(pool.entries.Select(x=>x.key+"="+x.remaining)),"exact finite native offer pool survives resume: "+pool.field);
  }
  Require(Object.FindObjectsOfType<flanne.Pickups.ChestPickup>().Count(c=>AccessTools.Field(typeof(flanne.Pickups.ChestPickup),"_xpFountainCoroutine").GetValue(c)==null)==original.chests&&Object.FindObjectsOfType<flanne.Pickups.DevilDealPickup>().Length==original.devilDeals,"uncollected native chest/devil-deal counts restore without duplicating defeated encounters");
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("runtime"))yield return VerifyResumeRuntime(current,player,original);
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("cycle"))yield return VerifyEndlessCycle(original);
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("challenge"))Require(ChallengeRun.Instance.EnemyKills==original.challengeEnemies&&ChallengeRun.Instance.BossKills==original.challengeBosses&&ChallengeRun.Instance.KilledBosses.SetEquals(original.killedBosses)&&ChallengeRun.Instance.KilledElites.SetEquals(original.killedElites),"Challenge resumes its exact quotas and defeated encounter identities");
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("runes"))Require(Loadout.RuneSelection.Single(r=>r.name=="GrowthRuneData").level==2&&Object.FindObjectsOfType<flanne.UI.RuneUnlocker>().Length==0,"saved run-local rune rank is restored independently of title purchases");
  yield return Capture("resume-restored-native-run.png");
  yield return CheckRevolver(current,player,"resume-original-gun-input.png",wanted.recipe);
  yield return AchievementExit(current,false);
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("custom"))VerifyCustomResult(fixture,wanted.points);
  var invalid=File.ReadAllText(RunCheckpoint);Require(invalid.Contains("\"valid\":false"),"actual native give-up/death invalidates the suspended checkpoint");
  yield return AchievementRun("Shana","RevolverData","20M_Forest");current=Object.FindObjectOfType<GameController>();
  Require(current.hitlessDetector.hitless&&GameTimer.SharedInstance.timer<1,"ordinary new run has fresh hitless/clock state and no resume prompt");yield return AchievementExit(current,false);
  SetPhase("PASS");Log("complete","Fresh-process Continue, Back preservation, native build/health/XP/ammo/hitless/score/egg restoration, defeated-boss suppression, original gun input and new-run cleanup verified");Application.Quit(0);
 }
 IEnumerator PrepareResumeRuntime(GameController game,PlayerController player)
 {
  yield return Picks(game,"TrainedDragon","AgedDragon");
  var dragon=player.GetComponentsInChildren<ShootingSummon>(true).Single(s=>s.SummonTypeID=="Dragon");int initial=dragon.baseDamage;
  Time.timeScale=20;while(dragon.baseDamage==initial)yield return null;Time.timeScale=1;
  Require(dragon.baseDamage>initial,"actual native dragon training increases damage before saving");
  var entry=((System.Collections.Generic.List<PowerupPoolItem>)AccessTools.Field(typeof(PowerupGenerator),"powerupPool").GetValue(game.powerupGenerator)).Single(x=>x.powerup.name=="Revamped_Adrenaline");
  AccessTools.Field(typeof(PowerupGenerator),"powerupPool").SetValue(game.powerupGenerator,new System.Collections.Generic.List<PowerupPoolItem>{entry});
  yield return ChooseFallback(game,offers=>offers.Single());
  var buffs=player.GetComponentInChildren<PlayerBuffs>();Time.timeScale=20;
  while(((System.Collections.Generic.List<Buff>)AccessTools.Field(typeof(PlayerBuffs),"buffs").GetValue(buffs)).OfType<TemporaryStatBuff>().Any())yield return null;
  Time.timeScale=1;Require(entry.numTimeRepeatable==2,"expired first Adrenaline retains its actual finite Endless consumption");
  yield return ChooseFallback(game,offers=>offers.Single());
  Require(((System.Collections.Generic.List<Buff>)AccessTools.Field(typeof(PlayerBuffs),"buffs").GetValue(buffs)).OfType<TemporaryStatBuff>().Count()==1&&entry.numTimeRepeatable==1,"one expired and one active native Adrenaline exist in acquisition history, with one remaining offer");
 }
 IEnumerator VerifyResumeRuntime(GameController game,PlayerController player,RunResume.Snapshot original)
 {
  var dragon=player.GetComponentsInChildren<ShootingSummon>(true).Single(s=>s.SummonTypeID=="Dragon");var wanted=original.runtime.summons.Single(s=>s.id=="Dragon");
  Require(dragon.baseDamage==wanted.damage&&Mathf.Abs((float)AccessTools.Field(typeof(StatMod),"_multiplierBonus").GetValue(dragon.attackSpeedMod)-wanted.speed.bonus)<.001f,"native trained dragon damage and attack-speed bonuses survive fresh-process Continue");
  var buffs=player.GetComponentInChildren<PlayerBuffs>();
  Require(((System.Collections.Generic.List<Buff>)AccessTools.Field(typeof(PlayerBuffs),"buffs").GetValue(buffs)).OfType<TemporaryStatBuff>().Count()==1,"resume reconstructs only the active timed buff, not its expired earlier acquisition");
  var damage=player.stats[StatType.BulletDamage];float before=(float)AccessTools.Field(typeof(StatMod),"_multiplierBonus").GetValue(damage);
  float start=Time.time;Time.timeScale=20;
  while(((System.Collections.Generic.List<Buff>)AccessTools.Field(typeof(PlayerBuffs),"buffs").GetValue(buffs)).OfType<TemporaryStatBuff>().Any())yield return null;
  Time.timeScale=1;float after=(float)AccessTools.Field(typeof(StatMod),"_multiplierBonus").GetValue(damage);
  Require(Mathf.Abs(before-after-.5f)<.001f&&Time.time-start>=original.runtime.temporary.Single().remaining-.5f,"the restored native Adrenaline expires at its saved remaining duration and removes its bonus once");
 }
}
