using HarmonyLib;
using flanne;
using UnityEngine;
namespace TwentyMinutes.Modernization;

// A125 correction: the source asset's Update has no Unity message owner.
// Bind its original rule to this run's horde and game clock instead.
[HarmonyPatch(typeof(EnemyDamageModifierAfterTime),nameof(EnemyDamageModifierAfterTime.ModifyHordeSpawner))]
class AndroidTimedEnemyDamage
{
 static bool Prefix(HordeSpawner hordeSpawner,int ___additionalDamage,float ___timeRemainingToActivate)
 {hordeSpawner.gameObject.AddComponent<AndroidLateEnemyDamage>().Configure(hordeSpawner,___additionalDamage,___timeRemainingToActivate);return false;}
 internal static void RestoreOwner(HordeSpawner horde)
 {
  // Continue restores aggregate difficulty fields instead of replaying the
  // modifier list. Recreate this reached clock owner from that same original
  // D10 entry before saved fields replace the fresh native baseline.
  var initializer=UnityEngine.Object.FindObjectOfType<DifficultyInitializer>();
  var list=(DifficultyModList)AccessTools.Field(typeof(DifficultyInitializer),"modList").GetValue(initializer);
  ((EnemyDamageModifierAfterTime)list.mods[10]).ModifyHordeSpawner(horde);
 }
}
public sealed class AndroidLateEnemyDamage:MonoBehaviour
{
 HordeSpawner horde;int baseDamage,additional;float remaining;bool applied;
 internal void Configure(HordeSpawner owner,int damage,float timeRemaining){horde=owner;baseDamage=owner.enemyDamage;additional=damage;remaining=timeRemaining;}
 void Update(){if(!RunResume.Restoring)Sync();}
 internal void Sync()
 {
  if(applied||GameTimer.SharedInstance.timer<=GameTimer.SharedInstance.timeLimit-remaining)return;
  horde.enemyDamage=baseDamage+additional;applied=true;
 }
}

// Android D15 assigns its serialized0.55 multiplier. Windows instead adds
// its0.8 value to the existing multiplier, producing1.8 on a fresh boss spawner.
[HarmonyPatch(typeof(BossCDRModifier),nameof(BossCDRModifier.ModifyBossSpawner))]
class AndroidDarknessBossCooldown
{
 internal const float SourceReduction=.55f;
 static bool Prefix(BossSpawner bossSpawner)
 {bossSpawner.cooldownRate=SourceReduction;return false;}
}
