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
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 GameObject wizardTarget,wizardProjectile,wizardPrototype;int wizardImpacts;readonly List<float> wizardBurns=new List<float>();readonly HashSet<GameObject> wizardMarkedShots=new HashSet<GameObject>();
 static MonoBehaviour WizardFlags(ShootingSummon ghost)=>ghost.GetComponents<MonoBehaviour>().SingleOrDefault(c=>c.GetType().Name=="AndroidShootingFlags");
 void WizardImpact(object sender,object args){if(wizardTarget!=null&&(args as GameObject)==wizardTarget&&(sender as GameObject)==PlayerController.Instance.gameObject)wizardImpacts++;}
 void WizardBurn(object sender,object args)
 {
  if(wizardTarget==null)return;
  Require(ReferenceEquals(sender,BurnSystem.SharedInstance)&&args==null,"source burn notification has the system sender and null payload");
  // This controlled shot fixture observes the original pre-application event.
  wizardBurns.Add(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(wizardTarget)?.Damage??0);
 }
 IEnumerator WizardHit(ShootingSummon ghost,bool forceMage,float[] expectedBurns,int expectedImpacts,bool assassinProbe=false)
 {
  var pool=ObjectPooler.SharedInstance;var actor=pool.GetPooledObject("Validation_Wizard_Enemy");var health=DurableEnemy(actor,ghost.transform.position+Vector3.right*3,false);actor.SetActive(true);{var fixedBody=health.GetComponent<Rigidbody2D>();if(fixedBody!=null)fixedBody.constraints=RigidbodyConstraints2D.FreezeAll;}
  if(assassinProbe){health.maxHP=100;Plugin.SetField(health,"<HP>k__BackingField",20+Mathf.FloorToInt(ShootingRecipe(ghost).damage));}
  wizardTarget=actor;wizardImpacts=0;wizardBurns.Clear();int hurt=0;ObserveDamage(health,d=>hurt++);
  var originalPrefab=(GameObject)WindField(ghost,"projectilePrefab");Plugin.SetField(ghost,"projectilePrefab",wizardPrototype);
  string tag=wizardPrototype.name;foreach(var shot in pool.pooledObjectsDictionary[tag])ReturnFixtureActor(shot);
  ForceGlareBurn=forceMage;
  try
  {
   ShootingNativeProbe.Summon=ghost;int shots=ShootingNativeProbe.Calls;
   Require(ShootingAttack(ghost),"actual Ghost shoots its pooled target");
   Log("wizard-target-selection","direction="+ShootingNativeProbe.Direction+" expected="+(actor.transform.position-ghost.transform.position)+" candidates="+string.Join(";",pool.pooledObjectsDictionary.Values.SelectMany(list=>list).Where(candidate=>candidate.activeSelf&&candidate.GetComponent<Health>()!=null&&Mathf.Abs(candidate.transform.position.x-PlayerController.Instance.transform.position.x)<9&&Mathf.Abs(candidate.transform.position.y-PlayerController.Instance.transform.position.y)<6).Take(16).Select(candidate=>candidate.name+"@"+candidate.transform.position)));
   Require(ShootingNativeProbe.Calls==shots+1&&Vector2.Distance(ShootingNativeProbe.Direction,(Vector2)(actor.transform.position-ghost.transform.position))<.001f,"actual source automatic targeting selects the fixture's sole nearby pooled actor");
   wizardProjectile=pool.pooledObjectsDictionary[tag].Single(p=>p.activeInHierarchy);int observations=0;
   while(hurt==0)
   {
    if(observations++<6){var state=wizardProjectile.GetComponent<AndroidProjectile>();Log("projectile-diagnostic","shot="+wizardProjectile.transform.position+" target="+actor.transform.position+" velocity="+wizardProjectile.GetComponent<MoveComponent2D>().vector+" owner="+(state!=null)+" sampleRadius="+(state==null?"none":WindField(state,"Radius").ToString())+" hp="+health.HP+" targetLayer="+actor.layer+" collider="+(Physics2D.OverlapCircle(actor.transform.position,.2f,1<<actor.layer)!=null));}
    if(!wizardProjectile.activeInHierarchy)Require(false,"shot remains active until its real target contact; a missed/expired shot is a diagnostic failure");yield return null;
   }
  }
  finally{ForceGlareBurn=false;ShootingNativeProbe.Summon=null;Plugin.SetField(ghost,"projectilePrefab",originalPrefab);}
  Log("wizard-contact","impact="+wizardImpacts+" burns="+string.Join(",",wizardBurns)+" projectile="+wizardProjectile.GetInstanceID());
  Require(wizardImpacts==expectedImpacts&&wizardBurns.SequenceEqual(expectedBurns),"actual shot has one correctly owned impact and exact ordered source pre-application burn notifications");
  if(assassinProbe){Log("plain-ghost-assassin","HP="+health.HP+" dead="+health.isDead);Require(!health.isDead&&health.HP==20,"plain Ghost bulletEffect=false excludes global Assassin at actual contact");}
  if(expectedBurns.Length>0)wizardMarkedShots.Add(wizardProjectile);
  actor.transform.position=PlayerController.Instance.transform.position+Vector3.right*50;wizardTarget=null;
 }
 IEnumerator CheckAndroidGhostWizard(bool continuation)
 {
  bool projectileSuite=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("projectiles-");if(projectileSuite)PrepareProjectileAssets();
  GameController game;PlayerController player;
  if(!continuation)
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();yield return OpenCustomConfiguration();var map=CustomMode.Maps().Single(m=>m.name=="Endless_Forest");Click(CustomButton("CustomLoadDefaults"));Click(CustomButton("CustomAction"+Array.IndexOf(CustomMode.Maps(),map)));
   Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>();Click(CustomButton("CustomStart"));while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
   player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;Object.FindObjectOfType<AndroidEndlessRun>().enabled=false;player.SetPosition(new Vector3(100,100,0));GameTimer.SharedInstance.Stop();yield return AndroidPick(game,"GhostFriend",game.playerXP.level);
  }
  else
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState)){var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));if(error!=null)throw new InvalidOperationException(error.text);yield return null;}
   player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;Object.FindObjectOfType<AndroidEndlessRun>().enabled=false;GameTimer.SharedInstance.Stop();
  }
  foreach(var ghost in Ghosts()){while(!SummonStarted(ghost))yield return null;ghost.enabled=false;}
  var pool=ObjectPooler.SharedInstance;
  foreach(var actor in pool.pooledObjectsDictionary.Values.SelectMany(list=>list).Where(actor=>actor.activeSelf&&actor.layer==LayerMask.NameToLayer("Enemy")&&actor.GetComponent<Health>()!=null).Distinct().ToArray())ReturnFixtureActor(actor);
  Log("fixture-precondition","The actual source scheduler is disabled and pre-existing pooled monsters are returned for controlled projectile/summon contacts. This test does not validate encounter scheduling or natural target frequency.");
  var template=pool.itemDictionary.Values.First(i=>i.objectToPool.CompareTag("Enemy")&&i.objectToPool.GetComponent<Health>()!=null&&i.objectToPool.GetComponent<MoveComponent2D>()!=null).objectToPool;pool.AddObject("Validation_Wizard_Enemy",template,4);
  this.AddObserver(WizardImpact,Projectile.ImpactEvent);this.AddObserver(WizardBurn,BurnSystem.InflictBurnEvent);
  var first=Ghosts()[0];
  wizardPrototype=Object.Instantiate((GameObject)WindField(first,"projectilePrefab"));wizardPrototype.SetActive(false);wizardPrototype.name="Validation_Wizard_Projectile";wizardPrototype.transform.SetParent(pool.transform);pool.AddObject(wizardPrototype.name,wizardPrototype,2);
  Require(pool.pooledObjectsDictionary[wizardPrototype.name].Count==0,"source registration leaves the fixture-only Ghost pool lazy before its first real shot");
  Log("fixture-precondition","Actual source lazy allocation and returned-object reuse exercise wizard/plain flags; no native preallocation or rotating-selection assumption");
  if(projectileSuite)
  {
   if(!continuation)yield return AndroidPick(game,"Assassin",game.playerXP.level);
   yield return CheckSharedProjectiles(game,continuation);
  }
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="resume-ghost-wizard-projectile")
  {
   // A85 diagnostic reproduction. Expected to fail until the common source
   // projectile producer is migrated; this is not an acceptance-pass mode.
   yield return AndroidPick(game,"Assassin",game.playerXP.level);yield return WizardHit(first,false,new float[0],0,true);
   yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Plain source Ghost projectile does not dispatch the global bullet-impact Assassin consumer");Application.Quit(0);yield break;
  }
  if(!continuation)
  {
   yield return WizardHit(first,false,new float[0],0);yield return AndroidPick(game,"GhostWizard",game.playerXP.level);
   var flags=WizardFlags(first);Require(flags!=null&&(bool)WindField(flags,"BulletEffect")&&(int)WindField(flags,"BurnDamage")==6,"actual Ghost Wizard assigns the source per-instance bulletEffect and burn-six fields");
   yield return WizardHit(first,false,new[]{0f},1);
   yield return AndroidPick(game,"GhostWizard",game.playerXP.level);Require(first.GetComponents<MonoBehaviour>().Count(c=>c.GetType().Name=="AndroidShootingFlags")==1&&(int)WindField(WizardFlags(first),"BurnDamage")==6,"repeat Ghost Wizard assigns six once without duplicate native summon-hit actions");
   yield return AndroidPick(game,"PyroMage",game.playerXP.level);Log("fixture-precondition","The actual source BurnOnHit entry fixes the Pyro Mage roll before its source callback for controlled projectile contact; no probability-frequency claim");
   yield return WizardHit(first,true,new[]{0f,6f},1);
   yield return AndroidPick(game,"GhostFriend",game.playerXP.level);var later=Ghosts()[1];while(!SummonStarted(later))yield return null;later.enabled=false;
   Require(WizardFlags(later)==null&&ShootingRecipe(later).owner==later.gameObject,"later pristine Ghost does not inherit the older wizard fields or player-impact ownership");
   yield return WizardHit(later,true,new float[0],0);Require(wizardMarkedShots.Contains(wizardProjectile),"actual reused pooled projectile clears the older wizard shot's burn and player-impact flags");
  }
  else
  {
   Require(Ghosts().Length==2&&Ghosts().All(g=>WizardFlags(g)!=null&&(bool)WindField(WizardFlags(g),"BulletEffect")&&(int)WindField(WizardFlags(g),"BurnDamage")==6),"source grouped acquisition replay creates both Ghosts before Ghost Wizard upgrades, rather than restoring later-actor snapshots");
   foreach(var ghost in Ghosts())Require(StartedSummonTimers[ghost.GetInstanceID()]==0,"source-unsaved Ghost interval starts fresh before its first Update");
   yield return WizardHit(first,true,new[]{0f,6f},1);yield return WizardHit(first,true,new[]{0f,6f},1);yield return WizardHit(Ghosts()[1],true,new[]{0f,6f},1);Require(wizardMarkedShots.Contains(wizardProjectile),"continued wizard shots reuse an actual returned source projectile");
  }
  this.RemoveObserver(WizardImpact,Projectile.ImpactEvent);this.RemoveObserver(WizardBurn,BurnSystem.InflictBurnEvent);
  if(continuation)
  {
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();Require(Object.FindObjectsOfType<ShootingSummon>().Length==0&&Object.FindObjectsOfType<MonoBehaviour>().All(c=>c.GetType().Name!="AndroidShootingFlags"),"ordinary restart clears wizard actors and their source fields");yield return CheckRevolver(game,PlayerController.Instance,"android-ghost-wizard-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Source Ghost Wizard mixed actor/shot flags, actual ordered burn/native Pyro proc, pooled reuse and original restart survive Continue");Application.Quit(0);yield break;
  }
  for(int i=0;i<2;i++)Plugin.SetField(Ghosts()[i],"_timer",new[]{.03f,.08f}[i]);yield return Capture("android-ghost-wizard-save.png");PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Source Ghost Wizard assignment/repeats, actual burn-before-bullet-proc shots, pristine later Ghost, pool reuse and mixed checkpoint saved");Application.Quit(0);
 }
}
