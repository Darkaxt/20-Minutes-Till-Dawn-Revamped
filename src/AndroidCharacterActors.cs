using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PowerupSystem;
namespace TwentyMinutes.Modernization;

internal static class AndroidCharacterActors
{
 internal static void ConfigurePrefab(GameObject prefab)
 {
  // The source Spirit has this lifetime component; Windows' prefab lacks it.
  if(prefab.name=="SpiritSummon"&&prefab.GetComponent<TimeToLive>()==null)
   Plugin.SetField(prefab.AddComponent<TimeToLive>(),"lifetime",5f);
  // Implosion has Bullet-classified Harmful with PostNotification=false,
  // the same contact contract as the existing character hitbox adapter.
  if(prefab.name=="PF_LunaBlackHoleDamage"&&prefab.GetComponent<AndroidCharacterHitbox>()==null)
   prefab.AddComponent<AndroidCharacterHitbox>();
 }
}
[HarmonyPatch(typeof(SpawnHarmfulObjOnNotification),"Start")]
class AndroidNotificationHarmOwner
{
 static bool Prefix(SpawnHarmfulObjOnNotification __instance,GameObject ___prefab,int ___amountToInitInObjPool,string ___notification,ref ObjectPooler ___OP)
 {
  ___OP=ObjectPooler.SharedInstance;
  ___OP.GetComponent<AndroidPools>().Register(___prefab.name,___prefab,___amountToInitInObjPool);
  var handler=(Action<object,object>)Delegate.CreateDelegate(typeof(Action<object,object>),__instance,AccessTools.Method(typeof(SpawnHarmfulObjOnNotification),"OnNotification"));
  __instance.AddObserver(handler,___notification);return false;
 }
}
[HarmonyPatch(typeof(SpawnHarmfulObjOnNotification),nameof(SpawnHarmfulObjOnNotification.Spawn))]
class AndroidNotificationHarmSpawn
{
 static bool Prefix(Vector3 spawnPos,GameObject ___prefab,float ___damageMultiplier,ObjectPooler ___OP)
 {
  var actor=___OP.GetPooledObject(___prefab.name);actor.transform.position=spawnPos;
  actor.GetComponent<Harmful>().damageAmount=Mathf.FloorToInt(PlayerController.Instance.gun.damage*___damageMultiplier);
  actor.SetActive(true);return false;
 }
}
[HarmonyPatch(typeof(ProjectileOnShoot),"SpawnProjectile")]
class AndroidCharacterProjectileSpawn
{
 static bool Prefix(ProjectileOnShoot __instance,Vector2 direction,GameObject ___projectilePrefab,float ___speed,bool ___setToGunDamage,float ___percentOfGunDamage,bool ___projHasPeriodicDamage,float ___periodicDamageFrequency)
 {
  var player=PlayerController.Instance;
  var actor=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().Get(___projectilePrefab.name,__instance.transform.position,null);
  var projectile=actor.GetComponent<Projectile>();
  // The damage-property bridge owns the moth's serialized child Harmful too.
  projectile.damage=player.stats[StatType.BulletDamage].Modify(___projectilePrefab.GetComponentInChildren<Harmful>().damageAmount);
  float size=player.stats[StatType.ProjectileSize].Modify(1);projectile.size=size;
  projectile.vector=___speed*direction.normalized;projectile.angle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
  projectile.bounce=0;
  if(___setToGunDamage)projectile.damage=___percentOfGunDamage*player.gun.damage;
  var state=actor.GetComponent<AndroidProjectile>();if(state==null)state=actor.AddComponent<AndroidProjectile>();
  state.Configure(new AndroidProjectileRecipe{objectPoolTag=___projectilePrefab.name,BulletEffect=true,BurnDamage=0,size=size});
  if(___projHasPeriodicDamage)
  {var periodic=actor.GetComponent<PeriodicGameObjectActivator>();if(periodic!=null)periodic.timeBetweenActivations=___periodicDamageFrequency;}
  return false;
 }
}
[HarmonyPatch(typeof(ProjectileSummon),"OnEnable")]
class AndroidSpiritEnable{static bool Prefix()=>false;}
[HarmonyPatch(typeof(SummonOnEnemyDeath),"OnDeath")]
class AndroidSpiritDeathSpawn
{
 static bool Prefix(GameObject ___summonPrefab,SoundEffectSO ___soundFX,object sender)
 {
  // Source observes every Health death. It does not filter the Enemy tag,
  // reparent the spirit to the player, or explicitly activate it a second time.
  var dead=(sender as Health).gameObject;
  var actor=ObjectPooler.SharedInstance.GetPooledObject(___summonPrefab.name);
  actor.transform.position=dead.transform.position;
  ___soundFX?.Play();return false;
 }
}
[HarmonyPatch(typeof(ProjectileSummon),"OnCollisionEnter2D")]
class AndroidSpiritContact
{
 static bool Prefix(ProjectileSummon __instance,Collision2D __0,int ___baseDamage)
 {
  var target=__0.gameObject;
  if(target.layer==(int)TagLayerUtil.PlayerProjectileMod)return false;
  var health=target.GetComponent<Health>();
  if(health!=null)AndroidDamageRounding.ChangeHealth(health,Mathf.FloorToInt(-__instance.summonDamageMod.Modify(___baseDamage)),DamageType.Summon);
  AndroidPools.Return(__instance.gameObject);return false;
 }
}
[HarmonyPatch(typeof(SeekEnemy),"GetNewTarget")]
class AndroidSeekerTarget
{
 static bool Prefix(float ___seekDistanceX,float ___seekDistanceY,ref Transform ____target)
 {
  var target=AndroidSummonTargets.Random(___seekDistanceX,___seekDistanceY);
  ____target=target==null?PlayerController.Instance.transform:target.transform;return false;
 }
}
