using HarmonyLib;
using UnityEngine;
using flanne;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// Android 8.6 BurnBuff: one accumulated damage value and refreshed tick count,
// preserving the existing tick phase on reapplication. The shipped scene's cap
// is 1000, not the constructor's unused default of 100.
public sealed class AndroidBurnStatus:MonoBehaviour
{
 public const float BaseDuration=5;
 public const int TickCap=1000;
 public bool Live{get;private set;}
 BurnSystem owner;Health health;ObjectPooler pool;GameObject effect;
 float elapsed,damage;int remaining;
 public void Apply(BurnSystem source,Health target,int amount)
 {
  if(!Live)
  {
   owner=source;health=target;pool=ObjectPooler.SharedInstance;elapsed=0;damage=0;
   effect=pool.GetPooledObject((string)AccessTools.Field(typeof(BurnSystem),"burnFXOPTag").GetValue(source));
   effect.transform.position=target.transform.position;effect.SetActive(true);Live=true;enabled=true;
  }
  damage+=amount;remaining=Mathf.FloorToInt(source.burnDurationMultiplier.Modify(BaseDuration)+.1f);
 }
 void Update()
 {
  if(!Live)return;
  if(owner==null||health==null||health.isDead||remaining<=0){Release();return;}
  effect.transform.position=health.transform.position;elapsed+=Time.deltaTime;
  if(elapsed<1)return;
  elapsed=0;remaining--;
  int tick=Mathf.Min(TickCap,Mathf.FloorToInt(owner.burnDamageMultiplier.Modify(damage)));
  health.TakeDamage(DamageType.Burn,tick);
  if(remaining<=0)Release();
 }
 void OnDisable()=>Release();
 void Release()
 {
  if(!Live)return;
  Live=false;elapsed=0;damage=0;remaining=0;
  if(effect!=null)
  {
   if(pool!=null){effect.transform.SetParent(pool.transform,false);effect.SetActive(false);}
   else Object.Destroy(effect);
  }
  effect=null;owner=null;health=null;pool=null;enabled=false;
 }
}
[HarmonyPatch(typeof(BurnSystem),"Start")]
class AndroidBurnDuration
{
 static void Prefix(BurnSystem __instance)=>Plugin.SetField(__instance,"baseBurnDuration",AndroidBurnStatus.BaseDuration);
}
[HarmonyPatch(typeof(BurnSystem),nameof(BurnSystem.Burn))]
class AndroidApplyBurn
{
 static bool Prefix(BurnSystem __instance,GameObject target,int burnDamage)
 {
  if(target==null||!target.activeSelf||target.CompareTag("Player"))return false;
  var health=target.GetComponent<Health>();if(health==null)return false;
  var status=target.GetComponent<AndroidBurnStatus>();if(status==null)status=target.AddComponent<AndroidBurnStatus>();
  status.Apply(__instance,health,burnDamage);
  // Keep the native notification payload expected by the Windows consumers.
  __instance.PostNotification(BurnSystem.InflictBurnEvent,target);return false;
 }
}
[HarmonyPatch(typeof(BurnSystem),nameof(BurnSystem.IsBurning))]
class AndroidIsBurning
{
 static bool Prefix(GameObject target,ref bool __result)
 {__result=target!=null&&target.activeSelf&&target.GetComponent<AndroidBurnStatus>() is AndroidBurnStatus status&&status.Live;return false;}
}
