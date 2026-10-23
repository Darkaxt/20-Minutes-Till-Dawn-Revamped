using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 static int GeneratorCount(AndroidGenerator owner)=>(int)WindField(owner.GetComponent<BuffPlayerStats>(),"_stacks");
 static void GeneratorClock(float value)=>Plugin.SetField(GameTimer.SharedInstance,"<timer>k__BackingField",value);
 IEnumerator CheckAndroidGenerator(bool continuation)
 {
  GameController game;PlayerController player;float baseline;
  if(continuation)
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;
   Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))
   {
    var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));
    if(error!=null)throw new InvalidOperationException(error.text);yield return null;
   }
   player=PlayerController.Instance;baseline=float.Parse(File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR"),"generator-baseline.txt")),CultureInfo.InvariantCulture);
  }
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;StopFixtureSpawning();
   yield return AndroidPick(game,"Generator",game.playerXP.level);baseline=Bonus(player.stats[StatType.FireRate]);
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  var owners=player.GetComponentsInChildren<AndroidGenerator>();while(owners.Any(g=>WindField(g,"buff")==null))yield return null;
  Require(owners.Length==(continuation?2:1)&&owners.All(g=>g.GetComponent<BuffPlayerStats>()!=null),"each acquired source Generator attaches its paired event/deadline and stat-counter owners");
  Log("fixture-precondition","Actual ammo notifications and source Generator Update are exercised; stopped source game-clock values explicitly isolate deadline equality/expiry. No natural encounter or wall-time scheduling claim.");
  if(continuation)
  {
   Require(owners.Select(GeneratorCount).SequenceEqual(new[]{6,1})&&owners.All(g=>!g.Applied),"Continue restores source buff counts6/1 while omitting Generator active flags and deadlines");
   GeneratorRate(player,baseline+2.45f,"source counted replay applies seven35-percent recipes, retaining Android's count/contribution mismatch");
   GeneratorClock(GameTimer.SharedInstance.timer+20);yield return null;
   GeneratorRate(player,baseline+2.45f,"restored count contributions have no invented expiry while unsaved Generator flags are inactive");
   player.ammo.UseAmmo(1);Require(owners.Select(GeneratorCount).SequenceEqual(new[]{7,2})&&owners.All(g=>g.Applied),"the first actual continued consumption applies each owner after the restored-ammo notification was omitted before Start");
   GeneratorRate(player,baseline+3.15f,"first continued ammo change adds one35-percent contribution per source owner");
   player.ammo.GainAmmo(1);Require(owners.Select(GeneratorCount).SequenceEqual(new[]{8,3}),"active source refresh increments each saved counter without decrementing it during removal");
   GeneratorRate(player,baseline+3.15f,"active repeated-owner refresh removes and reapplies only one contribution each");
   GeneratorClock(owners[0].AppliedAt+2);yield return null;Require(owners.All(g=>g.Applied),"continued source Generator keeps contributions at exact two-second equality");
   GeneratorClock(owners[0].AppliedAt+2.01f);while(owners.Any(g=>g.Applied))yield return null;
   GeneratorRate(player,baseline+2.45f,"continued expiry removes one contribution per owner while preserving source restored-count leftovers");
   Require(owners.Select(GeneratorCount).SequenceEqual(new[]{8,3}),"expiry does not normalize Android's nondecremented buff counters");
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;
   Require(player.GetComponentsInChildren<AndroidGenerator>().Length==0,"original restart clears source Generator owners");GeneratorRate(player,0,"original restart clears all acquired and replayed Generator stat contributions");
   yield return CheckRevolver(game,player,"android-generator-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Source Generator paired-count replay, omitted active phases, actual ammo-change refresh/expiry quirks and original restart");Application.Quit(0);yield break;
  }
  Require(GeneratorCount(owners[0])==0&&!owners[0].Applied,"fresh Generator starts without a fabricated stack or active contribution");
  player.ammo.Reload();Require(GeneratorCount(owners[0])==1&&owners[0].Applied,"actual reload amount notification activates Generator");GeneratorRate(player,baseline+.35f,"reload applies source35-percent Fire Rate");
  player.ammo.UseAmmo(1);Require(GeneratorCount(owners[0])==2,"ammo consumption also refreshes source Generator");GeneratorRate(player,baseline+.35f,"consumption refresh retains one live contribution");
  player.ammo.GainAmmo(1);Require(GeneratorCount(owners[0])==3,"actual ammo gain refreshes source Generator");
  player.ammo.GainAmmo(1000);Require(GeneratorCount(owners[0])==4&&player.ammo.amount==player.ammo.max,"full clamped ammo-change notifications still refresh source Generator");GeneratorRate(player,baseline+.35f,"clamped refresh increases its counter while keeping one live contribution");
  GeneratorClock(owners[0].AppliedAt+2);yield return null;Require(owners[0].Applied,"source expiry is strict and excludes exact two-second equality");
  GeneratorClock(owners[0].AppliedAt+2.01f);while(owners[0].Applied)yield return null;GeneratorRate(player,baseline,"fresh expiry removes the one actual contribution");Require(GeneratorCount(owners[0])==4,"expiry leaves Android's application count unchanged");
  player.ammo.UseAmmo(1);Require(GeneratorCount(owners[0])==5&&owners[0].Applied,"consumption after expiry reapplies the full source buff");
  yield return AndroidPick(game,"Generator",game.playerXP.level);owners=player.GetComponentsInChildren<AndroidGenerator>();while(owners.Any(g=>WindField(g,"buff")==null))yield return null;
  Require(owners.Length==2,"repeat acquisition creates another complete source Generator pair");
  player.ammo.GainAmmo(1);Require(owners.Select(GeneratorCount).SequenceEqual(new[]{6,1})&&owners.All(g=>g.Applied),"one real amount-change notification reaches both independent source Generator owners");GeneratorRate(player,baseline+.7f,"repeated source Generator owners each contribute35 percent");
  File.WriteAllText(Path.Combine(ResultDir,"generator-baseline.txt"),baseline.ToString("R",CultureInfo.InvariantCulture));
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Source Generator real reload/consumption/gain/clamped-change events, strict expiry, paired repeats and count-mismatch checkpoint");Application.Quit(0);
 }
}
