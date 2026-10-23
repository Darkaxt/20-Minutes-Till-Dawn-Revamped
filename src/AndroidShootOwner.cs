using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine.Events;
using flanne;
using flanne.PowerupSystem;
namespace TwentyMinutes.Modernization;

// Source AttackOnShoot obtains the current player's gun on each access. Keep
// native threshold/counter/virtual Attack and Init behavior, without its cached
// parent-player gun. The source destroy callback also reads the current gun.
internal static class AndroidShootListener
{
 static readonly MethodInfo Increment=AccessTools.Method(typeof(AttackOnShoot),"IncrementCounter");
 internal static UnityAction Listener(AttackOnShoot owner)=>(UnityAction)Delegate.CreateDelegate(typeof(UnityAction),owner,Increment);
}
[HarmonyPatch(typeof(AttackOnShoot),"Start")]
class AndroidShootOwnerStart
{
 static bool Prefix(AttackOnShoot __instance)
 {
  PlayerController.Instance.gun.OnShoot.AddListener(AndroidShootListener.Listener(__instance));
  AccessTools.Method(__instance.GetType(),"Init").Invoke(__instance,null);return false;
 }
}
[HarmonyPatch(typeof(AttackOnShoot),"OnDestroy")]
class AndroidShootOwnerDestroy
{
 static bool Prefix(AttackOnShoot __instance)
 {PlayerController.Instance.gun.OnShoot.RemoveListener(AndroidShootListener.Listener(__instance));return false;}
}
