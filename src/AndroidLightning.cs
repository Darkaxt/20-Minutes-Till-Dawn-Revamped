using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Android GameObjectPool.Get pops the most recently released instance; native
// Windows rotates through its inactive list. The difference affects the damage
// retained by positional lightning, so bind only this pool's release order.
public sealed class AndroidThunderPool:MonoBehaviour
{
 readonly Stack<GameObject> returned=new Stack<GameObject>();
 internal GameObject Acquire(ObjectPooler native,string tag)=>returned.Count==0?native.GetPooledObject(tag):returned.Pop();
 internal void Release(GameObject visual)=>returned.Push(visual);
}
public sealed class AndroidThunderPoolMember:MonoBehaviour
{
 internal AndroidThunderPool Owner;
 void OnDisable(){if(Owner!=null)Owner.Release(gameObject);}
}
// Source Harmful is a kinematic, full-contact circle. Its disabled behaviour
// still receives Unity collision callbacks; do not add an enabled guard.
public sealed class AndroidThunderHitbox:MonoBehaviour
{
 public int Damage=22;
 void OnCollisionEnter2D(Collision2D collision)
 {
  var target=collision.gameObject;if(target.layer!=(int)TagLayerUtil.Enemy)return;
  var health=target.GetComponent<Health>();if(health!=null)AndroidLightning.ChangeHealth(health,-Damage,DamageType.Bullet);
 }
}
public static class AndroidLightning
{
 internal static void ChangeHealth(Health health,int change,DamageType type)
 {
  // Adapt negative source changes to Windows' positive damage API so existing
  // native damage modifiers/kill notifications keep their calling convention.
  if(change<0){health.TakeDamage(type,-change);return;}
  // The positional Android overload really passes positive changes. Preserve
  // type/global signed flooring and source heal/resurrection/event ordering.
  int value=AndroidDamageRounding.SignedModifiers(change,"Health.Tweak"+type+"Damage",health);
  value=AndroidDamageRounding.SignedModifiers(value,Health.TweakDamageEvent,health);
  int previous=health.HP;Plugin.SetField(health,"<HP>k__BackingField",Mathf.Clamp(previous+value,0,health.maxHP));
  health.onHealthChange.Invoke(health.HP);
  if(value<0)health.onHurt.Invoke(value);else if(value>0)health.onHeal.Invoke(value);
  if(previous==0)return;
  if(!health.isDead)health.PostNotification(AndroidSurvivingHurt.Event,type);
  else
  {health.onDeath.Invoke();health.PostNotification("Health."+type+"DamageKill",null);health.PostNotification(Health.DeathEvent,null);}
 }
 static GameObject Visual(ThunderGenerator owner,Vector3 position,int? damage,GameObject prefab,GameObject impactPrefab,ObjectPooler pool,SoundEffectSO sound)
 {
  var order=owner.GetComponent<AndroidThunderPool>();if(order==null)order=owner.gameObject.AddComponent<AndroidThunderPool>();
  var visual=order.Acquire(pool,prefab.name);
  var member=visual.GetComponent<AndroidThunderPoolMember>();if(member==null){member=visual.AddComponent<AndroidThunderPoolMember>();member.Owner=order;}
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
  Visual(owner,target.transform.position,damage,prefab,impactPrefab,pool,sound);
  owner.PostNotification(ThunderGenerator.ThunderHitEvent,target);
 }
 internal static void Area(ThunderGenerator owner,Vector3 position,int damage,float radius,GameObject prefab,GameObject impactPrefab,ObjectPooler pool,SoundEffectSO sound)
 {
  Visual(owner,position,null,prefab,impactPrefab,pool,sound);
  // Query the native monster pool once per actor, before callbacks can acquire
  // additional pooled effects or mutate its dictionaries.
  var targets=pool.pooledObjectsDictionary.Values.SelectMany(list=>list).Where(actor=>actor!=null&&actor.activeInHierarchy&&actor.layer==(int)TagLayerUtil.Enemy&&Vector2.Distance(actor.transform.position,position)<radius*owner.sizeMultiplier).Distinct().ToArray();
  foreach(var target in targets)
  {
   var health=target.GetComponent<Health>();if(health!=null)ChangeHealth(health,damage,DamageType.Thunder);
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
