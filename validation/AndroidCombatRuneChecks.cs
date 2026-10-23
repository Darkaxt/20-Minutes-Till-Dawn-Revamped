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
 internal static bool CombatRuneCaptureShots;
 internal static readonly List<Vector3> CombatRuneShotRecipes=new List<Vector3>();
 static bool BarrageBlocked(ElementalBarrageRune rune)=>(bool)WindField(rune,"disableInflictGain");
 static int BarrageCount(ElementalBarrageRune rune)=>(int)WindField(rune,"inflictCounter");
 static void DedicationStats(bool active)
 {
  var p=PlayerController.Instance;Require((bool)WindField(p.GetComponentInChildren<DedicationRune>(),"active")==active,"source Dedication active flag follows the actual active summon count");
  CombatClose(Bonus(p.stats[StatType.SummonDamage]),active?.3f:0,"rank-three Dedication actual Summon Damage");CombatClose(Bonus(p.stats[StatType.SummonAttackSpeed]),active?.3f:0,"rank-three Dedication actual Summon Attack Speed");
 }
 IEnumerator CombatRuneShots(GameController game,int count)
 {
  var p=PlayerController.Instance;while(PauseController.isPaused||!(p.CurrentState is flanne.Player.IdleState))yield return null;
  p.ammo.Reload();game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.up*8);CombatRuneShotRecipes.Clear();CombatRuneCaptureShots=true;
  try
  {
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));
   while(CombatRuneShotRecipes.Count<count)yield return null;
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});while(!(p.CurrentState is flanne.Player.IdleState))yield return null;
  }
  finally{CombatRuneCaptureShots=false;}
  Require(CombatRuneShotRecipes.Count==count,"real native input produces the requested individual Revolver shots and completes the source release/reload flow");
  for(int i=0;i<count;i++)
  {
   var expected=i==2?new Vector3(32,1.6f,99):new Vector3(20,1,0);Require(Vector3.Distance(CombatRuneShotRecipes[i],expected)<.002f,"actual shot"+(i+1)+" recipe damage/size/piercing="+CombatRuneShotRecipes[i]+" expected="+expected);
  }
  Require(CombatCount(p.GetComponentInChildren<CadenceRune>(),"_counter")==count%3,"actual native shot events advance/reset Cadence's source counter");
  foreach(var projectile in Object.FindObjectsOfType<Projectile>())if(projectile.owner==p.gameObject||projectile.owner==p.gun.gameObject)ReturnFixtureActor(projectile.gameObject);
 }
 IEnumerator CombatRuneMomentum(MomentumRune rune)
 {
  var p=PlayerController.Instance;Require((float)WindField(rune,"duration")==1&&(float)WindField(rune,"moveSpeedBoostPerLevel")==.07f,"original Momentum one-second/seven-percent-per-rank fields reach the actual helper");
  yield return CombatXP();float first=Time.time;CombatClose(Bonus(p.stats[StatType.MoveSpeed]),.21f,"actual XP pickup activates rank-three Momentum21-percent movement");
  while(Time.time-first<.5f)yield return null;yield return CombatXP();while(Time.time-first<1.1f)yield return null;
  CombatClose(Bonus(p.stats[StatType.MoveSpeed]),.21f,"second real XP pickup refreshes Momentum beyond the original deadline without stacking");
  while((float)WindField(rune,"_timer")>0)yield return null;CombatClose(Bonus(p.stats[StatType.MoveSpeed]),0,"actual refreshed Momentum expiry removes one contribution");
 }
 void CombatRuneBarrage(ElementalBarrageRune rune,Health target,float fireBaseline)
 {
  Require(!BarrageBlocked(rune)&&BarrageCount(rune)==0,"actual source Barrage is ready with zero inflict count");
  for(int i=0;i<49;i++)BurnSystem.SharedInstance.Burn(target.gameObject,1);
  Require(BarrageCount(rune)==49&&!BarrageBlocked(rune),"49 real repeated burns advance the original source elemental threshold without activation");CombatClose(Bonus(PlayerController.Instance.stats[StatType.FireRate]),fireBaseline,"Barrage does not activate below fifty source inflicts");
  FreezeSystem.SharedInstance.Freeze(target.gameObject);Require(BarrageCount(rune)==0&&BarrageBlocked(rune),"actual Freeze event completes the shared fifty-inflict threshold and blocks new gain");
  CombatClose(Bonus(PlayerController.Instance.stats[StatType.FireRate]),fireBaseline+1,"source Barrage applies both50-percent contributions to Fire Rate");CombatClose(Bonus(PlayerController.Instance.stats[StatType.ReloadRate]),0,"source Barrage retains the shared missing Reload Rate bonus");
 }
 IEnumerator CombatRuneBarrageExpire(ElementalBarrageRune rune,float baseline,bool cooldown)
 {
  float started=Time.time,largestDelta=0;Time.timeScale=4;
  while(Bonus(PlayerController.Instance.stats[StatType.FireRate])>baseline+.01f){largestDelta=Mathf.Max(largestDelta,Time.deltaTime);yield return null;}
  float expired=Time.time;Time.timeScale=1;
  Require(expired-started>=3-.02f&&expired-started<=3+largestDelta*2+.02f,"rank-three Barrage's real bonus phase lasts three Unity seconds within measured frame quantization");
  Require(BarrageBlocked(rune),"actual Barrage expiry retains the subsequent cooldown block");CombatClose(Bonus(PlayerController.Instance.stats[StatType.FireRate]),baseline,"Barrage expiry removes both Fire Rate contributions exactly");
  if(!cooldown)yield break;
  Time.timeScale=4;while(BarrageBlocked(rune)){largestDelta=Mathf.Max(largestDelta,Time.deltaTime);yield return null;}Time.timeScale=1;
  Require(Time.time-expired>=10-.02f&&Time.time-expired<=10+largestDelta*2+.02f,"actual Barrage cooldown waits ten further Unity seconds after bonus expiry");
 }
 IEnumerator CheckAndroidCombatRunes(bool continuation)
 {
  bool repeated=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("-repeated-");GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();
   yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>
   {
    Loadout.RuneSelection=new[]{"ElementalBarrageRuneData","CadenceRuneData","DedicationRuneData","MomentumRuneData","TenacityRuneData"}.Select(name=>
    {var copy=Object.Instantiate(Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.name==name));copy.name=name;copy.level=3;return copy;}).ToList();
   });
   game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return AndroidPick(game,"GhostFriend",game.playerXP.level);
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));
  var ghosts=p.GetComponentsInChildren<ShootingSummon>().Where(s=>s.SummonTypeID=="GhostFriend").ToArray();Require(ghosts.Length==1,"actual acquired Ghost Friend is present through source reconstruction");while(ghosts.Any(g=>!SummonStarted(g)))yield return null;foreach(var ghost in ghosts)ghost.enabled=false;
  var cadence=p.GetComponentInChildren<CadenceRune>();var barrage=p.GetComponentInChildren<ElementalBarrageRune>();var momentum=p.GetComponentInChildren<MomentumRune>();var tenacity=p.GetComponentInChildren<HPThresholdStatRune>();yield return null;
  Require(cadence!=null&&barrage!=null&&momentum!=null&&tenacity!=null,"five explicitly selected native rank-three rune helpers are initialized");
  Require((int)WindField(barrage,"inflictsToActivate")==50&&(float)WindField(barrage,"secondsPerLevel")==1&&(float)WindField(barrage,"cooldown")==10,"actual Barrage retains original threshold/duration/cooldown fields");
  Log("fixture-precondition","Five explicit native rank-three rune copies, actual perk choices, native input/projectile recipes, pooled XP and real repeated burn/Freeze/health notifications. Encounter/game clock stopped; accelerated Unity time only for source3/10-second phases. No imposed rune counters, synthetic inflict publishers, purchase or natural encounter-frequency claim.");
  DedicationStats(!repeated);Require(!BarrageBlocked(barrage)&&BarrageCount(barrage)==0&&CombatCount(cadence,"_counter")==0&&(float)WindField(momentum,"_timer")==0,"fresh/source Continue resets Barrage active/cooldown/count, Cadence count and Momentum phase");
  if(continuation)
  {
   Require(p.playerHealth.hp==(repeated?4:2)&&(bool)WindField(tenacity,"statsActive")==!repeated,"source Continue recomputes Tenacity from actual restored health instead of importing its old flag");
   CombatClose(Bonus(p.stats[StatType.FireRate]),repeated?0:.09f,"continued Fire Rate retains only the recomputed Tenacity bonus");CombatClose(Bonus(p.stats[StatType.Knockback]),repeated?0:.15f,"continued Tenacity uses the original five-percent-per-rank knockback");
   CombatClose(Bonus(p.stats[StatType.BulletDamage]),0,"Continue omits Cadence's old prepared damage phase");CombatClose(Bonus(p.stats[StatType.ProjectileSize]),0,"Continue omits Cadence's old size phase");Require(p.stats[StatType.Piercing].FlatBonus==0,"Continue omits Cadence's old99-pierce phase");CombatClose(Bonus(p.stats[StatType.MoveSpeed]),0,"Continue omits the old active Momentum bonus");
   p.playerHealth.Heal(2);Require(!(bool)WindField(tenacity,"statsActive"),"real continued healing deactivates Tenacity above half health");CombatClose(Bonus(p.stats[StatType.FireRate]),0,"Tenacity actual healing removes its nine-percent Fire Rate");CombatClose(Bonus(p.stats[StatType.Knockback]),0,"Tenacity actual healing removes its fifteen-percent knockback");
  }
  else Require(!(bool)WindField(tenacity,"statsActive")&&p.playerHealth.hp==4,"fresh full-health Tenacity remains inactive");
  if(!repeated)
  {
   CombatClose(ShootingRecipe(ghosts[0]).damage,10.4f,"Dedication's live stat reaches the real Ghost projectile recipe");float baseCooldown=(float)WindField(ghosts[0],"attackCooldown");CombatClose(ShootingCooldown(ghosts[0]),Mathf.Max(.2f,p.stats[StatType.SummonAttackSpeed].ModifyInverse(baseCooldown)),"Dedication's live attack-speed stat reaches the actual Ghost cooldown");
  }
  yield return CombatRuneShots(game,3);yield return CombatRuneMomentum(momentum);
  var target=HitEffectTarget(maximum:1000000,tag:"Untagged");CombatRuneBarrage(barrage,target,0);
  for(int i=0;i<5;i++)BurnSystem.SharedInstance.Burn(target.gameObject,1);Require(BarrageCount(barrage)==0,"source Barrage ignores additional actual inflicts during the bonus phase");
  if(repeated)
  {
   Require(p.GetComponentsInChildren<StabbingSummon>().Length==2&&Object.FindObjectsOfType<Summon>().Length==3,"second source Continue recreates all three actors from the two acquired powers and keeps Dedication inactive");
   yield return CombatRuneBarrageExpire(barrage,0,false);ReturnFixtureActor(target.gameObject);
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
   Require(p.GetComponentsInChildren<Rune>().Length==0&&Object.FindObjectsOfType<Summon>().Length==0,"ordinary restart clears all five rune families and acquired summons");
   foreach(var type in new[]{StatType.FireRate,StatType.ReloadRate,StatType.BulletDamage,StatType.ProjectileSize,StatType.MoveSpeed,StatType.Knockback,StatType.SummonDamage,StatType.SummonAttackSpeed})CombatClose(Bonus(p.stats[type]),0,"ordinary restart clears rune "+type+" contributions");Require(p.stats[StatType.Piercing].FlatBonus==0,"ordinary restart clears prepared Cadence piercing");
   yield return CheckRevolver(game,p,"android-combat-runes-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Source repeated rune Continue resets cooldown/prepared phases, recomputes summon/health conditions, actual shots/XP/elemental producers and ordinary restart");Application.Quit(0);yield break;
  }
  yield return CombatRuneBarrageExpire(barrage,0,!continuation);
  if(continuation)
  {
   yield return AndroidPick(game,"MagicSpear",game.playerXP.level);while(p.GetComponentsInChildren<StabbingSummon>().Any(s=>!SummonStarted(s)))yield return null;
   Require(Object.FindObjectsOfType<Summon>().Length==3&&p.GetComponentsInChildren<StabbingSummon>().Length==2,"real native Magic Spear acquisition adds its original two actors, producing three summons and disabling Dedication");DedicationStats(false);
   Require(BarrageBlocked(barrage),"source cooldown remains active in the resaved three-summon run");
   yield return DragonSuspend(game,"Actual resumed rune phases/producers and one-to-three-summon Dedication deactivation, source cooldown checkpoint");yield break;
  }
  Require(!BarrageBlocked(barrage),"source Barrage finishes its full bonus and cooldown sequence before reactivation");
  yield return CombatRuneShots(game,2);Require(p.stats[StatType.Piercing].FlatBonus==99,"Cadence prepares the next third-shot bonus in the source checkpoint");CharacterHurt(2);Require(p.playerHealth.hp==2&&(bool)WindField(tenacity,"statsActive"),"actual red-health hits activate Tenacity at exactly half health");CombatClose(Bonus(p.stats[StatType.FireRate]),.09f,"rank-three Tenacity Fire Rate at half health");CombatClose(Bonus(p.stats[StatType.Knockback]),.15f,"rank-three Tenacity retains its shared knockback tooltip mismatch");
  yield return CombatXP();CombatClose(Bonus(p.stats[StatType.MoveSpeed]),.21f,"source checkpoint has actual active Momentum");CombatRuneBarrage(barrage,target,.09f);
  var source=ResumeRuntime.Capture(p);Require(source.stats.Count==0&&source.providers.Count==0&&source.timed.Count==0&&source.summons.Count==0&&source.sourceHealing==null&&source.sourceBodySoul==null,"source rune checkpoint omits generic phases and raw totals while retaining acquired content and live health");
  yield return DragonSuspend(game,"Actual native Cadence recipes, Dedication single summon, Momentum refresh, Barrage3/10-second sequence and mixed active rune source checkpoint");
 }
}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class ObserveCombatRuneProjectileRecipe
{
 static void Prefix(ProjectileRecipe __0)
 {if(RuntimeChecks.CombatRuneCaptureShots)RuntimeChecks.CombatRuneShotRecipes.Add(new Vector3(__0.damage,__0.size,__0.piercing));}
}
