using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using UnityEngine.Events;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool ObserveSourceProjectiles;internal static Projectile LastSourceProjectile;
 GameObject projectileObserved;Projectile observedShot;readonly List<string> projectileOrder=new List<string>();readonly List<DamageType> projectileKinds=new List<DamageType>();
 readonly Dictionary<string,GameObject> projectileAssets=new Dictionary<string,GameObject>();
 void PrepareProjectileAssets()
 {
  var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();var guns=NativeSelection.GunItems(title.gunMenu).Select(i=>i.data).ToArray();
  var root=new GameObject("Validation_Source_Projectile_Assets");root.SetActive(false);Object.DontDestroyOnLoad(root);
  var prefabs=new List<GameObject>{guns.Single(g=>g.name=="GrenadeLauncherData").bullet};
  prefabs.Add(guns.Select(g=>g.bullet).First(p=>p.GetComponent<SpawnOnDisableProjectile>()!=null));
  var shadows=Resources.FindObjectsOfTypeAll<ShootingSummon>().Where(s=>s.SummonTypeID=="ShadowClone").Select(s=>(GameObject)WindField(s,"projectilePrefab")).Distinct().ToArray();Require(shadows.Length==1,"fixture resolves one original ShadowClone projectile through its actual summon reference");prefabs.Add(shadows[0]);
  foreach(var prefab in prefabs){var copy=Object.Instantiate(prefab,root.transform);copy.name=prefab.name;copy.SetActive(false);projectileAssets.Add(copy.name,copy);}
 }
 int impactHp,killHp,bounceNotices;object bouncePayload;Vector2 bounceDirection;int firstContactHp;float firstSize;int firstBounces;
 void ProjectileImpact(object sender,object args)
 {if(args as GameObject!=projectileObserved)return;if(impactHp<0)impactHp=projectileObserved.GetComponent<Health>().HP;projectileOrder.Add("impact");}
 void ProjectileKill(object sender,object args)
 {if(args as GameObject!=projectileObserved)return;killHp=projectileObserved.GetComponent<Health>().HP;projectileOrder.Add("kill");}
 void ProjectileTweak(object sender,object args){if((sender as Projectile)==observedShot)projectileOrder.Add("tweak");}
 void ProjectileBounce(object sender,object args){if((sender as Projectile)==observedShot){bounceNotices++;bouncePayload=args;}}
 void ProjectileHurtKind(object sender,object args){if((sender as Health)?.gameObject==projectileObserved)projectileKinds.Add((DamageType)args);}
 Health ProjectileEnemy(Vector3 position,int hp=1000,int max=1000)
 {
  var actor=ObjectPooler.SharedInstance.GetPooledObject("Validation_Wizard_Enemy");var health=DurableEnemy(actor,position,false);actor.SetActive(true);health.maxHP=max;Plugin.SetField(health,"<HP>k__BackingField",hp);actor.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;actor.GetComponent<CircleCollider2D>().radius=.25f;return health;
 }
 ProjectileRecipe ProjectileRecipe(float damage=8,float speed=50,int pierce=0,int bounce=0,string pool=null)
 {
  var player=PlayerController.Instance;var recipe=(ProjectileRecipe)AccessTools.Method(typeof(Gun),"GetProjectileRecipe").Invoke(player.gun,null);
  recipe.damage=damage;recipe.projectileSpeed=speed;recipe.size=1;recipe.knockback=0;recipe.piercing=pierce;recipe.bounce=bounce;if(pool!=null)recipe.objectPoolTag=pool;return recipe;
 }
 IEnumerator ProjectileContact(ProjectileRecipe recipe,Health target,Vector3? start=null)
 {
  projectileObserved=target.gameObject;projectileOrder.Clear();projectileKinds.Clear();impactHp=killHp=-1;firstContactHp=-1;bounceNotices=0;bouncePayload=new object();
  observedShot=ProjectileFactory.SharedInstance.SpawnProjectile(recipe,Vector2.right,start??PlayerController.Instance.transform.position);
  UnityAction<int> hurt=d=>{if(firstContactHp<0){firstContactHp=target.HP;firstSize=observedShot.transform.localScale.x;firstBounces=observedShot.bounce;bounceDirection=observedShot.GetComponent<MoveComponent2D>().vector;}projectileOrder.Add("hurt");};target.onHurt.AddListener(hurt);
  try
  {
   Physics2D.SyncTransforms();
   while(firstContactHp<0&&!target.isDead){if(!observedShot.gameObject.activeSelf)Require(false,"real source projectile reaches its target before deactivation");yield return null;}
   observedShot.gameObject.SetActive(false);yield return new WaitForEndOfFrame();
  }
  finally{target.onHurt.RemoveListener(hurt);}
  Log("projectile-contact","order="+string.Join(",",projectileOrder)+" impactHP="+impactHp+" killHP="+killHp+" firstHP="+firstContactHp+" bounces="+firstBounces);
 }
 void MoveLensesAway()
 {
  foreach(var lens in PlayerController.Instance.GetComponentsInChildren<BulletEnhanceSummon>())
  {foreach(var orbit in lens.GetComponents<MonoBehaviour>().Where(c=>c.GetType().Name.Contains("Orbit")))orbit.enabled=false;lens.transform.position=PlayerController.Instance.transform.position+Vector3.up*30;}
  Physics2D.SyncTransforms();
 }
 IEnumerator CheckSharedProjectiles(GameController game,bool continuation)
 {
  var player=PlayerController.Instance;var pool=ObjectPooler.SharedInstance;
  foreach(var pair in projectileAssets)if(!pool.pooledObjectsDictionary.ContainsKey(pair.Key))pool.AddObject(pair.Key,pair.Value,2);
  this.AddObserver(ProjectileImpact,Projectile.ImpactEvent);this.AddObserver(ProjectileKill,Projectile.KillEvent);this.AddObserver(ProjectileTweak,Projectile.TweakPierceBounce);this.AddObserver(ProjectileBounce,Projectile.BounceEvent);this.AddObserver(ProjectileHurtKind,"Revamped.AndroidSurvivingHurt");
  ColdNoFreeze=true;
  ObserveSourceProjectiles=true;
  try
  {
   MoveLensesAway();Log("fixture-precondition","Native factory/physics contacts use explicit damage8, fixed enemy geometry, zero knockback and selected speeds/pierce/bounce; native OnHit chance is fixed to fail other acquired elemental procs. No natural spawn, DPS or frequency claim.");
   var target=ProjectileEnemy(player.transform.position+Vector3.right*3,100,100);yield return ProjectileContact(ProjectileRecipe(),target);
   Require(impactHp==100&&projectileOrder.Take(3).SequenceEqual(new[]{"impact","tweak","hurt"}),"actual ordinary projectile dispatches impact/tweak before its separate damage component");target.gameObject.SetActive(false);
   target=ProjectileEnemy(player.transform.position+Vector3.right*3,7,7);yield return ProjectileContact(ProjectileRecipe(),target);
   Require(killHp==7&&projectileOrder.Take(3).SequenceEqual(new[]{"kill","impact","tweak"}),"source predictive projectile kill precedes actual damage/death");target.gameObject.SetActive(false);
   Log("fixture-precondition","Native gun threshold targets use radius0.7 to include ordinary Revolver shot spread; the narrower0.25 targets belong to the controlled factory-geometry cases.");
   target=ProjectileEnemy(player.transform.position+Vector3.right*3,21,100);target.GetComponent<CircleCollider2D>().radius=.7f;yield return ColdShot(game,target,false);
   target=ProjectileEnemy(player.transform.position+Vector3.right*3,20,100);target.GetComponent<CircleCollider2D>().radius=.7f;target.tag="EnemyChampion";yield return ColdShot(game,target,true);
   ColdNoFreeze=true;
   target=ProjectileEnemy(player.transform.position+Vector3.right*3);var second=ProjectileEnemy(player.transform.position+Vector3.right*5);
   yield return ProjectileContact(ProjectileRecipe(speed:1000,pierce:1),target);
   Require(firstContactHp==992&&second.HP==992,"fast real projectile samples the crossed segment and pierces both targets without duplicate collider hits");target.gameObject.SetActive(false);second.gameObject.SetActive(false);
   target=ProjectileEnemy(player.transform.position+Vector3.right*3);second=ProjectileEnemy(player.transform.position+Vector3.right*5);
   yield return ProjectileContact(ProjectileRecipe(speed:10,bounce:1),target);
   Require(bounceDirection.x<0&&Mathf.Abs(bounceDirection.magnitude-10)<.01f&&second.HP==1000,"actual source bounce reflects with random spread instead of homing toward the next enemy");Require(bounceNotices==1&&bouncePayload==null,"actual bounce emits one null-payload notification after end of frame, even when the shot has returned");target.gameObject.SetActive(false);second.gameObject.SetActive(false);
   if(!continuation)
   {
    yield return AndroidPick(game,"MagicLens",game.playerXP.level);yield return AndroidPick(game,"Refraction",game.playerXP.level);yield return AndroidPick(game,"IgnitingLens",game.playerXP.level);yield return AndroidPick(game,"FocalPoint",game.playerXP.level);yield return AndroidPick(game,"FocalPoint",game.playerXP.level);
    var first=player.GetComponentsInChildren<BulletEnhanceSummon>().Single();Require(first.overallMultiplier==3&&first.damageMultiplier==1.3f&&first.additionalBounces==2&&first.addBurn&&Mathf.Abs(first.transform.localScale.x-.25f)<.001f,"actual Lens upgrades use source full repeat factor, shrink and flags");
    yield return AndroidPick(game,"MagicLens",game.playerXP.level);
   }
   var lenses=player.GetComponentsInChildren<BulletEnhanceSummon>();Require(lenses.Length==2&&lenses[0].overallMultiplier==3&&lenses[1].overallMultiplier==1&&lenses[1].damageMultiplier==1.3f&&!lenses[1].addBurn,"late original Lens and Continue retain independent upgraded/pristine actors");
   MoveLensesAway();lenses[0].transform.position=player.transform.position+Vector3.right*1.5f;lenses[1].transform.position=player.transform.position+Vector3.right*3;
   target=ProjectileEnemy(player.transform.position+Vector3.right*5);yield return ProjectileContact(ProjectileRecipe(speed:30),target);
   Require(firstContactHp==969&&Mathf.Abs(firstSize-3.9f)<.001f&&firstBounces==5,"source Lens scales current shot damage/size and adds scaled bounces once across two actual Lens colliders");Require((bool)WindField(observedShot.GetComponent<AndroidProjectile>(),"IsBurn")&&(int)WindField(observedShot.GetComponent<AndroidProjectile>(),"BurnDamage")==9,"source Lens assigns the per-shot burn flag and scaled nine damage");target.gameObject.SetActive(false);MoveLensesAway();
   target=ProjectileEnemy(player.transform.position+Vector3.right*3);yield return ProjectileContact(ProjectileRecipe(),target);Require(firstContactHp==992&&!(bool)WindField(observedShot.GetComponent<AndroidProjectile>(),"IsBurn"),"ordinary pooled reuse resets the earlier Lens burn and shot state");target.gameObject.SetActive(false);
   // Verify the native explosive subtype's real contact and explosion owner,
   // without pretending that the ordinary-bullet handler covers this override.
   var grenade=projectileAssets.Values.Single(g=>g.GetComponent<ExplosiveProjectile>()!=null);target=ProjectileEnemy(player.transform.position+Vector3.right*3);
   yield return ProjectileContact(ProjectileRecipe(damage:40,speed:30,pierce:1,pool:grenade.name),target);
   Require(impactHp==1000&&firstContactHp==980,"actual grenade override emits its source impact before half contact damage");
   observedShot.gameObject.SetActive(false);
   while(pool.pooledObjectsDictionary["GrenadeExplosion"].Any(g=>g.activeSelf))yield return null;
   Require(projectileKinds.Count>=2&&projectileKinds[0]==(DamageType)10&&projectileKinds.Skip(1).All(k=>k==DamageType.Bullet),"grenade contact and actual splash retain separate source Monster/Bullet classifications");target.gameObject.SetActive(false);
   // The source multi-projectile queries a30-slot buffer once per activation.
   MoveLensesAway();var crowd=Enumerable.Range(0,32).Select(i=>ProjectileEnemy(player.transform.position+Vector3.up*8+new Vector3((i%8-3.5f)*.1f,(i/8-1.5f)*.1f,0))).ToArray();
   var used=new HashSet<GameObject>();bool reused=false;
   for(int use=0;use<3;use++)
   {
    foreach(var h in crowd)Plugin.SetField(h,"<HP>k__BackingField",1000);
    var shot=ProjectileFactory.SharedInstance.SpawnProjectile(ProjectileRecipe(speed:0,pierce:99999,pool:"PF_ShadowCloneAttack"),Vector2.right,player.transform.position+Vector3.up*8);reused|=!used.Add(shot.gameObject);Physics2D.SyncTransforms();
    while(crowd.All(h=>h.HP==1000)){if(!shot.gameObject.activeSelf)Require(false,"multi-projectile performs its actual area query while active");yield return null;}
    for(int step=0;step<3;step++)yield return new WaitForFixedUpdate();
    Require(crowd.Count(h=>h.HP==992)==30&&crowd.Count(h=>h.HP==1000)==2,"actual multi-projectile hits at most30 colliders once, including after its third fixed update");shot.gameObject.SetActive(false);
   }
   Require(reused,"actual pooled multi-projectile reuse clears its single-query activation flag");foreach(var h in crowd)h.gameObject.SetActive(false);
   var bow=projectileAssets.Values.Single(g=>g.GetComponent<SpawnOnDisableProjectile>()!=null);var replacement=(GameObject)WindField(bow.GetComponent<SpawnOnDisableProjectile>(),"prefab");
   target=ProjectileEnemy(player.transform.position+Vector3.right*3);yield return ProjectileContact(ProjectileRecipe(pool:bow.name),target);
   Require(pool.pooledObjectsDictionary[replacement.name].Any(g=>g.activeSelf),"actual spawn-on-disable subtype creates its native source counterpart on contact return");target.gameObject.SetActive(false);foreach(var g in pool.pooledObjectsDictionary[replacement.name])g.SetActive(false);
   var secondary=ProjectileFactory.SharedInstance.SpawnProjectile(ProjectileRecipe(speed:0,pool:bow.name),Vector2.right,player.transform.position+Vector3.up*15,1,true);yield return null;secondary.gameObject.SetActive(false);
   Require(pool.pooledObjectsDictionary[replacement.name].All(g=>!g.activeSelf),"secondary spawn-on-disable projectile suppresses the recursive replacement");
   // Existing real gun input is exercised by ColdShot above; the continuation
   // fixture additionally owns the acquired actors/fields and original restart.
  }
  finally
  {ObserveSourceProjectiles=false;ColdNoFreeze=false;projectileObserved=null;this.RemoveObserver(ProjectileImpact,Projectile.ImpactEvent);this.RemoveObserver(ProjectileKill,Projectile.KillEvent);this.RemoveObserver(ProjectileTweak,Projectile.TweakPierceBounce);this.RemoveObserver(ProjectileBounce,Projectile.BounceEvent);this.RemoveObserver(ProjectileHurtKind,"Revamped.AndroidSurvivingHurt");}
 }
}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class SourceProjectileObservation
{
 static void Postfix(Projectile __result)
 {
  if(!RuntimeChecks.ObserveSourceProjectiles)return;RuntimeChecks.LastSourceProjectile=__result;
  RuntimeChecks.Log("native-projectile-spawn","position="+__result.transform.position+" velocity="+__result.GetComponent<MoveComponent2D>().vector+" owner="+__result.owner?.name+" prefab="+__result.name);
 }
}
