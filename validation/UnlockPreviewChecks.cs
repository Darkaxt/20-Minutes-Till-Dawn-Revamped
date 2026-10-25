using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.TitleScreen;
using flanne.RuneSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckUnlockPreview(TitleScreenController title)
 {
  SetPhase("unlock-natural-preview");RunResume.Invalidate();var pad=InputSystem.AddDevice<Gamepad>("RevampedUnlockController");
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
  var character=Progression.Gates.Single(g=>g.Id=="character:Diamond");var weapon=Progression.Gates.Single(g=>g.Id=="weapon:ModernizationChargeCannon_None");
  var earned=Progression.State.earned.ToArray();var attempts=Progression.State.attempts.ToArray();
  foreach(var gate in new[]{character,weapon})
  {
   Progression.State.earned.RemoveAll(id=>id==gate.Id);Progression.State.attempts.RemoveAll(a=>a.id==gate.Id);
   Progression.State.attempts.Add(new Progression.Attempt{id=gate.Id,text="Last attempt: Darkness 0 / 2; current hearts: 4 / 7"});
  }
  Progression.Refresh();string ownership=Ownership();
  yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f||title.selectPanel.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  int index=Characters.Items(title.characterMenu).FindIndex(x=>x.data==character.Data);var button=Entries(title.characterMenu)[index].button;
  yield return SelectorMove(pad,title.characterMenu,button);RequireCharacterPreview(title,index);
  Require(!SelectorText().Contains("Last attempt:")&&!SelectorText().Contains("Not attempted"),"browsing locked character hides progress diagnostics");
  Require(character.Goal=="Defeat a miniboss in the Forest with at least 7 maximum red hearts.","Diamond requirement is a natural sentence without redundant Darkness0");
  foreach(var gate in Progression.Gates)
  {
   Require(gate.Goal.EndsWith(".")&&!gate.Goal.Contains(";")&&!gate.Goal.Contains("Darkness 0+"),"unlock sentence readable: "+gate.Id);
   Require(gate.Perks.All(key=>gate.Goal.Contains(LocalizationSystem.GetLocalizedValue(key))),"all required skill names remain present: "+gate.Id);
   if(gate.MinimumDarkness>0)Require(gate.Goal.Contains("Darkness "+gate.MinimumDarkness+" or higher"),"minimum Darkness remains explicit: "+gate.Id);
  }
  yield return Capture("unlock-character-preview.png");
  yield return SelectorPad(pad,GamepadButton.South);
  Require(title.CurrentState is CharacterSelectState&&SelectorText().Contains("Last attempt:"),"controller Confirm reveals locked character progress in place");
  Require(Ownership()==ownership,"character inspection cannot unlock or spend credits");yield return Capture("unlock-character-details.png");
  yield return SelectorMove(pad,title.characterMenu,Entries(title.characterMenu)[0].button);
  yield return SelectorMove(pad,title.characterMenu,button);Require(!SelectorText().Contains("Last attempt:"),"browsing away clears character detail expansion");
  yield return MenuPointer(button);Require(SelectorText().Contains("Last attempt:"),"actual pointer click reveals locked character progress");
  yield return SelectorPad(pad,GamepadButton.East);while(!Active(title.mainMenu))yield return null;
  yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  yield return SelectorMove(pad,title.characterMenu,Entries(title.characterMenu)[index].button);Require(!SelectorText().Contains("Last attempt:"),"reopening the roster starts with concise text");
  yield return SelectorMove(pad,title.characterMenu,Entries(title.characterMenu)[0].button);yield return SelectorPad(pad,GamepadButton.South);
  while(!Active(title.gunMenu)||title.gunMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  index=NativeSelection.GunItems(title.gunMenu).FindIndex(x=>x.data==weapon.Data);button=Entries(title.gunMenu)[index].button;
  yield return SelectorMove(pad,title.gunMenu,button);Require(!SelectorText().Contains("Last attempt:"),"browsing locked weapon hides progress diagnostics");
  Require(weapon.Goal.Contains("second boss in the Forest on Darkness 2 or higher"),"weapon requirement preserves boss order, scenario and difficulty");
  yield return SelectorPad(pad,GamepadButton.South);Require(title.CurrentState is GunSelectState&&SelectorText().Contains("Last attempt:"),"controller Confirm reveals locked weapon progress without starting play");
  Require(Ownership()==ownership,"weapon inspection cannot unlock or spend credits");yield return Capture("unlock-weapon-details.png");
  yield return SelectorMove(pad,title.gunMenu,Entries(title.gunMenu)[0].button);yield return SelectorMove(pad,title.gunMenu,button);
  Require(!SelectorText().Contains("Last attempt:"),"browsing away clears weapon detail expansion");
  yield return MenuPointer(button);Require(SelectorText().Contains("Last attempt:"),"actual pointer click reveals locked weapon progress");
  Progression.State.attempts=attempts.ToList();Progression.State.earned=earned.ToList();Progression.Refresh();
  yield return SelectorPad(pad,GamepadButton.East);Log("native-weapon-back",title.CurrentState.GetType().Name);
  SetPhase("PASS");Log("complete","Natural unlock sentences and on-demand progress preserve native ownership/navigation through actual controller and pointer activation");Application.Quit(0);
 }
 IEnumerator CheckHudOrder()
 {
  SetPhase("hud-natural-order");RunResume.Invalidate();
  yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<RuneData>());
  StopFixtureSpawning();StatsOverlay.Enabled=true;StatsOverlay.Instance.Dirty();yield return null;
  var hud=StatsOverlay.Instance;
  string[] names={"Max HP","Move speed","Dodge","Summon damage","Summon speed","Damage","Fire rate","Reload","Magazine","Projectiles","Bullet speed","Bullet size","Pierce","Bounce"};
  string[] icons={"Vitality","Haste","Evasive","SummonMastery","Trainer","PowerShot","RapidFire","QuickHands","FreshClip","DoubleShot","Sniper","BigShot","Penetration","RubberBullets"};
  string[] rows=hud.LastStatsText.Split('\n');
  Require(rows.Select(r=>r.Substring(0,r.IndexOf("  ",StringComparison.Ordinal))).SequenceEqual(names),"HUD groups character traits, summons, then weapons");
  Require(hud.AttributeIcons.Select(i=>i.name).SequenceEqual(icons.Select(i=>"Attribute_"+i)),"HUD native icons match the reordered values");
  Require(rows[0].Contains("4")&&rows[1].Contains("3.2")&&rows[3].Contains("100%")&&rows[5].Contains("20"),"controlled Shana/Revolver fixture retains character, summon and weapon values");
  Require(hud.Body.text.Contains("<color=#87988b>")&&hud.Body.text.Contains(" +0)"),"effective/base/bonus notation is retained");
  yield return Capture("hud-character-summon-weapon-order.png");
  SetPhase("PASS");Log("complete","Transparent HUD orders character traits, summons and weapons with unchanged values");Application.Quit(0);
 }
}
