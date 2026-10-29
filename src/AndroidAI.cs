using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.AISpecials;
namespace TwentyMinutes.Modernization;

static class AndroidAIRecipes
{
 static readonly HashSet<string> Bodyless=new HashSet<string>{"BrainMonster","EyeMonster","Lamprey","PF_SpawnedBug","PF_TreeMonster","PF_TreeMonster (1)","PF_TreeMonster (12)","PF_TreeMonster (14)","PF_TreeMonster (15)","PF_TreeMonster (4)","PF_TreeMonster (8)","PF_TreeMonster (9)"};
 internal static bool IsBodyless(AIComponent actor)
 {
  string name=actor.name;if(name.EndsWith("(Clone)"))name=name.Substring(0,name.Length-7);
  return Bodyless.Contains(name);
 }
 internal static void ConfigureTemplate(GameObject owned)
 {
  AndroidForestEncounter.ConfigureTemplate(owned);
  // Modify an inactive owned template before activation/physics callbacks,
  // rather than remove bodies during a spawned actor's Awake. Original source
  // roots and ancestors for these actors have no Rigidbody2D. Native Rb reads
  // occur only in the replaced movement and disable paths.
  foreach(var actor in owned.GetComponentsInChildren<AIComponent>(true))
   if(IsBodyless(actor)){Object.DestroyImmediate(actor.GetComponent<Rigidbody2D>());actor.moveComponent.Rb=null;}
 }
 internal static GameObject SpawnMap(GameObject prefab)=>Object.Instantiate(ObjectPooler.SharedInstance.GetComponent<AndroidPools>().SourceTemplate(prefab));
 internal static void ConfigurePrefab(GameObject prefab)
 {
  // The common-key actor AI field comparison differs here: Android's shipped
  // Shoggoth has a four-second special cooldown, Windows has two seconds.
  if(prefab.name=="Shoggoth")prefab.GetComponent<AIComponent>().specialCooldown=4;
  var ai=prefab.GetComponent<AIComponent>();
  if(ai!=null&&ai.specialSO is AIRandomAreaProjectileSpecial lobbed&&lobbed.name=="RhogogShoot")
   Plugin.SetField(lobbed,"airTime",new Vector2(.70968f,1.3194f));
 }
}
[HarmonyPatch(typeof(Spawner))]
class AndroidSecondaryEnemySpawner
{
 [HarmonyPrefix,HarmonyPatch("Start")]
 static bool Start(GameObject ___spawnedObject,ref ObjectPooler ___OP)
 {
  ___OP=ObjectPooler.SharedInstance;
  ___OP.GetComponent<AndroidPools>().Register(___spawnedObject.name,___spawnedObject);
  return false;
 }
 [HarmonyPrefix,HarmonyPatch(nameof(Spawner.Spawn))]
 static bool Spawn(Spawner __instance,GameObject ___spawnedObject,Transform[] ___spawnPoints,float ___initialSpeed,ObjectPooler ___OP)
 {
  // Acquire with the source position before the pool's SetActive call,
  // replacing native fetch-at-zero, reposition and second activation.
  var pool=___OP.GetComponent<AndroidPools>();
  foreach(var point in ___spawnPoints)
  {
   var actor=pool.Get(___spawnedObject.name,point.position);
   var move=actor.GetComponent<MoveComponent2D>();
   if(move!=null)move.vector=(actor.transform.position-__instance.transform.position).normalized*___initialSpeed;
  }
  return false;
 }
}
[HarmonyPatch(typeof(HomeTowardsPlayer),"FixedUpdate")]
class AndroidEnemyProjectileHoming
{
 static bool Prefix(HomeTowardsPlayer __instance,Transform ___playerTransform,MoveComponent2D ___moveComponent,float ___acceleration)
 {
  if(___playerTransform==null)return false;
  Vector2 direction=___playerTransform.position-__instance.transform.position;
  Vector2 before=___moveComponent.vector;
  ___moveComponent.vector=before+direction.normalized*___acceleration*Time.fixedDeltaTime;
  return false;
 }
}
public sealed class AndroidAIActor:MonoBehaviour
{
 internal AIComponent Actor;internal bool Initialized;
 internal Transform LastClosest;internal float SpecialCooldownMultiplier=1;
 void LateUpdate()
 {
  if(!Actor.enabled||Initialized)return;
  Initialized=true;AIController.SharedInstance.Register(Actor);
 }
 void OnDisable(){AIController.SharedInstance.UnRegister(Actor);Initialized=false;}
 // Android exposes this receiver to animation events.
 public void PoolRelease()=>AndroidPools.Return(gameObject);
}
public sealed class AndroidAI:MonoBehaviour
{
 static readonly FieldInfo Actors=AccessTools.Field(typeof(AIController),"aiComponents"),Follow=AccessTools.Field(typeof(AIController),"followTransform");
 static readonly MethodInfo Look=AccessTools.Method(typeof(AIController),"AILookTowards"),Closest=AccessTools.Method(typeof(AIController),"GetClosestAI");
 internal AIController Native;int frame;float elapsed;
 void Update()
 {
  if(!Native.enabled)return;
  frame++;elapsed+=Time.deltaTime;if(frame%3!=0)return;
  float dt=elapsed;elapsed=0;
  var actors=(List<AIComponent>)Actors.GetValue(null);var follow=(Transform)Follow.GetValue(Native);
  for(int i=0;i<actors.Count;i++)
  {
   var ai=actors[i];if(!ai.gameObject.activeInHierarchy||ai.frozen)continue;
   var control=ai.GetComponent<ElementalControl>();if(control!=null&&(control.Stun>0||control.Fear>0))continue;
   var state=ai.GetComponent<AndroidAIActor>();Vector2 perceived=control!=null?control.Perceived(follow.position):(Vector2)follow.position;Vector2 direction=perceived-(Vector2)ai.transform.position;
   if(Native.playerRepel)
   {
    Vector2 away=-direction.normalized;
    if(Vector2.Dot(ai.moveComponent.vector,away)<ai.maxMoveSpeed)ai.moveComponent.vector+=away*ai.acceleration*dt;
   }
   else if(ai.specialTimer>0||(ai.specialRangeSqr>0&&direction.sqrMagnitude<ai.specialRangeSqr))
   {
    if(!ai.dontFaceDuringSpecial)Look.Invoke(Native,new object[]{ai,direction});
    if(ai.specialTimer<=0)
    {
     var elite=ai.GetComponent<EnhancedEnemy>();float frequency=elite!=null&&elite.Enhanced&&elite.FasterEye?Mathf.Pow(2f/3f,elite.Tier):1;
     ai.specialTimer+=ai.specialCooldown*state.SpecialCooldownMultiplier*frequency;
     ai.specialSO.Use(ai,control!=null?control.Aim(follow):follow);
     if(!ai.dontLookAtPlayer)Look.Invoke(Native,new object[]{ai,direction});
    }
    ai.specialTimer-=dt;
   }
   else
   {
    if(!ai.dontLookAtPlayer)Look.Invoke(Native,new object[]{ai,direction});
    if(state.LastClosest!=null)
    {
     // Source's cached-neighbor branch has no max-speed condition and clears
     // the reference after using it, alternating query and cached updates.
     Vector2 away=ai.transform.position-state.LastClosest.position;
     ai.moveComponent.vector+=away.normalized*ai.acceleration*dt;state.LastClosest=null;
    }
    else
    {
     if(!ai.ignoreFlock)
     {
      var closest=(Transform)Closest.Invoke(Native,new object[]{ai});
      if(closest!=null){state.LastClosest=closest;direction=ai.transform.position-closest.position;}
     }
     if(Vector2.Dot(ai.moveComponent.vector,direction.normalized)<ai.maxMoveSpeed)ai.moveComponent.vector+=direction.normalized*ai.acceleration*dt;
    }
   }
  }
 }
}
[HarmonyPatch(typeof(AIController),"Awake")]
class AndroidAIInitialization
{
 static void Postfix(AIController __instance,ref float ___flockDistance)
 {___flockDistance=.4f;__instance.gameObject.AddComponent<AndroidAI>().Native=__instance;}
}
[HarmonyPatch(typeof(AIComponent),"Awake")]
class AndroidAIActorInitialization
{
 static void Postfix(AIComponent __instance)
 {__instance.gameObject.AddComponent<AndroidAIActor>().Actor=__instance;}
}
[HarmonyPatch(typeof(AIComponent),"Start")]
class AndroidAIActorStart{static bool Prefix()=>false;}
[HarmonyPatch(typeof(AIComponent),"OnDestroy")]
class AndroidAIActorDestroy{static bool Prefix()=>false;}
[HarmonyPatch(typeof(AIController),"FixedUpdate")]
class AndroidAINativeStep{static bool Prefix()=>false;}
[HarmonyPatch(typeof(AIController),"GetClosestAI")]
class AndroidAIClosest
{
 static bool Prefix(AIComponent ai,float ___flockDistance,Collider2D[] ____colliders,int ____layer,ref Transform __result)
 {
  __result=null;if(ai.gameObject.layer!=(int)TagLayerUtil.Enemy)return false;
  int count=Physics2D.OverlapCircleNonAlloc(ai.transform.position,___flockDistance,____colliders,____layer);
  if(count<2)return false;
  for(int i=0;i<count;i++)if(____colliders[i].gameObject!=ai.gameObject){__result=____colliders[i].transform;break;}
  return false;
 }
}
[HarmonyPatch(typeof(AIShootSpecial),nameof(AIShootSpecial.Use))]
class AndroidAIShoot
{
 static bool Prefix(AIComponent ai,Transform target,string ___projectileOPTag,float ___projectileSpeed,SoundEffectSO ___soundFX)
 {
  // All four shipped Android AIShootSpecial assets have rotateProjectile=false.
  // Source Use emits one projectile immediately; the older burst coroutine and
  // its serialized numRepeated/delayBetweenShots fields no longer exist there.
  var shot=ObjectPooler.SharedInstance.GetPooledObject(___projectileOPTag);
  shot.transform.position=ai.specialPoint.position;
  shot.GetComponent<MoveComponent2D>().vector=___projectileSpeed*(Vector2)(target.position-ai.specialPoint.position).normalized;
  ___soundFX?.Play();return false;
 }
}
[HarmonyPatch(typeof(MapInitializer),"Start")]
class AndroidNativeMapPhysics
{
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  int replaced=0;
  foreach(var instruction in instructions)
  {
   if(instruction.operand is MethodInfo method&&method.DeclaringType==typeof(Object)&&method.Name=="Instantiate"&&method.IsGenericMethod&&method.GetGenericArguments()[0]==typeof(GameObject)&&method.GetParameters().Length==1)
   {instruction.operand=AccessTools.Method(typeof(AndroidAIRecipes),nameof(AndroidAIRecipes.SpawnMap));replaced++;}
   yield return instruction;
  }
  if(replaced!=1)throw new System.InvalidOperationException("Unsupported native map instantiation boundary: "+replaced);
 }
}
