using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PowerupSystem;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

public sealed class AndroidCurseOnHit:MonoBehaviour
{
 public float Chance=.35f;
 CurseSystem curse;
 void Start()
 {
  this.AddObserver(Impact,Projectile.ImpactEvent);
  curse=CurseSystem.Instance;
 }
 void OnDestroy()=>this.RemoveObserver(Impact,Projectile.ImpactEvent);
 void Impact(object sender,object args)
 {
  if(UnityEngine.Random.Range(0f,1f)<Chance)
  {
   // Source resolves the actor ID after its roll, before the Unity null test.
   // Native event arguments carry that actor instead of its source integer ID.
   var target=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetFromID(((GameObject)args).GetInstanceID()).gameObject;
   if(target==null)return;
   // Both source monster visibility layers use the native Enemy layer.
   if(target.layer==(int)TagLayerUtil.Enemy)curse.Curse(target);
  }
 }
}
public sealed class AndroidPierceOnKill:MonoBehaviour
{
 void Start()=>this.AddObserver(Killed,Projectile.KillEvent);
 void OnDestroy()=>this.RemoveObserver(Killed,Projectile.KillEvent);
 void Killed(object sender,object args)=>((Projectile)sender).piercing++;
}

[Serializable]
public sealed class AndroidStatusAttachment:flanne.PerkSystem.Action
{
 public string Key;
 internal static string Root(string key)
 {
  switch(key)
  {
   case "yuki_perk_brittle_name":return "PF_DamageBoostAgainstFrozen";
   case "dark_arts_name":return "PF_CurseOnHit";
   case "wither_name":return "PF_DamageBoostAgainstCursed";
   case "holy_might_name":return "PF_SmiteDamageHPScaling";
   case "reaper_rounds":return "PF_PierceOnKill";
   case "death_plague_name":return "PF_SpreadCurse";
   case "frost_fire_name":return "PF_BurnOnFreeze";
   default:return null;
  }
 }
 public override void Activate(GameObject target)
 {
  var root=new GameObject(Root(Key));
  root.transform.SetParent(PlayerController.Instance.transform,false);
  MonoBehaviour helper;
  switch(Key)
  {
   case "yuki_perk_brittle_name":
    helper=root.AddComponent<DamageBoostAgainstFrozen>();Plugin.SetField(helper,"damageBoost",.3f);break;
   case "dark_arts_name":helper=root.AddComponent<AndroidCurseOnHit>();break;
   case "wither_name":
    helper=root.AddComponent<DamageBoostAgainstCursed>();Plugin.SetField(helper,"damageBoost",.3f);break;
   case "holy_might_name":helper=root.AddComponent<SmiteHPDamageScaling>();break;
   case "reaper_rounds":helper=root.AddComponent<AndroidPierceOnKill>();break;
   case "death_plague_name":
    // This source helper uses collider overlap, not the pooled-center area action.
    helper=root.AddComponent<SpreadCurseOnKill>();Plugin.SetField(helper,"range",1f);break;
   default:helper=root.AddComponent<BurnOnFreeze>();Plugin.SetField(helper,"burnDamage",6);break;
  }
  // Source has no saved helper phase. Start must observe restored health/ammo.
  if(RunResume.Restoring){helper.enabled=false;RunRecorder.Instance.SourceStarts.Add(helper);}
 }
}
[HarmonyPatch]
class AndroidStatusPerkRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);
  if(AndroidStatusAttachment.Root(key)==null)return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidStatusAttachment{Key=key});
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
