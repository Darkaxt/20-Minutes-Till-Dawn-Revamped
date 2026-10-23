using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
using flanne.PowerupSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool MothMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-moths-");
 internal static readonly List<Projectile> MothProduced=new List<Projectile>();
 internal static readonly Dictionary<int,float> MothStarts=new Dictionary<int,float>();
 internal static readonly Dictionary<int,float> MothBorn=new Dictionary<int,float>();
 internal static readonly Dictionary<int,List<float>> MothPulses=new Dictionary<int,List<float>>();
 internal static int MothTarget,MothContactActor,MothContactCount,MothContactDamage;
 internal static readonly List<int> MothDamage=new List<int>();
 static Projectile[] Moths()=>Object.FindObjectsOfType<PeriodicGameObjectActivator>().Where(a=>a.GetComponentInChildren<FreezeOrHarm>(true)!=null).Select(a=>a.GetComponent<Projectile>()).ToArray();
 static void ParkMoth(Projectile shot,Vector3 at)
 {shot.GetComponent<MoveComponent2D>().enabled=false;shot.GetComponent<Rigidbody2D>().velocity=Vector2.zero;shot.transform.position=at;}
 IEnumerator MothShots(GameController game,int count)
 {
  var p=PlayerController.Instance;while(!(p.CurrentState is flanne.Player.IdleState))yield return null;p.ammo.Reload();yield return null;
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  int actual=0;UnityEngine.Events.UnityAction observed=()=>actual++;p.gun.OnShoot.AddListener(observed);
  var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*5);
  try
  {
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(actual<count)yield return null;
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});while(!(p.CurrentState is flanne.Player.IdleState))yield return null;
   Require(actual==count,"actual native input produces exactly "+count+" gun OnShoot callbacks");
  }
  finally{p.gun.OnShoot.RemoveListener(observed);}
  ReturnPlayerShots();
 }
 void MothHurt(int value)=>MothDamage.Add(value);
 IEnumerator MothContact(Projectile shot,int expected,bool firstFreeze)
 {
  var p=PlayerController.Instance;var target=HitEffectTarget(maximum:100000,tag:"Untagged");target.transform.position=p.transform.position+Vector3.right*5;
  foreach(var other in Moths())ParkMoth(other,p.transform.position+Vector3.up*40);
  MothContactActor=shot.GetComponentInChildren<FreezeOrHarm>(true).gameObject.GetInstanceID();MothTarget=target.gameObject.GetInstanceID();MothContactCount=0;MothDamage.Clear();
  characterActorTarget=target;characterActorKinds.Clear();ObserveDamage(target,MothHurt);ParkMoth(shot,target.transform.position);Physics2D.SyncTransforms();
  while(MothContactCount==0&&shot.gameObject.activeSelf)yield return null;
  Require(MothContactCount>0&&FreezeSystem.SharedInstance.IsFrozen(target.gameObject),"actual periodic child collision freezes an untagged NPC-layer target");
  if(firstFreeze)Require(target.HP==100000&&MothContactDamage==0,"first moth contact freezes without damaging an unfrozen target");
  while(MothDamage.Count==0&&shot.gameObject.activeSelf)yield return null;
  Require(MothDamage.Count>0&&MothDamage[0]==expected&&characterActorKinds.Contains((DamageType)9),"later physical pulse deals source freeze-classified "+expected+" damage");
  Require(shot.damage==shot.GetComponentInChildren<FreezeOrHarm>(true).damageAmount,"projectile property and actual periodic consumer share one live integer damage field");
  UnobserveDamage(target,MothHurt);ReturnFixtureActor(target.gameObject);while(FreezeSystem.SharedInstance.IsFrozen(target.gameObject))yield return null;characterActorTarget=null;MothContactActor=MothTarget=0;ParkMoth(shot,p.transform.position+Vector3.up*40);
 }
 IEnumerator MothCadence(Projectile shot,float expected)
 {
  int id=shot.gameObject.GetInstanceID();if(!MothPulses.ContainsKey(id))MothPulses[id]=new List<float>();MothPulses[id].Clear();float largest=0;
  while(MothPulses[id].Count<2&&shot.gameObject.activeSelf){largest=Mathf.Max(largest,Time.deltaTime);yield return null;}
  Require(MothPulses[id].Count>=2,"actual source periodic owner emits two active pulses before moth expiry");
  float gap=MothPulses[id][1]-MothPulses[id][0];Log("moth-cadence","id="+id+" captured="+MothStarts[id]+" field="+shot.GetComponent<PeriodicGameObjectActivator>().timeBetweenActivations+" observed="+gap);
  Require(Mathf.Abs(gap-expected)<=largest*2+.03f,"actual repeated moth pulses retain first-Start cadence "+expected+" within measured frame quantization");
 }
 IEnumerator CheckAndroidMoths(bool continuation)
 {
  GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else{RunResume.Invalidate();Progression.State.earned.Add("character:Yuki");Progression.Refresh();yield return AchievementRun("Yuki","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();}
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));var generator=p.GetComponentInChildren<ProjectileOnShoot>();
  while(WindField(generator,"OP")==null)yield return null;
  Require(p.loadedCharacter.name=="Yuki"&&Moths().Length==0&&Convert.ToInt32(WindField(generator,"_counter"))==0,"native Yuki/source Continue has one pristine six-shot producer and no saved moth actors/counter");
  Require(Convert.ToInt32(WindField(generator,"shotsPerAttack"))==6,"original source moth trigger requires six actual shots");
  this.AddObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");
  Log("fixture-precondition","Real native gun input/exclusive chests, source-pooled untagged NPCs, actual periodic callbacks and physical child contacts. Enemy scheduling/game clock stopped and selected moth movement/positions isolated. First-Start cadence sampled rather than rescheduled. Lens modifier entry is explicitly invoked on a moth because its original root has no circle collider; this does not claim a natural moth/Lens collision. No synthetic gun/contact/status events or natural encounter claim.");
  Projectile old=null;
  if(!continuation)
  {
   MothProduced.Clear();yield return MothShots(game,5);Require(MothProduced.Count==0&&Convert.ToInt32(WindField(generator,"_counter"))==5,"first five native gun shots produce no moth");
   yield return MothShots(game,1);Require(MothProduced.Count==1&&Convert.ToInt32(WindField(generator,"_counter"))==0,"sixth actual native shot produces one moth and resets its trigger");old=MothProduced.Single();
   while(!MothStarts.ContainsKey(old.gameObject.GetInstanceID()))yield return null;
   CombatClose(MothStarts[old.gameObject.GetInstanceID()],1.2f,"fresh moth captures the generator's source1.2-second cadence");CombatClose(old.damage,20,"fresh moth captures current gun damage");
   CombatClose((float)WindField(old.GetComponent<TimeToLive>(),"lifetime"),6.5f,"source moth root lifetime");var child=old.GetComponentInChildren<FreezeOrHarm>(true);
   CombatClose(child.GetComponent<CircleCollider2D>().radius,2,"source moth child radius");CombatClose((float)WindField(child.GetComponent<TimeToLive>(),"lifetime"),.5f,"source moth child activation lifetime");
   Require(old.GetComponent<CircleCollider2D>()==null&&child.GetComponent<Rigidbody2D>().useFullKinematicContacts,"source moth has no root collision circle and has a full-contact periodic child");
   CombatClose(old.GetComponent<MoveComponent2D>().vector.magnitude,2,"actual producer uses original speed2 before fixture parking");yield return MothContact(old,20,true);
   float from=Time.time,largest=0;Time.timeScale=4;while(old.gameObject.activeSelf){largest=Mathf.Max(largest,Time.deltaTime);yield return null;}Time.timeScale=1;
   Log("moth-baseline-expiry","remaining observed="+(Time.time-from)+" largest-frame="+largest);Require(Time.time-MothBorn[old.gameObject.GetInstanceID()]>=6.49f&&Time.time-MothBorn[old.gameObject.GetInstanceID()]<=6.5f+largest*2+.03f,"actual source moth lifetime expires at6.5 within measured frame quantization");
   foreach(string key in new[]{"yuki_perk_pair_up_name","yuki_perk_butterfly_dance_name","yuki_perk_brittle_name"})yield return AndroidWhirlwindChest(game,key);
  }
  Require(generator.numProjectiles==2,"native Pair Up/source replay creates two moths per sixth shot");CombatClose(generator.periodicDamageFrequency,.6f,"native Butterfly Dance/source replay halves the spawn field");
  var brittle=p.GetComponentsInChildren<DamageBoostAgainstFrozen>();while(brittle.Any(b=>WindField(b,"FS")==null))yield return null;
  Require(brittle.Length==1,"Brittle/source replay owns exactly one started global frozen-damage observer");CombatClose((float)WindField(brittle.Single(),"damageBoost"),.3f,"Brittle source multiplier operand");
  MothProduced.Clear();yield return MothShots(game,6);Require(MothProduced.Count==2,"actual upgraded sixth shot produces the source pair");var pair=MothProduced.ToArray();
  while(pair.Any(a=>!MothStarts.ContainsKey(a.gameObject.GetInstanceID())))yield return null;
  foreach(var shot in pair){ParkMoth(shot,p.transform.position+Vector3.up*40);CombatClose(shot.GetComponent<PeriodicGameObjectActivator>().timeBetweenActivations,.6f,"each upgraded spawn writes the source interval field");}
  if(!continuation){Require(pair.Contains(old),"actual source pool reuses the previously expired moth");yield return MothCadence(old,1.2f);}
  var fresh=pair.First(a=>continuation||a!=old);CombatClose(MothStarts[fresh.gameObject.GetInstanceID()],.6f,"first-Start upgraded moth captures the halved-interval field");yield return MothCadence(fresh,.6f);yield return MothContact(fresh,26,true);
  foreach(var shot in pair)if(shot.gameObject.activeSelf)ReturnFixtureActor(shot.gameObject);
  if(!continuation)yield return AndroidPick(game,"MagicLens",game.playerXP.level);
  var lens=p.GetComponentsInChildren<BulletEnhanceSummon>().Single();while(!SummonStarted(lens))yield return null;MoveLensesAway();
  MothProduced.Clear();yield return MothShots(game,6);var modified=MothProduced.First();foreach(var shot in MothProduced)ParkMoth(shot,p.transform.position+Vector3.up*40);
  AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidLens"),"Apply").Invoke(null,new object[]{lens,modified});CombatClose(modified.damage,26,"explicit reconstructed Android Lens entry updates the moth's live child to26");yield return MothContact(modified,34,true);
  if(!continuation)
  {
   MothProduced.Clear();yield return MothShots(game,2);Require(Convert.ToInt32(WindField(generator,"_counter"))==2&&Moths().Length>0,"source checkpoint contains a partial trigger and transient moths");
   Require(ResumeRuntime.Capture(p).summons.Count==0,"source checkpoint does not serialize transient moths");yield return DragonSuspend(game,"Yuki native six-shot producer/three exclusives/source periodic physics/cached cadence/Lens child damage and partial-trigger checkpoint");yield break;
  }
  this.RemoveObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();
  Require(p.GetComponentsInChildren<ProjectileOnShoot>().Length==0&&p.GetComponentsInChildren<DamageBoostAgainstFrozen>().Length==0&&Moths().Length==0,"ordinary restart clears Yuki trigger/Brittle/actors");
  yield return MothShots(game,6);Require(Moths().Length==0,"six real native gun shots after restart produce no old moths");yield return CheckRevolver(game,p,"android-moths-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Yuki six-shot/three-exclusives/physical freeze-damage/first-Start cadence/controlled Lens entry/source Continue and native restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(ProjectileOnShoot),"SpawnProjectile")]
class ObserveActualMothProducer
{
 [HarmonyPriority(Priority.First)]static void Prefix(out HashSet<int> __state)=>__state=RuntimeChecks.MothMode?new HashSet<int>(Object.FindObjectsOfType<PeriodicGameObjectActivator>().Select(a=>a.gameObject.GetInstanceID())):null;
 static void Postfix(HashSet<int> __state){if(__state!=null)foreach(var a in Object.FindObjectsOfType<PeriodicGameObjectActivator>().Where(a=>!__state.Contains(a.gameObject.GetInstanceID())&&a.GetComponentInChildren<FreezeOrHarm>(true)!=null)){RuntimeChecks.MothProduced.Add(a.GetComponent<Projectile>());RuntimeChecks.MothBorn[a.gameObject.GetInstanceID()]=Time.time;}}
}
[HarmonyPatch(typeof(PeriodicGameObjectActivator),"Start")]
class ObserveActualMothSchedule
{static void Prefix(PeriodicGameObjectActivator __instance){if(RuntimeChecks.MothMode)RuntimeChecks.MothStarts[__instance.gameObject.GetInstanceID()]=__instance.timeBetweenActivations;}}
[HarmonyPatch(typeof(PeriodicGameObjectActivator),"ActivateObject")]
class ObserveActualMothPulse
{static void Prefix(PeriodicGameObjectActivator __instance){if(RuntimeChecks.MothMode&&__instance.gameObject.activeInHierarchy){int id=__instance.gameObject.GetInstanceID();if(!RuntimeChecks.MothPulses.ContainsKey(id))RuntimeChecks.MothPulses[id]=new List<float>();RuntimeChecks.MothPulses[id].Add(Time.time);}}}
[HarmonyPatch(typeof(FreezeOrHarm),"OnCollisionEnter2D")]
class ObserveActualMothContact
{
 [HarmonyPriority(Priority.First)]static void Prefix(FreezeOrHarm __instance,Collision2D __0,out int __state)=>__state=__instance.gameObject.GetInstanceID()==RuntimeChecks.MothContactActor&&__0.gameObject.GetInstanceID()==RuntimeChecks.MothTarget?__0.gameObject.GetComponent<Health>().HP:int.MinValue;
 static void Postfix(Collision2D __0,int __state){if(__state!=int.MinValue){RuntimeChecks.MothContactCount++;RuntimeChecks.MothContactDamage=__state-__0.gameObject.GetComponent<Health>().HP;}}
}
