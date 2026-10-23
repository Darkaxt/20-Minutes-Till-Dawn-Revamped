using HarmonyLib;
using flanne;
namespace TwentyMinutes.Modernization;

// Android Health.HPChange posts a typed notification only after a valid
// operation leaves the target alive. Windows' producer-only SummonOnHit event
// includes lethal/protected hits and misses other Summon-classified damage.
[HarmonyPatch(typeof(Health),nameof(Health.TakeDamage))]
class AndroidSurvivingHurt
{
 internal const string Event="Revamped.AndroidSurvivingHurt";
 static void Prefix(Health __instance,int damage,out bool __state)=>__state=AndroidHealthEvents.Source(__instance)==null&&!__instance.isDead&&!__instance.isInvincible.value&&damage>=0;
 static void Postfix(Health __instance,DamageType damageType,bool __state)
 {if(__state&&!__instance.isDead)__instance.PostNotification(Event,damageType);}
}
