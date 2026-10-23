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
using flanne.RuneSystem;
using flanne.PerkSystem.Actions;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static int RedHeartSpawns;
 internal static Collision2D WindCollision;
 IEnumerator CheckAndroidRuneConsumers(bool continuation)
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
   p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();
   var clock=p.GetComponentInChildren<AndroidBodySoul>();var wall=p.GetComponentInChildren<ProjectileShieldRune>();
   Require(clock.Grants==2&&clock.IsInvoking("DropHP")&&p.playerHealth.maxSHP==6,"Continue restores Body and Soul grant count/capacity and starts its fresh source repeating schedule");
   Require(!p.ammo.infiniteAmmo.value&&wall.GetComponent<Collider2D>().enabled&&(int)WindField(p.GetComponentInChildren<EtherealRune>(),"_counter")==0,"source Continue starts Ethereal and Wind Shield fresh rather than importing their unsaved active/cooldown/counter phases");
   var source=RunResume.Read().runtime;Require(source.sourceBodySoul==2&&source.sourceHealing==0&&source.providers.Count==0,"source checkpoint retains the two supported rune counters without generic phase providers");
   while(clock.Grants<clock.Limit){int next=clock.Grants+1;clock.Invoke("DropHP",0);while(clock.Grants<next)yield return null;}
   int soul=p.playerHealth.shp;clock.Invoke("DropHP",0);yield return null;
   Require(clock.Grants==6&&p.playerHealth.shp==soul,"Android Body and Soul stops after its six rank-three grants");
   yield return Capture("android-rune-consumers-continue.png");yield return AchievementExit(game,false);
   yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
   Require(p.GetComponentInChildren<AndroidBodySoul>()==null&&!p.ammo.infiniteAmmo.value&&p.playerHealth.maxSHP==0,"normal restart clears rune providers, ammo ownership and extra soul capacity");
   yield return CheckRevolver(game,p,"android-rune-consumers-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Android rune-provider clocks/grant limits, fresh-process Continue and original restart");Application.Quit(0);yield break;
  }
  RunResume.Invalidate();
  Log("fixture-precondition","Five rank-three native rune copies explicitly selected in the isolated ordinary run; no rune purchase/progression claim. Body and Soul grant and death counters are controlled at actual callbacks, not a naturally played300-second/kill-count claim.");
  yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>
  {
   Loadout.RuneSelection=new[]{"HealingRuneData","EtherealMagRuneData","BodyAndSoulData","WindWallData","CloseCombatRuneData"}.Select(name=>
   {var copy=Object.Instantiate(Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.name==name));copy.name=name;copy.level=3;return copy;}).ToList();
  });
  game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  var body=p.GetComponentInChildren<AndroidBodySoul>();var shield=p.GetComponentInChildren<ProjectileShieldRune>();
  Require(body.Limit==6&&p.playerHealth.maxSHP==6&&p.playerHealth.shp==0,"Android Body and Soul adds six capacity to the source zero baseline, with six finite rank-three grants and no immediate pickup");
  for(int i=1;i<=2;i++){body.Invoke("DropHP",0);while(body.Grants<i)yield return null;}
  Require(p.playerHealth.shp==2,"source Body and Soul callbacks grant souls directly without collection");
  var target=DurableEnemy(ObjectPooler.SharedInstance.GetPooledObject("BrainMonster"),p.transform.position,false);target.gameObject.tag="Untagged";
  var boost=p.GetComponentInChildren<DamageBoostInRange>();Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
  var inRange=(List<Health>)AccessTools.Field(typeof(DamageBoostInRange),"_targetsInRange").GetValue(boost);
  Require(inRange.Contains(target),"native Close Combat collision includes a Health target without an Enemy tag");
  int before=target.HP;target.TakeDamage(DamageType.Bullet,20);
  Require(before-target.HP==22,"Android signed modifier rounding turns twenty damage with rank-three Close Combat into twenty-two");
  target.transform.position=p.transform.position+Vector3.right*35;Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
  before=target.HP;target.TakeDamage(DamageType.Bullet,20);Require(before-target.HP==20&&!inRange.Contains(target),"native Close Combat exit removes the modifier for the same untagged Health target");
  int red=RedHeartSpawns;yield return AndroidPick(game,"Regeneration",game.playerXP.level);
  Require(RedHeartSpawns==red,"Android Regeneration has no immediate acquisition heart");
  float start=Time.time;Time.timeScale=40;while(RedHeartSpawns==red)yield return null;Time.timeScale=1;
  Require(Time.time-start>=89&&RedHeartSpawns==red+1,"native Regeneration first periodic drop waits its full ninety-second interval");
  var healing=p.GetComponentInChildren<DropHPOnKillRune>();var ethereal=p.GetComponentInChildren<EtherealRune>();
  Plugin.SetField(healing,"_counter",healing.killThreshold-1);Plugin.SetField(ethereal,"_counter",(int)WindField(ethereal,"killsToActivate")-1);
  var deathPosition=target.transform.position;
  Log("healing-fixture-before","HP="+target.HP+" active="+target.gameObject.activeSelf+" tag="+target.tag+" position="+deathPosition+" counter="+AccessTools.Field(typeof(DropHPOnKillRune),"_counter").GetValue(healing));
  target.TakeDamage(DamageType.Bullet,1000000);yield return null;
  Log("healing-fixture-after","HP="+target.HP+" counter="+AccessTools.Field(typeof(DropHPOnKillRune),"_counter").GetValue(healing)+" pickups="+string.Join(";",Object.FindObjectsOfType<flanne.Pickups.HPPickup>().Select(h=>h.transform.position.ToString())));
  Require((int)AccessTools.Field(typeof(DropHPOnKillRune),"_counter").GetValue(healing)==0,"Android Healing counts the native death of a Health actor without an Enemy tag");
  Require(Object.FindObjectsOfType<flanne.Pickups.HPPickup>().Any(h=>Vector2.Distance(h.transform.position,deathPosition)<.1f),"Android Healing drops the heart at the actual distant death location");
  Require(p.ammo.infiniteAmmo.value,"Android Ethereal counts that same native Health death");
  WindCollision=null;var projectile=ObjectPooler.SharedInstance.GetPooledObject("CultistProjectile");projectile.transform.position=shield.GetComponent<Collider2D>().bounds.center;
  var rigidbody=projectile.GetComponent<Rigidbody2D>();if(rigidbody!=null)rigidbody.velocity=Vector2.zero;
  Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
  Require(WindCollision!=null&&!projectile.activeSelf&&!shield.GetComponent<Collider2D>().enabled,"real enemy-projectile collision returns the native pooled shot before Wind Shield cooldown");
  Require(typeof(AndroidBodySoul).GetField("Remaining")==null,"Body and Soul does not retain a source-unsaved timer phase");
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Android rune loadout, direct finite souls, unqualified Health death filters, Close Combat and signed rounding, Regeneration timing, real projectile block and checkpoint");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(DropPrefabOverTime),"DropHP")]
class ObserveNativeHeartSpawn
{static void Postfix(DropPrefabOverTime __instance){if(__instance.gameObject.name=="PF_DropHPOverTime")RuntimeChecks.RedHeartSpawns++;}}
[HarmonyPatch(typeof(ProjectileShieldRune),"OnCollisionEnter2D")]
class ObserveNativeWindCollision
{static void Prefix(Collision2D collision){RuntimeChecks.WindCollision=collision;RuntimeChecks.Log("native-wind-collision",collision.gameObject.name+" layer="+collision.gameObject.layer);}}
