using HarmonyLib;
namespace TwentyMinutes.Modernization;

// Complete Android MultValueModifier.Modify body (0x19272b4). Constructors,
// AddValueModifier and ClampValueModifier retain their matching native bodies.
// Android composes against the running value; Windows instead adds a factor
// of the original input. Keep the shared evaluator source-shaped for every
// consumer, rather than compensating in individual status/tome callbacks.
[HarmonyPatch(typeof(MultValueModifier),nameof(MultValueModifier.Modify))]
class AndroidValueMultiplication
{
 static bool Prefix(float __1,float ___toMultiply,ref float __result)
 {__result=___toMultiply*__1;return false;}
}
