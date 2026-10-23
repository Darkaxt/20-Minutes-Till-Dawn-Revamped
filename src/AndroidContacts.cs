using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

public sealed class AndroidHarmfulKind:MonoBehaviour
{
 // Public so Unity copies the recovered category with its native prefab.
 public DamageType Kind=DamageType.Bullet;
 internal const DamageType Powerup=(DamageType)11,MagicRetrieving=(DamageType)13;
}
static class AndroidContacts
{
 static readonly HashSet<string> Names=new HashSet<string>{"PF_DeerHitbox","PF_LunaBlackHoleDamage","Damage","PF_BombExplosion","PF_ThunderCallerProjectileRetrieving","PF_MagicBowProjectileRetrieving","Shockwave","PF_FireWave","PF_GrenadeExplosion","PF_SporeExplosion","BigBoomerExplosion","AttackHitbox","PF_OverloadExplosion","PF_Shatter","PF_Fireball","BoomerExplosion"};
 static string Name(GameObject actor)=>actor.name.Replace("(Clone)","");
 internal static bool OwnsTemplate(GameObject prefab)=>prefab.name=="PF_GrenadeExplosion";
 internal static void ConfigureTemplate(GameObject prefab)
 {
  // Source GrenadeExplosion posts its impact from Harmful itself. The native
  // prefab's separate, unfiltered collision sender would post a second impact
  // (and post for rejected contacts). Remove it from the inactive owned copy,
  // before pool members are cloned, rather than destroying an asset component.
  if(OwnsTemplate(prefab))Object.DestroyImmediate(prefab.GetComponent<TriggerOnHitEffects>());
 }
 internal static void ConfigurePrefab(GameObject prefab)
 {
  if(prefab.name=="PF_FlameCannonProjectile"||prefab.name=="PF_MeteorGunProjectile")
   Plugin.SetField(prefab.GetComponent<SpawnOnCollision>(),"objPoolTag","FireExplosionSmall");
  foreach(var harm in prefab.GetComponentsInChildren<HarmfulOnContact>(true))
  {
   string name=Name(harm.gameObject);if(!Names.Contains(name))continue;
   Configure(harm,name);
   harm.procOnHit=name=="PF_GrenadeExplosion"||name=="PF_ThunderCallerProjectileRetrieving"||name=="PF_SporeExplosion";
  }
 }
 internal static AndroidHarmfulKind Ensure(HarmfulOnContact harm)
 {
  var state=harm.GetComponent<AndroidHarmfulKind>();if(state!=null)return state;
  string name=Name(harm.gameObject);
  if(Names.Contains(name))return Configure(harm,name);
  // Both source constructors explicitly assign EnumDamage.OnPlayer (Bullet).
  return harm.gameObject.AddComponent<AndroidHarmfulKind>();
 }
 static AndroidHarmfulKind Configure(HarmfulOnContact harm,string name)
 {
  var state=harm.GetComponent<AndroidHarmfulKind>();if(state==null)state=harm.gameObject.AddComponent<AndroidHarmfulKind>();
  state.Kind=name=="PF_SporeExplosion"?AndroidHarmfulKind.Powerup:name=="PF_MagicBowProjectileRetrieving"?AndroidHarmfulKind.MagicRetrieving:DamageType.Bullet;
  harm.gameObject.layer=name=="Damage"?0:name=="BigBoomerExplosion"||name=="BoomerExplosion"?LayerMask.NameToLayer("HarmEverythingExplosion"):(int)TagLayerUtil.PlayerProjectile;
  foreach(var collider in harm.GetComponents<Collider2D>())collider.isTrigger=false;
  var body=harm.GetComponent<Rigidbody2D>();
  // The source Boomer explosion has this body; Windows only has a trigger.
  if(name=="BoomerExplosion"&&body==null)body=harm.gameObject.AddComponent<Rigidbody2D>();
  if(body!=null){body.bodyType=RigidbodyType2D.Kinematic;body.useFullKinematicContacts=true;}
  if(name=="PF_Fireball")harm.GetComponent<CircleCollider2D>().radius=.2f;
  return state;
 }
 internal static void Harm(HarmfulOnContact harm,GameObject target)
 {
  if(target.layer!=(int)TagLayerUtil.Enemy)return;
  var health=target.GetComponent<Health>();if(health==null||target.GetComponent<PlayerHealth>()!=null)return;
  var kind=Ensure(harm).Kind;AndroidAffixes.Modify(harm,target);
  AndroidDamageRounding.ChangeHealth(health,-harm.damageAmount,kind);
  if(harm.procOnHit)
  {
   // Source IDs are represented by native GameObjects at observer boundaries.
   // Only OnPlayer damage sends the impact event from the player object.
   if(kind==DamageType.Bullet)PlayerController.Instance.gameObject.PostNotification(Projectile.ImpactEvent,target);
   else harm.PostNotification(Projectile.ImpactEvent,target);
  }
 }
 internal static void Knockback(Knockback knockback,GameObject target,MoveComponent2D own)
 {
  if(target.layer!=(int)TagLayerUtil.Enemy)return;
  var move=target.GetComponent<MoveComponent2D>();if(move==null||move.knockbackImmune)return;
  Vector2 direction=own!=null?own.vectorLastFrame:(Vector2)(target.transform.position-knockback.transform.position);
  move.vector=knockback.knockbackForce*direction.normalized;
 }
}
[HarmonyPatch(typeof(HarmfulOnContact),"Start")]
class AndroidHarmfulInitialization
{static bool Prefix(HarmfulOnContact __instance){AndroidContacts.Ensure(__instance);return false;}}
[HarmonyPatch(typeof(HarmfulOnContact),"Harm")]
class AndroidHarmfulApplication
{static bool Prefix(HarmfulOnContact __instance,GameObject __0){AndroidContacts.Harm(__instance,__0);return false;}}
[HarmonyPatch(typeof(HarmfulOnContact),"OnCollisionEnter2D")]
class AndroidHarmfulCollision
{
 static bool Prefix(HarmfulOnContact __instance,Collision2D __0)
 {if(__instance.GetComponent<AndroidProjectile>()==null)AndroidContacts.Harm(__instance,__0.gameObject);return false;}
}
[HarmonyPatch(typeof(HarmfulOnContact),"OnTriggerEnter2D")]
class AndroidHarmfulUnityTrigger{static bool Prefix()=>false;}
[HarmonyPatch(typeof(Knockback),"OnCollisionEnter2D")]
class AndroidKnockbackCollision
{
 static bool Prefix(Knockback __instance,Collision2D __0,MoveComponent2D ___myMove)
 {if(__instance.GetComponent<AndroidProjectile>()==null)AndroidContacts.Knockback(__instance,__0.gameObject,___myMove);return false;}
}
