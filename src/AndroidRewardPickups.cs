using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Pickups;
namespace TwentyMinutes.Modernization;

// Original InGameSaveCollectIgnore is a component, not a coroutine-state test.
// Both source reward pickups clear it with deferred Destroy on enable.
public sealed class AndroidCollectIgnore:MonoBehaviour
{
 internal static void Clear(Component owner)
 {var mark=owner.GetComponent<AndroidCollectIgnore>();if(mark!=null)Object.Destroy(mark);}
}

public sealed class AndroidDevilPickupLifecycle:MonoBehaviour
{
 internal static void Configure(GameObject prefab)
 {if(prefab.GetComponent<DevilDealPickup>()!=null&&prefab.GetComponent<AndroidDevilPickupLifecycle>()==null)prefab.AddComponent<AndroidDevilPickupLifecycle>();}
 void OnEnable()=>AndroidCollectIgnore.Clear(this);
}

public sealed class AndroidChestLifecycle:MonoBehaviour
{
 internal static readonly AccessTools.FieldRef<ChestPickup,IEnumerator> Fountain=AccessTools.FieldRefAccess<ChestPickup,IEnumerator>("_xpFountainCoroutine");
 internal static void Configure(GameObject prefab)
 {if(prefab.GetComponent<ChestPickup>()!=null&&prefab.GetComponent<AndroidChestLifecycle>()==null)prefab.AddComponent<AndroidChestLifecycle>();}
 internal static void Stop(ChestPickup chest)
 {var routine=Fountain(chest);if(routine!=null){chest.StopCoroutine(routine);Fountain(chest)=null;}}
 void OnDisable()=>Stop(GetComponent<ChestPickup>());
}
[HarmonyPatch(typeof(ChestPickup),"OnEnable")]
class AndroidChestEnable
{
 static void Prefix(ChestPickup __instance)=>AndroidChestLifecycle.Stop(__instance);
 static void Postfix(ChestPickup __instance)
 {AndroidCollectIgnore.Clear(__instance);}
}
[HarmonyPatch]
class AndroidRewardPickupTrigger
{
 static IEnumerable<MethodBase> TargetMethods()
 {yield return AccessTools.Method(typeof(ChestPickup),"OnTriggerEnter2D");yield return AccessTools.Method(typeof(DevilDealPickup),"OnTriggerEnter2D");}
 static bool Prefix(MonoBehaviour __instance,Collider2D other)
 {
  if(__instance is ChestPickup chest&&AndroidChestLifecycle.Fountain(chest)!=null)return false;
  if(other.tag=="Player"||other.tag=="MapBounds")
   if(__instance.GetComponent<AndroidCollectIgnore>()==null)__instance.gameObject.AddComponent<AndroidCollectIgnore>();
  return true;
 }
}
[HarmonyPatch(typeof(ChestPickup),"XPFountainCR")]
class AndroidChestFountain
{
 static readonly AccessTools.FieldRef<ChestPickup,int> Amount=AccessTools.FieldRefAccess<ChestPickup,int>("amountOfXP");
 static readonly AccessTools.FieldRef<ChestPickup,string> Tag=AccessTools.FieldRefAccess<ChestPickup,string>("xpOPTag");
 static readonly AccessTools.FieldRef<ChestPickup,SpriteRenderer> Renderer=AccessTools.FieldRefAccess<ChestPickup,SpriteRenderer>("spriteRenderer");
 static readonly AccessTools.FieldRef<ChestPickup,Sprite> Open=AccessTools.FieldRefAccess<ChestPickup,Sprite>("chestOpen");
 static readonly AccessTools.FieldRef<ChestPickup,SoundEffectSO> Sound=AccessTools.FieldRefAccess<ChestPickup,SoundEffectSO>("xpSpawnSFX");
 static readonly AccessTools.FieldRef<ChestPickup,ObjectPooler> Pool=AccessTools.FieldRefAccess<ChestPickup,ObjectPooler>("OP");
 static bool Prefix(ChestPickup __instance,ref IEnumerator __result){__result=Run(__instance);return false;}
 static IEnumerator Run(ChestPickup chest)
 {
  yield return new WaitForSeconds(.1f);
  Renderer(chest).sprite=Open(chest);
  for(int i=0;i<Amount(chest);i++)
  {
   var xp=Pool(chest).GetPooledObject(Tag(chest));if(xp==null)break;
   xp.transform.position=chest.transform.position;xp.SetActive(true);
   float x=xp.transform.position.x+Random.Range(-1f,1f),y=xp.transform.position.y+Random.Range(-1f,1f);
   LeanTween.move(xp,new Vector3(x,y,0),.5f);Sound(chest)?.Play();
   yield return new WaitForSeconds(.1f);
  }
  AndroidChestLifecycle.Fountain(chest)=null;AndroidPools.Return(chest.gameObject);
 }
}
