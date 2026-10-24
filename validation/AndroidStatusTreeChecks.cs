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
using flanne.PowerupSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool StatusTreeMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-status-tree-");
 internal static bool StatusCaptureShot,StatusCurseSuccess,StatusFreezeSuccess,StatusTrackCurse;
 internal static Projectile StatusShot;
 internal static float StatusShotDamage;internal static int StatusShotPierce,StatusImpactActor,StatusImpactCount,StatusKills;
 internal static readonly List<int> StatusPierceEdges=new List<int>();
 internal static readonly Dictionary<int,int> StatusCurseCalls=new Dictionary<int,int>();
 Health statusTarget;int statusPreviousHP;readonly Dictionary<DamageType,List<int>> statusDamage=new Dictionary<DamageType,List<int>>();
 static readonly Type[] StatusTypes={typeof(DamageBoostAgainstFrozen),typeof(AndroidCurseOnHit),typeof(DamageBoostAgainstCursed),typeof(SmiteHPDamageScaling),typeof(AndroidPierceOnKill),typeof(SpreadCurseOnKill),typeof(BurnOnFreeze)};
 static bool StatusSubscribed(object owner)
 {
  var table=(Dictionary<string,Dictionary<object,List<Action<object,object>>>>)WindField(NotificationCenter.instance,"_table");
  return table.Values.SelectMany(s=>s.Values).SelectMany(h=>h).Any(h=>ReferenceEquals(h.Target,owner));
 }
 void StatusObserve(Health target){statusTarget=target;statusPreviousHP=target.HP;statusDamage.Clear();}
 void StatusHurt(object sender,object args)
 {
  if(!ReferenceEquals(sender,statusTarget))return;var kind=(DamageType)args;
  if(!statusDamage.ContainsKey(kind))statusDamage[kind]=new List<int>();statusDamage[kind].Add(statusPreviousHP-statusTarget.HP);statusPreviousHP=statusTarget.HP;
 }
 void StatusImpact(object sender,object args)
 {if(statusTarget!=null&&ReferenceEquals(args,statusTarget.gameObject)){StatusImpactActor=((Component)sender).gameObject.GetInstanceID();StatusImpactCount++;}}
 void StatusKilled(object sender,object args){if(ReferenceEquals(sender,StatusShot))StatusKills++;}
 Health StatusEnemy(string tag="Untagged",int maximum=100000)
 {
  var h=HitEffectTarget(maximum:maximum,tag:tag);var body=h.GetComponent<Rigidbody2D>();if(body!=null)body.constraints=RigidbodyConstraints2D.FreezeAll;return h;
 }
 IEnumerator StatusReturn(Health target)
 {
  var burn=AndroidBurnState.Get(BurnSystem.SharedInstance).Find(target.gameObject);if(burn!=null)AccessTools.Method(typeof(AndroidBurnStatus),"Release").Invoke(burn,null);
  ReturnFixtureActor(target.gameObject);while(CursedActors().Contains(target)||FreezeSystem.SharedInstance.IsFrozen(target.gameObject))yield return null;statusTarget=null;
 }
 IEnumerator StatusGunHit(GameController game,Health target,bool curse,bool freeze=false)
 {
  var p=PlayerController.Instance;while(!(p.CurrentState is flanne.Player.IdleState))yield return null;p.ammo.Reload();
  StatusObserve(target);StatusCurseSuccess=curse;StatusFreezeSuccess=freeze;StatusImpactCount=StatusKills=0;StatusImpactActor=0;StatusPierceEdges.Clear();StatusShot=null;
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*15);InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});
  while(Vector2.Distance(ShootingCursor.Instance.cursorPosition,aim)>.1f)yield return null;
  StatusCaptureShot=true;InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(StatusShot==null)yield return null;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});var movement=StatusShot.GetComponent<MoveComponent2D>();
  // Seed the emitted actor/target encounter away from last-ammo Smite. The
  // source sampler/health callbacks still perform the actual contact; this
  // does not claim natural travel distance or grenade trajectory accuracy.
  StatusShot.transform.position=p.transform.position+Vector3.right*15;target.transform.position=StatusShot.transform.position+(Vector3)movement.vector.normalized*.5f;target.GetComponent<CircleCollider2D>().radius=1.25f/target.transform.lossyScale.x;Physics2D.SyncTransforms();
  while(StatusImpactCount==0&&StatusShot.gameObject.activeSelf)yield return null;
  Log("status-contact-result","name="+StatusShot.name+" active="+StatusShot.gameObject.activeSelf+" count="+StatusImpactCount+" sender="+StatusImpactActor+" projectile="+StatusShot.gameObject.GetInstanceID()+" hp="+target.HP);
  Require(StatusImpactCount==1&&StatusImpactActor==StatusShot.gameObject.GetInstanceID()&&StatusShot is ExplosiveProjectile,"actual native grenade publishes one projectile-owned global impact");
  Log("status-grenade",new{curse,freeze,raw=StatusShotDamage,pierceBefore=StatusShotPierce,pierceAfter=StatusShot.piercing,hp=target.HP,kills=StatusKills,edges=StatusPierceEdges.ToArray(),damage=statusDamage.ToDictionary(k=>k.Key.ToString(),v=>v.Value.ToArray())});
  // Keep the real disable/explosion callback, but move this fixture grenade away
  // from all measured NPCs before returning it. No synthetic hit/kill events.
  StatusShot.transform.position=p.transform.position+Vector3.up*100;ReturnFixtureActor(StatusShot.gameObject);
 }
 IEnumerator StatusSmite(PlayerController p,int expected)
 {
  while(JusticeSmiteFlowProbe.Running)yield return null;var target=StatusEnemy();target.transform.position=p.transform.position+Vector3.right*2;Physics2D.SyncTransforms();StatusObserve(target);
  p.ammo.Reload();p.ammo.UseAmmo(p.ammo.amount);while(!statusDamage.ContainsKey(DamageType.Smite))yield return null;while(JusticeSmiteFlowProbe.Running)yield return null;
  Require(statusDamage[DamageType.Smite].SequenceEqual(new[]{expected}),"two actual Holy Might owners scale last-ammo Smite using live current HP: "+expected);yield return StatusReturn(target);
 }
 IEnumerator StatusPlague(PlayerController p)
 {
  var seed=StatusEnemy(maximum:1);var near=StatusEnemy();var far=StatusEnemy();seed.transform.position=p.transform.position+Vector3.right*13;
  near.transform.position=seed.transform.position+Vector3.right*1.3f;far.transform.position=seed.transform.position+Vector3.right*5;
  near.GetComponent<CircleCollider2D>().radius=.8f/Mathf.Abs(near.transform.lossyScale.x);var duplicate=near.gameObject.AddComponent<CircleCollider2D>();duplicate.radius=.8f/Mathf.Abs(near.transform.lossyScale.x);Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();Physics2D.SyncTransforms();
  int colliders=Physics2D.OverlapCircleAll(seed.transform.position,1,1<<(int)TagLayerUtil.Enemy).Count(c=>c.gameObject==near.gameObject);
  Log("status-plague-geometry","seed="+seed.transform.position+" near="+near.transform.position+" scale="+near.transform.lossyScale+" layer="+near.gameObject.layer+" count="+colliders+" colliders="+string.Join(";",near.GetComponents<Collider2D>().Select(c=>c.enabled+"/"+c.bounds+"/"+c.isTrigger)));
  Require(Vector2.Distance(seed.transform.position,near.transform.position)>1&&colliders>=2,"declared duplicate-collider fixture overlaps source radius despite its center being outside");
  StatusCurseCalls.Clear();StatusTrackCurse=true;CurseSystem.Instance.Curse(seed.gameObject);while(!seed.isDead)yield return null;
  Require(CursedActors().Count(h=>h==near)==1&&StatusCurseCalls[near.gameObject.GetInstanceID()]==colliders*2&&!StatusCurseCalls.ContainsKey(far.gameObject.GetInstanceID()),"actual curse death reaches both Death Plague physics queries, preserves duplicate collider calls and deduplicates curse state");
  Log("status-plague",new{colliders,nearCalls=StatusCurseCalls[near.gameObject.GetInstanceID()],seedHP=seed.HP});StatusTrackCurse=false;Object.Destroy(duplicate);yield return StatusReturn(near);yield return StatusReturn(far);statusTarget=null;
 }
 IEnumerator CheckAndroidStatusTree(bool continuation)
 {
  GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();foreach(string gate in new[]{"character:Yuki","weapon:GrenadeLauncherData","scenario:Endless_Forest"})Progression.State.earned.Add(gate);Progression.Refresh();
   yield return AchievementRun("Yuki","GrenadeLauncherData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));
  var generator=p.GetComponentInChildren<ProjectileOnShoot>();
  Require(Moths().Length==0&&Convert.ToInt32(WindField(generator,"_counter"))==0&&CursedActors().Count==0,"fresh/source Continue starts with no world statuses or partial Yuki trigger");
  Log("fixture-precondition","Owned Yuki/Grenade/Endless access, no runes, stopped scheduling, stationary original source-pooled NPCs, individual controlled proc rolls. Each acquisition starts below evolution level20; these are native menu/XP integrations, not natural-level or probability measurements. Five actual shots avoid an untested sixth-shot moth interaction.");
  if(!continuation)
  {
   yield return AndroidWhirlwindChest(game,"yuki_perk_brittle_name");yield return AndroidWhirlwindChest(game,"yuki_perk_brittle_name");
   foreach(string key in new[]{"dark_arts_name","wither_name","reaper_rounds","death_plague_name","frost_fire_name","holy_might_name"})
   for(int i=0;i<2;i++)
   {
    Plugin.SetField(game.playerXP,"<level>k__BackingField",1);Plugin.SetField(game.playerXP,"xp",0f);
    var power=FallbackPool(game.powerupGenerator).Single(x=>RunResume.Key(x.powerup)==key).powerup;yield return AndroidPick(game,power.name,game.playerXP.level);
   }
   Plugin.SetField(game.playerXP,"<level>k__BackingField",1);Plugin.SetField(game.playerXP,"xp",0f);yield return AndroidPick(game,"IntenseFlame",game.playerXP.level);
  }
  CombatClose(BurnSystem.SharedInstance.burnDamageMultiplier.Modify(1),1.7f,"two actual Intense Burn acquisitions/source replay contribute35% each to the global source multiplier");
  foreach(var type in StatusTypes)
  {
   while(p.GetComponentsInChildren(type).Cast<Component>().Any(h=>!StatusSubscribed(h)))yield return null;
   Require(p.GetComponentsInChildren(type).Length==2,"actual acquisition/source replay creates exactly two independent "+type.Name+" owners");
  }
  this.AddObserver(StatusHurt,"Revamped.AndroidSurvivingHurt");this.AddObserver(StatusImpact,Projectile.ImpactEvent);this.AddObserver(StatusKilled,Projectile.KillEvent);
  var target=StatusEnemy();yield return StatusGunHit(game,target,false);
  Require(!CursedActors().Contains(target)&&statusDamage[(DamageType)10].SequenceEqual(new[]{(int)StatusShotDamage/2}),"failed Dark Arts rolls leave actual grenade integer half damage unmodified");yield return StatusReturn(target);
  target=StatusEnemy();yield return StatusGunHit(game,target,true);
  Require(CursedActors().Count(h=>h==target)==1&&statusDamage[(DamageType)10].SequenceEqual(new[]{-Mathf.FloorToInt(-((int)StatusShotDamage/2)*1.3f*1.3f)}),"two Dark Arts globally curse the grenade target before two Wither general modifiers apply signed half damage");
  int before=target.HP;target.TakeDamage(DamageType.Summon,10);Require(before-target.HP==17,"repeated Wither also modifies direct nonprojectile Summon damage");yield return StatusReturn(target);
  target=StatusEnemy(maximum:1);yield return StatusGunHit(game,target,true);
  Require(target.isDead&&StatusKills==1&&StatusPierceEdges.SequenceEqual(new[]{StatusShotPierce+1,StatusShotPierce+2})&&StatusShot.piercing==StatusShotPierce+1,"actual post-damage grenade kill grants both Reaper increments before source contact consumes one pierce");yield return StatusReturn(target);
  target=StatusEnemy("Enemy");yield return StatusGunHit(game,target,false,true);
  Require(FreezeSystem.SharedInstance.IsFrozen(target.gameObject)&&AndroidBurnState.Get(BurnSystem.SharedInstance).Find(target.gameObject).Damage==12,"actual FreezeOnHit global producer invokes both Frostfire owners for raw burn12");
  Require(statusDamage[(DamageType)10].SequenceEqual(new[]{-Mathf.FloorToInt(-((int)StatusShotDamage/2)*1.3f*1.3f)}),"Brittle general damage scaling follows real grenade freeze application");
  FreezeSystem.SharedInstance.Freeze(target.gameObject);
  Require(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(target.gameObject).Damage==12,"actual Freeze API refreshes existing state without another Frostfire notification or burn stack");
  CurseSystem.Instance.Curse(target.gameObject);before=target.HP;target.TakeDamage(DamageType.Summon,10);Require(before-target.HP==-Mathf.FloorToInt(-10*1.3f*1.3f*1.3f*1.3f),"two Brittle and two Wither modifiers compose on general signed damage");
  while(!statusDamage.ContainsKey(DamageType.Burn))yield return null;Log("status-burn","damage="+string.Join(",",statusDamage[DamageType.Burn])+" multiplier="+BurnSystem.SharedInstance.burnDamageMultiplier.Modify(12)+" raw="+AndroidBurnState.Get(BurnSystem.SharedInstance).Find(target.gameObject)?.Damage+" frozen="+FreezeSystem.SharedInstance.IsFrozen(target.gameObject)+" cursed="+CursedActors().Contains(target));Require(statusDamage[DamageType.Burn].All(d=>d==34||d==58),"actual Frostfire ticks use repeated Intense Burn before flooring, then active frozen/curse general modifiers");yield return StatusReturn(target);
  target=StatusEnemy();yield return StatusGunHit(game,target,false,true);Require(FreezeSystem.SharedInstance.IsFrozen(target.gameObject)&&!BurnSystem.SharedInstance.IsBurning(target.gameObject),"Frostfire retains source Enemy-tag gate even when actual grenade FreezeOnHit accepts an untagged NPC");yield return StatusReturn(target);
  yield return StatusPlague(p);
  if(p.playerHealth.hp==p.playerHealth.maxHP){if(p.playerHealth.isInvincible.value)p.playerHealth.isInvincible.UnFlip();AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(p.playerHealth,new object[]{1});}
  Require(p.playerHealth.hp==p.playerHealth.maxHP-1,"Holy Might fixture uses actual injured current HP");yield return StatusSmite(p,20+20*p.playerHealth.hp);
  p.playerHealth.Heal(1);yield return StatusSmite(p,20+20*p.playerHealth.hp);
  Require(Convert.ToInt32(WindField(generator,"_counter"))==5&&Moths().Length==0,"exactly five actual grenade shots retain one real partial Yuki counter without moth interference");
  StatusCurseSuccess=StatusFreezeSuccess=false;
  if(!continuation)
  {
   target=StatusEnemy("Enemy");target.transform.position=p.transform.position+Vector3.right*12;FreezeSystem.SharedInstance.Freeze(target.gameObject);CurseSystem.Instance.Curse(target.gameObject);
   Require(FreezeSystem.SharedInstance.IsFrozen(target.gameObject)&&CursedActors().Contains(target)&&BurnSystem.SharedInstance.IsBurning(target.gameObject),"checkpoint includes actual live freeze/curse/Frostfire state");
   yield return DragonSuspend(game,"Seven repeated status upgrades: native acquisitions/global grenade producers, signed general damage, actual curse-death collider propagation, live-health Smite and active-world source checkpoint");yield break;
  }
  this.RemoveObserver(StatusHurt,"Revamped.AndroidSurvivingHurt");this.RemoveObserver(StatusImpact,Projectile.ImpactEvent);this.RemoveObserver(StatusKilled,Projectile.KillEvent);
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();Require(StatusTypes.All(t=>p.GetComponentsInChildren(t).Length==0)&&CursedActors().Count==0,"ordinary restart removes all seven acquired status owner types and world curses");
  target=StatusEnemy();target.TakeDamage(DamageType.Summon,10);Require(target.HP==99990,"ordinary restart has no retained general status damage modifiers");yield return StatusReturn(target);
  yield return CheckRevolver(game,p,"android-status-tree-original-restart.png");yield return AchievementExit(game,false);yield return StatusTomeComposition();SetPhase("PASS");Log("complete","Repeated source status tree native grenade/kill/freeze/curse-death/Smite acquisition, exact-input Continue, combined native tomes and ordinary restart");Application.Quit(0);
 }
 IEnumerator StatusTomeComposition()
 {
  yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();p.SetPosition(new Vector3(100,100,0));
  yield return AndroidPick(game,"Haste",game.playerXP.level);
  foreach(string key in new[]{"tome_of_wind_name","tome_of_might_name","tome_of_glass_name"})
  {
   var pool=(List<PowerupPoolItem>)WindField(game.powerupGenerator,"devilPool");yield return NativeTomePick(game,false,pool.Single(x=>RunResume.Key(x.powerup)==key).powerup.name);
  }
  var target=StatusEnemy();float movement=p.stats[StatType.MoveSpeed].Modify(1);CombatClose(movement,1.2f,"native Haste supplies source Wind's live later-order movement factor");
  for(int factor=2;factor<=6;factor+=2)
  {
   if(factor>2){var pool=(List<PowerupPoolItem>)WindField(game.powerupGenerator,"devilPool");yield return NativeTomePick(game,false,pool.Single(x=>RunResume.Key(x.powerup)=="tome_of_glass_name").powerup.name);}
   int before=target.HP;target.TakeDamage(DamageType.Bullet,20);float expected=(-20*.85f+2)*factor*movement;
   Require(before-target.HP==-Mathf.FloorToInt(expected),"actual Wind/Might/Glass order composes signed Bullet damage with source retained Glass factor "+factor);
   before=target.HP;target.HPChange(7);expected=(7*.85f+2)*factor*movement;
   Require(target.HP-before==Mathf.FloorToInt(expected),"same shared source evaluator preserves positive health-change composition with Glass factor "+factor);
  }
  Log("fixture-precondition","Temporary unrecorded movement operand exercises source Wind's nonpositive-factor branch and is removed immediately; ordinary Haste remains an actual acquisition.");
  p.stats[StatType.MoveSpeed].AddMultiplierBonus(-movement);int old=target.HP;target.TakeDamage(DamageType.Summon,20);Require(old-target.HP==-Mathf.FloorToInt((-20*.85f+2)*6),"zero movement omits Wind's later multiplier while retaining source0.85, Might and Glass6");p.stats[StatType.MoveSpeed].AddMultiplierBonus(movement);yield return StatusReturn(target);
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();target=StatusEnemy();target.TakeDamage(DamageType.Bullet,20);Require(target.HP==99980,"ordinary scene restart removes original tome asset observers and their accumulated factors");yield return StatusReturn(target);yield return CheckRevolver(game,p,"android-modifier-tomes-original-restart.png");yield return AchievementExit(game,false);
 }
}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class StatusTreeShotProbe
{
 static void Postfix(Projectile __result)
 {if(!RuntimeChecks.StatusCaptureShot)return;RuntimeChecks.StatusCaptureShot=false;RuntimeChecks.StatusShot=__result;RuntimeChecks.StatusShotDamage=__result.damage;RuntimeChecks.StatusShotPierce=__result.piercing;}
}
[HarmonyPatch(typeof(AndroidCurseOnHit),"Impact")]
class StatusTreeCurseRoll
{[HarmonyPriority(Priority.First)]static void Prefix(){if(RuntimeChecks.StatusTreeMode)RuntimeChecks.SeedFirstRoll(RuntimeChecks.StatusCurseSuccess?0:.5f,RuntimeChecks.StatusCurseSuccess?.1f:1);}}
[HarmonyPatch(typeof(FreezeOnHit),"OnImpact")]
class StatusTreeFreezeRoll
{[HarmonyPriority(Priority.First)]static void Prefix(){if(RuntimeChecks.StatusTreeMode)RuntimeChecks.SeedFirstRoll(RuntimeChecks.StatusFreezeSuccess?0:.5f,RuntimeChecks.StatusFreezeSuccess?.1f:1);}}
[HarmonyPatch(typeof(BurnOnHit),"OnImpact")]
class StatusTreeBurnRoll
{[HarmonyPriority(Priority.First)]static void Prefix(){if(RuntimeChecks.StatusTreeMode)RuntimeChecks.SeedFirstRoll(.5f,1);}}
[HarmonyPatch(typeof(AndroidPierceOnKill),"Killed")]
class StatusTreePierceProbe
{static void Postfix(object sender){if(RuntimeChecks.StatusTreeMode&&ReferenceEquals(sender,RuntimeChecks.StatusShot))RuntimeChecks.StatusPierceEdges.Add(((Projectile)sender).piercing);}}
[HarmonyPatch(typeof(CurseSystem),nameof(CurseSystem.Curse))]
class StatusTreeCurseCalls
{[HarmonyPriority(Priority.First)]static void Prefix(GameObject target){if(!RuntimeChecks.StatusTrackCurse)return;int id=target.GetInstanceID();RuntimeChecks.StatusCurseCalls[id]=RuntimeChecks.StatusCurseCalls.TryGetValue(id,out var count)?count+1:1;}}
