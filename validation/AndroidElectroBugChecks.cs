using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool ElectroBugMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-electro-bug-");
 internal sealed class BugAttack{internal int Owner;internal bool Success;internal float Before,Delta,After,Cooldown;internal readonly List<int> Targets=new List<int>(),Damage=new List<int>();}
 internal static readonly List<BugAttack> BugAttacks=new List<BugAttack>();
 internal static BugAttack BugCurrent;
 internal static float BugUpdateBefore,BugUpdateDelta;
 static ThunderAttackSummon[] Bugs()=>PlayerController.Instance.GetComponentsInChildren<ThunderAttackSummon>();
 static float BugCooldown(ThunderAttackSummon bug)=>(float)AccessTools.PropertyGetter(typeof(AttackingSummon),"finalAttackCooldown").Invoke(bug,null);
 void BugPark(ThunderAttackSummon bug,int index)
 {
  bug.enabled=false;foreach(var orbital in bug.GetComponents<Orbital>())orbital.enabled=false;
  bug.transform.position=PlayerController.Instance.transform.position+Vector3.right*(25+index*5);
 }
 IEnumerator BugAttempt(ThunderAttackSummon bug,Health target,int raw)
 {
  int count=BugAttacks.Count,hp=target!=null?target.HP:0;Plugin.SetField(bug,"_timer",0f);bug.enabled=true;
  while(BugAttacks.Count==count)yield return null;bug.enabled=false;
  Require(BugAttacks.Count==count+1,"one actual Update deadline performs one Electro Bug attack");var edge=BugAttacks.Last();
  CombatClose(edge.After,edge.Before+edge.Delta-edge.Cooldown,"Electro Bug consumes the source interval before its success/failure result");
  Require(edge.Owner==bug.GetInstanceID()&&edge.Success==(target!=null),"actual Electro Bug attack returns the final target's success state");
  if(target==null)Require(edge.Targets.Count==0&&RecoveryBolts().Length==0,"failed real Electro Bug attack consumes its interval without lightning");
  else
  {
   Require(edge.Targets.SequenceEqual(new[]{target.gameObject.GetInstanceID(),target.gameObject.GetInstanceID()})&&edge.Damage.SequenceEqual(new[]{raw,raw}),"two actual Electro Bug strikes use player-centered strict source targets and current summon damage");
   while(target.HP==hp)yield return null;yield return RecoveryWaitBolts();
   Require(hp-target.HP==2*(raw+12),"two physical Electro Bug lightning contacts use source Mastery flat12 and Bullet damage");
  }
  Log("electro-bug-attack","owner="+edge.Owner+" success="+edge.Success+" cooldown="+edge.Cooldown+" timer="+edge.Before+"+"+edge.Delta+"->"+edge.After+" raw="+string.Join(",",edge.Damage)+" damage="+(target!=null?hp-target.HP:0));
 }
 IEnumerator CheckAndroidElectroBug(bool continuation)
 {
  GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
   foreach(string name in new[]{"ElectroBug","ElectroBug","ElectroMastery","Frenzy","Frenzy"})
   {Plugin.SetField(game.playerXP,"<level>k__BackingField",1);Plugin.SetField(game.playerXP,"xp",0f);yield return AndroidPick(game,name,game.playerXP.level);}
  }
  var p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();p.SetPosition(new Vector3(100,100,0));var bugs=Bugs();
  while(bugs.Any(b=>WindField(b,"TGen")==null))yield return null;
  Require(bugs.Length==2&&bugs.All(b=>ReferenceEquals(WindField(b,"TGen"),ThunderGenerator.SharedInstance)&&b.GetComponent<AndroidSummonPulse>()==null),"native repeated acquisition/source replay owns two initialized Electro Bugs without the source-omitted base pulse");
  for(int i=0;i<bugs.Length;i++)BugPark(bugs[i],i);
  foreach(var actor in p.GetComponentsInChildren<StabbingSummon>()){actor.enabled=false;foreach(var collider in actor.GetComponentsInChildren<Collider2D>())collider.enabled=false;}
  Log("fixture-precondition","Owned Endless access/no runes, native prerequisites/repeats, stopped scheduler/clock, source-pooled stationary targets. Frenzy's actual Scythe prerequisite is parked only in this fixture; Electro Bug orbit is parked far from player to distinguish query origin. Temporary summon-stat operand is removed before save. No natural encounter/frequency/full-run claim.");
  CombatClose(Bonus(p.stats[StatType.SummonAttackSpeed]),.6f,"two actual Frenzy acquisitions/source replay grant60% global summon attack speed");
  Require(ThunderGenerator.SharedInstance.damageMod.FlatBonus==12,"actual Electro Mastery restores flat12 at the shared lightning generator");
  foreach(var bug in bugs)
  {
   Require(bug.SummonTypeID=="ElectroBug"&&(int)WindField(bug,"baseDamage")==22&&(int)WindField(bug,"thundersPerAttack")==2&&(Vector2)WindField(bug,"range")==new Vector2(9,5)&&bug.transform.parent==p.transform,"source Electro Bug serialized damage/count/range and native attachment ownership are retained");
   CombatClose(BugCooldown(bug),3/1.6f,"actual Electro Bug cooldown uses source global inverse stat arithmetic");
  }
  if(continuation)Require(bugs.All(b=>(float)WindField(b,"_timer")<.25f),"source Continue omits both saved Electro Bug partial attack clocks");
  BugAttacks.Clear();yield return RecoveryWaitBolts();yield return BugAttempt(bugs[0],null,22);
  var target=RecoveryEnemy(new Vector3(4,4));var xBoundary=RecoveryEnemy(new Vector3(9,0));var yBoundary=RecoveryEnemy(new Vector3(0,5));
  yield return BugAttempt(bugs[0],target,22);
  p.stats[StatType.SummonDamage].AddMultiplierBonus(.5f);yield return BugAttempt(bugs[1],target,33);p.stats[StatType.SummonDamage].AddMultiplierBonus(-.5f);
  Require(xBoundary.HP==100000&&yBoundary.HP==100000,"strict source9/5 boundaries exclude both boundary targets from query/contact");
  foreach(var victim in new[]{target,xBoundary,yBoundary})yield return StatusReturn(victim);
  if(!continuation)
  {
   Plugin.SetField(bugs[0],"_timer",.73f);Plugin.SetField(bugs[1],"_timer",1.21f);
   Log("electro-bug-save","two actual acquired actors; explicitly seeded partial attack clocks0.73/1.21, no saved world target");
   yield return DragonSuspend(game,"Electro Bug native repeated acquisition, independent real Update cadence/failure consumption, strict player-centered targets, actual double lightning and live summon damage, source partial-clock omission checkpoint");yield break;
  }
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();
  Require(Bugs().Length==0&&ThunderGenerator.SharedInstance.damageMod.FlatBonus==0,"ordinary native restart removes both Electro Bug owners and source Mastery contribution");CombatClose(Bonus(p.stats[StatType.SummonAttackSpeed]),0,"ordinary restart clears both Frenzy contributions");
  int previous=BugAttacks.Count;yield return CheckRevolver(game,p,"android-electro-bug-original-restart.png");Require(BugAttacks.Count==previous&&RecoveryBolts().Length==0,"real original gun input after restart produces no old Electro Bug attack/lightning");
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Electro Bug complete native acquisition, source actual cadence/targeting/physical lightning/live stats, exact current Continue and ordinary restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(AttackingSummon),"Update")]
class ElectroBugUpdateProbe
{[HarmonyPriority(Priority.First)]static void Prefix(AttackingSummon __instance){if(RuntimeChecks.ElectroBugMode&&__instance is ThunderAttackSummon){RuntimeChecks.BugUpdateBefore=(float)AccessTools.Field(typeof(AttackingSummon),"_timer").GetValue(__instance);RuntimeChecks.BugUpdateDelta=Time.deltaTime;}}}
[HarmonyPatch(typeof(ThunderAttackSummon),"Attack")]
class ElectroBugAttackProbe
{
 [HarmonyPriority(Priority.First)]static void Prefix(ThunderAttackSummon __instance){if(RuntimeChecks.ElectroBugMode)RuntimeChecks.BugCurrent=new RuntimeChecks.BugAttack{Owner=__instance.GetInstanceID(),Before=RuntimeChecks.BugUpdateBefore,Delta=RuntimeChecks.BugUpdateDelta,Cooldown=(float)AccessTools.PropertyGetter(typeof(AttackingSummon),"finalAttackCooldown").Invoke(__instance,null)};}
 static void Postfix(ThunderAttackSummon __instance,bool __result){var edge=RuntimeChecks.BugCurrent;if(edge==null)return;edge.Success=__result;edge.After=(float)AccessTools.Field(typeof(AttackingSummon),"_timer").GetValue(__instance);RuntimeChecks.BugAttacks.Add(edge);RuntimeChecks.BugCurrent=null;}
}
[HarmonyPatch(typeof(ThunderGenerator),nameof(ThunderGenerator.GenerateAt),new[]{typeof(GameObject),typeof(int)})]
class ElectroBugStrikeProbe
{static void Prefix(GameObject target,int damage){var edge=RuntimeChecks.BugCurrent;if(edge!=null){edge.Targets.Add(target!=null?target.GetInstanceID():0);edge.Damage.Add(damage);}}}
