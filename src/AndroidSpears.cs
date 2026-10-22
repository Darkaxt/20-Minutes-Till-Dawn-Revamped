using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Actions;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

// Source global coefficients belong to the current player/build. Keep owned
// persistent spear references when the native stab temporarily detaches them.
public sealed class AndroidSpearBonuses:MonoBehaviour
{
 internal int HpBonus,SoulBonus;
 internal readonly List<StabbingSummon> Spears=new List<StabbingSummon>();
 internal static AndroidSpearBonuses Get(PlayerController player)
 {var owner=player.GetComponent<AndroidSpearBonuses>();return owner!=null?owner:player.gameObject.AddComponent<AndroidSpearBonuses>();}
}
[HarmonyPatch(typeof(AttachPrefabToPlayerAction),nameof(AttachPrefabToPlayerAction.Activate))]
class AndroidSpearRegistration
{
 static void Postfix(GameObject ___prefab)
 {
  if(___prefab.GetComponentInChildren<StabbingSummon>(true)==null)return;
  var player=PlayerController.Instance;var owner=AndroidSpearBonuses.Get(player);
  foreach(var spear in player.GetComponentsInChildren<StabbingSummon>(true))if(!owner.Spears.Contains(spear))owner.Spears.Add(spear);
 }
}
[Serializable]
public sealed class AndroidSpearUpgrade:flanne.PerkSystem.Action
{
 public bool Soul;
 public override void Activate(GameObject target)
 {var owner=AndroidSpearBonuses.Get(PlayerController.Instance);if(Soul)owner.SoulBonus=15;else owner.HpBonus+=10;}
}
[HarmonyPatch]
class AndroidSpearRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);if(key!="holy_spear_name"&&key!="soul_knight_name")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidSpearUpgrade{Soul=key=="soul_knight_name"});
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
[HarmonyPatch(typeof(StabbingSummon),"OnCollisionEnter2D")]
class AndroidSpearContact
{
 static bool Prefix(StabbingSummon __instance,Collision2D other,int ___baseDamage)
 {
  // Android's Monster/VisibleInFogMonster collision layers map to the native
  // combat Enemy layer; Windows keeps fog rendering on separate objects.
  var actor=other.gameObject;if(actor.layer!=(int)TagLayerUtil.Enemy)return false;var health=actor.GetComponent<Health>();if(health==null)return false;
  var player=PlayerController.Instance;var owner=player.GetComponent<AndroidSpearBonuses>();
  float amount=__instance.summonDamageMod.Modify(___baseDamage)+(owner!=null?player.playerHealth.maxHP*owner.HpBonus+player.playerHealth.shp*owner.SoulBonus:0);
  int damage=Mathf.FloorToInt(amount);if(CustomMode.Active)damage=Mathf.FloorToInt(damage*CustomMode.Get("summon:"+__instance.SummonTypeID+":damage"));
  AndroidDamageRounding.ChangeHealth(health,-damage,DamageType.Summon);return false;
 }
}
[HarmonyPatch(typeof(StabbingSummon),"Attack")]
class AndroidSpearAttack
{
 static readonly MethodInfo Stab=AccessTools.Method(typeof(StabbingSummon),"StabCR");
 static bool Prefix(StabbingSummon __instance,float ___range,ref IEnumerator ____stabbingCoroutine,ref bool __result)
 {
  __result=false;if(____stabbingCoroutine!=null)return false;
  var center=PlayerController.Instance.transform.position;float radius=Mathf.Abs(___range);
  var targets=ObjectPooler.SharedInstance.pooledObjectsDictionary.Values.SelectMany(list=>list).Where(actor=>actor!=null&&actor.activeInHierarchy&&actor.layer==(int)TagLayerUtil.Enemy&&Mathf.Abs(actor.transform.position.x-center.x)<radius&&Mathf.Abs(actor.transform.position.y-center.y)<radius).Distinct().ToArray();
  if(targets.Length==0)return false;
  Vector2 origin=__instance.transform.position,direction=(Vector2)targets[UnityEngine.Random.Range(0,targets.Length)].transform.position-origin;
  var destination=origin+direction.normalized*radius;____stabbingCoroutine=(IEnumerator)Stab.Invoke(__instance,new object[]{destination});__instance.StartCoroutine(____stabbingCoroutine);__result=true;return false;
 }
}
[HarmonyPatch(typeof(AttackingSummon),"get_finalAttackCooldown")]
class AndroidSpearCooldown
{
 static bool Prefix(AttackingSummon __instance,float ___attackCooldown,ref float __result)
 {
  if(!(__instance is StabbingSummon))return true;
  __result=Mathf.Clamp(__instance.summonAtkSpdMod.ModifyInverse(___attackCooldown)/__instance.attackSpeedMod.Modify(1),.2f,float.MaxValue);return false;
 }
}
[HarmonyPatch(typeof(AttackingSummon),"Update")]
class AndroidSpearInterval
{
 static readonly MethodInfo Cooldown=AccessTools.PropertyGetter(typeof(AttackingSummon),"finalAttackCooldown"),Attack=AccessTools.Method(typeof(StabbingSummon),"Attack");
 static bool Prefix(AttackingSummon __instance,ref float ____timer,Animator ___animator,SoundEffectSO ___attackSoundFX)
 {
  if(!(__instance is StabbingSummon))return true;
  ____timer+=Time.deltaTime;float interval=(float)Cooldown.Invoke(__instance,null);if(____timer<interval)return false;
  ____timer-=interval;if((bool)Attack.Invoke(__instance,null)){___animator?.SetTrigger("Attack");___attackSoundFX?.Play();}return false;
 }
}
