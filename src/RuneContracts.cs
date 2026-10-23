using HarmonyLib;
using flanne.RuneSystem;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(GrowthRune),"Init")]
class GrowthExperienceBonus
{
 // Runtime clone only: keep the native XP modifier, heal-on-level callback,
 // rune level, spending and subscriptions. The current tooltip promises 4%.
 static void Prefix(ref float ___xpMultiplierPerLevel)=>___xpMultiplierPerLevel=.04f;
}
