using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Android stores an ordered list of pooled GameObject IDs, including duplicate
// contacts. It does not clear the list or tick phase on disable/reuse.
public sealed class AndroidPersistentTargets:MonoBehaviour
{
 internal readonly List<int> IDs=new List<int>();
 internal static bool Eligible(GameObject target)=>target.layer==(int)TagLayerUtil.Enemy&&target.GetComponent<Health>()!=null;
}
[HarmonyPatch(typeof(PersistentHarmBasedOnGunDamage),"Awake")]
class AndroidPersistentInitialization
{
 static bool Prefix(PersistentHarmBasedOnGunDamage __instance)
 {__instance.tickRate=new StatMod();__instance.gameObject.AddComponent<AndroidPersistentTargets>();return false;}
}
[HarmonyPatch(typeof(PersistentHarmBasedOnGunDamage),"OnCollisionEnter2D")]
class AndroidPersistentEnter
{
 static bool Prefix(PersistentHarmBasedOnGunDamage __instance,Collision2D __0)
 {var target=__0.gameObject;if(AndroidPersistentTargets.Eligible(target))__instance.GetComponent<AndroidPersistentTargets>().IDs.Add(target.GetInstanceID());return false;}
}
[HarmonyPatch(typeof(PersistentHarmBasedOnGunDamage),"OnCollisionExit2D")]
class AndroidPersistentExit
{
 static bool Prefix(PersistentHarmBasedOnGunDamage __instance,Collision2D __0)
 {var target=__0.gameObject;if(AndroidPersistentTargets.Eligible(target))__instance.GetComponent<AndroidPersistentTargets>().IDs.Remove(target.GetInstanceID());return false;}
}
[HarmonyPatch(typeof(PersistentHarmBasedOnGunDamage),"DealDamage")]
class AndroidPersistentDamage
{
 static bool Prefix(PersistentHarmBasedOnGunDamage __instance,Gun ___playerGun)
 {
  var ids=__instance.GetComponent<AndroidPersistentTargets>().IDs;
  var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();
  for(int i=ids.Count-1;i>=0;i--)
  {
   var target=pool.GetFromID(ids[i]);if(target==null)continue;
   var health=target.GetComponent<Health>();
   if(health!=null)AndroidDamageRounding.ChangeHealth(health,-Mathf.FloorToInt(__instance.multiplier*___playerGun.damage),__instance.damageType);
   // Windows observers consume the GameObject represented by Android's ID.
   if(__instance.triggerOnHit)PlayerController.Instance.gameObject.PostNotification(Projectile.ImpactEvent,target);
  }
  return false;
 }
}
