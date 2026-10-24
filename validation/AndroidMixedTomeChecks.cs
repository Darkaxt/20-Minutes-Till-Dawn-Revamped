using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool MixedTomeMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-mixed-tomes-");
 internal static readonly List<DamageType> MixedHolyHeals=new List<DamageType>();
 internal static bool MixedSmiteActive;
 internal static readonly List<float> MixedGlassApplications=new List<float>();
 internal static readonly HashSet<int> MixedDestroyedAssets=new HashSet<int>();
 int mixedNominalSmite,mixedPlayerHurts;
 void MixedSmite(object sender,object args)=>mixedNominalSmite++;
 void MixedHurt()=>mixedPlayerHurts++;
 static Dictionary<string,float> MixedAssetValues()=>Resources.FindObjectsOfTypeAll<AndroidDamageTomeAsset>().Where(a=>StatusSubscribed(a)).ToDictionary(a=>a.Key,a=>a.Damage);
 IEnumerator MixedPick(GameController game,string key)
 {
  var pool=(List<PowerupPoolItem>)WindField(game.powerupGenerator,"devilPool");yield return NativeTomePick(game,false,pool.Single(x=>RunResume.Key(x.powerup)==key).powerup.name);
 }
 IEnumerator MixedDamage(PlayerController p,int glass=6)
 {
  var values=MixedAssetValues();Log("mixed-asset-values",string.Join(";",values.Select(p=>p.Key+"="+p.Value)));Require(values.Count==3&&values["tome_of_glass_name"]==glass,"three original-asset tome listeners register once, with source-owned Glass factor"+glass);
  CombatClose(p.stats[StatType.MoveSpeed].Modify(1),1.2f,"actual Haste/source replay restores one20% movement bonus");
  Require((bool)WindField(p.playerHealth.GetComponent<AndroidPlayerCollision>(),"Fragile"),"actual Glass acquisition/source replay enables the source fragile player branch");
  var target=StatusEnemy();float movement=p.stats[StatType.MoveSpeed].Modify(1);int hp=target.HP;target.TakeDamage(DamageType.Bullet,20);int expected=-Mathf.FloorToInt((-20*.85f+2)*glass*movement);
  Require(hp-target.HP==expected,"two Wind/Might picks remain singular callbacks while source Glass composes signed Bullet damage");Log("mixed-tome-damage","input=-20 damage="+(hp-target.HP)+" movement="+movement+" glass="+values["tome_of_glass_name"]);
  hp=target.HP;target.HPChange(7);expected=Mathf.FloorToInt((7*.85f+2)*glass*movement);Require(target.HP-hp==expected,"repeated mixed tome source owners also compose positive health changes once");
  p.stats[StatType.MoveSpeed].AddMultiplierBonus(-movement);hp=target.HP;target.TakeDamage(DamageType.Summon,20);Require(hp-target.HP==-Mathf.FloorToInt((-20*.85f+2)*glass),"zero movement retains Wind0.85/Might2/source Glass while omitting the later multiplier");p.stats[StatType.MoveSpeed].AddMultiplierBonus(movement);yield return StatusReturn(target);
 }
 IEnumerator MixedAngelic(PlayerController p,bool protectedTarget,int glass=6)
 {
  while(JusticeSmiteFlowProbe.Running)yield return null;
  var helpers=p.GetComponentsInChildren<HealOnSmiteKill>();foreach(var helper in helpers)Plugin.SetField(helper,"_killCounter",499);
  // Glass makes ordinary damage lethal. This explicit health precondition
  // exercises the real Smite/Angelic healing path, not a survived Glass hit.
  p.playerHealth.hp=p.playerHealth.maxHP-2;int before=p.playerHealth.hp;Require(before>0,"Angelic fixture seeds living injured red HP without claiming a survived Glass hit");
  var target=StatusEnemy();target.transform.position=p.transform.position+Vector3.right*2;if(protectedTarget)target.isInvincible.Flip();Physics2D.SyncTransforms();int hp=target.HP,events=mixedNominalSmite;MixedHolyHeals.Clear();
  int input=20+before*10*p.GetComponentsInChildren<SmiteHPDamageScaling>().Length;int expected=protectedTarget?0:-Mathf.FloorToInt((-input*.85f+2)*glass*p.stats[StatType.MoveSpeed].Modify(1));
  MixedSmiteActive=true;p.ammo.Reload();p.ammo.UseAmmo(p.ammo.amount);while(mixedNominalSmite==events)yield return null;while(JusticeSmiteFlowProbe.Running)yield return null;MixedSmiteActive=false;
  Require(mixedNominalSmite==events+1&&!target.isDead&&hp-target.HP==expected,"actual "+(protectedTarget?"protected":"surviving")+" Smite emits one nominal kill event after mixed-tome damage");
  Require(helpers.All(h=>(int)WindField(h,"_killCounter")==0)&&p.playerHealth.hp==before+2&&MixedHolyHeals.SequenceEqual(new[]{DamageType.Smite,DamageType.Smite}),"both500th-event Angelic helpers reset and heal one Holy heart without Glass/Might multiplying player healing");
  Log("mixed-angelic","protected="+protectedTarget+" source-input="+input+" npc-damage="+(hp-target.HP)+" player="+before+"->"+p.playerHealth.hp+" holy-heals="+MixedHolyHeals.Count);
  if(protectedTarget)target.isInvincible.UnFlip();yield return StatusReturn(target);
 }
 IEnumerator MixedFragileDeathAndRestart(GameController game)
 {
  var p=PlayerController.Instance;if(!p.playerHealth.isInvincible.value)p.playerHealth.isInvincible.Flip();int hp=p.playerHealth.hp;mixedPlayerHurts=0;p.playerHealth.onHurt.AddListener(MixedHurt);
  p.playerHealth.maxSHP=1;p.playerHealth.shp=1;AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(p.playerHealth,new object[]{1});
  Require(p.playerHealth.hp==hp&&p.playerHealth.shp==1&&mixedPlayerHurts==0,"real invulnerability rejection precedes Glass fragile death and soul-heart absorption");
  p.playerHealth.isInvincible.UnFlip();Require(!p.playerHealth.isInvincible.value,"fixture has no remaining protection contribution before actual fragile damage");AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(p.playerHealth,new object[]{1});
  Require(p.playerHealth.hp==0&&p.playerHealth.shp==1&&mixedPlayerHurts==0,"actual Glass hit kills red health before consuming soul hearts or invoking hurt callbacks");
  while(!(game.CurrentState is PlayerDeadState))yield return null;p.playerHealth.onHurt.RemoveListener(MixedHurt);
  Button back=null;while(back==null||!back.IsInteractable()){back=Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(b=>b.name=="ReturnToMenuButton"&&b.gameObject.activeInHierarchy);yield return null;}Click(back);
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();
  Require(p.GetComponentsInChildren<HealOnSmiteKill>().Length==0&&p.GetComponentsInChildren<SmitePassive>().Length==0&&!(bool)WindField(p.playerHealth.GetComponent<AndroidPlayerCollision>(),"Fragile")&&MixedAssetValues().Count==0,"normal restart removes mixed original-asset listeners, fragile state and Angelic/Smite owners");
  var target=StatusEnemy();target.TakeDamage(DamageType.Bullet,20);Require(target.HP==99980,"actual NPC damage after restart has no retained Wind/Might/Glass modifier");yield return StatusReturn(target);yield return CheckRevolver(game,p,"android-mixed-tomes-original-restart.png");yield return AchievementExit(game,false);
 }
 IEnumerator CheckAndroidMixedTomes(string mode)
 {
  bool continuation=mode.EndsWith("continue"),last=mode.Contains("-repeat-"),warm=mode.Contains("-warm-");GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
   foreach(string name in new[]{"Haste","Angelic","Angelic"}){Plugin.SetField(game.playerXP,"<level>k__BackingField",1);Plugin.SetField(game.playerXP,"xp",0f);yield return AndroidPick(game,name,game.playerXP.level);}
   foreach(string key in new[]{"tome_of_wind_name","tome_of_wind_name","tome_of_might_name","tome_of_might_name","tome_of_glass_name","tome_of_glass_name","tome_of_glass_name"})yield return MixedPick(game,key);
  }
  var p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();p.SetPosition(new Vector3(100,100,0));
  Log("fixture-precondition","Native repeated Haste/Angelic/tome acquisition or exact source Continue; no runes, controlled progression/XP, stopped scheduler and source-pooled target geometry. Angelic counters are seeded just before500; real ammo-zero Smite performs the actual event/healing. No natural500-kill or full-run claim.");
  var helpers=p.GetComponentsInChildren<HealOnSmiteKill>();while(helpers.Any(h=>!StatusSubscribed(h)))yield return null;Require(helpers.Length==2&&helpers.All(h=>(int)WindField(h,"_killCounter")==0),"source native acquisition/Continue creates two Angelic owners with no saved partial counts");
  this.AddObserver(MixedSmite,SmitePassive.SmiteKillNotification);yield return MixedDamage(p);yield return MixedAngelic(p,false);yield return MixedAngelic(p,true);
  if(warm)
  {
   int glassId=Resources.FindObjectsOfTypeAll<AndroidDamageTomeAsset>().Single(a=>a.Key=="tome_of_glass_name"&&StatusSubscribed(a)).GetInstanceID(),applies=MixedGlassApplications.Count;
   Plugin.SetField(helpers[0],"_killCounter",233);Plugin.SetField(helpers[1],"_killCounter",497);
   PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="ResumeSaveAndReturn"));
   while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
   File.Copy(RunCheckpoint,Path.Combine(ResultDir,"warm-saved-run.json"));File.Copy(RunCheckpoint,Path.Combine(ResultDir,"warm-input-run.json"));Log("mixed-warm-continue","Actual native Save & return followed by Continue in the same player process; no asset retention/unload operation is forced.");
   yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();p.SetPosition(new Vector3(100,100,0));helpers=p.GetComponentsInChildren<HealOnSmiteKill>();
   while(helpers.Any(h=>!StatusSubscribed(h)))yield return null;Require(helpers.Length==2&&helpers.All(h=>(int)WindField(h,"_killCounter")==0),"same-process source Continue also omits Angelic partial counters without duplicating owners");
   Require(Resources.FindObjectsOfTypeAll<AndroidDamageTomeAsset>().Single(a=>a.Key=="tome_of_glass_name"&&StatusSubscribed(a)).GetInstanceID()==glassId&&!MixedDestroyedAssets.Contains(glassId)&&MixedGlassApplications.Skip(applies).SequenceEqual(new[]{8f,10f,12f}),"same-process retained original asset has no destroy/reset and source replay increments6->8->10->12");
   Log("mixed-retained-glass","owner="+glassId+" no-destroy=true replay=6->8->10->12; original ClearData resets level only");
   this.AddObserver(MixedSmite,SmitePassive.SmiteKillNotification);yield return MixedDamage(p,12);yield return MixedAngelic(p,false,12);yield return MixedAngelic(p,true,12);last=true;
  }
  if(!last)
  {
   Plugin.SetField(helpers[0],"_killCounter",continuation?233:137);Plugin.SetField(helpers[1],"_killCounter",continuation?497:499);Log("mixed-counter-save","partial="+string.Join(",",helpers.Select(h=>WindField(h,"_killCounter")))+" glass="+MixedAssetValues()["tome_of_glass_name"]);
   yield return DragonSuspend(game,"Repeated Wind/Might/Glass original-asset ownership and full signed/positive/zero-movement composition, two actual Angelic Holy heals on surviving/protected nominal Smite events and partial-counter source checkpoint");yield break;
  }
  this.RemoveObserver(MixedSmite,SmitePassive.SmiteKillNotification);yield return MixedFragileDeathAndRestart(game);SetPhase("PASS");Log("complete","Mixed tome repeated native acquisition, two exact source Continues, source Angelic counter omission/Holy healing, real Glass damage ordering/death and ordinary restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(AndroidPlayerCollision),"Change")]
class MixedHolyHealProbe
{static void Prefix(int change,DamageType kind){if(RuntimeChecks.MixedTomeMode&&RuntimeChecks.MixedSmiteActive&&change>0)RuntimeChecks.MixedHolyHeals.Add(kind);}}
[HarmonyPatch(typeof(AndroidDamageTomeAsset),"Apply")]
class MixedAssetApplyTrace
{
 static void Prefix(AndroidDamageTomeAsset __instance){if(RuntimeChecks.MixedTomeMode)RuntimeChecks.Log("mixed-asset-apply-before","id="+__instance.GetInstanceID()+" key="+__instance.Key+" damage="+__instance.Damage);}
 static void Postfix(AndroidDamageTomeAsset __instance){if(RuntimeChecks.MixedTomeMode){RuntimeChecks.Log("mixed-asset-apply-after","id="+__instance.GetInstanceID()+" key="+__instance.Key+" damage="+__instance.Damage);if(__instance.Key=="tome_of_glass_name")RuntimeChecks.MixedGlassApplications.Add(__instance.Damage);}}
}
[HarmonyPatch(typeof(AndroidDamageTomeAsset),"OnDestroy")]
class MixedAssetDestroyTrace
{static void Prefix(AndroidDamageTomeAsset __instance){if(RuntimeChecks.MixedTomeMode){RuntimeChecks.Log("mixed-asset-destroy","id="+__instance.GetInstanceID()+" key="+__instance.Key+" damage="+__instance.Damage);RuntimeChecks.MixedDestroyedAssets.Add(__instance.GetInstanceID());}}}
