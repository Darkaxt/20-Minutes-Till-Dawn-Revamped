using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source BurnBuff is a pooled plain object, not a component on the target.
public sealed class AndroidBurnStatus
{
 public const float BaseDuration=5;
 public const int TickCap=1000;
 public GameObject Target{get;private set;}
 public float Damage=>damage;
 internal GameObject Effect;
 float elapsed,damage,interval;int remaining;Health health;
 internal bool Affect(GameObject target,float cd,float amount,int count)
 {
  if(!target.activeSelf)return false;
  health=target.GetComponent<Health>();if(health==null)return false;
  Target=target;interval=cd;remaining=count;damage+=amount;return true;
 }
 internal void Tick(float dt)
 {
  if(health!=null&&health.isDead){Release();return;}
  if(remaining<1){Release();return;}
  if(Effect!=null)Effect.transform.position=Target.transform.position;
  elapsed+=dt;if(elapsed<interval)return;
  elapsed=0;remaining--;
  if(!health.isDead)
  {
   var owner=BurnSystem.SharedInstance;
   int amount=Mathf.FloorToInt(owner.burnDamageMultiplier.Modify(damage));
   int cap=AndroidBurnState.Get(owner).MaxDamage;if(amount>cap)amount=cap;
   AndroidDamageRounding.ChangeHealth(health,-amount,DamageType.Burn);
  }
  if(remaining<=0)Release();
 }
 internal void Release()
 {
  AndroidBurnState.Get(BurnSystem.SharedInstance).Release(this);
  damage=0;remaining=0;elapsed=0;interval=0;health=null;Target=null;
  if(Effect!=null){AndroidPools.Return(Effect);Effect=null;}
 }
}

public sealed class AndroidBurnState:MonoBehaviour
{
 readonly Stack<AndroidBurnStatus> free=new Stack<AndroidBurnStatus>();
 List<AndroidBurnStatus> active;
 internal BurnSystem Owner;
 internal int MaxDamage=AndroidBurnStatus.TickCap;
 internal string Tag;
 public static AndroidBurnState Get(BurnSystem owner)=>owner.GetComponent<AndroidBurnState>();
 internal void Initialize(BurnSystem owner,string tag,GameObject prefab)
 {
  Owner=owner;Tag=tag;
  ObjectPooler.SharedInstance.GetComponent<AndroidPools>().Register(tag,prefab,-1);
  owner.burnDamageMultiplier=new StatMod();owner.burnDurationMultiplier=new StatMod();
  active=new List<AndroidBurnStatus>();
 }
 internal int Index(GameObject target)
 {for(int i=0;i<active.Count;i++)if(active[i].Target==target)return i;return -1;}
 public AndroidBurnStatus Find(GameObject target)
 {int index=Index(target);return index<0?null:active[index];}
 internal void Burn(GameObject target,int amount)
 {
  if(target==null||!target.activeSelf||target.layer==(int)TagLayerUtil.Player)return;
  if(AndroidAffixes.Has(target,0))return;
  int count=Mathf.FloorToInt(Owner.burnDurationMultiplier.Modify(AndroidBurnStatus.BaseDuration)+.1f);
  if(count<1||target.GetComponent<Health>()==null)return;
  int index=Index(target);AndroidBurnStatus buff;
  if(index>=0)buff=active[index];
  else
  {
   buff=free.Count>0?free.Pop():new AndroidBurnStatus();
   if(!buff.Affect(target,1,amount,count)){free.Push(buff);return;}
   buff.Effect=ObjectPooler.SharedInstance.GetPooledObject(Tag);active.Add(buff);
  }
  if(index>=0&&!buff.Affect(target,1,amount,count))return;
  Owner.PostNotification(BurnSystem.InflictBurnEvent,null);
 }
 void FixedUpdate()
 {
  // Forward the original component's enabled ownership of this Unity callback.
  if(!Owner.enabled)return;
  for(int i=active.Count-1;i>=0;i--)active[i].Tick(Time.fixedDeltaTime);
 }
 internal void Release(AndroidBurnStatus buff){free.Push(buff);active.Remove(buff);}
 internal void ReleaseAll(){for(int i=active.Count-1;i>=0;i--)active[i].Release();}
 internal static void Clear()=>Get(BurnSystem.SharedInstance).ReleaseAll();
}
[HarmonyPatch(typeof(BurnSystem),"Awake")]
class AndroidBurnOwner
{static void Postfix(BurnSystem __instance){var state=__instance.gameObject.AddComponent<AndroidBurnState>();state.Owner=__instance;}}
[HarmonyPatch(typeof(BurnSystem),"Start")]
class AndroidBurnInitialization
{
 static bool Prefix(BurnSystem __instance,string ___burnFXOPTag,GameObject ___burnFXPrefab)
 {
  Plugin.SetField(__instance,"baseBurnDuration",AndroidBurnStatus.BaseDuration);
  AndroidBurnState.Get(__instance).Initialize(__instance,___burnFXOPTag,___burnFXPrefab);return false;
 }
}
[HarmonyPatch(typeof(BurnSystem),nameof(BurnSystem.Burn))]
class AndroidApplyBurn
{
 static bool Prefix(BurnSystem __instance,GameObject target,int burnDamage)
 {AndroidBurnState.Get(__instance).Burn(target,burnDamage);return false;}
}
[HarmonyPatch(typeof(BurnSystem),nameof(BurnSystem.IsBurning))]
class AndroidIsBurning
{
 // Keep Windows' Boolean ABI; the source method returns an index or minus1.
 static bool Prefix(BurnSystem __instance,GameObject target,ref bool __result)
 {__result=AndroidBurnState.Get(__instance).Index(target)>=0;return false;}
}
