using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PowerupSystem;
namespace TwentyMinutes.Modernization;

// The shipped Generator effect attaches an empty prefab. Restore the already
// compiled native helper on that effect's actual instantiated object; native
// Start/OnDestroy continue to own its ammo-event subscription and coroutine.
[HarmonyPatch]
class GeneratorBuff
{
 internal const string RootName="PF_FireRateBuffOnAmmoGain(Clone)";
 static readonly AccessTools.FieldRef<Powerup,LocalizedString> Key=AccessTools.FieldRefAccess<Powerup,LocalizedString>("nameStrID");
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Postfix(Powerup __instance,PlayerController __0)
 {
  if(Key(__instance).key!="generator_name")return;
  foreach(Transform child in __0.transform)
  {
   if(child.name!=RootName||child.GetComponent<FireRateBuffOnGainAmmo>()!=null)continue;
   var buff=child.gameObject.AddComponent<FireRateBuffOnGainAmmo>();
   Plugin.SetField(buff,"duration",2f);Plugin.SetField(buff,"buffAmount",.35f);
  }
 }
}

[HarmonyPatch(typeof(FireRateBuffOnGainAmmo),"OnAmmoGained")]
class GeneratorTimerRestart
{
 static readonly AccessTools.FieldRef<FireRateBuffOnGainAmmo,IEnumerator> Routine=AccessTools.FieldRefAccess<FireRateBuffOnGainAmmo,IEnumerator>("_buffCoroutine");
 static readonly AccessTools.FieldRef<FireRateBuffOnGainAmmo,float> Timer=AccessTools.FieldRefAccess<FireRateBuffOnGainAmmo,float>("_timer");
 static void Prefix(FireRateBuffOnGainAmmo __instance)
 {
  if(__instance.name==GeneratorBuff.RootName&&Routine(__instance)==null)Timer(__instance)=0f;
 }
}
