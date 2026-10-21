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
namespace TwentyMinutes.Modernization.Validation;
using TwentyMinutes.Modernization;
public partial class RuntimeChecks
{
 IEnumerator CheckOverlay(GameController game,PlayerController player,Mouse mouse,Keyboard keyboard)
 {
  SetPhase("overlay-base");var overlay=StatsOverlay.Instance;
  while(overlay.Panel==null || !overlay.Panel.activeInHierarchy || overlay.LastStatsText==null)yield return null;
  overlay.Refresh();Require(overlay.LastStatsText.Contains("Damage  20"),"overlay reads native base damage");
  yield return Capture("overlay-base.png");
  var big=Resources.FindObjectsOfTypeAll<Powerup>().First(x=>x.name=="BigShot");var haste=Resources.FindObjectsOfTypeAll<Powerup>().First(x=>x.name=="Haste");
  Log("diagnostic-action","Apply two Big Shot pickups and Haste through native PlayerPerks.Equip; not a simulated level-up selection");
  player.playerPerks.Equip(big);player.playerPerks.Equip(big);player.playerPerks.Equip(haste);
  overlay.Refresh();
  Require(Mathf.Abs(player.gun.damage-38)<.01f,"two real Big Shot applications change native damage to 38");
  Require(overlay.LastStatsText.Contains("Damage  38") && overlay.LastStatsText.Contains("+18"),"overlay shows damage base and actual +18 net bonus");
  Require(overlay.Collected.Values.Single(x=>x.Name==big.nameString).Count==2,"actual collected Big Shot stack count = 2");
  Require(overlay.Collected.Values.Single(x=>x.Name==haste.nameString).Tier=="Tier 1","tree tier distinguished from stack count");
  Require(Mathf.Abs(player.finalMoveSpeed-player.movementSpeed*1.2f)<.01f,"Haste changes native movement speed by 20 percent");
  File.WriteAllText(Path.Combine(ResultDir,"overlay-live-values.txt"),overlay.LastStatsText+"\n\n"+overlay.LastSkillsText);
  SetPhase("overlay-live-modifiers");yield return Capture("overlay-modified.png");
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F2));while(StatsOverlay.Enabled)yield return null;
  InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
  Require(!overlay.Panel.activeSelf,"native F2 input hides overlay");
  yield return Capture("overlay-hidden.png");
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.F2));while(!StatsOverlay.Enabled)yield return null;
  InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
  Require(overlay.Panel.activeSelf,"native F2 input restores overlay");
  Require(overlay.Panel.GetComponentInParent<UnityEngine.UI.GraphicRaycaster>()==null,"overlay cannot intercept gameplay pointer input");
  yield return Shot(mouse,.1f,Evolution.None,38,1,null);
  yield return Capture("overlay-after-shot.png");
  var extra=Resources.FindObjectsOfTypeAll<Powerup>().Where(x=>x.name!="BigShot" && x.name!="Haste" && ((flanne.PerkSystem.PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(x)).Length==0 && ((StatChange[])AccessTools.Field(typeof(Powerup),"statChanges").GetValue(x)).Length>0 && ((StatChange[])AccessTools.Field(typeof(Powerup),"statChanges").GetValue(x)).All(s=>s.isFlatMod?s.flatValue>=0:s.value>=0)).GroupBy(x=>x.name).Select(x=>x.First()).Take(8).ToArray();
  Require(extra.Length==8,"long-list fixture has eight real nonnegative stat-only powerups");
  foreach(var powerup in extra)player.playerPerks.Equip(powerup);
  overlay.Refresh();yield return null;
  Require(overlay.Skills.preferredHeight>78,"long collected-skill list exceeds viewport and remains scrollable");
  yield return Capture("overlay-long-skills-top.png");
  var scroll=overlay.Skills.GetComponentInParent<ScrollRect>();
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.PageDown));while(scroll.verticalNormalizedPosition>.51f)yield return null;
  InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
  Require(overlay.Skills.rectTransform.anchoredPosition.y>0,"Page Down reaches later collected skills");
  yield return Capture("overlay-long-skills-scrolled.png");
  InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.PageUp));while(scroll.verticalNormalizedPosition<.99f)yield return null;
  InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;
  Log("overlay-geometry","panel="+overlay.Panel.GetComponent<RectTransform>().rect+" stats preferredHeight="+overlay.Body.preferredHeight+" skills preferredHeight="+overlay.Skills.preferredHeight);
 }
}
