using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// The native Score has no field for Android's difficultyScore. Preserve its
// native UI/currency ABI while carrying that fourth source term on this result.
sealed class AndroidScore:Score
{internal int Difficulty;}
[HarmonyPatch(typeof(ScoreCalculator),nameof(ScoreCalculator.GetScore))]
class AndroidScoreCalculation
{
 static void Postfix(ref Score __result)
 {
  var value=__result;
  __result=new AndroidScore{
   timeSurvivedString=value.timeSurvivedString,timeSurvivedScore=value.timeSurvivedScore,
   enemiesKilled=value.enemiesKilled,enemiesKilledScore=value.enemiesKilledScore,
   levelsEarned=value.levelsEarned,levelsEarnedScore=value.levelsEarnedScore,
   Difficulty=Mathf.CeilToInt((value.timeSurvivedScore+value.enemiesKilledScore)*.1f*Loadout.difficultyLevel)
  };
 }
}
[HarmonyPatch(typeof(Score),"get_totalScore")]
class AndroidScoreTotal
{
 static void Postfix(Score __instance,ref int __result)
 {if(__instance is AndroidScore value)__result+=value.Difficulty;}
}
[HarmonyPatch(typeof(ScoreCalculator),"OnDeath")]
class AndroidScoreDeath
{
 static bool Prefix(object sender,ref int ____enemiesKilled)
 {
  // Source resolves the Health sender, but does not filter its GameObject tag.
  _=(sender as Health).gameObject;____enemiesKilled++;return false;
 }
}
