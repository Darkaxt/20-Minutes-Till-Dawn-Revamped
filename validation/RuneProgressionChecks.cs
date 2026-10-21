using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using flanne.UI;
using flanne.RuneSystem;
using Object=UnityEngine.Object;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 RuneRowUI[] RuneRows(RuneTreeUI tree)=>(RuneRowUI[])AccessTools.Field(typeof(RuneTreeUI),"rows").GetValue(tree);
 RuneUnlocker[] RuneEntries(RuneRowUI row)=>(RuneUnlocker[])AccessTools.Field(typeof(RuneRowUI),"runes").GetValue(row);
 RuneUnlocker[] AllRunes(TitleScreenController title)=>new[]{title.swordRuneTree,title.shieldRuneTree}.SelectMany(t=>RuneRows(t)).SelectMany(r=>RuneEntries(r)).ToArray();
 IEnumerator OpenRuneMenu(TitleScreenController title)
 {
  Click(Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||!title.characterMenu.interactable)yield return null;
  Click(title.runesButton);while(!(title.CurrentState is RuneMenuCharacterState)||!title.runeConfirmButton.IsInteractable())yield return null;
 }
 void RunePress(RuneUnlocker rune)
 {
  var button=(Button)AccessTools.Field(typeof(RuneUnlocker),"button").GetValue(rune);button.Select();
  rune.OnSelect(null);AccessTools.Method(typeof(RuneUnlocker),"StartPress").Invoke(rune,null);
 }
 void RuneRelease(RuneUnlocker rune)=>AccessTools.Method(typeof(RuneUnlocker),"ReleasePress").Invoke(rune,null);
 IEnumerator RuneBuy(RuneUnlocker rune,int expectedPrice)
 {
  int rank=rune.level;PointsTracker.pts=expectedPrice;RunePress(rune);
  while(AccessTools.Field(typeof(RuneUnlocker),"_pressAndHoldCoroutine").GetValue(rune)!=null)yield return null;RuneRelease(rune);
  Require(rune.level==rank+1&&PointsTracker.pts==0,"selected native hold buys rank "+(rank+1)+" at exact displayed price "+expectedPrice+": "+rune.rune.data.name);
 }
 IEnumerator RuneReject(RuneUnlocker rune,int credits,string reason)
 {
  int rank=rune.level;PointsTracker.pts=credits;RunePress(rune);
  while(AccessTools.Field(typeof(RuneUnlocker),"_pressAndHoldCoroutine").GetValue(rune)!=null)yield return null;RuneRelease(rune);
  Require(rune.level==rank&&PointsTracker.pts==credits,"purchase rejected without spending: "+reason);
 }
 IEnumerator CheckRuneProgressionUI(TitleScreenController title)
 {
  SetPhase("native-rune-menu-keyboard-and-labels");yield return OpenRuneMenu(title);
  while(title.runeMenuPanel.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  var labels=title.runeMenuPanel.GetComponentsInChildren<TMP_Text>(true);
  Log("native-rune-panel-labels",string.Join(";",labels.Select(t=>t.name+"="+t.text.Replace("\n"," "))));
  for(int i=1;i<=4;i++)Require(labels.Any(t=>t.text=="Tier "+i),"actual rune panel shows revised tier label "+i);
  Require(!labels.Any(t=>new[]{"Rank1+","Rank5+","Rank10+","Rank15+"}.Contains(t.text.Replace(" ",""))),"actual panel contains no obsolete total-rank gate labels");
  var row=RuneEntries(RuneRows(title.swordRuneTree)[0]);Require(row.All(r=>r.level==5&&r.toggleOn),"persisted maximum row starts with all three enabled");
  var target=row[0];((Button)AccessTools.Field(typeof(RuneUnlocker),"button").GetValue(target)).Select();yield return null;
  InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.Enter));yield return null;
  InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());yield return null;
  Require(!target.toggleOn&&row.Skip(1).All(r=>r.toggleOn),"actual native keyboard Submit disables one rune without disabling its neighbors");
  InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.Enter));yield return null;
  InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());yield return null;
  Require(row.All(r=>r.toggleOn),"actual native keyboard Submit enables it alongside the other two");
  var description=title.runeMenuPanel.GetComponentsInChildren<RuneDescription>(true).Single(d=>d.data==target.rune.data);
  Require(((TMP_Text)AccessTools.Field(typeof(RuneDescription),"costTMP").GetValue(description)).text=="MAX","actual native keyboard selection displays maximum rank instead of a purchasable price");
  yield return Capture("rune-final-menu-keyboard.png");Click(title.runeConfirmButton);
  SetPhase("PASS");Log("complete","All24 selections persist in another fresh process; actual native keyboard toggles are independent and all revised tier/MAX labels appear in the actual rune panel");Application.Quit(0);
 }
 [Serializable] class RuneSnapshot{public string[] enabled,names;public int[] levels;public int credits;}
 RuneSnapshot RuneState(TitleScreenController title)=>new RuneSnapshot{enabled=new[]{title.swordRuneTree,title.shieldRuneTree}.SelectMany(t=>t.GetActiveRunes()).Select(r=>r.name).OrderBy(n=>n).ToArray(),names=AllRunes(title).Select(r=>r.rune.data.name).ToArray(),levels=AllRunes(title).Select(r=>r.level).ToArray(),credits=PointsTracker.pts};
 IEnumerator CheckRuneProgression(TitleScreenController title,bool relaunch)
 {
  SetPhase(relaunch?"rune-progression-relaunch":"rune-progression-migration-and-purchases");var all=AllRunes(title);
  Require(title.runeUnlocker.unlockData.unlocks.SequenceEqual(SaveSystem.data.runeUnlocks.unlocks),"native saved rune ranks load without losing purchases");
  var original=RuneState(title);
  File.WriteAllText(Path.Combine(ResultDir,"runes-loaded.json"),JsonUtility.ToJson(original,true));
  if(!relaunch)
  {
   // This profile has never stored multiple selections. Preserve exactly the
   // original saved representative in each row before preparing fixture ranks.
   Require(Progression.State.enabledRunes==null,"old companion journal has no invented multi-selections before migration");
   var expected=new List<string>();
   foreach(var pair in new[]{new{tree=title.swordRuneTree,selections=SaveSystem.data.swordRuneSelections},new{tree=title.shieldRuneTree,selections=SaveSystem.data.shieldRuneSelections}})
    for(int i=0;i<(pair.selections?.Length??0);i++)if(pair.selections[i]>=0)expected.Add(RuneEntries(RuneRows(pair.tree)[i])[pair.selections[i]].rune.data.name);
   Require(original.enabled.SequenceEqual(expected.OrderBy(n=>n)),"legacy single selections migrate exactly before any new purchase");
   yield return OpenRuneMenu(title);
   while(title.runeMenuPanel.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
   foreach(var r in all){r.level=0;r.toggleOn=false;}
   var rows=RuneRows(title.swordRuneTree);var first=RuneEntries(rows[0])[0];var neighbor=RuneEntries(rows[0])[1];var below=RuneEntries(rows[1])[0];var bottom=RuneEntries(rows[2])[0];
   Log("rune-layout",string.Join(";",rows.Select((row,i)=>i+":"+string.Join(",",RuneEntries(row).Select(r=>r.rune.data.name)))));
   yield return RuneReject(below,100000,"unbought preceding rune in same column");
   int price=first.rune.data.costPerLevel;
   for(int rank=1;rank<=5;rank++){Require(first.costPerLevel==price,"next price compounds by20% from previous rounded price");yield return RuneBuy(first,price);price=(int)((price*6L+4)/5);}
   yield return RuneReject(first,100000,"native five-rank maximum");
   Require(RuneProgression.CanUpgrade(below)&&!RuneProgression.CanUpgrade(RuneEntries(rows[1])[1]),"a fully purchased column opens independently of neighboring columns");
   yield return RuneBuy(neighbor,neighbor.rune.data.costPerLevel);
   first.toggleOn=true;neighbor.toggleOn=true;
   Require(first.toggleOn&&neighbor.toggleOn&&title.swordRuneTree.GetActiveRunes().Count==2,"two purchased runes remain enabled in one native row");
   yield return RuneBuy(below,below.rune.data.costPerLevel);
   yield return RuneReject(bottom,100000,"every earlier row in that same column must be fully upgraded");
   var neverBought=RuneEntries(rows[0])[2];yield return RuneReject(neverBought,neverBought.costPerLevel-1,"insufficient credits");
   // Credit changes during a hold must not allow a stale-balance purchase.
   PointsTracker.pts=neverBought.costPerLevel;RunePress(neverBought);PointsTracker.pts=0;
   while(AccessTools.Field(typeof(RuneUnlocker),"_pressAndHoldCoroutine").GetValue(neverBought)!=null)yield return null;
   Require(neverBought.level==0&&PointsTracker.pts==0,"hold completion rechecks the actual credit balance");
   yield return RuneBuy(neverBought,neverBought.costPerLevel);first.toggleOn=true;neighbor.toggleOn=true;
   Require(RuneEntries(rows[0]).All(r=>r.toggleOn),"all three purchased runes in one row can be enabled together");
   var legacy=RuneEntries(rows[1])[1];legacy.level=2;legacy.toggleOn=true;
   yield return RuneReject(legacy,100000,"previously owned lower rune remains usable but needs its ancestors for new ranks");legacy.toggleOn=true;
   Require(title.swordRuneTree.GetActiveRunes().Contains(legacy.rune.data),"previously purchased lower-rank ownership remains usable despite the new prerequisite");
   // The other tree follows the same column rule. Rank4 is an explicitly
   // seeded precondition; this check does not claim four purchases were played.
   var shield=RuneRows(title.shieldRuneTree);var shieldTop=RuneEntries(shield[0])[0];var shieldBelow=RuneEntries(shield[1])[0];shieldTop.level=4;
   yield return RuneReject(shieldBelow,100000,"shield column ancestor at rank4");
   yield return RuneBuy(shieldTop,shieldTop.costPerLevel);yield return RuneBuy(shieldBelow,shieldBelow.costPerLevel);
   var fourthTier=RuneEntries(rows[3])[0];yield return RuneReject(fourthTier,100000,"fourth tier requires all three earlier runes in its column");
   below.level=5;bottom.level=5;yield return RuneBuy(fourthTier,fourthTier.costPerLevel);
   Require(!RuneProgression.CanUpgrade(RuneEntries(rows[3])[1]),"maxing one complete column does not open a neighboring fourth-tier rune");
   // Show the actual remaining column requirement and the next compounded price.
   var button=(Button)AccessTools.Field(typeof(RuneUnlocker),"button").GetValue(legacy);button.Select();yield return null;
   var select=title.runeMenuPanel.GetComponentsInChildren<RuneDescriptionSetOnSelect>(true).Single(s=>((RuneIcon)AccessTools.Field(typeof(RuneDescriptionSetOnSelect),"runeIcon").GetValue(s)).data==legacy.rune.data);select.OnSelect(null);
   var description=(RuneDescription)AccessTools.Field(typeof(RuneDescriptionSetOnSelect),"runeDescription").GetValue(select);
   description.Refresh();var label=(TMP_Text)AccessTools.Field(typeof(RuneDescription),"descriptionTMP").GetValue(description);
   Require(label.text.Contains("fully upgrade")&&label.text.Contains(RuneEntries(rows[0])[1].rune.data.nameString),"focused native description names the missing rune in the same column");
   Require(((TMP_Text)AccessTools.Field(typeof(RuneDescription),"costTMP").GetValue(description)).text==legacy.costPerLevel.ToString(),"native description displays the actual next-rank price");
   Require(rows.SelectMany(r=>r.GetComponentsInChildren<TMP_Text>(true)).All(t=>!t.text.Replace(" ","").StartsWith("Rank5+")&&!t.text.Replace(" ","").StartsWith("Rank10+")&&!t.text.Replace(" ","").StartsWith("Rank15+")),"menu no longer advertises obsolete total-rank row gates");
   yield return Capture("rune-multiple-enabled-and-column-gates.png");
   PointsTracker.pts=12345;Click(title.runeConfirmButton);
   while(!(title.CurrentState is CharacterSelectState)||!title.characterMenu.interactable)yield return null;
   var saved=RuneState(title);
   Require(Progression.State.enabledRunes.OrderBy(n=>n).SequenceEqual(saved.enabled),"native menu save persists every enabled rune in the companion journal");
   Require(SaveSystem.data.swordRuneSelections.All(i=>i>=-1&&i<3)&&SaveSystem.data.shieldRuneSelections.All(i=>i>=-1&&i<3),"original selection arrays retain stock-compatible representative indices");
   Require(saved.enabled.Length>=5,"purchase fixture saved multiple rows and both native trees");
   File.WriteAllText(Path.Combine(ResultDir,"runes-expected.json"),JsonUtility.ToJson(saved,true));
   SetPhase("PASS");Log("complete","Native hold purchases, compounded prices, exact/insufficient/stale balances, independent columns, multiple same-row selections, preserved owned lower runes, visible requirements and native save pass; wallet/rank setup is fixture-only");Application.Quit(0);yield break;
  }
  Require(original.credits==12345&&original.enabled.Length>=5&&Progression.State.enabledRunes.OrderBy(n=>n).SequenceEqual(original.enabled),"fresh process restores the exact wallet and every enabled rune");
  var enabled=new[]{title.swordRuneTree,title.shieldRuneTree}.SelectMany(t=>t.GetActiveRunes()).ToArray();
  yield return AchievementRun("Shana","RevolverData","20M_Forest");var game=Object.FindObjectOfType<GameController>();var player=PlayerController.Instance;
  Require(Loadout.RuneSelection.Select(r=>r.name).OrderBy(n=>n).SequenceEqual(original.enabled),"ordinary native weapon confirmation carries every persisted enabled rune into Loadout");
  var applied=player.GetComponentsInChildren<Rune>();Require(applied.Length==enabled.Length,"native LoadoutInitializer applies every selected rune once");
  foreach(var data in enabled)
  {
   var component=applied.Single(r=>r.name==data.runePrefab.name+"(Clone)");
   Require((int)AccessTools.Field(typeof(Rune),"level").GetValue(component)==data.level,"native runtime rank matches purchased rank: "+data.name);
  }
  var bonuses=new Dictionary<StatType,float>();
  foreach(var data in enabled.Where(d=>d.runePrefab is StatRune))
   foreach(var change in (StatChange[])AccessTools.Field(typeof(StatRune),"statChanges").GetValue(data.runePrefab))
    if(!change.isFlatMod&&change.value>0){if(!bonuses.ContainsKey(change.type))bonuses[change.type]=0;bonuses[change.type]+=data.level*change.value;}
  foreach(var pair in bonuses)Require(Bonus(player.stats[pair.Key])>=pair.Value-.0001f,"native combined rune stat reaches combat: "+pair.Key+" "+pair.Value);
  yield return CheckRevolver(game,player,"rune-multiple-native-combat.png");yield return AchievementExit(game,false);
  title=Object.FindObjectOfType<TitleScreenController>();Require(RuneState(title).enabled.SequenceEqual(original.enabled),"enabled selections survive combat exit and native title reload");
  // Fully purchased ranks here are a fixture precondition, not a claim that
  // the entire economy was played. Exercise the maximum combined native path.
  foreach(var rune in AllRunes(title)){rune.level=5;rune.toggleOn=true;}title.Save();
  yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;
  Require(Loadout.RuneSelection.Count==24&&player.GetComponentsInChildren<Rune>().Length==24,"full native loadout applies all24 enabled runes, including three in every row");
  while(player.GetComponentInChildren<PreventDamage>()==null)yield return null;
  Require(player.GetComponentsInChildren<SetGameObjectActiveSummon>().Count(w=>w.SummonTypeID=="SeismicWard")==2,"full combined build includes exactly two native Seismic Wards");
  Require(Mathf.Abs(Bonus(game.playerXP.xpMultiplier)-.2f)<.0001f,"full combined build retains the corrected rank5 Growth XP bonus");
  yield return CheckRevolver(game,player,"rune-all24-native-combat.png");yield return AchievementExit(game,false);
  title=Object.FindObjectOfType<TitleScreenController>();Require(RuneState(title).enabled.Length==24,"full24 selection survives native combat exit/title reload");
  SetPhase("PASS");Log("complete","Fresh-process purchases/multiple selections, ordinary native loadout and combat stats, full24-rune combined play, original Revolver shooting and native title reload pass; full rank5 grid is a fixture precondition");Application.Quit(0);
 }
 IEnumerator CheckRuneProgressionBaseline(TitleScreenController title)
 {
  SetPhase("rune-progression-before-change");var all=AllRunes(title);var levels=all.Select(r=>r.level).ToArray();var selected=all.Select(r=>r.toggleOn).ToArray();int points=PointsTracker.pts;
  yield return OpenRuneMenu(title);foreach(var r in all)r.level=0;
  var entries=RuneEntries(RuneRows(title.swordRuneTree)[0]);var first=entries[0];var second=entries[1];
  PointsTracker.pts=100000;int cost=first.costPerLevel;RunePress(first);while(first.level==0)yield return null;RuneRelease(first);
  int afterFirst=PointsTracker.pts;RunePress(first);while(first.level==1)yield return null;RuneRelease(first);
  Require(afterFirst-PointsTracker.pts==cost&&first.costPerLevel==cost,"baseline next rank still uses the same flat native price");
  RunePress(second);while(second.level==0)yield return null;RuneRelease(second);first.toggleOn=true;second.toggleOn=true;
  Require(!first.toggleOn&&second.toggleOn&&title.swordRuneTree.GetActiveRunes().Count==1,"baseline native toggle group permits only one rune in a row");
  yield return Capture("rune-progression-baseline.png");
  for(int i=0;i<all.Length;i++){all[i].level=levels[i];all[i].toggleOn=false;}
  for(int i=0;i<all.Length;i++)all[i].toggleOn=selected[i];PointsTracker.pts=points;
  Click(title.runeConfirmButton);SetPhase("PASS");Log("complete","Baseline native hold purchases reproduce flat prices and mutually exclusive same-row runes; isolated input levels/selections/credits restored");Application.Quit(0);
 }
}
