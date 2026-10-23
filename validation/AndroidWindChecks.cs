using System;
using System.IO;
using System.Runtime.Serialization.Json;
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
using flanne.PerkSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 static object WindField(object owner,string name)=>AccessTools.Field(owner.GetType(),name).GetValue(owner);
 static GameObject[] ActiveGales()=>ObjectPooler.SharedInstance.pooledObjectsDictionary.Where(p=>p.Key=="PF_Gale"||p.Key=="Revamped_AndroidGale").SelectMany(p=>p.Value).Where(o=>o.activeSelf).ToArray();
 static flanne.PerkSystem.Action WindCast()
 {
  var effects=(Dictionary<string,PerkEffect>)AccessTools.Field(typeof(ResumeRuntime),"NativeEffects").GetValue(null);
  return effects.Where(p=>p.Key.StartsWith("aero_magic_name#")).Select(p=>(flanne.PerkSystem.Action)WindField(p.Value,"action")).Single(a=>a.GetType().Name=="AndroidGaleCast"||a.GetType().Name=="ThrowProjectileTowardsCursorAction");
 }
 GameObject CastGale()
 {
  var before=ActiveGales();WindCast().Activate(PlayerController.Instance.gameObject);
  return ActiveGales().Single(g=>!before.Contains(g));
 }
 IEnumerator CheckAndroidWind(bool continuation)
 {
  GameController game;PlayerController p;
  if(continuation)
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;
   Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))
   {
    var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));
    if(error!=null)throw new InvalidOperationException(error.text);yield return null;
   }
   p=PlayerController.Instance;
   RunResume.Snapshot expected;
   using(var saved=File.OpenRead(Path.Combine(Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR"),"saved-run.json")))expected=(RunResume.Snapshot)new DataContractJsonSerializer(typeof(RunResume.Snapshot)).ReadObject(saved);
   var wanted=expected.runtime.effects.Single(e=>e.id=="aero_magic_name#0@0");
   var effects=(Dictionary<string,PerkEffect>)AccessTools.Field(typeof(ResumeRuntime),"NativeEffects").GetValue(null);
   var trigger=(flanne.PerkSystem.Triggers.PeriodicTrigger)WindField(effects[wanted.id],"trigger");
   var deadlines=(Dictionary<flanne.PerkSystem.Triggers.PeriodicTrigger,float>)AccessTools.Field(typeof(ResumeRuntime),"PeriodicDeadlines").GetValue(null);
   Require(Mathf.Abs(deadlines[trigger]-Time.time-wanted.periodicRemaining)<.15f,"fresh-process Continue restores the actual wind producer's remaining deadline rather than restarting its interval");
  }
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  }
  Object.FindObjectOfType<HordeSpawner>().enabled=false;
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  var screen=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*3);
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=screen});
  while(Vector2.Distance(ShootingCursor.Instance.cursorPosition,screen)>.1f)yield return null;
  if(!continuation)yield return AndroidPick(game,"AeroMagic",game.playerXP.level);
  while(ActiveGales().Length==0)yield return null;
  var first=ActiveGales()[0];
  Require(Mathf.Abs((float)WindField(first.GetComponent<TimeToLive>(),"lifetime")-4.5f)<.001f,"source Gale lifetime is four point five seconds rather than native ten seconds");
  var wind=p.GetComponents<MonoBehaviour>().Single(c=>c.GetType().Name=="AndroidWind");
  var prototype=(GameObject)WindField(wind,"prototype");var original=(GameObject)WindField(wind,"original");
  Require(prototype.GetComponent<Projectile>()==null&&prototype.GetComponent<PersistentHarm>()==null&&original.GetComponent<Projectile>()!=null&&original.GetComponent<PersistentHarm>()!=null,"owned source Gale excludes Windows projectile/harm callbacks without mutating the original prefab");
  if(!continuation)
  {
   yield return AndroidPick(game,"Windborne",game.playerXP.level);yield return AndroidPick(game,"AeroMastery",game.playerXP.level);yield return AndroidPick(game,"EyeOfTheStorm",game.playerXP.level);
   Require(!(bool)WindField(first.GetComponents<MonoBehaviour>().Single(c=>c.GetType().Name=="AndroidWindActor"),"AreaEnabled"),"acquiring Eye does not retroactively enable the moving area on an existing Gale");
  }
  Require((int)AccessTools.Property(wind.GetType(),"Damage").GetValue(wind)==(continuation?82:46),"native wind acquisitions apply flat mastery and Eye bonuses before unique movement scaling, including Continue reconstruction");
  foreach(var active in ActiveGales())active.SetActive(false);
  UnityEngine.Random.State chosen=default;bool found=false;
  for(int seed=0;!found;seed++){UnityEngine.Random.InitState(seed);chosen=UnityEngine.Random.state;found=UnityEngine.Random.Range(-60,60)==59;}
  UnityEngine.Random.state=chosen;var gale=CastGale();var actor=gale.GetComponents<MonoBehaviour>().Single(c=>c.GetType().Name=="AndroidWindActor");
  var movement=gale.GetComponent<MoveComponent2D>();Vector2 aim=(Vector2)Camera.main.ScreenToWorldPoint(ShootingCursor.Instance.cursorPosition)-(Vector2)p.transform.position;
  Require(Vector2.Distance(gale.transform.position,p.transform.position)<.01f&&Mathf.Abs(movement.vector.magnitude-3)<.001f&&Mathf.Abs(Vector2.SignedAngle(aim,movement.vector)-59)<.01f,"actual acquired wind cast starts at the player with speed three and source integer spread reaching fifty nine degrees");
  var position=p.transform.position+Vector3.right*35;gale.transform.position=position;movement.vector=Vector2.zero;
  Plugin.SetField(actor,"ContactTimer",0f);Plugin.SetField(actor,"AreaTimer",0f);
  var pool=ObjectPooler.SharedInstance;var template=pool.itemDictionary.Values.First(i=>i.objectToPool.layer==(int)TagLayerUtil.Enemy&&i.objectToPool.GetComponent<Health>()!=null).objectToPool;
  pool.AddObject("Validation_Wind_Enemies",template,3);
  var target=pool.GetPooledObject("Validation_Wind_Enemies");var health=DurableEnemy(target,position,false);target.AddComponent<CircleCollider2D>().radius=.18f;target.SetActive(true);
  Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();while(health.HP==10000)yield return null;
  int captured=continuation?82:46;
  Require(10000-health.HP==(continuation?160:92),"real source Gale contact retains duplicate colliders and deals captured Wind damage twice at its half second tick, including signed Might on Continue");target.SetActive(false);
  if(!continuation)yield return AndroidPick(game,"Haste",game.playerXP.level);
  int live=continuation?82:54;
  Require((int)WindField(actor,"Damage")==captured&&(int)AccessTools.Property(wind.GetType(),"Damage").GetValue(wind)==live,"existing contact damage remains captured while source GaleSystem damage follows actual movement-stat changes");
  float remaining=2-(float)WindField(actor,"AreaTimer");movement.vector=Vector2.right*3;
  Vector3 start=gale.transform.position;var center=start+Vector3.right*(remaining*3);
  var nearObject=pool.GetPooledObject("Validation_Wind_Enemies");var near=DurableEnemy(nearObject,center+Vector3.up*2.5f,false);nearObject.SetActive(true);
  var outsideObject=pool.GetPooledObject("Validation_Wind_Enemies");var outside=DurableEnemy(outsideObject,center+Vector3.up*3.5f,false);outsideObject.GetComponent<CircleCollider2D>().radius=.7f;outsideObject.SetActive(true);
  while(near.HP==10000)yield return null;
  Require(10000-near.HP==(continuation?162:108)&&outside.HP==10000&&Vector2.Distance(near.transform.position,gale.transform.position)<3&&Vector2.Distance(outside.transform.position,gale.transform.position)>3&&Vector2.Distance(start,gale.transform.position)>1&&Vector2.Distance(p.transform.position,gale.transform.position)>30,"Eye ticks around the moving Gale, doubles live floored damage before Might and uses strict actor centers rather than collider overlap or player position");
  nearObject.SetActive(false);outsideObject.SetActive(false);
  float contactTimer=(float)WindField(actor,"ContactTimer"),areaTimer=(float)WindField(actor,"AreaTimer");gale.SetActive(false);var reused=CastGale();
  Require(reused==gale&&(float)WindField(actor,"ContactTimer")==contactTimer&&(float)WindField(actor,"AreaTimer")==areaTimer,"source Gale pool reuses the last returned actor without resetting either retained clock");
  if(!continuation)
  {
   yield return AndroidPick(game,"Windborne",game.playerXP.level);yield return AndroidPick(game,"AeroMastery",game.playerXP.level);
   Require((int)AccessTools.Property(wind.GetType(),"Damage").GetValue(wind)==82,"repeat Windborne adds movement without duplicating its scaling provider, and repeat Mastery adds fifteen before scaling");
   yield return NativeTomePick(game,false,"TomeOfMight");
  }
  foreach(var active in ActiveGales())active.SetActive(false);
  var expiring=CastGale();expiring.transform.position=position*2;expiring.GetComponent<MoveComponent2D>().vector=Vector2.zero;
  var probe=expiring.AddComponent<WindLifetimeProbe>();float spawned=Time.time;
  while(probe.ReturnedAt<0)yield return null;
  Require(Mathf.Abs(probe.ReturnedAt-spawned-4.5f)<.1f,"the actual Gale returns to its owned pool after four point five game seconds through native TimeToLive");
  yield return Capture(continuation?"android-wind-continue.png":"android-wind-save.png");
  if(!continuation)
  {
   PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
   while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
   File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Android wind native acquisition, duplicate contacts, moving Eye, repeat composition, pool state and checkpoint");Application.Quit(0);yield break;
  }
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  Require(!p.GetComponents<MonoBehaviour>().Any(c=>c.GetType().Name=="AndroidWind"),"normal restart removes the source wind owner and all acquired providers");
  yield return CheckRevolver(game,p,"android-wind-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Android wind fresh-process Continue and original restart");Application.Quit(0);
 }
}
public sealed class WindLifetimeProbe:MonoBehaviour
{
 public float ReturnedAt=-1;
 void OnDisable(){ReturnedAt=Time.time;}
}
