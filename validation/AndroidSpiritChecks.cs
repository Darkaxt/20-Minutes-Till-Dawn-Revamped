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
 internal static bool SpiritMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-spirits-");
 internal static readonly List<GameObject> SpiritProduced=new List<GameObject>();
 internal static readonly List<Vector3> SpiritBirthPositions=new List<Vector3>();
 internal static int SpiritContactActor,SpiritContactTarget,SpiritContactCount,SpiritContactDamage;internal static bool SpiritContactActive;
 static ProjectileSummon[] Spirits()=>Object.FindObjectsOfType<ProjectileSummon>().Where(s=>s.SummonTypeID=="Spirit").ToArray();
 static bool SpiritDeathListener(object owner)
 {
  var table=(Dictionary<string,Dictionary<object,List<Action<object,object>>>>)WindField(NotificationCenter.instance,"_table");
  return table.TryGetValue(Health.DeathEvent,out var listeners)&&listeners.Values.SelectMany(x=>x).Any(c=>ReferenceEquals(c.Target,owner));
 }
 static bool SpiritProducersReady()=>PlayerController.Instance.GetComponentsInChildren<SummonOnEnemyDeath>().All(SpiritDeathListener);
 static void ParkSpirit(ProjectileSummon spirit,Vector3 position)
 {spirit.GetComponent<SeekEnemy>().enabled=false;spirit.GetComponent<MoveComponent2D>().enabled=false;spirit.GetComponent<Rigidbody2D>().velocity=Vector2.zero;spirit.transform.position=position;}
 void SpiritDeath(int expected)
 {
  var dead=HitEffectTarget(maximum:10,tag:"Untagged");var at=PlayerController.Instance.transform.position+Vector3.right*4;dead.transform.position=at;SpiritProduced.Clear();SpiritBirthPositions.Clear();dead.TakeDamage(DamageType.Bullet,10);
  Require(dead.isDead&&SpiritProduced.Count==expected,"actual untagged Health death activates exactly "+expected+" native Spirit producers");
  Require(SpiritBirthPositions.All(p=>Vector3.Distance(p,at)<.001f),"actual source producer places each spirit at the dying Health position before pool release");
 }
 IEnumerator SpiritPhysical(ProjectileSummon spirit,int expected,bool automatic,Health prepared=null)
 {
  while(!SummonStarted(spirit))yield return null;
  foreach(var other in Spirits())if(other!=spirit)ParkSpirit(other,PlayerController.Instance.transform.position+Vector3.right*40);
  characterActorTarget=prepared??HitEffectTarget(maximum:100000);if(prepared==null)characterActorTarget.transform.position=spirit.transform.position+Vector3.right*(automatic?3:0);Physics2D.SyncTransforms();characterActorKinds.Clear();characterActorImpacts=0;
  SpiritContactActor=spirit.gameObject.GetInstanceID();SpiritContactTarget=characterActorTarget.gameObject.GetInstanceID();SpiritContactCount=0;var start=spirit.transform.position;
  if(automatic){spirit.GetComponent<SeekEnemy>().enabled=true;spirit.GetComponent<MoveComponent2D>().enabled=true;}
  else {ParkSpirit(spirit,characterActorTarget.transform.position+Vector3.left*4);Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;spirit.transform.position=characterActorTarget.transform.position;Physics2D.SyncTransforms();}
  while(SpiritContactCount==0&&spirit.gameObject.activeSelf)yield return null;
  Require(SpiritContactCount==1&&SpiritContactDamage==expected&&!SpiritContactActive&&!spirit.gameObject.activeSelf,"actual Spirit contact deals source signed "+expected+" and returns its actor immediately");
  Require(characterActorKinds.SequenceEqual(new[]{DamageType.Summon})&&characterActorImpacts==0,"actual Spirit keeps Summon classification without a projectile impact publisher");
  if(automatic)Require(Vector3.Distance(start,spirit.transform.position)>.1f,"actual Spirit seek/movement reaches its source-pooled enemy");
  ReturnFixtureActor(characterActorTarget.gameObject);characterActorTarget=null;SpiritContactActor=SpiritContactTarget=0;
 }
 IEnumerator SpiritLayerGate(ProjectileSummon spirit)
 {
  while(!SummonStarted(spirit))yield return null;ParkSpirit(spirit,PlayerController.Instance.transform.position+Vector3.right*35);
  var probe=new GameObject("Spirit_Physical_Layer_Probe");probe.layer=(int)TagLayerUtil.PlayerProjectileMod;probe.transform.position=PlayerController.Instance.transform.position+Vector3.right*4;probe.AddComponent<CircleCollider2D>().radius=.5f;
  Require(!Physics2D.GetIgnoreLayerCollision(spirit.gameObject.layer,probe.layer),"native source-equivalent modifier layer physically reaches the Spirit callback");
  SpiritContactActor=spirit.gameObject.GetInstanceID();SpiritContactTarget=probe.GetInstanceID();SpiritContactCount=0;spirit.transform.position=probe.transform.position;Physics2D.SyncTransforms();
  while(SpiritContactCount==0&&spirit.gameObject.activeSelf)yield return null;
  Require(SpiritContactCount==1&&SpiritContactActive&&spirit.gameObject.activeSelf,"actual Spirit ignores the modifier-layer collision without returning");
  spirit.transform.position=probe.transform.position+Vector3.left*4;Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
  probe.layer=(int)TagLayerUtil.Enemy;SpiritContactCount=0;spirit.transform.position=probe.transform.position;Physics2D.SyncTransforms();
  while(SpiritContactCount==0&&spirit.gameObject.activeSelf)yield return null;
  Require(SpiritContactCount==1&&!SpiritContactActive&&!spirit.gameObject.activeSelf&&SpiritContactDamage==0,"actual non-Health enemy-layer collision still returns the Spirit without inventing damage eligibility");
  Object.Destroy(probe);SpiritContactActor=SpiritContactTarget=0;
 }
 IEnumerator CheckAndroidSpirits(bool continuation)
 {
  GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else {RunResume.Invalidate();Progression.State.earned.Add("character:Lilith");Progression.Refresh();yield return AchievementRun("Lilith","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();}
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));while(!SpiritProducersReady())yield return null;
  Require(p.loadedCharacter.name=="Lilith"&&p.GetComponentsInChildren<SummonOnEnemyDeath>().Length==(continuation?2:1)&&Spirits().Length==0,"actual Lilith/source Continue has its original producer count and no saved transient spirits");
  this.AddObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");this.AddObserver(CharacterActorImpact,Projectile.ImpactEvent);
  Log("fixture-precondition","Actual native Lilith/exclusive chests, source-pooled untagged Health deaths, actual producer observers, real seek/contact/lifetime/pool reuse and native Save/Continue/restart. AI/target positions and selected actor movement are isolated. A plain physical collider tests the Spirit's modifier-layer/non-Health gates. Encounter/game clock stopped; no synthetic death/contact events or natural encounter claim.");
  if(!continuation)
  {
   var victim=HitEffectTarget(maximum:100000);victim.transform.position=p.transform.position+Vector3.right*7;
   SpiritDeath(1);var initial=SpiritProduced.Single().GetComponent<ProjectileSummon>();Require(initial.GetComponent<Rigidbody2D>().useFullKinematicContacts,"original Spirit full-contact body reaches bodyless source NPCs");CombatClose((float)WindField(initial.GetComponent<TimeToLive>(),"lifetime"),5,"Spirit owns the source five-second lifetime missing from the native prefab");yield return SpiritPhysical(initial,8,true,victim);
   SpiritDeath(1);var held=SpiritProduced.Single().GetComponent<ProjectileSummon>();while(!SummonStarted(held))yield return null;ParkSpirit(held,p.transform.position+Vector3.right*40);
   foreach(string key in new[]{"lilith_perk_necromancer_name","lilith_perk_undead_army_name"})yield return AndroidWhirlwindChest(game,key);
   Require(held.gameObject.activeSelf,"the existing source spirit remains alive across the native upgrade acquisitions");yield return SpiritPhysical(held,13,false);
  }
  else
  {
   Require(CombatCount(CharacterBuff("PF_BuffOnDeath"))==1,"source Continue replays the saved Necrophile count once");CombatClose(Bonus(p.stats[StatType.MoveSpeed]),.35f,"source Continue retains the count-derived Necrophile bonus without its old phase");
   var victim=HitEffectTarget(maximum:100000);victim.transform.position=p.transform.position+Vector3.right*7;
   SpiritDeath(2);var actors=SpiritProduced.Select(a=>a.GetComponent<ProjectileSummon>()).ToArray();while(actors.Any(a=>!SummonStarted(a)))yield return null;yield return SpiritPhysical(actors[0],13,true,victim);ReturnFixtureActor(actors[1].gameObject);
  }
  while(!SpiritProducersReady())yield return null;Require(p.GetComponentsInChildren<SummonOnEnemyDeath>().Length==2,"Necromancer actual acquisition/source replay adds one independent death producer");
  CombatClose(Bonus(p.stats[StatType.SummonDamage]),.55f,"Necromancer20-percent plus Undead Army35-percent summon damage applies once");CombatClose(Bonus(p.stats[StatType.SummonAttackSpeed]),.35f,"Undead Army applies its source35-percent attack-speed stat");
  SpiritDeath(2);float created=Time.time;var pair=SpiritProduced.Select(a=>a.GetComponent<ProjectileSummon>()).ToArray();while(pair.Any(a=>!SummonStarted(a)))yield return null;foreach(var actor in pair)ParkSpirit(actor,p.transform.position+Vector3.right*40);
  yield return SpiritLayerGate(pair[0]);float largest=0;Time.timeScale=4;while(pair[1].gameObject.activeSelf){largest=Mathf.Max(largest,Time.deltaTime);yield return null;}Time.timeScale=1;
  Require(Time.time-created>=4.99f&&Time.time-created<=5+largest*2+.02f,"actual unused Spirit returns after five Unity seconds within measured frame quantization");
  var last=pair[1].gameObject;foreach(var actor in pair){actor.GetComponent<SeekEnemy>().enabled=true;actor.GetComponent<MoveComponent2D>().enabled=true;}
  SpiritDeath(2);Require(SpiritProduced[0]==last,"actual later death reacquires the last-returned source Spirit");
  var reused=SpiritProduced.Select(a=>a.GetComponent<ProjectileSummon>()).ToArray();while(reused.Any(a=>!SummonStarted(a)))yield return null;foreach(var actor in reused){Require(actor.transform.IsChildOf(ObjectPooler.SharedInstance.transform)&&!actor.transform.IsChildOf(p.transform),"source Spirit reacquisition retains its pool container without a per-death player reparent");ReturnFixtureActor(actor.gameObject);}
  if(!continuation)
  {
   yield return AndroidWhirlwindChest(game,"lilith_perk_necrophile_name");yield return null;
   SpiritDeath(2);Require(CombatCount(CharacterBuff("PF_BuffOnDeath"))==1,"actual final Health death records one mixed-build Necrophile application");Require(ResumeRuntime.Capture(p).providers.Count==0&&ResumeRuntime.Capture(p).summons.Count==0,"source checkpoint omits generic provider and transient Spirit actor snapshots");
   yield return DragonSuspend(game,"Lilith native three-exclusives, death/spirit source physics/lifetime/reuse and active-Spirit mixed Necrophile checkpoint");yield break;
  }
  this.RemoveObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");this.RemoveObserver(CharacterActorImpact,Projectile.ImpactEvent);
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();
  Require(p.GetComponentsInChildren<SummonOnEnemyDeath>().Length==0&&Spirits().Length==0&&p.GetComponentsInChildren<BuffPlayerStats>().Length==0,"ordinary restart clears Lilith death/spirit/Necrophile owners");CombatClose(Bonus(p.stats[StatType.SummonDamage]),0,"ordinary restart clears Lilith summon damage");CombatClose(Bonus(p.stats[StatType.SummonAttackSpeed]),0,"ordinary restart clears Undead Army attack speed");
  var ordinary=HitEffectTarget(maximum:10);ordinary.TakeDamage(DamageType.Bullet,10);Require(Spirits().Length==0,"real Health death after ordinary restart has no old Spirit producer");
  yield return CheckRevolver(game,p,"android-spirits-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Lilith native death producers, source Spirit seek/damage/gates/TTL/reuse, mixed exclusive source Continue and ordinary restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(SummonOnEnemyDeath),"OnDeath")]
class ObserveActualSpiritProducer
{
 [HarmonyPriority(Priority.First)] static void Prefix(out HashSet<int> __state)=>__state=RuntimeChecks.SpiritMode?new HashSet<int>(Object.FindObjectsOfType<ProjectileSummon>().Select(s=>s.GetInstanceID())):null;
 static void Postfix(HashSet<int> __state)
 {if(__state!=null)foreach(var actor in Object.FindObjectsOfType<ProjectileSummon>().Where(s=>s.SummonTypeID=="Spirit"&&!__state.Contains(s.GetInstanceID()))){RuntimeChecks.SpiritProduced.Add(actor.gameObject);RuntimeChecks.SpiritBirthPositions.Add(actor.transform.position);}}
}
[HarmonyPatch(typeof(ProjectileSummon),"OnCollisionEnter2D")]
class ObserveActualSpiritContact
{
 [HarmonyPriority(Priority.First)] static void Prefix(ProjectileSummon __instance,Collision2D __0,out int __state)
 {__state=__instance.gameObject.GetInstanceID()==RuntimeChecks.SpiritContactActor&&__0.gameObject.GetInstanceID()==RuntimeChecks.SpiritContactTarget?(__0.gameObject.GetComponent<Health>()?.HP??0):int.MinValue;}
 static void Postfix(ProjectileSummon __instance,Collision2D __0,int __state)
 {if(__state!=int.MinValue){RuntimeChecks.SpiritContactCount++;RuntimeChecks.SpiritContactDamage=__state-(__0.gameObject.GetComponent<Health>()?.HP??0);RuntimeChecks.SpiritContactActive=__instance.gameObject.activeSelf;}}
}
