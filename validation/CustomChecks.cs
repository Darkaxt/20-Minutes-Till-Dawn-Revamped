using System;
using System.IO;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 string Ownership()=>string.Join("|",new[]{string.Join(",",SaveSystem.data.characterUnlocks.unlocks),string.Join(",",SaveSystem.data.gunUnlocks.unlocks),string.Join(",",SaveSystem.data.runeUnlocks.unlocks),string.Join(",",SaveSystem.data.characterHighestClear),string.Join(",",SaveSystem.data.gunHighestClear),SaveSystem.data.difficultyUnlocked.ToString(),string.Join(",",Progression.State.earned.OrderBy(x=>x)),string.Join(",",Progression.State.facts.OrderBy(x=>x)),string.Join(",",Progression.State.attempts.Select(a=>a.id+"="+a.text).OrderBy(x=>x)),Progression.State.challengeDifficultyUnlocked.ToString()});
 Button CustomButton(string name)=>CustomMenu.Instance.GetComponentsInChildren<Button>().Single(b=>b.name==name);
 void CustomValue(int index,string value)
 {
  var field=CustomMenu.Instance.GetComponentsInChildren<TMP_InputField>().Single(f=>f.name=="CustomValue"+index);field.text=value;field.onEndEdit.Invoke(value);
 }
 IEnumerator OpenCustomConfiguration(string weapon="RevolverData")
 {
  SetPhase("native-custom-configuration");File.WriteAllText(Path.Combine(ResultDir,"custom-ownership.txt"),Ownership());
  var title=Object.FindObjectOfType<TitleScreenController>();
  Click(Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  Click(Entries(title.characterMenu)[Characters.Items(title.characterMenu).FindIndex(c=>c.data.name=="Shana")].button);
  while(!Active(title.gunMenu)||title.gunMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  Click(Entries(title.gunMenu)[NativeSelection.GunItems(title.gunMenu).FindIndex(g=>g.data.name==weapon)].button);
  while(!(title.CurrentState is ModeSelectState))yield return null;
  float originalDamage=Loadout.GunSelection.damage;File.WriteAllText(Path.Combine(ResultDir,"custom-basegun.txt"),originalDamage.ToString(System.Globalization.CultureInfo.InvariantCulture));
  ChallengeMode.Toggles(title.modeSelectMenu).Single(t=>t.GetComponent<flanne.UI.MapEntry>().data==CustomMode.MenuMap).isOn=true;Click(title.modeSelectStartButton);
  while(CustomMenu.Instance==null)yield return null;
  Require(CustomMode.Maps().All(Progression.Allowed),"Custom scenario choices contain only obtained content");
  var all=CustomMenu.Instance.GetComponentsInChildren<Selectable>();Require(all.All(c=>c.navigation.mode==Navigation.Mode.Explicit),"Custom uses native selectable controls with explicit navigation");
  yield return Capture("custom-native-run-menu.png");
 }
 IEnumerator StartCustomRun()
 {
  yield return OpenCustomConfiguration();float originalDamage=float.Parse(File.ReadAllText(Path.Combine(ResultDir,"custom-basegun.txt")),System.Globalization.CultureInfo.InvariantCulture);
  Click(CustomButton("CustomTab1"));CustomValue(0,"33");yield return Capture("custom-native-weapon-menu.png");
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("custom-native"))CustomValue(1,"2");
  Click(CustomButton("CustomTab2"));CustomValue(3,"2");
  Click(CustomButton("CustomTab3"));CustomValue(1,"2");CustomValue(2,"2");CustomValue(7,"2");
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("custom-native"))
  {CustomValue(0,"2");CustomValue(3,"1.5");CustomValue(4,"1.75");CustomValue(5,"3");CustomValue(6,"4");Click(CustomButton("CustomNext"));CustomValue(8,"3");CustomValue(9,"0.5");}
  Click(CustomButton("CustomTab0"));CustomValue(1,SaveSystem.data.difficultyUnlocked.ToString());CustomValue(2,"2");
  Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>();Click(CustomButton("CustomStart"));GameController game;while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
  GameTimer.SharedInstance.Stop();PlayerController.Instance.playerHealth.isInvincible.Flip();
  Require(CustomMode.Active&&SelectedMap.MapData.achievementsDisabled&&PlayerController.Instance.gun.gunData.damage==33,"native Custom start uses its isolated recipe and disables normal progression");
  Require(Loadout.GunSelection.damage==originalDamage,"the selected original weapon asset remains unchanged: "+originalDamage);
  yield return CustomSourceSpawns(game);
  Require(!(bool)AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.CustomNoSteamAchievement"),"Prefix").Invoke(null,null)&&!(bool)AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.CustomNoLeaderboard"),"Prefix").Invoke(null,null),"both production Steam guards deny Custom publication at the isolated offline boundary");
  float before=(float)AccessTools.Field(typeof(flanne.Player.PlayerXP),"xp").GetValue(game.playerXP);var originalXP=ResumeRuntime.Capture(game.playerXP.xpMultiplier);game.playerXP.GainXP(1);float after=(float)AccessTools.Field(typeof(flanne.Player.PlayerXP),"xp").GetValue(game.playerXP);
  Require(Mathf.Abs(after-before-2*(1+originalXP.flat)*(1+originalXP.bonus)*originalXP.reduction)<.01f,"native XP gain uses twice the underlying native experience value");
  // Continue with neutral XP during acquisition fixtures; the live editor below
  // restores2 before the actual suspend/fresh-process continuation.
  CustomMode.Set("xp",1);
 }
 IEnumerator CheckCustomLive(GameController game)
 {
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();
  var button=game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="CustomSettings");Click(button);while(CustomMenu.Instance==null)yield return null;
  Click(CustomButton("CustomTab2"));CustomValue(3,"3");Require(CustomMode.Get("stat:BulletDamage")==3,"native in-run attribute editor applies a live multiplier");
  Click(CustomButton("CustomTab5"));
  var candidate=CustomMode.AvailablePerks().Single(p=>p.Item2.name=="GhostFriend");Require(candidate!=null,"Custom native pool exposes the prerequisite-valid Ghost Friend upgrade");
  int acquired=((System.Collections.IList)AccessTools.Field(AccessTools.TypeByName("TwentyMinutes.Modernization.RunRecorder"),"Acquired").GetValue(game.GetComponent(AccessTools.TypeByName("TwentyMinutes.Modernization.RunRecorder")))).Count;
  string label="Obtain: "+candidate.Item2.nameString;var row=CustomMenu.Instance.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t=>t.name.StartsWith("CustomRow")&&t.text==label);
  int pages=int.Parse(CustomMenu.Instance.GetComponentsInChildren<TMP_Text>().Single(t=>t.name=="CustomPage").text.Split('/')[1]);
  for(int page=0;row==null&&page<pages;page++){Click(CustomButton("CustomNext"));row=CustomMenu.Instance.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t=>t.name.StartsWith("CustomRow")&&t.text==label);}
  Require(row!=null,"native paginated upgrade controls reach the available Ghost Friend");Click(CustomButton(row.name.Replace("CustomRow","CustomAction")));yield return null;
  int count=((System.Collections.IList)AccessTools.Field(AccessTools.TypeByName("TwentyMinutes.Modernization.RunRecorder"),"Acquired").GetValue(game.GetComponent(AccessTools.TypeByName("TwentyMinutes.Modernization.RunRecorder")))).Count;
  Require(count==acquired+1,"Custom obtains one native prerequisite-valid upgrade through normal acquisition");
  Click(CustomButton("CustomTab4"));var summon=Object.FindObjectsOfType<ShootingSummon>().Single(s=>s.SummonTypeID=="GhostFriend");
  float summonDamage=((ProjectileRecipe)AccessTools.Method(typeof(ShootingSummon),"GetProjectileRecipe").Invoke(summon,null)).damage;
  float cooldown=(float)AccessTools.Property(typeof(AttackingSummon),"finalAttackCooldown").GetValue(summon);
  CustomValue(0,"2");CustomValue(1,"1.5");
  Require(((ProjectileRecipe)AccessTools.Method(typeof(ShootingSummon),"GetProjectileRecipe").Invoke(summon,null)).damage==summonDamage*2&&Mathf.Abs((float)AccessTools.Property(typeof(AttackingSummon),"finalAttackCooldown").GetValue(summon)-Mathf.Max(.1f,cooldown/1.5f))<.001f,"native obtained summon recipe and cooldown use the live editor's independent multipliers");
  yield return Capture("custom-native-summon-menu.png");
  Click(CustomButton("CustomTab6"));int pickups=Object.FindObjectsOfType<flanne.Pickups.HPPickup>().Length+Object.FindObjectsOfType<flanne.Pickups.SHPPickup>().Length;
  Click(CustomButton("CustomAction0"));Require(Object.FindObjectsOfType<flanne.Pickups.HPPickup>().Length+Object.FindObjectsOfType<flanne.Pickups.SHPPickup>().Length==pickups+1,"native object control creates one real health pickup");
  var powerShot=Resources.FindObjectsOfTypeAll<Powerup>().First(p=>p.name=="PowerShot");float damage=PlayerController.Instance.gun.damage;CustomMode.Set("perk:"+RunResume.Key(powerShot)+":scale",2);
  Require(PlayerController.Instance.gun.damage>damage,"editing a collected upgrade changes its existing native stat contribution");
  Click(CustomButton("CustomTab0"));CustomValue(1,"2"); // Active Run screen has scenario row0 and XP row1.
  yield return Capture("custom-live-native-menu.png");Click(CustomButton("CustomBack"));while(!(game.CurrentState is PauseState))yield return null;
  Click(game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;
  Require(!PauseController.isPaused&&Time.timeScale>0,"returning from Custom rules and native Resume releases the original pause hold exactly once");
 }
 void VerifyCustomResult(string fixture,int startingCredits)
 {
  Require(Ownership()==File.ReadAllText(Path.Combine(fixture,"custom-ownership.txt")),"Custom exit leaves ownership, rune ranks, Darkness, clear records and achievement facts unchanged");
  Require(PointsTracker.pts>startingCredits&&SaveSystem.data.points==PointsTracker.pts,"Custom result deposits native credits and persists them");
  Require(!CustomMode.Active&&CustomMode.Current==null,"Custom run-local rules are released on return to title");
 }
 IEnumerator CheckCustom()
 {
  RunResume.Invalidate();string locked="scenario:Challenge_Forest";int previousCount=Progression.State.earned.RemoveAll(id=>id==locked);Progression.Save();Progression.Refresh();
  Log("fixture-precondition","The isolated journal's Challenge scenario is explicitly locked to test Custom ownership and unlock suppression; the original profile is restored afterward.");
  yield return StartCustomRun();var game=Object.FindObjectOfType<GameController>();
  yield return CustomNativeInput(game);
  yield return Picks(game,"PowerShot","BigShot","Haste","DragonEgg");yield return CheckCustomLive(game);
  yield return CheckRevolver(game,PlayerController.Instance,"custom-native-shooting.png");
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").EndsWith("-save"))
  {if(previousCount>0){Progression.State.earned.AddRange(Enumerable.Repeat(locked,previousCount));Progression.Save();Progression.Refresh();}yield return CustomSourceSuspend(game);yield break;}
  int credits=PointsTracker.pts;yield return AchievementWin(game);VerifyCustomResult(ResultDir,credits);
  Require(!Progression.State.earned.Contains(locked),"Custom Forest clear at the maximum earned Darkness does not award its otherwise eligible Challenge scenario");
  if(previousCount>0){Progression.State.earned.AddRange(Enumerable.Repeat(locked,previousCount));Progression.Save();Progression.Refresh();}
  yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();
  float originalDamage=float.Parse(File.ReadAllText(Path.Combine(ResultDir,"custom-basegun.txt")),System.Globalization.CultureInfo.InvariantCulture);
  Require(!CustomMode.Active&&PlayerController.Instance.gun.gunData.damage==originalDamage&&!SelectedMap.MapData.achievementsDisabled,"ordinary Forest restart uses the original gun and progression rules");
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Native configured/live Custom run, real modified shooting, credits-only victory and ordinary restart verified");Application.Quit(0);
 }
 IEnumerator CheckFreshCustom()
 {
  RunResume.Invalidate();yield return OpenCustomConfiguration();
  Require(CustomMode.Maps().Select(m=>m.name).OrderBy(n=>n).SequenceEqual(new[]{"10M_Forest","20M_Forest"}),"early-profile Custom presets expose only obtained Forest and Quickplay");
  Click(CustomButton("CustomStart"));GameController game;while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
  GameTimer.SharedInstance.Stop();PlayerController.Instance.playerHealth.isInvincible.Flip();
  Require(Loadout.RuneSelection.Count==0,"fresh Custom cannot enable unpurchased rune ranks");
  Require(!CustomMode.AvailablePerks().Any(p=>Characters.Powers.Values.Contains(p.Item2)||p.Item2.name=="Fusillade"),"fresh Custom upgrade menu excludes unavailable character powers and unmet native prerequisites");
  var source=SelectedMap.MapData.spawnSessions.First(w=>!w.isElite);var horde=Object.FindObjectOfType<HordeSpawner>();var existing=Object.FindObjectsOfType<Health>().Select(h=>h.GetInstanceID()).ToArray();
  AccessTools.Method(typeof(HordeSpawner),"Spawn").Invoke(horde,new object[]{source.monsterPrefab.name,source.HP,false});
  Object.FindObjectsOfType<Health>().Single(h=>!existing.Contains(h.GetInstanceID())&&h.name==source.monsterPrefab.name+"(Clone)").TakeDamage(DamageType.Bullet,1000000);
  int points=PointsTracker.pts;yield return CheckRevolver(game,PlayerController.Instance,"custom-fresh-gameplay.png");yield return AchievementExit(game,false);VerifyCustomResult(ResultDir,points);
  SetPhase("PASS");Log("complete","Early-profile owned Custom modes, purchased-rune and native perk prerequisites, actual shooting/credits-only result verified");Application.Quit(0);
 }
 IEnumerator CheckCustomChallenge()
 {
  RunResume.Invalidate();Progression.State.earned.Add("scenario:Challenge_Forest");Progression.State.challengeDifficultyUnlocked=0;Progression.Refresh();
  Log("fixture-precondition","Isolated ownership explicitly enables Challenge tier1; a normal win would unlock tier2. No natural map-unlock claim.");
  yield return OpenCustomConfiguration();var source=CustomMode.Maps().Single(m=>m.name=="Challenge_Forest");
  Click(CustomButton("CustomLoadDefaults"));Click(CustomButton("CustomAction"+Array.IndexOf(CustomMode.Maps(),source)));
  CustomValue(1,(Progression.State.challengeDifficultyUnlocked+1).ToString());Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>();Click(CustomButton("CustomStart"));
  GameController game;while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
  GameTimer.SharedInstance.Stop();StopFixtureSpawning();PlayerController.Instance.playerHealth.isInvincible.Flip();var run=ChallengeRun.Instance;
  Require(CustomMode.Active&&run!=null&&SelectedMap.MapData.achievementsDisabled,"Custom Challenge uses its real quota scheduler and native disabled-achievement map");
  run.EnemyKills=ChallengeMode.EnemyTargets[run.Tier]-1;run.BossKills=ChallengeMode.BossTargets[run.Tier];
  Log("fixture-precondition","Challenge quotas seeded one real enemy death below victory; no claim of a full unassisted Custom Challenge");
  int points=PointsTracker.pts;var last=ObjectPooler.SharedInstance.GetPooledObject("BrainMonster");last.transform.position=PlayerController.Instance.transform.position+Vector3.right*15;last.GetComponent<Health>().TakeDamage(DamageType.Bullet,1000000);
  Require(run.Completed&&game.CurrentState is KillEnemiesState,"actual final native enemy death completes Custom Challenge through the normal win transition");
  while(!(game.CurrentState is PlayerSurvivedState))yield return null;
  Require(Ownership()==File.ReadAllText(Path.Combine(ResultDir,"custom-ownership.txt")),"native Custom Challenge win leaves Challenge tier and all ordinary progression unchanged");
  yield return Capture("custom-challenge-native-win.png");yield return AchievementExit(game,true);VerifyCustomResult(ResultDir,points);
  SetPhase("PASS");Log("complete","Actual Custom Challenge quota victory, native caller achievement guard, unchanged earned tier/ownership and retained credits verified");Application.Quit(0);
 }
}
