using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.UI;
using flanne.PowerupSystem;
using flanne.PowerupSystems;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool LegacyMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-legacy-source-");
 internal static float LegacyFreshDamage,LegacyRecordedDamage;
 internal static bool LegacyCaptureShot;
 internal static int LegacyPrimaryShots;
 static RunResume.Snapshot LegacyRaw(string file){using(var stream=File.OpenRead(file))return (RunResume.Snapshot)new DataContractJsonSerializer(typeof(RunResume.Snapshot)).ReadObject(stream);}
 void LegacyLive(string family,RunResume.Snapshot expected,bool migrated)
 {
  var p=PlayerController.Instance;Require(p.playerHealth.maxHP==expected.maxHP&&p.playerHealth.maxSHP==expected.maxSoulHP,"native source rebuild retains the converted red/soul capacity: "+family);
  if(family=="buffs")
  {
   Require(p.GetComponentsInChildren<DamageUpOnReload>().Length==1&&p.GetComponentsInChildren<FireRateOnHurt>().Length==1&&p.GetComponentsInChildren<TemporaryRefreshingBuff>().Length==1,"legacy build reconstructs exactly one source owner for each recorded refreshing attachment");
   CombatClose(Bonus(p.stats[StatType.BulletDamage]),0,"legacy active damage boosts are omitted instead of importing their old aggregate/phase");
   var owner=p.GetComponentInChildren<TemporaryRefreshingBuff>();var saved=expected.runtime.sourcePowerups.SingleOrDefault(v=>v.key=="abby_perk_whirlwind_name");int count=saved==null?0:saved.values.Sum(v=>v.count);
   Require((int)WindField(owner,"_stacks")==count,"legacy/current reader uses only the source Whirlwind counter, without importing its old timer");
   var shields=p.GetComponentsInChildren<PreventDamage>();Require(shields.Length==1&&shields[0].isActive,"source shield restarts active rather than restoring the old recovering phase");CombatClose(shields[0].finalCooldown,10,"source repeated Stalwart arithmetic reconstructs the shield cooldown");
  }
  if(family=="soul")
  {
   Require(Object.FindObjectsOfType<StabbingSummon>().Length>0&&Object.FindObjectsOfType<OrbitalWeapon>().Length>0,"legacy Soul/Spear/Scythe acquisitions recreate actual source actors");
   var feed=AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidKillEffectRecipes"),"Existing").Invoke(null,new object[]{"feed_the_beasts_name"});
   CombatClose((float)WindField(feed,"Accumulated"),0,"unknown legacy unlimited Feed history rebuilds at source zero");
  }
  if(family=="d15"||family=="custom")
  {
   var horde=Object.FindObjectOfType<HordeSpawner>();CombatClose(horde.enemyDamage,LegacyFreshDamage,"legacy D10 history cannot overwrite the actual post-Start source damage");CombatClose(Object.FindObjectOfType<BossSpawner>().cooldownRate,.55f,"D15 reconstructs the source boss cooldown assignment");
   if(!migrated)Require(LegacyRecordedDamage==11&&horde.enemyDamage!=11,"derived old damage11 is discarded using the fresh source field, without inferring a timer/writer");
  }
  if(family=="custom")
  {
   int ammo=migrated?9:7,projectiles=migrated?8:7;
   Log("legacy-custom-operands",new{actualAmmo=p.gun.maxAmmo,actualProjectiles=p.gun.numOfProjectiles,hp=p.playerHealth.hp,canSave=RunResume.CanSave()});
   Require(CustomMode.Active&&p.gun.maxAmmo==ammo&&p.gun.numOfProjectiles==projectiles,"Custom Mini Clip/Rage/Fusillade use current unknown-history reconstruction or retained known operands");
   CombatClose(CustomMode.Get("perk:mini_clip_name:scale"),2,"legacy Mini Clip scale survives source reconstruction");
  }
  Log("legacy-source-live",new{family,migrated,weapon=p.gun.gunData.name,ammo=p.gun.maxAmmo,projectiles=p.gun.numOfProjectiles,damage=p.gun.GetProjectileRecipe().damage,maxSoul=p.playerHealth.maxSHP});
 }
 IEnumerator LegacyTakeChest(GameController game)
 {
  var chest=(ChestUIController)AccessTools.Property(typeof(GameState),"chestUIController").GetValue(game.CurrentState);var take=(UnityEngine.UI.Button)AccessTools.Field(typeof(ChestUIController),"takeButton").GetValue(chest);
  while(game.CurrentState is ChestState&&!take.IsInteractable())yield return null;
  if(game.CurrentState is ChestState)Click(take);while(game.CurrentState is ChestState)yield return null;
  Log("legacy-restored-chest","Actual persisted chest pickup reached the native menu; Take completes before continuing the firing workflow.");
 }
 IEnumerator LegacyNativeFire(GameController game)
 {
  var p=PlayerController.Instance;game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  while(!(game.CurrentState is CombatState)||!(p.CurrentState is flanne.Player.IdleState)||PauseController.isPaused)
  {if(game.CurrentState is ChestState)yield return LegacyTakeChest(game);else yield return null;}
  if(!p.ammo.fullOnAmmo)
  {InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.R));while(!p.ammo.fullOnAmmo){if(game.CurrentState is ChestState)yield return LegacyTakeChest(game);else yield return null;}InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());}
  while(!(p.CurrentState is flanne.Player.IdleState)||PauseController.isPaused)yield return null;
  int volleys=0;UnityEngine.Events.UnityAction fired=()=>volleys++;p.gun.OnShoot.AddListener(fired);LegacyPrimaryShots=0;LegacyCaptureShot=true;
  var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*5);InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));
  while(LegacyPrimaryShots==0)
  {if(game.CurrentState is ChestState){InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});yield return LegacyTakeChest(game);InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));}else yield return null;}
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});while(!(p.CurrentState is flanne.Player.IdleState)||!p.ammo.fullOnAmmo||!(game.CurrentState is CombatState)){if(game.CurrentState is ChestState)yield return LegacyTakeChest(game);else yield return null;}LegacyCaptureShot=false;p.gun.OnShoot.RemoveListener(fired);
  Require(volleys>0&&LegacyPrimaryShots==volleys*p.gun.numOfProjectiles,"actual native input after legacy/current rebuild emits the configured primary volleys and completes source reload");
 }
 IEnumerator LegacyReturn(GameController game,string name)
 {
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.pauseMenu.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="ResumeSaveAndReturn"));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,name+"-schema7.json"));var saved=RunResume.Read();Require(saved.version==7&&saved.runtime.sourcePowerups!=null&&saved.runtime.stats.Count==0&&saved.runtime.timed.Count==0&&saved.runtime.shields.Count==0,"real Save & return writes only the source schema/runtime families: "+name);
 }
 IEnumerator LegacyCustomInputs(GameController game)
 {
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.pauseMenu.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="CustomSettings"));Click(CustomButton("CustomTab1"));CustomValue(2,"7");CustomValue(4,"4");Click(CustomButton("CustomBack"));Click(game.pauseResumeButton);
  while(!(game.CurrentState is CombatState)||PauseController.isPaused)yield return null;
  Require(PlayerController.Instance.gun.maxAmmo==9&&PlayerController.Instance.gun.numOfProjectiles==8,"actual native Custom edits change the base to7/4 while retaining reconstructed Rage10/Fusillade3 operands");
  var wave=SelectedMap.MapData.spawnSessions.First(w=>!w.isElite);var before=Object.FindObjectsOfType<Health>().Select(h=>h.GetInstanceID()).ToArray();
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.pauseMenu.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="CustomSettings"));Click(CustomButton("CustomTab6"));
  string label=System.Text.RegularExpressions.Regex.Replace(wave.monsterPrefab.name.Replace("PF_","").Replace("(Clone)",""),"([a-z0-9])([A-Z])","$1 $2").Replace('_',' ').Trim();yield return CustomCommand("Spawn: "+label);
  var actor=Object.FindObjectsOfType<Health>().Single(h=>!before.Contains(h.GetInstanceID())&&h.name==wave.monsterPrefab.name);Require(actor.GetComponent<AIComponent>().damageToPlayer==(int)(LegacyFreshDamage*3),"native Custom source spawn applies factor3 to fresh D15 damage, not to discarded legacy11");ReturnFixtureActor(actor.gameObject);Click(CustomButton("CustomBack"));Click(game.pauseResumeButton);
 }
 IEnumerator CheckLegacySource(bool continuation)
 {
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").EndsWith("-codecs"))
  {
   byte[] before=File.ReadAllBytes(RunCheckpoint);int read=0;
   try
   {
    foreach(string path in File.ReadAllLines(Environment.GetEnvironmentVariable("MODERNIZATION_LEGACY_READER_INPUTS")).Where(p=>p.Length>0))
    {
     byte[] bytes=File.ReadAllBytes(path);File.WriteAllBytes(RunCheckpoint,bytes);var original=LegacyRaw(path);var rebuilt=RunResume.Read();
     Require(rebuilt!=null&&rebuilt.version==original.version&&rebuilt.runtime.sourcePowerups!=null&&rebuilt.runtime.stats.Count==0,"actual supported historical checkpoint enters the source reader: "+path);
     Require(bytes.SequenceEqual(File.ReadAllBytes(RunCheckpoint)),"source decoding preserves the historical file bytes: "+path);read++;
    }
   }
   finally{File.WriteAllBytes(RunCheckpoint,before);}
   Require(before.SequenceEqual(File.ReadAllBytes(RunCheckpoint)),"reader corpus check restores the isolated checkpoint exactly");SetPhase("PASS");Log("complete","Actual native source reader reconstructed "+read+" pinned historical checkpoints without rewriting their inputs; selected gameplay families retain separate native proof");Application.Quit(0);yield break;
  }
  if(continuation)
  {
   byte[] bytes=File.ReadAllBytes(RunCheckpoint);var saved=RunResume.Read();Require(saved.version==7&&saved.runtime.customRageAmmo.SequenceEqual(new[]{10})&&saved.runtime.customFusilladeProjectiles.SequenceEqual(new[]{3}),"fresh-process checkpoint contains the actual rebuilt source operands10/3");Require(bytes.SequenceEqual(File.ReadAllBytes(RunCheckpoint)),"current source read preserves exact checkpoint bytes");
   yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return null;LegacyLive("custom",saved,true);
   if(saved.chests>0&&game.CurrentState is CombatState)
   {var chest=Object.FindObjectsOfType<flanne.Pickups.ChestPickup>().First(c=>c.gameObject.activeInHierarchy);PlayerController.Instance.SetPosition(chest.transform.position);Physics2D.SyncTransforms();while(!(game.CurrentState is ChestState))yield return null;}
   yield return LegacyNativeFire(game);yield return LegacyReturn(game,"custom-fresh");
   yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();Require(!CustomMode.Active&&PlayerController.Instance.gun.maxAmmo==6&&PlayerController.Instance.gun.numOfProjectiles==1,"ordinary native restart clears legacy Custom rules/flat ammo/projectile history");yield return CheckRevolver(game,PlayerController.Instance,"legacy-source-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Exact migrated source checkpoint, retained Custom base operands, source D15 fields and real firing/reload/original restart");Application.Quit(0);yield break;
  }
  string folder=Environment.GetEnvironmentVariable("MODERNIZATION_LEGACY_CASES");
  var families=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("-custom-")?new[]{"custom"}:new[]{"buffs","soul","d15","custom"};
  foreach(string family in families)
  {
   SetPhase("legacy-source-"+family);string input=Path.Combine(folder,family+".schema3.json");byte[] bytes=File.ReadAllBytes(input);File.WriteAllBytes(RunCheckpoint,bytes);File.Copy(input,Path.Combine(ResultDir,family+"-input.json"));var original=LegacyRaw(input);var saved=RunResume.Read();
   Require(saved.version==3&&saved.runtime.sourcePowerups!=null&&saved.runtime.stats.Count==0&&saved.runtime.timed.Count==0&&saved.runtime.shields.Count==0,"old schema3 uses the selected source rebuild and drops aggregate/timed/shield phase records: "+family);Require(bytes.SequenceEqual(File.ReadAllBytes(RunCheckpoint)),"old source read leaves the owned input file byte-identical: "+family);
   Require(saved.maxSoulHP==original.maxSoulHP+3*(original.acquired.Count(a=>a.key=="soul_shield_name")-1),"old Windows soul baseline is converted once at the reader boundary");
   foreach(string id in new[]{"character:"+saved.character,"weapon:"+saved.weapon,"scenario:"+saved.map})if(!Progression.State.earned.Contains(id))Progression.State.earned.Add(id);Progression.Refresh();
   yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return null;LegacyLive(family,saved,false);
   if(family=="custom")yield return LegacyCustomInputs(game);yield return LegacyNativeFire(game);yield return LegacyReturn(game,family);
   if(family=="buffs"||family=="soul")
   {var current=RunResume.Read();yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return null;LegacyLive(family,current,true);yield return LegacyReturn(game,family+"-roundtrip");}
   if(family=="custom")
   {var current=RunResume.Read();Require(current.runtime.customRageAmmo.SequenceEqual(new[]{10})&&current.runtime.customFusilladeProjectiles.SequenceEqual(new[]{3}),"new source writer records the reconstructed pickup operands, not the later edited base");File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));}
  }
  SetPhase("PASS");Log("complete",families.Length==1?"Derived legacy Custom unknown-history/D15 source rebuild, actual native base edits/firing and recorded source operands":"Actual old buff/soul saves and explicit D15/ambiguous Custom reader cases rebuild through native Continue, real source firing, schema7 resave and current roundtrips");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(RunResume),"RestoreSpawners")]
class LegacySourceSpawnerProbe
{static void Prefix(RunResume.Snapshot saved){if(!RuntimeChecks.LegacyMode)return;RuntimeChecks.LegacyFreshDamage=Object.FindObjectOfType<HordeSpawner>().enemyDamage;RuntimeChecks.LegacyRecordedDamage=saved.hordeModifiers[3];}}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class LegacyPrimaryProbe
{static void Postfix(ProjectileRecipe __0,bool __4){if(RuntimeChecks.LegacyCaptureShot&&!__4&&__0.objectPoolTag==PlayerController.Instance.gun.gunData.bullet.name)RuntimeChecks.LegacyPrimaryShots++;}}
