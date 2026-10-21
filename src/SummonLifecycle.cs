using HarmonyLib;
using flanne;
namespace TwentyMinutes.Modernization;

// Unused pool members are destroyed without ever running Summon.Start/Init.
// They never subscribed. Initialized members retain the native unsubscription
// against their original stat object, rather than looking up a later player.
[HarmonyPatch(typeof(SeekerWeapon),"OnDestroy")]
class UninitializedSeekerCleanup
{
 static readonly AccessTools.FieldRef<Summon,StatsHolder> Stats=AccessTools.FieldRefAccess<Summon,StatsHolder>("stats");
 static bool Prefix(SeekerWeapon __instance)=>!object.ReferenceEquals(Stats(__instance),null);
}
