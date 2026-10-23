using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

public sealed class AndroidFrostbite:MonoBehaviour
{
 // Windows has no Freeze category. Keep it distinct from its existing damage
 // kinds so source freeze damage cannot become a Bullet/Summon proc.
 internal const DamageType FreezeDamage=(DamageType)9;
 void Start()=>this.AddObserver(OnFreeze,FreezeSystem.InflictFreezeEvent);
 void OnDestroy()=>this.RemoveObserver(OnFreeze,FreezeSystem.InflictFreezeEvent);
 void OnFreeze(object sender,object args)
 {
  var target=(GameObject)args;var health=target.GetComponent<Health>();
  if(health==null){Debug.LogError("health == null:"+target.name);return;}
  int damage=Mathf.FloorToInt(health.maxHP*(target.tag.Contains("Champion")?.01f:.15f));
  AndroidDamageRounding.ChangeHealth(health,-damage,FreezeDamage);
 }
}
public sealed class AndroidAssassin:MonoBehaviour
{
 void Start()=>this.AddObserver(OnImpact,Projectile.ImpactEvent);
 void OnDestroy()=>this.RemoveObserver(OnImpact,Projectile.ImpactEvent);
 void OnImpact(object sender,object args)
 {
  // Source resolves a pooled transform ID. The native notification already
  // supplies that target GameObject; no damage-event or Champion predicate.
  var target=(GameObject)args;if(target.layer!=(int)TagLayerUtil.Enemy)return;
  var health=target.GetComponent<Health>();
  if((float)health.HP/health.maxHP<=.2f&&health.HP>=1)health.AutoKill();
 }
}
public sealed class AndroidShatterContact:MonoBehaviour
{
 public int Damage;
 void OnCollisionEnter2D(Collision2D collision)
 {
  var target=collision.gameObject;if(target.layer!=(int)TagLayerUtil.Enemy)return;
  var health=target.GetComponent<Health>();if(health!=null){if(AndroidAffixes.Has(target,3))Damage/=2;AndroidDamageRounding.ChangeHealth(health,-Damage,DamageType.Bullet);}
 }
}
public sealed class AndroidShatterPool:MonoBehaviour
{
 internal const string Tag="Revamped_AndroidShatter";
 GameObject prototype;ObjectPooler pool;
 internal void Prepare(GameObject original)
 {
  if(prototype!=null)return;
  pool=GetComponent<ObjectPooler>();
  prototype=Object.Instantiate(original);prototype.SetActive(false);prototype.name=Tag;prototype.transform.SetParent(transform,false);
  Object.DestroyImmediate(prototype.GetComponent<HarmfulOnContact>());prototype.AddComponent<AndroidShatterContact>();
  pool.AddObject(Tag,prototype,50);
 }
 internal void Cast(Health victim,SoundEffectSO sound)
 {
  // Source IsMax uses strictly greater-than, allowing the51st active actor.
  var visual=pool.GetPooledObject(Tag);if(visual==null)return;
  visual.transform.position=victim.transform.position;visual.GetComponent<AndroidShatterContact>().Damage=Mathf.FloorToInt(victim.maxHP*.07f);visual.SetActive(true);sound?.Play();
 }
}
public sealed class AndroidShatter:MonoBehaviour
{
 internal GameObject Prefab;internal SoundEffectSO Sound;AndroidShatterPool pool;
 void Start()
 {
  var native=ObjectPooler.SharedInstance;pool=native.GetComponent<AndroidShatterPool>();if(pool==null)pool=native.gameObject.AddComponent<AndroidShatterPool>();pool.Prepare(Prefab);
  this.AddObserver(OnDeath,Health.DeathEvent);
 }
 void OnDestroy()=>this.RemoveObserver(OnDeath,Health.DeathEvent);
 void OnDeath(object sender,object args)=>pool.Cast((Health)sender,Sound);
}
[Serializable]
public sealed class AndroidColdAcquire:flanne.PerkSystem.Action
{
 public int Kind;public GameObject Prefab;public SoundEffectSO Sound;
 public override void Activate(GameObject target)
 {
  var helper=new GameObject(Kind==0?"PF_DamageOnFreeze":Kind==1?"PF_IceShatterOnDeath":"PF_AutoKill");helper.transform.SetParent(PlayerController.Instance.transform,false);
  if(Kind==0)helper.AddComponent<AndroidFrostbite>();
  else if(Kind==2)helper.AddComponent<AndroidAssassin>();
  else{var shatter=helper.AddComponent<AndroidShatter>();shatter.Prefab=Prefab;shatter.Sound=Sound;}
 }
}
[HarmonyPatch]
class AndroidColdRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);int kind=key=="frostbite_name"?0:key=="shatter_name"?1:key=="assassin_name"?2:-1;if(kind<0)return;
  var effects=(PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(__instance);
  var original=AccessTools.Field(typeof(PerkEffect),"action").GetValue(effects[0]);if(original is AndroidColdAcquire)return;
  var action=new AndroidColdAcquire{Kind=kind};
  if(kind==1){action.Prefab=(GameObject)AccessTools.Field(original.GetType(),"shatterPrefab").GetValue(original);action.Sound=(SoundEffectSO)AccessTools.Field(original.GetType(),"soundFX").GetValue(original);}
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new flanne.PerkSystem.Triggers.InstantTrigger());Plugin.SetField(effect,"action",action);Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
