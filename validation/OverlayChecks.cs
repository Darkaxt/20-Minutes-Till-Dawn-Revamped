using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using HarmonyLib;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
namespace TwentyMinutes.Modernization.Validation;
using TwentyMinutes.Modernization;
public partial class RuntimeChecks
{
 IEnumerator CheckOverlay(GameController game,PlayerController player,Mouse mouse,Keyboard keyboard)
 {
  SetPhase("overlay-base");var overlay=StatsOverlay.Instance;
  while(overlay.Panel==null || !overlay.Panel.activeInHierarchy || overlay.LastStatsText==null)yield return null;
  StatsOverlay.Mode=MetricsView.Extended;overlay.Refresh();Require(overlay.LastStatsText.Contains("Damage  20"),"overlay reads native base damage");
  yield return Capture("overlay-base.png");
  var big=Resources.FindObjectsOfTypeAll<Powerup>().First(x=>x.name=="BigShot");var haste=Resources.FindObjectsOfTypeAll<Powerup>().First(x=>x.name=="Haste");
  Log("diagnostic-action","Apply two Big Shot pickups and Haste through native PlayerPerks.Equip; not a simulated level-up selection");
  player.playerPerks.Equip(big);player.playerPerks.Equip(big);player.playerPerks.Equip(haste);
  overlay.Refresh();
  Require(Mathf.Abs(player.gun.damage-38)<.01f,"two real Big Shot applications change native damage to 38");
  Require(System.Text.RegularExpressions.Regex.Replace(overlay.LastStatsText,"<[^>]*>","").Contains("Damage  38") && overlay.LastStatsText.Contains("+18"),"overlay shows damage base and actual +18 net bonus");
  Require(overlay.Collected.Values.Single(x=>x.Name==big.nameString).Count==2,"actual collected Big Shot stack count = 2");
  Require(overlay.Collected.Values.Single(x=>x.Name==haste.nameString).Tier=="Tier 1","tree tier distinguished from stack count");
  Require(Mathf.Abs(player.finalMoveSpeed-player.movementSpeed*1.2f)<.01f,"Haste changes native movement speed by 20 percent");
  File.WriteAllText(Path.Combine(ResultDir,"overlay-live-values.txt"),overlay.LastStatsText+"\n\n"+overlay.LastSkillsText);
  SetPhase("overlay-live-modifiers");yield return Capture("overlay-modified.png");
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F2));while(StatsOverlay.Enabled)yield return null;
  InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
  Require(!overlay.Body.transform.parent.gameObject.activeSelf&&overlay.Panel.activeSelf&&overlay.SkillContent.gameObject.activeInHierarchy,"native F2 Off hides metrics while retaining selected skills");
  yield return Capture("overlay-hidden.png");
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F2));while(!StatsOverlay.Enabled)yield return null;
  InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
  Require(overlay.Panel.activeSelf,"native F2 input restores overlay");
  Require(overlay.Panel.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>()==null,"overlay cannot intercept gameplay pointer input");
  if(player.gun.shooters[0] is PortedShooter)yield return Shot(mouse,.1f,Evolution.None,38,1,null);else yield return CheckRevolver(game,player);
  yield return Capture("overlay-after-shot.png");
  // Use this run's ordinary pool. Global Resources also contains exclusive
  // character recipes whose source actions are installed lazily on Apply.
  var extra=FallbackPool(game.powerupGenerator).Select(item=>item.powerup).Where(x=>x.powerupTreeUIData!=null && x.name!="BigShot" && x.name!="Haste" && ((flanne.PerkSystem.PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(x)).Length==0 && ((StatChange[])AccessTools.Field(typeof(Powerup),"statChanges").GetValue(x)).Length>0 && ((StatChange[])AccessTools.Field(typeof(Powerup),"statChanges").GetValue(x)).All(s=>s.isFlatMod?s.flatValue>=0:s.value>=0)).GroupBy(x=>x.name).Select(x=>x.First()).Take(14).ToArray();
  Require(extra.Length==14,"long-list fixture has fourteen real nonnegative stat-only powerups");
  foreach(var powerup in extra)player.playerPerks.Equip(powerup);
  overlay.Refresh();yield return null;
  Require(overlay.SkillContent.rect.height>420,"long collected-skill list exceeds viewport and remains scrollable");
  yield return Capture("overlay-long-skills-top.png");
  var scroll=overlay.SkillScroll;
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.PageDown));while(scroll.verticalNormalizedPosition>.51f)yield return null;
  InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
  Require(overlay.SkillContent.anchoredPosition.y>0,"Page Down reaches later collected skills");
  yield return Capture("overlay-long-skills-scrolled.png");
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.PageUp));while(scroll.verticalNormalizedPosition<.99f)yield return null;
  InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
  Require(overlay.Panel.GetComponentsInChildren<Image>().All(i=>i.sprite!=null&&!i.raycastTarget),"every HUD image is a native icon with no panel, border or raycast target");
  Require(overlay.AttributeIcons.Count==14&&overlay.SkillIcons.Count==overlay.Collected.Count,"native attributes and every collected skill have their own icons");
  Require(!overlay.Body.text.Contains("Damage")&&overlay.Body.fontSize>=16,"floating attribute numbers have no repeated labels and remain legible");
  Log("overlay-geometry","root="+overlay.Panel.GetComponent<RectTransform>().rect+" stats preferredHeight="+overlay.Body.preferredHeight+" skills height="+overlay.SkillContent.rect.height);
 }
 IEnumerator CheckIntegratedHud(flanne.TitleScreen.TitleScreenController title)
 {
  while(TitleArtwork.Instance==null||TitleArtwork.Instance.Logos.Count==0)yield return null;
  while(title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  VerifyTitleArtwork();yield return Capture("revamped-title.png");
  title.ChangeState<flanne.TitleScreen.OptionsMenuState>();while(!Active(title.optionsMenu))yield return null;
  var overlay=StatsOverlay.Instance;while(overlay.SettingButton==null||!overlay.SettingButton.gameObject.activeInHierarchy||!overlay.SettingButton.IsInteractable())yield return null;
  StatsOverlay.Mode=MetricsView.Extended;Click(overlay.SettingButton);Require(!StatsOverlay.Enabled,"native Options disables metrics");Click(overlay.SettingButton);Require(StatsOverlay.Mode==MetricsView.Little,"native Options enables Little metrics");
  var style=ColorfulStyle.Instance;
  if(style.Enabled)Click(style.SettingButton);
  Click(style.SettingButton);Require(style.Enabled&&style.AppliedCount>0,"native title Options enables the selected compatible artwork");
  Click(style.SettingButton);Require(!style.Enabled&&style.LastRestored>0&&style.LastRestoreFailures==0,"native title Options restores original artwork exactly");
  yield return Capture("integrated-title-options.png");
  title.ChangeState<flanne.TitleScreen.TitleMainMenuState>();while(!Active(title.mainMenu))yield return null;
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<RuneData>());
  var game=UnityEngine.Object.FindObjectOfType<GameController>();var player=PlayerController.Instance;StopFixtureSpawning();
  yield return CheckOverlay(game,player,fixtureMouse,fixtureKeyboard);
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return null;Require(!overlay.Panel.activeSelf,"native pause hides floating HUD");
  yield return Capture("integrated-pause.png");
  Click(game.optionsButton);while(!(game.CurrentState is OptionsState)||!Active(game.optionsMenu))yield return null;
  while(style.SettingButton==null||!style.SettingButton.gameObject.activeInHierarchy||!style.SettingButton.IsInteractable())yield return null;
  Click(style.SettingButton);Require(style.Enabled&&style.AppliedCount>0,"native combat Options enables compatible artwork without replacing gameplay objects");
  yield return Capture("integrated-combat-options.png");
  Click(Entries(game.optionsMenu)[0].button);while(!(game.CurrentState is PauseState))yield return null;
  Click(game.pauseResumeButton);while(!(game.CurrentState is CombatState)||PauseController.isPaused)yield return null;
  yield return Capture("integrated-colorful-hud.png");
  yield return AchievementExit(game,false);Require(overlay.Collected.Count==0,"native scene exit clears collected HUD ownership");
  Require(StatsOverlay.Enabled&&style.Enabled,"both enhancement settings remain enabled for the separate fresh-process persistence check");
  Plugin.Instance.Config.Save();File.Copy(Plugin.Instance.Config.ConfigFilePath,Path.Combine(ResultDir,"saved-enhancements.cfg"));
  SetPhase("PASS");Log("complete","Native icon HUD/title, actual modifiers, counts/tiers, clipping, Options/F2/PgUp/PgDn, shooting and scene cleanup verified");Application.Quit(0);
 }
}
