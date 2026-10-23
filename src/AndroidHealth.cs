using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(PlayerController),"Awake")]
class AndroidSoulCapacityInitialization
{
 // Source Health's private, nonserialized capacity starts at zero. The native
 // player prefab serializes three; source runes/perks grant capacity later.
 static void Postfix(PlayerController __instance)=>__instance.playerHealth.maxSHP=0;
}

[HarmonyPatch(typeof(StatModHealth),"OnStatChanged")]
class AndroidHealthStat
{
 [System.ThreadStatic]internal static bool ExplicitMaximum;
 static bool Prefix(StatsHolder ___stats,PlayerHealth ___health)
 {if(!ExplicitMaximum)___health.maxHP=Mathf.Max(1,Mathf.FloorToInt(___stats[StatType.MaxHP].Modify(___health.baseMaxHP)));return false;}
}
[HarmonyPatch(typeof(PlayerHealth),nameof(PlayerHealth.maxHP),MethodType.Setter)]
class AndroidMaximumHealth
{
 static readonly AccessTools.FieldRef<PlayerHealth,int> HP=AccessTools.FieldRefAccess<PlayerHealth,int>("_hp");
 static bool Prefix(PlayerHealth __instance,int value,ref int ____maxHP)
 {
  int difference=value-____maxHP;____maxHP=value;
  __instance.onMaxHPChangedTo.Invoke(value);
  // Source's increase branch writes the sum directly. Its non-increase
  // branch clamps against the live maximum after max-health observers run.
  // Calling the native hp setter here adds a clamp to the increase branch.
  int restored=difference>0?__instance.hp+difference:Mathf.Clamp(__instance.hp,0,____maxHP);
  HP(__instance)=restored;__instance.onHealthChangedTo.Invoke(restored);
  if(difference<=0&&____maxHP<=0)__instance.onDeath.Invoke();
  return false;
 }
}
[HarmonyPatch(typeof(PlayerHealth),nameof(PlayerHealth.shp),MethodType.Setter)]
class AndroidSoulHealth
{
 internal const string ChangeEvent="Health.ShieldHealthChangeEvent";
 static bool Prefix(PlayerHealth __instance,int value,ref int ____shp)
 {
  // Android rejects an over-cap request, rather than truncating it. Red HP
  // and shield capacity are independent; there is no combined twenty cap.
  if(value>__instance.maxSHP)return false;
  int previous=____shp;____shp=value;
  if(__instance.transform==PlayerController.Instance.playerHealth.transform)
   __instance.PostNotification(ChangeEvent,value-previous);
  if(value>previous)__instance.onGainedSHP.Invoke();
  else if(value<previous)__instance.onLostSHP.Invoke();
  __instance.onSoulHPChangedTo.Invoke(value);return false;
 }
}
[HarmonyPatch(typeof(StatModTransformScale),"Start")]
class AndroidVisionScale
{
 static void Prefix(StatType ___statType,ref float ___maxScale)
 {if(___statType==StatType.VisionRange)___maxScale=float.PositiveInfinity;}
}
