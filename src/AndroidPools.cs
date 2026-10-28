using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// Android's shipped GameObjectPool<Transform> contract, at the native pool
// boundary. Keep native prefab identities and public lists for their consumers.
public sealed class AndroidPools:MonoBehaviour
{
 sealed class Pool
 {
  internal GameObject Prefab;internal Transform Parent;internal int Maximum,Visible;
  internal readonly Stack<GameObject> Returned=new Stack<GameObject>();
  internal readonly List<GameObject> All=new List<GameObject>(),Active=new List<GameObject>(),Hidden=new List<GameObject>();
  internal readonly Dictionary<GameObject,SpriteRenderer> EvictedRenderers=new Dictionary<GameObject,SpriteRenderer>();
  internal int Count=>All.Count-Returned.Count;
 }
 readonly Dictionary<string,Pool> pools=new Dictionary<string,Pool>();
 readonly List<Pool> monsterPools=new List<Pool>();
 readonly List<GameObject> randomTargets=new List<GameObject>();
 readonly Dictionary<int,GameObject> byID=new Dictionary<int,GameObject>();
 readonly Dictionary<GameObject,GameObject> sourceTemplates=new Dictionary<GameObject,GameObject>();
 readonly Dictionary<int,Transform> timedReleases=new Dictionary<int,Transform>();
 readonly List<int> expiredReleases=new List<int>();
 Transform sourceTemplateRoot;
 internal GameObject SourceTemplate(GameObject original)
 {
  if(!AndroidProjectileTemplates.OwnsEnemy(original)&&!AndroidLobbedProjectiles.OwnsTemplate(original)&&!AndroidContacts.OwnsTemplate(original)&&!original.GetComponentsInChildren<AIComponent>(true).Any(AndroidAIRecipes.IsBodyless))return original;
  if(sourceTemplates.TryGetValue(original,out var owned))return owned;
  if(sourceTemplateRoot==null)
  {var root=new GameObject("AndroidActorTemplates");root.SetActive(false);root.transform.SetParent(transform,false);sourceTemplateRoot=root.transform;}
  owned=Object.Instantiate(original,sourceTemplateRoot,false);owned.name=original.name;
  AndroidAIRecipes.ConfigureTemplate(owned);AndroidLobbedProjectiles.ConfigureTemplate(owned);AndroidProjectileTemplates.ConfigureEnemy(owned);AndroidContacts.ConfigureTemplate(owned);sourceTemplates.Add(original,owned);return owned;
 }
 internal GameObject GetFromID(int id)=>byID.TryGetValue(id,out var value)?value:null;
 // Source get_countActive is allocated minus returned, including hidden
 // allocations. It is not an activeInHierarchy or visible-list count.
 internal int CountAllocated(string tag)=>pools.TryGetValue(tag,out var pool)?pool.Count:int.MinValue;
 internal int CountCollectableDrops(string tag)
 {
  if(!pools.TryGetValue(tag,out var pool))return 0;
  int count=0;
  foreach(var actor in pool.Active)
   if(actor!=null&&actor.activeInHierarchy&&actor.GetComponent<AndroidCollectIgnore>()==null)count++;
  return count;
 }
 internal void GetMonsterInRange(List<int> targets,float radius,Vector2 center)
 {
  foreach(var pool in monsterPools)
  {
   foreach(var actor in pool.Active)
    if(actor!=null&&Vector2.Distance(actor.transform.position,center)<radius)targets.Add(actor.GetInstanceID());
  }
 }
 internal GameObject GetRandomEnemy(float x,float y)
 {
  randomTargets.Clear();
  foreach(var pool in monsterPools)
  {
   var center=PlayerController.Instance.transform.position;
   foreach(var actor in pool.Active)
    if(Mathf.Abs(actor.transform.position.x-center.x)<x&&Mathf.Abs(actor.transform.position.y-center.y)<y)randomTargets.Add(actor);
  }
  return randomTargets.Count==0?null:randomTargets[UnityEngine.Random.Range(0,randomTargets.Count)];
 }
 ObjectPooler native;
 internal void Initialize(ObjectPooler owner)
 {
  native=owner;ObjectPooler.SharedInstance=owner;
  owner.itemDictionary=new Dictionary<string,ObjectPoolItem>();owner.pooledObjectsDictionary=new Dictionary<string,List<GameObject>>();owner.pooledObjects=new List<GameObject>();
  var originals=Resources.FindObjectsOfTypeAll<GameObject>().Where(g=>!g.scene.IsValid()).GroupBy(g=>g.name).ToDictionary(g=>g.Key,g=>g.First());
  // Windows creates one-shot Sound objects instead of shipping this source
  // pool prefab. Construct its default 2D AudioSource under the scene owner.
  originals["Audio Source"]=AndroidGameplaySound.Template(transform);
  // Source target indices follow its serialized scene-table registration order.
  // Resolve the same owned counterparts as before, before native-only entries.
  foreach(var item in AndroidPoolTables.Items)
  {
   var counterpart=owner.itemsToPool.FirstOrDefault(nativeItem=>nativeItem.tag==item.Tag);
   if(counterpart!=null)Register(item.Tag,counterpart.objectToPool);
   else if(originals.TryGetValue(item.Prefab,out var prefab))Register(item.Tag,prefab);
  }
  foreach(var item in owner.itemsToPool)Register(item.tag,item.objectToPool);
 }
 internal void Register(string tag,GameObject prefab,int? sourceVisible=null)
 {
  if(pools.ContainsKey(tag))return;
  AndroidProjectile.ConfigurePrefab(prefab);
  AndroidChestLifecycle.Configure(prefab);
  AndroidDevilPickupLifecycle.Configure(prefab);
  AndroidCharacterActors.ConfigurePrefab(prefab);
  AndroidAIRecipes.ConfigurePrefab(prefab);
  AndroidContacts.ConfigurePrefab(prefab);
  BossArenaLifetime.ConfigurePrefab(prefab);
  AndroidDamagePopup.ConfigurePrefab(prefab);
  ColorfulProjectileVisuals.Configure(prefab);
  if(prefab.GetComponent<flanne.Pickups.ChestPickup>()!=null&&prefab.GetComponent<ChestDirectionIndicator>()==null)prefab.AddComponent<ChestDirectionIndicator>();
  prefab=SourceTemplate(prefab);
  var source=AndroidPoolTables.Items.FirstOrDefault(i=>i.Tag==tag)??AndroidPoolTables.Items.FirstOrDefault(i=>i.Prefab==prefab.name);
  int maximum=source?.Maximum??99999,visible=sourceVisible??source?.Visible??-1,initial=source?.Initial??0;
  // These source helpers register explicit create limits outside the
  // scene table. Native's old third argument meant preallocation instead.
  if(tag==AndroidShatterPool.Tag)maximum=50;
  if(tag=="BurnFX")maximum=50;
  if(tag=="PF_FreezeFXSmall"||tag=="PF_FreezeFXLarge")maximum=35;
  var root=new GameObject(tag);root.transform.SetParent(transform);root.transform.localPosition=Vector3.zero;
  var pool=new Pool{Prefab=prefab,Parent=root.transform,Maximum=maximum,Visible=visible};pools.Add(tag,pool);
  if(prefab.layer==(int)TagLayerUtil.Enemy)monsterPools.Add(pool);
  native.itemDictionary.Add(tag,new ObjectPoolItem(tag,prefab,0,true));native.pooledObjectsDictionary.Add(tag,pool.All);native.pooledObjects=pool.All;
  // The source warm-up fetches and releases in the same loop; it does not
  // retain initial simultaneously allocated objects.
  for(int i=0;i<initial;i++){var value=Acquire(pool,Vector3.zero,pool.Parent);Release(value);}
 }
 internal GameObject Get(string tag)
 {
  if(!pools.TryGetValue(tag,out var pool)||pool.Count>=pool.Maximum)return null;
  var value=Acquire(pool,Vector3.zero,pool.Parent);value.name=tag;return value;
 }
 internal void TimeOutRelease(Transform target,int duration)
 {
  int end=unchecked(AndroidGameClock.Milliseconds+duration);
  while(timedReleases.ContainsKey(end))end=unchecked(end+1);
  timedReleases.Add(end,target);
 }
 void Update()
 {
  // Follow the source ObjectPooler component's Unity message eligibility.
  if(!native.enabled)return;
  expiredReleases.Clear();
  foreach(var entry in timedReleases)
   if(AndroidGameClock.Milliseconds>=entry.Key)
   {expiredReleases.Add(entry.Key);if(entry.Value!=null)Release(entry.Value.gameObject);}
  foreach(int key in expiredReleases)timedReleases.Remove(key);
 }
 internal GameObject Get(string tag,Vector3 position,Transform parent)
 {
  if(!pools.TryGetValue(tag,out var pool)||pool.Count>=pool.Maximum)return null;
  var value=Acquire(pool,position,parent);value.name=tag;return value;
 }
 internal GameObject Get(string tag,Vector3 position)
 {
  if(!pools.TryGetValue(tag,out var pool)||pool.Count>=pool.Maximum)return null;
  var value=Acquire(pool,position,pool.Parent);value.name=tag;return value;
 }
 GameObject Acquire(Pool pool,Vector3 position,Transform parent)
 {
  GameObject value;
  if(pool.Returned.Count>0)
  {value=pool.Returned.Pop();value.transform.position=position;var trail=value.GetComponentInChildren<TrailRenderer>();if(trail!=null)trail.Clear();}
  else
  {value=Object.Instantiate(pool.Prefab,parent);AndroidPickup.Attach(value);value.name=pool.Prefab.name;value.transform.position=position;value.transform.localRotation=Quaternion.identity;pool.All.Add(value);}
  if(pool.EvictedRenderers.TryGetValue(value,out var evicted)){evicted.enabled=true;pool.EvictedRenderers.Remove(value);}
  value.SetActive(true);
  if(value.transform.parent!=parent)value.transform.SetParent(parent);
  if(pool.Visible>0&&pool.Active.Count>=pool.Visible)
  {var oldest=pool.Active[0];var renderer=oldest.GetComponentInChildren<SpriteRenderer>();if(renderer!=null&&renderer.enabled){renderer.enabled=false;pool.EvictedRenderers[oldest]=renderer;}pool.Hidden.Add(oldest);pool.Active.RemoveAt(0);}
  pool.Active.Add(value);byID[value.GetInstanceID()]=value;return value;
 }
 internal void Release(GameObject value)
 {
  if(value==null||!value.activeSelf)return;
  // GameObjectPoolMap.Release selects by the current Transform name. It
  // does not require membership in an instance-ownership registry.
  if(!pools.TryGetValue(value.name,out var pool)){value.SetActive(false);return;}
  ReleaseMember(value,pool);
 }
 static void ReleaseMember(GameObject value,Pool pool)
 {
  // Source deactivation callbacks run before stack insertion and list removal.
  value.SetActive(false);pool.Returned.Push(value);
  if(value.transform.parent!=pool.Parent)value.transform.SetParent(pool.Parent);
  pool.Active.Remove(value);pool.Hidden.Remove(value);
 }
 internal void ReleaseAll()
 {
  // Source walks each live pool, active entries backward, then hidden entries
  // backward. It calls the underlying release even for an inactive member.
  foreach(var pool in pools.Values)
  {
   for(int i=pool.Active.Count-1;i>=0;i--)ReleaseMember(pool.Active[i],pool);
   for(int i=pool.Hidden.Count-1;i>=0;i--)ReleaseMember(pool.Hidden[i],pool);
  }
 }
 internal static void Clear()=>ObjectPooler.SharedInstance.GetComponent<AndroidPools>().ReleaseAll();
 internal static void SetActive(GameObject value,bool active)
 {
  if(active){value.SetActive(true);return;}
  ObjectPooler.SharedInstance.GetComponent<AndroidPools>().Release(value);
 }
 internal static void Return(GameObject value)=>SetActive(value,false);
}
[HarmonyPatch(typeof(TimeToLive),"Deactivate")]
class AndroidTimedPoolRelease
{static bool Prefix(TimeToLive __instance){AndroidPools.Return(__instance.gameObject);return false;}}
[HarmonyPatch(typeof(ObjectPooler),"Awake")]
class AndroidPoolInitialization
{static bool Prefix(ObjectPooler __instance){__instance.gameObject.AddComponent<AndroidPools>().Initialize(__instance);return false;}}
[HarmonyPatch(typeof(ObjectPooler),nameof(ObjectPooler.AddObject))]
class AndroidPoolRegistration
{static bool Prefix(ObjectPooler __instance,string tag,GameObject GO){__instance.GetComponent<AndroidPools>().Register(tag,GO);return false;}}
[HarmonyPatch(typeof(ObjectPooler),nameof(ObjectPooler.GetPooledObject))]
class AndroidPoolAcquisition
{static bool Prefix(ObjectPooler __instance,string tag,ref GameObject __result){__result=__instance.GetComponent<AndroidPools>().Get(tag);return false;}}

