using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

// Source AttachVisionDamage owns its own clock and queries monster centers;
// the native vision collider and PersistentHarm are not the damage owner.
public sealed class AndroidGlare:MonoBehaviour
{
 public float Damage=15,Multiplier=1,Seconds=1;
 public bool TriggerOnHit;
 internal float Timer;
 void Update()
 {
  Timer+=Time.deltaTime;if(Timer<Seconds)return;Timer-=Seconds;
  var player=PlayerController.Instance;
  float radius=player.stats[StatType.VisionRange].Modify(2);
  var targets=ObjectPooler.SharedInstance.pooledObjectsDictionary.Values.SelectMany(list=>list).Where(actor=>actor!=null&&actor.activeInHierarchy&&actor.layer==(int)TagLayerUtil.Enemy&&Vector2.Distance(actor.transform.position,player.transform.position)<radius).Distinct().ToArray();
  for(int i=targets.Length-1;i>=0;i--)
  {
   var target=targets[i];var health=target.GetComponent<Health>();if(health==null)continue;
   AndroidDamageRounding.ChangeHealth(health,-Mathf.CeilToInt(Damage*Multiplier),DamageType.Glare);
   // Android emits an actor ID on its unscoped hit channel. Windows' native
   // on-hit effects require the owning player and a GameObject payload.
   if(TriggerOnHit)player.gameObject.PostNotification(Projectile.ImpactEvent,target);
  }
 }
}
[Serializable]
public sealed class AndroidGlareAcquire:flanne.PerkSystem.Action
{
 public override void Activate(GameObject target)
 {
  var helper=new GameObject("PF_AttachVisionDamage");helper.transform.SetParent(PlayerController.Instance.transform,false);helper.AddComponent<AndroidGlare>();
 }
}
[Serializable]
public sealed class AndroidGlareUpgrade:flanne.PerkSystem.Action
{
 public float MultiplierBonus,Seconds;
 public bool TriggerOnHit;
 public override void Activate(GameObject target)
 {
  foreach(var helper in PlayerController.Instance.GetComponentsInChildren<AndroidGlare>())
  {helper.Multiplier+=MultiplierBonus;if(Seconds>0)helper.Seconds=Seconds;helper.TriggerOnHit|=TriggerOnHit;}
 }
}
[HarmonyPatch]
class AndroidGlareRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  flanne.PerkSystem.Action action;
  switch(RunResume.Key(__instance))
  {
   case "glare_name":action=new AndroidGlareAcquire();break;
   case "intense_glare_name":action=new AndroidGlareUpgrade{MultiplierBonus=1};break;
   case "saccade_name":action=new AndroidGlareUpgrade{Seconds=.5f};break;
   case "sight_magic_name":action=new AndroidGlareUpgrade{TriggerOnHit=true};break;
   default:return;
  }
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",action);
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
