using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.AISpecials;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool EnemyAttackMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-enemy-attacks-");
 internal sealed class EnemyShot
 {internal AIComponent AI;internal AIShootSpecial Special;internal GameObject Actor;internal Vector3 Origin;internal Vector2 Velocity;internal float Started,Returned;}
 internal static readonly List<EnemyShot> EnemyShots=new List<EnemyShot>();
 internal static EnemyShot EnemyShooting;
 internal static int EnemyUnscopedShots,EnemyPlayerContacts,EnemyHomingSteps;
 internal static GameObject EnemyContactTarget;
 internal static GameObject EnemyRecontactRoot;
 internal static EnemyLob EnemyContactLob;
 internal static readonly HashSet<GameObject> EnemyRhogogContacts=new HashSet<GameObject>();
 internal static readonly HashSet<GameObject> EnemyRhogogConsumed=new HashSet<GameObject>();
 internal static readonly Dictionary<GameObject,int> EnemyRhogogContactCounts=new Dictionary<GameObject,int>();
 internal static bool EnemySuccessfulDodge,EnemyObserveHoming;
 internal sealed class EnemySpawnBatch{internal Spawner Owner;internal readonly List<GameObject> Children=new List<GameObject>();}
 internal static EnemySpawnBatch EnemySpawning;
 internal static readonly List<EnemySpawnBatch> EnemyBatches=new List<EnemySpawnBatch>();
 internal sealed class EnemyLob
 {internal EnemyAreaProjectile Projectile;internal GameObject Indicator,Explosion;internal Vector2 Target;internal Vector3 Start;internal float Duration;internal bool Landed,ReturnedProjectile,ReturnedIndicator,ExplosionArmed;}
 internal static readonly List<EnemyLob> EnemyLobs=new List<EnemyLob>();
 internal static EnemyLob EnemyLanding;
 internal static void EnemyCheck(bool condition,string criterion)=>Require(condition,criterion);
 internal static object EnemyField(object owner,string name)=>WindField(owner,name);
 internal static void EnemyClose(float actual,float expected,string contract,float tolerance=.002f)=>Require(Mathf.Abs(actual-expected)<tolerance,contract+" (actual="+actual+", expected="+expected+")");
 static GameObject EnemyOriginal(string name)=>Resources.FindObjectsOfTypeAll<GameObject>().First(g=>g.name==name&&!g.scene.IsValid());
 static GameObject EnemyActor(string name,Vector3 offset)
 {
  var pool=ObjectPooler.SharedInstance;pool.AddObject(name,EnemyOriginal(name),1);var actor=pool.GetPooledObject(name);actor.transform.position=PlayerController.Instance.transform.position+offset;
  actor.GetComponent<MoveComponent2D>().vector=Vector2.zero;actor.GetComponent<AIComponent>().frozen=false;return actor;
 }
 internal static void EnemyParkShot(EnemyShot shot)
 {
  shot.Actor.transform.position=PlayerController.Instance.transform.position+Vector3.up*3;
  var move=shot.Actor.GetComponent<MoveComponent2D>();move.vector=Vector2.zero;move.enabled=false;
  var home=shot.Actor.GetComponent<HomeTowardsPlayer>();if(home!=null)home.enabled=false;
 }
 IEnumerator EnemyFire(string owner,string special)
 {
  int previous=EnemyShots.Count;var actor=EnemyActor(owner,Vector3.right*2);var ai=actor.GetComponent<AIComponent>();ai.specialTimer=0;
  SetPhase("enemy-ai-"+owner);while(!EnemyShots.Skip(previous).Any(s=>s.Special.name==special))yield return null;
  var shot=EnemyShots.Skip(previous).First(s=>s.Special.name==special);
  Require(shot.Actor!=null&&shot.Actor.activeSelf,"native AI special synchronously emits its actual pooled projectile: "+special);
  CombatClose(Vector3.Distance(shot.Origin,ai.specialPoint.position),0,"actual source shot begins at specialPoint");
  float expected=(float)WindField(shot.Special,"projectileSpeed");CombatClose(shot.Velocity.magnitude,expected,"actual shooting-special speed: "+special);
  Require(shot.Actor.GetComponent<DisableOnCollision>()==null,"owned enemy projectile omits native DisableOnCollision");
  Require(shot.Actor.GetComponent<AndroidProjectile>()==null||!(bool)WindField(shot.Actor.GetComponent<AndroidProjectile>(),"manual"),"enemy shot does not join the player projectile sampler");
  Log("enemy-source-shot",special+" pool="+shot.Actor.name+" speed="+shot.Velocity.magnitude);
 }
 IEnumerator EnemyShotContact(EnemyShot shot,bool protectedHit,bool dodge)
 {
  var p=PlayerController.Instance;var health=p.playerHealth;
  while((bool)WindField(health.GetComponent<AndroidPlayerCollision>(),"OnHitInvicibility"))yield return null;
  if(protectedHit)health.isInvincible.Flip();
  if(dodge){p.stats[StatType.Dodge].AddMultiplierBonus(1);p.stats[StatType.DodgeCapMod].AddFlatBonus(40);}
  int hp=health.hp,contacts=EnemyPlayerContacts;EnemySuccessfulDodge=dodge;EnemyContactTarget=shot.Actor;
  EnemyParkShot(shot);shot.Actor.transform.position=health.transform.position+Vector3.right*.1f;Physics2D.SyncTransforms();
  while(EnemyPlayerContacts==contacts)yield return null;EnemyContactTarget=null;
  Require(health.hp==hp-(protectedHit||dodge?0:1),"actual enemy projectile contact preserves source damage/protection/dodge order");
  Require(shot.Actor.activeSelf==dodge,"source player contact returns ordinary/protected shots but retains a successfully dodged shot");
  if(protectedHit)health.isInvincible.UnFlip();
  if(dodge){p.stats[StatType.Dodge].AddMultiplierBonus(-1);p.stats[StatType.DodgeCapMod].AddFlatBonus(-40);ReturnFixtureActor(shot.Actor);}
  Log("enemy-player-contact","protected="+protectedHit+" dodge="+dodge+" HP="+health.hp+" returned="+!shot.Actor.activeSelf);
 }
 IEnumerator EnemyShootingWorkflow()
 {
  var p=PlayerController.Instance;var health=p.playerHealth;int beginning=EnemyShots.Count;
  foreach(var pair in new[]{new[]{"EyeMonster","EyeMonsterShoot"},new[]{"PF_Cultist","CultistShoot"},new[]{"PF_EyeBat","EyeBatShoot"},new[]{"PF_HasturBoss","HasturShoot"}})yield return EnemyFire(pair[0],pair[1]);
  var firstEye=EnemyShots.Skip(beginning).First(s=>s.Special.name=="EyeMonsterShoot");
  while(firstEye.Actor.activeSelf)yield return null;
  EnemyClose(firstEye.Returned-firstEye.Started,6,"source Eye projectile returns after its six-second lifetime",.08f);
  while(EnemyShots.Skip(beginning).Any(s=>s.Returned==0))yield return null;
  foreach(var shot in EnemyShots.Skip(beginning).Where(s=>s.Special.name!="EyeMonsterShoot"))
   EnemyClose(shot.Returned-shot.Started,3,"source Cultist-derived projectile reaches its three-second lifetime without native extra bursts",.08f);
  Require(EnemyUnscopedShots==0,"all four actual AI shooting specials emit synchronously; no native delayed burst projectile remains");
  foreach(var actor in EnemyShots.Select(s=>s.AI.gameObject).Distinct().Where(a=>a.activeSelf).ToArray())ReturnFixtureActor(actor);
  EnemyObserveHoming=true;yield return EnemyFire("PF_Cultist","CultistShoot");var homing=EnemyShots.Last();
  var home=homing.Actor.GetComponent<HomeTowardsPlayer>();home.enabled=true;homing.Actor.GetComponent<MoveComponent2D>().enabled=true;
  while(EnemyHomingSteps==0)yield return null;EnemyObserveHoming=false;EnemyParkShot(homing);
  health.isInvincible.UnFlip();Require(!health.isInvincible.value,"remove only the fixture's protection before actual projectile damage");
  yield return EnemyShotContact(homing,false,false);
  yield return EnemyFire("EyeMonster","EyeMonsterShoot");yield return EnemyShotContact(EnemyShots.Last(),true,false);
  yield return EnemyFire("PF_Cultist","CultistShoot");yield return EnemyShotContact(EnemyShots.Last(),false,true);
  health.isInvincible.Flip();WorldReturnEnemies();
 }
 IEnumerator EnemySecondaryWorkflow()
 {
  foreach(var tuple in new[]{new[]{"PF_Yog","PF_YogsBoomer","3","5"},new[]{"SpawnerMonster","PF_SpawnedBug","6","2"},new[]{"PF_Reaper","PF_EyeBat","2","3"}})
  {
   SetPhase("enemy-secondary-"+tuple[0]);int previous=EnemyBatches.Count;var actor=EnemyActor(tuple[0],Vector3.right*2);
   while(!EnemyBatches.Skip(previous).Any(b=>b.Owner.transform.IsChildOf(actor.transform)))yield return null;
   var batch=EnemyBatches.Skip(previous).First(b=>b.Owner.transform.IsChildOf(actor.transform));var points=(Transform[])WindField(batch.Owner,"spawnPoints");
   Require(batch.Children.Count==int.Parse(tuple[2])&&batch.Children.Count==points.Length,"real AI/animation Spawner emits each original child point: "+tuple[0]);
   Require(batch.Children.All(c=>c.name==tuple[1]),"actual secondary producer retains source child identity");
   CombatClose((float)WindField(batch.Owner,"initialSpeed"),float.Parse(tuple[3]),"original secondary speed");
   Log("enemy-secondary",tuple[0]+" -> "+tuple[1]+" count="+batch.Children.Count);WorldReturnEnemies();
  }
 }
 IEnumerator EnemyForestAttack(bool interrupt)
 {
  var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;var forest=Object.FindObjectOfType<ForestMapRhogogSpawner>();var trees=(Health[])WindField(forest,"treeHealths");
  var active=trees.Where(t=>t.gameObject.activeSelf).ToArray();Require(active.Length>0&&active.All(t=>t.maxHP==90000),"actual owned Forest trees import source HP90000");
  var positions=active.Select(t=>t.transform.position).ToArray();var walk=(GameObject)WindField(forest,"walkingTreePrefab");int previous=ObjectPooler.SharedInstance.pooledObjectsDictionary[walk.name].Count;
  active[0].AutoKill();yield return null;
  var walking=ObjectPooler.SharedInstance.pooledObjectsDictionary[walk.name].Where(a=>a.activeSelf).ToArray();
  Require((bool)WindField(forest,"treesActivated")&&walking.Length==positions.Length&&active.All(t=>!t.gameObject.activeSelf),"real scoped tree death replaces every active source tree including the dying tree before release");
  foreach(var position in positions)Require(walking.Any(a=>Vector3.Distance(a.transform.position,position)<1),"source walking tree is acquired at an original tree position");WorldReturnEnemies();
  var timer=GameTimer.SharedInstance;WorldTime(timer.timeLimit-1.01f);Plugin.SetField(timer,"oneSecondWarningSent",false);timer.Start();var prefab=(GameObject)WindField(forest,"rhogogPrefab");
  GameObject boss=null;while(boss==null){boss=ObjectPooler.SharedInstance.pooledObjectsDictionary[prefab.name].FirstOrDefault(a=>a.activeSelf);yield return null;}
  EnemyClose(Vector3.Distance(boss.transform.position,p.transform.position+Vector3.right*20),0,"real warning notification spawns Rhogog twenty units right without an arena",.4f);
  float stopped=timer.timer;int clock=WorldMilliseconds;while(WorldMilliseconds==clock)yield return null;Require(timer.timer==stopped,"Rhogog stops run time while the source independent clock still advances");
  int start=EnemyLobs.Count;boss.transform.position=p.transform.position+Vector3.right*2;var ai=boss.GetComponent<AIComponent>();
  while(EnemyLobs.Count==start)yield return null;ai.frozen=true;ai.moveComponent.vector=Vector2.zero;
  var lobs=EnemyLobs.Skip(start).ToArray();Require(lobs.Length==51&&lobs[0].Duration==1&&lobs.Skip(1).All(l=>l.Duration>=.70968f&&l.Duration<=1.3194f),"actual Rhogog AI emits one targeted and fifty scatter arcs with source duration operands");
  Require(lobs.All(l=>l.Indicator.activeSelf&&Vector2.Distance(l.Indicator.transform.position,l.Target)<.001f),"all actual lobbed indicators use positioned source pool acquisition");
  if(interrupt)
  {
   WorldTime(12.34f);yield return DragonSuspend(game,"Actual AI/animation enemy attacks and active Rhogog51-flight scene exit; explicit elapsed12.34 checkpoint omits source-unsaved world actors/projectiles/tree phase");yield break;
  }
  EnemyContactLob=lobs[0];while(lobs.Any(l=>!l.Landed))yield return null;
  Require(lobs.All(l=>l.ReturnedProjectile&&l.ReturnedIndicator&&l.Explosion!=null),"real LeanTween landing returns every flight/indicator and acquires the source explosion");
  var explosions=ObjectPooler.SharedInstance.pooledObjectsDictionary["RhogogSporeExplosion"].Where(a=>a.activeSelf).ToArray();
  Require(explosions.Length>0,"actual Rhogog scatter has live source explosion roots");
  var explosion=explosions[0];var damage=explosion.transform.Find("Damage").gameObject;
  Require(damage.GetComponent<TimeToLive>()==null,"source explosion child omits native short-lived damage timer");
  CombatClose((float)WindField(explosion.GetComponent<TimeToLive>(),"lifetime"),.3f,"source explosion root lifetime");
  while(explosions.Any(e=>e.activeSelf))yield return null;
  var hitRoot=lobs[0].Explosion;var hitChild=hitRoot.transform.Find("Damage").gameObject;
  Require(EnemyRhogogContacts.Contains(hitChild)&&EnemyRhogogConsumed.Contains(hitChild),"actual targeted Rhogog landing physically contacts the protected player and returns only its unregistered Damage child");
  EnemyContactLob=null;
  // Other scatter landings can legitimately reuse/re-arm this root before
  // the whole attack expires. Restore the actually observed consumed state
  // for the directed next-acquisition regression, without a synthetic hit.
  hitChild.SetActive(false);Log("fixture-pool-state","Restore the child state observed at actual protected contact; later scatter reuse is not assumed to leave it disabled.");
  int priorContacts=EnemyRhogogContactCounts[hitChild];
  // Isolate the already-contacted root at the top of the real returned stack.
  // These explicit pool-order operands do not claim a natural reuse frequency.
  var ordered=new List<GameObject>();int allocated=ObjectPooler.SharedInstance.pooledObjectsDictionary["RhogogSporeExplosion"].Count;
  for(int i=0;i<allocated;i++){var value=ObjectPooler.SharedInstance.GetPooledObject("RhogogSporeExplosion");ordered.Add(value);if(value==hitRoot)break;}
  Require(ordered.Last()==hitRoot,"actual source pool still owns the contacted explosion root");
  foreach(var value in ordered.Where(v=>v!=hitRoot))ReturnFixtureActor(value);ReturnFixtureActor(hitRoot);
  EnemyRecontactRoot=hitRoot;int second=EnemyLobs.Count;ai.specialTimer=0;ai.frozen=false;
  while(EnemyLobs.Count==second)yield return null;ai.frozen=true;ai.moveComponent.vector=Vector2.zero;
  var reused=EnemyLobs.Skip(second).ToArray();Require(reused.Length==51,"real Rhogog AI starts a second complete source attack");
  while(reused.Any(l=>!l.Landed))yield return null;
  Require(reused.Any(l=>l.Explosion==hitRoot&&l.ExplosionArmed),"M15 A242 actual second AI attack re-arms the contacted root's existing Damage child before physics");
  Log("enemy-explosion-reuse","Protected contact consumes the current child; the next native AI attack re-arms that same pooled root");
  while(ObjectPooler.SharedInstance.pooledObjectsDictionary["RhogogSporeExplosion"].Any(a=>a.activeSelf))yield return null;
  EnemyRecontactRoot=null;
  Require(EnemyRhogogContactCounts[hitChild]>priorContacts,"M15 A242 re-armed reused explosion makes another actual player physics contact");
  var health=boss.GetComponent<Health>();while(health.isInvincible.value)yield return null;health.TakeDamage(DamageType.Bullet,health.maxHP);
  while(!(game.CurrentState is PlayerSurvivedState))yield return null;yield return AchievementExit(game,true);
  Log("enemy-forest-victory","Actual tree death -> walking pools -> warning/independent clock -> Rhogog AI51 arcs -> source landing/explosion expiry -> real boss death/native victory");
 }
 IEnumerator CheckAndroidEnemyAttacks(bool continuation)
 {
  Log("fixture-precondition","Real native AI specials/animation/physics and Forest death/warning/landing/exit entries. Scheduler disabled after native entry; enemy positions, contact positions, dodge rolls, final clock/boss damage and checkpoint elapsed are explicit operands. No natural difficulty/RNG-frequency/full-run claim.");
  if(continuation)
  {
   yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();GameTimer.SharedInstance.Stop();StopFixtureSpawning();
   Require(EnemyLobs.Count==0&&EnemyShots.Count==0,"exact source Continue does not replay omitted live enemy projectile/AI phases");
   var forest=Object.FindObjectOfType<ForestMapRhogogSpawner>();Require(!(bool)WindField(forest,"treesActivated"),"source-unsaved Forest activation reconstructs its original tree phase");
   Require(ObjectPooler.SharedInstance.pooledObjectsDictionary.Where(k=>k.Key=="EyeMonsterProjectile"||k.Key=="CultistProjectile"||k.Key=="RhogogSporeExplosion").All(k=>k.Value.All(a=>!a.activeSelf)),"fresh Continue has no retained attack/explosion world objects");
   yield return EnemyFire("EyeMonster","EyeMonsterShoot");WorldReturnEnemies();yield return AchievementExit(game,false);
   yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();Require(EnemyLanding==null&&EnemySpawning==null&&EnemyShooting==null,"native ordinary restart retains no in-flight source owner");
   yield return CheckRevolver(game,PlayerController.Instance,"android-enemy-attacks-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Enemy attack exact source Continue and ordinary native restart");Application.Quit(0);yield break;
  }
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());StopFixtureSpawning();
  yield return EnemyShootingWorkflow();yield return EnemySecondaryWorkflow();yield return EnemyForestAttack(false);
  yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());StopFixtureSpawning();yield return EnemyForestAttack(true);
 }
}
[HarmonyPatch(typeof(AIShootSpecial),nameof(AIShootSpecial.Use))]
class EnemyShootProbe
{
 [HarmonyPriority(Priority.First)]static void Prefix(AIShootSpecial __instance,AIComponent ai){if(RuntimeChecks.EnemyAttackMode)RuntimeChecks.EnemyShooting=new RuntimeChecks.EnemyShot{AI=ai,Special=__instance,Started=Time.time};}
 static void Postfix()
 {var shot=RuntimeChecks.EnemyShooting;if(shot==null)return;RuntimeChecks.EnemyCheck(shot.Actor!=null,"one actual source pool acquisition occurs within AIShootSpecial.Use");shot.Origin=shot.Actor.transform.position;shot.Velocity=shot.Actor.GetComponent<MoveComponent2D>().vector;RuntimeChecks.EnemyParkShot(shot);shot.AI.frozen=true;shot.AI.moveComponent.vector=Vector2.zero;RuntimeChecks.EnemyShots.Add(shot);RuntimeChecks.EnemyShooting=null;}
}
[HarmonyPatch(typeof(ObjectPooler),nameof(ObjectPooler.GetPooledObject))]
class EnemyPoolProbe
{
 static void Postfix(string tag,GameObject __result)
 {
  if(!RuntimeChecks.EnemyAttackMode||__result==null)return;
  if(tag=="EyeMonsterProjectile"||tag=="CultistProjectile")
  {var shot=RuntimeChecks.EnemyShooting;if(shot==null)RuntimeChecks.EnemyUnscopedShots++;else{RuntimeChecks.EnemyCheck(shot.Actor==null,"exactly one projectile per source shooting-special call");shot.Actor=__result;}}
 }
}
[HarmonyPatch(typeof(AndroidPools),"Return")]
class EnemyReturnProbe
{
 static void Prefix(GameObject __0)
 {
  var actor=__0;if(!RuntimeChecks.EnemyAttackMode)return;foreach(var shot in RuntimeChecks.EnemyShots.Where(s=>s.Actor==actor&&s.Returned==0))shot.Returned=Time.time;
  var land=RuntimeChecks.EnemyLanding;if(land!=null){if(actor==land.Projectile.gameObject)land.ReturnedProjectile=true;if(actor==land.Indicator)land.ReturnedIndicator=true;}
 }
}
[HarmonyPatch(typeof(AndroidPlayerCollision),"Enter")]
class EnemyPlayerContactProbe
{
 static void Postfix(Collision2D collision)
 {
  var child=collision.gameObject;
  if(RuntimeChecks.EnemyAttackMode&&collision.transform.parent!=null&&collision.transform.parent.name=="RhogogSporeExplosion"&&!child.activeSelf)
   RuntimeChecks.EnemyRhogogConsumed.Add(child);
 }
 [HarmonyPriority(Priority.First)]static void Prefix(Collision2D collision)
 {
  if(RuntimeChecks.EnemyAttackMode&&collision.transform.parent!=null&&collision.transform.parent.name=="RhogogSporeExplosion")
  {var child=collision.gameObject;RuntimeChecks.EnemyRhogogContacts.Add(child);RuntimeChecks.EnemyRhogogContactCounts.TryGetValue(child,out int count);RuntimeChecks.EnemyRhogogContactCounts[child]=count+1;}
  if(collision.gameObject!=RuntimeChecks.EnemyContactTarget)return;RuntimeChecks.SeedFirstRoll(RuntimeChecks.EnemySuccessfulDodge?0:.99f,RuntimeChecks.EnemySuccessfulDodge?.01f:1);RuntimeChecks.EnemyPlayerContacts++;
 }
}
[HarmonyPatch(typeof(HomeTowardsPlayer),"FixedUpdate")]
class EnemyHomingProbe
{
 [HarmonyPriority(Priority.First)]static void Prefix(HomeTowardsPlayer __instance,out Vector2 __state)=>__state=__instance.GetComponent<MoveComponent2D>().vector;
 [HarmonyPriority(Priority.First)]static void Postfix(HomeTowardsPlayer __instance,Vector2 __state)
 {if(!RuntimeChecks.EnemyObserveHoming)return;Vector2 direction=PlayerController.Instance.transform.position-__instance.transform.position;var expected=__state+direction.normalized*(float)RuntimeChecks.EnemyField(__instance,"acceleration")*Time.fixedDeltaTime;RuntimeChecks.EnemyClose(Vector2.Distance(expected,__instance.GetComponent<MoveComponent2D>().vector),0,"real FixedUpdate applies source unnormalized homing acceleration");RuntimeChecks.EnemyHomingSteps++;}
}
[HarmonyPatch(typeof(Spawner),nameof(Spawner.Spawn))]
class EnemySecondaryProbe
{
 [HarmonyPriority(Priority.First)]static void Prefix(Spawner __instance){if(RuntimeChecks.EnemyAttackMode)RuntimeChecks.EnemySpawning=new RuntimeChecks.EnemySpawnBatch{Owner=__instance};}
 static void Postfix()
 {
  var batch=RuntimeChecks.EnemySpawning;if(batch==null)return;
  foreach(var child in batch.Children)
  {
   var move=child.GetComponent<MoveComponent2D>();if(move==null)continue;
   var expected=(child.transform.position-batch.Owner.transform.position).normalized*(float)RuntimeChecks.EnemyField(batch.Owner,"initialSpeed");
   RuntimeChecks.EnemyClose(Vector2.Distance(move.vector,expected),0,"actual secondary Spawn assigns source outward velocity before returning");
  }
  RuntimeChecks.EnemyBatches.Add(batch);batch.Owner.GetComponentInParent<AIComponent>().frozen=true;RuntimeChecks.EnemySpawning=null;
 }
}
[HarmonyPatch]
class EnemyPositionedPoolProbe
{
 static IEnumerable<MethodBase> TargetMethods()=>AccessTools.GetDeclaredMethods(typeof(AndroidPools)).Where(m=>m.Name=="Get");
 static void Postfix(GameObject __result)
 {
  if(!RuntimeChecks.EnemyAttackMode||__result==null)return;
  var batch=RuntimeChecks.EnemySpawning;if(batch!=null)
  {var points=(Transform[])RuntimeChecks.EnemyField(batch.Owner,"spawnPoints");RuntimeChecks.EnemyClose(Vector3.Distance(__result.transform.position,points[batch.Children.Count].position),0,"actual secondary acquisition is positioned before velocity assignment");batch.Children.Add(__result);}
  if(RuntimeChecks.EnemyLanding!=null&&__result.name=="RhogogSporeExplosion")
  {
   RuntimeChecks.EnemyLanding.Explosion=__result;RuntimeChecks.EnemyLanding.ExplosionArmed=__result.transform.Find("Damage").gameObject.activeSelf;
   // Pool order binds the root to the first landing, which may be a scatter
   // rather than the targeted arc. Place the real player at that exact landing
   // before physics; do not invoke/synthesize the contact callback.
   if(__result==RuntimeChecks.EnemyRecontactRoot||ReferenceEquals(RuntimeChecks.EnemyLanding,RuntimeChecks.EnemyContactLob)){PlayerController.Instance.SetPosition(__result.transform.position);Physics2D.SyncTransforms();}
  }
 }
}
[HarmonyPatch(typeof(EnemyAreaProjectile),nameof(EnemyAreaProjectile.TargetPos))]
class EnemyLobProbe
{
 static void Postfix(EnemyAreaProjectile __instance,Vector2 pos,float duration)
 {if(!RuntimeChecks.EnemyAttackMode)return;RuntimeChecks.EnemyLobs.Add(new RuntimeChecks.EnemyLob{Projectile=__instance,Indicator=(GameObject)RuntimeChecks.EnemyField(__instance,"_indicator"),Start=__instance.transform.position,Target=pos,Duration=duration});}
}
[HarmonyPatch(typeof(EnemyAreaProjectile),"OnTargetReached")]
class EnemyLandingProbe
{
 [HarmonyPriority(Priority.First)]static void Prefix(EnemyAreaProjectile __instance){if(RuntimeChecks.EnemyAttackMode)RuntimeChecks.EnemyLanding=RuntimeChecks.EnemyLobs.Last(l=>l.Projectile==__instance&&!l.Landed);}
 static void Postfix(){var land=RuntimeChecks.EnemyLanding;if(land==null)return;RuntimeChecks.EnemyClose(Vector2.Distance(land.Explosion.transform.position,land.Target),0,"actual explosion begins at its source indicator landing point");land.Landed=true;RuntimeChecks.EnemyLanding=null;}
}