// Source SpawnFromObjectPool.Spawn (0x1999490) skips an exhausted
// effect pool. This is a source branch, so the following XP/drop still runs.
[HarmonyPatch(typeof(SpawnFromObjectPool),nameof(SpawnFromObjectPool.Spawn))]
class AndroidDeathDropPoolConsumer
{
 static bool Prefix(SpawnFromObjectPool __instance,string objectPoolTag)
 {
  var pool=ObjectPooler.SharedInstance;var enhanced=__instance.GetComponent<EnhancedEnemy>();
  int copies=enhanced!=null&&pool.itemDictionary.TryGetValue(objectPoolTag,out var item)&&item.objectToPool.GetComponent<flanne.Pickups.XPPickup>()!=null?enhanced.XpCopies:1;
  for(int i=0;i<copies;i++){var value=pool.GetPooledObject(objectPoolTag);if(value!=null)value.transform.position=__instance.transform.position;}
  return false;
 }
}
[HarmonyPatch(typeof(SpawnPrefabFromObjectPool),"Start")]
class AndroidPrefabDropPoolOwner
{
 static readonly HashSet<string> SourceOwners=new HashSet<string>{"BigBoomer","PF_LunaBlackHole","PF_HeadshotProjectile","PF_CursedFX"};
 static bool Prefix(SpawnPrefabFromObjectPool __instance,GameObject ___prefab,int ___amountToInitInObjPool,ref ObjectPooler ___OP,ref bool ___isInitialized)
 {
  string name=AndroidPoolTables.PrefabName(__instance.name.Replace("(Clone)",""));
  if(!SourceOwners.Contains(name))return true;
  ___OP=ObjectPooler.SharedInstance;___OP.GetComponent<AndroidPools>().Register(___prefab.name,___prefab,___amountToInitInObjPool);
  ___isInitialized=true;return false;
 }
}

