using HarmonyLib;
using flanne;
namespace TwentyMinutes.Modernization;

// Source ModifyHordeSpawner (0x196c114) only stores its argument. The damage
// addition is in a non-virtual Update on this ScriptableObject, which does not
// receive Unity's player-loop Update message. Original executable references,
// metadata and serialized bindings supply no caller. Do not keep the Windows
// WaitToIncreaseDamage coroutine or invent a MonoBehaviour to run that method.
[HarmonyPatch(typeof(EnemyDamageModifierAfterTime),nameof(EnemyDamageModifierAfterTime.ModifyHordeSpawner))]
class AndroidTimedEnemyDamage
{static bool Prefix()=>false;}

// Android D15 assigns its serialized0.55 multiplier. Windows instead adds
// its0.8 value to the existing multiplier, producing1.8 on a fresh boss spawner.
[HarmonyPatch(typeof(BossCDRModifier),nameof(BossCDRModifier.ModifyBossSpawner))]
class AndroidDarknessBossCooldown
{
 internal const float SourceReduction=.55f;
 static bool Prefix(BossSpawner bossSpawner)
 {bossSpawner.cooldownRate=SourceReduction;return false;}
}
