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
  var pool=ObjectPooler.SharedInstance;var actor=pool.GetPooledObject("Validation_Wizard_Enemy");var health=DurableEnemy(actor,ghost.transform.position+Vector3.right*3,false);actor.SetActive(true);health.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
  if(assassinProbe){health.maxHP=100;Plugin.SetField(health,"<HP>k__BackingField",20+Mathf.FloorToInt(ShootingRecipe(ghost).damage));}
  wizardTarget=actor;wizardImpacts=0;wizardBurns.Clear();int hurt=0;health.onHurt.AddListener(d=>hurt++);
  var originalPrefab=(GameObject)WindField(ghost,"projectilePrefab");Plugin.SetField(ghost,"projectilePrefab",wizardPrototype);
  string tag=wizardPrototype.name;foreach(var shot in pool.pooledObjectsDictionary[tag])shot.SetActive(false);
  ForceGlareBurn=forceMage;
  try
  {
   Require(ShootingAttack(ghost),"actual Ghost shoots its pooled target");wizardProjectile=pool.pooledObjectsDictionary[tag].Single(p=>p.activeInHierarchy);int observations=0;
   while(hurt==0)
   {
    if(observations++<6){var state=wizardProjectile.GetComponent<AndroidProjectile>();Log("projectile-diagnostic","shot="+wizardProjectile.transform.position+" target="+actor.transform.position+" velocity="+wizardProjectile.GetComponent<MoveComponent2D>().vector+" owner="+(state!=null)+" sampleRadius="+(state==null?"none":WindField(state,"Radius").ToString())+" hp="+health.HP+" targetLayer="+actor.layer+" collider="+(Physics2D.OverlapCircle(actor.transform.position,.2f,1<<actor.layer)!=null));}
    if(!wizardProjectile.activeInHierarchy)Require(false,"shot remains active until its real target contact; a missed/expired shot is a diagnostic failure");yield return null;
   }
  }
  finally{ForceGlareBurn=false;Plugin.SetField(ghost,"projectilePrefab",originalPrefab);}
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
   player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;player.SetPosition(new Vector3(100,100,0));GameTimer.SharedInstance.Stop();yield return AndroidPick(game,"GhostFriend",game.playerXP.level);
  }
  else
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState)){var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));if(error!=null)throw new InvalidOperationException(error.text);yield return null;}
   player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;GameTimer.SharedInstance.Stop();
  }
  foreach(var ghost in Ghosts()){while(WindField(ghost,"stats")==null)yield return null;ghost.enabled=false;}
  var pool=ObjectPooler.SharedInstance;var template=pool.itemsToPool.First(i=>i.objectToPool.CompareTag("Enemy")&&i.objectToPool.GetComponent<Health>()!=null&&i.objectToPool.GetComponent<MoveComponent2D>()!=null).objectToPool;pool.AddObject("Validation_Wizard_Enemy",template,4);
  this.AddObserver(WizardImpact,Projectile.ImpactEvent);this.AddObserver(WizardBurn,BurnSystem.InflictBurnEvent);
  var first=Ghosts()[0];
  wizardPrototype=Object.Instantiate((GameObject)WindField(first,"projectilePrefab"));wizardPrototype.SetActive(false);wizardPrototype.name="Validation_Wizard_Projectile";wizardPrototype.transform.SetParent(pool.transform);pool.AddObject(wizardPrototype.name,wizardPrototype,2);
  Require(pool.pooledObjectsDictionary[wizardPrototype.name].Count==2,"fixture starts with two actual native pooled Ghost projectiles");
  Log("fixture-precondition","Two-member pool from the actual native Ghost projectile prefab exercises native rotating selection; wizard contacts warm both members before a plain shot");
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
   yield return AndroidPick(game,"PyroMage",game.playerXP.level);Log("fixture-precondition","Existing native OnHitTrigger chance hook fixes the Pyro Mage roll for actual projectile contact; no probability-frequency claim");
   yield return WizardHit(first,true,new[]{0f,6f},1);
   yield return AndroidPick(game,"GhostFriend",game.playerXP.level);var later=Ghosts()[1];while(WindField(later,"stats")==null)yield return null;later.enabled=false;
   Require(WizardFlags(later)==null&&ShootingRecipe(later).owner==later.gameObject,"later pristine Ghost does not inherit the older wizard fields or player-impact ownership");
   yield return WizardHit(later,true,new float[0],0);Require(wizardMarkedShots.Contains(wizardProjectile),"actual reused pooled projectile clears the older wizard shot's burn and player-impact flags");
  }
  else
  {
   Require(Ghosts().Length==2&&WizardFlags(first)!=null&&(bool)WindField(WizardFlags(first),"BulletEffect")&&(int)WindField(WizardFlags(first),"BurnDamage")==6&&WizardFlags(Ghosts()[1])==null,"Continue preserves mixed upgraded/unupgraded Ghost flags through acquisition order");
   float[] clocks={.03f,.08f};for(int i=0;i<2;i++)Require(Mathf.Abs((float)WindField(Ghosts()[i],"_timer")-clocks[i])<.025f,"Continue preserves the Ghost Wizard actor interval "+i);
   yield return WizardHit(first,true,new[]{0f,6f},1);yield return WizardHit(first,true,new[]{0f,6f},1);yield return WizardHit(Ghosts()[1],true,new float[0],0);Require(wizardMarkedShots.Contains(wizardProjectile),"continued pooled reuse clears source wizard flags for the plain Ghost");
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
