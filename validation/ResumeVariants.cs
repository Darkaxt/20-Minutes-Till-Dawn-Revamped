using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
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
 void PrepareRunRuneRanks()
 {
  var selection=Loadout.RuneSelection.ToList();int index=selection.FindIndex(r=>r.name=="GrowthRuneData");
  Require(index>=0&&selection[index].level==5,"isolated title has a fully purchased Growth rune before the run-local rank fixture");
  var copy=Object.Instantiate(selection[index]);copy.name=selection[index].name;copy.level=2;selection[index]=copy;Loadout.RuneSelection=selection;
  Log("fixture-precondition","Run-local Growth rank2 with title-owned rank5; title purchases are not changed");
 }
 void PrepareEndlessCycle()
 {
  var map=SelectedMap.MapData;var initializer=Object.FindObjectOfType<MapInitializer>();var horde=Object.FindObjectOfType<HordeSpawner>();var boss=Object.FindObjectOfType<BossSpawner>();
  initializer.StopAllCoroutines();var native=(IEnumerator)AccessTools.Method(typeof(MapInitializer),"EndlessLoop").Invoke(initializer,new object[]{map});
  Require(native.MoveNext()&&native.Current is WaitForSeconds,"native Endless setup yields its original initial-round wait");
  // Explicit late-run precondition: skip only that native wait, clearing the
  // initial fixture's pending schedules before executing the actual cycle body.
  horde.StopAllCoroutines();boss.StopAllCoroutines();((List<SpawnSession>)AccessTools.Field(typeof(HordeSpawner),"activeSpawners").GetValue(horde)).Clear();
  AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(GameTimer.SharedInstance,1200f);
  Require(native.MoveNext()&&native.Current is WaitForSeconds,"actual native first Endless cycle applies its schedules/scaling");
  var recorder=Object.FindObjectOfType<GameController>().GetComponent(AccessTools.TypeByName("TwentyMinutes.Modernization.RunRecorder"));
  Require((int)AccessTools.Field(recorder.GetType(),"EndlessCycle").GetValue(recorder)==1&&horde.healthMultiplier==2&&boss.healthMultiplier==4,"explicit native first-cycle identity and unchanged Windows scaling");
  Log("fixture-precondition","Initial 1200-second wait explicitly skipped; subsequent state is produced by the real native EndlessLoop body, not an unassisted run");
 }
 IEnumerator VerifyEndlessCycle(RunResume.Snapshot saved)
 {
  var horde=Object.FindObjectOfType<HordeSpawner>();var boss=Object.FindObjectOfType<BossSpawner>();
  Require(saved.endlessCycle==1&&Mathf.Abs(horde.healthMultiplier-saved.hordeModifiers[1])<.001f&&Mathf.Abs(boss.healthMultiplier-saved.bossModifiers[0])<.001f,"resume preserves native first-cycle enemy/boss scaling without applying Darkness twice");
  float start=Time.time,remaining=SelectedMap.MapData.endlessLoopTime-saved.roundElapsed;Time.timeScale=100;
  var recorder=Object.FindObjectOfType<GameController>().GetComponent(AccessTools.TypeByName("TwentyMinutes.Modernization.RunRecorder"));
  while((int)AccessTools.Field(recorder.GetType(),"EndlessCycle").GetValue(recorder)==1)yield return null;
  Time.timeScale=1;
  Require(Time.time-start>=remaining-1&&horde.healthMultiplier==9&&horde.eliteHealthMultiplier==5&&boss.healthMultiplier==7,"the next actual restored loop fires at its saved remaining deadline and uses native cycle2 cubic/quadratic scaling");
 }
 IEnumerator SelectResumeEvolution(GameController game)
 {
  Log("fixture-precondition","Seed level19/zero XP; actual native XP gain opens the level20 evolution menu");
  AccessTools.Field(typeof(flanne.Player.PlayerXP),"<level>k__BackingField").SetValue(game.playerXP,19);AccessTools.Field(typeof(flanne.Player.PlayerXP),"xp").SetValue(game.playerXP,0f);
  game.playerXP.GainXP((float)AccessTools.Property(typeof(flanne.Player.PlayerXP),"xpToLevel").GetValue(game.playerXP)+1);
  while(!(game.CurrentState is GunEvoMenuState)||!(AccessTools.Field(typeof(flanne.UI.Menu),"ClickEvent").GetValue(game.gunEvoMenu) is Delegate))yield return null;
  var choice=game.gunEvoMenu.GetEntry(0);string key=RunResume.Key(choice.GetComponent<flanne.UI.GunEvoUI>().data);
  Click(choice.GetComponent<Button>());while(!(game.CurrentState is CombatState))yield return null;
  var recorder=game.GetComponent(AccessTools.TypeByName("TwentyMinutes.Modernization.RunRecorder"));
  var acquisitions=(List<RunResume.Acquisition>)AccessTools.Field(recorder.GetType(),"Acquired").GetValue(recorder);
  Require(acquisitions.Any(a=>a.evolution&&a.key==key),"actual native evolution is recorded in acquisition order");
 }
 IEnumerator CheckInvalidResume(TitleScreenController title)
 {
  SetPhase("resume-invalid-and-new");
  var fixture=Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR");
  if(!string.IsNullOrEmpty(fixture))
  {
   RunResume.Snapshot saved;using(var input=File.OpenRead(Path.Combine(fixture,"saved-run.json")))saved=(RunResume.Snapshot)new DataContractJsonSerializer(typeof(RunResume.Snapshot)).ReadObject(input);
   string character=saved.character;
   for(int variant=0;variant<3;variant++)
   {
    saved.custom.version=variant==0?99:1;saved.character=variant==2?"Revamped_Haruka":character;
    if(variant==1)saved.custom.values.Add(new CustomMode.Value{key="unknown-custom-rule",value=1});
    if(variant==2)saved.custom.values.RemoveAll(v=>v.key=="unknown-custom-rule");
    using(var output=File.Create(RunCheckpoint))new DataContractJsonSerializer(typeof(RunResume.Snapshot)).WriteObject(output,saved);
    string preserved=File.ReadAllText(RunCheckpoint);Click(Entries(title.mainMenu)[0].button);yield return null;
    var resume=Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy);
    if(variant==2){Require(resume.IsInteractable(),"structurally valid Custom checkpoint reaches ownership validation");Click(resume);}
    Require(!resume.IsInteractable()&&File.ReadAllText(RunCheckpoint)==preserved&&Object.FindObjectOfType<GameController>()==null,"unknown Custom schema/rules or unowned content is rejected without loading battle or replacing the checkpoint");
    Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeBack"&&b.gameObject.activeInHierarchy));yield return null;
   }
  }
  foreach(string malformed in new[]{"not-json","{\"version\":999,\"valid\":true}","{\"version\":3,\"valid\":true}"})
  {
   File.WriteAllText(RunCheckpoint,malformed);Click(Entries(title.mainMenu)[0].button);yield return null;
   var resume=Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy);
   var back=Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeBack"&&b.gameObject.activeInHierarchy);
   Require(!resume.IsInteractable()&&File.ReadAllText(RunCheckpoint)==malformed,"malformed/unknown/incomplete checkpoint disables Continue and preserves the exact file");
   Click(back);yield return null;Require(title.mainMenu.interactable,"Back from an invalid checkpoint restores native menu input");
  }
  Click(Entries(title.mainMenu)[0].button);yield return null;
  var fresh=Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeNewRun"&&b.gameObject.activeInHierarchy);Click(fresh);
  while(!Active(title.characterMenu))yield return null;
  Require(RunResume.Read()==null,"explicit New run invalidates an unusable checkpoint through the real native prompt");
  Click(title.loadoutBackButton);while(!Active(title.mainMenu))yield return null;
  yield return AchievementRun("Shana","RevolverData","20M_Forest");var game=Object.FindObjectOfType<GameController>();
  Require(game.CurrentState is CombatState&&game.playerXP.level==1,"ordinary native new run works after explicit discard of a corrupt checkpoint");
  yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete","Malformed, unsupported and incomplete checkpoints preserved, Continue disabled, Back restored input, explicit New discards and native play/exit works");Application.Quit(0);
 }
}
