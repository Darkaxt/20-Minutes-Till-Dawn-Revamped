using System;
using System.Linq;
using System.Collections;
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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 RuneRowUI[] RuneRows(RuneTreeUI tree)=>RuneProgression.Rows(tree);
 RuneUnlocker[] RuneEntries(RuneRowUI row)=>RuneProgression.Entries(row);
 RuneUnlocker[] AllRunes(TitleScreenController title)=>new[]{title.swordRuneTree,title.shieldRuneTree}.SelectMany(RuneRows).SelectMany(RuneEntries).ToArray();
 IEnumerator OpenRuneMenu(TitleScreenController title)
 {Click(Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||!title.characterMenu.interactable)yield return null;Click(title.runesButton);while(!(title.CurrentState is RuneMenuCharacterState)||!title.runeConfirmButton.IsInteractable())yield return null;}
 void RunePress(RuneUnlocker rune){((Button)WindField(rune,"button")).Select();rune.OnSelect(null);AccessTools.Method(typeof(RuneUnlocker),"StartPress").Invoke(rune,null);}
 void RuneRelease(RuneUnlocker rune)=>AccessTools.Method(typeof(RuneUnlocker),"ReleasePress").Invoke(rune,null);
 IEnumerator CheckRuneProgressionBaseline(TitleScreenController title){yield return CheckRuneProgressionUI(title);}
 IEnumerator CheckRuneProgression(TitleScreenController title,bool relaunch){yield return CheckRuneProgressionUI(title);}
 IEnumerator CheckRuneProgressionUI(TitleScreenController title)
 {
  SetPhase("character-runes-native-menu");RunResume.Invalidate();yield return OpenRuneMenu(title);
  while(title.runeMenuPanel.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  var all=AllRunes(title);var profile=RuneProgression.Profile(title.characterMenu[(int)WindField(title.characterMenu,"index")].name);int credits=PointsTracker.pts;
  foreach(var value in profile.ranks)value.rank=0;
  var rank=profile.ranks.First();rank.rank=1;var neighborRank=profile.ranks[1];neighborRank.rank=1;profile.enabled=new System.Collections.Generic.List<string>{rank.name,neighborRank.name};
  // Reopen the actual character projection after preparing the fixture rank.
  typeof(RuneProgression).GetField("projected",System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic).SetValue(null,null);
  AccessTools.Method(typeof(RuneProgression),"Project").Invoke(null,null);
  var target=all.Single(r=>r.rune.data.name==rank.name);bool enabled=target.toggleOn;
  RunePress(target);RuneRelease(target);yield return null;
  Require(target.toggleOn!=enabled&&target.level==1&&PointsTracker.pts==credits,"native rune Submit toggles without purchasing or spending");
  var pad=InputSystem.AddDevice<Gamepad>("CharacterRuneController");yield return SelectorPad(pad,GamepadButton.South);
  Require(target.toggleOn==enabled&&target.level==1&&PointsTracker.pts==credits,"actual controller Submit toggles an earned rune back without purchasing");
  Require(all.Single(r=>r.rune.data.name==neighborRank.name).toggleOn,"another earned rune in the same row stays enabled");
  var selectedRunes=new[]{title.swordRuneTree,title.shieldRuneTree}.SelectMany(tree=>tree.GetActiveRunes()).ToArray();
  Require(selectedRunes.Length==2&&selectedRunes.All(r=>r.level==profile.ranks.Single(v=>v.name==r.name).rank),"native battle selection transfers each character's enabled earned rank into RuneData");
  AccessTools.Method(typeof(RuneUnlocker),"LevelUp").Invoke(target,null);
  Require(target.level==1&&PointsTracker.pts==credits,"native hold completion cannot buy a rank");
  Require(all.All(r=>!r.locked)&&all.GroupBy(r=>r.rune.data.name).Count()==24,"all 24 native entries remain inspectable without column prerequisites");
  RuneProgression.RefreshDescriptions();var costs=title.runeMenuPanel.GetComponentsInChildren<RuneDescription>(true).Where(d=>d.data!=null).Select(d=>(TMP_Text)WindField(d,"costTMP"));
  Require(costs.All(c=>c.text==""),"no purchase price remains in actual descriptions");
  foreach(var cost in costs)Log("rune-cost-hierarchy",cost.name+" parent="+cost.transform.parent.name+" children="+string.Join(",",cost.transform.parent.GetComponentsInChildren<Transform>(true).Select(t=>t.name)));
  title.Save();yield return Capture("character-rune-rewards-menu.png");
  Click(title.runeConfirmButton);while(!Active(title.characterMenu)||!title.characterMenu.interactable)yield return null;
  var other=RuneProgression.Profile(title.characterMenu[1].name);foreach(var value in other.ranks)value.rank=0;other.enabled.Clear();
  yield return MenuMove(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,Entries(title.characterMenu)[1].button);
  yield return PadClick(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,title.runesButton);while(!(title.CurrentState is RuneMenuCharacterState))yield return null;
  Require(all.All(r=>r.level==0&&!r.toggleOn),"native rune menu projects the newly inspected character's independent ranks");
  Click(title.runeConfirmButton);while(!Active(title.characterMenu)||!title.characterMenu.interactable)yield return null;
  yield return MenuMove(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,Entries(title.characterMenu)[0].button);
  yield return PadClick(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,title.runesButton);while(!(title.CurrentState is RuneMenuCharacterState))yield return null;
  Require(all.Sum(r=>r.level)==2&&target.toggleOn&&all.Single(r=>r.rune.data.name==neighborRank.name).toggleOn,"returning to first character restores both independently enabled ranks");title.Save();
  SetPhase("PASS");Log("complete","Native rune menu uses character-owned ranks and independent no-purchase toggles");Application.Quit(0);
 }
}
