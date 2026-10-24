using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.PowerupSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 int shieldPrevented,shieldBreaks;
 void ShieldPrevented()=>shieldPrevented++;
 void ShieldBroken()=>shieldBreaks++;
 static MonoBehaviour ShieldOwner(PreventDamage shield)=>shield.GetComponent<AndroidShieldLifecycle>();
 static void ShieldGateway(PlayerHealth health)
 {AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(health,new object[]{1});}
 IEnumerator ShieldSmite(PlayerController p,Health target,int expectedHP)
 {
  while(JusticeSmiteFlowProbe.Running)yield return null;
  Physics2D.SyncTransforms();p.ammo.Reload();p.ammo.UseAmmo(p.ammo.amount);
  while(target.HP==(expectedHP==0?1:10000)&&!target.isDead)yield return null;
  while(JusticeSmiteFlowProbe.Running)yield return null;
  Log("shield-smite-result",new{hp=target.HP,expectedHP});
  Require(target.HP==expectedHP,"actual last-ammo Smite producers deliver their full source damage");
 }
 IEnumerator CheckAndroidShieldOwners(bool continuation)
 {
  GameController game;PlayerController p;
  if(continuation)
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;
   Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))
   {
    var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));
    if(error!=null)throw new InvalidOperationException(error.text);yield return null;
   }
   p=PlayerController.Instance;GameTimer.SharedInstance.Stop();
  }
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();
   yield return AndroidPick(game,"HolyShield",game.playerXP.level);yield return AndroidPick(game,"HolyShield",game.playerXP.level);
  }
  StopFixtureSpawning();p.SetPosition(new Vector3(100,100,0));
  var shields=p.GetComponentsInChildren<PreventDamage>();
  Require(shields.Length==(continuation?3:2),"each reached repeated Holy Shield application creates a distinct source shield owner");
  while(shields.Any(s=>s.cooldownRate==null))yield return null;
  var first=ShieldOwner(shields[0]);var second=ShieldOwner(shields[1]);
  if(continuation)
  {
   var wrath=p.GetComponentsInChildren<ThunderDuringHolyShield>();
   while(wrath.Any(w=>!ShieldWrathInitialization.Timers.ContainsKey(w.GetInstanceID())))yield return null;
   Require(wrath.Length==2&&wrath.All(w=>ShieldWrathInitialization.Timers[w.GetInstanceID()]==0),"Continue recreates independent source-unsaved Wrath clocks at zero");foreach(var w in wrath)w.enabled=false;
   var continuedSmites=p.GetComponentsInChildren<SmitePassive>();while(continuedSmites.Any(s=>WindField(s,"ammo")==null))yield return null;
   Require(continuedSmites.Length==2,"Continue reconstructs both full-strength source Holy Arts producers");
   Require(shields.All(s=>s.isActive&&(int)WindField(ShieldOwner(s),"Count")==1&&(int)WindField(ShieldOwner(s),"LastCount")==1&&(float)WindField(ShieldOwner(s),"ApplyAt")==-1&&(float)WindField(ShieldOwner(s),"BreakUntil")==-1),"Continue recreates active source shield owners without unsaved phases or the fixture-only configured hit count");
   Require(shields[0].finalCooldown==10&&shields.Skip(1).All(s=>s.finalCooldown==120)&&Mathf.Abs(Bonus(p.stats[StatType.MoveSpeed])-.5f)<.001f,"Continue replays first-shield-only Stalwart and repeated Blessing upgrades");
   Require(p.playerHealth.isProtected&&p.playerHealth.isInvincible.flips==3,"Continue has exactly three real shield invulnerability contributions");
   int hp=p.playerHealth.hp;ShieldGateway(p.playerHealth);
   Require(p.playerHealth.hp==hp&&shields.All(s=>s.isActive)&&p.playerHealth.isInvincible.flips==3,"continued shields protect throughout the source half-second break");
   while(shields.Any(s=>s.isActive))yield return null;
   Require(!p.playerHealth.isProtected&&p.playerHealth.isInvincible.flips==0&&Mathf.Abs(Bonus(p.stats[StatType.MoveSpeed]))<.001f,"every continued source shield removes its own protection and first-shield Blessings on break");
   var continuedPool=ObjectPooler.SharedInstance;continuedPool.AddObject("Validation_Shield_Continued_Enemy",continuedPool.itemDictionary["BrainMonster"].objectToPool,1);
   var victim=continuedPool.GetPooledObject("Validation_Shield_Continued_Enemy");var victimHealth=DurableEnemy(victim,p.transform.position+Vector3.right,false);victim.tag="Enemy";Physics2D.SyncTransforms();
   float deadline=(float)WindField(first,"ApplyAt"),otherDeadline=(float)WindField(second,"ApplyAt");
   yield return ShieldSmite(p,victimHealth,9960);
   Require((float)WindField(first,"ApplyAt")==deadline&&(float)WindField(second,"ApplyAt")==otherDeadline,"corrected surviving Smite does not publish a false kill or advance either shield deadline");ReturnFixtureActor(victim);
   ShieldGateway(p.playerHealth);Require(p.playerHealth.hp==hp-1,"unprotected continued source health gateway deals ordinary damage");
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
   Require(p.GetComponentsInChildren<PreventDamage>().Length==0&&!p.playerHealth.isProtected&&p.GetComponentsInChildren<SmitePassive>().Length==0,"original restart clears all shield/Smite owners and counted protection");yield return CheckRevolver(game,p,"android-shield-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Source repeated shields, actual damage/Smite, first-shield upgrades and unsaved-phase reconstruction survive Continue and restart");Application.Quit(0);yield break;
  }
  Require(shields.All(s=>s.cooldownTime==120&&s.finalCooldown==120&&Bonus(s.cooldownRate)==0),"repeated Holy Shield preserves each source base cooldown instead of adding a native cooldown-rate bonus");
  foreach(string name in new[]{"DivineBlessing","DivineBlessing","DivineWrath","DivineWrath","StalwartShield","StalwartShield","StalwartShield"})yield return AndroidPick(game,name,game.playerXP.level);
  Require(shields[0].finalCooldown==10&&shields[1].finalCooldown==120&&Mathf.Abs(Bonus(p.stats[StatType.MoveSpeed])-.5f)<.001f,"Stalwart targets the first shield only and both repeated Blessing helpers follow that first active shield");
  var wraths=p.GetComponentsInChildren<ThunderDuringHolyShield>();while(wraths.Any(w=>!ShieldWrathInitialization.Timers.ContainsKey(w.GetInstanceID())))yield return null;Require(wraths.Length==2&&wraths.All(w=>ShieldWrathInitialization.Timers[w.GetInstanceID()]==0),"repeated Divine Wrath creates independent source clocks initialized at zero");
  p.SetPosition(new Vector3(100,100,0));var pool=ObjectPooler.SharedInstance;var template=pool.itemDictionary["BrainMonster"].objectToPool;
  pool.AddObject("Validation_Shield_Enemy",template,2);var eligible=pool.GetPooledObject("Validation_Shield_Enemy");var eligibleHealth=DurableEnemy(eligible,p.transform.position+new Vector3(8.5f,4.5f),false);eligible.tag="Untagged";eligible.SetActive(true);
  var outside=pool.GetPooledObject("Validation_Shield_Enemy");DurableEnemy(outside,p.transform.position+Vector3.right*9.1f,false);outside.GetComponent<CircleCollider2D>().radius=.8f;outside.SetActive(true);
  var unpooled=DurableEnemy(template,p.transform.position+Vector3.left,true);unpooled.gameObject.SetActive(true);
  int thunderCalls=HitEffectThunderCalls;foreach(var wrath in wraths)Plugin.SetField(wrath,"_timer",.97f);
  while(HitEffectThunderCalls<thunderCalls+2)yield return null;
  Require(HitEffectThunderTarget==eligible,"actual repeated Divine Wrath producers choose a pooled untagged monster inside strict source bounds, excluding tagged unpooled and outside-center candidates");
  Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
  Require(eligibleHealth.HP==9956,"both real Divine Wrath casts deliver their source twenty-two damage through the reconstructed lightning collision backend");
  foreach(var wrath in wraths)wrath.enabled=false;ReturnFixtureActor(outside);unpooled.gameObject.SetActive(false);Object.Destroy(unpooled.gameObject);ReturnFixtureActor(eligible);
  p.playerHealth.isInvincible.UnFlip();int beforeHp=p.playerHealth.hp,invincibleBefore=p.playerHealth.isInvincible.flips;
  p.playerHealth.onDamagePrevented.AddListener(ShieldPrevented);foreach(var shield in shields)shield.OnDamagePrevented.AddListener(ShieldBroken);
  eligible=pool.GetPooledObject("Validation_Shield_Enemy");eligibleHealth=DurableEnemy(eligible,p.transform.position+Vector3.right*.25f,false);eligible.tag="Enemy";
  Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();while(shieldPrevented==0)yield return null;ReturnFixtureActor(eligible);
  Require(p.playerHealth.hp==beforeHp&&p.playerHealth.isProtected&&shields.All(s=>s.isActive)&&p.playerHealth.isInvincible.flips==invincibleBefore,"real native physics prevention preserves counted protection and active shield flags throughout the source half-second break without a new invincibility flip");
  ShieldGateway(p.playerHealth);
  Require(shieldBreaks==2&&(int)WindField(first,"LastCount")==1-shieldPrevented&&(int)WindField(second,"LastCount")==1-shieldPrevented,"repeated prevention notifications decrement each source hit counter but start only one break routine per owner");
  Require(Mathf.Abs(Bonus(p.stats[StatType.MoveSpeed]))<.001f&&shields.All(s=>!s.transform.Find("ShieldSprite").gameObject.activeSelf),"source break events remove both first-shield Blessing bonuses and hide each native shield sprite");
  while(shields.Any(s=>s.isActive))yield return null;
  Require(!p.playerHealth.isProtected&&p.playerHealth.isInvincible.flips==0,"each completed source break removes exactly its own shield invulnerability contribution");
  ShieldGateway(p.playerHealth);Require(p.playerHealth.hp==beforeHp-1,"actual unprotected health gateway deals ordinary damage");
  // Explicit game-clock/deadline seeds isolate the strict source comparison.
  Plugin.SetField(GameTimer.SharedInstance,"<timer>k__BackingField",1f);Plugin.SetField(first,"ApplyAt",2f);Plugin.SetField(second,"ApplyAt",121f);
  yield return AndroidPick(game,"SwordAndShield",game.playerXP.level);
  SetPhase("shield-smite-owner");Log("smite-owner",string.Join(";",Object.FindObjectsOfType<SmitePassive>().Select(s=>s.name+" parent="+s.transform.parent?.name+" enabled="+s.enabled+" ammo="+(WindField(s,"ammo")!=null))));
  var smite=p.GetComponentInChildren<SmitePassive>();Require(smite!=null,"actual Holy Arts acquisition attaches its source Smite producer");while(WindField(smite,"ammo")==null)yield return null;
  eligible=pool.GetPooledObject("Validation_Shield_Enemy");eligible.tag="Enemy";eligibleHealth=DurableEnemy(eligible,p.transform.position+Vector3.right,false);Plugin.SetField(eligibleHealth,"<HP>k__BackingField",1);Physics2D.SyncTransforms();
  Require(eligibleHealth.HP==1&&Physics2D.OverlapCircleAll(smite.transform.position,(float)WindField(smite,"range"),1<<(int)TagLayerUtil.Enemy).Any(c=>c.gameObject==eligible),"lethal Smite fixture is active at one HP inside the actual native overlap query");
  SetPhase("shield-native-smite");yield return ShieldSmite(p,eligibleHealth,0);
  Require((float)WindField(first,"ApplyAt")==1&&(float)WindField(second,"ApplyAt")==121&&!shields[0].isActive,"an actual native lethal Smite advances only the first deadline to exact equality without prematurely recharging a stopped clock");
  SetPhase("shield-game-clock-recharge");GameTimer.SharedInstance.Start();while(!shields[0].isActive)yield return null;GameTimer.SharedInstance.Stop();
  Require(!shields[1].isActive&&p.playerHealth.isProtected&&Mathf.Abs(Bonus(p.stats[StatType.MoveSpeed])-.5f)<.001f,"actual game-clock advancement recharges the first shield and its Blessing helpers independently of the second");
  yield return AndroidPick(game,"HolyArts",game.playerXP.level);var smites=p.GetComponentsInChildren<SmitePassive>();while(smites.Any(s=>WindField(s,"ammo")==null))yield return null;
  Require(smites.Length==2&&smites.All(s=>(int)WindField(s,"baseDamage")==20&&(float)WindField(s,"range")==4&&WindField(s,"soundFX")==null),"repeated native Holy Arts attaches a second full-strength radius-four source producer with its serialized silent sound field");
  eligible=pool.GetPooledObject("Validation_Shield_Enemy");eligible.tag="Enemy";eligibleHealth=DurableEnemy(eligible,p.transform.position+Vector3.right,false);yield return ShieldSmite(p,eligibleHealth,9960);ReturnFixtureActor(eligible);
  yield return AndroidPick(game,"HolyShield",game.playerXP.level);shields=p.GetComponentsInChildren<PreventDamage>();while(shields.Length<3||shields.Any(s=>s.cooldownRate==null)){shields=p.GetComponentsInChildren<PreventDamage>();yield return null;}
  var last=ShieldOwner(shields[2]);Plugin.SetField(last,"Count",2);Plugin.SetField(last,"LastCount",2);
  Log("fixture-precondition","Explicit source configured-hit-count seed on the third owner exercises the recovered count contract; no claim that a current menu upgrade grants this count");
  ShieldGateway(p.playerHealth);
  Require((float)WindField(first,"BreakUntil")>Time.time&&shields[2].isActive&&(int)WindField(last,"LastCount")==1,"the source count contract leaves the third owner active while the first enters another break and the second keeps recovering");
  p.playerHealth.onDamagePrevented.RemoveListener(ShieldPrevented);foreach(var shield in shields)shield.OnDamagePrevented.RemoveListener(ShieldBroken);
  Plugin.SetField(wraths[0],"_timer",.27f);Plugin.SetField(wraths[1],"_timer",.67f);
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return Capture("android-shield-mixed-source-phases.png");
  Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Source repeated shield owners, actual physics/damage/Smite, first-shield buffs/cooldowns and mixed unsaved-phase checkpoint");Application.Quit(0);
 }
}

[HarmonyPatch(typeof(ThunderDuringHolyShield),"Start")]
class ShieldWrathInitialization
{
 internal static readonly Dictionary<int,float> Timers=new Dictionary<int,float>();
 static void Postfix(ThunderDuringHolyShield __instance)=>Timers[__instance.GetInstanceID()]=(float)AccessTools.Field(typeof(ThunderDuringHolyShield),"_timer").GetValue(__instance);
}
