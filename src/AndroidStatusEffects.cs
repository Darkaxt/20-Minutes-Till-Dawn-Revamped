using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.RuneSystem;
using flanne.PerkSystem.Actions;
namespace TwentyMinutes.Modernization;

// Ordinary world status effects are not frozen checkpoint actors. Their native
// system owns the coroutines and their source event/physics lifecycle.
public sealed class AndroidFreezeState:MonoBehaviour
{
 sealed class Entry{internal GameObject Target;internal float Remaining;}
 readonly List<Entry> entries=new List<Entry>();
 internal bool Contains(GameObject target)=>entries.Exists(e=>e.Target==target);
 internal void Apply(FreezeSystem owner,GameObject target,float duration)
 {
  if(target==null)return;var ai=target.GetComponent<AIComponent>();
  if(ai==null){Debug.Log("ai == null:"+target.name);return;}
  if(AndroidAffixes.Has(target,2))return;
  float basis=target.CompareTag("EnemyChampion")?duration/10:duration;
  var entry=entries.Find(e=>e.Target==target);
  if(entry==null)
  {entry=new Entry{Target=target,Remaining=owner.durationMod.Modify(basis)};owner.StartCoroutine(Freeze(owner,entry));}
  else if(basis>entry.Remaining)entry.Remaining=owner.durationMod.Modify(basis);
  ((SoundEffectSO)AccessTools.Field(typeof(FreezeSystem),"soundFX").GetValue(owner))?.Play();
 }
 IEnumerator Freeze(FreezeSystem owner,Entry entry)
 {
  owner.PostNotification(FreezeSystem.InflictFreezeEvent,entry.Target);
  var ai=entry.Target.GetComponent<AIComponent>();if(ai!=null)ai.frozen=true;
  var animator=entry.Target.GetComponent<Animator>();if(animator!=null)animator.speed=0;
  entries.Add(entry);
  var field=entry.Target.CompareTag("EnemyChampion")?"freezeFXLargePrefab":"freezeFXPrefab";
  var prefab=(GameObject)AccessTools.Field(typeof(FreezeSystem),field).GetValue(owner);
  var visual=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().Get(prefab.name,entry.Target.transform.position,entry.Target.transform);
  if(visual!=null)visual.transform.localPosition=Vector3.zero;
  while(entry.Remaining>0&&entry.Target.activeInHierarchy){yield return null;entry.Remaining-=Time.deltaTime;}
  ai.frozen=false;if(animator!=null)animator.speed=1;entries.Remove(entry);
  if(visual!=null)AndroidPools.Return(visual);
 }
}
[HarmonyPatch(typeof(FreezeSystem),"Start")]
class AndroidFreezeOwner{static void Postfix(FreezeSystem __instance)=>__instance.gameObject.AddComponent<AndroidFreezeState>();}
[HarmonyPatch(typeof(FreezeSystem),nameof(FreezeSystem.Freeze))]
class AndroidFreezeApplication
{
 internal static void Apply(FreezeSystem owner,GameObject target,float duration)=>owner.GetComponent<AndroidFreezeState>().Apply(owner,target,duration);
 static bool Prefix(FreezeSystem __instance,GameObject target){Apply(__instance,target,1.5f);return false;}
}
[HarmonyPatch(typeof(FreezeSystem),nameof(FreezeSystem.IsFrozen))]
class AndroidFreezeQuery
{static bool Prefix(FreezeSystem __instance,GameObject target,ref bool __result){__result=__instance.GetComponent<AndroidFreezeState>().Contains(target);return false;}}

[HarmonyPatch(typeof(FreezeOnCollision),"OnCollisionEnter2D")]
class AndroidFreezeCollision
{static bool Prefix(Collision2D __0,string ___hitTag){if(__0.gameObject.tag.Contains(___hitTag))AndroidFreezeApplication.Apply(FreezeSystem.SharedInstance,__0.gameObject,3);return false;}}
[HarmonyPatch(typeof(FreezeOrHarm),"OnCollisionEnter2D")]
class AndroidFreezeOrDamage
{
 static bool Prefix(FreezeOrHarm __instance,Collision2D __0)
 {
  var target=__0.gameObject;var health=target.GetComponent<Health>();if(health==null)return false;
  var freeze=FreezeSystem.SharedInstance;
  if(freeze.IsFrozen(target))AndroidDamageRounding.ChangeHealth(health,-__instance.damageAmount,AndroidFrostbite.FreezeDamage);
  else AndroidFreezeApplication.Apply(freeze,target,3);return false;
 }
}
[HarmonyPatch(typeof(BurnFreezeOnHitRune),"OnImpact")]
class AndroidMysticismFreeze
{
 static bool Prefix(BurnFreezeOnHitRune __instance,object args,int ___level)
 {
  var target=(GameObject)args;if(target.layer!=(int)TagLayerUtil.Enemy)return false;
  if(UnityEngine.Random.Range(0f,1f)<__instance.chancePerLevel*___level)BurnSystem.SharedInstance.Burn(target,__instance.burnDamge);
  if(UnityEngine.Random.Range(0f,1f)<__instance.chancePerLevel*___level)AndroidFreezeApplication.Apply(FreezeSystem.SharedInstance,target,__instance.freezeDuration);
  return false;
 }
}

