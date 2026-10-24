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
using flanne.CharacterPassives;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool TransformMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-transform-");
 internal static bool TransformCosmicActive;
 internal static readonly List<Emission> TransformBursts=new List<Emission>();
 internal static readonly List<Projectile> TransformDeerShots=new List<Projectile>();
 internal static int TransformImplosionTarget,TransformImplosionDamage,TransformImplosionHits,TransformImplosionImpacts;
 internal static int TransformImpactCount=>Object.FindObjectOfType<RuntimeChecks>().characterActorImpacts;
 internal static float TransformDueAt,TransformWarningAt,TransformHumanAt;
 internal static bool TransformDueReload;
 static bool TransformCollision(PlayerController p)=>((BoolToggle)WindField(p.playerHealth.GetComponent<AndroidPlayerCollision>(),"EnableCollision")).value;
 static Vacuum[] TransformHoles()=>Object.FindObjectsOfType<Vacuum>().Where(v=>v.name.StartsWith("PF_LunaBlackHole")).ToArray();
 static bool DeerForm(AndroidDasher state)=>(bool)WindField(state,"Transformed");
 static HarmfulOnContact DeerHitbox(AndroidDasher state)=>(HarmfulOnContact)WindField(state,"Hitbox");
 void TransformAim(GameController game)
 {
  var p=PlayerController.Instance;game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*8)});
 }
 IEnumerator TransformReload(GameController game)
 {
  var p=PlayerController.Instance;while(!(p.CurrentState is flanne.Player.IdleState))yield return null;
  p.ammo.UseAmmo(1);TransformAim(game);InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.R));while(!(p.CurrentState is flanne.Player.ReloadState))yield return null;InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());
  while(!(p.CurrentState is flanne.Player.IdleState))yield return null;Require(p.ammo.amount==p.gun.maxAmmo,"actual native R input completes source reload");
 }
 IEnumerator CheckAndroidTransforms(string mode)
 {
  bool luna=mode.Contains("luna"),continuation=mode.EndsWith("continue");string name=luna?"Luna":"Dasher";GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else{RunResume.Invalidate();Progression.State.earned.Add("character:"+name);Progression.State.earned.Add("scenario:Endless_Forest");if(!luna)Progression.State.earned.Add("weapon:SwordData");Progression.Refresh();yield return AchievementRun(name,luna?"RevolverData":"SwordData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();}
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();PlayerController.Instance.SetPosition(new Vector3(100,100,0));
  Log("fixture-precondition","Native character/weapon/Endless selection, exclusive chest choices, synthetic native R/movement/gun input, actual source actor callbacks/physics and exact checkpoints. Earned access/chest offer notification seeded; enemy scheduler/game clock stopped, no runes. Controlled nearby source-pooled NPCs, actor parking and one declared Wall-layer geometry fixture. No synthetic transformation, reload, hole-end, collision or projectile events; not natural encounter/frequency/legacy/Custom/package proof.");
  if(luna)yield return TransformLuna(game,continuation);else yield return TransformDasher(game,continuation);
 }
 IEnumerator TransformLuna(GameController game,bool continuation)
 {
  var p=PlayerController.Instance;while(p.GetComponentsInChildren<SpawnObjectTowardsCursorOnReload>().Any(s=>WindField(s,"OP")==null))yield return null;
  var startup=TransformHoles();Require(startup.Length<=1,"source startup may create one fresh passive hole, without restoring the saved two-hole phase");Log("luna-bootstrap","holes="+startup.Length+" positions="+string.Join(";",startup.Select(v=>v.transform.position.ToString())));while(startup.Any(v=>v.gameObject.activeSelf))yield return null;while(Object.FindObjectsOfType<HarmfulOnContact>().Any(h=>h.name.StartsWith("PF_LunaBlackHoleDamage")))yield return null;ReturnPlayerShots();
  this.AddObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");this.AddObserver(CharacterActorImpact,Projectile.ImpactEvent);
  if(!continuation)
  {
   yield return TransformLunaCycle(game,false);
   foreach(string key in new[]{"luna_perk_parallel_star_name","luna_perk_cosmic_burst_name","luna_perk_implosion_name"})yield return AndroidWhirlwindChest(game,key);
  }
  Require(p.GetComponentsInChildren<SpawnObjectTowardsCursorOnReload>().Length==2&&p.GetComponentsInChildren<SprayBulletsOnNotification>().Length==1&&p.GetComponentsInChildren<SpawnHarmfulObjOnNotification>().Length==1,"Luna actual acquisitions/source replay creates exactly two hole producers, one Cosmic Burst and one Implosion");
  while(p.GetComponentsInChildren<SpawnObjectTowardsCursorOnReload>().Any(s=>WindField(s,"OP")==null)||WindField(p.GetComponentInChildren<SprayBulletsOnNotification>(),"PF")==null||WindField(p.GetComponentInChildren<SpawnHarmfulObjOnNotification>(),"OP")==null)yield return null;
  Require(p.GetComponentsInChildren<SpawnObjectTowardsCursorOnReload>().Select(s=>(float)WindField(s,"distanceFromPlayerToSpawn")).OrderBy(v=>v).SequenceEqual(new[]{-4f,5f}),"source Luna offsets remain asymmetric minus4/plus5");yield return TransformLunaCycle(game,true);
  if(!continuation)
  {
   TransformAim(game);yield return null;p.ammo.Reload();Require(TransformHoles().Length==2,"native source save contains two active transient holes");yield return DragonSuspend(game,"Luna native baseline/three exclusives/asymmetric holes/vacuum/actual hole-end Cosmic Burst and Implosion/current transient checkpoint");yield break;
  }
  this.RemoveObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");this.RemoveObserver(CharacterActorImpact,Projectile.ImpactEvent);yield return TransformRestart(game,"Luna");
 }
 IEnumerator TransformLunaCycle(GameController game,bool upgraded)
 {
  var p=PlayerController.Instance;TransformBursts.Clear();TransformImplosionDamage=TransformImplosionHits=TransformImplosionImpacts=0;yield return TransformReload(game);var holes=TransformHoles();Require(holes.Length==(upgraded?2:1),"real native reload produces the correct number of active holes");
  var forward=holes.Single(h=>h.transform.position.x>p.transform.position.x);CombatClose(Vector3.Distance(forward.transform.position,p.transform.position),5,"actual forward hole uses source five-unit offset from the live player/cursor projection");
  if(upgraded){var back=holes.Single(h=>h!=forward);CombatClose(Vector3.Distance(back.transform.position,p.transform.position),4,"actual Parallel Star uses source four-unit offset");CombatClose(Vector2.Dot(((Vector2)(back.transform.position-p.transform.position)).normalized,((Vector2)(forward.transform.position-p.transform.position)).normalized),-1,"actual Parallel Star points opposite the live forward hole");}
  characterActorTarget=HitEffectTarget(maximum:100000);characterActorTarget.transform.position=forward.transform.position+Vector3.right*1.5f;var move=characterActorTarget.GetComponent<MoveComponent2D>();move.enabled=true;move.vector=Vector2.zero;TransformImplosionTarget=characterActorTarget.gameObject.GetInstanceID();characterActorKinds.Clear();characterActorImpacts=0;Physics2D.SyncTransforms();float initial=Vector3.Distance(characterActorTarget.transform.position,forward.transform.position);
  float closest=initial,inward=0;while(holes.Any(h=>h.gameObject.activeSelf)){closest=Mathf.Min(closest,Vector3.Distance(characterActorTarget.transform.position,forward.transform.position));inward=Mathf.Max(inward,Vector2.Dot(move.vector,((Vector2)(forward.transform.position-characterActorTarget.transform.position)).normalized));yield return null;}yield return null;
  Log("luna-vacuum","initial="+initial+" closest="+closest+" inward="+inward+" final="+Vector3.Distance(characterActorTarget.transform.position,forward.transform.position)+" final-vector="+move.vector+" immune="+move.knockbackImmune);
  Require(closest<initial&&inward>0,"real source full-contact hole physically vacuums the tagged bodyless NPC before its actual end/stun");
  if(upgraded)
  {
   Require(TransformBursts.Count==6,"two actual BlackHoleEnd callbacks produce exactly six Cosmic Burst projectiles");
   foreach(var group in TransformBursts.GroupBy(e=>e.Position))
   {var shots=group.ToArray();Require(shots.Length==3,"each actual hole end produces three shots from its own position");for(int i=0;i<3;i++){CombatClose(shots[i].Damage,10,"actual Cosmic Burst projectile uses half current gun damage");if(i>0)CombatClose(Mathf.Abs(Vector2.SignedAngle(shots[i-1].Direction,shots[i].Direction)),120,"actual Cosmic Burst uses120-degree indexed directions");}}
   var explosions=Object.FindObjectsOfType<HarmfulOnContact>().Where(h=>h.name.StartsWith("PF_LunaBlackHoleDamage")).ToArray();while(explosions.Any(h=>h.gameObject.activeSelf)&&TransformImplosionHits==0)yield return null;
   Require(TransformImplosionHits==1&&TransformImplosionDamage==20&&characterActorKinds.Contains(DamageType.Bullet),"real Implosion physical contact deals source live gun20 Bullet damage");
   Require(TransformImplosionImpacts==0,"actual Implosion Harmful callback emits no source impact notification");Log("luna-impact-scope","all-primary-projectile-impacts="+characterActorImpacts+" implosion-callback-impacts="+TransformImplosionImpacts);
   while(explosions.Any(h=>h.gameObject.activeSelf))yield return null;
  }
  else Require(TransformBursts.Count==0&&TransformImplosionHits==0,"baseline hole has no unacquired Cosmic Burst or Implosion");
  ReturnFixtureActor(characterActorTarget.gameObject);characterActorTarget=null;TransformImplosionTarget=0;ReturnPlayerShots();
 }
 IEnumerator TransformDasher(GameController game,bool continuation)
 {
  var p=PlayerController.Instance;var config=p.GetComponentInChildren<DasherSpecial>();AndroidDasher state;while((state=config.GetComponent<AndroidDasher>())==null)yield return null;
  Require(!DeerForm(state)&&!DeerHitbox(state).gameObject.activeSelf&&(float)WindField(state,"Countdown")>9,"source startup/Continue reconstructs pristine human form and a fresh ten-second countdown");
  if(p.playerHealth.isInvincible.value)p.playerHealth.isInvincible.UnFlip();this.AddObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");
  if(!continuation)
  {
   Require(config.projectiles==0,"source Dasher starts without Hail Bringer projectiles");yield return TransformDeerCycle(game,state,false,false);
   foreach(string key in new[]{"dasher_perk_hail_bringer_name","dasher_perk_hail_bringer_name","dasher_perk_deer_friend_name","dasher_perk_natures_blessing_name"})yield return AndroidWhirlwindChest(game,key);
  }
  Require(config.projectiles==1,"two actual Hail Bringer acquisitions/replay assign one projectile rather than incrementing");Require(NamedSeekers("DeerFriend").Length==1,"actual attachment-only Deer Friend acquisition/replay produces one source summon");
  CombatClose(Bonus(p.stats[StatType.MoveSpeed]),.3f,"Nature's Blessing source movement bonus");CombatClose(Bonus(p.stats[StatType.SummonDamage]),.3f,"Nature's Blessing source summon bonus");CombatClose(Bonus(p.stats[StatType.CharacterSize]),.5f,"Nature's Blessing source character-size bonus");
  var friend=NamedSeekers("DeerFriend").Single();while(!SummonStarted(friend))yield return null;Require(friend.baseDamage==20,"Deer Friend imports original Android base20 instead of retained native50");CombatClose(friend.summonDamageMod.Modify(friend.baseDamage),26,"Deer Friend uses only source summon scaling at base20");p.stats[StatType.MoveSpeed].AddMultiplierBonus(1);CombatClose(friend.summonDamageMod.Modify(friend.baseDamage),26,"Deer Friend has no removed Windows movement-damage compensation");p.stats[StatType.MoveSpeed].AddMultiplierBonus(-1);
  this.AddObserver(WeaponTyped,"Health.TweakSummonDamage");this.AddObserver(WeaponCollision,"WeaponSummon.CollisionEvent");NamedTarget(tag:"Untagged");yield return NamedSeekerContact(friend,26);NamedReturnTarget();this.RemoveObserver(WeaponTyped,"Health.TweakSummonDamage");this.RemoveObserver(WeaponCollision,"WeaponSummon.CollisionEvent");
  yield return TransformDeerCycle(game,state,true,!continuation);if(!continuation)yield break;
  this.RemoveObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");yield return TransformRestart(game,"Dasher");
 }
 IEnumerator TransformDeerCycle(GameController game,AndroidDasher state,bool upgraded,bool save)
 {
  var p=PlayerController.Instance;TransformDeerShots.Clear();TransformDueAt=TransformWarningAt=TransformHumanAt=0;TransformDueReload=false;
  if(upgraded)
  {
   while(DeerForm(state)||!TransformCollision(p))yield return null;
   // A declared near-boundary clock seed exercises the source Sword-only
   // reload branch through actual native input, rather than invoking its coroutine.
   p.ammo.Reload();TransformAim(game);int shots=0;UnityEngine.Events.UnityAction fired=()=>shots++;p.gun.OnShoot.AddListener(fired);var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*8);InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(!(p.CurrentState is flanne.Player.ReloadState))yield return null;InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});p.gun.OnShoot.RemoveListener(fired);Require(shots>=p.gun.maxAmmo&&p.gun.gunData.disableManualReload,"real complete Sword clip enters automatic ReloadState with manual reload still disabled");Log("dasher-sword-auto-reload","actual-shots="+shots+" clip="+p.gun.maxAmmo);ReturnPlayerShots();TransformDeerShots.Clear();TransformDueAt=TransformWarningAt=TransformHumanAt=0;TransformDueReload=false;Plugin.SetField(state,"Countdown",.05f);
  }
  while(!DeerForm(state))yield return null;float began=Time.time;Require(!PauseController.isPaused&&Time.timeScale>0,"source deer transition does not globally pause gameplay");
  if(upgraded)Require(TransformDueReload&&began-TransformDueAt>=1.5f-.03f,"actual Sword ReloadState delays source transformation by1.5 seconds");
  Require(!TransformCollision(p)&&!p.gun.GetComponent<AndroidGunRuntime>().ShootEnable.value&&p.disableAction.value&&p.disableMove.value&&p.disableAnimation.value&&!p.playerHealth.isInvincible.value,"source deer protection disables collision/shooting/action/movement/animation without adding Health invulnerability");
  var hitbox=DeerHitbox(state);Require(hitbox.damageAmount==(upgraded?169:100),"source deer hitbox snapshots nested movement/summon damage");CombatClose(((Knockback)WindField(state,"Knockback")).knockbackForce,upgraded?15:10,"source deer knockback snapshot");
  characterActorTarget=HitEffectTarget(maximum:100000);characterActorTarget.transform.position=hitbox.transform.position;characterActorKinds.Clear();characterActorImpacts=0;int hp=characterActorTarget.HP;Physics2D.SyncTransforms();while(characterActorTarget.HP==hp&&DeerForm(state))yield return null;Require(hp-characterActorTarget.HP==hitbox.damageAmount&&characterActorKinds.Contains(DamageType.Bullet),"actual deer hitbox physical contact deals the source typed damage");ReturnFixtureActor(characterActorTarget.gameObject);characterActorTarget=null;
  if(!upgraded){int old=p.playerHealth.hp;AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(p.playerHealth,new object[]{1});Require(p.playerHealth.hp==old-1,"source collision protection does not prevent an actual direct Health damage call");}
  if(upgraded){while(TransformDeerShots.Count==0&&DeerForm(state))yield return null;Require(TransformDeerShots.Count>0,"actual source deer shot timer emits Hail");yield return EmitterContact(TransformDeerShots.Last(),42);}
  var wall=new GameObject("DasherBoundaryFixture");wall.layer=LayerMask.NameToLayer("Wall");wall.transform.position=p.transform.position+Vector3.right*3;wall.AddComponent<BoxCollider2D>().size=new Vector2(1,8);Physics2D.SyncTransforms();TransformAim(game);InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.D));Vector3 start=p.transform.position;
  if(save)
  {
   while(!TransformDeerShots.Any(s=>s.gameObject.activeSelf)&&DeerForm(state))yield return null;Require(TransformDeerShots.Any(s=>s.gameObject.activeSelf),"upgraded actual deer timer produces a live Hail projectile before checkpoint");InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());Object.Destroy(wall);Require(DeerForm(state)&&!TransformCollision(p),"native checkpoint captures an active source deer phase");yield return DragonSuspend(game,"Dasher complete source lifecycle/native movement/typed physical hitbox/repeated exclusives/Sword-only wait/real Hail shot and active-form checkpoint");yield break;
  }
  while(DeerForm(state))yield return null;TransformHumanAt=Time.time;InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());
  Require(TransformHumanAt-began>=10.2f-.05f&&TransformWarningAt>began&&Mathf.Abs(TransformHumanAt-TransformWarningAt-2)<.12f,"actual source form lasts0.2+8+2 seconds and warning precedes human form by2");
  Require(p.transform.position.x>start.x+.5f&&p.transform.position.x<=wall.transform.position.x-.5f-.35f+.05f&&p.playerSprite.transform.localPosition==Vector3.zero,"actual forced deer movement advances the player root and respects the shared source Wall boundary");Object.Destroy(wall);
  Require(!hitbox.gameObject.activeSelf&&p.gun.gunObj.activeSelf&&p.gun.GetComponent<AndroidGunRuntime>().ShootEnable.value&&!p.disableAction.value&&!p.disableMove.value&&!p.disableAnimation.value&&!TransformCollision(p),"source human return restores gun/actions while collision stays disabled for the final half-second");
  while(!TransformCollision(p))yield return null;Require(Time.time-TransformHumanAt>=.5f-.02f,"source collision protection ends after its actual0.5-second tail");
  Require(!PauseController.isPaused,"source human transition has no old Windows hit-stop");if(!upgraded){Require(TransformDeerShots.Count==0,"base deer emits no Hail projectiles");p.playerHealth.Heal(1);}
  else{Require(TransformDeerShots.Count>0&&TransformDeerShots.All(s=>s.damage==42),"actual Hail shots use source quarter of nested169 damage");foreach(var shot in TransformDeerShots.Where(s=>s.gameObject.activeSelf))ReturnFixtureActor(shot.gameObject);}
 }
 IEnumerator TransformRestart(GameController game,string former)
 {
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;StopFixtureSpawning();
  Require(p.GetComponentInChildren<AndroidDasher>()==null&&p.GetComponentInChildren<DasherSpecial>()==null&&NamedSeekers("DeerFriend").Length==0&&TransformHoles().Length==0&&p.GetComponentsInChildren<SpawnObjectTowardsCursorOnReload>().Length==0&&p.GetComponentInChildren<SprayBulletsOnNotification>()==null&&p.GetComponentInChildren<SpawnHarmfulObjOnNotification>()==null,"ordinary restart clears all prior "+former+" owners/actors");
  Require(TransformCollision(p)&&p.gun.GetComponent<AndroidGunRuntime>().ShootEnable.value&&!p.disableAction.value&&!p.disableMove.value&&!p.disableAnimation.value,"ordinary restart has fresh source collision/shoot/action/movement flags");yield return CheckRevolver(game,p,"android-"+former.ToLowerInvariant()+"-original-restart.png");
  p.SetPosition(new Vector3(100,100,0));InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.D));while(p.moveVec.x<.9f)yield return null;float at=Time.time,largest=0;Vector3 from=p.transform.position;while(p.transform.position.x-from.x<.5f){largest=Mathf.Max(largest,Time.deltaTime);yield return null;}float distance=p.transform.position.x-from.x,elapsed=Time.time-at;InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());while(p.moveVec!=Vector3.zero)yield return null;Require(Mathf.Abs(distance-p.finalMoveSpeed*elapsed)<=p.finalMoveSpeed*largest*2+.01f,"fresh native Shana input preserves ordinary source movement after forced-movement integration");
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete",former+" complete source character workflow/native producers/physics/current Continue/restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(SprayBulletsOnNotification),"ShootBullets")]
class ObserveTransformCosmic
{static void Prefix(){RuntimeChecks.TransformCosmicActive=RuntimeChecks.TransformMode;}static void Postfix(){RuntimeChecks.TransformCosmicActive=false;}}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class ObserveTransformCosmicShot
{static void Postfix(ProjectileRecipe __0,Vector2 __1,Vector3 __2,Projectile __result){if(RuntimeChecks.TransformCosmicActive)RuntimeChecks.TransformBursts.Add(new RuntimeChecks.Emission{Shot=__result,Position=__2,Direction=__1,Damage=__result.damage,RecipeDamage=__0.damage});}}
[HarmonyPatch(typeof(HarmfulOnContact),"OnCollisionEnter2D")]
class ObserveTransformImplosion
{
 [HarmonyPriority(Priority.First)]static void Prefix(HarmfulOnContact __instance,Collision2D __0,out int[] __state)=>__state=RuntimeChecks.TransformMode&&__instance.name.StartsWith("PF_LunaBlackHoleDamage")&&__0.gameObject.GetInstanceID()==RuntimeChecks.TransformImplosionTarget?new[]{__0.gameObject.GetComponent<Health>().HP,RuntimeChecks.TransformImpactCount}:null;
 static void Postfix(Collision2D __0,int[] __state){if(__state!=null){RuntimeChecks.TransformImplosionHits++;RuntimeChecks.TransformImplosionDamage=__state[0]-__0.gameObject.GetComponent<Health>().HP;RuntimeChecks.TransformImplosionImpacts+=RuntimeChecks.TransformImpactCount-__state[1];}}
}
[HarmonyPatch(typeof(AndroidDasher),"Transform")]
class ObserveTransformDue
{static void Prefix(){if(RuntimeChecks.TransformMode){RuntimeChecks.TransformDueAt=Time.time;RuntimeChecks.TransformDueReload=PlayerController.Instance.CurrentState is flanne.Player.ReloadState;}}}
[HarmonyPatch(typeof(AndroidDasher),"Shoot")]
class ObserveTransformDeerShot
{
 static void Prefix(out HashSet<int> __state)=>__state=RuntimeChecks.TransformMode?new HashSet<int>(Object.FindObjectsOfType<Projectile>().Select(s=>s.GetInstanceID())):null;
 static void Postfix(HashSet<int> __state){if(__state==null)return;foreach(var shot in Object.FindObjectsOfType<Projectile>().Where(s=>s.name.StartsWith("PF_DeerProjectile")&&!__state.Contains(s.GetInstanceID()))){RuntimeChecks.TransformDeerShots.Add(shot);shot.GetComponent<MoveComponent2D>().enabled=false;shot.GetComponent<Rigidbody2D>().velocity=Vector2.zero;shot.transform.position=PlayerController.Instance.transform.position+Vector3.up*35;}}
}
[HarmonyPatch(typeof(SoundEffectSO),nameof(SoundEffectSO.Play))]
class ObserveTransformWarning
{static void Prefix(SoundEffectSO __instance){if(RuntimeChecks.TransformMode&&__instance.name.StartsWith("DasherTransformWarning"))RuntimeChecks.TransformWarningAt=Time.time;}}
