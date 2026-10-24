using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.PowerupSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool RecoveryMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-recovery-");
 internal static readonly HashSet<int> RecoveryHealRolls=new HashSet<int>(),RecoveryAmmoRolls=new HashSet<int>();
 internal static bool RecoveryHealing;internal static int RecoveryBurnCalls;
 internal static GameObject RecoveryBurnActor;
 internal static readonly List<DamageType> RecoveryHealKinds=new List<DamageType>();
 internal static readonly List<float> RecoveryBurnBeforeHeal=new List<float>();
 internal sealed class RecoveryRefund{internal int Owner,Before,After,GainCalls,SoundAt=-1;internal string Clip;}
 internal static RecoveryRefund RecoveryCurrentRefund;
 internal static readonly List<RecoveryRefund> RecoveryRefunds=new List<RecoveryRefund>();
 internal sealed class RecoveryStrike{internal string Source;internal GameObject Target;internal int Damage;}
 internal static string RecoveryStrikeSource;
 internal static readonly List<RecoveryStrike> RecoveryStrikes=new List<RecoveryStrike>();
 int recoveryHealEvents,recoveryThunderEvents;
 void RecoveryHealed()=>recoveryHealEvents++;
 void RecoveryThunder(object sender,object args)=>recoveryThunderEvents++;
 Health RecoveryEnemy(Vector3 offset)
 {var h=StatusEnemy();h.transform.position=PlayerController.Instance.transform.position+offset;Plugin.SetField(h.GetComponent<MoveComponent2D>(),"knockbackImmune",true);Physics2D.SyncTransforms();return h;}
 static GameObject[] RecoveryBolts()
 {
  var prefab=(GameObject)WindField(ThunderGenerator.SharedInstance,"thunderPrefab");
  return ObjectPooler.SharedInstance.pooledObjectsDictionary[prefab.name].Where(g=>g!=null&&g.activeSelf).ToArray();
 }
 IEnumerator RecoveryWaitBolts(){while(RecoveryBolts().Length>0)yield return null;}
 void RecoveryBurn(Health target,params AndroidHealOnBurn[] passing)
 {
  RecoveryHealRolls.Clear();foreach(var helper in passing)RecoveryHealRolls.Add(helper.GetInstanceID());RecoveryBurnActor=target.gameObject;
  BurnSystem.SharedInstance.Burn(target.gameObject,3);RecoveryHealRolls.Clear();RecoveryBurnActor=null;
 }
 IEnumerator RecoveryWarmth(PlayerController p)
 {
  var helpers=p.GetComponentsInChildren<AndroidHealOnBurn>();var target=RecoveryEnemy(Vector3.right*15);
  if(p.playerHealth.isInvincible.value)p.playerHealth.isInvincible.UnFlip();AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(p.playerHealth,new object[]{p.playerHealth.hp-1});
  Require(p.playerHealth.hp==1&&p.playerHealth.shp==0,"recovery fixture uses actual injured red HP and no soul hearts");
  RecoveryBurnCalls=0;RecoveryHealKinds.Clear();RecoveryBurnBeforeHeal.Clear();recoveryHealEvents=0;
  RecoveryBurn(target);Require(p.playerHealth.hp==1&&RecoveryBurnCalls==2&&RecoveryHealKinds.Count==0,"two Warmth helpers reject separately controlled rolls just above0.0005");
  RecoveryBurn(target,helpers[0]);Require(p.playerHealth.hp==2&&RecoveryBurnCalls==4&&recoveryHealEvents==1,"one passing actual burn-application callback heals one red heart");
  RecoveryBurn(target,helpers);Require(p.playerHealth.hp==4&&RecoveryBurnCalls==6&&recoveryHealEvents==3,"both independent Warmth callbacks heal through actual source Cure changes");
  RecoveryBurn(target,helpers[1]);Require(p.playerHealth.hp==4&&recoveryHealEvents==4&&RecoveryBurnCalls==8,"successful Warmth at full health retains the positive source heal callback");
  Require(RecoveryHealKinds.All(k=>k==DamageType.None)&&RecoveryBurnBeforeHeal.SequenceEqual(new[]{3f,6f,6f,9f}),"source Cure classification and all healing precede the current burn amount assignment");
  int hp=target.HP;while(target.HP==hp)yield return null;
  Require(hp-target.HP==12&&RecoveryBurnCalls==8&&recoveryHealEvents==4,"actual raw12 burn tick neither rerolls Warmth nor emits another heal");
  Log("recovery-warmth","callbacks="+RecoveryBurnCalls+" cure="+RecoveryHealKinds.Count+" old-burn="+string.Join(",",RecoveryBurnBeforeHeal)+" tick="+(hp-target.HP));yield return StatusReturn(target);
 }
 IEnumerator RecoveryXP(GameController game,int ammoBefore,int passing)
 {
  var p=PlayerController.Instance;var helpers=p.GetComponentsInChildren<AndroidAmmoOnXP>();RecoveryAmmoRolls.Clear();foreach(var helper in helpers.Take(passing))RecoveryAmmoRolls.Add(helper.GetInstanceID());
  p.ammo.Reload();p.ammo.UseAmmo(p.ammo.amount-ammoBefore);RecoveryRefunds.Clear();Plugin.SetField(game.playerXP,"xp",0f);float xp=(float)WindField(game.playerXP,"xp");
  yield return CombatXP();
  Require((float)WindField(game.playerXP,"xp")>xp&&RecoveryRefunds.Count==2,"real source SmallXP travel/UsePickup delivers one callback to each Recharge owner");
  Require(p.ammo.amount==Mathf.Min(p.ammo.max,ammoBefore+passing),"actual XP pickup refills only independently successful Recharge helpers");
  foreach(var edge in RecoveryRefunds)
  {
   bool success=RecoveryAmmoRolls.Contains(edge.Owner);
   Require(success?edge.GainCalls==1&&edge.SoundAt==edge.After&&edge.After==Mathf.Min(p.ammo.max,edge.Before+1)&&!string.IsNullOrEmpty(edge.Clip):edge.GainCalls==0&&edge.SoundAt==-1,"Recharge grants cached ammo before actual Energized audio, including capped/full-ammo success");
   Log("recovery-recharge","owner="+edge.Owner+" before="+edge.Before+" after="+edge.After+" gain="+edge.GainCalls+" sound-ammo="+edge.SoundAt+" clip="+edge.Clip);
  }
  RecoveryAmmoRolls.Clear();
 }
 IEnumerator RecoveryTomeAndMage(GameController game)
 {
  var p=PlayerController.Instance;var near=RecoveryEnemy(new Vector3(.8f,.8f));var boundary=RecoveryEnemy(new Vector3(2,.8f));var wide=RecoveryEnemy(new Vector3(3,1));var far=RecoveryEnemy(new Vector3(7,4));
  yield return RecoveryWaitBolts();RecoveryStrikes.Clear();int events=recoveryThunderEvents,hp=near.HP;
  p.ammo.Reload();Require(RecoveryStrikes.Count==0,"nonzero ammo reload does not invoke Tome of Electro's producer");p.ammo.UseAmmo(p.ammo.amount);
  Require(RecoveryStrikes.Count==2&&RecoveryStrikes.All(s=>s.Source=="tome"&&s.Target==near.gameObject&&s.Damage==22)&&near.HP==hp&&recoveryThunderEvents==events+2,"actual zero-ammo notification invokes both same-asset Tome listeners, strict2/2 source target query and pre-physics thunder notification");
  while(near.HP==hp)yield return null;yield return RecoveryWaitBolts();Require(hp-near.HP==68&&boundary.HP==100000&&wide.HP==100000&&far.HP==100000,"two actual Tome lightning contacts capture source Mastery damage34 each without an area strike or boundary target");
  Log("recovery-tome","callbacks="+RecoveryStrikes.Count+" source-damage=22 physical-total="+(hp-near.HP));
  foreach(var actor in new[]{near,boundary,wide,far})yield return StatusReturn(actor);
  RecoveryStrikes.Clear();events=recoveryThunderEvents;p.ammo.Reload();p.ammo.UseAmmo(p.ammo.amount);
  Require(RecoveryStrikes.Count==2&&RecoveryStrikes.All(s=>s.Source=="tome"&&s.Target==null)&&recoveryThunderEvents==events&&RecoveryBolts().Length==0,"empty source query still reaches nullable GenerateAt twice, whose source null branch emits no visual/notification");
  near=RecoveryEnemy(new Vector3(.8f,.8f));boundary=RecoveryEnemy(new Vector3(2,.8f));wide=RecoveryEnemy(new Vector3(3,1));far=RecoveryEnemy(new Vector3(7,4));
  var mage=p.GetComponentInChildren<ThunderOnAttack>();Require((int)WindField(mage,"_counter")==0,"Electro Mage begins with its source-unsaved counter at zero");RecoveryStrikes.Clear();
  yield return MothShots(game,1);Require(RecoveryStrikes.Count==0&&(int)WindField(mage,"_counter")==1,"first actual native shot retains one partial Electro Mage trigger");
  yield return MothShots(game,1);Require(RecoveryStrikes.Count==1&&RecoveryStrikes[0].Source=="mage"&&RecoveryStrikes[0].Damage==22&&RecoveryStrikes[0].Target!=far.gameObject&&(int)WindField(mage,"_counter")==0,"second actual native shot invokes one source7/4 pooled-target Electro Mage attack");
  var chosen=RecoveryStrikes[0].Target.GetComponent<Health>();while(chosen.HP==100000)yield return null;yield return RecoveryWaitBolts();Require(chosen.HP==99966&&far.HP==100000,"actual Electro Mage lightning contact uses Mastery before source Bullet damage34 and excludes strict7/4 boundary");
  foreach(var actor in new[]{near,boundary,wide,far})yield return StatusReturn(actor);
  yield return MothShots(game,1);Require((int)WindField(mage,"_counter")==1,"third real shot prepares one source-unsaved partial Electro Mage trigger");
 }
 IEnumerator CheckAndroidRecovery(bool continuation)
 {
  GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();
  }
  var p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();p.SetPosition(new Vector3(100,100,0));
  Log("fixture-precondition","No runes; controlled earned Endless access, level/XP below evolution, stopped source scheduling, stationary source-pooled NPC geometry and per-helper source RNG draws. Native chest/tome grant entry is seeded; actual menu Apply/XP travel/gun/ammo/physics/cleanup performs the behavior. No natural proc frequency/encounter or full-run claim.");
  if(!continuation)
  {
   foreach(string name in new[]{"SoothingWarmth","SoothingWarmth","Recharge","Recharge","ElectroMastery"})
   {Plugin.SetField(game.playerXP,"<level>k__BackingField",1);Plugin.SetField(game.playerXP,"xp",0f);yield return AndroidPick(game,name,game.playerXP.level);}
   foreach(var bug in p.GetComponentsInChildren<ThunderAttackSummon>())bug.enabled=false;
   var target=RecoveryEnemy(new Vector3(.8f,.8f));p.ammo.UseAmmo(p.ammo.amount);RecoveryStrikes.Clear();
   for(int i=0;i<2;i++)yield return NativeTomePick(game,false,"TomeOfElectro");
   Require(RecoveryStrikes.Count==0&&target.HP==100000,"both actual Tome pickups at zero ammo apply stats/listeners without an immediate grant strike");yield return StatusReturn(target);
  }
  else Require(RecoveryStrikes.Count==2&&RecoveryStrikes.All(s=>s.Source=="tome"&&s.Target==null),"source zero-ammo restoration notifies both already-applied Tome listeners before deferred helper Starts, without restoring enemy actors");
  foreach(var bug in p.GetComponentsInChildren<ThunderAttackSummon>())bug.enabled=false;
  Log("fixture-precondition","Electro Mastery's actual Electro Bug prerequisite is retained; its autonomous attack behaviour is disabled only in this fixture to isolate Tome/Electro Mage producers. No Electro Bug cadence acceptance is claimed.");
  foreach(var type in new[]{typeof(AndroidHealOnBurn),typeof(AndroidAmmoOnXP)})
  {while(p.GetComponentsInChildren(type).Cast<Component>().Any(h=>!StatusSubscribed(h)))yield return null;Require(p.GetComponentsInChildren(type).Length==2,"native acquisition/source replay owns exactly two "+type.Name+" helpers");}
  Require(p.GetComponentsInChildren<AndroidAmmoOnXP>().All(h=>ReferenceEquals(WindField(h,"ammo"),p.ammo)&&h.transform.localPosition==Vector3.zero),"Recharge Start caches the current parent ammo and resets its local position");
  CombatClose(Reduction(p.stats[StatType.BulletDamage]),.85f*.85f,"two Tome Electro stat recipes apply source15% Bullet Damage reduction factors");CombatClose(Bonus(p.stats[StatType.ReloadRate]),.5f,"two Tome Electro stat recipes apply source plus25% Reload Rate each");
  Require(ThunderGenerator.SharedInstance.damageMod.FlatBonus==12&&Mathf.Abs(ThunderGenerator.SharedInstance.sizeMultiplier-1.75f)<.001f,"actual Electro Mastery acquisition/source replay restores source flat12 and size1.75 once");
  this.AddObserver(RecoveryThunder,ThunderGenerator.ThunderHitEvent);p.playerHealth.onHealed.AddListener(RecoveryHealed);
  yield return RecoveryWarmth(p);yield return RecoveryXP(game,3,0);yield return RecoveryXP(game,3,1);yield return RecoveryXP(game,3,2);yield return RecoveryXP(game,5,2);yield return RecoveryTomeAndMage(game);
  if(!continuation)
  {
   if(p.playerHealth.isInvincible.value)p.playerHealth.isInvincible.UnFlip();AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(p.playerHealth,new object[]{3});var burning=RecoveryEnemy(Vector3.right*15);RecoveryBurn(burning);RecoveryBurn(burning,p.GetComponentsInChildren<AndroidHealOnBurn>()[0]);
   var near=RecoveryEnemy(new Vector3(.8f,.8f));p.ammo.Reload();p.ammo.UseAmmo(p.ammo.amount);Require(p.ammo.amount==0&&p.playerHealth.hp==2&&RecoveryBolts().Length==2&&BurnSystem.SharedInstance.IsBurning(burning.gameObject),"explicit checkpoint contains zero ammo, injured health, live burn/two lightning visuals and partial Electro Mage counter");
   yield return DragonSuspend(game,"Recovery and elemental callbacks: independent Warmth and Recharge source rolls/Cure/pre-burn ordering/real XP/audio, duplicate Tome events, actual Electro Mage two-shot/physical damage and zero-ammo active-world checkpoint");yield break;
  }
  p.playerHealth.onHealed.RemoveListener(RecoveryHealed);this.RemoveObserver(RecoveryThunder,ThunderGenerator.ThunderHitEvent);yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();
  Require(p.GetComponentsInChildren<AndroidHealOnBurn>().Length==0&&p.GetComponentsInChildren<AndroidAmmoOnXP>().Length==0&&p.GetComponentsInChildren<ThunderOnAttack>().Length==0,"ordinary restart removes all recovery and Electro Mage owners");RecoveryStrikes.Clear();p.ammo.UseAmmo(p.ammo.amount);Require(RecoveryStrikes.Count==0,"original restart ammo-zero producer has no old Tome callback");
  var target2=RecoveryEnemy(Vector3.right*15);int calls=RecoveryBurnCalls;RecoveryBurn(target2);Require(RecoveryBurnCalls==calls,"actual burn after restart reaches no old Warmth callback");yield return StatusReturn(target2);
  RecoveryRefunds.Clear();p.ammo.Reload();p.ammo.UseAmmo(1);Plugin.SetField(game.playerXP,"xp",0f);yield return CombatXP();Require(RecoveryRefunds.Count==0&&p.ammo.amount==p.ammo.max-1,"actual XP after restart reaches no old Recharge callback");
  yield return MothShots(game,2);Require(RecoveryStrikes.Count==0,"actual native gun shots after restart produce no old Electro Mage strike");yield return CheckRevolver(game,p,"android-recovery-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Full recovery/elemental native acquisition, controlled actual producers/physics/audio/order, zero-ammo active-world source Continue and ordinary restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(AndroidHealOnBurn),"Inflicted")]
class RecoveryWarmthRoll
{
 [HarmonyPriority(Priority.First)]static void Prefix(AndroidHealOnBurn __instance){if(!RuntimeChecks.RecoveryMode)return;RuntimeChecks.RecoveryBurnCalls++;RuntimeChecks.RecoveryHealing=true;bool pass=RuntimeChecks.RecoveryHealRolls.Contains(__instance.GetInstanceID());RuntimeChecks.SeedFirstRoll(pass?0:.0005f,pass?.0005f:.001f);}
 static void Postfix(){if(RuntimeChecks.RecoveryMode)RuntimeChecks.RecoveryHealing=false;}
}
[HarmonyPatch(typeof(AndroidPlayerCollision),"Change")]
class RecoveryCureProbe
{
 static void Prefix(int change,DamageType kind){if(!RuntimeChecks.RecoveryMode||!RuntimeChecks.RecoveryHealing)return;RuntimeChecks.RecoveryHealKinds.Add(kind);RuntimeChecks.RecoveryBurnBeforeHeal.Add(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(RuntimeChecks.RecoveryBurnActor).Damage);}
}
[HarmonyPatch(typeof(AndroidAmmoOnXP),"PickedUp")]
class RecoveryXPRoll
{
 [HarmonyPriority(Priority.First)]static void Prefix(AndroidAmmoOnXP __instance)
 {if(!RuntimeChecks.RecoveryMode)return;var edge=new RuntimeChecks.RecoveryRefund{Owner=__instance.GetInstanceID(),Before=PlayerController.Instance.ammo.amount,After=PlayerController.Instance.ammo.amount};RuntimeChecks.RecoveryCurrentRefund=edge;RuntimeChecks.RecoveryRefunds.Add(edge);bool pass=RuntimeChecks.RecoveryAmmoRolls.Contains(edge.Owner);RuntimeChecks.SeedFirstRoll(pass?0:.1f,pass?.1f:.2f);}
 static void Postfix(){if(RuntimeChecks.RecoveryMode)RuntimeChecks.RecoveryCurrentRefund=null;}
}
[HarmonyPatch(typeof(Ammo),nameof(Ammo.GainAmmo))]
class RecoveryAmmoGainProbe
{static void Postfix(Ammo __instance){var edge=RuntimeChecks.RecoveryCurrentRefund;if(edge!=null){edge.GainCalls++;edge.After=__instance.amount;}}}
[HarmonyPatch(typeof(SoundEffectSO),nameof(SoundEffectSO.Play))]
class RecoverySoundProbe
{static void Postfix(SoundEffectSO __instance,AudioSource __result){var edge=RuntimeChecks.RecoveryCurrentRefund;if(edge==null)return;if(__instance.name!="Energized_SFX"||__result==null||__result.clip==null)throw new InvalidOperationException("Recharge sound does not match original Energized playback");edge.SoundAt=PlayerController.Instance.ammo.amount;edge.Clip=__result.clip.name;}}
[HarmonyPatch(typeof(AndroidTomeElectro),"Changed")]
class RecoveryTomeScope
{[HarmonyPriority(Priority.First)]static void Prefix(out string __state){__state=RuntimeChecks.RecoveryStrikeSource;if(RuntimeChecks.RecoveryMode)RuntimeChecks.RecoveryStrikeSource="tome";}static void Postfix(string __state)=>RuntimeChecks.RecoveryStrikeSource=__state;}
[HarmonyPatch(typeof(ThunderOnAttack),nameof(ThunderOnAttack.Attack))]
class RecoveryMageScope
{[HarmonyPriority(Priority.First)]static void Prefix(out string __state){__state=RuntimeChecks.RecoveryStrikeSource;if(RuntimeChecks.RecoveryMode)RuntimeChecks.RecoveryStrikeSource="mage";}static void Postfix(string __state)=>RuntimeChecks.RecoveryStrikeSource=__state;}
[HarmonyPatch(typeof(ThunderGenerator),nameof(ThunderGenerator.GenerateAt),new[]{typeof(GameObject),typeof(int)})]
class RecoveryStrikeProbe
{static void Prefix(GameObject target,int damage){if(RuntimeChecks.RecoveryMode&&RuntimeChecks.RecoveryStrikeSource!=null)RuntimeChecks.RecoveryStrikes.Add(new RuntimeChecks.RecoveryStrike{Source=RuntimeChecks.RecoveryStrikeSource,Target=target,Damage=damage});}}
[HarmonyPatch(typeof(BurnOnHit),"OnImpact")]
class RecoveryBurnHitRoll
{[HarmonyPriority(Priority.First)]static void Prefix(){if(RuntimeChecks.RecoveryMode)RuntimeChecks.SeedFirstRoll(.5f,1);}}
