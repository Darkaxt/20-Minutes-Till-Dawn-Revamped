using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PowerupSystem;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

public sealed class AndroidFanFire:MonoBehaviour
{
 public int Amount=10;public float DamageMultiplier=.15f,Delay=.05f;
 ProjectileFactory factory;
 void Start()
 {factory=ProjectileFactory.SharedInstance;PlayerController.Instance.ammo.OnAmmoChanged.AddListener(Changed);}
 void OnDestroy()=>PlayerController.Instance.ammo.OnAmmoChanged.RemoveListener(Changed);
 void Changed(int amount){if(amount==0)StartCoroutine(Spray());}
 IEnumerator Spray()
 {
  Vector2 initial=Vector2.zero;
  while(initial==Vector2.zero)initial=new Vector2(UnityEngine.Random.Range(-1f,1f),UnityEngine.Random.Range(-1f,1f));
  yield return new WaitForSeconds(PlayerController.Instance.gun.shotCooldown);
  for(int i=0;i<Amount;i++)
  {
   Vector2 direction=initial.Rotate((float)i/Amount*360);
   factory.SpawnProjectile(PlayerController.Instance.gun.GetProjectileRecipe(),direction,transform.position,DamageMultiplier,false);
   PlayerController.Instance.gun.gunData.gunshotSFX?.Play();
   yield return new WaitForSeconds(Delay);
  }
 }
}
public sealed class AndroidIceShards:MonoBehaviour
{
 public GameObject Prefab;public SoundEffectSO Sound;public int Amount=3;
 void Start()
 {
  ObjectPooler.SharedInstance.GetComponent<AndroidPools>().Register(Prefab.name,Prefab,-1);
  PlayerController.Instance.ammo.OnAmmoChanged.AddListener(Changed);
 }
 void OnDestroy()=>PlayerController.Instance.ammo.OnAmmoChanged.RemoveListener(Changed);
 void Changed(int amount)
 {
  if(amount!=0)return;
  for(int i=0;i<Amount;i++)
  {
   var actor=ObjectPooler.SharedInstance.GetPooledObject(Prefab.name);
   actor.transform.position=PlayerController.Instance.transform.position;
   actor.GetComponent<SeekEnemy>(); // Source lookup is deliberately unused.
   var move=actor.GetComponent<MoveComponent2D>();
   if(move!=null)move.vector=new Vector2(UnityEngine.Random.Range(-1f,1f),UnityEngine.Random.Range(-1f,1f)).normalized;
   actor.SetActive(true);
  }
  Sound?.Play();
 }
}
[Serializable]
public sealed class AndroidWeaponPerkAttachment:flanne.PerkSystem.Action
{
 public string Key;
 static GameObject Prefab(string name)=>Resources.FindObjectsOfTypeAll<GameObject>().Single(p=>!p.scene.IsValid()&&p.name==name);
 static SoundEffectSO Sound(string name)=>Resources.FindObjectsOfTypeAll<SoundEffectSO>().Single(s=>s.name==name);
 public override void Activate(GameObject target)
 {
  var root=new GameObject(Key=="fire_starter_name"?"PF_FireballGenerator":Key=="fan_fire_name"?"PF_SprayOnLastAmmo":"PF_SpawnIcicleOnLastAmmo");
  root.transform.SetParent(PlayerController.Instance.transform,false);
  MonoBehaviour helper;
  if(Key=="fire_starter_name")
  {
   // Keep the native mouse/controller aiming and matching four-shot counter.
   // SpawnProjectile already enters the shared source actor/contact adapter.
   var fire=root.AddComponent<ProjectileOnShoot>();helper=fire;
   Plugin.SetField(fire,"projectilePrefab",Prefab("PF_Fireball"));Plugin.SetField(fire,"speed",5f);Plugin.SetField(fire,"inaccuracy",45f);
   fire.numProjectiles=1;Plugin.SetField(fire,"shotsPerAttack",4);Plugin.SetField(fire,"soundFX",Sound("FireballWoosh_SFX"));
   Plugin.SetField(fire,"setToGunDamage",false);Plugin.SetField(fire,"percentOfGunDamage",0f);Plugin.SetField(fire,"projHasPeriodicDamage",false);Plugin.SetField(fire,"periodicDamageFrequency",0f);
  }
  else if(Key=="fan_fire_name")helper=root.AddComponent<AndroidFanFire>();
  else
  {
   var ice=root.AddComponent<AndroidIceShards>();ice.Prefab=Prefab("PF_Icicle");ice.Sound=AndroidPerkSounds.Icicle();helper=ice;
  }
  // These source helpers do not save their partial shot counter or coroutine.
  if(RunResume.Restoring){helper.enabled=false;RunRecorder.Instance.SourceStarts.Add(helper);}
 }
}
[HarmonyPatch]
class AndroidWeaponPerkRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);
  if(key!="fire_starter_name"&&key!="fan_fire_name"&&key!="ice_shards_name")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidWeaponPerkAttachment{Key=key});
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
