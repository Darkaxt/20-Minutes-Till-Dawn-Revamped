using System;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.PowerupSystem;
using System.Collections.Generic;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool PhoenixMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="phoenix-fixed";
 internal static int PhoenixActualSpawns;
 internal static bool PhoenixObserveInput;
 internal static void PhoenixSpawned(ProjectileOnShoot owner,Vector2 direction)
 {
  if(!PhoenixMode||!PhoenixObserveInput)return;
  var prefab=(GameObject)WindField(owner,"projectilePrefab");
  var actor=ObjectPooler.SharedInstance.pooledObjectsDictionary[prefab.name].Last(x=>x.activeSelf);
  float expected=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
  Require(Mathf.Abs(Mathf.DeltaAngle(actor.transform.eulerAngles.z,expected))<.01f,"actual passive spawn publishes visible aim synchronously: "+prefab.name);PhoenixActualSpawns++;
 }
 static string PhoenixPose(GameObject actor)
 {
  var rb=actor.GetComponent<Rigidbody2D>();var move=actor.GetComponent<MoveComponent2D>();
  return actor.name+" angle="+actor.transform.eulerAngles.z+" rb="+rb?.rotation+" scale="+actor.transform.localScale+" move="+move?.vector+" follow="+move?.rotateTowardsMove;
 }
 static string PhoenixParticles(ParticleSystem ps)
 {
  var r=ps.GetComponent<ParticleSystemRenderer>();var main=ps.main;var shape=ps.shape;
  return ps.name+" local="+ps.transform.localEulerAngles+" world="+ps.transform.eulerAngles+" alignment="+r.alignment+" mode="+r.renderMode+" space="+main.simulationSpace+" rotation="+main.startRotation.constant+" rotation3D="+main.startRotation3D+" shape="+shape.shapeType+":"+shape.rotation+" particles="+ps.particleCount;
 }
 IEnumerator CheckPhoenix(bool inspect)
 {
  RunResume.Invalidate();Progression.State.earned.Add("character:Scarlett");Progression.Refresh();
  yield return AchievementRun("Scarlett","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;this.AddObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");
  var generator=EmitterGenerator("PF_FirewaveGenerator");while(WindField(generator,"OP")==null)yield return null;
  Log("fixture-precondition","Actual native Scarlett and third-shot phoenix producer, then explicit native SpawnProjectile calls at eight aim directions to isolate orientation. Native particle simulation advances0.08s for windowless GPU captures. Enemy scheduling stopped; no OS input or natural balance claim.");
  PhoenixObserveInput=true;yield return MothShots(game,3);PhoenixObserveInput=false;var actual=Object.FindObjectsOfType<Projectile>().Single(x=>x.name=="PF_FireWave");Log("phoenix-actual",PhoenixPose(actual.gameObject));
  foreach(var ps in actual.GetComponentsInChildren<ParticleSystem>())Log("phoenix-actual-particle",PhoenixParticles(ps));
  if(!inspect){Require(PhoenixActualSpawns==1,"actual Scarlett third shot reaches corrected spawn exactly once");yield return EmitterContact(actual,0,3);}
  foreach(var actor in Object.FindObjectsOfType<Projectile>())ReturnFixtureActor(actor.gameObject);
  var spawn=AccessTools.Method(typeof(ProjectileOnShoot),"SpawnProjectile");var pool=ObjectPooler.SharedInstance;
  bool immediateFacing=true;
  foreach(bool colorful in new[]{false,true})
  {
   if(ColorfulStyle.Instance.Enabled!=colorful)ColorfulStyle.Instance.Toggle();
   for(int i=0;i<8;i++)
   {
    float angle=i*45;var direction=(Vector2)(Quaternion.Euler(0,0,angle)*Vector3.right);
    var active=pool.pooledObjectsDictionary["PF_FireWave"].Where(x=>x.activeSelf).ToArray();
    spawn.Invoke(generator,new object[]{direction});var actor=pool.pooledObjectsDictionary["PF_FireWave"].Single(x=>x.activeSelf&&!active.Contains(x));actor.transform.position=p.transform.position+(Vector3)(direction*4);
    immediateFacing&=Mathf.Abs(Mathf.DeltaAngle(actor.transform.eulerAngles.z,angle))<.01f;
    Log("phoenix-spawn",PhoenixPose(actor));
    Require(actor.GetComponent<Projectile>().damage==0&&(float)WindField(actor.GetComponent<AndroidProjectile>(),"Size")==p.stats[StatType.ProjectileSize].Modify(1),"phoenix visible rotation preserves source zero Bullet damage and live size");
    actor.GetComponent<MoveComponent2D>().enabled=false;
   }
   yield return new WaitForFixedUpdate();
   foreach(var actor in pool.pooledObjectsDictionary["PF_FireWave"].Where(x=>x.activeSelf))
   {Log("phoenix-after-physics",PhoenixPose(actor));foreach(var ps in actor.GetComponentsInChildren<ParticleSystem>()){ps.Simulate(.08f,true,true);Log("phoenix-particle",PhoenixParticles(ps));}}
   Time.timeScale=0;yield return Capture("phoenix-eight-"+(colorful?"reborn":"original")+".png");Time.timeScale=1;
   foreach(var actor in pool.pooledObjectsDictionary["PF_FireWave"].Where(x=>x.activeSelf).ToArray())ReturnFixtureActor(actor);
  }
  foreach(var prefab in Resources.FindObjectsOfTypeAll<GameObject>().Where(x=>!x.scene.IsValid()&&(x.GetComponent<Projectile>()!=null||x.GetComponent<SeekEnemy>()!=null||x.GetComponent<MoveComponent2D>()?.rotateTowardsMove==true)).OrderBy(x=>x.name))
  {
   Log("directional-inventory",PhoenixPose(prefab));
   foreach(var ps in prefab.GetComponentsInChildren<ParticleSystem>(true))Log("directional-particle",prefab.name+"/"+PhoenixParticles(ps));
  }
  Require(immediateFacing,"visible phoenix rotation matches each requested angle before first emission, including reuse");
  if(!inspect)yield return CheckDirectionalOwners(game);
  SetPhase("PASS");Log("complete","Phoenix inspection and bounded loaded directional-template inventory");Application.Quit(0);
 }
 IEnumerator CheckDirectionalOwners(GameController game)
 {
  var pool=ObjectPooler.SharedInstance;var p=PlayerController.Instance;
  var prefabs=Resources.FindObjectsOfTypeAll<GameObject>().Where(x=>!x.scene.IsValid()&&x.GetComponent<Projectile>()!=null).GroupBy(x=>x.name).Select(x=>x.First()).OrderBy(x=>x.name).ToArray();
  foreach(var prefab in prefabs)
  {
   pool.AddObject(prefab.name,prefab,0);var actor=pool.GetPooledObject(prefab.name);actor.transform.position=p.transform.position+Vector3.up*6;var shot=actor.GetComponent<Projectile>();
   var children=actor.GetComponentsInChildren<Transform>(true);var local=children.Select(x=>x.localRotation).ToArray();
   for(int cycle=0;cycle<2;cycle++)
   {
    if(cycle!=0){ReturnFixtureActor(actor);var reused=pool.GetPooledObject(prefab.name);Require(reused==actor,"directional audit reuses same pooled actor: "+prefab.name);}
    foreach(float angle in new[]{0f,45f,90f,135f,180f,225f,270f,315f})
    {
     var vector=(Vector2)(Quaternion.Euler(0,0,angle)*Vector3.right)*7;shot.vector=vector;shot.angle=angle;
     Require(Mathf.Abs(Mathf.DeltaAngle(actor.transform.eulerAngles.z,angle))<.01f,"immediate visible angle "+angle+" cycle"+cycle+" "+prefab.name);
     Require(Mathf.Abs(Mathf.DeltaAngle(actor.GetComponent<Rigidbody2D>().rotation,angle))<.01f&&Vector2.Distance(shot.vector,vector)<.001f,"rotation preserves body angle and velocity: "+prefab.name);
     for(int i=1;i<children.Length;i++)Require(Quaternion.Angle(children[i].localRotation,local[i])<.01f,"rotation preserves authored child pose: "+prefab.name+"/"+children[i].name);
    }
   }
   ReturnFixtureActor(actor);Log("directional-audit-complete",prefab.name+" eight angles/two activations; transform/body/velocity/authored children");
  }
  // Seekers use MoveSystem's distinct rotation path; exercise all loaded owners
  // rather than applying the Projectile angle fix to their sprites/particles.
  var seekers=Resources.FindObjectsOfTypeAll<GameObject>().Where(x=>!x.scene.IsValid()&&x.GetComponent<MoveComponent2D>()?.rotateTowardsMove==true&&x.GetComponent<Projectile>()==null).GroupBy(x=>x.name).Select(x=>x.First()).ToArray();
  foreach(var prefab in seekers)
  {
   pool.AddObject(prefab.name,prefab,0);var actor=pool.GetPooledObject(prefab.name);actor.transform.position=p.transform.position+Vector3.up*6;var move=actor.GetComponent<MoveComponent2D>();
   foreach(var seek in actor.GetComponents<SeekEnemy>())seek.enabled=false;
   yield return null;
   foreach(var direction in new[]{Vector2.right,Vector2.up,Vector2.left,Vector2.down})
   {
    move.vector=direction*7;yield return new WaitForFixedUpdate();yield return null;
    Require(Mathf.Abs(Vector2.SignedAngle(actor.transform.right,direction))<.01f,"seeker movement owns facing: "+prefab.name+" "+direction);
   }
   ReturnFixtureActor(actor);Log("seeker-audit-complete",prefab.name);
  }
  foreach(var owner in Resources.FindObjectsOfTypeAll<GameObject>().Where(x=>!x.scene.IsValid()&&(x.GetComponent<ShootingSummon>()!=null||x.GetComponent<OrbitalWeapon>()!=null||x.GetComponent<StabbingSummon>()!=null)).OrderBy(x=>x.name))
   Log("separate-summon-owner",owner.name+" components="+string.Join(",",owner.GetComponents<Component>().Select(x=>x.GetType().Name)));
  Log("directional-audit-scope",prefabs.Length+" Projectile templates; "+seekers.Length+" independent movement-facing templates; separate summon owner inventory retained. No camera-facing particle alignment or authored animation/texture changed.");
  yield return AndroidPick(game,"FireStarter",game.playerXP.level);var fire=EmitterGenerator("PF_FireballGenerator");while(WindField(fire,"OP")==null)yield return null;
  PhoenixObserveInput=true;yield return MothShots(game,4);PhoenixObserveInput=false;var ball=Object.FindObjectsOfType<Projectile>().First(x=>x.name=="PF_Fireball");yield return EmitterContact(ball,(int)ball.damage);
  yield return AchievementExit(game,false);
  Progression.State.earned.Add("character:Yuki");Progression.Refresh();
  yield return AchievementRun("Yuki","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  var next=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var moth=PlayerController.Instance.GetComponentInChildren<ProjectileOnShoot>();while(WindField(moth,"OP")==null)yield return null;
  Require(Object.FindObjectsOfType<Projectile>().All(x=>x.name!="PF_FireWave"),"scene restart leaves no phoenix actor");
  PhoenixObserveInput=true;yield return MothShots(next,6);PhoenixObserveInput=false;var butterfly=Object.FindObjectsOfType<Projectile>().Single(x=>x.name=="PF_FrostMoth");var fx=butterfly.GetComponentInChildren<SpriteFX>();var renderer=fx.GetComponent<SpriteRenderer>();var before=renderer.sprite;
  while(renderer.sprite==before&&butterfly.gameObject.activeSelf)yield return null;
  Require(butterfly.gameObject.activeSelf&&renderer.sprite!=before,"native moth animation still advances after scene restart");
  yield return AchievementExit(next,false);
 }
}
[HarmonyPatch(typeof(ProjectileOnShoot),"SpawnProjectile")]
class PhoenixPassivePoseProbe
{
 static void Postfix(ProjectileOnShoot __instance,Vector2 direction)=>RuntimeChecks.PhoenixSpawned(__instance,direction);
}