// Adapt remaining native numeric-popup calls to the source PopDamage entry.
// Reached Android Health.Hurt calls that entry directly.
[HarmonyPatch(typeof(DamagePopupSpawner),nameof(DamagePopupSpawner.OnDamageTaken))]
class AndroidDamagePopupPoolConsumer
{
 static bool Prefix(DamagePopupSpawner __instance,int amount)
 {
  if(amount!=0)AndroidDamagePopup.PopDamage(Mathf.Abs(amount),__instance.transform);
  return false;
 }
}

// Original source callers of ObjectPooler.Release have native SetActive(false)
// counterparts. Route those exact component families, including their compiler
// generated coroutines, through the same source name-based release owner.
// A missing name follows the source's plain deactivation branch.
[HarmonyPatch]
class AndroidPoolReleaseCallers
{
 static readonly Type[] Owners={typeof(TimeToLive),typeof(DisableAction),typeof(DisableOnInvisible),typeof(DisableOnCollision),typeof(AreaProjectile),typeof(EnemyAreaProjectile),typeof(ProjectileSummon),typeof(flanne.Pickups.Pickup),typeof(flanne.Pickups.ChestPickup),typeof(flanne.Pickups.DevilDealPickup),typeof(FreezeSystem),typeof(CurseSystem),typeof(BulletGlyph),typeof(flanne.RuneSystem.ProjectileShieldRune),typeof(flanne.WorldTextPopup)};
 static IEnumerable<Type> Family(Type owner)=>new[]{owner}.Concat(owner.GetNestedTypes(BindingFlags.Public|BindingFlags.NonPublic).SelectMany(Family));
 static IEnumerable<MethodBase> TargetMethods()=>Owners.SelectMany(Family).SelectMany(t=>t.GetMethods(BindingFlags.DeclaredOnly|BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)).Where(m=>m.GetMethodBody()!=null);
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  var original=AccessTools.Method(typeof(GameObject),nameof(GameObject.SetActive));
  foreach(var instruction in instructions){if(instruction.Calls(original)){instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(AndroidPools),nameof(AndroidPools.SetActive));}yield return instruction;}
 }
}
