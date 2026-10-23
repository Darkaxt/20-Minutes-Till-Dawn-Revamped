using HarmonyLib;
using UnityEngine;
using flanne;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

internal static class AndroidProjectileTemplates
{
 internal static bool OwnsEnemy(GameObject prefab)=>prefab.name=="EyeMonsterProjectile"||prefab.name=="PF_CultistProjectile";
 internal static void ConfigureEnemy(GameObject owned)
 {
  if(!OwnsEnemy(owned))return;
  Object.DestroyImmediate(owned.GetComponent<DisableOnCollision>());
  if(owned.name=="EyeMonsterProjectile")Plugin.SetField(owned.AddComponent<TimeToLive>(),"lifetime",6f);
  // Source Eye Projectile.Awake skips its sampler on EnemyProjectile. Its
  // physical handler accepts only monster layers excluded by this actor's
  // original collision matrix; PlayerCollider owns actual player contacts.
 }
 internal static GameObject Cannon(GameObject native)
 {
  // Prepare under an inactive persistent owner before any enable-time lifetime
  // or physics callbacks. Native gun initialization registers this actual name.
  var root=new GameObject("AndroidProjectileTemplates");root.SetActive(false);
  root.transform.SetParent(Plugin.Instance.transform,false);
  var prefab=Object.Instantiate(native,root.transform,false);prefab.name="PF_ChargeCannonProjectile";
  prefab.GetComponent<MoveComponent2D>().drag=3;
  var circle=prefab.GetComponent<CircleCollider2D>();circle.radius=.15f;circle.offset=Vector2.zero;circle.isTrigger=false;
  var body=prefab.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.useFullKinematicContacts=true;
  Plugin.SetField(prefab.GetComponent<TimeToLive>(),"lifetime",.8f);
  AccessTools.Field(typeof(Projectile),"trail").SetValue(prefab.GetComponent<Projectile>(),null);
  foreach(var trail in prefab.GetComponentsInChildren<TrailRenderer>(true))Object.DestroyImmediate(trail);
  Object.DestroyImmediate(prefab.GetComponent<SpawnOnCollision>());
  return prefab;
 }
}
