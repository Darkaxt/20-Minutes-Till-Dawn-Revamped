using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.RuneSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

internal static class RuneMechanics
{
 internal static void ElementalInflict(GameObject target)
 {
  foreach(var rune in PlayerController.Instance.GetComponentsInChildren<ElementalBarrageRune>())
   AccessTools.Method(typeof(ElementalBarrageRune),"OnInflict").Invoke(rune,new object[]{null,target});
 }
 internal static int SummonFamilies()=>Object.FindObjectsOfType<Summon>().Where(s=>s.SummonTypeID!="SeismicWard").Select(s=>s.SummonTypeID).Distinct().Count();
 internal static void RefreshDedication()
 {if(PlayerController.Instance!=null)foreach(var rune in PlayerController.Instance.GetComponentsInChildren<DedicationRune>())AccessTools.Method(typeof(DedicationRune),"CheckSummons").Invoke(rune,null);}
 internal static void WildMagic(SpreadStatusRune rune,Health dead,DamageType kind,int rank,float chance)
 {
  bool curse=kind==DamageType.Curse||((List<Health>)AccessTools.Field(typeof(CurseSystem),"_cursedTargets").GetValue(CurseSystem.Instance)).Contains(dead);
  bool smite=kind==DamageType.Smite;
  if((!curse&&!smite)||UnityEngine.Random.Range(0f,1f)>=chance*rank)return;
  var targets=Physics2D.OverlapCircleAll(dead.transform.position,1f,1<<(int)TagLayerUtil.Enemy).Select(c=>c.GetComponent<Health>()).Where(h=>h!=null&&h!=dead&&!h.isDead&&h.HP>0).Distinct().ToArray();
  if(curse)foreach(var target in targets)CurseSystem.Instance.Curse(target.gameObject);
  if(smite)
  {
   var passive=PlayerController.Instance.GetComponentInChildren<SmitePassive>();
   var onHit=PlayerController.Instance.GetComponentInChildren<SmiteOnHit>();
   MonoBehaviour source=(MonoBehaviour)passive??onHit;
   if(source!=null)rune.StartCoroutine(Echo(source,targets));
  }
 }
 static IEnumerator Echo(MonoBehaviour source,Health[] targets)
 {
  yield return new WaitForSeconds(.1f);
  var prefab=(GameObject)AccessTools.Field(source.GetType(),"smiteFXPrefab").GetValue(source);
  foreach(var h in targets)if(h!=null&&h.gameObject.activeInHierarchy&&h.HP>0)
  {var fx=ObjectPooler.SharedInstance.GetPooledObject(prefab.name);fx.transform.position=h.transform.position;fx.SetActive(true);}
  yield return new WaitForSeconds(.1f);
  foreach(var h in targets)if(h!=null&&h.gameObject.activeInHierarchy&&h.HP>0)
  {
   int damage=(int)AccessTools.Field(source.GetType(),"baseDamage").GetValue(source);
   if(source is SmitePassive)damage=damage.NotifyModifiers(SmitePassive.SmiteTweakDamageNotification,source);
   AndroidDamageRounding.ChangeHealth(h,-damage,DamageType.Smite);
   if(h.isDead&&source is SmitePassive)source.PostNotification(SmitePassive.SmiteKillNotification);
  }
 }
}
[HarmonyPatch(typeof(SpreadStatusRune),"OnDeath")]
class WildMagicNonElemental
{
 static bool Prefix(SpreadStatusRune __instance,object sender,object args,int ___level,float ___spreadChancePerLevel)
 {RuneMechanics.WildMagic(__instance,(Health)sender,args is DamageType kind?kind:DamageType.Bullet,___level,___spreadChancePerLevel);return false;}
}
[HarmonyPatch(typeof(DedicationRune),"CheckSummons")]
class DedicationFamilies
{
 static bool Prefix(DedicationRune __instance)
 {AccessTools.Method(typeof(DedicationRune),RuneMechanics.SummonFamilies()==1?"Activate":"Deactivate").Invoke(__instance,null);return false;}
}
[HarmonyPatch(typeof(Summon),"Start")]
class DedicationNewFamily
{
 static void Postfix(Summon __instance)=>__instance.gameObject.AddComponent<RuneSummonFamily>();
}
public sealed class RuneSummonFamily:MonoBehaviour
{void OnEnable()=>RuneMechanics.RefreshDedication();void OnDisable()=>RuneMechanics.RefreshDedication();}
[HarmonyPatch(typeof(RuneData),"get_description")]
class RevisedRuneDescriptions
{
 static void Postfix(RuneData __instance,ref string __result)
 {
  switch(__instance.name)
  {
   case "WildMagicRuneData":__result="On an enemy's death, a 5% chance per rank to spread its Curse or echo its killing Smite to nearby enemies. Requires the corresponding effect in your build.";break;
   case "ElementalBarrageRuneData":__result="After 50 Burn, Freeze, electrical stun or wind disorientation applications, gain +50% Fire Rate and Reload Speed for 1 second per rank. Then cool down for 10 seconds.";break;
   case "DedicationRuneData":__result="With exactly one offensive summon type, gain +10% Summon Damage and Attack Speed per rank. Paired summons count as one type; Seismic Ward does not count.";break;
  }
 }
}
