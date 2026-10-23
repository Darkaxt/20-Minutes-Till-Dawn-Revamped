using System;
using System.IO;
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
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 static float ShootingCooldown(ShootingSummon summon)=>(float)AccessTools.Property(typeof(AttackingSummon),"finalAttackCooldown").GetValue(summon);
 static ProjectileRecipe ShootingRecipe(ShootingSummon summon)=>(ProjectileRecipe)AccessTools.Method(typeof(ShootingSummon),"GetProjectileRecipe").Invoke(summon,null);
 static bool ShootingAttack(ShootingSummon summon)=>(bool)AccessTools.Method(typeof(ShootingSummon),"Attack").Invoke(summon,null);
 IEnumerator CheckAndroidShooting(bool continuation)
 {
  GameController game;PlayerController player;
  if(!continuation)
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();yield return OpenCustomConfiguration();
   var map=CustomMode.Maps().Single(m=>m.name=="Endless_Forest");Click(CustomButton("CustomLoadDefaults"));Click(CustomButton("CustomAction"+Array.IndexOf(CustomMode.Maps(),map)));
   Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>();Click(CustomButton("CustomStart"));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
   player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;player.SetPosition(new Vector3(100,100,0));GameTimer.SharedInstance.Stop();
   yield return AndroidPick(game,"GhostFriend",game.playerXP.level);
  }
  else
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;
   Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))
   {var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));if(error!=null)throw new InvalidOperationException(error.text);yield return null;}
   player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;GameTimer.SharedInstance.Stop();
  }
  var summon=player.GetComponentsInChildren<ShootingSummon>().Single(s=>s.SummonTypeID=="GhostFriend");while(!SummonStarted(summon))yield return null;summon.enabled=false;
  if(continuation)
  {
   Require(StartedSummonTimers[summon.GetInstanceID()]==0,"source-unsaved shooting interval starts fresh before its first Update");
   Require(Mathf.Abs(Bonus(summon.attackSpeedMod))<.001f&&CustomMode.Get("summon:GhostFriend:damage")==2&&CustomMode.Get("summon:GhostFriend:speed")==1.5f,"Continue rebuilds the source-unsaved manual actor bonus while retaining actual Custom controls");
   Require(Mathf.Abs(ShootingRecipe(summon).damage-16f)<.001f,"Continue rebuilds unattributed manual stat mutations and retains the source recipe with the recorded Custom factor");
  }
  else
  {
   Plugin.SetField(summon,"_timer",1.5f);float delta=Time.deltaTime,cd=ShootingCooldown(summon);AccessTools.Method(typeof(AttackingSummon),"Update").Invoke(summon,null);
   Require(Mathf.Abs((float)WindField(summon,"_timer")-(1.5f+delta-cd))<.001f,"source shooting summon consumes one interval even when no target is available");
   player.stats[StatType.SummonAttackSpeed].AddMultiplierBonus(100);Require(Mathf.Abs(ShootingCooldown(summon)-.2f)<.001f,"source shooting summon shares the0.2-second minimum");player.stats[StatType.SummonAttackSpeed].AddMultiplierBonus(-100);
   player.stats[StatType.SummonDamage].AddMultiplierBonus(.12f);Require(Mathf.Abs(ShootingRecipe(summon).damage-8.96f)<.001f,"source shooting recipe retains fractional transformed damage before projectile creation, including default Custom settings");
   summon.inheritPlayerDamage=true;Require(Mathf.Abs(ShootingRecipe(summon).damage-summon.summonDamageMod.Modify(player.gun.damage))<.001f,"source inherited-player-damage branch applies the summon stat without early producer rounding");summon.inheritPlayerDamage=false;
  }
  var pool=ObjectPooler.SharedInstance;var template=pool.itemDictionary.Values.First(i=>i.objectToPool.CompareTag("Enemy")&&i.objectToPool.GetComponent<Health>()!=null&&i.objectToPool.GetComponent<MoveComponent2D>()!=null).objectToPool;
  pool.AddObject("Validation_Shooting_Enemy",template,2);var targetActor=pool.GetPooledObject("Validation_Shooting_Enemy");var target=DurableEnemy(targetActor,player.transform.position+new Vector3(8,5),false);targetActor.tag="Untagged";targetActor.SetActive(true);{var fixedBody=target.GetComponent<Rigidbody2D>();if(fixedBody!=null)fixedBody.constraints=RigidbodyConstraints2D.FreezeAll;}
  var boundary=pool.GetPooledObject("Validation_Shooting_Enemy");DurableEnemy(boundary,player.transform.position+Vector3.right*9,false);boundary.GetComponent<CircleCollider2D>().radius=.8f;boundary.SetActive(true);
  var unpooled=DurableEnemy(template,player.transform.position+Vector3.right*2,true);unpooled.gameObject.SetActive(true);
  ShootingNativeProbe.Summon=summon;int shots=ShootingNativeProbe.Calls;Vector2 direction=(Vector2)(target.transform.position-summon.transform.position);
  var oldRandom=UnityEngine.Random.state;UnityEngine.Random.InitState(2371);UnityEngine.Random.Range(0,1);var selectedState=UnityEngine.Random.state;UnityEngine.Random.InitState(2371);
  Require(ShootingAttack(summon)&&ShootingNativeProbe.Calls==shots+1&&Vector2.Distance(ShootingNativeProbe.Direction,direction)<.001f&&ShootingNativeProbe.BeforeShoot.Equals(selectedState),"source shooting attack chooses once from strict pooled9x6 centers, excluding boundary centers and unpooled tagged actors");UnityEngine.Random.state=oldRandom;
  boundary.SetActive(false);unpooled.gameObject.SetActive(false);targetActor.SetActive(false);
  foreach(var projectile in Object.FindObjectsOfType<Projectile>())if(projectile.owner==summon.gameObject)projectile.gameObject.SetActive(false);
  targetActor.tag="Enemy";targetActor.SetActive(true);target.transform.position=summon.transform.position+Vector3.right*3;
  int hurt=0,last=0;ObserveDamage(target,amount=>{hurt++;last=amount;});
  Require(ShootingAttack(summon),"actual native summon attack creates a projectile for contact verification");
  while(hurt==0)yield return null;
  Log("shooting-contact","damage="+last+" recipe="+ShootingRecipe(summon).damage);
  Require(last==(continuation?15:8),"actual native projectile creation floors the source recipe once before contact and the saved signed health modifier");
  targetActor.SetActive(false);
  if(continuation)
  {
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();Require(!CustomMode.Active&&Object.FindObjectsOfType<ShootingSummon>().Length==0,"original restart clears source shooting summons and Custom controls");yield return CheckRevolver(game,PlayerController.Instance,"android-shooting-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Actual source shooting-summon attack, native projectile contact, independent interval/Custom state and original restart survive fresh-process Continue");Application.Quit(0);yield break;
  }
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  var screen=(Vector2)Camera.main.WorldToScreenPoint(player.transform.position+Vector3.left*3);InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=screen});while(Vector2.Distance(ShootingCursor.Instance.cursorPosition,screen)>.1f)yield return null;
  summon.targetMouse=true;shots=ShootingNativeProbe.Calls;Require(ShootingAttack(summon)&&ShootingNativeProbe.Calls==shots+1&&ShootingNativeProbe.Direction.x<0,"native mouse aiming remains available for the targetMouse consumer branch");summon.targetMouse=false;
  yield return NativeTomePick(game,false,"TomeOfMight");
  foreach(var projectile in Object.FindObjectsOfType<Projectile>())if(projectile.owner==summon.gameObject)projectile.gameObject.SetActive(false);
  targetActor.SetActive(true);target.transform.position=summon.transform.position+Vector3.right*3;hurt=0;Require(ShootingAttack(summon),"actual post-Might shooting summon creates its source recipe");while(hurt==0)yield return null;
  Log("shooting-contact","Might damage="+last+" recipe="+ShootingRecipe(summon).damage);Require(last==6&&Mathf.Abs(ShootingRecipe(summon).damage-8.96f)<.001f,"source signed Might health modifier applies after native projectile rounding, leaving the producer recipe unchanged");targetActor.SetActive(false);
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="CustomSettings"));Click(CustomButton("CustomTab4"));CustomValue(0,"2");CustomValue(1,"1.5");yield return Capture("android-shooting-native-custom-controls.png");Click(CustomButton("CustomBack"));Click(game.pauseResumeButton);
  summon.attackSpeedMod.AddMultiplierBonus(.25f);Require(Mathf.Abs(ShootingCooldown(summon)-.8f/1.25f/1.5f)<.001f,"source shooting interval composes independent speed with actual Custom controls");
  Plugin.SetField(summon,"_timer",.31f);PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Actual source shooting-summon target/interval/recipe, native projectile contact, preserved mouse input and native Custom controls saved");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(ShootingSummon),"Shoot")]
class ShootingNativeProbe
{
 internal static ShootingSummon Summon;internal static int Calls;internal static Vector2 Direction;internal static UnityEngine.Random.State BeforeShoot;
 static void Prefix(ShootingSummon __instance,Vector2 direction){if(__instance==Summon){Calls++;Direction=direction;BeforeShoot=UnityEngine.Random.state;}}
}
