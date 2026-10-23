using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Pickups;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

// These source pickups attach independent helpers. The native inline trigger
// recipes do not own their source Start/destruction or unsaved timer lifetime.
public sealed class AndroidHealOnBurn:MonoBehaviour
{
 public float Chance=.0005f;
 void Start()=>this.AddObserver(Inflicted,BurnSystem.InflictBurnEvent);
 void OnDestroy()=>this.RemoveObserver(Inflicted,BurnSystem.InflictBurnEvent);
 void Inflicted(object sender,object args)
 {
  if(UnityEngine.Random.Range(0f,1f)<Chance)
   AndroidPlayerCollision.Get(PlayerController.Instance.playerHealth).Change(1,DamageType.None);
 }
}
public sealed class AndroidAmmoOnXP:MonoBehaviour
{
 public float Chance=.1f;public int Amount=1;public SoundEffectSO Sound;
 Ammo ammo;
 void Start()
 {
  ammo=transform.parent.GetComponentInChildren<Ammo>();
  transform.localPosition=Vector3.zero;
  this.AddObserver(PickedUp,XPPickup.XPPickupEvent);
 }
 void OnDestroy()=>this.RemoveObserver(PickedUp,XPPickup.XPPickupEvent);
 void PickedUp(object sender,object args)
 {
  if(UnityEngine.Random.Range(0f,1f)<Chance)
  {
   if(ammo!=null){ammo.GainAmmo(Amount);Sound.Play();}
   else Debug.LogWarning("No ammo component found");
  }
 }
}
public sealed class AndroidSoulShieldRegeneration:MonoBehaviour
{
 public int Amount=1;public float Duration=90,Remaining;
 void Start()=>Remaining=Duration;
 void Update()
 {
  Remaining-=Time.deltaTime;
  if(Remaining<=0)
  {
   Remaining=Duration;
   PlayerController.Instance.playerHealth.shp+=Amount;
  }
 }
}
[Serializable]
public sealed class AndroidRecoveryAttachment:flanne.PerkSystem.Action
{
 public string Key;
 public override void Activate(GameObject target)
 {
  var root=new GameObject(Key=="soul_shield_name"?"SoulShieldDurationCreate":Key=="soothing_warmth_name"?"PF_HealOnBurn":"PF_AmmoOnXP");
  root.transform.SetParent(PlayerController.Instance.transform,false);
  MonoBehaviour helper;
  if(Key=="soothing_warmth_name")helper=root.AddComponent<AndroidHealOnBurn>();
  else if(Key=="recharge_name")
  {
   var refill=root.AddComponent<AndroidAmmoOnXP>();
   refill.Sound=Resources.FindObjectsOfTypeAll<SoundEffectSO>().Single(sound=>sound.name=="Energized_SFX");helper=refill;
  }
  else
  {
   helper=root.AddComponent<AndroidSoulShieldRegeneration>();
   // Source invokes base attachment before capacity and direct heart grant.
   var health=PlayerController.Instance.playerHealth;health.maxSHP+=3;health.shp++;
  }
  // Source does not save these helper phases. Recreated Start follows the
  // actual health/ammo restore boundary through the existing source queue.
  if(RunResume.Restoring){helper.enabled=false;RunRecorder.Instance.SourceStarts.Add(helper);}
 }
}
[HarmonyPatch]
class AndroidRecoveryRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);
  if(key!="soothing_warmth_name"&&key!="recharge_name"&&key!="soul_shield_name")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidRecoveryAttachment{Key=key});
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
