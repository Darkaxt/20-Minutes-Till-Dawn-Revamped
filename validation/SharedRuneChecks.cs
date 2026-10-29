using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Runtime.Serialization.Json;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using flanne;
using flanne.Core;
using flanne.UI;
using flanne.RuneSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 const string SharedRune="PowerRuneData";internal static bool SharedRuneRoll;
 static System.Random SharedRuneRandom(int original)
 {
  if(!SharedRuneRoll)return new System.Random(original);
  for(int seed=0;;seed++)if(new System.Random(seed).Next(6)==0)return new System.Random(seed);
 }
 static int SharedFloor()=> (int)AccessTools.Method(typeof(RuneProgression),"SharedMinimum").Invoke(null,new object[]{SharedRune});
 static RunResume.RuneRank SharedRank(RuneProfile profile,string name=SharedRune)=>profile.ranks.Single(r=>r.name==name);
 static RuneData SharedDefinition()=> (RuneData)AccessTools.Method(typeof(RuneProgression),"Definition").Invoke(null,new object[]{SharedRune});
 void SharedDescriptionBounds(RuneDescription description)
 {
  var text=(TMP_Text)WindField(description,"descriptionTMP");Canvas.ForceUpdateCanvases();text.ForceMeshUpdate();var panel=(RectTransform)description.transform;
  Log("shared-tooltip-frame",description.data.name+" frame="+panel.rect+" textAnchor="+text.rectTransform.rect+" rendered="+text.textBounds);
  Require(text.textInfo.characterInfo.Take(text.textInfo.characterCount).Where(c=>c.isVisible).All(c=>panel.rect.Contains(panel.InverseTransformPoint(text.transform.TransformPoint(c.bottomLeft)))&&panel.rect.Contains(panel.InverseTransformPoint(text.transform.TransformPoint(c.topRight)))),"complete rendered rune description stays inside its native frame: "+description.data.name);
 }
 string SharedProfiles()=>string.Join("|",Progression.State.characterRunes.OrderBy(p=>p.character).Select(p=>p.character+":"+string.Join(",",p.ranks.OrderBy(r=>r.name).Select(r=>r.name+"="+r.rank))+"/"+string.Join(",",p.enabled.OrderBy(n=>n))));
 void SharedExpectedProfiles()=>Require(SharedProfiles()==File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR"),"shared-profiles.txt")),"reload preserves exact permanent ranks and each character's enabled choices without additive grants");
 void SharedKill(GameController game)
 {
  RuneSpawnBoss(0);SharedRuneRoll=true;try{RuneDefeatBoss(game,0);}finally{SharedRuneRoll=false;}
 }
 IEnumerator SharedBattle(string character,int rank)
 {
  RunResume.Invalidate();Progression.State.earned.Add("character:"+character);Progression.State.earned.Add("scenario:10M_Forest");Progression.Refresh();
  yield return AchievementRun(character,"RevolverData","10M_Forest",prepareRun:()=>
  {
   var profile=RuneProgression.Profile(character);profile.enabled=new List<string>{SharedRune};var data=SharedDefinition();data.level=rank;Loadout.RuneSelection=new List<RuneData>{data};
  });
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();Require(Loadout.RuneSelection.Single().name==SharedRune&&Loadout.RuneSelection.Single().level==rank,"real native battle starts with the selected character's rank "+rank);
 }
 IEnumerator SharedMenu()
 {
  var title=Progression.Title;var pad=InputSystem.AddDevice<Gamepad>("SharedRuneController");InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
  yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  yield return MenuMove(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,title.runesButton);yield return SelectorPad(pad,GamepadButton.South);while(title.runeMenuPanel.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  var rune=AllRunes(title).Single(r=>r.rune.data.name==SharedRune);var button=(Button)WindField(rune,"button");yield return MenuMove(pad,title.runeMenuPanel.transform,button);
  bool selected=rune.toggleOn;string character=title.characterMenu[(int)WindField(title.characterMenu,"index")].name;yield return SelectorPad(pad,GamepadButton.South);Require(rune.toggleOn!=selected&&rune.level==RuneProgression.Rank(character,SharedRune),"controller still toggles a rune without changing its rank");yield return SelectorPad(pad,GamepadButton.South);Require(rune.toggleOn==selected,"independent enabled choice is restored");
  foreach(var candidate in AllRunes(title)){yield return MenuMove(pad,title.runeMenuPanel.transform,(Button)WindField(candidate,"button"));var description=title.runeMenuPanel.GetComponentsInChildren<RuneDescription>().Single(d=>d.data?.name==candidate.rune.data.name);SharedDescriptionBounds(description);}
  yield return MenuMove(pad,title.runeMenuPanel.transform,button);RuneProgression.RefreshDescriptions();var selectedDescription=title.runeMenuPanel.GetComponentsInChildren<RuneDescription>().Single(d=>d.data?.name==SharedRune);var text=(TMP_Text)WindField(selectedDescription,"descriptionTMP");Require(text.text.Contains("Shared minimum: 4 / 4"),"native rune description explains the roster's actual shared minimum");
  foreach(var size in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})foreach(bool reborn in new[]{false,true})
  {Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);if(ColorfulStyle.Instance.Enabled!=reborn)ColorfulStyle.Instance.Toggle();yield return null;SharedDescriptionBounds(selectedDescription);yield return Capture("shared-runes-menu-"+size.x+"-"+(reborn?"reborn":"original")+".png");}
  title.Save();Click(title.runeConfirmButton);while(!Active(title.characterMenu)||!title.characterMenu.interactable)yield return null;
 }
 IEnumerator CheckSharedRunes(string mode)
 {
  if(mode=="shared-runes-inspect"){yield return SharedMenu();SetPhase("PASS");Log("complete","Diagnostic tooltip/controller layout only; not feature acceptance");Application.Quit(0);yield break;}
  if(mode=="shared-runes-migrate")
  {
   Require(SharedFloor()==4&&RuneProgression.Rank("Hastur",SharedRune)==4,"existing rank-five characters retroactively raise a pre-feature rank-zero recipient to four");SharedExpectedProfiles();yield return SharedMenu();SetPhase("PASS");Log("complete","Retroactive shared minimum, preserved higher/partial ranks and selections, native controller description across viewports/styles; no new save schema/reset");Application.Quit(0);yield break;
  }
  if(mode!="shared-runes-fresh")
  {
   SharedExpectedProfiles();yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
   Require(SharedFloor()==4&&RuneProgression.Rank("Katana",SharedRune)==4&&RuneProgression.Rank("Abby",SharedRune)==4,"persisted five masters retain the capped shared minimum without granting personal fives");
   if(mode=="shared-runes-prior")
   {Require(Loadout.RuneSelection.Count==0,"public1.3.4 Continue retains its saved empty battle loadout despite permanent shared ranks");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Public1.3.4 Continue preserves its original loadout and current character progression");Application.Quit(0);yield break;}
   Require(Loadout.RuneSelection.Single().level==4&&RuneProgression.Rank("Luna",SharedRune)==5,"Continue preserves the battle-start rank while retaining its personal fifth rank");
   string before=SharedProfiles();var run=RuneRewardRun.Instance;
   if(mode=="shared-runes-replay"){Require(run.State.rewards.Count==0,"replay uses the genuine pre-boss checkpoint");SharedKill(game);yield return RunePopup(game);Require(run.State.rewards.Single().draws.Single().rank==5,"the journal reuses the committed mastery result without rerolling");yield return RuneConfirm(game);while(!(game.CurrentState is CombatState))yield return null;}
   ElementCall(run,"BossKilled",0);Require(SharedProfiles()==before,"replayed/duplicate boss identities cannot compound shared or personal ranks");
   float priorDamage=Bonus(PlayerController.Instance.stats[StatType.BulletDamage]);PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return null;Click(game.giveupButton);while(!(game.CurrentState is PlayerDeadState)||!game.retryRunButton.IsInteractable())yield return null;int previous=game.GetInstanceID();Click(game.retryRunButton);
   while((game=Object.FindObjectOfType<GameController>())==null||game.GetInstanceID()==previous||!(game.CurrentState is CombatState))yield return null;
   StopFixtureSpawning();GameTimer.SharedInstance.Stop();Require(Loadout.RuneSelection.Single().level==5,"actual death/Retry starts the next battle with the current personal fifth rank");CombatClose(Bonus(PlayerController.Instance.stats[StatType.BulletDamage]),priorDamage+.02f,"the personal fifth Power rank reaches the actual next battle's damage stat");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Exact current or pre-boss Continue, permanent/battle ownership, journal replay, duplicate boss rejection and actual Retry");Application.Quit(0);yield break;
  }
  Log("fixture-precondition","Reset only the isolated validation journal's rune profiles/seed. Four characters have controlled prior rank-four progress; actual scheduled Quickplay boss deaths complete one deterministic Power draw each. The validation-only Random-construction hook controls the outcome, without replacing grants, persistence, menus or game rules.");
  var state=Progression.State;state.characterRunes=new List<RuneProfile>();state.runeSeed=AllRunes(Progression.Title).Select(r=>new RunResume.RuneRank{name=r.rune.data.name,rank=0}).ToList();state.runeSeedEnabled=new List<string>();AccessTools.Method(typeof(RuneProgression),"Initialize").Invoke(null,null);
  var partial=RuneProgression.Profile("Abby");SharedRank(partial).rank=4;partial.enabled.Add(SharedRune);var low=RuneProgression.Profile("Luna");SharedRank(low,"AlacrityRuneData").rank=2;low.enabled.Add("AlacrityRuneData");
  var masters=new[]{"Shana","Diamond","Scarlett","Hina"};
  for(int i=0;i<masters.Length;i++)
  {
   SharedRank(RuneProgression.Profile(masters[i])).rank=4;yield return SharedBattle(masters[i],4);var game=Object.FindObjectOfType<GameController>();var run=RuneRewardRun.Instance;float damage=Bonus(PlayerController.Instance.stats[StatType.BulletDamage]);
   SharedKill(game);yield return RunePopup(game);Require(run.State.rewards.Single().draws.Single().rune==SharedRune&&run.State.rewards[0].draws[0].rank==5,"real boss lottery completes the controlled personal fifth rank");
   Require(RuneProgression.Rank("Luna",SharedRune)==i+1,"mastery raises the low recipient to shared minimum "+(i+1));
   Require(RuneProgression.Rank("Abby",SharedRune)==4,"shared rank never adds onto a recipient's existing rank four or creates a cascade");Require(SharedFloor()==i+1,"each distinct character contributes once to this rune");
   Require(low.enabled.SequenceEqual(new[]{"AlacrityRuneData"})&&SharedRank(low,"AlacrityRuneData").rank==2&&partial.enabled.Contains(SharedRune),"shared ranks preserve existing enabled choices and unrelated rune progress");
   Require(Loadout.RuneSelection.Single().level==4,"mastery does not replace the active battle rune");CombatClose(Bonus(PlayerController.Instance.stats[StatType.BulletDamage]),damage,"mastery leaves the current battle's actual damage bonus unchanged");
   string before=SharedProfiles();ElementCall(run,"BossKilled",0);Require(SharedProfiles()==before,"duplicate boss callback grants no extra shared minimum");yield return RuneConfirm(game);while(!(game.CurrentState is CombatState))yield return null;yield return AchievementExit(game,false);
  }
  Require(RuneProgression.Rank("Katana",SharedRune)==4&&RuneProgression.Profile("Katana").enabled.Count==0,"a character profile first created after four masters receives the head start with its own selections");
  yield return SharedBattle("Shana",5);var cappedGame=Object.FindObjectOfType<GameController>();SharedKill(cappedGame);yield return RunePopup(cappedGame);Require(RuneRewardRun.Instance.State.rewards.Single().draws.Single().capped,"a personally maxed rune keeps the original capped-draw rule");yield return RuneConfirm(cappedGame);while(!(cappedGame.CurrentState is CombatState)||RuneRewardRun.Instance.State.xpLevels>0)yield return null;
  CombatClose((float)WindField(cappedGame.playerXP,"xp")/AndroidXPRequirement(cappedGame.playerXP.level),.5f,"capped row-one draw still converts to exactly half a level");yield return AchievementExit(cappedGame,false);
  yield return SharedBattle("Luna",4);var final=Object.FindObjectOfType<GameController>();var reward=RuneRewardRun.Instance;ElementCall(reward,"Ensure");Progression.Save();RunResume.Save();File.Copy(RunResume.PathName,Path.Combine(ResultDir,"pre-boss-run.json"),true);SharedKill(final);yield return RunePopup(final);
  Require(RuneProgression.Rank("Luna",SharedRune)==5&&RuneProgression.Rank("Abby",SharedRune)==4&&RuneProgression.Rank("Katana",SharedRune)==4&&SharedFloor()==4,"fifth character earns its own final rank without raising anyone above four");yield return RuneConfirm(final);while(!(final.CurrentState is CombatState))yield return null;RunResume.Save();File.Copy(RunResume.PathName,Path.Combine(ResultDir,"saved-run.json"),true);yield return AchievementExit(final,false);
  RuneProgression.Profile("Hastur");Progression.Save();yield return SharedMenu();File.WriteAllText(Path.Combine(ResultDir,"shared-profiles.txt"),SharedProfiles());
  var serializer=new DataContractJsonSerializer(typeof(Progression.Journal));Progression.Journal legacy;using(var stream=File.OpenRead(Progression.JournalPath))legacy=(Progression.Journal)serializer.ReadObject(stream);SharedRank(legacy.characterRunes.Single(p=>p.character=="Hastur")).rank=0;using(var stream=File.Create(Path.Combine(ResultDir,"legacy-journal.json")))serializer.WriteObject(stream,legacy);
  SetPhase("PASS");Log("complete","Actual four-character boss mastery to shared minima1–4, partial/new profile preservation, current-run damage/rank ownership, fifth personal completion, capped XP, controller menu and current/pre-boss/legacy checkpoints");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(RuneRewardRun),"BossKilled")]
class SharedRuneLotteryFixture
{
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
 {
  var constructor=AccessTools.Constructor(typeof(System.Random),new[]{typeof(int)});var replacement=AccessTools.Method(typeof(RuntimeChecks),"SharedRuneRandom");
  foreach(var instruction in source){if(instruction.opcode==OpCodes.Newobj&&Equals(instruction.operand,constructor))yield return new CodeInstruction(OpCodes.Call,replacement);else yield return instruction;}
 }
}
