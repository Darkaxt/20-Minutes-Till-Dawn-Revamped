using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source Harmful is a kinematic, full-contact circle. Its disabled behaviour
// still receives Unity collision callbacks; do not add an enabled guard.
public sealed class AndroidThunderHitbox:MonoBehaviour
{
 public int Damage=22;
 void OnCollisionEnter2D(Collision2D collision)
 {
  var target=collision.gameObject;if(target.layer!=(int)TagLayerUtil.Enemy)return;
  var health=target.GetComponent<Health>();if(health!=null){if(AndroidAffixes.Has(target,3))Damage/=2;AndroidDamageRounding.ChangeHealth(health,-Damage,DamageType.Bullet);}
 }
}
public static class AndroidLightning
{
 static readonly ConditionalWeakTable<ThunderGenerator,List<int>> AreaTargets=new ConditionalWeakTable<ThunderGenerator,List<int>>();
 static GameObject Visual(ThunderGenerator owner,Vector3 position,int? damage,GameObject prefab,GameObject impactPrefab,ObjectPooler pool,SoundEffectSO sound)
 {
  var visual=pool.GetPooledObject(prefab.name);
  visual.transform.position=position+new Vector3(UnityEngine.Random.Range(-.05f,.05f),UnityEngine.Random.Range(-.05f,.05f));
  visual.transform.eulerAngles=new Vector3(0,0,UnityEngine.Random.Range(-45f,45f));
  var hitbox=visual.GetComponentInChildren<AndroidThunderHitbox>(true);
  if(hitbox==null)
  {
   var child=new GameObject("Harmful");child.SetActive(false);child.layer=(int)TagLayerUtil.PlayerProjectile;child.transform.SetParent(visual.transform,false);
   var body=child.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.useFullKinematicContacts=true;
   child.AddComponent<CircleCollider2D>().radius=.35f;hitbox=child.AddComponent<AndroidThunderHitbox>();child.SetActive(true);
  }
  hitbox.transform.localScale=Vector3.one*owner.sizeMultiplier;hitbox.enabled=damage.HasValue;
  if(damage.HasValue)hitbox.Damage=Mathf.FloorToInt(owner.damageMod.Modify(damage.Value));
  visual.SetActive(true);
  var impact=pool.GetPooledObject(impactPrefab.name);impact.transform.position=visual.transform.position;impact.transform.localScale=Vector3.one*owner.sizeMultiplier;impact.SetActive(true);
  visual.GetComponent<SpriteRenderer>().flipX=UnityEngine.Random.Range(0,1)==0;sound.Play();return visual;
 }
 internal static void Targeted(ThunderGenerator owner,GameObject target,int damage,GameObject prefab,GameObject impactPrefab,ObjectPooler pool,SoundEffectSO sound)
 {
  if(target==null)return;
  if(AndroidAffixes.Has(target,1))return;
  Visual(owner,target.transform.position,damage,prefab,impactPrefab,pool,sound);
  owner.PostNotification(ThunderGenerator.ThunderHitEvent,target);
 }
 internal static void Area(ThunderGenerator owner,Vector3 position,int damage,float radius,GameObject prefab,GameObject impactPrefab,ObjectPooler pool,SoundEffectSO sound)
 {
  Visual(owner,position,null,prefab,impactPrefab,pool,sound);
  var targets=AreaTargets.GetOrCreateValue(owner);targets.Clear();
  ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetMonsterInRange(targets,radius*owner.sizeMultiplier,position);
  foreach(int id in targets)
  {
   var target=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetFromID(id);if(target==null)continue;
   if(AndroidAffixes.Has(target,1))continue;
   var health=target.GetComponent<Health>();if(health!=null)AndroidDamageRounding.ChangeHealth(health,-damage,DamageType.Thunder);
   owner.PostNotification(ThunderGenerator.ThunderHitEvent,target);
  }
 }
}
[HarmonyPatch(typeof(ThunderGenerator),nameof(ThunderGenerator.GenerateAt),new[]{typeof(GameObject),typeof(int)})]
class AndroidTargetedThunder
{
 static bool Prefix(ThunderGenerator __instance,GameObject target,int damage,GameObject ___thunderPrefab,GameObject ___thunderImpactPrefab,ObjectPooler ___OP,SoundEffectSO ___soundFX)
 {AndroidLightning.Targeted(__instance,target,damage,___thunderPrefab,___thunderImpactPrefab,___OP,___soundFX);return false;}
}
[HarmonyPatch(typeof(ThunderGenerator),nameof(ThunderGenerator.GenerateAt),new[]{typeof(Vector3),typeof(int)})]
class AndroidAreaThunder
{
 static bool Prefix(ThunderGenerator __instance,Vector3 position,int damage,float ___baseAoE,GameObject ___thunderPrefab,GameObject ___thunderImpactPrefab,ObjectPooler ___OP,SoundEffectSO ___soundFX)
 {AndroidLightning.Area(__instance,position,damage,___baseAoE,___thunderPrefab,___thunderImpactPrefab,___OP,___soundFX);return false;}
}
[Serializable]
public sealed class AndroidElectroMastery:flanne.PerkSystem.Action
{
 public override void Activate(GameObject target)
 {var thunder=ThunderGenerator.SharedInstance;thunder.damageMod.AddFlatBonus(12);thunder.sizeMultiplier*=1.75f;}
}
