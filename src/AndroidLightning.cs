using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// One victim set spans the initial splash and its chain. Native visuals have
// no damage callback: all producer overloads enter this radial sampler.
public static class AndroidLightning
{
 internal static int LastVictims;
 internal static float Falloff(float distance,float radius)=>Mathf.Lerp(1,.35f,Mathf.Clamp01(distance/radius));
 static void Visual(ThunderGenerator owner,Vector3 position,GameObject prefab,GameObject impactPrefab,ObjectPooler pool,SoundEffectSO sound)
 {
  var visual=pool.GetPooledObject(prefab.name);visual.transform.position=position;visual.transform.eulerAngles=new Vector3(0,0,UnityEngine.Random.Range(-45f,45f));visual.SetActive(true);
  var impact=pool.GetPooledObject(impactPrefab.name);impact.transform.position=position;impact.transform.localScale=Vector3.one*owner.sizeMultiplier;impact.SetActive(true);sound.Play();
 }
 internal static bool Eligible(GameObject target)=>target!=null&&target.activeInHierarchy&&target.layer==(int)TagLayerUtil.Enemy&&!AndroidAffixes.Has(target,1)&&target.GetComponent<Health>() is Health h&&!h.isDead;
 static void Hit(ThunderGenerator owner,GameObject target,int damage,HashSet<int> visited,DamageType kind)
 {
  if(!Eligible(target)||!visited.Add(target.GetInstanceID()))return;
  int value=AndroidAffixes.Has(target,3)?damage/2:damage;AndroidDamageRounding.ChangeHealth(target.GetComponent<Health>(),-value,kind);
  if(Eligible(target))ElementalControl.Get(target).Electrocute();owner.PostNotification(ThunderGenerator.ThunderHitEvent,target);
 }
 internal static void Strike(ThunderGenerator owner,Vector3 position,int damage,GameObject primary,GameObject prefab,GameObject impactPrefab,ObjectPooler pool,SoundEffectSO sound)
 {
  var state=ElementalRun.Instance;float radius=1.1f*state.BodyWidth*owner.sizeMultiplier;int full=Mathf.FloorToInt(owner.damageMod.Modify(damage));
  var visited=new HashSet<int>();var ids=new List<int>();var actors=pool.GetComponent<AndroidPools>();
  DamageType kind=primary!=null?DamageType.Bullet:DamageType.Thunder;
  Visual(owner,position,prefab,impactPrefab,pool,sound);state.Pulse(position,radius,new Color(.65f,.85f,1));actors.GetMonsterInRange(ids,radius,position);
  if(primary!=null)Hit(owner,primary,full,visited,kind);
  foreach(int id in ids){var target=actors.GetFromID(id);if(Eligible(target))Hit(owner,target,Mathf.FloorToInt(full*Falloff(Vector2.Distance(position,target.transform.position),radius)),visited,kind);}
  if(state.Chain)
  {
   var p=PlayerController.Instance;int jumps=3+Mathf.FloorToInt(p.stats[StatType.Piercing].Modify(0))+Mathf.FloorToInt(p.stats[StatType.Bounce].Modify(0));Vector3 from=position;
   for(int i=0;i<jumps;i++)
   {
    ids.Clear();actors.GetMonsterInRange(ids,state.BodyWidth*3,from);
    var next=ids.Select(actors.GetFromID).Where(t=>Eligible(t)&&!visited.Contains(t.GetInstanceID())).OrderBy(t=>Vector2.SqrMagnitude(t.transform.position-from)).ThenBy(t=>t.GetInstanceID()).FirstOrDefault();if(next==null)break;
    var to=next.transform.position;Visual(owner,to,prefab,impactPrefab,pool,sound);state.Arc(from,to);Hit(owner,next,Mathf.FloorToInt(full*Mathf.Pow(.7f,i+1)),visited,kind);from=to;
   }
  }
  LastVictims=visited.Count;
 }
 internal static void Targeted(ThunderGenerator owner,GameObject target,int damage,GameObject prefab,GameObject impactPrefab,ObjectPooler pool,SoundEffectSO sound)
 {if(Eligible(target))Strike(owner,target.transform.position,damage,target,prefab,impactPrefab,pool,sound);}
 internal static void Area(ThunderGenerator owner,Vector3 position,int damage,float radius,GameObject prefab,GameObject impactPrefab,ObjectPooler pool,SoundEffectSO sound)=>Strike(owner,position,damage,null,prefab,impactPrefab,pool,sound);
}
[HarmonyPatch(typeof(ThunderGenerator),nameof(ThunderGenerator.GenerateAt),new[]{typeof(GameObject),typeof(int)})]
class AndroidTargetedThunder
{
 static bool Prefix(ThunderGenerator __instance,GameObject target,int damage,GameObject ___thunderPrefab,GameObject ___thunderImpactPrefab,ObjectPooler ___OP,SoundEffectSO ___soundFX)
 {AndroidLightning.Targeted(__instance,target,damage,___thunderPrefab,___thunderImpactPrefab,___OP,___soundFX);return false;}
}
[HarmonyPatch(typeof(ThunderGenerator),nameof(ThunderGenerator.GenerateAt),new[]{typeof(Vector3),typeof(int)})]
class AndroidAreaThunder
{
 static bool Prefix(ThunderGenerator __instance,Vector3 position,int damage,float ___baseAoE,GameObject ___thunderPrefab,GameObject ___thunderImpactPrefab,ObjectPooler ___OP,SoundEffectSO ___soundFX)
 {AndroidLightning.Area(__instance,position,damage,___baseAoE,___thunderPrefab,___thunderImpactPrefab,___OP,___soundFX);return false;}
}
[Serializable]
public sealed class AndroidElectroMastery:flanne.PerkSystem.Action
{public override void Activate(GameObject target){ThunderGenerator.SharedInstance.damageMod.AddFlatBonus(12);ElementalRun.Instance.Chain=true;}}