[HarmonyPatch]
class AndroidFreezeHitLifecycle
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(FreezeOnHit),"Start"),AccessTools.Method(typeof(FreezeOnHit),"OnDestroy")};
 static bool Prefix(FreezeOnHit __instance,MethodBase __originalMethod)
 {
  var action=(Action<object,object>)Delegate.CreateDelegate(typeof(Action<object,object>),__instance,AccessTools.Method(typeof(FreezeOnHit),"OnImpact"));
  if(__originalMethod.Name=="Start")__instance.AddObserver(action,Projectile.ImpactEvent);else __instance.RemoveObserver(action,Projectile.ImpactEvent);return false;
 }
}
[HarmonyPatch(typeof(FreezeOnHit),"OnImpact")]
class AndroidFreezeHit
{
 static bool Prefix(FreezeOnHit __instance,object args)
 {
  var target=(GameObject)args;
  if(target.layer==(int)TagLayerUtil.Enemy&&UnityEngine.Random.Range(0f,1f)<__instance.chanceToHit)AndroidFreezeApplication.Apply(FreezeSystem.SharedInstance,target,__instance.freezeDuration);
  return false;
 }
}

[HarmonyPatch(typeof(CurseSystem),"Awake")]
class AndroidCurseBaseDuration
{static void Prefix(CurseSystem __instance)=>__instance.duration=.75f;}

[HarmonyPatch(typeof(CurseSystem),"Start")]
class AndroidCurseInitialization
{
 static bool Prefix(GameObject ___curseFXPrefab,ref List<Health> ____cursedTargets)
 {
  ObjectPooler.SharedInstance.GetComponent<AndroidPools>().Register(___curseFXPrefab.name,___curseFXPrefab,-1);
  ____cursedTargets=new List<Health>();return false;
 }
}

// The retained native property has an int ABI. Its callers still receive that
// type, using the source live player; actual source float consumers use their
// final calculation directly, including the curse coroutine below.
[HarmonyPatch(typeof(CurseSystem),"get_curseDamage")]
class AndroidCurseDamageValue
{
 static bool Prefix(CurseSystem __instance,ref int __result)
 {__result=Mathf.FloorToInt(PlayerController.Instance.gun.damage*__instance.curseDamageMultiplier);return false;}
}

[HarmonyPatch(typeof(CurseSystem),nameof(CurseSystem.Curse))]
class AndroidCurseApplication
{
 static bool Prefix(CurseSystem __instance,GameObject target,List<Health> ____cursedTargets,GameObject ___curseFXPrefab)
 {
  var health=target.GetComponent<Health>();if(health==null||____cursedTargets.Contains(health))return false;
  __instance.StartCoroutine(Curse(__instance,health,____cursedTargets,___curseFXPrefab));return false;
 }
 static IEnumerator Curse(CurseSystem owner,Health health,List<Health> targets,GameObject prefab)
 {
  targets.Add(health);
  var visual=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().Get(prefab.name,health.transform.position,health.transform);
  yield return new WaitForSeconds(owner.duration);targets.Remove(health);
  if(health.gameObject.activeSelf)
  {
   AndroidDamageRounding.ChangeHealth(health,Mathf.FloorToInt(-PlayerController.Instance.gun.damage*owner.curseDamageMultiplier),DamageType.Curse);
   if(health.isDead)owner.PostNotification(CurseSystem.CurseKillNotification,health.gameObject);
  }
  // Source dereferences its uncapped curse visual at this boundary.
  AndroidPools.Return(visual.transform.gameObject);
 }
}

// Source Napalm uses gun damage directly. Native's identically named action
// instead reads curseDamage and couples the burn to unrelated curse upgrades.
[HarmonyPatch(typeof(ApplyBurnEqualToBulletDamageAction),nameof(ApplyBurnEqualToBulletDamageAction.Activate))]
class AndroidNapalmBurnDamage
{
 static bool Prefix(GameObject target,float ___damageMultiplier)
 {BurnSystem.SharedInstance.Burn(target,Mathf.FloorToInt(PlayerController.Instance.gun.damage*___damageMultiplier));return false;}
}
