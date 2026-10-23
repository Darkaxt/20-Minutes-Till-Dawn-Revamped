using System.Collections;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Keep the original arrow families on one source drop/retrieval lifecycle.
// Arrow Caller deliberately uses the same ground template as Thunder Caller
// in the shipped Android asset graph, rather than the native Magic Bow one.
internal static class AndroidArrowGround
{
 static readonly FieldInfo Prefab=AccessTools.Field(typeof(SpawnOnDisableProjectile),"prefab");
 internal static void ConfigurePrefab(GameObject prefab)
 {
  if(prefab.name!="PF_ArrowCallerProjectile")return;
  var thunder=Resources.FindObjectsOfTypeAll<SpawnOnDisableProjectile>().Single(p=>!p.gameObject.scene.IsValid()&&p.name=="PF_ThunderCallerProjectile");
  Prefab.SetValue(prefab.GetComponent<SpawnOnDisableProjectile>(),Prefab.GetValue(thunder));
 }
}
[HarmonyPatch(typeof(SpawnOnDisableProjectile))]
class AndroidArrowGroundLifecycle
{
 [HarmonyPrefix,HarmonyPatch("Start")]
 static bool Start(GameObject ___prefab,ref ObjectPooler ___OP,ref bool ___isInitialized)
 {
  ___OP=ObjectPooler.SharedInstance;___OP.GetComponent<AndroidPools>().Register(___prefab.name,___prefab);
  ___isInitialized=true;return false;
 }
 [HarmonyPrefix,HarmonyPatch("OnDisable")]
 static bool Disable(SpawnOnDisableProjectile __instance,GameObject ___prefab,ObjectPooler ___OP,bool ___isInitialized)
 {
  if(!__instance.isSecondary&&___isInitialized)
   ___OP.GetComponent<AndroidPools>().Get(___prefab.name,__instance.transform.position);
  return false;
 }
}

public sealed class AndroidHoming:MonoBehaviour
{
 Camera mainCamera;
 TrailRenderer[] trails;SpriteTrail[] spriteTrails;ParticleSystem[] particles;
 Transform target;Vector2 cachedDirection;bool visible;int frame;
 void Awake()
 {
  mainCamera=Camera.main;trails=GetComponentsInChildren<TrailRenderer>(true);spriteTrails=GetComponentsInChildren<SpriteTrail>(true);particles=GetComponentsInChildren<ParticleSystem>(true);
 }
 internal void Activate()
 {
  var actor=AndroidSummonTargets.Random(float.PositiveInfinity,float.PositiveInfinity);target=actor!=null?actor.transform:null;
  frame=0;visible=true;SetTrails(true);
  // Source OnEnable does not reset cachedDirection across pooled reuse.
 }
 void SetTrails(bool value)
 {
  foreach(var trail in trails)if(trail)trail.emitting=value;
  foreach(var trail in spriteTrails)if(trail)trail.SetEnabled(value);
  foreach(var particle in particles)if(particle){var emission=particle.emission;emission.enabled=value;}
 }
 internal void Step(MoveComponent2D movement,float acceleration)
 {
  frame++;
  if(frame%(visible?5:30)==0&&mainCamera!=null)
  {
   bool next=GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(mainCamera),new Bounds(transform.position,Vector3.one*.5f));
   if(next!=visible){visible=next;SetTrails(visible);}
  }
  if(visible||frame%3==0)
  {
   if(target!=null&&target.gameObject.activeSelf)cachedDirection=((Vector2)(target.position-transform.position)).normalized;
   else if(frame%10==0)
   {var actor=AndroidSummonTargets.Random(float.PositiveInfinity,float.PositiveInfinity);target=actor!=null?actor.transform:null;}
  }
  if(cachedDirection!=Vector2.zero)movement.vector+=cachedDirection*acceleration*Time.fixedDeltaTime;
 }
 void OnDisable(){SetTrails(false);foreach(var trail in trails)if(trail)trail.Clear();}
}
[HarmonyPatch(typeof(HomingProjectile),"OnEnable")]
class AndroidHomingEnable
{
 static bool Prefix(HomingProjectile __instance)
 {var state=__instance.GetComponent<AndroidHoming>();if(state==null)state=__instance.gameObject.AddComponent<AndroidHoming>();state.Activate();return false;}
}
[HarmonyPatch(typeof(HomingProjectile),"FixedUpdate")]
class AndroidHomingStep
{static bool Prefix(HomingProjectile __instance,MoveComponent2D ___moveComponent,float ___acceleration){__instance.GetComponent<AndroidHoming>().Step(___moveComponent,___acceleration);return false;}}

