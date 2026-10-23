using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
using flanne.PowerupSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool EmitterMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-emitters-");
 internal sealed class Emission{internal Projectile Shot;internal string Name;internal Vector3 Position;internal Vector2 Direction;internal float At,Damage,RecipeDamage,Speed;}
 internal static readonly List<Emission> Emitted=new List<Emission>(),FanEmitted=new List<Emission>();
 internal static readonly List<GameObject> IceEmitted=new List<GameObject>();
 internal static readonly HashSet<int> EmitterStarted=new HashSet<int>();
 internal static SoundEffectSO EmitterIceSound;internal static int EmitterIceSounds,EmitterGunSounds;internal static bool EmitterPendingSave;
 static ProjectileOnShoot EmitterGenerator(string name)=>PlayerController.Instance.GetComponentsInChildren<ProjectileOnShoot>().Single(p=>p.name.StartsWith(name));
 internal static void EmitterPark(Projectile p)
 {p.GetComponent<MoveComponent2D>().enabled=false;p.GetComponent<Rigidbody2D>().velocity=Vector2.zero;p.transform.position=PlayerController.Instance.transform.position+Vector3.up*3;}
 static void EmitterClearActors()
 {
  foreach(var actor in Object.FindObjectsOfType<Projectile>().Where(p=>p.name.StartsWith("PF_FireWave")||p.name.StartsWith("PF_Fireball")).ToArray())ReturnFixtureActor(actor.gameObject);
  foreach(var actor in Object.FindObjectsOfType<FreezeOnCollision>().Where(p=>p.name.StartsWith("PF_Icicle")).ToArray())ReturnFixtureActor(actor.gameObject);
  ReturnPlayerShots();
 }
 static void EmitterResetRecords(){Emitted.Clear();FanEmitted.Clear();IceEmitted.Clear();EmitterIceSounds=EmitterGunSounds=0;}
 readonly List<int> emitterHits=new List<int>();
 void EmitterHurt(int value)=>emitterHits.Add(value);
 IEnumerator EmitterContact(Projectile shot,int damage,int burnTick=0)
 {
  var p=PlayerController.Instance;var target=HitEffectTarget(maximum:100000);target.transform.position=p.transform.position+Vector3.right*8;emitterHits.Clear();characterActorTarget=target;characterActorKinds.Clear();ObserveDamage(target,EmitterHurt);
  shot.transform.position=target.transform.position+Vector3.left*2;shot.GetComponent<MoveComponent2D>().enabled=true;shot.GetComponent<MoveComponent2D>().vector=Vector2.right*20;Physics2D.SyncTransforms();
  if(burnTick!=0)
  {
   while(!BurnSystem.SharedInstance.IsBurning(target.gameObject)&&shot.gameObject.activeSelf)yield return null;
   Require(BurnSystem.SharedInstance.IsBurning(target.gameObject)&&target.HP==100000&&characterActorKinds.Contains(DamageType.Bullet),"real FireWave contact preserves source zero Bullet damage and applies burn");
   CombatClose(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(target.gameObject).Damage,3,"FireWave actual contact contributes raw burn3");
   while(!emitterHits.Contains(burnTick))yield return null;Require(characterActorKinds.Contains(DamageType.Burn),"actual source burn tick is classified Burn and deals "+burnTick);
   AccessTools.Method(typeof(AndroidBurnStatus),"Release").Invoke(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(target.gameObject),null);
  }
  else
  {
   while(emitterHits.Count==0&&shot.gameObject.activeSelf)yield return null;
   Require(emitterHits.Count>0&&emitterHits[0]==damage&&characterActorKinds.Contains(DamageType.Bullet),"real emitter projectile deals source "+damage+" Bullet damage");
  }
  UnobserveDamage(target,EmitterHurt);ReturnFixtureActor(target.gameObject);characterActorTarget=null;if(shot.gameObject.activeSelf)ReturnFixtureActor(shot.gameObject);
 }
 IEnumerator EmitterIceContact(GameObject actor)
 {
  var p=PlayerController.Instance;foreach(var other in IceEmitted.Where(o=>o.activeSelf)){other.GetComponent<SeekEnemy>().enabled=false;other.GetComponent<MoveComponent2D>().enabled=false;other.GetComponent<Rigidbody2D>().velocity=Vector2.zero;other.transform.position=p.transform.position+Vector3.up*3;}
  var target=HitEffectTarget(maximum:100000);target.transform.position=p.transform.position+Vector3.right*5;Require(target.GetComponent<Rigidbody2D>()==null&&actor.GetComponent<Rigidbody2D>().useFullKinematicContacts,"source full-contact icicle reaches a genuinely bodyless original NPC");
  actor.transform.position=target.transform.position;Physics2D.SyncTransforms();while(!FreezeSystem.SharedInstance.IsFrozen(target.gameObject)&&actor.activeSelf)yield return null;
  Require(FreezeSystem.SharedInstance.IsFrozen(target.gameObject)&&!actor.activeSelf&&target.HP==100000,"actual icicle freezes without health damage and returns through its native collision event");
  float at=Time.time,largest=0;while(FreezeSystem.SharedInstance.IsFrozen(target.gameObject)){largest=Mathf.Max(largest,Time.deltaTime);yield return null;}
  Require(Time.time-at>=3-largest*2-.03f&&Time.time-at<=3+largest*2+.03f,"actual icicle freeze expires at source three seconds within measured frame quantization");ReturnFixtureActor(target.gameObject);
 }
 IEnumerator EmitterBurst(bool liveRecipe)
 {
  var p=PlayerController.Instance;EmitterClearActors();p.ammo.Reload();EmitterResetRecords();float began=Time.time,delay=p.gun.shotCooldown,largest=0;float before=p.gun.GetProjectileRecipe().damage;p.ammo.UseAmmo(p.ammo.amount);
  Require(IceEmitted.Count==3&&EmitterIceSounds==1,"one actual zero-ammo change creates three icicles and plays one imported source sound");
  while(FanEmitted.Count==0){largest=Mathf.Max(largest,Time.deltaTime);yield return null;}
  if(liveRecipe)p.stats[StatType.BulletDamage].AddFlatBonus(10);float after=p.gun.GetProjectileRecipe().damage;
  while(FanEmitted.Count<10){largest=Mathf.Max(largest,Time.deltaTime);yield return null;}
  Require(FanEmitted.Count==10&&EmitterGunSounds==10&&p.ammo.amount==0,"actual Fan Fire creates ten projectiles/sounds without consuming or refunding ammo");
  Require(FanEmitted[0].At-began>=delay-.02f&&FanEmitted[0].At-began<=delay+largest*3+.04f,"Fan Fire waits the source live gun cooldown before its first projectile");
  CombatClose(FanEmitted[0].RecipeDamage,before,"first actual Fan Fire shot reads the pre-change live recipe");
  for(int i=0;i<10;i++)
  {
   var r=FanEmitted[i];CombatClose(r.Damage,Mathf.Floor(r.RecipeDamage*.15f),"Fan Fire source15-percent integer damage for projectile"+i);CombatClose(Vector3.Distance(r.Position,p.transform.position),0,"Fan Fire uses the source helper position without native muzzle offset");
   if(i>0){CombatClose(Mathf.Abs(Vector2.SignedAngle(FanEmitted[i-1].Direction,r.Direction)),36,"Fan Fire uses source indexed36-degree ring step");Require(r.At-FanEmitted[i-1].At>=.05f-.002f,"Fan Fire preserves its source delayed sequential shots");if(liveRecipe)CombatClose(r.RecipeDamage,after,"later Fan Fire shot reads the changed live gun recipe");}
  }
  if(liveRecipe){Require(after!=before,"live-recipe probe changes an actual gun stat during the burst");p.stats[StatType.BulletDamage].AddFlatBonus(-10);}
  yield return EmitterContact(FanEmitted.Last().Shot,(int)FanEmitted.Last().Damage);yield return EmitterIceContact(IceEmitted.First());
  EmitterClearActors();p.ammo.Reload();EmitterResetRecords();p.ammo.UseAmmo(p.ammo.amount);p.ammo.UseAmmo(0);
  while(FanEmitted.Count<20)yield return null;Require(FanEmitted.Count==20&&IceEmitted.Count==6&&EmitterIceSounds==2&&EmitterGunSounds==20,"two actual zero-ammo notifications retain overlapping source fan bursts and six icicles without a deduplication gate");EmitterClearActors();p.ammo.Reload();
 }
 IEnumerator CheckAndroidEmitters(bool continuation)
 {
  GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else{RunResume.Invalidate();Progression.State.earned.Add("character:Scarlett");Progression.Refresh();yield return AchievementRun("Scarlett","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();}
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));ColdNoFreeze=true;this.AddObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");
  var wave=EmitterGenerator("PF_FirewaveGenerator");while(WindField(wave,"OP")==null)yield return null;
  Require(Convert.ToInt32(WindField(wave,"_counter"))==0&&Convert.ToInt32(WindField(wave,"shotsPerAttack"))==3,"native Scarlett/source Continue starts the original three-shot passive at zero counter");
  Log("fixture-precondition","Native Scarlett three exclusives and actual ordinary perk offers, real gun input/ammo callbacks, source-pooled bodyless enemies and actual source sampler/Unity collisions. Only selected actor positions/velocities are isolated. Pyro Mage/Frost Magic chance rolls fixed to fail unrelated effects. Fan Fire temporary Bullet Damage flat10 is removed before save. Source sound is verified through actual AudioSource/clip metadata while the windowless player remains muted. No natural encounter/chance-frequency/Android runtime or delivered-package claim.");
  if(!continuation)
  {
   EmitterResetRecords();yield return MothShots(game,2);Require(Emitted.Count==0,"two actual shots produce no Scarlett fire wave");yield return MothShots(game,1);Require(Emitted.Count==1&&Emitted[0].Name=="PF_FireWave","third actual shot produces one source fire wave");
   CombatClose(Emitted[0].Speed,5,"source FireWave producer speed");CombatClose(Vector3.Distance(Emitted[0].Position,p.transform.position),0,"source FireWave spawns at the player without a muzzle offset");yield return EmitterContact(Emitted[0].Shot,0,3);
   foreach(string key in new[]{"scarlett_perk_pyro_affinity_name","scarlett_perk_lasting_flame_name","scarlett_perk_pyromaniac_name"})yield return AndroidWhirlwindChest(game,key);
  }
  if(!continuation)CombatClose(p.stats[StatType.BulletDamage].Modify(1),.75f,"Pyro Affinity source bullet reduction before ordinary multishot acquisition");CombatClose(BurnSystem.SharedInstance.burnDamageMultiplier.Modify(1),1.67f,"Pyro Affinity source burn multiplier once");CombatClose(BurnSystem.SharedInstance.burnDurationMultiplier.Modify(5),10,"Lasting Flame source duration doubles to ten ticks");Require(p.GetComponentsInChildren<BuffPlayerStats>().Count(b=>b.name.StartsWith("BuffOnBurn"))==1,"Pyromaniac/source replay owns one burn listener");
  if(!continuation)foreach(string name in new[]{"PyroMage","FireStarter","DoubleShot","FanFire","FrostMage","IceShard"})yield return AndroidPick(game,name,game.playerXP.level);
  var fire=EmitterGenerator("PF_FireballGenerator");var fan=p.GetComponentsInChildren<AndroidFanFire>().Single();var ice=p.GetComponentsInChildren<AndroidIceShards>().Single();while(WindField(fire,"OP")==null||!EmitterStarted.Contains(fan.GetInstanceID())||!EmitterStarted.Contains(ice.GetInstanceID()))yield return null;EmitterIceSound=ice.Sound;
  Require(Convert.ToInt32(WindField(fire,"_counter"))==0&&Convert.ToInt32(WindField(fire,"shotsPerAttack"))==4,"actual Fire Starter/source Continue reconstructs one four-shot listener with no saved partial counter");Require(Emitted.Count==0||!continuation,"source Continue omits transient waves/fireballs and pending emitter actions");
  var clip=((AudioClip[])WindField(ice.Sound,"clips")).Single();Require(clip.name=="Spell_Ice_Small_Whoosh_01"&&clip.channels==2&&clip.frequency==44100&&clip.samples==44100,"actual icicle attachment loads its owned source stereo44100Hz one-second clip");
  EmitterResetRecords();yield return MothShots(game,3);Require(Emitted.Count(e=>e.Name=="PF_FireWave")==1&&Emitted.All(e=>e.Name!="PF_Fireball"),"three real shots trigger Scarlett while Fire Starter still waits for its fourth");
  yield return EmitterContact(Emitted.Single().Shot,0,5);Require(CombatCount(CharacterBuff("BuffOnBurn"))==1,"actual fire-wave burn increments Pyromaniac once");CombatClose(Bonus(p.stats[StatType.FireRate]),.1f,"actual source Pyromaniac burn grants ten-percent Fire Rate");
  EmitterResetRecords();yield return MothShots(game,1);var ball=Emitted.Single(e=>e.Name=="PF_Fireball");int expected=Mathf.FloorToInt(p.stats[StatType.BulletDamage].Modify(40));CombatClose(ball.Damage,expected,"actual Fire Starter transforms source base40 through Bullet Damage");CombatClose(ball.Speed,5,"actual Fire Starter source speed5");CombatClose(Vector3.Distance(ball.Position,p.transform.position),0,"actual Fire Starter has no native muzzle offset");CombatClose((float)WindField(ball.Shot.GetComponent<AndroidProjectile>(),"Radius"),.2f,"actual Fire Starter owns the source0.2 sampler radius");yield return EmitterContact(ball.Shot,expected);
  EmitterClearActors();EmitterResetRecords();yield return MothShots(game,p.ammo.max);while(FanEmitted.Count<10)yield return null;Require(FanEmitted.Count==10&&IceEmitted.Count==3,"real final gun shot reaches both source last-ammo listeners exactly once");EmitterClearActors();
  yield return EmitterBurst(true);
  if(!continuation)
  {
   yield return MothShots(game,1);Require(Convert.ToInt32(WindField(wave,"_counter"))>0&&Convert.ToInt32(WindField(fire,"_counter"))>0,"source checkpoint prepares both real partial shot counters");EmitterClearActors();EmitterResetRecords();p.ammo.UseAmmo(p.ammo.amount);while(FanEmitted.Count==0)yield return null;Require(FanEmitted.Count<10,"native suspension begins during an unfinished fan coroutine");EmitterPendingSave=true;yield return DragonSuspend(game,"Scarlett three-exclusive/fire-wave and Fire Starter/Fan Fire/Ice Shards full acquisition/physical/audio/overlap/live-recipe source checkpoint");yield break;
  }
  this.RemoveObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");ColdNoFreeze=false;yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();EmitterResetRecords();
  Require(p.GetComponentsInChildren<ProjectileOnShoot>().Length==0&&p.GetComponentsInChildren<AndroidFanFire>().Length==0&&p.GetComponentsInChildren<AndroidIceShards>().Length==0&&p.GetComponentsInChildren<BuffPlayerStats>().Length==0,"ordinary restart clears Scarlett/three emitter/Pyromaniac owners");CombatClose(BurnSystem.SharedInstance.burnDamageMultiplier.Modify(1),1,"ordinary restart clears Pyro Affinity");CombatClose(BurnSystem.SharedInstance.burnDurationMultiplier.Modify(5),5,"ordinary restart clears Lasting Flame");yield return MothShots(game,6);Require(Emitted.Count==0&&FanEmitted.Count==0&&IceEmitted.Count==0,"six actual shots and zero ammo after restart produce no retained source emitters");yield return CheckRevolver(game,p,"android-emitters-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Scarlett and three shared emitters native acquisition/real triggers/source physics/audio/overlapping bursts/live recipe/current Continue and ordinary restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(ProjectileOnShoot),"SpawnProjectile")]
class ObserveActualEmitterProducer
{
 [HarmonyPriority(Priority.First)]static void Prefix(out HashSet<int> __state)=>__state=RuntimeChecks.EmitterMode?new HashSet<int>(Object.FindObjectsOfType<Projectile>().Select(a=>a.GetInstanceID())):null;
 static void Postfix(HashSet<int> __state){if(__state==null)return;foreach(var p in Object.FindObjectsOfType<Projectile>().Where(a=>!__state.Contains(a.GetInstanceID())&&(a.name.StartsWith("PF_FireWave")||a.name.StartsWith("PF_Fireball")))){RuntimeChecks.Emitted.Add(new RuntimeChecks.Emission{Shot=p,Name=p.name.Replace("(Clone)",""),Position=p.transform.position,Direction=p.GetComponent<MoveComponent2D>().vector,Speed=p.GetComponent<MoveComponent2D>().vector.magnitude,At=Time.time,Damage=p.damage});RuntimeChecks.EmitterPark(p);}}
}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class ObserveActualFanProjectile
{
 static void Postfix(ProjectileRecipe __0,Vector2 __1,Vector3 __2,float __3,Projectile __result){if(!RuntimeChecks.EmitterMode||__3!=.15f)return;RuntimeChecks.FanEmitted.Add(new RuntimeChecks.Emission{Shot=__result,Name=__result.name,Position=__2,Direction=__1,At=Time.time,Damage=__result.damage,RecipeDamage=__0.damage});RuntimeChecks.EmitterPark(__result);}
}
[HarmonyPatch(typeof(AndroidIceShards),"Changed")]
class ObserveActualIcicleProducer
{
 [HarmonyPriority(Priority.First)]static void Prefix(out HashSet<int> __state)=>__state=RuntimeChecks.EmitterMode?new HashSet<int>(Object.FindObjectsOfType<FreezeOnCollision>().Select(a=>a.gameObject.GetInstanceID())):null;
 static void Postfix(HashSet<int> __state){if(__state==null)return;foreach(var c in Object.FindObjectsOfType<FreezeOnCollision>().Where(c=>c.name.StartsWith("PF_Icicle")&&!__state.Contains(c.gameObject.GetInstanceID())))RuntimeChecks.IceEmitted.Add(c.gameObject);}
}
[HarmonyPatch]
class ObserveActualEmitterStart
{
 static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(AndroidFanFire),"Start"),AccessTools.Method(typeof(AndroidIceShards),"Start")};
 static void Postfix(MonoBehaviour __instance){if(RuntimeChecks.EmitterMode)RuntimeChecks.EmitterStarted.Add(__instance.GetInstanceID());}
}
[HarmonyPatch(typeof(SoundEffectSO),nameof(SoundEffectSO.Play))]
class ObserveActualEmitterSound
{
 static void Postfix(SoundEffectSO __instance,AudioSource __result){if(!RuntimeChecks.EmitterMode)return;if(__instance==RuntimeChecks.EmitterIceSound){if(__result==null||__result.clip.name!="Spell_Ice_Small_Whoosh_01"||__result.pitch<1||__result.pitch>1.26f)throw new InvalidOperationException("Icicle source playback did not return the expected audio source/clip/pitch");RuntimeChecks.EmitterIceSounds++;}if(PlayerController.Instance!=null&&PlayerController.Instance.gun!=null&&PlayerController.Instance.gun.gunData!=null&&__instance==PlayerController.Instance.gun.gunData.gunshotSFX)RuntimeChecks.EmitterGunSounds++;}
}
[HarmonyPatch(typeof(RunResume),nameof(RunResume.Save))]
class ObserveEmitterPendingSave
{static void Postfix(){if(RuntimeChecks.EmitterPendingSave&&PlayerController.Instance!=null)RuntimeChecks.Log("emitter-save-phase","fan="+RuntimeChecks.FanEmitted.Count+" icicles="+RuntimeChecks.IceEmitted.Count);}}
