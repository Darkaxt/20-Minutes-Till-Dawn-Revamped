using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

[Serializable]
public sealed class AndroidHolyArtsAcquire:flanne.PerkSystem.Action
{
 public override void Activate(GameObject target)
 {
  // The source prefab contains only Transform and SmitePassive. Its visual
  // payload is the matching original Windows PF_SmiteFX asset.
  var fx=Resources.FindObjectsOfTypeAll<GameObject>().Single(g=>g.name=="PF_SmiteFX"&&!g.scene.IsValid());
  var helper=new GameObject("PF_SmitePassive");helper.transform.SetParent(PlayerController.Instance.transform,false);
  var smite=helper.AddComponent<SmitePassive>();Plugin.SetField(smite,"smiteFXPrefab",fx);
  Plugin.SetField(smite,"range",4f);Plugin.SetField(smite,"baseDamage",20);Plugin.SetField(smite,"damageBonusPerHP",10);
  // The Android serialized soundFX is null; preserve that source behavior.
 }
}
[HarmonyPatch]
class AndroidHolyArtsRecipe
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  if(RunResume.Key(__instance)!="holy_arts_name")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidHolyArtsAcquire());
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
[HarmonyPatch]
class AndroidSmiteDamage
{
 static MethodBase TargetMethod()=>AccessTools.Method(typeof(SmitePassive),"SmiteCR");
 static bool Prefix(SmitePassive __instance,GameObject ___smiteFXPrefab,float ___range,int ___baseDamage,SoundEffectSO ___soundFX,ObjectPooler ___OP,ref IEnumerator __result)
 {__result=Smite(__instance,___smiteFXPrefab,___range,___baseDamage,___soundFX,___OP);return false;}
 static IEnumerator Smite(SmitePassive owner,GameObject prefab,float range,int baseDamage,SoundEffectSO sound,ObjectPooler pool)
 {
  yield return new WaitForSeconds(.1f);
  var enemies=Physics2D.OverlapCircleAll(owner.transform.position,range,1<<(int)TagLayerUtil.Enemy);sound?.Play();
  foreach(var enemy in enemies)
  {
   var effect=pool.GetPooledObject(prefab.name);
   var offset=new Vector3(UnityEngine.Random.Range(-.05f,.05f),UnityEngine.Random.Range(-.05f,.05f),0);
   float angle=UnityEngine.Random.Range(-45f,45f);
   if(effect==null)continue;
   effect.transform.position=enemy.transform.position+offset;effect.transform.eulerAngles=new Vector3(0,0,angle);
   effect.transform.position=enemy.transform.position;effect.SetActive(true);
  }
  yield return new WaitForSeconds(.1f);
  foreach(var enemy in enemies)
  {
   var health=enemy.GetComponent<Health>();bool alive=health.HP>0&&!health.isDead;
   int damage=baseDamage.NotifyModifiers(SmitePassive.SmiteTweakDamageNotification,owner);
   AndroidDamageRounding.ChangeHealth(health,-damage,DamageType.Smite);
   if(alive&&health.isDead)owner.PostNotification(SmitePassive.SmiteKillNotification);
  }
 }
}
