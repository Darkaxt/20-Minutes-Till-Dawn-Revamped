using System;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.UI;
using flanne.Player;
using flanne.CharacterPassives;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static int FlashActivations;
 IEnumerator CheckAndroidBase()
 {
  RunResume.Invalidate();foreach(string id in new[]{"weapon:GrenadeLauncherData","weapon:DualSMGsData","character:Abby","character:Hina"})Progression.State.earned.Add(id);Progression.Refresh();
  yield return AchievementRun("Shana","GrenadeLauncherData","20M_Forest");StopFixtureSpawning();var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;
  Require(p.gun.gunData.damage==45&&Mathf.Abs(p.movementSpeed-3.2f)<.001f,"Android Grenade Launcher base damage and Shana movement load natively");
  yield return CheckRevolver(game,p,"android-grenade-shana.png","GrenadeLauncherData");yield return AchievementExit(game,false);
  yield return AchievementRun("Abby","DualSMGsData","20M_Forest");StopFixtureSpawning();game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  Require(p.playerHealth.maxHP==3&&p.movementSpeed==3,"Android Abby starts with three hearts and base movement");
  var xp=game.playerXP;AccessTools.Field(typeof(PlayerXP),"<level>k__BackingField").SetValue(xp,19);AccessTools.Field(typeof(PlayerXP),"xp").SetValue(xp,0f);
  xp.GainXP((float)AccessTools.Property(typeof(PlayerXP),"xpToLevel").GetValue(xp)+1);
  while(!(game.CurrentState is GunEvoMenuState)||AccessTools.Field(typeof(flanne.UI.Menu),"ClickEvent").GetValue(game.gunEvoMenu)==null)yield return null;
  Click(Enumerable.Range(0,3).Select(i=>game.gunEvoMenu.GetEntry(i)).Single(b=>b.GetComponent<GunEvoUI>().data.name=="FlashMag"));
  while(!(game.CurrentState is CombatState)||p.gun.gunData.name!="FlashMagGun")yield return null;
  while(PauseController.isPaused)yield return null;
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  int shots=0;bool early=false;FlashActivations=0;UnityEngine.Events.UnityAction count=()=>{shots++;if(shots<20&&FlashActivations!=0)early=true;};p.gun.OnShoot.AddListener(count);
  var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.up*8);
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));
  while(shots<20)yield return null;
  Require(!early,"native Flash Mag does not activate before twenty actual gun shots, including paired shots in one frame");
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});p.gun.OnShoot.RemoveListener(count);
  Require(FlashActivations==1,"native Flash Mag activates its lightning action on the twentieth shot");
  while(!(p.CurrentState is IdleState))yield return null;
  yield return Capture("android-flash-mag.png");yield return AchievementExit(game,false);
  yield return AchievementRun("Hina","RevolverData","20M_Forest");StopFixtureSpawning();game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  var skill=p.GetComponentInChildren<ShadowClonePassive>();Require((float)AccessTools.Field(typeof(SkillPassive),"cooldown").GetValue(skill)==1,"Android Hina native skill cooldown is one second");
  var input=(InputActionAsset)AccessTools.Field(typeof(SkillPassive),"inputs").GetValue(skill);var action=input.FindActionMap("PlayerMap").FindAction("Skill");
  Require(action.bindings.Any(b=>b.effectivePath=="<Mouse>/rightButton"),"native Hina skill retains the Windows right-button binding");
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Right));
  while(!p.disableAction.value)yield return null;
  Require(Mathf.Abs(p.movementSpeed-18)<.001f,"Android Hina dash uses six times native base movement");
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});while(p.disableAction.value)yield return null;
  Require(p.movementSpeed==3,"native Hina dash restores movement after completion");
  yield return CheckRevolver(game,p,"android-hina-revolver.png");yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete","Android native grenade damage, Shana movement, Abby health, Flash Mag twenty-shot trigger and Hina dash/cooldown");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(flanne.PerkSystem.Actions.LineThunderAttackTowadsCursorAction),nameof(flanne.PerkSystem.Actions.LineThunderAttackTowadsCursorAction.Activate))]
class ObserveNativeFlashMag
{static void Prefix(){RuntimeChecks.FlashActivations++;}}
[HarmonyPatch(typeof(flanne.PerkSystem.Triggers.OnShootTrigger),nameof(flanne.PerkSystem.Triggers.OnShootTrigger.OnEquip))]
class ObserveNativeShotThreshold
{
 static void Postfix(int ___shotsToTrigger)
 {if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="powerups-android-base")RuntimeChecks.Log("native-shots-to-trigger",___shotsToTrigger);}
}
