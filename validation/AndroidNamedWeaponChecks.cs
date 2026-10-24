using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool NamedFireEdgeRoll,NamedFireEdgeSuccess;
 internal static int NamedContactActor,NamedContactTarget,NamedContactCount,NamedContactDamage;
 static SeekerWeapon[] NamedSeekers(string id)=>Object.FindObjectsOfType<SeekerWeapon>().Where(s=>s.SummonTypeID==id).ToArray();
 static float CrowBurn()=> (float)AccessTools.Field(AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidCrowCollision"),"BurnMultiplier").GetValue(null);
 static void ParkSeeker(SeekerWeapon actor,Vector3 position)
 {
  actor.GetComponent<SeekEnemy>().enabled=false;actor.GetComponent<MoveComponent2D>().enabled=false;var body=actor.GetComponent<Rigidbody2D>();body.velocity=Vector2.zero;actor.transform.position=position;
 }
 void NamedTarget(int maximum=100000,string tag="Enemy")
 {
  weaponObserved=HitEffectTarget(maximum:maximum,tag:tag);weaponObserved.transform.position=PlayerController.Instance.transform.position+Vector3.right*4;
  characterActorTarget=weaponObserved;characterActorKinds.Clear();weaponHits.Clear();weaponEvents=weaponTyped=0;characterActorImpacts=0;ObserveDamage(weaponObserved,NamedObservedDamage);Physics2D.SyncTransforms();
 }
 void NamedObservedDamage(int value)=>weaponHits.Add(value);
 void NamedReturnTarget()
 {UnobserveDamage(weaponObserved,NamedObservedDamage);ReturnFixtureActor(weaponObserved.gameObject);characterActorTarget=null;}
 IEnumerator NamedSeekerContact(SeekerWeapon actor,int expected,bool automatic=false)
 {
  while(!SummonStarted(actor))yield return null;
  foreach(var other in Object.FindObjectsOfType<SeekerWeapon>())ParkSeeker(other,PlayerController.Instance.transform.position+Vector3.right*40);
  actor.transform.position=weaponObserved.transform.position+Vector3.left*(automatic?2:4);Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
  weaponEvents=weaponTyped=0;NamedContactCount=0;NamedContactActor=actor.gameObject.GetInstanceID();NamedContactTarget=weaponObserved.gameObject.GetInstanceID();Vector3 start=actor.transform.position;
  if(automatic){actor.GetComponent<SeekEnemy>().enabled=true;actor.GetComponent<MoveComponent2D>().enabled=true;}
  else {actor.transform.position=weaponObserved.transform.position;Physics2D.SyncTransforms();}
  while(NamedContactCount==0&&actor.gameObject.activeSelf)yield return null;
  Require(NamedContactCount==1&&NamedContactDamage==expected&&weaponEvents==1&&weaponPayload.SummonName==actor.SummonTypeID&&weaponPayload.OtherID==weaponObserved.gameObject.GetInstanceID(),"actual "+actor.SummonTypeID+" physical source damage/payload="+expected);
  if(automatic)Require(Vector3.Distance(start,actor.transform.position)>.1f,"actual native seeker movement reaches the source-pooled enemy without teleporting into contact");
  ParkSeeker(actor,PlayerController.Instance.transform.position+Vector3.right*40);Physics2D.SyncTransforms();NamedContactActor=NamedContactTarget=0;
 }
 IEnumerator NamedNativeShot(GameController game,bool burn,string tag="Untagged",bool forceSuccess=false)
 {
  var p=PlayerController.Instance;while(!(p.CurrentState is flanne.Player.IdleState))yield return null;
  p.ammo.Reload();yield return null;foreach(var seeker in Object.FindObjectsOfType<SeekerWeapon>())ParkSeeker(seeker,p.transform.position+Vector3.right*40);
  NamedTarget(tag:tag);int before=weaponObserved.HP;bool fired=false;UnityEngine.Events.UnityAction shot=()=>fired=true;p.gun.OnShoot.AddListener(shot);
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  var aim=(Vector2)Camera.main.WorldToScreenPoint(weaponObserved.transform.position);NamedFireEdgeRoll=true;NamedFireEdgeSuccess=burn||forceSuccess;
  try
  {
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(!fired)yield return null;InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});
   while(weaponObserved.HP==before)yield return null;
   Require(before-weaponObserved.HP==(int)p.gun.damage&&BurnSystem.SharedInstance.IsBurning(weaponObserved.gameObject)==burn,"real player shot preserves source Fire Edge roll and layer-only eligibility on "+tag);
   if(burn)CombatClose(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(weaponObserved.gameObject).Damage,6,"actual Fire Edge projectile callback burns for six");
   NamedReturnTarget();while(!(p.CurrentState is flanne.Player.IdleState))yield return null;
  }
  finally{NamedFireEdgeRoll=false;p.gun.OnShoot.RemoveListener(shot);}
  ReturnPlayerShots();foreach(var seeker in Object.FindObjectsOfType<SeekerWeapon>())ParkSeeker(seeker,p.transform.position+Vector3.right*40);
 }
 IEnumerator CheckAndroidNamedWeapons(bool continuation)
 {
  bool raven=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("raven-");string name=raven?"Raven":"Katana";GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else {RunResume.Invalidate();Progression.State.earned.Add("character:"+name);Progression.Refresh();yield return AchievementRun(name,"RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();}
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));yield return new WaitForEndOfFrame();yield return null;
  Require(p.loadedCharacter.name==name,"actual native character/source Continue selects "+name);
  this.AddObserver(WeaponTyped,"Health.TweakSummonDamage");this.AddObserver(WeaponCollision,"WeaponSummon.CollisionEvent");this.AddObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");
  Log("fixture-precondition","Actual native character/exclusive chests, real native reloads and gun input, source-pooled targets and physical seeker contacts. Baseline seeking uses native movement from a declared nearby initial position; later contacts are isolated through actor placement/movement disabling. Fire Edge rolls and Raven temporary flat Summon Damage10/bullet multiplier minus0.1 are controlled. No natural encounter, chance-frequency, legacy or delivered-package claim.");
  if(raven)yield return NamedRaven(game,continuation);else yield return NamedKatana(game,continuation);
  this.RemoveObserver(WeaponTyped,"Health.TweakSummonDamage");this.RemoveObserver(WeaponCollision,"WeaponSummon.CollisionEvent");this.RemoveObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");
 }
 IEnumerator NamedKatana(GameController game,bool continuation)
 {
  var p=PlayerController.Instance;var spawner=p.GetComponentInChildren<SpawnSummonOnReload>();while(spawner.GetComponent<AndroidReloadSwords>()==null)yield return null;var state=spawner.GetComponent<AndroidReloadSwords>();
  Log("sword-startup","continued="+continuation+" counter="+state.Counter+" amount="+state.Amount+" active="+NamedSeekers("MagicSword").Length);
  Require(NamedSeekers("MagicSword").Length==0&&state.Counter<=1,"source bootstrap/Continue omits live swords and the previously accumulated reload counter");
  if(!continuation)
  {
   Require(state.Amount==1,"fresh Katana source spawn amount starts at one");int before=state.Counter;p.ammo.UseAmmo(1);p.ammo.Reload();
   Require(state.Counter==before+1&&NamedSeekers("MagicSword").Length==(before==0?0:1),"actual first manual reload follows source post-increment/first-callback skip");
   if(before==0){p.ammo.UseAmmo(1);p.ammo.Reload();}
   var initial=NamedSeekers("MagicSword").Single();CombatClose(Vector3.Distance(initial.transform.position,p.transform.position),.5f,"native reload spawns the original sword half a unit from player");NamedTarget();yield return NamedSeekerContact(initial,22,true);NamedReturnTarget();
   foreach(string key in new[]{"katana_perk_fire_edge_name","katana_perk_sword_mastery_name"})yield return AndroidWhirlwindChest(game,key);
   var disabledObject=new GameObject("LedgerDisabledSwordObserver");var disabled=disabledObject.AddComponent<AndroidReloadSwords>();BugCall(disabled,"Listen");disabled.enabled=false;
   var destroyedObject=new GameObject("LedgerDestroyedSwordObserver");var destroyed=destroyedObject.AddComponent<AndroidReloadSwords>();BugCall(destroyed,"Listen");Object.Destroy(destroyedObject);yield return null;
   yield return AndroidWhirlwindChest(game,"katana_perk_thousand_blades_name");
   Require(disabled.Amount==2&&destroyed.Amount==1,"M15 A234 native Thousand Blades upgrades a disabled live helper once without retaining or mutating its destroyed peer");
   Object.Destroy(disabledObject);yield return null;
  }
  Require(state.Amount==2,"native Thousand Blades event/source replay reaches the existing Start-registered spawner");
  CombatClose(Bonus(p.stats[StatType.BulletDamage]),.4f,"actual Sword Mastery/source replay adds forty-percent bullet damage once");Require(p.GetComponent<AndroidWeaponRules>().Bullet.SetEquals(new[]{"MagicSword"}),"source Sword Mastery uses one named multiplicative membership");
  int live=NamedSeekers("MagicSword").Length,previous=state.Counter;p.ammo.UseAmmo(1);p.ammo.Reload();if(previous==0){p.ammo.UseAmmo(1);p.ammo.Reload();}
  Require(NamedSeekers("MagicSword").Length==live+2,"actual upgraded reload produces two source swords instead of attaching an old per-reload perk action");
  var blade=NamedSeekers("MagicSword").Last();NamedTarget(tag:"Untagged");NamedFireEdgeRoll=true;NamedFireEdgeSuccess=true;yield return NamedSeekerContact(blade,31);NamedFireEdgeRoll=false;
  Require(BurnSystem.SharedInstance.IsBurning(weaponObserved.gameObject),"actual MagicSword typed collision reaches source Fire Edge without an Enemy tag predicate");CombatClose(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(weaponObserved.gameObject).Damage,6,"source sword Fire Edge burn operand remains six");NamedReturnTarget();
  yield return NamedNativeShot(game,false);yield return NamedNativeShot(game,true);
  if(!continuation)
  {
   p.ammo.UseAmmo(1);p.ammo.Reload();Require(NamedSeekers("MagicSword").Length>=2&&state.Counter>1,"actual source checkpoint retains active swords and an advanced unsaved reload counter");
   Log("sword-source-checkpoint","counter="+state.Counter+" amount="+state.Amount+" active="+NamedSeekers("MagicSword").Length);yield return DragonSuspend(game,"Katana native reload/event/spawn/seek/Fire Edge/Sword Mastery physical consumers and active-sword source checkpoint");yield break;
  }
  yield return NamedWeaponRestart(game,"Katana");
 }
 IEnumerator NamedRaven(GameController game,bool continuation)
 {
  var p=PlayerController.Instance;var crows=NamedSeekers("Crow");while(crows.Any(c=>!SummonStarted(c)))yield return null;
  Require(crows.Length==(continuation?2:1)&&crows.All(c=>c.baseDamage==0),"actual Raven source crow count/base zero is reconstructed without saved field overrides");
  if(continuation)Require(CursedActors().Count==0&&((ICollection)WindField(AndroidBurnState.Get(BurnSystem.SharedInstance),"active")).Count==0,"source Continue omits world curse/burn status phases");
  if(!continuation)
  {
   CombatClose(CurseSystem.Instance.curseDamageMultiplier,2,"fresh source curse multiplier");CombatClose(CrowBurn(),0,"fresh Crow has no static Shadow Flame multiplier");
   NamedTarget();yield return NamedSeekerContact(crows[0],0,true);Require(CursedActors().Contains(weaponObserved)&&!BurnSystem.SharedInstance.IsBurning(weaponObserved.gameObject),"actual baseline Crow contact curses without invented immediate gun damage or burn");
   while(CursedActors().Contains(weaponObserved))yield return null;Require(weaponHits.Contains(40)&&characterActorKinds.Count(k=>k==DamageType.Curse)==1,"actual source baseline Crow curse deals forty after its native duration");NamedReturnTarget();
   foreach(string key in new[]{"raven_perk_flock_of_crows_name","raven_perk_shadow_affinity_name","raven_perk_shadow_flame_name"})yield return AndroidWhirlwindChest(game,key);
  }
  crows=NamedSeekers("Crow");while(crows.Any(c=>!SummonStarted(c)))yield return null;Require(crows.Length==2&&p.GetComponent<AndroidWeaponRules>().Bullet.SetEquals(new[]{"Crow"}),"Flock of Crows/source replay adds one actor and named bullet-factor membership");
  Log("raven-fire-rate","bonus="+Bonus(p.stats[StatType.FireRate])+" effective="+p.stats[StatType.FireRate].Modify(1));
  CombatClose(p.stats[StatType.FireRate].Modify(1),.9f,"Shadow Affinity stat0 uses the native/source negative-stat reduction channel for ten-percent Fire Rate loss");Require(p.playerHealth.maxHP==3,"Shadow Affinity does not invent a maximum-HP penalty");CombatClose(p.gun.damage,20,"Shadow Affinity does not invent a bullet-damage penalty");CombatClose(CurseSystem.Instance.curseDamageMultiplier,2.75f,"Shadow Affinity actual additive source curse factor");CombatClose(CrowBurn(),.35f,"Shadow Flame actual source static multiplier");
  NamedTarget();CurseSystem.Instance.Curse(weaponObserved.gameObject);Require(!BurnSystem.SharedInstance.IsBurning(weaponObserved.gameObject),"Shadow Flame stays scoped to actual Crow contact and does not retain the old generic OnCurse burn trigger");
  while(CursedActors().Contains(weaponObserved))yield return null;Require(weaponHits.Contains(55),"direct source curse deals55 without the Crow burn callback");NamedReturnTarget();
  p.stats[StatType.BulletDamage].AddMultiplierBonus(-.1f);p.stats[StatType.SummonDamage].AddFlatBonus(10);NamedTarget(tag:"Untagged");yield return NamedSeekerContact(crows[0],9);
  Require(CursedActors().Contains(weaponObserved)&&BurnSystem.SharedInstance.IsBurning(weaponObserved.gameObject),"actual upgraded Crow applies both statuses using NPC-layer eligibility on an untagged actor");CombatClose(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(weaponObserved.gameObject).Damage,17,"Crow uses float curse49.5 times0.35 then floors burn to17");
  yield return NamedSeekerContact(crows[1],9);CombatClose(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(weaponObserved.gameObject).Damage,34,"second actual Crow contact adds another17 burn even while Curse already exists");Require(CursedActors().Count(h=>h==weaponObserved)==1,"repeated contact does not register a second source curse");
  while(CursedActors().Contains(weaponObserved))yield return null;Require(weaponHits.Contains(50)&&characterActorKinds.Count(k=>k==DamageType.Curse)==1,"actual delayed curse uses signed floor of minus49.5 and deals50 once");p.stats[StatType.SummonDamage].AddFlatBonus(-10);p.stats[StatType.BulletDamage].AddMultiplierBonus(.1f);NamedReturnTarget();
  if(!continuation)
  {
   NamedTarget();yield return NamedSeekerContact(crows[0],0);Require(CursedActors().Contains(weaponObserved)&&BurnSystem.SharedInstance.IsBurning(weaponObserved.gameObject)&&p.stats[StatType.SummonDamage].FlatBonus==0,"source checkpoint has actual live statuses without retaining the temporary test stat operand");
   CombatClose(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(weaponObserved.gameObject).Damage,19,"unseeded source Crow at gun20/curse2.75 burns for floor55 times0.35 equals19");CombatClose(p.gun.damage,20,"source checkpoint restores the temporary bullet stat operand");
   yield return DragonSuspend(game,"Raven native crows/seek/contact/curse/Shadow Flame/Shadow Affinity source consumers and active-status checkpoint");yield break;
  }
  yield return NamedWeaponRestart(game,"Raven");
 }
 IEnumerator NamedWeaponRestart(GameController game,string character)
 {
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;StopFixtureSpawning();
  Require(Object.FindObjectsOfType<SeekerWeapon>().Length==0&&p.GetComponentInChildren<SpawnSummonOnReload>()==null&&p.GetComponent<AndroidWeaponRules>()==null,"ordinary restart clears "+character+" actor/spawner/rule ownership");CombatClose(CrowBurn(),0,"source exit/retry clears static Crow burn");CombatClose(CurseSystem.Instance.curseDamageMultiplier,2,"ordinary restart resets curse multiplier");CombatClose(p.gun.damage,20,"ordinary restart resets exclusive bullet stat changes");
  yield return NamedNativeShot(game,false,forceSuccess:true);Require(!BurnSystem.SharedInstance.IsBurning(weaponObserved.gameObject),"ordinary restart clears Fire Edge callbacks even with the controlled success seed");
  yield return CheckRevolver(game,p,"android-"+character.ToLowerInvariant()+"-weapons-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete",character+" native named-weapon/exclusive/physical/status consumers, source Continue and original restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(AndroidFireEdge),"Impact")]
class ControlNamedFireEdge
{
 [HarmonyPriority(Priority.First)] static void Prefix(){if(RuntimeChecks.NamedFireEdgeRoll)RuntimeChecks.SeedFirstRoll(RuntimeChecks.NamedFireEdgeSuccess?0:.5f,RuntimeChecks.NamedFireEdgeSuccess?.49f:1);}
}
[HarmonyPatch(typeof(WeaponSummon),"OnCollisionEnter2D")]
class ObserveNamedWeaponDirectDamage
{
 [HarmonyPriority(Priority.First)] static void Prefix(WeaponSummon __instance,Collision2D __0,out int __state)
 {__state=__instance.gameObject.GetInstanceID()==RuntimeChecks.NamedContactActor&&__0.gameObject.GetInstanceID()==RuntimeChecks.NamedContactTarget?__0.gameObject.GetComponent<Health>().HP:int.MinValue;}
 static void Postfix(Collision2D __0,int __state){if(__state!=int.MinValue){RuntimeChecks.NamedContactCount++;RuntimeChecks.NamedContactDamage=__state-__0.gameObject.GetComponent<Health>().HP;}}
}
