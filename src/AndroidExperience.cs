using HarmonyLib;
using flanne.Player;
namespace TwentyMinutes.Modernization;

// Source PlayerXP.get_xpToLevel and GainXP use these same two inclusive tier
// boundaries. The >=60 quadratic branch belongs only to the Windows build;
// Android continues the linear final tier. GainXP still advances once per call.
[HarmonyPatch(typeof(PlayerXP),"get_xpToLevel")]
class AndroidExperience
{
 static bool Prefix(PlayerXP __instance,ref float __result)
 {int next=__instance.level+1;__result=next<21?next*10-5:next<41?next*13-6:next*16-8;return false;}
}
