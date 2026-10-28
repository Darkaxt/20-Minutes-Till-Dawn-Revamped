using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// The retained native setter queues Rigidbody2D.rotation. Rendering/particle
// emission can run before physics publishes that pose (especially on reuse).
// Publish the same angle to the Transform at the existing angle boundary too.
[HarmonyPatch(typeof(Projectile),"set_angle")]
class ProjectileVisibleAngle
{
 static void Postfix(Projectile __instance,float __0)=>__instance.transform.rotation=Quaternion.AngleAxis(__0,Vector3.forward);
}

// Native has a base Projectile on this owned prefab; Android has MultiProjectile.
// Keep that source component identity when a pool tag or owner changes.
public sealed class AndroidMultiProjectile:MonoBehaviour{}

// Native's integer damage property already matches the ordinary source harm
// scalar. Moth has a separate live FreezeOrHarm consumer: source Projectile's
// serialized harm points there, so every producer/modifier must share it.
[HarmonyPatch(typeof(Projectile),"get_damage")]
class AndroidProjectileChildDamageRead
{
 static bool Prefix(Projectile __instance,ref float __result)
 {
  var harm=__instance.GetComponentInChildren<FreezeOrHarm>(true);
  if(harm==null)return true;
  __result=harm.damageAmount;return false;
 }
}
[HarmonyPatch(typeof(Projectile),"set_damage")]
class AndroidProjectileChildDamageWrite
{
 static void Postfix(Projectile __instance,int ____damage)
 {
  var harm=__instance.GetComponentInChildren<FreezeOrHarm>(true);
  if(harm!=null)harm.damageAmount=____damage;
 }
}

