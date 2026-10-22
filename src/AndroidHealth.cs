using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

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
 static bool Prefix(PlayerHealth __instance,int value,ref int ____maxHP)
 {
  int difference=value-____maxHP;____maxHP=value;
  __instance.onMaxHPChangedTo.Invoke(value);
  __instance.hp=difference>0?__instance.hp+difference:__instance.hp;
  if(difference<=0&&value<=0)__instance.onDeath.Invoke();
  return false;
 }
}
[HarmonyPatch(typeof(PlayerHealth),nameof(PlayerHealth.shp),MethodType.Setter)]
class AndroidSoulHealth
{
 static bool Prefix(PlayerHealth __instance,int value,ref int ____shp)
 {
  // Android rejects an over-cap request, rather than truncating it. Red HP
  // and shield capacity are independent; there is no combined twenty cap.
  if(value>__instance.maxSHP)return false;
  int previous=____shp;____shp=value;
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
