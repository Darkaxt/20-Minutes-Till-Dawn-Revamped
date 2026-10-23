using System;
using System.Collections;
using HarmonyLib;
using flanne;
using UnityEngine;
using UnityEngine.Events;
namespace TwentyMinutes.Modernization;

internal static class AndroidAmmo
{
 static readonly AccessTools.FieldRef<Ammo,int> Amount=AccessTools.FieldRefAccess<Ammo,int>("<amount>k__BackingField");
 internal static void Restore(Ammo ammo,int value)
 {Amount(ammo)=Mathf.Clamp(value,0,ammo.max);ammo.OnAmmoChanged.Invoke(ammo.amount);}
 internal static IEnumerator Initialize(Ammo ammo,Gun gun)
 {
  yield return new WaitForEndOfFrame();
  ammo.Reload();ammo.OnAmmoChanged.Invoke(ammo.amount);ammo.OnMaxAmmoChanged.Invoke(gun.maxAmmo);
  var changed=(EventHandler)Delegate.CreateDelegate(typeof(EventHandler),ammo,AccessTools.Method(typeof(Ammo),"AmmoModChanged"));
  gun.stats[StatType.MaxAmmo].ChangedEvent+=changed;
 }
}
[HarmonyPatch(typeof(Ammo),"Start")]
class AndroidAmmoInitialization
{
 static bool Prefix(Ammo __instance,Gun ___gun)
 {__instance.infiniteAmmo=new BoolToggle(false);__instance.StartCoroutine(AndroidAmmo.Initialize(__instance,___gun));return false;}
}
[HarmonyPatch(typeof(Ammo),nameof(Ammo.UseAmmo))]
class AndroidAmmoConsumption
{
 static bool Prefix(Ammo __instance,int __0)
 {if(!__instance.infiniteAmmo.value)AndroidAmmo.Restore(__instance,__instance.amount-__0);return false;}
}
[HarmonyPatch(typeof(Ammo),nameof(Ammo.GainAmmo))]
class AndroidAmmoGain
{static bool Prefix(Ammo __instance,int __0){AndroidAmmo.Restore(__instance,__instance.amount+__0);return false;}}
[HarmonyPatch(typeof(Ammo),"OnDestroy")]
class AndroidAmmoDestruction
{
 // The source cleanup is named OnDestory, not Unity's OnDestroy. Do not repair
 // that source callback by retaining the native automatic unsubscribe.
 static bool Prefix()=>false;
}
[HarmonyPatch]
class AndroidReloadEventOwner
{
 static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()
 {yield return AccessTools.Method(typeof(OnPlayerReloadEvent),"Start");yield return AccessTools.Method(typeof(OnPlayerReloadEvent),"OnDestroy");}
 static bool Prefix(OnPlayerReloadEvent __instance,System.Reflection.MethodBase __originalMethod)
 {
  var handler=(UnityAction)Delegate.CreateDelegate(typeof(UnityAction),__instance,AccessTools.Method(typeof(OnPlayerReloadEvent),"OnReload"));
  var ammo=PlayerController.Instance.ammo;
  if(__originalMethod.Name=="Start")ammo.OnReload.AddListener(handler);else ammo.OnReload.RemoveListener(handler);
  return false;
 }
}