// The shared Android movement owner dispatches the source IMoveUpdate callback.
// This component owns the contact state that the older native Projectile lacks.
public sealed class AndroidProjectile:MonoBehaviour
{
 internal const DamageType MonsterDamage=(DamageType)10;
 static readonly int EnemyLayer=LayerMask.NameToLayer("Enemy"),LensLayer=LayerMask.NameToLayer("PlayerProjectileMod"),SporeLayer=LayerMask.NameToLayer("SporeFlower");
 static readonly int TriggerMask=(1<<EnemyLayer)|(1<<LensLayer)|(1<<SporeLayer);
 static readonly FieldInfo KnockbackMove=AccessTools.Field(typeof(Knockback),"myMove"),ImpactTag=AccessTools.Field(typeof(SpawnOnCollision),"objPoolTag"),LastImpact=AccessTools.Field(typeof(SpawnOnCollision),"lastImpactFX");
 static readonly List<Component> projectileTriggers=new List<Component>();
 internal Projectile Shot;internal MoveComponent2D Move;internal float Size=1,Radius;
 internal bool BulletEffect=true,IsBurn;internal int BurnDamage;
 bool manual,multi,changed,multiTriggered,shootingPet,freezePulse;int frame;
 internal bool HasMoveUpdate=>manual;
 readonly HashSet<Collider2D> hit=new HashSet<Collider2D>();readonly Collider2D[] multiHits=new Collider2D[30];
 Component[] ordered;string poolTag;
 void Awake(){Shot=GetComponent<Projectile>();Move=GetComponent<MoveComponent2D>();ordered=GetComponents<Component>();}
 internal static void ConfigurePrefab(GameObject prefab)
 {
  if(prefab.name=="PF_ShadowCloneAttack"&&prefab.GetComponent<AndroidMultiProjectile>()==null)prefab.AddComponent<AndroidMultiProjectile>();
  if(prefab.name=="PF_RevolverProjectile")prefab.GetComponent<MoveComponent2D>().drag=1;
  AndroidArrowGround.ConfigurePrefab(prefab);
 }
 void OnEnable(){if(!multi)hit.Clear();Array.Clear(multiHits,0,multiHits.Length);changed=false;multiTriggered=false;}
 internal void Configure(ProjectileRecipe recipe)
 {
  poolTag=recipe.objectPoolTag;var flags=recipe as AndroidProjectileRecipe;BulletEffect=flags?.BulletEffect??true;BurnDamage=flags?.BurnDamage??0;IsBurn=BurnDamage>0;
  freezePulse=GetComponentInChildren<FreezeOrHarm>(true)!=null;
  string summon=recipe.owner?.GetComponent<ShootingSummon>()?.SummonTypeID;
  multi=GetComponent<AndroidMultiProjectile>()!=null;shootingPet=summon=="GhostFriend"||summon=="Dragon";
  var circle=GetComponent<CircleCollider2D>();manual=circle!=null;Radius=circle!=null?circle.radius:0;
  if(poolTag=="PF_Fireball")Radius=.2f;
  if(multi)Radius=1.7f;
  Size=Mathf.Clamp(recipe.size,0,5);if(circle!=null)circle.enabled=false;
  changed=false;multiTriggered=false;Array.Clear(multiHits,0,multiHits.Length);if(!multi)hit.Clear();
 }
 internal void Step(float dt)
 {
  frame++;if(multi&&frame%3==0){frame=0;hit.Clear();}
  float radius=Radius*Size;Vector2 distance=dt*Move.vector,end=transform.position;
  int samples=Mathf.FloorToInt(distance.magnitude*.5f/radius);
  if(samples<2){Check(end,radius);return;}
  Vector2 pos=end-distance,step=Move.vector.normalized*(2*radius);
  for(int i=1;i<=samples;i++){pos+=step;Check(pos,radius);if(!isActiveAndEnabled)break;}
 }
 void Check(Vector2 pos,float radius)
 {
  if(multi)
  {
   var lens=Physics2D.OverlapCircle(pos,radius,1<<LensLayer);
   if(!changed&&lens!=null){changed=true;Enhance(lens.gameObject);return;}
   if(multiTriggered)return;
   Physics2D.OverlapCircleNonAlloc(pos,radius,multiHits,1<<EnemyLayer);
   // Source loops the whole30-slot buffer and only performs this query once.
   foreach(var collider in multiHits)if(collider!=null)Contact(collider.gameObject);
   multiTriggered=true;return;
  }
  var other=Physics2D.OverlapCircle(pos,radius,TriggerMask);if(other==null||!hit.Add(other))return;
  int layer=other.gameObject.layer;
  if(layer==LensLayer){if(!changed){changed=true;Enhance(other.gameObject);}return;}
  if(layer==SporeLayer){Enhance(other.gameObject);return;}
  Contact(other.gameObject);
 }
 void Enhance(GameObject target)
 {
  // Source uses one static IProjectileTrigger list, filled in component order.
  // Keep its live count/index behavior and clear only after dispatch completes.
  projectileTriggers.Clear();
  foreach(var component in target.GetComponents<Component>())
   if(component is BulletEnhanceSummon||component is SporeFlower)projectileTriggers.Add(component);
  for(int i=0;i<projectileTriggers.Count;i++)
  {
   var component=projectileTriggers[i];
   if(component is BulletEnhanceSummon lens)AndroidLens.Apply(lens,Shot);
   else if(component is SporeFlower flower)AndroidSpores.OnProjectile(flower);
  }
  projectileTriggers.Clear();
 }
 internal void Contact(GameObject target)
 {
  if(target.layer!=EnemyLayer)return;var health=target.GetComponent<Health>();if(health==null){Debug.LogError("Health == null:"+target.name);return;}
  if(poolTag=="PF_Fireball"){foreach(var item in ordered)if(item is Knockback knockback)Knockback(knockback,target);ElementalFire.Explode(Shot,target,BulletEffect);AndroidPools.Return(gameObject);return;}
  if(Shot is ExplosiveProjectile)
  {
   Shot.PostNotification(Projectile.ImpactEvent,target);
   if(Shot.piercing!=0||Shot.bounce!=0)
   {
    AndroidDamageRounding.ChangeHealth(health,-((int)Shot.damage/2),MonsterDamage);
    if(health.HP<=0)Shot.PostNotification(Projectile.KillEvent,target);
    if(Shot.piercing!=0)Shot.piercing--;else{Shot.bounce--;Bounce(false);}
   }
   else AndroidPools.Return(gameObject);
  }
  else
  {
   if(Shot.damage>=health.HP)Shot.PostNotification(Projectile.KillEvent,target);
   if(IsBurn)BurnSystem.SharedInstance.Burn(target,BurnDamage);
   if(BulletEffect)PlayerController.Instance.gameObject.PostNotification(Projectile.ImpactEvent,target);
   Shot.PostNotification(Projectile.TweakPierceBounce);
   if(Shot.piercing!=0)Shot.piercing--;
   else if(Shot.bounce!=0){hit.Clear();Shot.bounce--;Bounce(true);}
   else AndroidPools.Return(gameObject);
  }
  // Source invokes IColliderTrigger components after OnTrigger, even after
  // that callback returns this shot to the pool. Native Projectile embeds the
  // Harmful operation; restore its distinct position in the source sequence.
  bool delayedDamage=poolTag=="PF_Fireball"||poolTag=="PF_FireWave";
  if(!(Shot is ExplosiveProjectile)&&!delayedDamage&&!freezePulse)Damage(health);
  foreach(var component in ordered)
  {
   if(component is Knockback knockback){Knockback(knockback,target);if(delayedDamage)Damage(health);}
   else if(component is SpawnOnCollision spawn&&!shootingPet)Impact(spawn,target,health);
   else if(component is BurnOnCollision burn)BurnSystem.SharedInstance.Burn(target,burn.burnDamage);
  }
 }
 void Damage(Health target){if(AndroidAffixes.Has(target.gameObject,3))Shot.damage=(int)Shot.damage/2;AndroidDamageRounding.ChangeHealth(target,-(int)Shot.damage,DamageType.Bullet);}
 void Knockback(Knockback knockback,GameObject target)
 {
  var own=(MoveComponent2D)KnockbackMove.GetValue(knockback);
  AndroidContacts.Knockback(knockback,target,own);
 }
 void Impact(SpawnOnCollision spawn,GameObject target,Health health)
 {
  Vector3 position=(target.transform.position+transform.position)*.5f;
  var fx=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().Get((string)ImpactTag.GetValue(spawn),position);LastImpact.SetValue(spawn,fx);
  if(fx!=null)fx.transform.position=position;
 }
 void Bounce(bool notify)
 {
  float speed=Move.vector.magnitude;Vector2 reflected=Vector2.Reflect(transform.right,Move.vector).normalized*speed;
  var rotation=Quaternion.AngleAxis(UnityEngine.Random.Range(-45,45),Vector3.forward);
  Move.vector=((Vector2)(rotation*reflected)).normalized*speed;
  if(notify)PlayerController.Instance.StartCoroutine(BounceNotice(Shot));
 }
 static IEnumerator BounceNotice(Projectile shot){yield return new WaitForEndOfFrame();shot.PostNotification(Projectile.BounceEvent);}
}

