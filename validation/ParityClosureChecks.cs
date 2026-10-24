using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.CharacterPassives;
using flanne.Player;
using flanne.RuneSystem;
using flanne.PowerupSystem;
using flanne.PowerupSystems;
using flanne.PerkSystem.Triggers;
using flanne.Pickups;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;

public partial class RuntimeChecks
{
 internal static bool ClosureMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-closure-");
 internal static bool ClosureCapture,ClosureAbbyShot,ClosureActivation;
 internal static int ClosureSounds,ClosureThunders;
 internal static bool ClosureObserveImpact;
 internal static object ClosureImpactSender;
 void ClosureImpactNotice(object sender,object args){if(ClosureObserveImpact)ClosureImpactSender=sender;}
 internal static readonly List<Vector2> ClosureDirections=new List<Vector2>();
 internal static readonly List<string> ClosureShotEvents=new List<string>();
 int closureTweaks;
 void ClosureTweak(object sender,object args)=>closureTweaks++;
 IEnumerator ClosureStart(string character,string gun,string map="20M_Forest",bool courage=false)
 {
  RunResume.Invalidate();Progression.State.earned.Add("character:"+character);Progression.State.earned.Add("weapon:"+gun);Progression.State.earned.Add("scenario:"+map);Progression.Refresh();
  yield return AchievementRun(character,gun,map,prepareRun:()=>Loadout.RuneSelection=courage?new[]{"CourageRuneData"}.Select(name=>{var rune=Object.Instantiate(Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.name==name));rune.name=name;rune.level=1;return rune;}).ToList():new List<RuneData>());
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();PlayerController.Instance.SetPosition(new Vector3(100,100,0));
  CombatClose(Time.fixedDeltaTime,.03f,"actual Unity player uses original Android0.03 physics step");
 }
 IEnumerator ClosureWarm(GameController game,string label)
 {
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.pauseMenu.GetComponentsInChildren<UnityEngine.UI.Button>().Single(b=>b.name=="ResumeSaveAndReturn"));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,label+"-saved-run.json"));
  yield return CombatBuffContinue();StopFixtureSpawning();GameTimer.SharedInstance.Stop();PlayerController.Instance.SetPosition(new Vector3(100,100,0));
 }
 IEnumerator ClosureAnger(GameController game)
 {
  foreach(string name in new[]{"Vitality","AngerPoint"})yield return AndroidPick(game,name,game.playerXP.level);
  var p=PlayerController.Instance;while(p.GetComponentInChildren<FireRateOnHurt>()==null)yield return null;yield return null;
  var order=new List<string>();EventHandler fire=(s,e)=>order.Add("F"),damage=(s,e)=>order.Add("D");
  p.stats[StatType.FireRate].ChangedEvent+=fire;p.stats[StatType.BulletDamage].ChangedEvent+=damage;
  float f=Bonus(p.stats[StatType.FireRate]),d=Bonus(p.stats[StatType.BulletDamage]);
  if(p.playerHealth.isInvincible.value)p.playerHealth.isInvincible.UnFlip();p.playerHealth.Heal(p.playerHealth.maxHP);
  BugCall(p.playerHealth,"TakeDamage",1);Require(order.SequenceEqual(new[]{"F","D"}),"real first Anger Point hurt applies Fire Rate before independent Bullet Damage");order.Clear();
  BugCall(p.playerHealth,"TakeDamage",1);Require(order.SequenceEqual(new[]{"F","D","F","D"}),"actual hurt refresh removes and reapplies both operands in source order");
  CombatClose(Bonus(p.stats[StatType.FireRate]),f+.7f,"real hurt grants source Fire Rate operand");CombatClose(Bonus(p.stats[StatType.BulletDamage]),d+.7f,"real hurt grants independent source Bullet Damage operand");order.Clear();
  float started=Time.time;Time.timeScale=40;while(Time.time-started<31)yield return null;Time.timeScale=1;
  Require(order.SequenceEqual(new[]{"F","D"}),"actual30-second expiry removes both contributions in source order");
  CombatClose(Bonus(p.stats[StatType.FireRate]),f,"expired Fire Rate returns to acquired baseline");CombatClose(Bonus(p.stats[StatType.BulletDamage]),d,"expired Bullet Damage returns to acquired baseline");
  p.stats[StatType.FireRate].ChangedEvent-=fire;p.stats[StatType.BulletDamage].ChangedEvent-=damage;
 }
 IEnumerator ClosureGlassDamage()
 {
  var p=PlayerController.Instance;var assets=Resources.FindObjectsOfTypeAll<AndroidDamageTomeAsset>().Where(a=>StatusSubscribed(a)&&a.Key=="tome_of_glass_name").ToArray();
  Require(assets.Length==1&&assets[0].Damage==6,"three real Glass acquisitions reconstruct exactly one factor6 listener");
  var target=StatusEnemy();target.transform.position=p.transform.position+Vector3.right*3;target.GetComponent<CircleCollider2D>().radius=1;
  int hp=target.HP;target.TakeDamage(DamageType.Bullet,10);Require(hp-target.HP==60,"real native health damage uses Glass factor6 exactly once");
  hp=target.HP;Physics2D.SyncTransforms();var recipe=p.gun.GetProjectileRecipe();recipe.knockback=0;recipe.damage=10;
  var shot=ProjectileFactory.SharedInstance.SpawnProjectile(recipe,Vector2.right,p.transform.position);
  while(target.HP==hp&&shot.gameObject.activeSelf)yield return new WaitForFixedUpdate();
  Require(hp-target.HP==60,"real sampled projectile contact uses the reconstructed Glass factor at0.03");
  if(shot.gameObject.activeSelf)ReturnFixtureActor(shot.gameObject);yield return StatusReturn(target);
 }
 IEnumerator ClosureGlass(bool continuation)
 {
  if(continuation)yield return CombatBuffContinue();else
  {
   yield return ClosureStart("Shana","RevolverData","Endless_Forest");var fresh=Object.FindObjectOfType<GameController>();yield return ClosureAnger(fresh);
   foreach(int i in Enumerable.Range(0,3))yield return NativeTomePick(fresh,false,"TomeOfGlass");
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return ClosureGlassDamage();
  for(int i=0;i<2;i++)
  {yield return ClosureWarm(Object.FindObjectOfType<GameController>(),"glass-warm-"+i);yield return ClosureGlassDamage();}
  var game=Object.FindObjectOfType<GameController>();
  if(!continuation){yield return DragonSuspend(game,"Three native Glass picks, actual damage/contact, two warm source Continues and Anger Point hurt/refresh/expiry order at Android physics step");yield break;}
  yield return AchievementExit(game,false);yield return ClosureStart("Shana","RevolverData");game=Object.FindObjectOfType<GameController>();
  Require(!Resources.FindObjectsOfTypeAll<AndroidDamageTomeAsset>().Any(StatusSubscribed)&&PlayerController.Instance.GetComponentInChildren<FireRateOnHurt>()==null,"ordinary restart removes Glass/Anger observers and helpers");
  yield return CheckRevolver(game,PlayerController.Instance,"closure-glass-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Exact fresh-process Glass Continue, repeated warm factor6, original restart/input and observer teardown");Application.Quit(0);
 }
 void ClosureBaseShots(PlayerController p)
 {
  var stat=p.stats[StatType.Projectiles];int initial=p.gun.numOfProjectiles;
  stat.AddFlatBonus(-initial);Require(p.gun.numOfProjectiles==0,"source gun exposes zero projectile count without a Windows clamp");
  stat.AddFlatBonus(-2);Require(p.gun.numOfProjectiles==-2,"source gun exposes a negative directed count to the receiving shooter");
  stat.AddFlatBonus(initial+2);Require(p.gun.numOfProjectiles==initial,"directed count operands are removed before native firing");
  var shooter=p.gun.shooters[0];p.ammo.Reload();this.AddObserver(ClosureTweak,Shooter.TweakProjectileRecipe);ClosureCapture=true;
  foreach(float spread in new[]{0f,30f})
  {
   ClosureDirections.Clear();shooter.Shoot(p.gun.GetProjectileRecipe(),Vector2.right,3,spread,0);
   Require(ClosureDirections.Count==3,"real base shooter emits all three projectiles");
   for(int i=0;i<3;i++)CombatClose(Vector2.SignedAngle(Vector2.right,ClosureDirections[i]),i/3f*spread-spread*.5f,"base shooter uses exact Android spread angle"+i+" at spread"+spread);
  }
  ClosureCapture=false;Require(closureTweaks==0,"base emission never publishes the removed Windows recipe-tweak event");this.RemoveObserver(ClosureTweak,Shooter.TweakProjectileRecipe);
  foreach(var shot in Object.FindObjectsOfType<Projectile>())ReturnFixtureActor(shot.gameObject);
 }
 IEnumerator ClosureAbbySkill(PlayerController p,int shooters)
 {
  var skill=p.GetComponentInChildren<DumpAmmoPassive>();while(WindField(skill,"myGun")==null)yield return null;
  var original=p.gun.shooters.ToArray();while(p.gun.shooters.Count<shooters)p.gun.shooters.Add(original[0]);
  p.ammo.Reload();int initial=p.ammo.amount;int shots=0;
  UnityAction observe=()=>
  {
   if(!ClosureAbbyShot)return;shots++;ClosureShotEvents.Add("event");
   bool final=p.ammo.amount==0;Require(p.disableAction.value!=final,"Abby's real final shot releases action lock before its gun event");
   Require(p.disableAnimation.value!=final,"Abby's final shot restores animation before its gun event");
  };
  p.gun.OnShoot.AddListener(observe);ClosureShotEvents.Clear();
  Plugin.SetField(skill,"_timer",0f);yield return SkillInput(null);
  while((bool)WindField(skill,"_isSpraying"))yield return null;
  p.gun.OnShoot.RemoveListener(observe);Require(shots==initial&&p.ammo.amount==0,"actual Abby skill consumes one ammo/event per full shot even with"+shooters+" configured shooters");
  Require(ClosureShotEvents.Count==initial*2&&Enumerable.Range(0,initial).All(i=>ClosureShotEvents[i*2]=="sound"&&ClosureShotEvents[i*2+1]=="event"),"every complete Abby shot plays gun sound then one Gun.OnShoot");
  p.gun.shooters.Clear();p.gun.shooters.AddRange(original);InputSystem.QueueStateEvent(fixtureMouse,new MouseState());
  while(!p.ammo.fullOnAmmo||!(p.CurrentState is IdleState))yield return null;
 }
 IEnumerator ClosureReload(GameController game)
 {
  yield return ArrowEvolution(game,PlayerController.Instance.gun.gunData.gunEvolutions.Single(e=>((GunData)WindField(e,"gunData")).name=="SwordOfDeathGun").name);
  var owners=PlayerController.Instance.GetComponentsInChildren<ActivateGameobjectOnReload>(true);while(owners.Length<2){yield return null;owners=PlayerController.Instance.GetComponentsInChildren<ActivateGameobjectOnReload>(true);}
  yield return null;Require(owners.Any(o=>WindField(o,"sfx")!=null)&&owners.Any(o=>WindField(o,"sfx")==null),"actual Courage and Sword of Death both use the shared source reload entry");
  foreach(var o in owners)((GameObject)WindField(o,"obj")).SetActive(false);ClosureSounds=0;ClosureActivation=true;PlayerController.Instance.ammo.Reload();ClosureActivation=false;
  Require(owners.All(o=>((GameObject)WindField(o,"obj")).activeSelf)&&ClosureSounds==0,"real ammo reload enables both native objects with no extra activation sound/RNG");
 }
 IEnumerator ClosureGameplay(bool continuation)
 {
  if(continuation)yield return CombatBuffContinue();else yield return ClosureStart("Abby","RevolverData");
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;
  ClosureBaseShots(p);yield return ClosureAbbySkill(p,1);yield return ClosureAbbySkill(p,2);
  ClosurePlayerTransition(p);yield return CheckRevolver(game,p,"closure-abby-fire-reload.png");
  if(!continuation){yield return DragonSuspend(game,"Native Abby input, complete single/multi-shooter spray, base spread/event order and real source player transitions");yield break;}
  var old=p.GetComponentInChildren<DumpAmmoPassive>();yield return AchievementExit(game,false);yield return ClosureStart("Shana","SwordData",courage:true);game=Object.FindObjectOfType<GameController>();
  Require(old==null,"ordinary restart destroys Abby skill and its state owner");yield return ClosureReload(game);yield return ClosureBurnPhysics();yield return ClosurePickups();yield return ClosureSingleton(game);
  yield return AchievementExit(game,false);yield return ClosureStart("Shana","CrossbowData");game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  // Exercise ChargedShooter's actual nonvirtual base emission, not only the
  // virtual ordinary Shooter entry. Explicit multishot operands expose spread.
  p.ammo.Reload();ClosureDirections.Clear();ClosureCapture=true;p.gun.shooters[0].OnStopShoot(p.gun.GetProjectileRecipe(),Vector2.right,3,30,0);ClosureCapture=false;
  Require(ClosureDirections.Count==3,"actual charged release reaches the source base emission");
  for(int i=0;i<3;i++)CombatClose(Vector2.SignedAngle(Vector2.right,ClosureDirections[i]),-15+i*10,"charged base source spread"+i);
  yield return GapCamera(game);yield return GapChargedInput(game,true);yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete","Abby current/warm/fresh input, base and charged emission, Courage/Death Spin reload, standalone fire and original scene exits at0.03");Application.Quit(0);
 }
 void ClosurePlayerTransition(PlayerController p)
 {
  var original=p.CurrentState;ClosureNestedState.Attempts=0;p.CurrentState=p.GetState<ClosureNestedState>();
  Require(p.CurrentState is ClosureNestedState&&ClosureNestedState.Attempts==1,"actual player source Transition rejects nested ChangeState from Enter");p.CurrentState=original;
 }
 IEnumerator ClosureBurnPhysics()
 {
  var pool=ObjectPooler.SharedInstance;var p=PlayerController.Instance;var burn=AndroidBurnState.Get(BurnSystem.SharedInstance);
  foreach(string tag in new[]{"FireExplosion","FireExplosionSmall","WildMagicFireSpreader"})
  {
   var target=StatusEnemy("Untagged");target.transform.position=p.transform.position+Vector3.right*4;Physics2D.SyncTransforms();
   // Use the actual original pool prototype even when this weapon has not
   // registered it yet; registration/activation remains the production path.
   if(!pool.pooledObjectsDictionary.ContainsKey(tag))
   {
    var prefab=Resources.FindObjectsOfTypeAll<BurnOnCollision>().First(b=>!b.gameObject.scene.IsValid()&&(b.gameObject.name=="PF_"+tag||b.gameObject.name==tag)).gameObject;
    pool.AddObject(tag,prefab,0);
   }
   var actor=pool.GetPooledObject(tag);actor.transform.position=target.transform.position;actor.SetActive(true);Physics2D.SyncTransforms();
   while(burn.Find(target.gameObject)==null&&actor.activeSelf)yield return new WaitForFixedUpdate();
   Require(burn.Find(target.gameObject)!=null,"actual standalone"+tag+" burns an untagged NPC-layer contact");ReturnFixtureActor(actor);BugCall(burn.Find(target.gameObject),"Release");yield return StatusReturn(target);
  }
 }
 IEnumerator ClosureImpact(bool continuation)
 {
  if(continuation)yield return CombatBuffContinue();else
  {
   yield return ClosureStart("Spark","SporeGunData","Endless_Forest");var fresh=Object.FindObjectOfType<GameController>();
   foreach(string name in new[]{"DragonEgg","DragonEgg","DragonBond"})yield return AndroidPick(fresh,name,fresh.playerXP.level);
   yield return ArrowEvolution(fresh,PlayerController.Instance.gun.gunData.gunEvolutions.Single(e=>((GunData)WindField(e,"gunData")).name=="MagicSprayerGun").name);
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;var game=Object.FindObjectOfType<GameController>();p.SetPosition(new Vector3(100,100,0));
  var spark=p.GetComponentInChildren<ThunderOnHit>();var bond=p.GetComponentInChildren<DragonBonusDamage>();while(!StatusSubscribed(spark)||!StatusSubscribed(bond))yield return null;
  var dragons=p.GetComponentsInChildren<ShootingSummon>(true).Where(s=>s.SummonTypeID=="Dragon").ToArray();Require(dragons.Length>=2&&ReferenceEquals(WindField(bond,"_dragon"),dragons[0]),"actual Dragon Bond acquisition/Continue selects first of the real inactive Dragons");
  foreach(var dragon in dragons)dragon.enabled=false;
  // Directed source invalid-ID and non-NPC cases must not advance global RNG.
  var rng=UnityEngine.Random.state;BugCall(spark,"OnImpact",p.gameObject,null);Require(JsonUtility.ToJson(rng)==JsonUtility.ToJson(UnityEngine.Random.state),"Spark Unity-null target returns before chance RNG");
  var invalid=new GameObject("ClosureNonNpc");rng=UnityEngine.Random.state;BugCall(spark,"OnImpact",p.gameObject,invalid);Require(JsonUtility.ToJson(rng)==JsonUtility.ToJson(UnityEngine.Random.state),"Spark rejects non-NPC layer before chance RNG");Object.Destroy(invalid);
  var trigger=new OnHitTrigger();rng=UnityEngine.Random.state;BugCall(trigger,"OnImpact",p.gameObject,null);Require(JsonUtility.ToJson(rng)==JsonUtility.ToJson(UnityEngine.Random.state),"source evolution OnHitTrigger resolves null before its chance draw");
  this.AddObserver(ClosureImpactNotice,Projectile.ImpactEvent);ClosureObserveImpact=true;
  var target=StatusEnemy("Untagged");target.transform.position=p.transform.position+Vector3.right*4;target.GetComponent<CircleCollider2D>().radius=1;Physics2D.SyncTransforms();
  var flower=p.GetComponentInChildren<SporeFlower>();while(flower==null||flower.GetComponent<AndroidSporeTargets>()==null||!flower.procsOnHit){yield return null;flower=p.GetComponentInChildren<SporeFlower>();}flower.enabled=false;flower.transform.position=p.transform.position;
  Require(p.gun.gunData.name=="MagicSprayerGun"&&flower.procsOnHit,"actual native Magic Sprayer evolution enables its non-player Spore impact producer");
  // Guarantee this one directed proc to isolate observer routing across the
  // actual target selection, arc, landing sound and physical impact. The
  // original chance is asserted/restored; this is not a proc-frequency test.
  float chance=(float)WindField(spark,"chanceToHit");CombatClose(chance,.5f,"original Spark chance remains fifty percent");Plugin.SetField(spark,"chanceToHit",1f);
  int hp=target.HP;ClosureImpactSender=null;ClosureThunders=0;Require((bool)BugCall(flower,"Attack"),"actual source Spore Flower attack selects the real nearby pooled target");
  while(ClosureImpactSender==null&&(SporeActive("sporePrefab").Length>0||SporeActive("sporeExplosionPrefab").Length>0))yield return new WaitForFixedUpdate();Plugin.SetField(spark,"chanceToHit",chance);
  var harm=ClosureImpactSender as HarmfulOnContact;
  Require(harm!=null&&harm.procOnHit&&harm.GetComponent<AndroidHarmfulKind>().Kind==(DamageType)11&&ClosureThunders==1,"real Spore arc/landing/physics dispatches a non-player impact and reaches Spark's global observer once");
  int expected=harm.damageAmount+Mathf.FloorToInt(dragons[0].baseDamage*(float)WindField(bond,"percentOfDragonsDamage"))+(int)WindField(spark,"baseDamage");
  var thunderPrefab=(GameObject)WindField(ThunderGenerator.SharedInstance,"thunderPrefab");var thunderActors=ObjectPooler.SharedInstance.pooledObjectsDictionary[thunderPrefab.name].Where(a=>a.activeSelf).ToArray();
  // Targeted source thunder creates its physical hitbox; its damage belongs
  // to the next actual contact, not the GenerateAt notification itself.
  while(hp-target.HP<expected&&thunderActors.Any(a=>a.activeSelf))yield return new WaitForFixedUpdate();
  Log("closure-nonplayer-impact",new{actual=hp-target.HP,expected,harm=harm.damageAmount,dragon=dragons[0].baseDamage,percent=WindField(bond,"percentOfDragonsDamage"),spark=WindField(spark,"baseDamage")});
  Require(hp-target.HP==expected,"actual non-player impact includes original Harmful, first Dragon Bond damage and Spark thunder");
  foreach(var actor in SporeActive("sporePrefab").Concat(SporeActive("sporeExplosionPrefab")))ReturnFixtureActor(actor);yield return StatusReturn(target);ClosureObserveImpact=false;this.RemoveObserver(ClosureImpactNotice,Projectile.ImpactEvent);
  // The actual primary source projectile reaches the same registered owners.
  target=StatusEnemy("Untagged");target.transform.position=p.transform.position+Vector3.right*3;target.GetComponent<CircleCollider2D>().radius=1;Physics2D.SyncTransforms();hp=target.HP;var recipe=p.gun.GetProjectileRecipe();recipe.knockback=0;
  var shot=ProjectileFactory.SharedInstance.SpawnProjectile(recipe,Vector2.right,p.transform.position);while(target.HP==hp&&shot.gameObject.activeSelf)yield return new WaitForFixedUpdate();Require(target.HP<hp,"real primary projectile still reaches source impact/damage owners at0.03");if(shot.gameObject.activeSelf)ReturnFixtureActor(shot.gameObject);yield return StatusReturn(target);
  if(!continuation){yield return ClosureWarm(game,"impact-warm");game=Object.FindObjectOfType<GameController>();Require(PlayerController.Instance.GetComponentsInChildren<ThunderOnHit>().Length==1&&PlayerController.Instance.GetComponentsInChildren<DragonBonusDamage>().Length==1,"warm Continue reconstructs one Spark and one Dragon Bond observer owner");yield return DragonSuspend(game,"Spark and first-Dragon global impact lifetime through actual non-player Spore physics and primary projectile, invalid-target RNG and warm Continue");yield break;}
  var oldSpark=spark;var oldBond=bond;yield return AchievementExit(game,false);yield return ClosureStart("Shana","RevolverData");game=Object.FindObjectOfType<GameController>();
  Require(oldSpark==null&&oldBond==null&&!StatusSubscribed(oldSpark)&&!StatusSubscribed(oldBond),"ordinary restart destroys and unsubscribes the exact old Spark/Dragon Bond owners");
  yield return CheckRevolver(game,PlayerController.Instance,"closure-impact-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Fresh-process and warm Spark/Dragon first selection/global impacts, source filters/RNG and native restart");Application.Quit(0);
 }
 IEnumerator ClosurePickups()
 {
  var p=PlayerController.Instance;var range=p.transform.Find("PickupRange");range.gameObject.SetActive(false);
  var xp=ObjectPooler.SharedInstance.GetPooledObject("SmallXP");xp.transform.position=p.transform.position;Physics2D.SyncTransforms();
  yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
  Require(WindField(xp.GetComponent<AndroidPickup>(),"target")==null,"actual body-only XP physics contact is rejected as in the original source matrix");
  range.gameObject.SetActive(true);Physics2D.SyncTransforms();while(WindField(xp.GetComponent<AndroidPickup>(),"target")==null&&xp.activeSelf)yield return new WaitForFixedUpdate();
  Require(WindField(xp.GetComponent<AndroidPickup>(),"target")!=null,"actual native pickup range collects the same XP actor");while(xp.activeSelf)yield return null;
  range.gameObject.SetActive(false);var heartPrefab=Resources.FindObjectsOfTypeAll<HPPickup>().First(h=>!h.gameObject.scene.IsValid()&&h.name=="PF_HeartPickup").gameObject;
  const string tag="ClosureHeart";ObjectPooler.SharedInstance.AddObject(tag,heartPrefab,0);var heart=ObjectPooler.SharedInstance.GetPooledObject(tag);heart.transform.position=p.transform.position;Physics2D.SyncTransforms();
  while(WindField(heart.GetComponent<AndroidPickup>(),"target")==null&&heart.activeSelf)yield return new WaitForFixedUpdate();Require(WindField(heart.GetComponent<AndroidPickup>(),"target")!=null,"actual red-heart body contact remains admitted");while(heart.activeSelf)yield return null;range.gameObject.SetActive(true);
 }
 IEnumerator ClosureSingleton(GameController game)
 {
  var actual=PowerupGenerator.Instance;PowerupGenerator.Instance=null;var first=new GameObject("ClosureOldGenerator").AddComponent<PowerupGenerator>();var second=new GameObject("ClosureNewGenerator").AddComponent<PowerupGenerator>();
  Require(PowerupGenerator.Instance==second&&!PowerupGenerator.CanReroll,"source singleton installs the new generator and clears reroll");PowerupGenerator.Instance=actual;yield return null;Require(first==null&&second!=null&&game.powerupGenerator==actual,"source singleton destroys only the previous directed test owner");Object.Destroy(second.gameObject);
 }
 IEnumerator CheckParityClosure(string mode)
 {
  Log("fixture-precondition","Isolated native player; source0.03 physics, stopped encounter/game clocks, acquired real native content and exact checkpoint Continue. Explicit chance seeds, contact geometry, multishot operands, nested-state and duplicate-singleton edges are directed cases, not natural frequency/full-run claims.");
  if(mode.Contains("-glass-")){yield return ClosureGlass(mode.EndsWith("continue"));yield break;}
  if(mode.Contains("-impact-")){yield return ClosureImpact(mode.EndsWith("continue"));yield break;}
  yield return ClosureGameplay(mode.EndsWith("continue"));
 }
}
public sealed class ClosureNestedState:State
{
 public static int Attempts;
 public override void Enter(){Attempts++;GetComponent<PlayerController>().ChangeState<ClosureOtherState>();}
}
public sealed class ClosureOtherState:State{}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class ClosureProjectileProbe
{static void Prefix(Vector2 direction){if(RuntimeChecks.ClosureCapture)RuntimeChecks.ClosureDirections.Add(direction);}}
[HarmonyPatch]
class ClosureAbbyProbe
{
 static System.Reflection.MethodBase TargetMethod()=>AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidAbby"),"Shoot");
 static void Prefix(){if(RuntimeChecks.ClosureMode)RuntimeChecks.ClosureAbbyShot=true;}
 static void Postfix(){RuntimeChecks.ClosureAbbyShot=false;}
}
[HarmonyPatch(typeof(SoundEffectSO),nameof(SoundEffectSO.Play))]
class ClosureSoundProbe
{
 static void Prefix(SoundEffectSO __instance)
 {
  // The final ammo event synchronously enters ReloadState and plays its own
  // reload cue before a late fixture listener. Observe the actual gun cue,
  // rather than incorrectly counting that different native sound as a shot.
  if(RuntimeChecks.ClosureAbbyShot&&__instance==PlayerController.Instance.gun.gunData.gunshotSFX)RuntimeChecks.ClosureShotEvents.Add("sound");
  if(RuntimeChecks.ClosureActivation)RuntimeChecks.ClosureSounds++;
 }
}
[HarmonyPatch(typeof(ThunderGenerator),nameof(ThunderGenerator.GenerateAt),new[]{typeof(GameObject),typeof(int)})]
class ClosureThunderProbe
{static void Prefix(){if(RuntimeChecks.ClosureObserveImpact)RuntimeChecks.ClosureThunders++;}}
