using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.UI;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool ArrowMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-arrows-");
 internal sealed class ArrowDrop{internal GameObject Actor,Ground;internal Vector3 Position;internal bool Initialized,Secondary;}
 internal sealed class ArrowReload{internal ArrowRetrieveOnReload Owner;internal bool Null;internal int Active,Sound;internal readonly List<GameObject> Actors=new List<GameObject>();internal readonly List<Vector3> Positions=new List<Vector3>();}
 internal static readonly List<ArrowDrop> ArrowDrops=new List<ArrowDrop>();
 internal static readonly List<ArrowReload> ArrowReloads=new List<ArrowReload>();
 internal static readonly List<Projectile> ArrowPrimaries=new List<Projectile>();
 internal static ArrowDrop ArrowDropping;internal static ArrowReload ArrowReloading;
 internal static bool ArrowTrackDrops,ArrowCapturePrimary;
 internal static Health ArrowTarget;int arrowPreviousHP;readonly List<Vector2> arrowDamage=new List<Vector2>();
 static List<ArrowRetrievalPoint> ArrowPoints=>(List<ArrowRetrievalPoint>)AccessTools.Field(typeof(ArrowRetrieveOnReload),"retrievalPoints").GetValue(null);
 static ArrowRetrieveOnReload ArrowOwner()=>PlayerController.Instance.GetComponentInChildren<ArrowRetrieveOnReload>();
 static ArrowRetrievalPoint[] ArrowActivePoints()=>ArrowPoints==null?new ArrowRetrievalPoint[0]:ArrowPoints.Where(p=>p!=null&&p.gameObject.activeSelf).ToArray();
 static GameObject ArrowGroundPrefab()=>(GameObject)WindField(PlayerController.Instance.gun.gunData.bullet.GetComponent<SpawnOnDisableProjectile>(),"prefab");
 void ArrowHurt(object sender,object args){if(!ReferenceEquals(sender,ArrowTarget))return;arrowDamage.Add(new Vector2((int)(DamageType)args,arrowPreviousHP-ArrowTarget.HP));arrowPreviousHP=ArrowTarget.HP;}
 IEnumerator ArrowEvolution(GameController game,string evolution)
 {
  var xp=game.playerXP;Plugin.SetField(xp,"<level>k__BackingField",19);Plugin.SetField(xp,"xp",0f);xp.GainXP((float)AccessTools.Property(xp.GetType(),"xpToLevel").GetValue(xp)+1);
  while(!(game.CurrentState is GunEvoMenuState)||AccessTools.Field(typeof(flanne.UI.Menu),"ClickEvent").GetValue(game.gunEvoMenu)==null)yield return null;
  var choice=Enumerable.Range(0,3).Select(i=>game.gunEvoMenu.GetEntry(i)).Single(button=>button.GetComponent<GunEvoUI>().data.name==evolution);string name=((GunData)WindField(choice.GetComponent<GunEvoUI>().data,"gunData")).name;Click(choice);
  while(!(game.CurrentState is CombatState)||PlayerController.Instance.gun.gunData.name!=name||PauseController.isPaused)yield return null;
  Require(PlayerController.Instance.gun.gunData.name==name,"actual level20/native evolution menu selects arrow recipe "+name);
 }
 IEnumerator ArrowOwnerReady()
 {while(ArrowOwner()==null||WindField(ArrowOwner(),"OP")==null)yield return null;}
 IEnumerator ArrowNullBoundary()
 {
  SetPhase("arrow-null-empty-reload");var owner=ArrowOwner();var field=AccessTools.Field(typeof(ArrowRetrieveOnReload),"retrievalPoints");var original=ArrowPoints;Require(ArrowActivePoints().Length==0,"null/empty list fixture starts without an active ground point");
  try
  {
   field.SetValue(null,null);int before=ArrowReloads.Count;PlayerController.Instance.ammo.Reload();
   Require(ArrowReloads.Count==before+1&&ArrowReloads.Last().Null&&ArrowReloads.Last().Sound==0&&ArrowReloads.Last().Actors.Count==0,"actual ammo reload follows source null-list return before pool use and sound");
   field.SetValue(null,new List<ArrowRetrievalPoint>());before=ArrowReloads.Count;var pool=WindField(owner,"OP");Plugin.SetField(owner,"OP",null);
   try{PlayerController.Instance.ammo.Reload();}finally{Plugin.SetField(owner,"OP",pool);}
   Require(ArrowReloads.Count==before+1&&!ArrowReloads.Last().Null&&ArrowReloads.Last().Sound==1&&ArrowReloads.Last().Actors.Count==0,"actual ammo reload on a non-null empty list plays source sound without accessing an unused pool");
  }
  finally{field.SetValue(null,original);}
  yield return null;
 }
 IEnumerator ArrowInput(GameController game)
 {
  var p=PlayerController.Instance;while(!(p.CurrentState is flanne.Player.IdleState))yield return null;
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;ArrowPrimaries.Clear();ArrowCapturePrimary=true;ArrowTrackDrops=true;
  var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*8);InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));
  while(ArrowPrimaries.Count==0)yield return null;ArrowCapturePrimary=false;p.gun.GetComponent<AndroidGunRuntime>().ShootEnable.Flip();
  Require(!p.gun.GetComponent<AndroidGunRuntime>().ShootEnable.value,"fixture pauses further gun cadence after real native arrow emission; emitted actor lifetimes/contacts remain live");
 }
 IEnumerator ArrowLand(GameController game)
 {
  var p=PlayerController.Instance;int drops=ArrowDrops.Count;yield return ArrowInput(game);var first=ArrowPrimaries[0];int raw=(int)first.damage;
  ArrowTarget=StatusEnemy();ArrowTarget.transform.position=p.transform.position+Vector3.right*4;arrowPreviousHP=ArrowTarget.HP;arrowDamage.Clear();
  first.transform.position=ArrowTarget.transform.position;var move=first.GetComponent<MoveComponent2D>();move.vector=Vector2.zero;move.enabled=true;Physics2D.SyncTransforms();
  while(arrowDamage.Count==0&&first.gameObject.activeSelf)yield return null;
  Log("arrow-primary-contact",new{recipe=p.gun.gunData.name,raw,targetHP=ArrowTarget.HP,events=string.Join(";",arrowDamage.Select(d=>d.ToString())),active=first.gameObject.activeSelf});
  int expected=p.gun.gunData.model.name=="PF_ArrowCallerGun"?0:raw;
  Require(arrowDamage.Any(d=>(int)d.x==(int)DamageType.Bullet&&(int)d.y==expected),"actual primary contact preserves source Bullet damage, including Arrow Caller's authored zero-multiplier evolution buff");
  while(ArrowPrimaries.Any(s=>s.gameObject.activeSelf))yield return null;
  var actual=ArrowDrops.Skip(drops).Where(d=>d.Initialized&&!d.Secondary).ToArray();Require(actual.Length==ArrowPrimaries.Count&&actual.All(d=>d.Ground!=null),"every actual primary return/expiry produces its original positioned ground arrow");
  var ground=ArrowGroundPrefab();foreach(var drop in actual){Require(drop.Ground.name==ground.name,"actual source ground binding "+ground.name);CombatClose(Vector3.Distance(drop.Ground.transform.position,drop.Position),0,"ground acquisition uses the actual projectile disable position");}
  while(actual.Any(d=>ArrowPoints==null||!ArrowPoints.Contains(d.Ground.GetComponent<ArrowRetrievalPoint>())))yield return null;
  Require(actual.All(d=>ArrowPoints.Count(pnt=>pnt==d.Ground.GetComponent<ArrowRetrievalPoint>())==1),"actual ground Start registers each source point once");
  if(p.gun.gunData.bullet.name=="PF_ArrowCallerProjectile")Require(ground.name=="PF_ThunderCallerProjectileOnGround","actual Arrow Caller retains Android's shared Thunder Caller ground asset");
  Log("arrow-land",p.gun.gunData.name+" primary="+p.gun.gunData.bullet.name+" ground="+ground.name+" count="+actual.Length+" raw="+raw);
 }
 IEnumerator ArrowReloadInput(GameController game,bool saveActive)
 {
  var p=PlayerController.Instance;var owner=ArrowOwner();int before=ArrowReloads.Count;arrowDamage.Clear();arrowPreviousHP=ArrowTarget.HP;
  var expected=ArrowActivePoints().Select(point=>point.transform.position).ToArray();int raw=Mathf.FloorToInt(p.gun.damage*(float)WindField(owner,"damageMulti"));var prefab=(GameObject)WindField(owner,"retrieveArrowPrefab");bool stationary=(bool)WindField(owner,"dontRetrieve");
  var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*8);InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});p.gun.GetComponent<AndroidGunRuntime>().ShootEnable.UnFlip();
  while(ArrowReloads.Count==before)yield return null;var reload=ArrowReloads.Last();Require(reload.Owner==owner&&reload.Actors.Count==expected.Length&&reload.Active==expected.Length,"actual native release/reload traverses every registered active source point");
  Require(reload.Sound==1&&ArrowActivePoints().Length==0,"source reload returns the actual ground points and plays one source sound");
  for(int i=0;i<reload.Actors.Count;i++)
  {var actor=reload.Actors[i];Require(actor.name==prefab.name,"current weapon owns each actual return/explosion recipe");CombatClose(Vector3.Distance(reload.Positions[i],expected[i]),0,"actual return acquisition uses original ground position");CombatClose(actor.GetComponent<Harmful>().damageAmount,raw,"actual reload floors current gun damage times the authored multiplier");Require(stationary||actor.transform.parent==p.transform,"source moving return uses current player parent before its first yielded step");}
  if(saveActive)
  {
   Require(!stationary&&reload.Actors.Any(a=>a.activeSelf),"source checkpoint captures an actual moving return projectile");ArrowTrackDrops=false;
   File.WriteAllText(Path.Combine(ResultDir,"arrow-checkpoint-recipe.txt"),p.gun.gunData.name);yield return DragonSuspend(game,"All four native bow/evolution/input/drop/reload/retrieval workflows and actual moving-return scene exit/source checkpoint");yield break;
  }
  while(reload.Actors.Any(a=>a.activeSelf))yield return null;
  int kind=prefab.name=="PF_MagicBowProjectileRetrieving"?13:(int)DamageType.Bullet;
  Require(arrowDamage.Any(d=>(int)d.x==kind&&(int)d.y==raw),"actual return/explosion physical contact preserves its source damage category and amount");
  if(!stationary)foreach(var actor in reload.Actors)CombatClose(Vector3.Distance(actor.transform.position,p.transform.position),0,"source return releases at current player world position without native final zero reset");
  Log("arrow-reload",p.gun.gunData.name+" recipe="+prefab.name+" damage="+raw+" kind="+kind+" stationary="+stationary);
  ArrowTarget=null;while(!(p.CurrentState is flanne.Player.IdleState))yield return null;
 }
 IEnumerator ArrowSecondaryAndUninitialized()
 {
  var p=PlayerController.Instance;var pool=ObjectPooler.SharedInstance;var prefab=p.gun.gunData.bullet;string tag="Validation_Arrow_BeforeStart";int before=ArrowDrops.Count,points=ArrowActivePoints().Length;
  pool.AddObject(tag,prefab,1);var recipe=(ProjectileRecipe)AccessTools.Method(typeof(Gun),"GetProjectileRecipe").Invoke(p.gun,null);recipe.objectPoolTag=tag;
  var uninitialized=ProjectileFactory.SharedInstance.SpawnProjectile(recipe,Vector2.right,p.transform.position+Vector3.up*3);ReturnFixtureActor(uninitialized.gameObject);
  Require(ArrowDrops.Skip(before).Any(d=>d.Actor==uninitialized.gameObject&&!d.Initialized&&d.Ground==null)&&ArrowActivePoints().Length==points,"real pre-Start arrow return follows source uninitialized no-drop branch");
  recipe.objectPoolTag=prefab.name;before=ArrowDrops.Count;var secondary=ProjectileFactory.SharedInstance.SpawnProjectile(recipe,Vector2.right,p.transform.position+Vector3.up*3,1,true);secondary.GetComponent<MoveComponent2D>().enabled=false;
  while(!(bool)WindField(secondary.GetComponent<SpawnOnDisableProjectile>(),"isInitialized"))yield return null;ReturnFixtureActor(secondary.gameObject);
  Require(ArrowDrops.Skip(before).Any(d=>d.Actor==secondary.gameObject&&d.Initialized&&d.Secondary&&d.Ground==null)&&ArrowActivePoints().Length==points,"actual initialized secondary arrow suppresses recursive ground creation");
 }
 IEnumerator ArrowMixedGroundAndCap()
 {
  var p=PlayerController.Instance;var native=ObjectPooler.SharedInstance;var pool=native.GetComponent<AndroidPools>();
  foreach(var item in new[]{new{Name="PF_ThunderCallerProjectileOnGround",Distance=5f},new{Name="PF_BombProjectileOnGround",Distance=7f}})
  {var prefab=EnemyOriginal(item.Name);native.AddObject(item.Name,prefab,1);var ground=native.GetPooledObject(item.Name);ground.transform.position=p.transform.position+Vector3.up*item.Distance;while(ArrowPoints==null||!ArrowPoints.Contains(ground.GetComponent<ArrowRetrievalPoint>()))yield return null;}
  Require(ArrowActivePoints().Select(point=>point.name).Distinct().Count()==3,"three original ground-arrow pools coexist in the native static retrieval-point list");
  var pools=(IDictionary)WindField(pool,"pools");var entry=pools[ArrowGroundPrefab().name];int maximum=(int)WindField(entry,"Maximum"),before=ArrowDrops.Count,active=ArrowActivePoints().Length;Plugin.SetField(entry,"Maximum",0);
  try
  {
   var recipe=(ProjectileRecipe)AccessTools.Method(typeof(Gun),"GetProjectileRecipe").Invoke(p.gun,null);var shot=ProjectileFactory.SharedInstance.SpawnProjectile(recipe,Vector2.right,p.transform.position+Vector3.up*3);ReturnFixtureActor(shot.gameObject);
   Require(ArrowDrops.Skip(before).Any(d=>d.Actor==shot.gameObject&&d.Initialized&&!d.Secondary&&d.Ground==null)&&ArrowActivePoints().Length==active,"actual initialized primary return ignores the exhausted source ground-pool result without adding a new guard or point");
  }
  finally{Plugin.SetField(entry,"Maximum",maximum);}
 }
 IEnumerator CheckAndroidArrows(bool continuation)
 {
  Log("fixture-precondition","Actual native bow selection/level20 evolution menus/input/primary sampler/expiry/point Start/release-to-reload/physical return/scene exit. Explicit earned access/no runes, stopped scheduler/clock, stationary NPC/contact geometry and temporary gun cadence pause after real emission. Null/empty list and factory secondary/pre-Start branches are controlled operands, restored before leaving the case. No natural leveling/cadence/trajectory/unlock/frequency/device claim.");
  if(continuation)
  {
   yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var expected=File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR"),"arrow-checkpoint-recipe.txt"));
   this.AddObserver(ArrowHurt,"Revamped.AndroidSurvivingHurt");
   Require(PlayerController.Instance.gun.gunData.name==expected&&PlayerController.Instance.gun.GetComponent<AndroidGunRuntime>().ShootEnable.value,"exact source Continue restores evolved bow and ordinary shooting enable");yield return ArrowOwnerReady();
   Require(ArrowActivePoints().Length==0&&!Object.FindObjectsOfType<Harmful>().Any(h=>h.name.Contains("ProjectileRetrieving")),"source Continue omits live ground/retrieval/coroutine phases");
   PlayerController.Instance.SetPosition(new Vector3(100,100,0));yield return null;yield return ArrowLand(game);yield return ArrowReloadInput(game,false);ArrowTrackDrops=false;yield return AchievementExit(game,false);
   yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();Require(ArrowOwner()==null&&ArrowActivePoints().Length==0,"ordinary native restart clears bow owner and registered ground points");yield return CheckRevolver(game,PlayerController.Instance,"android-arrows-original-restart.png");yield return AchievementExit(game,false);this.RemoveObserver(ArrowHurt,"Revamped.AndroidSurvivingHurt");SetPhase("PASS");Log("complete","Four-family arrow source Continue/retrieval and ordinary original restart");Application.Quit(0);yield break;
  }
  RunResume.Invalidate();Progression.State.earned.Add("weapon:MagicBowData");Progression.Refresh();var original=Resources.FindObjectsOfTypeAll<GunData>().First(g=>g.name=="MagicBowData");
  var evolutions=original.gunEvolutions.ToArray();var last=evolutions.Single(e=>((GunData)WindField(e,"gunData")).model.name=="PF_ArrowCallerGun");var cases=new GunEvolution[]{null}.Concat(evolutions.Where(e=>!ReferenceEquals(e,last))).Concat(new[]{last}).ToArray();var families=new HashSet<string>();
  foreach(var evolution in cases)
  {
   yield return AchievementRun("Shana","MagicBowData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();if(evolution!=null)yield return ArrowEvolution(game,evolution.name);yield return ArrowOwnerReady();
   this.AddObserver(ArrowHurt,"Revamped.AndroidSurvivingHurt");
   var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));yield return null;SetPhase("arrow-family-"+p.gun.gunData.name);families.Add(p.gun.gunData.bullet.name);if(evolution==null)yield return ArrowNullBoundary();
   yield return ArrowLand(game);yield return ArrowSecondaryAndUninitialized();if(evolution==null)yield return ArrowMixedGroundAndCap();yield return ArrowReloadInput(game,false);
   if(ReferenceEquals(evolution,last)){Require(families.Count==4,"all four authored bow projectile families reached through actual native selection/evolutions");yield return ArrowLand(game);yield return ArrowReloadInput(game,true);yield break;}
   ArrowTrackDrops=false;yield return AchievementExit(game,false);
  }
 }
}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class ArrowPrimaryProbe
{
 static void Postfix(bool __4,Projectile __result)
 {if(!RuntimeChecks.ArrowCapturePrimary||__4)return;RuntimeChecks.ArrowPrimaries.Add(__result);RuntimeChecks.EmitterPark(__result);}
}
[HarmonyPatch(typeof(SpawnOnDisableProjectile),"OnDisable")]
class ArrowDropProbe
{
 [HarmonyPriority(Priority.First)]static void Prefix(SpawnOnDisableProjectile __instance)
 {if(RuntimeChecks.ArrowTrackDrops)RuntimeChecks.ArrowDropping=new RuntimeChecks.ArrowDrop{Actor=__instance.gameObject,Position=__instance.transform.position,Initialized=(bool)RuntimeChecks.EnemyField(__instance,"isInitialized"),Secondary=__instance.isSecondary};}
 static void Postfix(){var drop=RuntimeChecks.ArrowDropping;if(drop==null)return;RuntimeChecks.ArrowDrops.Add(drop);RuntimeChecks.ArrowDropping=null;}
}
[HarmonyPatch(typeof(ArrowRetrieveOnReload),"OnReload")]
class ArrowReloadProbe
{
 [HarmonyPriority(Priority.First)]static void Prefix(ArrowRetrieveOnReload __instance)
 {if(!RuntimeChecks.ArrowMode)return;var list=(List<ArrowRetrievalPoint>)AccessTools.Field(typeof(ArrowRetrieveOnReload),"retrievalPoints").GetValue(null);RuntimeChecks.ArrowReloading=new RuntimeChecks.ArrowReload{Owner=__instance,Null=list==null,Active=list==null?0:list.Count(p=>p.gameObject.activeSelf)};}
 static void Postfix(){var reload=RuntimeChecks.ArrowReloading;if(reload==null)return;RuntimeChecks.ArrowReloads.Add(reload);RuntimeChecks.ArrowReloading=null;}
}
[HarmonyPatch]
class ArrowPoolProbe
{
 static IEnumerable<MethodBase> TargetMethods()=>AccessTools.GetDeclaredMethods(typeof(AndroidPools)).Where(m=>m.Name=="Get");
 static void Postfix(string tag,GameObject __result)
 {
  if(!RuntimeChecks.ArrowMode||__result==null)return;
  var drop=RuntimeChecks.ArrowDropping;if(drop!=null&&__result.GetComponent<ArrowRetrievalPoint>()!=null)drop.Ground=__result;
  var reload=RuntimeChecks.ArrowReloading;if(reload!=null&&tag==((GameObject)RuntimeChecks.EnemyField(reload.Owner,"retrieveArrowPrefab")).name){reload.Actors.Add(__result);reload.Positions.Add(__result.transform.position);}
 }
}
[HarmonyPatch(typeof(SoundEffectSO),nameof(SoundEffectSO.Play))]
class ArrowSoundProbe
{static void Prefix(SoundEffectSO __instance){var reload=RuntimeChecks.ArrowReloading;if(reload!=null&&ReferenceEquals(__instance,RuntimeChecks.EnemyField(reload.Owner,"retrieveSFX")))reload.Sound++;}}
