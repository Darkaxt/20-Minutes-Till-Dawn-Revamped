using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 public static GameObject ColdLastReleased;public static bool ColdNoFreeze;Health coldObserved;int coldImpacts;readonly List<DamageType> coldDamage=new List<DamageType>();
 void ColdHurt(object sender,object args){if((sender as Health)==coldObserved)coldDamage.Add((DamageType)args);}
 void ColdImpact(object sender,object args)=>coldImpacts++;
 static MonoBehaviour[] ColdHelpers(string type)=>PlayerController.Instance.GetComponentsInChildren<MonoBehaviour>().Where(c=>c.GetType().Name==type).ToArray();
 static bool ColdObserversReady()
 {
  var table=(Dictionary<string,Dictionary<object,List<Action<object,object>>>>)AccessTools.Field(typeof(NotificationCenter),"_table").GetValue(NotificationCenter.instance);
  foreach(var entry in new[]{new{type="AndroidFrostbite",notice=FreezeSystem.InflictFreezeEvent},new{type="AndroidShatter",notice=Health.DeathEvent},new{type="AndroidAssassin",notice=Projectile.ImpactEvent}})
   foreach(var helper in ColdHelpers(entry.type))
    if(!table.TryGetValue(entry.notice,out var senders)||!senders.Values.SelectMany(handlers=>handlers).Any(handler=>ReferenceEquals(handler.Target,helper)))return false;
  return true;
 }
 Health ColdEnemy(ObjectPooler pool,Vector3 position,int max=101,int hp=101,string tag="Enemy")
 {
  var actor=pool.GetPooledObject("Validation_Cold_Enemy");DurableEnemy(actor,position,false);actor.SetActive(true);actor.tag=tag;var h=actor.GetComponent<Health>();h.maxHP=max;Plugin.SetField(h,"<HP>k__BackingField",hp);{var fixedBody=actor.GetComponent<Rigidbody2D>();if(fixedBody!=null)fixedBody.constraints=RigidbodyConstraints2D.FreezeAll;}return h;
 }
 IEnumerator ColdFreeze(Health target,int expected,string tag=null)
 {
  if(tag!=null)target.tag=tag;int before=target.HP;coldObserved=target;coldDamage.Clear();Log("cold-freeze-before","tag="+target.tag);FreezeSystem.SharedInstance.Freeze(target.gameObject);Log("cold-freeze-synchronous","tag="+target.tag);yield return null;Log("cold-freeze","before="+before+" after="+target.HP+" tag="+target.tag);Require(before-target.HP==expected,"actual freeze applies independently floored full-strength source Frostbite helpers");Require(coldDamage.All(t=>t==(DamageType)9),"source freeze keeps a distinct damage category instead of Bullet or Summon");ReturnFixtureActor(target.gameObject);while(FreezeSystem.SharedInstance.IsFrozen(target.gameObject))yield return null;coldObserved=null;
 }
 IEnumerator ColdShot(GameController game,Health target,bool expectedDead)
 {
  var player=PlayerController.Instance;while(PauseController.isPaused||!(player.CurrentState is flanne.Player.IdleState))yield return null;
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  int hits=0;ObserveDamage(target,d=>hits++);bool fired=false;UnityEngine.Events.UnityAction shot=()=>fired=true;player.gun.OnShoot.AddListener(shot);
  bool exitRegression=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("cold-exit");
  ColdNoFreeze=true;
  try
  {
   LastSourceProjectile=null;var aim=(Vector2)Camera.main.WorldToScreenPoint(target.transform.position);InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(!fired)yield return null;
   if(!exitRegression||!expectedDead)InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});player.gun.OnShoot.RemoveListener(shot);int observations=0;
   while(hits==0&&!target.isDead)
   {
    if(ObserveSourceProjectiles&&LastSourceProjectile!=null)
    {
     if(observations++<6)Log("native-shot-contact-diagnostic","shot="+LastSourceProjectile.transform.position+" target="+target.transform.position+" direction="+LastSourceProjectile.GetComponent<MoveComponent2D>().vector+" hp="+target.HP+" aim="+aim);
     if(!LastSourceProjectile.gameObject.activeSelf)Require(false,"actual native gun shot reaches its contact before deactivation");
    }
    yield return null;
   }
   yield return null;
  }
  finally{ColdNoFreeze=false;}
  Require(target.isDead==expectedDead,"actual native bullet impact observes the source inclusive Assassin threshold and Champion eligibility");ReturnFixtureActor(target.gameObject);if(!exitRegression)while(!(player.CurrentState is flanne.Player.IdleState))yield return null;
 }
 IEnumerator CheckAndroidCold(bool continuation)
 {
  GameController game;PlayerController player;
  if(!continuation)
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;
  }
  else
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState)){var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));if(error!=null)throw new InvalidOperationException(error.text);yield return null;}player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;
  }
  StopFixtureSpawning();player.SetPosition(new Vector3(100,100,0));GameTimer.SharedInstance.Stop();var pool=ObjectPooler.SharedInstance;pool.AddObject("Validation_Cold_Enemy",pool.itemDictionary["BrainMonster"].objectToPool,8);
  Log("cold-start-observers","before="+ColdObserversReady());while(!ColdObserversReady())yield return null;Log("cold-start-observers","ready=true");
  this.AddObserver(ColdHurt,"Revamped.AndroidSurvivingHurt");this.AddObserver(ColdImpact,Projectile.ImpactEvent);
  if(!continuation)
  {
   yield return AndroidPick(game,"Frostbite",game.playerXP.level);Require(ColdHelpers("AndroidFrostbite").Length==1,"actual Frostbite attaches an independent source owner");yield return ColdFreeze(ColdEnemy(pool,player.transform.position+Vector3.right*4),15);
   yield return AndroidPick(game,"Frostbite",game.playerXP.level);
  }
  Require(ColdHelpers("AndroidFrostbite").Length==2,"repeats and Continue retain two full-strength source Frostbite owners");yield return ColdFreeze(ColdEnemy(pool,player.transform.position+Vector3.right*4),continuation?26:30);yield return ColdFreeze(ColdEnemy(pool,player.transform.position+Vector3.right*4,101,101,"EnemyChampion"),continuation?0:2,"EnemyChampion");
  if(!continuation){yield return AndroidPick(game,"Shatter",game.playerXP.level);yield return AndroidPick(game,"Shatter",game.playerXP.level);}
  Require(ColdHelpers("AndroidShatter").Length==2,"repeats and Continue retain two full-strength source Shatter death owners");
  var victim=ColdEnemy(pool,player.transform.position+Vector3.right*5,101,101,"Untagged");var nearby=ColdEnemy(pool,victim.transform.position+Vector3.right*.7f,1000,1000);
  Require(!FreezeSystem.SharedInstance.IsFrozen(victim.gameObject),"source Shatter fixture victim is unfrozen and lacks an Enemy tag");coldObserved=nearby;coldDamage.Clear();int impacts=coldImpacts;victim.AutoKill();Physics2D.SyncTransforms();while(nearby.HP==1000)yield return null;yield return new WaitForFixedUpdate();Log("cold-shatter","hp="+nearby.HP);Require(nearby.HP==(continuation?990:986)&&coldDamage.SequenceEqual(new[]{DamageType.Bullet,DamageType.Bullet})&&coldImpacts==impacts,"actual untagged/unfrozen Health death spawns two independently modified source Bullet contacts without bullet-hit procs");ReturnFixtureActor(nearby.gameObject);coldObserved=null;
  while(pool.pooledObjectsDictionary["Revamped_AndroidShatter"].Any(actor=>actor.activeSelf))yield return null;
  Log("fixture-precondition","Twenty-six same-frame source Health deaths at a distant position exercise the original count>50 rejection, allowing51 active effects; no normal spawn-rate claim");
  float capStart=Time.time;for(int i=0;i<26;i++)ColdEnemy(pool,player.transform.position+Vector3.right*100,101,101,"Untagged").AutoKill();
  Require(pool.pooledObjectsDictionary["Revamped_AndroidShatter"].Count(g=>g.activeSelf)==51,"two independent Shatter owners preserve the source off-by-one cap with51 active effects");
  while(pool.pooledObjectsDictionary["Revamped_AndroidShatter"].Any(g=>g.activeSelf))yield return null;
  Require(Time.time-capStart>=.49f&&ColdLastReleased!=null,"source Shatter contacts expire and return after the actual half-second lifetime");var released=ColdLastReleased;
  ColdEnemy(pool,player.transform.position+Vector3.right*100,101,101,"Untagged").AutoKill();Require(released.activeSelf,"next real death reuses the last returned source Shatter effect");while(pool.pooledObjectsDictionary["Revamped_AndroidShatter"].Any(g=>g.activeSelf))yield return null;
  if(!continuation)
  {
   yield return NativeTomePick(game,false,"TomeOfMight");yield return ColdFreeze(ColdEnemy(pool,player.transform.position+Vector3.right*4),26);
   yield return AndroidPick(game,"TakeAim",game.playerXP.level);yield return AndroidPick(game,"Penetration",game.playerXP.level);float speed=player.stats[StatType.ProjectileSpeed].Modify(1);
   yield return AndroidPick(game,"Assassin",game.playerXP.level);Require(ColdHelpers("AndroidAssassin").Length==1&&Mathf.Abs(player.stats[StatType.ProjectileSpeed].Modify(1)-speed-.1f)<.001f,"first actual Assassin adds its source10% projectile speed");yield return AndroidPick(game,"Assassin",game.playerXP.level);Require(Mathf.Abs(player.stats[StatType.ProjectileSpeed].Modify(1)-speed-.2f)<.001f,"repeated actual Assassin adds another source10% projectile speed");
  }
  Require(ColdHelpers("AndroidAssassin").Length==2&&Mathf.Abs(player.stats[StatType.ProjectileSpeed].Modify(1)-1.65f)<.001f,"actual repeated Assassin and Continue retain independent source owners and speed alongside native prerequisites");
  var direct=ColdEnemy(pool,player.transform.position+Vector3.right*5,100,21);direct.TakeDamage(DamageType.Summon,3);Require(!direct.isDead&&direct.HP==20,"source Assassin ignores direct damage without a projectile impact");ReturnFixtureActor(direct.gameObject);
  Log("fixture-precondition","Stationary max100 targets start at21/20 HP to verify the source pre-Harmful impact threshold. Actual source FreezeOnHit/BurnOnHit roll fixed above50% to exclude unrelated elemental procs; no ordinary encounter or probability-frequency claim");
  yield return ColdShot(game,ColdEnemy(pool,player.transform.position+Vector3.right*3,100,21),false);yield return ColdShot(game,ColdEnemy(pool,player.transform.position+Vector3.right*3,100,20,"EnemyChampion"),true);
  this.RemoveObserver(ColdHurt,"Revamped.AndroidSurvivingHurt");this.RemoveObserver(ColdImpact,Projectile.ImpactEvent);
  if(continuation)
  {
   if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("cold-exit"))Require(player.CurrentState is flanne.Player.ShootingState&&player.gun.isShooting,"A84 regression leaves the continued cold/execute build during actual native shooting, with no Idle prerequisite");
   yield return AchievementExit(game,false);
   if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("cold-exit")){Log("fixture-held-mouse-cleanup",fixtureMouse.leftButton.isPressed);InputSystem.QueueStateEvent(fixtureMouse,new MouseState());while(fixtureMouse.leftButton.isPressed)yield return null;}
   yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();Require(ColdHelpers("AndroidFrostbite").Length+ColdHelpers("AndroidShatter").Length+ColdHelpers("AndroidAssassin").Length==0,"ordinary restart removes source cold/Assassin helpers");yield return CheckRevolver(game,PlayerController.Instance,"android-cold-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Source Frostbite/Shatter/Assassin repeats and actual producer/damage paths survive Continue and restart");Application.Quit(0);yield break;
  }
  yield return Capture("android-cold-source-helpers.png");PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Source Frostbite/Shatter/Assassin repeated acquisitions, real producers, no-hit exclusion and mixed checkpoint saved");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(TimeToLive),"Deactivate")]
class ColdReleaseProbe
{
 static void Prefix(TimeToLive __instance){if(__instance.GetComponents<MonoBehaviour>().Any(c=>c.GetType().Name=="AndroidShatterContact"))RuntimeChecks.ColdLastReleased=__instance.gameObject;}
}
[HarmonyPatch]
class ColdFreezeRollProbe
{
 static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(flanne.PerkSystem.Triggers.OnHitTrigger),"OnImpact"),AccessTools.Method(typeof(FreezeOnHit),"OnImpact"),AccessTools.Method(typeof(BurnOnHit),"OnImpact")};
 [HarmonyPriority(Priority.First)]
 static void Prefix(){if(RuntimeChecks.ColdNoFreeze)RuntimeChecks.SeedFirstRoll(.5f,1);}
}
