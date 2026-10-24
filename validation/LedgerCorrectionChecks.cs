using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using flanne.UI;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckLedgerDescriptions(TitleScreenController title)
 {
  SetPhase("ledger-rune-descriptions");yield return OpenRuneMenu(title);
  while(title.runeMenuPanel.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  foreach(string name in new[]{"CadenceRuneData","TenacityRuneData"})
  {
   var selector=title.runeMenuPanel.GetComponentsInChildren<RuneDescriptionSetOnSelect>(true).Single(s=>((RuneIcon)WindField(s,"runeIcon")).data.name==name);
   selector.OnSelect(null);yield return null;
   var description=(RuneDescription)WindField(selector,"runeDescription");var text=((TMP_Text)WindField(description,"descriptionTMP")).text;
   Require(name=="CadenceRuneData"?text.Contains("+99 Piercing")&&text.Contains("+20% Bullet Size per rank")&&text.Contains("+20% Bullet Damage per rank"):text.Contains("+3% Fire Rate")&&text.Contains("+5% Knockback per rank"),"M15 A61 actual native rune description matches unchanged rank arithmetic: "+name+" / "+text);
   yield return Capture("ledger-"+name+".png");
  }
  Click(title.runeConfirmButton);title.ChangeState<TitleMainMenuState>();while(!Active(title.mainMenu))yield return null;
  SetPhase("ledger-reckless-description");Click(Entries(title.mainMenu).Single(e=>e.name=="RevampedAchievements").button);
  SourceAchievementMenu menu;while((menu=Object.FindObjectOfType<SourceAchievementMenu>())==null)yield return null;
  while(!menu.GetComponentsInChildren<TMP_Text>().Any(t=>t.text.Contains("manual fire is allowed")))
  {var next=(UnityEngine.UI.Button)WindField(menu,"next");Require(next.IsInteractable(),"Reckless exists on a remaining native achievement page");Click(next);yield return null;}
  var reckless=menu.GetComponentsInChildren<TMP_Text>().Single(t=>t.text.Contains("manual fire is allowed"));
  Require(reckless.text.Contains("Abby")&&reckless.text.Contains("Grenade Launcher")&&reckless.text.Contains("skill at least once"),"M15 A219 rendered local Reckless description matches skill-use plus survival without forbidding manual fire");
  yield return Capture("ledger-reckless-description.png");Click((UnityEngine.UI.Button)WindField(menu,"back"));
  while(Object.FindObjectOfType<SourceAchievementMenu>()!=null)yield return null;
  yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();PauseController.SharedInstance.Pause();
  game.powerupMenuPanel.Show();
  foreach(string key in new[]{"thunderspawns_name","soul_knight_name"})
  {
   var power=Resources.FindObjectsOfTypeAll<Powerup>().First(p=>RunResume.Key(p)==key);
   AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidOfferMenus"),"Bind").Invoke(null,new object[]{game.powerupMenu,new List<Powerup>{power}});yield return null;
   var view=Object.FindObjectsOfType<PowerupDescription>().Single(d=>d.data==power);var text=((TMP_Text)WindField(view,"descriptionTMP")).text;
   Require(key=="thunderspawns_name"?text.Contains("15%")&&text.Contains("22")&&!text.Contains("30%"):text.Contains("+15")&&text.Contains("currently hold"),"M15 native powerup description matches source chance/current-heart rule: "+key+" / "+text);
   yield return Capture("ledger-"+key+".png");
  }
  game.powerupMenuPanel.Hide();PauseController.SharedInstance.UnPause();
  Log("fixture-precondition","Only native UI bindings are populated with two explicit asset choices for description rendering; no claim of natural prerequisite acquisition or proc-frequency measurement. Rune/achievement menu navigation uses actual controls. Gameplay formulas are unchanged.");
  yield return CheckRevolver(game,PlayerController.Instance,"ledger-text-original-input.png");yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete","M15 native rune/powerup/local achievement descriptions, unchanged original input and scene exit");Application.Quit(0);
 }
}
