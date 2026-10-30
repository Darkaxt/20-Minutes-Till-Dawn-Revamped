using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool ObserveCullingPopups;
 internal static readonly List<string> CullingPopups=new List<string>();
 IEnumerator CheckCullingPopup()
 {
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return AndroidPick(game,"Culling",game.playerXP.level);
  ControlSummonHurtRolls=true;CullingMinimum=0;CullingMaximum=.15f;ObserveCullingPopups=true;bool numbers=Plugin.Instance.DamageNumbers.Value;
  try
  {
   Plugin.Instance.DamageNumbers.Value=true;var target=HitEffectTarget(maximum:2000000);target.transform.position=PlayerController.Instance.transform.position+Vector3.right*2;
   CullingPopups.Clear();target.TakeDamage(DamageType.Summon,1);
   Require(target.HP==1000000&&!target.isDead,"Culling preserves signed999999 damage and does not force an AutoKill");
   Require(CullingPopups.SequenceEqual(new[]{"1","Culled!"}),"only the Culling hit replaces its numeric popup");
   yield return new WaitForEndOfFrame();Require(Resources.FindObjectsOfTypeAll<TextMeshPro>().Any(t=>t.gameObject.activeInHierarchy&&t.text=="Culled!"),"actual pooled world popup renders Culled!");yield return PoiCapture("culling-popup.png");ReturnFixtureActor(target.gameObject);
   target=HitEffectTarget(maximum:2000000);CullingPopups.Clear();target.TakeDamage(DamageType.Bullet,999999);
   Require(target.HP==1000001&&CullingPopups.SequenceEqual(new[]{"999999"}),"unrelated identical damage remains numeric after Culling");ReturnFixtureActor(target.gameObject);
   target=HitEffectTarget();CullingPopups.Clear();target.TakeDamage(DamageType.Summon,1);Require(target.isDead&&CullingPopups.SequenceEqual(new[]{"1","Culled!"}),"lethal Culling keeps its ordinary damage/death path and label");
   Plugin.Instance.DamageNumbers.Value=false;target=HitEffectTarget();CullingPopups.Clear();target.TakeDamage(DamageType.Summon,1);Require(target.isDead&&CullingPopups.Count==0,"Damage numbers Off hides Culling feedback without changing execution");
  }
  finally{Plugin.Instance.DamageNumbers.Value=numbers;ObserveCullingPopups=false;ControlSummonHurtRolls=false;}
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Culling label, actual native pooled render, unchanged damage/death, ordinary999999 feedback and setting Off verified.");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(AndroidDamagePopup),"Pop")]
class RecordCullingPopup
{static void Postfix(string text){if(RuntimeChecks.ObserveCullingPopups)RuntimeChecks.CullingPopups.Add(text);}}
