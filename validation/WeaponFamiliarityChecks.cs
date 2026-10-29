using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 void FamiliarityPointerFacts(string phase)
 {
  var module=EventSystem.current.currentInputModule;var point=AccessTools.Property(module.GetType(),"point")?.GetValue(module) as InputActionReference;
  Log("familiarity-pointer-"+phase,"state="+Progression.Title.CurrentState.GetType().Name+" cursor="+Cursor.lockState+" fixtureMouse="+fixtureMouse.deviceId+":"+fixtureMouse.position.ReadValue()+" enabled="+fixtureMouse.enabled+" added="+fixtureMouse.added+" currentMouse="+Mouse.current?.deviceId+":"+Mouse.current?.position.ReadValue()+" module="+module.GetType().Name+" update="+InputSystem.settings.updateMode+" pointEnabled="+point?.action?.enabled+" mask="+point?.action?.bindingMask+" point="+point?.action?.ReadValue<Vector2>()+" source="+point?.action?.activeControl?.path+" controls="+string.Join(",",point?.action?.controls.Select(c=>c.device.deviceId+":"+c.path)??Enumerable.Empty<string>())+" selected="+EventSystem.current.currentSelectedGameObject?.name+" text="+SelectorText());
 }
 IEnumerator FamiliarityHud(string name)
 {
  StatsOverlay.Mode=MetricsView.Extended;StatsOverlay.Instance.Refresh();yield return null;
  while(!StatsOverlay.Instance.Panel.activeInHierarchy||!StatsOverlay.Instance.Body.gameObject.activeInHierarchy)yield return null;
  StatsOverlay.Instance.Body.ForceMeshUpdate();Require(StatsOverlay.Instance.Body.text.Contains("#A8D5BA"),"visible native metrics show improved handling in pastel green");
  yield return PoiCapture(name);
 }
 void FamiliarityContract(int rank)
 {
  var p=PlayerController.Instance;var gun=p.gun;var data=gun.gunData;var recipe=gun.GetProjectileRecipe();float factor=1+rank*.02f;
  Require(p.GetComponents<WeaponFamiliarity>().Length==1&&p.GetComponent<WeaponFamiliarity>().Rank==rank,"one battle-owned familiarity rank "+rank);
  CombatClose(gun.shotCooldown,data.shotCooldown/factor,"native fire rate uses familiarity");
  CombatClose(gun.reloadDuration,data.reloadDuration/factor,"native reload duration uses inverse familiarity rate");
  CombatClose(recipe.projectileSpeed,data.projectileSpeed*factor,"native projectile recipe uses familiarity");
  CombatClose(recipe.damage,data.damage,"familiarity preserves damage");CombatClose(recipe.size,1,"familiarity preserves bullet size");CombatClose(recipe.knockback,data.knockback,"familiarity preserves knockback");
  CombatClose(gun.spread,data.spread,"familiarity preserves characteristic spread");
  Require(gun.maxAmmo==data.maxAmmo&&gun.numOfProjectiles==data.numOfProjectiles&&recipe.piercing==data.piercing&&recipe.bounce==data.bounce,"familiarity preserves magazine/projectile/Pierce/Bounce counts");
  StatsOverlay.Instance.Refresh();Require(StatsOverlay.Instance.LastStatsText.Contains("Reload"),"native effective handling reaches existing metrics HUD");
  Log("familiarity-contract",data.name+" rank="+rank+" cooldown="+gun.shotCooldown+" reload="+gun.reloadDuration+" speed="+recipe.projectileSpeed+" spread="+gun.spread);
 }
 IEnumerator CheckWeaponFamiliarity(string mode)
 {
  var title=Progression.Title;
  if(mode.EndsWith("view"))
  {
   RunResume.Invalidate();int slot=NativeSelection.GunItems(title.gunMenu).FindIndex(i=>i.data.name=="RevolverData");SaveSystem.data.gunHighestClear[slot]=5;
   yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();FamiliarityContract(5);yield return FamiliarityHud("weapon-familiarity-hud.png");
   yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Actual visible Extended HUD displays native familiarity totals and beneficial reload color");Application.Quit(0);yield break;
  }
  if(mode.EndsWith("continue")||mode.EndsWith("prior"))
  {
   var saved=RunResume.Read();bool prior=mode.EndsWith("prior");Require(prior?!saved.weaponFamiliarity.HasValue:saved.weaponFamiliarity==5,"exact input checkpoint has the expected optional familiarity contract");
   int slot=NativeSelection.GunItems(title.gunMenu).FindIndex(i=>i.data.name==saved.weapon);SaveSystem.data.gunHighestClear[slot]=15;SaveSystem.Save();
   yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
   FamiliarityContract(prior?15:5);RunResume.Save();Require(RunResume.Read().weaponFamiliarity==(prior?15:5),"Continue writes its single battle rank without duplicated aggregate stats");
   yield return FamiliarityHud(prior?"familiarity-prior-continue.png":"familiarity-continue.png");yield return AchievementExit(game,false);
   SetPhase("PASS");Log("complete","Exact current/prior checkpoint through actual Continue; changed profile crown, no duplicate modifiers, old writer adoption, HUD and teardown");Application.Quit(0);yield break;
  }
  RunResume.Invalidate();SaveSystem.data.difficultyUnlocked=15;
  // Explicit earned access and crown values are QA preconditions. Native menu,
  // scene, stat/evolution and result/save behavior below remains unmodified.
  foreach(var gate in Progression.Gates)if(!Progression.State.earned.Contains(gate.Id))Progression.State.earned.Add(gate.Id);
  Progression.State.pending.Clear();
  foreach(var tree in new[]{title.swordRuneTree,title.shieldRuneTree})foreach(var row in RuneProgression.Rows(tree))foreach(var rune in RuneProgression.Entries(row))rune.toggleOn=false;
  RuneProgression.Profile("Shana").enabled.Clear();Progression.Save();Progression.Refresh();
  var names=NativeSelection.GunItems(title.gunMenu).Select(i=>i.data.name).ToArray();
  if(mode.EndsWith("arsenal"))foreach(var name in names)
  {
   title=Progression.Title;int index=NativeSelection.GunItems(title.gunMenu).FindIndex(i=>i.data.name==name);SaveSystem.data.gunHighestClear[index]=15;
   var original=title.gunMenu[index];string before=JsonUtility.ToJson(original);
   yield return AchievementRun("Shana",name,"20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();FamiliarityContract(15);
   AccessTools.Method(typeof(WeaponFamiliarity),"Apply").Invoke(null,new object[]{PlayerController.Instance});FamiliarityContract(15);
   Require(JsonUtility.ToJson(original)==before,"battle familiarity never mutates shared weapon data: "+name);
   yield return AchievementExit(game,false);
  }
  if(mode.EndsWith("arsenal")){SetPhase("PASS");Log("complete","All twelve real native weapon selections, handling stats and unchanged identity attributes/shared assets");Application.Quit(0);yield break;}
  title=Progression.Title;var guns=NativeSelection.GunItems(title.gunMenu);int revolver=guns.FindIndex(i=>i.data.name=="RevolverData");int shotgun=guns.FindIndex(i=>i.data.name=="ShotgunData");
  SaveSystem.data.gunHighestClear[revolver]=5;SaveSystem.data.gunHighestClear[shotgun]=0;
  Require(WeaponFamiliarity.Crown(revolver)==5&&WeaponFamiliarity.Crown(shotgun)==0,"weapon crown records stay independent");
  bool pointerProbe=mode.EndsWith("pointer-probe");
  foreach(int rank in pointerProbe?new[]{0}:new[]{-1,0,5})
  {
   SaveSystem.data.gunHighestClear[revolver]=rank;
   yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();FamiliarityContract(Math.Max(0,rank));
   SaveSystem.data.gunHighestClear[revolver]=15;FamiliarityContract(Math.Max(0,rank));
   if(rank==0)
   {
    PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return null;Click(game.giveupButton);
    while(!(game.CurrentState is PlayerDeadState)||!game.retryRunButton.IsInteractable())yield return null;
    int previous=game.GetInstanceID();Click(game.retryRunButton);
    while((game=Object.FindObjectOfType<GameController>())==null||game.GetInstanceID()==previous||!(game.CurrentState is CombatState))yield return null;
    StopFixtureSpawning();GameTimer.SharedInstance.Stop();Log("familiarity-retry-runes",string.Join(",",Loadout.RuneSelection.Select(r=>r.name+":"+r.level)));FamiliarityContract(15);
   }
   yield return AchievementExit(game,false);
  }
  // Real controller and pointer selection render the same native description.
  title=Progression.Title;var pad=InputSystem.AddDevice<Gamepad>("FamiliarityController");
  while(title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||!title.characterMenu.interactable)yield return null;
  yield return PadClick(pad,title.characterMenu.transform,Entries(title.characterMenu)[0].button);while(!Active(title.gunMenu)||!title.gunMenu.interactable)yield return null;
  yield return MenuMove(pad,title.gunMenu.transform,Entries(title.gunMenu)[shotgun].button);
  Require(!SelectorText().Contains("Familiarity +"),"unmastered weapon preview has no handling bonus");
  yield return MenuMove(pad,title.gunMenu.transform,Entries(title.gunMenu)[revolver].button);Require(SelectorText().Contains("Familiarity +30%"),"controller preview shows existing crown handling bonus");
  while(title.gunMenu.GetComponent<CanvasGroup>().alpha<.999f||title.selectPanel.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  yield return PoiCapture("weapon-familiarity-preview.png");FamiliarityPointerFacts("before");yield return MenuPointer(Entries(title.gunMenu)[shotgun].button);FamiliarityPointerFacts("after");
  if(pointerProbe){yield return Capture("pointer-after.png");SetPhase("PROBE");Application.Quit(0);yield break;}
  Require(!SelectorText().Contains("Familiarity +"),"pointer preview changes to the other weapon's own crown");
  if(title.CurrentState is ModeSelectState){while(!title.modeSelectBackButton.IsInteractable()||title.modeSelectBackButton.GetComponentsInParent<CanvasGroup>().Any(g=>g.alpha<.999f))yield return null;yield return PadClick(pad,title.modeSelectPanel.transform,title.modeSelectBackButton);while(!(title.CurrentState is GunSelectState)||!GunBackReady)yield return null;}
  yield return SelectorPad(pad,GamepadButton.East);while(!(title.CurrentState is CharacterSelectState))yield return null;
  while(title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  yield return SelectorPad(pad,GamepadButton.East);while(!(title.CurrentState is TitleMainMenuState)||!title.mainMenu.interactable||title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  // A genuine native victory updates the existing crown; repeated same-tier
  // victories do not stack and only the next battle reads the new record.
  SaveSystem.data.gunHighestClear[revolver]=0;
  yield return AchievementRun("Shana","RevolverData","20M_Forest",darkness:5,prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  var current=Object.FindObjectOfType<GameController>();StopFixtureSpawning();FamiliarityContract(0);yield return AchievementWin(current);Require(SaveSystem.data.gunHighestClear[revolver]==5,"actual native victory raises familiarity source crown");
  yield return AchievementRun("Shana","RevolverData","20M_Forest",darkness:5,prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  current=Object.FindObjectOfType<GameController>();StopFixtureSpawning();FamiliarityContract(5);yield return AchievementWin(current);Require(SaveSystem.data.gunHighestClear[revolver]==5,"repeating a win does not accumulate familiarity");
  // Evolution owns different GunData but keeps the same player stats.
  yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  current=Object.FindObjectOfType<GameController>();StopFixtureSpawning();var p=PlayerController.Instance;
  yield return ArrowEvolution(current,p.gun.gunData.gunEvolutions[0].name);
  CombatClose(p.stats[StatType.FireRate].Modify(1),1.1f,"actual native evolution retains one familiarity contribution");
  p.stats[StatType.ReloadRate].AddMultiplierBonus(.2f);CombatClose(p.gun.reloadDuration,p.gun.gunData.reloadDuration/1.3f,"familiarity composes additively with native upgrade rate");
  yield return AchievementExit(current,false);
  // Native Custom battle startup uses the obtained weapon's current crown.
  title=Progression.Title;Loadout.GunSelection=title.gunMenu[revolver];Loadout.GunIndex=revolver;Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>();Loadout.difficultyLevel=0;
  CustomMode.Prepare(new CustomMode.Rules{map="20M_Forest"},Resources.FindObjectsOfTypeAll<MapData>().First(m=>m.name=="20M_Forest"));title.ChangeState<WaitToLoadIntoBattleState>();
  while((current=Object.FindObjectOfType<GameController>())==null||!(current.CurrentState is CombatState))yield return null;
  StopFixtureSpawning();FamiliarityContract(5);Require(CustomMode.Active,"familiarity is integrated into actual Custom startup");yield return AchievementExit(current,false);
  yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  current=Object.FindObjectOfType<GameController>();StopFixtureSpawning();FamiliarityContract(5);yield return FamiliarityHud("weapon-familiarity-hud.png");
  yield return DragonSuspend(current,"Rank cases, native victory/nonstacking, Retry, preview controller/pointer, evolution/native stat composition and Custom; snapshot rank5 for changed-profile Continue");
 }
}
