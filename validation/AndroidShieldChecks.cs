using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
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
 static MonoBehaviour ShieldOwner(PreventDamage shield)=>shield.GetComponents<MonoBehaviour>().Single(c=>c.GetType().Name=="AndroidShieldLifecycle");
 static void ShieldGateway(PlayerHealth health)
 {var enemy=new GameObject("ShieldGatewayFixture");enemy.tag="Enemy";AccessTools.Method(typeof(PlayerHealth),"CheckCollision").Invoke(health,new object[]{enemy});Object.Destroy(enemy);}
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
   game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;
   yield return AndroidPick(game,"HolyShield",game.playerXP.level);yield return AndroidPick(game,"HolyShield",game.playerXP.level);
  }
  Object.FindObjectOfType<HordeSpawner>().enabled=false;
  var shields=p.GetComponentsInChildren<PreventDamage>();
  Require(shields.Length==(continuation?3:2),"each reached repeated Holy Shield application creates a distinct source shield owner");
  while(shields.Any(s=>s.cooldownRate==null))yield return null;
  var first=ShieldOwner(shields[0]);var second=ShieldOwner(shields[1]);
  if(continuation)
  {
   var third=ShieldOwner(shields[2]);var wrath=p.GetComponentsInChildren<ThunderDuringHolyShield>();
   Require(p.GetComponentsInChildren<SmitePassive>().Length==2,"Continue reconstructs both full-strength source Holy Arts producers from ordered acquisitions");
   Require(shields[0].isActive&&!shields[1].isActive&&shields[2].isActive&&(float)WindField(first,"BreakUntil")>Time.time,"Continue restores a breaking first shield, a recovering second shield and an active third shield");
   Require((int)WindField(third,"Count")==2&&(int)WindField(third,"LastCount")==1&&(float)WindField(second,"ApplyAt")==121&&shields[0].finalCooldown==10&&shields[1].finalCooldown==120,"Continue restores distinct hit counts, reduction and absolute game-clock deadline without broadcasting first-shield upgrades");
   Require(wrath.Length==2&&Mathf.Abs((float)WindField(wrath[0],"_timer")-.27f)<.1f&&Mathf.Abs((float)WindField(wrath[1],"_timer")-.67f)<.1f,"both source Divine Wrath clocks persist independently");foreach(var w in wrath)w.enabled=false;
   while(shields[0].isActive)yield return null;
   Require(p.playerHealth.isProtected&&Mathf.Abs(Bonus(p.stats[StatType.MoveSpeed]))<.001f,"the third shield retains protection after the first breaks while first-shield Blessing stays inactive");
   var continuedPool=ObjectPooler.SharedInstance;var continuedTemplate=continuedPool.itemDictionary.Values.First(i=>i.objectToPool.CompareTag("Enemy")&&i.objectToPool.GetComponent<Health>()!=null&&i.objectToPool.GetComponent<MoveComponent2D>()!=null).objectToPool;
   continuedPool.AddObject("Validation_Shield_Continued_Enemy",continuedTemplate,1);var victim=continuedPool.GetPooledObject("Validation_Shield_Continued_Enemy");var victimHealth=DurableEnemy(victim,p.transform.position+Vector3.right,false);victim.SetActive(true);Physics2D.SyncTransforms();
   float deadline=(float)WindField(first,"ApplyAt");p.ammo.UseAmmo(p.ammo.amount);while(victimHealth.HP==10000)yield return null;yield return null;
   Require(victimHealth.HP==9960&&(float)WindField(first,"ApplyAt")==deadline-2&&(float)WindField(second,"ApplyAt")==121,"both continued full-strength Smite producers notify Sword and Shield even on a surviving target, advancing only the first shield twice as in source");victim.SetActive(false);
   while(p.playerHealth.isInvincible.value)yield return null;
   int hp=p.playerHealth.hp,invincible=p.playerHealth.isInvincible.flips;ShieldGateway(p.playerHealth);
   Require(p.playerHealth.hp==hp&&p.playerHealth.isProtected&&p.playerHealth.isInvincible.flips==invincible,"the remaining counted shield prevents damage during its break phase without adding an invincibility flip");
   while(shields[2].isActive)yield return null;
   Require(!p.playerHealth.isProtected,"protection clears only after every contributing shield has completed its source break phase");
   ShieldGateway(p.playerHealth);Require(p.playerHealth.hp==hp-1,"an unprotected native collision still deals ordinary damage");
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
   Require(p.GetComponentsInChildren<PreventDamage>().Length==0&&!p.playerHealth.isProtected,"original restart clears all shield owners and counted protection");yield return CheckRevolver(game,p,"android-shield-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Android shield mixed-phase counts/deadlines/Wrath clocks survive fresh-process Continue and clear on original restart");Application.Quit(0);yield break;
  }
  Require(shields.All(s=>s.cooldownTime==120&&s.finalCooldown==120&&Bonus(s.cooldownRate)==0),"repeated Holy Shield preserves each source base cooldown instead of adding a native cooldown-rate bonus");
  foreach(string name in new[]{"DivineBlessing","DivineBlessing","DivineWrath","DivineWrath","StalwartShield","StalwartShield","StalwartShield"})yield return AndroidPick(game,name,game.playerXP.level);
  Require(shields[0].finalCooldown==10&&shields[1].finalCooldown==120&&Mathf.Abs(Bonus(p.stats[StatType.MoveSpeed])-.5f)<.001f,"Stalwart targets the first shield only and both repeated Blessing helpers follow that first active shield");
  var wraths=p.GetComponentsInChildren<ThunderDuringHolyShield>();Require(wraths.Length==2,"repeated Divine Wrath retains two actual native producers");
  p.SetPosition(new Vector3(100,100,0));var pool=ObjectPooler.SharedInstance;var template=pool.itemDictionary.Values.First(i=>i.objectToPool.CompareTag("Enemy")&&i.objectToPool.GetComponent<Health>()!=null&&i.objectToPool.GetComponent<MoveComponent2D>()!=null&&!i.objectToPool.GetComponent<MoveComponent2D>().knockbackImmune).objectToPool;
  pool.AddObject("Validation_Shield_Enemy",template,2);var eligible=pool.GetPooledObject("Validation_Shield_Enemy");var eligibleHealth=DurableEnemy(eligible,p.transform.position+new Vector3(8.5f,4.5f),false);eligible.tag="Untagged";eligible.SetActive(true);
  var outside=pool.GetPooledObject("Validation_Shield_Enemy");DurableEnemy(outside,p.transform.position+Vector3.right*9.1f,false);outside.GetComponent<CircleCollider2D>().radius=.8f;outside.SetActive(true);
  var unpooled=DurableEnemy(template,p.transform.position+Vector3.left,true);unpooled.gameObject.SetActive(true);
  int thunderCalls=HitEffectThunderCalls;foreach(var wrath in wraths)Plugin.SetField(wrath,"_timer",.97f);
  while(HitEffectThunderCalls<thunderCalls+2)yield return null;
  Require(HitEffectThunderTarget==eligible,"actual repeated Divine Wrath producers choose a pooled untagged monster inside strict source bounds, excluding tagged unpooled and outside-center candidates");
  Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
  Require(eligibleHealth.HP==9956,"both real Divine Wrath casts deliver their source twenty-two damage through the reconstructed lightning collision backend");
  foreach(var wrath in wraths)wrath.enabled=false;outside.SetActive(false);unpooled.gameObject.SetActive(false);eligible.SetActive(false);
  p.playerHealth.isInvincible.UnFlip();int beforeHp=p.playerHealth.hp,invincibleBefore=p.playerHealth.isInvincible.flips;
  p.playerHealth.onDamagePrevented.AddListener(ShieldPrevented);foreach(var shield in shields)shield.OnDamagePrevented.AddListener(ShieldBroken);
  eligible.tag="Enemy";DurableEnemy(eligible,p.transform.position+Vector3.right*.25f,false);eligible.SetActive(true);
  Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();while(shieldPrevented==0)yield return null;eligible.SetActive(false);
  Require(p.playerHealth.hp==beforeHp&&p.playerHealth.isProtected&&shields.All(s=>s.isActive)&&p.playerHealth.isInvincible.flips==invincibleBefore,"real native physics prevention preserves counted protection and active shield flags throughout the source half-second break without a new invincibility flip");
  ShieldGateway(p.playerHealth);
  Require(shieldBreaks==2&&(int)WindField(first,"LastCount")==1-shieldPrevented&&(int)WindField(second,"LastCount")==1-shieldPrevented,"repeated prevention notifications decrement each source hit counter but start only one break routine per owner");
  Require(Mathf.Abs(Bonus(p.stats[StatType.MoveSpeed]))<.001f&&shields.All(s=>!s.transform.Find("ShieldSprite").gameObject.activeSelf),"source break events remove both first-shield Blessing bonuses and hide each native shield sprite");
  while(shields.Any(s=>s.isActive))yield return null;
  Require(!p.playerHealth.isProtected&&p.playerHealth.isInvincible.flips==invincibleBefore,"each completed break unflips its own protection contribution, preserving the unrelated invincibility owner");
  // Explicit game-clock/deadline seeds isolate the strict source comparison.
  Plugin.SetField(GameTimer.SharedInstance,"<timer>k__BackingField",1f);Plugin.SetField(first,"ApplyAt",2f);Plugin.SetField(second,"ApplyAt",121f);
  yield return AndroidPick(game,"SwordAndShield",game.playerXP.level);
  SetPhase("shield-smite-owner");Log("smite-owner",string.Join(";",Object.FindObjectsOfType<SmitePassive>().Select(s=>s.name+" parent="+s.transform.parent?.name+" enabled="+s.enabled+" ammo="+(WindField(s,"ammo")!=null))));
  var smite=p.GetComponentInChildren<SmitePassive>();Require(smite!=null,"actual Holy Arts acquisition attaches its source Smite producer");while(WindField(smite,"ammo")==null)yield return null;
  DurableEnemy(eligible,p.transform.position+Vector3.right,false);eligible.SetActive(true);Plugin.SetField(eligibleHealth,"<HP>k__BackingField",1);Physics2D.SyncTransforms();
  Require(eligibleHealth.HP==1&&Physics2D.OverlapCircleAll(smite.transform.position,(float)WindField(smite,"range"),1<<(int)TagLayerUtil.Enemy).Any(c=>c.gameObject==eligible),"lethal Smite fixture is active at one HP inside the actual native overlap query after Health.OnEnable resets pooled health");
  SetPhase("shield-native-smite");p.ammo.UseAmmo(p.ammo.amount);
  while(!eligibleHealth.isDead)yield return null;
  Require((float)WindField(first,"ApplyAt")==1&&(float)WindField(second,"ApplyAt")==121&&!shields[0].isActive,"an actual native lethal Smite advances only the first deadline to exact equality without prematurely recharging a stopped clock");
  SetPhase("shield-game-clock-recharge");GameTimer.SharedInstance.Start();while(!shields[0].isActive)yield return null;GameTimer.SharedInstance.Stop();
  Require(!shields[1].isActive&&p.playerHealth.isProtected&&Mathf.Abs(Bonus(p.stats[StatType.MoveSpeed])-.5f)<.001f,"actual game-clock advancement recharges the first shield and its Blessing helpers independently of the second");
  yield return AndroidPick(game,"HolyArts",game.playerXP.level);var smites=p.GetComponentsInChildren<SmitePassive>();while(smites.Any(s=>WindField(s,"ammo")==null))yield return null;
  Require(smites.Length==2&&smites.All(s=>(int)WindField(s,"baseDamage")==20&&(float)WindField(s,"range")==4&&WindField(s,"soundFX")==null),"repeated native Holy Arts attaches a second full-strength radius-four source producer with its serialized silent sound field");
  eligible.SetActive(false);DurableEnemy(eligible,p.transform.position+Vector3.right,false);eligible.SetActive(true);Physics2D.SyncTransforms();p.ammo.UseAmmo(p.ammo.amount);
  while(eligibleHealth.HP==10000)yield return null;yield return null;
  Require(eligibleHealth.HP==9960,"both acquired Holy Arts producers deliver twenty classified damage through the real last-ammo event, without Windows' half-strength repeat recipe");eligible.SetActive(false);
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
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Android repeated shield owners, counted physics protection, first-shield buffs/Smite, source clocks and mixed-phase checkpoint");Application.Quit(0);
 }
}
