using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using flanne.Pickups;
using flanne.UI;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
using TwentyMinutes.Modernization;
public partial class RuntimeChecks
{
 IEnumerator SkillInput(PortedCharacterSkill skill)
 {
  var action=Characters.Inputs.FindActionMap("PlayerMap").FindAction("Skill");Log("native-skill-bindings",string.Join(";",action.bindings.Select(x=>x.effectivePath)));
  Require(action.bindings.Any(x=>x.effectivePath=="<Mouse>/rightButton"),"native skill binding includes right mouse");
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState().WithButton(MouseButton.Right));yield return null;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState());yield return null;
 }
 Health DurableEnemy(GameObject original,Vector3 position,bool clone)
 {
  var enemy=clone?Object.Instantiate(original):original;
  // Native controllers iterate registered components even when their individual
  // MonoBehaviours are disabled. Remove only these fixture targets from AI;
  // retain native movement so real knockback can still be measured.
  foreach(var ai in enemy.GetComponents<AIComponent>()){ai.enabled=false;AIController.SharedInstance.UnRegister(ai);}
  var move=enemy.GetComponent<MoveComponent2D>();if(move!=null){move.vector=Vector2.zero;move.drag=0;move.enabled=true;}
  var body=enemy.GetComponent<Rigidbody2D>();if(body!=null)body.velocity=Vector2.zero;
  enemy.transform.position=position;var health=enemy.GetComponent<Health>();health.maxHP=10000;
  AccessTools.Field(typeof(Health),"<HP>k__BackingField").SetValue(health,10000);
  return health;
 }
 IEnumerator ExclusiveChest(GameController game,PortedCharacterSkill skill)
 {
  var offered=new HashSet<string>();
  for(int i=0;i<3;i++)
  {
   SetPhase("exclusive-chest-"+i);
   // Actual native chest-pickup notification -> native random exclusive pool ->
   // native take button -> deep-copied PerkEffect -> recovered action/stat rules.
   this.PostNotification(ChestPickup.ChestPickupEvent);
   while(!(game.CurrentState is ChestState))yield return null;
   var chest=(ChestUIController)AccessTools.Property(typeof(GameState),"chestUIController").GetValue(game.CurrentState);
   var take=(Button)AccessTools.Field(typeof(ChestUIController),"takeButton").GetValue(chest);
   while(!take.gameObject.activeInHierarchy || !take.IsInteractable())yield return null;
   var power=(Powerup)AccessTools.Field(typeof(ChestState),"_currPowerup").GetValue(game.CurrentState);
   Require(Characters.Powers.Values.Contains(power) && offered.Add(power.name),"native exclusive chest offers a distinct recovered upgrade: "+power.name);
   yield return Capture("exclusive-"+i+".png");Click(take);
   while(!(game.CurrentState is CombatState))yield return null;
  }
  Require(offered.Count==3,"all three exclusive upgrades are naturally drawn and consumed from this character's native pool");
 }
 IEnumerator CheckCharacter(GameController game,PlayerController player,string mode)
 {
  SetPhase("character-base");var skill=player.GetComponentInChildren<PortedCharacterSkill>();while(skill==null || !skill.Ready){skill=player.GetComponentInChildren<PortedCharacterSkill>();yield return null;}
  var contract=skill.Contract;Require(player.loadedCharacter==Characters.Data[contract.key],"native selected recovered character is the actual player loadout");
  Require(player.playerHealth.maxHP==contract.startHP && Mathf.Abs(player.movementSpeed-contract.moveSpeed)<.001f,"native base HP and movement match recovered form");
  Require(player.loadedCharacter.icon==Characters.Sprites[contract.clips.Single(x=>x.name.EndsWith("Idle")).frames[0].sprite],"native HUD/intro icon binds the recovered body frame instead of the selection portrait");
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  player.playerHealth.isInvincible.Flip();int ownInvincibility=player.playerHealth.isInvincible.flips;
  yield return CheckRevolver(game,player,"character-fire.png");
  foreach(string state in new[]{"Idle","Run","Walk"})
  {
   SetPhase("character-animation-"+state);
   if(state=="Idle")InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());
   else InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.D));
   while((player.moveVec==Vector3.zero)!=(state=="Idle"))yield return null;
   player.moveSpeedMultiplier=state=="Walk"?.5f:1;
   var clip=contract.clips.Single(x=>x.name.EndsWith(state));var expected=new HashSet<Sprite>(clip.frames.Select(x=>Characters.Sprites[x.sprite]));var seen=new HashSet<Sprite>();float elapsed=0;
   while(elapsed<clip.duration*2){yield return null;if(!expected.Contains(player.playerSprite.sprite))throw new InvalidOperationException("Native "+state+" animation left recovered source frames");seen.Add(player.playerSprite.sprite);elapsed+=Time.deltaTime;}
   Require(seen.Count==expected.Count,"complete recovered "+state+" animation cycle is visible");yield return Capture("character-"+state+".png");
  }
  InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());while(player.moveVec!=Vector3.zero)yield return null;player.moveSpeedMultiplier=1;player.SetPosition(Vector3.zero);
  var abilityCollider=skill.GetComponent<CircleCollider2D>();Require(!abilityCollider.isTrigger && skill.GetComponent<Rigidbody2D>().useFullKinematicContacts,"source contact collider uses native full kinematic contacts");
  GameObject original;
  while((original=AIController.SharedInstance.enemies.Where(x=>x.gameObject.activeInHierarchy&&x.moveComponent!=null&&!x.moveComponent.knockbackImmune).Select(x=>x.gameObject).FirstOrDefault())==null)yield return null;
  Log("fixture-enemy",original.name+" (native spawned, susceptible to knockback; stationary Forest trees are intentionally immune)");
  var target=DurableEnemy(original,player.transform.position+Vector3.right*.5f,false);
  if(mode=="gale")
  {
   Require(Mathf.Abs(skill.RealCooldown-8)<.001f && Mathf.Abs(skill.RealDuration-1)<.001f && Mathf.Abs(skill.RealRange-1.96f)<.001f,"base Gale cooldown/duration/range match serialized data and ARM64 constant");
   yield return SkillInput(skill);Require(skill.EffectRemaining>0 && PortedCharacterSkill.Timer(skill)>7,"native Skill starts Tempest Slash and eight-second cooldown");
   while(target.HP==10000)yield return null;Require((10000-target.HP)%4==0,"real contact ticks reduce enemy HP in source four-damage increments");
   yield return Capture("tempest-base.png");
   while(skill.EffectRemaining>0)yield return null;int stopped=target.HP;float cd=PortedCharacterSkill.Timer(skill);
   yield return SkillInput(skill);Require(skill.EffectRemaining==0 && PortedCharacterSkill.Timer(skill)<cd && target.HP==stopped,"native cooldown rejects early recast and ended skill stops damage");
   yield return ExclusiveChest(game,skill);
   Require(skill.Hurricane&&skill.Neural&&skill.Ultimate,"all three native deep-copied exclusive effects reach this run's Gale passive");
   Require(Mathf.Abs(skill.RealCooldown-7.45f)<.001f&&Mathf.Abs(skill.RealDuration-1.05f)<.001f&&Mathf.Abs(skill.RealRange-3.5f)<.001f,"exclusive stat bonuses compose with cooldown/duration/range scaling");
   player.stats[StatType.ReloadRate].AddMultiplierBonus(2);player.stats[StatType.MoveSpeed].AddMultiplierBonus(2);player.stats[StatType.FireRate].AddMultiplierBonus(2);player.stats[StatType.BulletDamage].AddMultiplierBonus(.5f);
   Require(Mathf.Abs(skill.RealCooldown-2.5f)<.001f&&Mathf.Abs(skill.RealDuration-1.5f)<.001f&&Mathf.Abs(skill.RealRange-8)<.001f,"Gale scaling caps at the evidenced 100 percent stat bonus");
   target.transform.position=player.transform.position+Vector3.right*5;int before=target.HP;PortedCharacterSkill.Timer(skill)=0;
   yield return SkillInput(skill);Require(Mathf.Abs(abilityCollider.radius-8)<.001f && PortedCharacterSkill.Timer(skill)>2,"native recast uses newly computed radius and cooldown immediately");
   while(target.HP==before)yield return null;Require((before-target.HP)%6==0,"real expanded-range contact receives six damage with native 50 percent bullet bonus");
   yield return Capture("tempest-upgraded.png");while(skill.EffectRemaining>0)yield return null;
   yield return AndroidPick(game,"AeroMastery",game.playerXP.level);
   Require(player.GetComponent<AndroidWind>().Damage>60,"native Gale character can acquire the reconstructed wind tree with its independent movement-scaled damage");
   // At this range a fresh speed-three Gale cannot reach the target during
   // the slash. This measures the character's separate Bullet Damage path.
   target.transform.position=player.transform.position+Vector3.right*7.5f;before=target.HP;PortedCharacterSkill.Timer(skill)=0;
   yield return SkillInput(skill);while(target.HP==before)yield return null;
   Require((before-target.HP)%6==0,"Aero Mastery's flat Gale damage does not leak into the character slash's six-damage Bullet-stat ticks");
   while(skill.EffectRemaining>0)yield return null;
  }
  else
  {
   yield return SkillInput(skill);Require(skill.ChargeMoving && skill.ChargeDamage==Mathf.FloorToInt(player.gun.damage*4),"native Bulwark input starts source four-times-bullet charge");
   Require(player.disableAction.value&&player.disableAnimation.value&&player.playerHealth.isInvincible.flips==ownInvincibility+1,"charge owns one native invincibility/action/animation toggle");
   while(target.HP==10000)yield return null;Require(target.HP==10000-skill.ChargeDamage,"real charge collision damages target once");
   var knocked=target.GetComponent<MoveComponent2D>();Log("knockback-state",target.name+" move="+(knocked!=null)+" immune="+knocked?.knockbackImmune+" vector="+knocked?.vector+" drag="+knocked?.drag+" source="+contract.skill.baseKnockback);
   Require(knocked!=null&&!knocked.knockbackImmune&&Mathf.Abs(knocked.vector.magnitude-15)<.01f,"source charge writes fifteen-unit native knockback to a susceptible enemy");
   Vector3 priorPosition=target.transform.position;while(Vector3.Distance(priorPosition,target.transform.position)<.1f)yield return null;knocked.vector=Vector2.zero;
   Require(Vector3.Distance(priorPosition,target.transform.position)>=.1f,"native enemy movement actually follows the charge knockback vector");
   yield return Capture("bulwark-base.png");while(skill.ChargeMoving)yield return null;
   Require(!player.disableAction.value&&!player.disableAnimation.value&&player.playerHealth.isInvincible.flips==ownInvincibility+1,"movement restores before source grace invincibility ends");
   while(skill.Charging)yield return null;Require(player.playerHealth.isInvincible.flips==ownInvincibility&&skill.NextShotBuffed,"grace ends without disturbing external invincibility and arms next-shot buff");
   yield return CheckBulwarkShot(game,player,skill);
   yield return ExclusiveChest(game,skill);Require(skill.Perpetual&&skill.Steel,"all native Bulwark exclusive effects reach this run's passive");
   Require(Mathf.Abs(player.gun.damage-20*1.35f)<.01f&&Mathf.Abs(player.stats[StatType.ProjectileSize].Modify(1)-1.35f)<.001f,"Barrel Reinforcement applies native damage and size stats");
   InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.D));while(player.moveVec.x<.99f)yield return null;
   player.SetPosition(Vector3.zero);
   var targets=Enumerable.Range(0,4).Select(i=>DurableEnemy(original,player.transform.position+Vector3.right*(.45f+i*.9f),true)).ToArray();
   PortedCharacterSkill.Timer(skill)=0;yield return SkillInput(skill);
   Require(skill.ChargeDamage==Mathf.FloorToInt(player.gun.damage*5)&&Mathf.Abs(player.moveSpeedMultiplier-3.5f)<.001f,"Steel Acceleration increases charge damage/speed by the recovered additions");
   string lastMovement=null;
   while(skill.ChargeMoving)
   {
    string movement=player.moveVec+" multiplier="+player.moveSpeedMultiplier+" disabled="+player.disableMove.value;
    if(movement!=lastMovement){Log("charge-movement",movement+" position="+player.transform.position+" keyD="+fixtureKeyboard.dKey.isPressed);lastMovement=movement;}
    yield return null;
   }
   InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());
   Log("charge-target-results",string.Join(";",targets.Select(x=>x.gameObject.GetInstanceID()+":"+x.HP+":"+x.transform.position))+" player="+player.transform.position);
   Require(targets.All(x=>x.HP==10000-skill.ChargeDamage),"upgraded native charge contacts each of four targets exactly once");
   Require(Mathf.Abs(skill.ReducedCooldown-3)<.001f,"Perpetual Fighter caps native cooldown reduction at three seconds");
   while(skill.Charging)yield return null;yield return CheckBulwarkShot(game,player,skill);
   yield return Capture("bulwark-upgraded.png");
  }
  // Leave during an active ability to exercise cancellation ownership and scene
  // destruction, then run an original character through its original shooter.
  PortedCharacterSkill.Timer(skill)=0;
  var pad=InputSystem.AddDevice<Gamepad>("RevampedCharacterGamepad");game.playerInput.SwitchCurrentControlScheme("Gamepad",pad);
  InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.LeftShoulder));yield return null;
  InputSystem.QueueStateEvent(pad,new GamepadState());yield return null;
  Require(mode=="gale"?skill.EffectRemaining>0:skill.ChargeMoving,"native controller Skill binding activates this form through synthetic device input");
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);InputSystem.RemoveDevice(pad);
  var previous=skill;int slot=Characters.Slots[contract.key];SaveSystem.data.characterHighestClear[slot]=mode=="gale"?2:3;SaveSystem.Save();
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.giveupButton);
  while(!(game.CurrentState is PlayerDeadState)||!game.quitToTitleButton.gameObject.activeInHierarchy||!game.quitToTitleButton.IsInteractable())yield return null;Click(game.quitToTitleButton);
  TitleScreenController title;while(SceneManager.GetActiveScene().name!="TitleScreen"||(title=Object.FindObjectOfType<TitleScreenController>())==null||!Active(title.mainMenu))yield return null;
  Require(previous==null&&player==null,"native scene exit destroys active passive and its subscriptions/effects");Require(Characters.Items(title.characterMenu).Count==15,"return to title has no duplicate character slots");
  Require(SaveSystem.data.characterHighestClear[slot]==(mode=="gale"?2:3),"new character clear index survives native save and scene reload");
  Click(Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu))yield return null;Click(Entries(title.characterMenu)[0].button);
  while(!Active(title.gunMenu))yield return null;Click(Entries(title.gunMenu)[0].button);while(!(title.CurrentState is ModeSelectState))yield return null;Click(title.modeSelectStartButton);while(!(title.CurrentState is MapSelectState))yield return null;Click(title.mapSelectStartButton);
  while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
  Require(PlayerController.Instance.GetComponentInChildren<PortedCharacterSkill>()==null && !PlayerController.Instance.disableAction.value,"original Shana run has no carried character skill or action lock");yield return CheckRevolver(game,PlayerController.Instance);
 }
 IEnumerator CheckBulwarkShot(GameController game,PlayerController player,PortedCharacterSkill skill)
 {
  float damage=player.gun.GetProjectileRecipe().damage;LastRecipeDamage=0;int before=Projectiles;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=(Vector2)Camera.main.WorldToScreenPoint(player.transform.position+Vector3.up*8)}.WithButton(MouseButton.Left));
  while(Projectiles==before)yield return null;InputSystem.QueueStateEvent(fixtureMouse,new MouseState());
  Require(Mathf.Abs(LastRecipeDamage-damage*3)<.01f && !skill.NextShotBuffed,"one actual native shot consumes the 200-percent additive buff for three-times damage");
  while(!(player.CurrentState is flanne.Player.IdleState))yield return null;
 }
 public static float LastRecipeDamage;
}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class CharacterRecipeProbe
{static void Prefix(ProjectileRecipe recipe){RuntimeChecks.LastRecipeDamage=recipe.damage;}}
