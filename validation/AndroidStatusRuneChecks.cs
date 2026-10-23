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
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool StatusRuneMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-status-runes-");
 internal static bool MysticRollControl,MysticBurn,MysticFreeze,SpreadRollControl,SpreadSuccess,GlyphBurst;
 internal static int MysticCallbacks,GlyphBursts;
 internal static readonly List<Projectile> GlyphShots=new List<Projectile>();
 internal static readonly List<Vector3> GlyphRecipes=new List<Vector3>();
 internal static Health GlyphKnockbackTarget;internal static int GlyphKnockbacks;internal static Vector2 GlyphKnockbackVector;
 Health statusShockTarget;readonly List<DamageType> statusShockKinds=new List<DamageType>();
 void StatusShockHurt(object sender,object args){if(ReferenceEquals(sender,statusShockTarget))statusShockKinds.Add((DamageType)args);}
 internal static void SeedMysticRolls()
 {
  for(int seed=0;;seed++)
  {
   UnityEngine.Random.InitState(seed);var state=UnityEngine.Random.state;
   float burn=UnityEngine.Random.Range(0f,1f),freeze=UnityEngine.Random.Range(0f,1f);
   if((burn<.06f)==MysticBurn&&(freeze<.06f)==MysticFreeze){UnityEngine.Random.state=state;return;}
  }
 }
 static GameObject[] StatusSpreaders()=>Object.FindObjectsOfType<TimeToLive>().Where(t=>t.name.StartsWith("WildMagic")).Select(t=>t.gameObject).ToArray();
 static void ReturnPlayerShots()
 {foreach(var shot in Object.FindObjectsOfType<Projectile>())if(shot.owner==PlayerController.Instance.gameObject||shot.owner==PlayerController.Instance.gun.gameObject)ReturnFixtureActor(shot.gameObject);}
 IEnumerator StatusRuneShot(GameController game,Health target,bool burn,bool freeze,string tag)
 {
  var p=PlayerController.Instance;target.tag=tag;target.transform.position=p.transform.position+Vector3.right*4;Physics2D.SyncTransforms();
  while(!(p.CurrentState is flanne.Player.IdleState))yield return null;p.ammo.Reload();
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  int hits=0,callbacks=MysticCallbacks;UnityEngine.Events.UnityAction<int> hurt=d=>hits++;ObserveDamage(target,hurt);bool fired=false;UnityEngine.Events.UnityAction onShoot=()=>fired=true;p.gun.OnShoot.AddListener(onShoot);
  MysticBurn=burn;MysticFreeze=freeze;MysticRollControl=true;
  try
  {
   var aim=(Vector2)Camera.main.WorldToScreenPoint(target.transform.position);
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(!fired)yield return null;
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});p.gun.OnShoot.RemoveListener(onShoot);
   while(hits==0)yield return null;
   Require(MysticCallbacks==callbacks+1,"one actual native player projectile reaches Mysticism's scoped impact callback");
   bool eligible=tag.Contains("Enemy");Require(BurnSystem.SharedInstance.IsBurning(target.gameObject)==(eligible&&burn)&&FreezeSystem.SharedInstance.IsFrozen(target.gameObject)==(eligible&&freeze),"controlled independent six-percent source rolls preserve tag eligibility: "+tag+" burn="+burn+" freeze="+freeze);
   if(eligible&&burn)CombatClose(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(target.gameObject).Damage,3,"Mysticism uses its original three-damage burn operand");
   if(eligible&&freeze)
   {
    var owner=FreezeSystem.SharedInstance.GetComponent<AndroidFreezeState>();var entries=(IEnumerable)WindField(owner,"entries");
    var entry=entries.Cast<object>().Single(e=>(GameObject)WindField(e,"Target")==target.gameObject);float remaining=(float)WindField(entry,"Remaining"),expected=tag=="EnemyChampion"?.3f:3;
    Require(remaining<=expected&&remaining>=expected-Time.deltaTime*2-.01f,"actual Mysticism freeze starts at three seconds, with exact Champion one-tenth scaling");
   }
   while(!(p.CurrentState is flanne.Player.IdleState))yield return null;
  }
  finally{MysticRollControl=false;p.gun.OnShoot.RemoveListener(onShoot);UnobserveDamage(target,hurt);}
  ReturnFixtureActor(target.gameObject);while(FreezeSystem.SharedInstance.IsFrozen(target.gameObject))yield return null;ReturnPlayerShots();
 }
 IEnumerator StatusRuneSpread(bool success)
 {
  var dead=HitEffectTarget(maximum:100000,tag:"Untagged");var center=dead.transform.position;
  var near=HitEffectTarget(maximum:100000);near.transform.position=center+Vector3.right*.5f;
  var far=HitEffectTarget(maximum:100000);far.transform.position=center+Vector3.right*3;
  var untagged=HitEffectTarget(maximum:100000,tag:"Untagged");untagged.transform.position=center+Vector3.up*.5f;
  BurnSystem.SharedInstance.Burn(dead.gameObject,1);AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidFreezeApplication"),"Apply").Invoke(null,new object[]{FreezeSystem.SharedInstance,dead.gameObject,3f});
  SpreadSuccess=success;SpreadRollControl=true;
  try{dead.TakeDamage(DamageType.Bullet,100000);}finally{SpreadRollControl=false;}
  var actors=StatusSpreaders();Require(actors.Length==(success?2:0),"actual untagged Health death uses one source15-percent roll and independently spawns both existing statuses");
  foreach(var actor in actors)
  {
   Require(Vector3.Distance(actor.transform.position,center)<.001f,"source status spreader originates at the dead actor");
   Require(actor.GetComponent<Rigidbody2D>().useFullKinematicContacts&&!actor.GetComponent<Collider2D>().isTrigger,"original full-kinematic non-trigger spreaders can reach the source bodyless NPC");
   CombatClose((float)WindField(actor.GetComponent<TimeToLive>(),"lifetime"),.1f,"original source spreader lifetime");
  }
  Physics2D.SyncTransforms();while(actors.Any(a=>a.activeInHierarchy))yield return null;
  Require(BurnSystem.SharedInstance.IsBurning(near.gameObject)==success&&FreezeSystem.SharedInstance.IsFrozen(near.gameObject)==success,"actual short-lived physical spreaders apply burn and freeze to the nearby enemy");
  Require(!BurnSystem.SharedInstance.IsBurning(far.gameObject)&&!FreezeSystem.SharedInstance.IsFrozen(far.gameObject)&&!BurnSystem.SharedInstance.IsBurning(untagged.gameObject)&&!FreezeSystem.SharedInstance.IsFrozen(untagged.gameObject),"source spread radius and collision tag predicates reject distant and untagged neighbors");
  if(success)CombatClose(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(near.gameObject).Damage,3,"actual spread collision applies the original three-damage burn");
  foreach(var h in new[]{near,far,untagged})ReturnFixtureActor(h.gameObject);
  while(new[]{dead,near,far,untagged}.Any(h=>FreezeSystem.SharedInstance.IsFrozen(h.gameObject)))yield return null;
 }
 IEnumerator StatusRuneGlyph(SpawnerRune spawner)
 {
  var p=PlayerController.Instance;var stat=p.stats[StatType.SummonDamage];stat.AddFlatBonus(7);stat.AddMultiplierBonus(.5f);
  var recipe=p.gun.GetProjectileRecipe();float expected=stat.Modify(recipe.damage)*.1f;GlyphShots.Clear();GlyphRecipes.Clear();int prior=GlyphBursts;
  spawner.enabled=true;while(Object.FindObjectOfType<BulletGlyph>()==null)yield return null;var glyph=Object.FindObjectOfType<BulletGlyph>();spawner.enabled=false;
  Require(Vector3.Distance(glyph.transform.position,p.transform.position)<.001f,"native rank-three Gun Glyph spawns automatically at the player");float created=Time.time;
  var knockback=(GameObject)WindField(glyph,"knockbackObject");GlyphKnockbackTarget=HitEffectTarget(maximum:100000);GlyphKnockbackTarget.transform.position=glyph.transform.position+Vector3.up*.25f;Physics2D.SyncTransforms();GlyphKnockbacks=0;
  while(!knockback.activeSelf)yield return null;float activated=Time.time;
  Require(activated-created>=2.7f,"original glyph charges before its final knockback phase");
  while(GlyphKnockbacks==0&&knockback.activeSelf)yield return null;Require(GlyphKnockbacks==1&&Mathf.Abs(GlyphKnockbackVector.magnitude-5)<.001f&&GlyphKnockbackVector.y>0,"actual Glyph child physically applies the original five-force outward knockback");ReturnFixtureActor(GlyphKnockbackTarget.gameObject);GlyphKnockbackTarget=null;
  while(GlyphBursts==prior)yield return null;
  Require(GlyphShots.Count==6&&GlyphRecipes.Count==6,"real glyph lifetime coroutine emits the original six-bullet secondary ring");
  foreach(var value in GlyphRecipes){CombatClose(value.x,expected,"Glyph applies summon flat and percent stats to gun damage before literal0.1");CombatClose(value.y,recipe.size*.5f,"Glyph retains half-size source bullets");CombatClose(value.z,recipe.knockback,"Glyph retains full source recipe knockback");}
  var directions=GlyphShots.Select(s=>s.GetComponent<MoveComponent2D>().vector.normalized).ToArray();for(int i=1;i<6;i++)CombatClose(Vector2.Dot(directions[i-1],directions[i]),.5f,"actual glyph directions are spaced sixty degrees apart");
  Require(!glyph.gameObject.activeSelf&&!knockback.activeSelf,"actual glyph lifetime returns its actor and disables its knockback child");
  var target=HitEffectTarget(maximum:100000);target.transform.position=GlyphShots[0].transform.position+(Vector3)directions[0]*2;Physics2D.SyncTransforms();int before=target.HP;
  while(target.HP==before&&GlyphShots[0].gameObject.activeSelf)yield return null;
  Require(before-target.HP==Mathf.FloorToInt(expected),"actual glyph projectile physically deals the source rounded damage");ReturnFixtureActor(target.gameObject);ReturnPlayerShots();
  stat.AddFlatBonus(-7);stat.AddMultiplierBonus(-.5f);
 }
 IEnumerator StatusRuneShock(GameController game,ActivateGameObjOnLastAmmoRune shock)
 {
  var p=PlayerController.Instance;var harm=(HarmfulOnContact)WindField(shock,"harm");Require(!harm.gameObject.activeSelf,"source Shockwave starts inactive");
  var target=HitEffectTarget(maximum:100000);target.transform.position=p.transform.position+Vector3.up;Physics2D.SyncTransforms();int before=target.HP;
  statusShockTarget=target;statusShockKinds.Clear();this.AddObserver(StatusShockHurt,"Revamped.AndroidSurvivingHurt");MysticBurn=false;MysticFreeze=false;MysticRollControl=true;
  var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*8);p.ammo.Reload();
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(p.ammo.amount!=0)yield return null;
  float emptied=Time.time;InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});while(!harm.gameObject.activeSelf)yield return null;
  Require(Time.time-emptied>=.08f,"actual last-ammo notification observes the source one-tenth-second delayed activation");Require(harm.damageAmount==12,"rank-three Shockwave uses floor of live20 damage times0.2 times3");
  while(target.HP==before&&harm.gameObject.activeSelf)yield return null;
  Require(before-target.HP==12&&statusShockKinds.Count==1&&statusShockKinds[0]==DamageType.Bullet,"real Shockwave collision deals its source Bullet-category damage exactly once");
  while(harm.gameObject.activeSelf)yield return null;Require((float)WindField(harm.GetComponent<TimeToLive>(),"lifetime")==.3f,"Shockwave retains its original0.3-second lifetime");
  MysticRollControl=false;this.RemoveObserver(StatusShockHurt,"Revamped.AndroidSurvivingHurt");statusShockTarget=null;ReturnFixtureActor(target.gameObject);while(!(p.CurrentState is flanne.Player.IdleState))yield return null;ReturnPlayerShots();
 }
 IEnumerator CheckAndroidStatusRunes(bool continuation)
 {
  GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>
   {Loadout.RuneSelection=new[]{"MysticismRuneData","WildMagicRuneData","GunGlyphRuneData","ShockwaveRuneData"}.Select(name=>{var copy=Object.Instantiate(Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.name==name));copy.name=name;copy.level=3;return copy;}).ToList();});game=Object.FindObjectOfType<GameController>();
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));
  var mystic=p.GetComponentInChildren<BurnFreezeOnHitRune>();var spread=p.GetComponentInChildren<SpreadStatusRune>();var glyph=p.GetComponentInChildren<SpawnerRune>();var shock=p.GetComponentInChildren<ActivateGameObjOnLastAmmoRune>();
  Require(mystic!=null&&spread!=null&&glyph!=null&&shock!=null,"all four original rank-three native rune helpers enter fresh/source Continue gameplay");glyph.enabled=false;yield return null;
  if(continuation)Require(Object.FindObjectOfType<BulletGlyph>()==null&&StatusSpreaders().Length==0&&!(bool)((HarmfulOnContact)WindField(shock,"harm")).gameObject.activeSelf&&(float)WindField(glyph,"_timer")<7,"fresh-process source Continue omits the saved active Glyph and transient status/Shockwave phases and rebuilds its native scheduler");
  Require(mystic.chancePerLevel==.02f&&mystic.freezeDuration==3&&mystic.burnDamge==3&&(float)WindField(spread,"spreadChancePerLevel")==.05f,"original Mysticism and Wild Magic operands are retained");
  Log("fixture-precondition","Four explicit rank-three rune copies in actual Standard mode. Real native input/projectile contacts, real pooled status death/spread physics, native Glyph spawn/lifetime and last-ammo Shockwave. Only source proc rolls and temporary Glyph stat operands are controlled; encounter/game clock stopped. No natural probability, purchases or encounter scheduling claim.");
  yield return StatusRuneShot(game,HitEffectTarget(),true,true,"Untagged");
  yield return StatusRuneShot(game,HitEffectTarget(),true,false,"Enemy");
  yield return StatusRuneShot(game,HitEffectTarget(),false,true,"Enemy");
  yield return StatusRuneShot(game,HitEffectTarget(),false,false,"Enemy");
  yield return StatusRuneShot(game,HitEffectTarget(),true,true,"EnemyChampion");
  yield return StatusRuneSpread(false);yield return StatusRuneSpread(true);yield return StatusRuneGlyph(glyph);yield return StatusRuneShock(game,shock);
  if(!continuation)
  {
   glyph.enabled=true;while(Object.FindObjectOfType<BulletGlyph>()==null)yield return null;glyph.enabled=false;
   Require((float)WindField(glyph,"_timer")<7,"actual source checkpoint retains a partially elapsed Glyph scheduler without manually assigning its timer");
   yield return DragonSuspend(game,"Native Mysticism eligibility/rolls, Wild Magic physical propagation, charged Glyph recipes/contact and last-ammo Shockwave, active-glyph source checkpoint");yield break;
  }
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  Require(p.GetComponentsInChildren<Rune>().Length==0&&Object.FindObjectOfType<BulletGlyph>()==null&&StatusSpreaders().Length==0,"ordinary restart removes all four rune owners and transient actors");
  yield return CheckRevolver(game,p,"android-status-runes-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Four native status/glyph/Shockwave rune workflows, source Continue and ordinary restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(BurnFreezeOnHitRune),"OnImpact")]
class ControlSourceMysticismRolls
{
 [HarmonyPriority(Priority.First)] static void Prefix(){if(RuntimeChecks.MysticRollControl){RuntimeChecks.MysticCallbacks++;RuntimeChecks.SeedMysticRolls();}}
}
[HarmonyPatch(typeof(SpreadStatusRune),"OnDeath")]
class ControlSourceWildMagicRoll
{
 [HarmonyPriority(Priority.First)] static void Prefix(){if(RuntimeChecks.SpreadRollControl)RuntimeChecks.SeedFirstRoll(RuntimeChecks.SpreadSuccess?0:.16f,RuntimeChecks.SpreadSuccess?.14f:1);}
}
[HarmonyPatch(typeof(BulletGlyph),"ShootBullets")]
class ObserveSourceGlyphBurst
{
 [HarmonyPriority(Priority.First)] static void Prefix(){if(RuntimeChecks.StatusRuneMode)RuntimeChecks.GlyphBurst=true;}
 static void Postfix(){if(RuntimeChecks.StatusRuneMode){RuntimeChecks.GlyphBurst=false;RuntimeChecks.GlyphBursts++;}}
}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class ObserveSourceGlyphRecipes
{
 static void Prefix(ProjectileRecipe __0){if(RuntimeChecks.GlyphBurst)RuntimeChecks.GlyphRecipes.Add(new Vector3(__0.damage,__0.size,__0.knockback));}
 static void Postfix(Projectile __result){if(RuntimeChecks.GlyphBurst)RuntimeChecks.GlyphShots.Add(__result);}
}
[HarmonyPatch(typeof(Knockback),"OnCollisionEnter2D")]
class ObserveSourceGlyphKnockback
{
 static void Postfix(Knockback __instance,Collision2D __0)
 {if(RuntimeChecks.GlyphKnockbackTarget!=null&&__0.gameObject==RuntimeChecks.GlyphKnockbackTarget.gameObject&&__instance.transform.parent?.GetComponent<BulletGlyph>()!=null){RuntimeChecks.GlyphKnockbacks++;RuntimeChecks.GlyphKnockbackVector=__0.gameObject.GetComponent<MoveComponent2D>().vector;}}
}
