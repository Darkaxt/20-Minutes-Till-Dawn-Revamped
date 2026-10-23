using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

public sealed class AndroidSalvo:MonoBehaviour
{
 internal readonly List<int> Locked=new List<int>(),Candidates=new List<int>();
 // The native compiler-generated wrapper calls Shooter.Shoot non-virtually,
 // preserving native Windows aiming, shot creation and shot notifications.
 static readonly MethodInfo BaseShoot=AccessTools.Method(typeof(SalvoShooter),"<>n__0");
 static readonly FieldInfo Shooting=AccessTools.Field(typeof(SalvoShooter),"_shootSalveCR");
 internal IEnumerator Fire(SalvoShooter owner,List<GameObject> indicators,ProjectileRecipe recipe,int projectiles,float spread,float inaccuracy)
 {
  foreach(var indicator in indicators)AndroidPools.Return(indicator);indicators.Clear();
  var player=PlayerController.Instance;float delay=player.stats[StatType.FireRate].ModifyInverse(.05f);
  var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();
  foreach(int id in Locked)
  {
   if(player.ammo.amount==0)continue;
   var target=pool.GetFromID(id);if(target==null)continue;
   player.gun.gunData.gunshotSFX.Play();
   BaseShoot.Invoke(owner,new object[]{recipe,(Vector2)(target.transform.position-owner.transform.position),projectiles,spread,inaccuracy});
   yield return new WaitForSeconds(delay);
  }
  Locked.Clear();Shooting.SetValue(owner,null);
 }
}
[HarmonyPatch(typeof(SalvoShooter),nameof(SalvoShooter.Init))]
class AndroidSalvoInitialization
{
 static void Postfix(SalvoShooter __instance)
 {if(__instance.GetComponent<AndroidSalvo>()==null)__instance.gameObject.AddComponent<AndroidSalvo>();}
}
[HarmonyPatch(typeof(SalvoShooter),"GetClosestUnlockedEnemy")]
class AndroidSalvoTarget
{
 static bool Prefix(SalvoShooter __instance,float ___range,ref Transform __result)
 {
  var state=__instance.GetComponent<AndroidSalvo>();var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();
  state.Candidates.Clear();pool.GetMonsterInRange(state.Candidates,___range,__instance.transform.position);
  float distance=float.PositiveInfinity;__result=null;
  foreach(int id in state.Candidates)
  {
   var target=pool.GetFromID(id);if(target==null||state.Locked.Contains(id))continue;
   float next=Vector2.Distance(target.transform.position,__instance.transform.position);
   if(next<distance){distance=next;__result=target.transform;}
  }
  return false;
 }
}
[HarmonyPatch(typeof(SalvoShooter),"LockOn")]
class AndroidSalvoLock
{
 static bool Prefix(SalvoShooter __instance,Transform target,List<GameObject> ____indicators,GameObject ___targetIndicatorPrefab,SoundEffectSO ___targetAcquiredSFX)
 {
  var state=__instance.GetComponent<AndroidSalvo>();if(state.Locked.Count>=PlayerController.Instance.ammo.amount)return false;
  state.Locked.Add(target.gameObject.GetInstanceID());
  var indicator=ObjectPooler.SharedInstance.GetPooledObject(___targetIndicatorPrefab.name);____indicators.Add(indicator);
  indicator.transform.position=target.position;indicator.SetActive(true);___targetAcquiredSFX?.Play();return false;
 }
}
[HarmonyPatch(typeof(SalvoShooter),"UpdateIndicators")]
class AndroidSalvoIndicators
{
 static bool Prefix(SalvoShooter __instance,List<GameObject> ____indicators)
 {
  var state=__instance.GetComponent<AndroidSalvo>();var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();
  for(int i=state.Locked.Count-1;i>=0;i--)
  {
   var target=pool.GetFromID(state.Locked[i]);if(target==null)continue;
   if(target.activeSelf)____indicators[i].transform.position=target.transform.position;
   else
   {
    // Source calls SetActive(false) here, unlike the firing coroutine's pool
    // Release. Retain its active-list/counter consequence instead of repairing it.
    ____indicators[i].SetActive(false);____indicators.RemoveAt(i);state.Locked.RemoveAt(i);
   }
  }
  return false;
 }
}
[HarmonyPatch(typeof(SalvoShooter),"OnDisable")]
class AndroidSalvoDisable
{
 static bool Prefix(SalvoShooter __instance,List<GameObject> ____indicators,ref IEnumerator ____shootSalveCR)
 {
  foreach(var indicator in ____indicators)indicator.SetActive(false);
  ____indicators.Clear();
  // Before Shooter.Start/Init the source constructor's lock list is empty;
  // there is no sidecar state to clear yet at that native lifecycle boundary.
  var state=__instance.GetComponent<AndroidSalvo>();if(state!=null)state.Locked.Clear();
  ____shootSalveCR=null;return false;
 }
}
[HarmonyPatch(typeof(SalvoShooter),"SalvoShootCR")]
class AndroidSalvoFire
{
 static bool Prefix(SalvoShooter __instance,ProjectileRecipe recipe,int numProjectiles,float spread,float inaccuracy,List<GameObject> ____indicators,ref IEnumerator __result)
 {__result=__instance.GetComponent<AndroidSalvo>().Fire(__instance,____indicators,recipe,numProjectiles,spread,inaccuracy);return false;}
}
