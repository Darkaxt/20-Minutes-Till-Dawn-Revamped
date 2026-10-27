using System;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.UI;
using TMPro;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckResultsPoi(bool inspect)
 {
  SetPhase("results-and-poi");RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var player=PlayerController.Instance;
  var powers=FallbackPool(game.powerupGenerator).Select(p=>p.powerup).ToArray();player.playerPerks.Equip(powers.First(p=>p.name=="BigShot"));
  foreach(var power in powers)
  {
   var relations=SkillPanelDetails.Describe(power);var duplicates=relations.SelectMany(r=>r.Icons.Select(i=>new{r.Penalty,i.Key})).GroupBy(i=>i.Key).Where(g=>g.Select(i=>i.Penalty).Distinct().Count()>1).ToArray();
   if(duplicates.Length>0)Log("ambiguous-relations",power.name+" "+string.Join(",",duplicates.Select(g=>g.Key)));
   if(!inspect)Require(duplicates.Length==0,"native skill has no contradictory duplicate benefit/penalty target: "+power.name);
  }
  var pad=InputSystem.AddDevice<Gamepad>("ResultsController");game.playerInput.SwitchCurrentControlScheme("Gamepad",pad);game.playerInput.neverAutoSwitchControlSchemes=false;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
  if(!inspect)
  {
   var big=powers.First(p=>p.name=="DoubleShot");var relations=SkillPanelDetails.Describe(big);
   Require(relations.Where(r=>r.Mixed).SelectMany(r=>r.Icons).Count(i=>i.Key.StartsWith("weapon:"))==1&&!relations.Where(r=>!r.Mixed).SelectMany(r=>r.Icons).Any(i=>i.Key.StartsWith("weapon:")),"Double Shot shows its weapon trade-off once under Mixed");
   game.ChangeState<PowerupMenuState>();while(WindField(game.powerupMenu,"ConfirmEvent")==null)yield return null;
   while(game.powerupMenu.GetComponentsInParent<CanvasGroup>().Any(c=>c.alpha<.99f))yield return null;
   AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidOfferMenus"),"Bind").Invoke(null,new object[]{game.powerupMenu,new System.Collections.Generic.List<Powerup>{big,powers.First(p=>p.name=="SummonMastery")}});
   var toggles=(Toggle[])WindField(game.powerupMenu,"toggles");toggles[0].Select();yield return null;
   var description=game.powerupMenuPanel.GetComponentsInChildren<PowerupDescription>(true).Single();
   foreach(var viewport in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})
   {Screen.SetResolution(viewport.x,viewport.y,false);yield return WaitDisplay(viewport.x,viewport.y);CheckRelationLayout(description);yield return Capture("mixed-relations-"+viewport.x+".png");}
   yield return ExplorationConfirmXP(game);
   var run=game.GetComponent<ExplorationRun>();while(run.State==null)yield return null;
   Log("fixture-precondition","Sample48 independent native spawn waves; clear only those controlled fixture chests between draws. Retain real geometry/camera and actual reward collection below.");
   var travel=new System.Collections.Generic.List<float>();CameraZoom.Value=0;FeedbackCameras();
   for(uint seed=1;seed<=48;seed++)
   {
    run.State.random=seed;AccessTools.Method(typeof(ExplorationRun),"SpawnWave").Invoke(run,null);
    travel.AddRange(run.Chests.Select(c=>Vector2.Distance(c.transform.position,player.transform.position)/player.finalMoveSpeed));
    foreach(var chest in run.Chests.ToArray())Object.Destroy(chest.gameObject);run.Chests.Clear();run.State.drops.Clear();yield return null;
   }
   Require(travel.Min()<=10&&travel.Max()>=25&&travel.All(t=>t>=4.99f&&t<=30.01f),"real Forest placements include near5–10s and far25–30s journeys in the wider band");Log("travel-distribution","min="+travel.Min()+" max="+travel.Max());
   var xp=game.playerXP;
   foreach(int level in new[]{1,20,45,75})
   {
    Plugin.SetField(xp,"<level>k__BackingField",level);Plugin.SetField(xp,"xp",0f);float threshold=(float)AccessTools.Property(xp.GetType(),"xpToLevel").GetValue(xp);
    float expected=xp.xpMultiplier.Modify(threshold*.3f);yield return ExplorationContact(ExplorationFixture(run,ExplorationReward.Experience,(Vector2)player.transform.position+Vector2.up*2),game);
    CombatClose((float)WindField(xp,"xp"),expected,"XP chest gives30% of the actual current level requirement at level"+level);
   }
   xp.xpMultiplier.AddMultiplierBonus(.4f);Plugin.SetField(xp,"xp",0f);float required=(float)AccessTools.Property(xp.GetType(),"xpToLevel").GetValue(xp);
   yield return ExplorationContact(ExplorationFixture(run,ExplorationReward.Experience,(Vector2)player.transform.position+Vector2.up*2),game);CombatClose((float)WindField(xp,"xp"),required*.42f,"XP chest retains the native forty-percent earned XP multiplier");xp.xpMultiplier.AddMultiplierBonus(-.4f);
   Plugin.SetField(xp,"<level>k__BackingField",20);required=(float)AccessTools.Property(xp.GetType(),"xpToLevel").GetValue(xp);Plugin.SetField(xp,"xp",required*.9f);
   yield return ExplorationContact(ExplorationFixture(run,ExplorationReward.Experience,(Vector2)player.transform.position+Vector2.up*2),game);Require(xp.level==21&&game.CurrentState is CombatState,"percentage XP crosses the level boundary through the real native choice menu");
   int credits=run.State.credits;yield return ExplorationContact(ExplorationFixture(run,ExplorationReward.Credits,(Vector2)player.transform.position+Vector2.up*2),game);Require(run.State.credits==credits+1000,"actual credit chest awards1000 saved run credits");
   yield return AchievementBoss(game,1);while(!(game.CurrentState is CombatState)){yield return ExplorationConfirmXP(game);yield return null;}
   game.playerInput.SwitchCurrentControlScheme("Gamepad",pad);
  }
  AccessTools.Field(typeof(GameTimer),"<timer>k__BackingField").SetValue(GameTimer.SharedInstance,1200f);GameTimer.SharedInstance.Start();
  while(!(game.CurrentState is PlayerSurvivedState))yield return null;
  while(!game.quitToTitleButton.IsInteractable()||game.quitToTitleButton.GetComponentsInParent<CanvasGroup>().Any(c=>c.alpha<.99f))yield return null;
  yield return null;yield return null;
  Log("results-focus",SelectorFocus());MenuSnapshot(game.transform,"results-all");
  foreach(var panel in Object.FindObjectsOfType<flanne.UI.Panel>())Log("native-panel",panel.name+" default="+WindField(panel,"gamepadDefaultSelectable")+" interactable="+panel.interactable+" alpha="+panel.GetComponent<CanvasGroup>()?.alpha);
  Log("result-parents","retry="+game.retryRunButton.transform.parent.name+" quit="+game.quitToTitleButton.transform.parent.name+" score="+game.endScreenUIC.name+" skills="+game.powerupListUI.name+" skillAlpha="+game.powerupListUI.GetComponent<CanvasGroup>().alpha);
  yield return Capture("results-controller.png");
  Require(game.powerupListUI.GetComponent<CanvasGroup>().alpha==0&&!game.powerupListUI.interactable,"end-of-run skill list is absent and cannot receive controller focus");
  Require(EventSystem.current.currentSelectedGameObject==game.retryRunButton.gameObject||EventSystem.current.currentSelectedGameObject==game.quitToTitleButton.gameObject,"victory presentation leaves focus on Retry or Quit");
  if(inspect){SetPhase("PASS");Application.Quit(0);yield break;}
  yield return ResultsFocusTraversal(game,pad);
  var previous=game;yield return PadClick(pad,game.quitToTitleButton.transform.parent,game.retryRunButton);
  while((game=Object.FindObjectOfType<GameController>())==null||game==previous||!(game.CurrentState is CombatState))yield return null;
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();Require(!game.powerupListUI.interactable,"Retry creates a fresh run without an inherited results skill list");
  game.playerInput.SwitchCurrentControlScheme("Gamepad",pad);PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();
  yield return PadClick(pad,game.pauseMenu.transform,game.giveupButton);
  while(!(game.CurrentState is PlayerDeadState)||!game.quitToTitleButton.IsInteractable()||game.quitToTitleButton.GetComponentsInParent<CanvasGroup>().Any(c=>c.alpha<.99f))yield return null;
  Require(game.powerupListUI.GetComponent<CanvasGroup>().alpha==0&&!game.powerupListUI.interactable,"death results also omit the skill list");yield return ResultsFocusTraversal(game,pad);yield return Capture("death-controller.png");
  yield return PadClick(pad,game.quitToTitleButton.transform.parent,game.quitToTitleButton);
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null)yield return null;
  SetPhase("PASS");Application.Quit(0);
 }
 IEnumerator ResultsFocusTraversal(GameController game,Gamepad pad)
 {
  var root=game.quitToTitleButton.transform.parent;
  foreach(var button in new[]{GamepadButton.DpadUp,GamepadButton.DpadRight,GamepadButton.DpadDown,GamepadButton.DpadLeft,GamepadButton.DpadUp})
  {yield return SelectorPad(pad,button);Require(EventSystem.current.currentSelectedGameObject==game.retryRunButton.gameObject||EventSystem.current.currentSelectedGameObject==game.quitToTitleButton.gameObject,"results controller navigation remains on the menu in every direction");}
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);EventSystem.current.SetSelectedGameObject(null);
  yield return SelectorPad(pad,GamepadButton.DpadDown);Require(EventSystem.current.currentSelectedGameObject!=null&&EventSystem.current.currentSelectedGameObject.transform.IsChildOf(root),"switching back to controller restores results focus on the first navigation event");
 }
}
