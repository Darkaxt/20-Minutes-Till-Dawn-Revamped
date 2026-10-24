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
using flanne.CharacterPassives;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 Health characterActorTarget;readonly List<DamageType> characterActorKinds=new List<DamageType>();int characterActorImpacts;
 internal static bool CharacterCloneCapture;internal static Projectile CharacterCloneShot;internal static Vector3 CharacterCloneRecipe;
 internal static Health CharacterBladeKnockbackTarget;internal static int CharacterBladeKnockbacks;internal static Vector2 CharacterBladeKnockbackVector;
 void CharacterActorHurt(object sender,object args){if(ReferenceEquals(sender,characterActorTarget))characterActorKinds.Add((DamageType)args);}
 void CharacterActorImpact(object sender,object args){if(ReferenceEquals(args,characterActorTarget?.gameObject))characterActorImpacts++;}
 static SetHitboxActiveSummon[] CharacterTentacles()=>Object.FindObjectsOfType<SetHitboxActiveSummon>().Where(s=>s.SummonTypeID=="Tentacle").ToArray();
 IEnumerator CheckAndroidCharacterActors(bool continuation)
 {
  bool hina=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("hina-");string character=hina?"Hina":"Hastur";GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("character:"+character);Progression.Refresh();
   yield return AchievementRun(character,"RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));
  Require(p.loadedCharacter.name==character,"actual native character/source Continue selects "+character);
  Log("fixture-precondition","Actual native character and exclusive chest acquisitions, native skill input or automatic four-second tentacle spawn, real pooled target contacts, source checkpoint/Continue/restart. Encounter/game clock stopped; only target placement/AI and observed actor enabling isolate contacts. No natural encounter, purchase, legacy or delivered-package claim.");
  this.AddObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");this.AddObserver(CharacterActorImpact,Projectile.ImpactEvent);
  if(hina)yield return CharacterHina(game,continuation);else yield return CharacterHastur(game,continuation);
  this.RemoveObserver(CharacterActorHurt,"Revamped.AndroidSurvivingHurt");this.RemoveObserver(CharacterActorImpact,Projectile.ImpactEvent);
 }
 IEnumerator CharacterHina(GameController game,bool continuation)
 {
  var p=PlayerController.Instance;var passive=p.GetComponentInChildren<ShadowClonePassive>();var clone=(GameObject)WindField(passive,"shadowClone");var summon=clone.GetComponent<ShootingSummon>();
  Require(!clone.activeSelf,"fresh/source Continue leaves Hina's source-unsaved shadow actor inactive");
  if(!continuation)foreach(string key in new[]{"hina_perk_enhanced_shadow_name","hina_perk_hidden_blade_name","hina_perk_maneuver_name"})yield return AndroidWhirlwindChest(game,key);
  Require(p.GetComponentsInChildren<DamageOnShadowCloneDash>().Length==1&&p.GetComponentsInChildren<ReloadOnShadowCloneDash>().Length==1,"actual Hidden Blade and Maneuver acquisitions/replay attach exactly one native owner each");
  CombatClose(Bonus(p.stats[StatType.SummonDamage]),.25f,"Enhanced Shadow's actual acquisition/replay adds source global25-percent summon damage once");CombatClose((float)WindField(summon,"attackCooldown"),.66f,"Enhanced Shadow multiplies the clone's original two-second base cooldown by0.33");
  Require(Bonus(summon.attackSpeedMod)==0,"Enhanced Shadow does not retain the old Windows direct attack-speed mutation");
  var blade=p.GetComponentInChildren<DamageOnShadowCloneDash>();var maneuver=p.GetComponentInChildren<ReloadOnShadowCloneDash>();
  while(blade.GetComponent<AndroidHiddenBlade>()==null||WindField(maneuver,"shadowClone")==null)yield return null;
  var harm=(Harmful)WindField(blade,"harm");var knockback=(GameObject)WindField(blade.GetComponent<AndroidHiddenBlade>(),"knockback");
  Require(knockback.GetComponent<Rigidbody2D>().useFullKinematicContacts&&Mathf.Abs(knockback.GetComponent<CircleCollider2D>().radius-1.7f)<.001f,"Hidden Blade reconstructs its original full-contact1.7-radius knockback child");
  while(!(p.CurrentState is flanne.Player.IdleState))yield return null;
  if(p.playerHealth.isInvincible.value)p.playerHealth.isInvincible.UnFlip();p.ammo.Reload();p.ammo.UseAmmo(2);Require(p.ammo.amount==4,"actual ammo consumption prepares the Maneuver reload observation");
  characterActorTarget=HitEffectTarget(maximum:100000);characterActorTarget.transform.position=p.transform.position+Vector3.up*.75f;Physics2D.SyncTransforms();int before=characterActorTarget.HP;characterActorKinds.Clear();characterActorImpacts=0;
  CharacterBladeKnockbackTarget=characterActorTarget;CharacterBladeKnockbacks=0;
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*8);
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Right));while(!p.disableAction.value)yield return null;
  summon.enabled=false;InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});
  Require(clone.activeSelf&&p.movementSpeed==18&&p.playerHealth.isInvincible.value,"native skill input creates the clone and starts source six-times movement/invulnerability");Require(p.ammo.amount==6,"actual native onUse invokes Maneuver's immediate source reload");Require(harm.damageAmount==20,"Hidden Blade uses live gun damage without the old triple multiplier or summon scaling");
  while(p.disableAction.value)yield return null;float ended=Time.time;Require(p.movementSpeed==3&&knockback.activeSelf,"native dash completion restores movement and activates the original end knockback");
  // The dash moved the owner away from the initial damage target. Place that
  // same real NPC within the end hitbox to test its corrected contact layer.
  characterActorTarget.transform.position=knockback.transform.position+Vector3.up*.75f;Physics2D.SyncTransforms();
  while(CharacterBladeKnockbacks==0&&knockback.activeSelf)yield return null;
  Log("hina-knockback-contact","callbacks="+CharacterBladeKnockbacks+" captured="+CharacterBladeKnockbackVector+" current="+characterActorTarget.GetComponent<MoveComponent2D>().vector+" immune="+characterActorTarget.GetComponent<MoveComponent2D>().knockbackImmune+" damage="+(before-characterActorTarget.HP)+" layers="+knockback.layer+"/"+characterActorTarget.gameObject.layer+" ignored="+Physics2D.GetIgnoreLayerCollision(knockback.layer,characterActorTarget.gameObject.layer)+" centers="+knockback.transform.position+"/"+characterActorTarget.transform.position+" parent="+knockback.transform.parent.name);
  Require(knockback.layer==(int)TagLayerUtil.PlayerProjectile&&!Physics2D.GetIgnoreLayerCollision(knockback.layer,characterActorTarget.gameObject.layer)&&CharacterBladeKnockbacks>0&&Mathf.Abs(CharacterBladeKnockbackVector.magnitude-4)<.001f,"A265 corrected Hidden Blade child makes a real NPC contact and applies its original force4 without a global matrix change");
  Require(before-characterActorTarget.HP==20&&characterActorKinds.SequenceEqual(new[]{DamageType.Bullet})&&characterActorImpacts==0,"actual Hidden Blade deals one20-damage Bullet contact without a player impact notification");
  ReturnFixtureActor(characterActorTarget.gameObject);CharacterBladeKnockbackTarget=null;characterActorTarget=null;
  while(p.playerHealth.isInvincible.value)yield return null;Require(Time.time-ended>=.28f,"source dash protection persists through its original0.3-second post-end phase with the Hidden Blade release balanced");
  Log("hina-lifetimes-before","harm="+harm.gameObject.activeSelf+" ttl="+harm.GetComponent<TimeToLive>().enabled+" lifetime="+WindField(harm.GetComponent<TimeToLive>(),"lifetime")+" knockback="+knockback.activeSelf+" clone="+clone.activeSelf+" position="+clone.transform.position+" started="+SummonStarted(summon));
  while(harm.gameObject.activeSelf||knockback.activeSelf)yield return null;
  Log("hina-lifetimes-after","clone="+clone.activeSelf+" position="+clone.transform.position+" started="+SummonStarted(summon));
  Require(SummonStarted(summon)&&clone.transform.parent==null&&clone.transform.position==Vector3.zero,"A136 preserves first-Start detachment at the original clone local origin");
  while((float)WindField(passive,"_timer")>0)yield return null;int uses=0;UnityEngine.Events.UnityAction used=()=>uses++;passive.onUse.AddListener(used);
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Right));while(uses==0)yield return null;InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});passive.onUse.RemoveListener(used);
  Require(clone.activeSelf&&Vector3.Distance(clone.transform.position,p.transform.position)<.02f,"second actual native skill repositions and refreshes the already-detached source clone");
  while(p.disableAction.value)yield return null;ended=Time.time;while(p.playerHealth.isInvincible.value)yield return null;
  Require(Time.time-ended>=.28f&&Time.time-ended<=.3f+Time.deltaTime*2+.02f,"second dash without NPC body contact isolates the original0.3-second protection and balanced Hidden Blade release");
  while(harm.gameObject.activeSelf||knockback.activeSelf)yield return null;
  characterActorTarget=HitEffectTarget(maximum:100000);characterActorTarget.transform.position=clone.transform.position+Vector3.up;Physics2D.SyncTransforms();before=characterActorTarget.HP;characterActorKinds.Clear();CharacterCloneShot=null;CharacterCloneCapture=true;summon.enabled=true;
  while(CharacterCloneShot==null&&clone.activeSelf)yield return null;summon.enabled=false;CharacterCloneCapture=false;
  Require(CharacterCloneShot!=null,"actual clone produces its native attack before its source lifetime ends");
  Require(CharacterCloneShot.owner==clone,"actual automatic Shadow Clone attack owns its projectile recipe");CombatClose(CharacterCloneRecipe.x,25,"real Shadow Clone recipe inherits live20 gun damage and global25-percent summon stat");CombatClose(CharacterCloneRecipe.y,15,"real Shadow Clone recipe retains source knockback15");CombatClose(CharacterCloneRecipe.z,99999,"real Shadow Clone recipe retains source pierce99999");
  while(characterActorTarget.HP==before&&CharacterCloneShot.gameObject.activeSelf)yield return null;Require(before-characterActorTarget.HP==25,"actual source MultiProjectile clone attack damages its sampled nearby enemy once");
  ReturnFixtureActor(characterActorTarget.gameObject);characterActorTarget=null;ReturnPlayerShots();if(CharacterCloneShot.gameObject.activeSelf)ReturnFixtureActor(CharacterCloneShot.gameObject);
  Require(SummonStarted(summon)&&Mathf.Abs(ShootingCooldown(summon)-.66f)<.002f,"actual initialized clone uses the source reduced cooldown");
  if(!continuation){Require(clone.activeSelf,"source checkpoint captures an actually active shadow clone");yield return DragonSuspend(game,"Hina native skill/Hidden Blade/Maneuver/Enhanced Shadow and actual clone projectile, active actor source checkpoint");yield break;}
  yield return CharacterActorRestart(game,"Hina");
 }
 IEnumerator CharacterTentacleHit(SetHitboxActiveSummon summon,int damage)
 {
  var p=PlayerController.Instance;while(!SummonStarted(summon))yield return null;foreach(var other in CharacterTentacles())other.enabled=false;
  var harm=(HarmfulOnContact)WindField(summon,"hitbox");while(harm.gameObject.activeSelf)yield return null;
  characterActorTarget=HitEffectTarget(maximum:100000);var axis=(summon.transform.position-p.transform.position).normalized;characterActorTarget.transform.position=p.transform.position+axis*2;Physics2D.SyncTransforms();int before=characterActorTarget.HP;characterActorKinds.Clear();characterActorImpacts=0;summon.enabled=true;
  while(characterActorTarget.HP==before)yield return null;summon.enabled=false;
  Require(harm.GetComponent<Rigidbody2D>().useFullKinematicContacts&&before-characterActorTarget.HP==damage&&harm.damageAmount==damage,"actual automatic Tentacle attack physically deals source "+damage+" through its original full-contact hitbox");
  Require(characterActorKinds.SequenceEqual(new[]{DamageType.Bullet})&&characterActorImpacts==0,"actual Tentacle preserves source Bullet category without impact notification");Require(summon.baseDamage==30,"source keyed Smash does not mutate the actor's original30 base damage");
  ReturnFixtureActor(characterActorTarget.gameObject);characterActorTarget=null;while(harm.gameObject.activeSelf)yield return null;
 }
 IEnumerator CharacterHastur(GameController game,bool continuation)
 {
  var p=PlayerController.Instance;var spawners=p.GetComponentsInChildren<SpawnSummonPassive>();Require(spawners.Length==(continuation?2:1)&&CharacterTentacles().Length==0,"fresh/source Continue initializes native Hastur producer count and omits transient tentacles");
  foreach(var spawner in spawners)spawner.enabled=false;var first=spawners[0];first.enabled=true;while(CharacterTentacles().Length==0)yield return null;first.enabled=false;var tentacle=CharacterTentacles().Single();float created=Time.time;
  CombatClose(Vector3.Distance(tentacle.transform.position,p.transform.position),3,"actual four-second Hastur producer places its tentacle three units from player");CombatClose((float)WindField(tentacle.GetComponent<TimeToLive>(),"lifetime"),15,"original Tentacle lifetime is fifteen seconds");
  if(!continuation)
  {
   yield return CharacterTentacleHit(tentacle,30);yield return AndroidWhirlwindChest(game,"hastur_perk_smash_name");
   Require(p.GetComponent<AndroidHasturProviders>().Damage==30,"actual Smash acquisition installs the keyed thirty-damage provider");yield return CharacterTentacleHit(tentacle,60);
   yield return AndroidWhirlwindChest(game,"hastur_perk_frenzy_name");yield return AndroidWhirlwindChest(game,"hastur_perk_abyssal_call_name");
  }
  var providers=p.GetComponent<AndroidHasturProviders>();Require(providers!=null&&providers.Damage==30&&providers.Cooldown!=null,"native acquisition/source replay reconstructs Smash and Frenzy providers");CombatClose(providers.Cooldown.ModifyInverse(3),1.5f,"source Frenzy provider itself retains the original plus-one inverse modifier");
  CombatClose((float)AccessTools.PropertyGetter(typeof(AttackingSummon),"finalAttackCooldown").Invoke(tentacle,null),3,"actual Tentacle attack cooldown retains its source global/effect consumer without inventing a Frenzy keyed read");yield return CharacterTentacleHit(tentacle,60);
  spawners=p.GetComponentsInChildren<SpawnSummonPassive>();Require(spawners.Length==2,"Abyssal Call actual acquisition/source replay adds exactly one independent native producer");
  var extra=spawners.Single(s=>s!=first);extra.enabled=true;while(CharacterTentacles().Length<2)yield return null;extra.enabled=false;var later=CharacterTentacles().Single(t=>t!=tentacle);yield return CharacterTentacleHit(later,60);
  Require(later.baseDamage==30,"later pooled actor receives keyed source Smash without retroactive base mutation");
  Time.timeScale=4;while(tentacle.gameObject.activeSelf)yield return null;Time.timeScale=1;Require(Time.time-created>=14.9f,"actual first Tentacle expires through its source fifteen-second lifetime");
  if(!continuation)
  {
   if(!later.gameObject.activeSelf){extra.enabled=true;while(CharacterTentacles().Length==0)yield return null;extra.enabled=false;}
   yield return DragonSuspend(game,"Hastur native independent spawners, keyed Smash/Frenzy consumers and physical base/upgraded/later Tentacle attacks, active-actor source checkpoint");yield break;
  }
  yield return CharacterActorRestart(game,"Hastur");
 }
 IEnumerator CharacterActorRestart(GameController game,string character)
 {
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;
  Require(p.GetComponentInChildren<ShadowClonePassive>()==null&&p.GetComponentInChildren<AndroidHiddenBlade>()==null&&p.GetComponent<AndroidHasturProviders>()==null&&p.GetComponentsInChildren<SpawnSummonPassive>().Length==0&&CharacterTentacles().Length==0,"ordinary restart clears "+character+" ability/exclusive/actor/provider ownership");CombatClose(Bonus(p.stats[StatType.SummonDamage]),0,"ordinary restart clears global character summon damage");
  yield return CheckRevolver(game,p,"android-"+character.ToLowerInvariant()+"-actors-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete",character+" actual native character/exclusives/physical actors, source Continue and original restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class ObserveCharacterCloneProjectile
{
 static void Prefix(ProjectileRecipe __0){if(RuntimeChecks.CharacterCloneCapture)RuntimeChecks.CharacterCloneRecipe=new Vector3(__0.damage,__0.knockback,__0.piercing);}
 static void Postfix(Projectile __result){if(RuntimeChecks.CharacterCloneCapture)RuntimeChecks.CharacterCloneShot=__result;}
}
[HarmonyPatch(typeof(Knockback),"OnCollisionEnter2D")]
class ObserveCharacterBladeKnockback
{
 static void Postfix(Knockback __instance,Collision2D __0)
 {if(RuntimeChecks.CharacterBladeKnockbackTarget!=null&&__0.gameObject==RuntimeChecks.CharacterBladeKnockbackTarget.gameObject&&__instance.transform.parent?.GetComponent<AndroidHiddenBlade>()!=null){RuntimeChecks.CharacterBladeKnockbacks++;RuntimeChecks.CharacterBladeKnockbackVector=__0.gameObject.GetComponent<MoveComponent2D>().vector;}}
}