[HarmonyPatch(typeof(Projectile),"SetSize")]
class AndroidProjectileSize
{
 static void Prefix(ref float __0)=>__0=Mathf.Clamp(__0,0,5);
 static void Postfix(Projectile __instance,float __0){var state=__instance.GetComponent<AndroidProjectile>();if(state!=null)state.Size=__0;}
}

// Source's ordinary physics callback passes a null Health to OnTrigger, which
// returns immediately. Its projectile sampler supplies the real Health through
// the separate contact dispatch implemented above.
[HarmonyPatch(typeof(SpawnOnCollision),"OnCollisionEnter2D")]
class AndroidImpactPhysicsCallback{static bool Prefix()=>false;}

[HarmonyPatch]
class AndroidProjectileContact
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Projectile),"OnCollisionEnter2D"),AccessTools.Method(typeof(ExplosiveProjectile),"OnCollisionEnter2D")};
 static bool Prefix(Projectile __instance,Collision2D __0){var state=__instance.GetComponent<AndroidProjectile>();if(state==null)return true;state.Contact(__0.gameObject);return false;}
}
[HarmonyPatch]
class AndroidProjectileNativeContacts
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Knockback),"OnCollisionEnter2D"),AccessTools.Method(typeof(SpawnOnCollision),"OnCollisionEnter2D"),AccessTools.Method(typeof(HarmfulOnContact),"OnCollisionEnter2D"),AccessTools.Method(typeof(HarmfulOnContact),"OnTriggerEnter2D")};
 static bool Prefix(Component __instance)=>__instance.GetComponent<AndroidProjectile>()==null;
}