// Android replaces the trail's BoolToggle counter with a simple assignment.
// Keep the native rendering/segment lifecycle and represent that boolean with
// a fresh native value; repeated true/false calls must not accumulate flips.
[HarmonyPatch(typeof(SpriteTrail),nameof(SpriteTrail.SetEnabled))]
class AndroidSpriteTrailEnable
{
 static bool Prefix(bool enabled,ref BoolToggle ___mbEnabled,float ___mSpawnInterval,ref float ___mSpawnTimer)
 {___mbEnabled=new BoolToggle(enabled);if(enabled)___mSpawnTimer=___mSpawnInterval;return false;}
}

// The Android type has only its constructor: no spawn/fall/disable callbacks.
[HarmonyPatch]
class AndroidSpawnBounce
{
 static System.Collections.Generic.IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(BounceOnSpawn),"OnEnable"),AccessTools.Method(typeof(BounceOnSpawn),"OnDisable"),AccessTools.Method(typeof(BounceOnSpawn),"Fall")};
 static bool Prefix()=>false;
}

[HarmonyPatch(typeof(ArrowRetrieveOnReload),"OnReload")]
class AndroidArrowReload
{
 static readonly FieldInfo Points=AccessTools.Field(typeof(ArrowRetrieveOnReload),"retrievalPoints");
 static readonly MethodInfo Retrieve=AccessTools.Method(typeof(ArrowRetrieveOnReload),"RetrieveCR");
 static bool Prefix(ArrowRetrieveOnReload __instance,GameObject ___retrieveArrowPrefab,float ___damageMulti,bool ___dontRetrieve,SoundEffectSO ___retrieveSFX)
 {
  var points=(System.Collections.Generic.List<ArrowRetrievalPoint>)Points.GetValue(null);var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();
  for(int i=0;i<points.Count;i++)
  {
   if(!points[i].gameObject.activeSelf)continue;
   var arrow=pool.Get(___retrieveArrowPrefab.name,points[i].transform.position);
   var harm=arrow.GetComponent<Harmful>();harm.damageAmount=Mathf.FloorToInt(PlayerController.Instance.gun.damage*___damageMulti);
   if(!___dontRetrieve)harm.StartCoroutine((IEnumerator)Retrieve.Invoke(__instance,new object[]{arrow}));
   AndroidPools.Return(points[i].gameObject);
  }
  ___retrieveSFX?.Play();return false;
 }
}
[HarmonyPatch(typeof(ArrowRetrieveOnReload),"RetrieveCR")]
class AndroidArrowRetrieval
{
 static readonly FieldInfo Speed=AccessTools.Field(typeof(ArrowRetrieveOnReload),"retrieveSpeed");
 static bool Prefix(ArrowRetrieveOnReload __instance,GameObject obj,ref IEnumerator __result)
 {__result=Retrieve(__instance,obj);return false;}
 static IEnumerator Retrieve(ArrowRetrieveOnReload owner,GameObject arrow)
 {
  arrow.transform.SetParent(PlayerController.Instance.transform);
  while(arrow.transform.localPosition!=Vector3.zero)
  {yield return null;arrow.transform.localPosition=Vector3.MoveTowards(arrow.transform.localPosition,Vector3.zero,(float)Speed.GetValue(owner)*Time.deltaTime);}
  AndroidPools.Return(arrow);
 }
}
