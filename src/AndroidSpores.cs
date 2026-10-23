using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

public sealed class AndroidSporeTargets:MonoBehaviour
{internal readonly List<int> IDs=new List<int>();}
static class AndroidSpores
{
 static readonly MethodInfo Launch=AccessTools.Method(typeof(SporeFlower),"LaunchSpore"),Spray=AccessTools.Method(typeof(SporeFlower),"SpraySpores");
 static readonly FieldInfo Animation=AccessTools.Field(typeof(AttackingSummon),"animator"),Counter=AccessTools.Field(typeof(SporeFlower),"_hitCtr"),Threshold=AccessTools.Field(typeof(SporeFlower),"sprayThreshold");
 internal static void OnProjectile(SporeFlower flower)
 {
  ((Animator)Animation.GetValue(flower)).SetTrigger("Attack");
  int count=(int)Counter.GetValue(flower)+1,threshold=(int)Threshold.GetValue(flower);Counter.SetValue(flower,count);
  if(count>=threshold){Counter.SetValue(flower,count-threshold);Spray.Invoke(flower,null);}
 }
 internal static void LaunchAt(SporeFlower flower,Vector2 target,float duration)=>Launch.Invoke(flower,new object[]{target,duration});
 internal static List<int> Targets(SporeFlower flower,float range)
 {
  var ids=flower.GetComponent<AndroidSporeTargets>().IDs;ids.Clear();
  ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetMonsterInRange(ids,range,flower.transform.position);return ids;
 }
}
[HarmonyPatch(typeof(SporeFlower),"Init")]
class AndroidSporeInitialization
{static void Postfix(SporeFlower __instance){if(__instance.GetComponent<AndroidSporeTargets>()==null)__instance.gameObject.AddComponent<AndroidSporeTargets>();}}
[HarmonyPatch(typeof(SporeFlower),"Attack")]
class AndroidSporeAttack
{
 static bool Prefix(SporeFlower __instance,float ___range,ref bool __result)
 {
  var ids=AndroidSpores.Targets(__instance,___range);__result=false;if(ids.Count==0)return false;
  var actor=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetFromID(ids[Random.Range(0,ids.Count)]);if(actor==null)return false;
  AndroidSpores.LaunchAt(__instance,actor.transform.position,.7f);__result=true;return false;
 }
}
[HarmonyPatch(typeof(SporeFlower),"SpraySpores")]
class AndroidSporeSpray
{
 static bool Prefix(SporeFlower __instance,float ___range,int ___sprayAmount)
 {
  var ids=AndroidSpores.Targets(__instance,___range);var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();int count=ids.Count;
  for(int i=0;i<Mathf.Min(___sprayAmount,count);i++)
  {
   var actor=pool.GetFromID(ids[i]);if(actor!=null)AndroidSpores.LaunchAt(__instance,actor.transform.position,.5f+Random.Range(-.2f,.2f));
  }
  for(int i=count;i<___sprayAmount;i++)
  {
   Vector2 target=(Vector2)__instance.transform.position+new Vector2(Random.Range(-1f,1f),Random.Range(-1f,1f)).normalized*Random.Range(1f,___range);
   AndroidSpores.LaunchAt(__instance,target,.7f+Random.Range(-.2f,.2f));
  }
  return false;
 }
}
[HarmonyPatch(typeof(SporeFlower),"OnCollisionEnter2D")]
class AndroidSporePhysicalCollision{static bool Prefix()=>false;}
