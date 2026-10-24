using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.CharacterPassives;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(ShadowClonePassive),"PerformSkill")]
class RevampedFirstClonePlacement
{
 // Detach the exact skill actor before activation. Its later source Start
 // keeps the same world pose because its local and world poses now coincide.
 // Other summons retain their original source parenting rules.
 static void Prefix(GameObject ___shadowClone)=>___shadowClone.transform.SetParent(null,true);
}

// Source StatsHolder.AddPower replaces the provider at the same key. Smash
// does not accumulate modifiers into every currently pooled tentacle.
public sealed class AndroidHasturProviders:MonoBehaviour
{
 public int Damage;
 public StatMod Cooldown;
 internal float ModifyCooldown(float value)=>Cooldown==null?value:Cooldown.ModifyInverse(value);
 internal static AndroidHasturProviders Get()
 {var player=PlayerController.Instance;var value=player.GetComponent<AndroidHasturProviders>();return value!=null?value:player.gameObject.AddComponent<AndroidHasturProviders>();}
}
[Serializable]
public sealed class AndroidHasturUpgrade:flanne.PerkSystem.Action
{
 public bool Speed;
 public override void Activate(GameObject target)
 {
  var providers=AndroidHasturProviders.Get();
  if(Speed){providers.Cooldown=new StatMod();providers.Cooldown.AddMultiplierBonus(1);}
  else providers.Damage=30;
 }
}
[HarmonyPatch]
class AndroidHasturRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);if(key!="hastur_perk_smash_name"&&key!="hastur_perk_frenzy_name")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidHasturUpgrade{Speed=key=="hastur_perk_frenzy_name"});
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
// These two original source hitboxes carry OnPlayer damage with no impact
// notification. Their native HarmfulOnContact otherwise uses untyped damage.
public sealed class AndroidCharacterHitbox:MonoBehaviour{}
[HarmonyPatch(typeof(SetHitboxActiveSummon),"Attack")]
class AndroidTentacleAttack
{
 static bool Prefix(SetHitboxActiveSummon __instance,HarmfulOnContact ___hitbox,float ___range,ref bool __result)
 {
  Vector2 position=__instance.transform.position;var target=AndroidSummonTargets.Random(___range,___range);__result=target!=null;if(!__result)return false;
  int extra=__instance.SummonTypeID=="Tentacle"?(PlayerController.Instance.GetComponent<AndroidHasturProviders>()?.Damage??0):0;
  ___hitbox.damageAmount=Mathf.FloorToInt(__instance.summonDamageMod.Modify(__instance.baseDamage+extra));
  if(___hitbox.GetComponent<AndroidCharacterHitbox>()==null)___hitbox.gameObject.AddComponent<AndroidCharacterHitbox>();
  ___hitbox.gameObject.layer=(int)TagLayerUtil.PlayerProjectile;___hitbox.GetComponent<Rigidbody2D>().useFullKinematicContacts=true;
  var direction=(Vector2)target.transform.position-position;___hitbox.transform.rotation=Quaternion.AngleAxis(Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg,Vector3.forward);___hitbox.gameObject.SetActive(true);return false;
 }
}
public sealed class AndroidHiddenBlade:MonoBehaviour
{
 ShadowClonePassive passive;
 GameObject knockback;
 internal void Connect(ShadowClonePassive owner)
 {
  passive=owner;knockback=new GameObject("Knockback");knockback.SetActive(false);knockback.layer=(int)TagLayerUtil.PlayerProjectile;knockback.transform.SetParent(transform,false);
  var body=knockback.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.useFullKinematicContacts=true;body.gravityScale=0;
  knockback.AddComponent<CircleCollider2D>().radius=1.7f;
  knockback.AddComponent<Knockback>().knockbackForce=4;
  var impact=knockback.AddComponent<SpawnOnCollision>();Plugin.SetField(impact,"hitTag","Enemy");Plugin.SetField(impact,"objPoolTag","BulletImpact");
  Plugin.SetField(knockback.AddComponent<TimeToLive>(),"lifetime",.1f);knockback.AddComponent<DisableOnInvisible>();
  passive.onEnd.AddListener(Ended);
 }
 void Ended(){knockback.SetActive(true);StartCoroutine(EndInvincible());}
 IEnumerator EndInvincible(){yield return new WaitForSeconds(.1f);PlayerController.Instance.playerHealth.isInvincible.UnFlip();}
 void OnDestroy(){passive.onEnd.RemoveListener(Ended);}
}
[HarmonyPatch(typeof(DamageOnShadowCloneDash),"Start")]
class AndroidHiddenBladeConnect
{
 static void Postfix(DamageOnShadowCloneDash __instance,Harmful ___harm,ShadowClonePassive ___shadowClone)
 {___harm.gameObject.AddComponent<AndroidCharacterHitbox>();__instance.gameObject.AddComponent<AndroidHiddenBlade>().Connect(___shadowClone);}
}
[HarmonyPatch(typeof(DamageOnShadowCloneDash),"OnShadowCloneStart")]
class AndroidHiddenBladeStart
{
 static bool Prefix(Harmful ___harm)
 {var player=PlayerController.Instance;player.playerHealth.isInvincible.Flip();___harm.damageAmount=Mathf.FloorToInt(player.gun.damage);___harm.gameObject.SetActive(true);return false;}
}
