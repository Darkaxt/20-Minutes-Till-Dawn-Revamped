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

// KillEffectPowerup owns one counter and grant recipe on the original source
// asset. Native PlayerPerks copies recipes; resolve their original owner just
// as for the asset-owned damage Tomes instead of creating a listener per pick.
public sealed class AndroidKillEffectAsset:ScriptableObject
{
 public string Key;
 public int Kills,IntEffect;
 public float FloatEffect,Accumulated;
 internal void Apply()
 {
  this.AddObserver(OnKill,"Health.DeathEvent");
  string effect=Key=="feed_the_beasts_name"?"0.01":"1";
  // The shipped Apply uses the current-culture overloads, independently.
  if(float.TryParse(effect,out float floating))FloatEffect+=floating;
  if(int.TryParse(effect,out int integer))unchecked{IntEffect+=integer;}
 }
 void OnKill(object sender,object args)
 {
  if((DamageType)args!=DamageType.Summon)return;
  unchecked{Kills++;}
  if(Kills<(Key=="feed_the_beasts_name"?15:500))return;
  Kills=0;
  var player=PlayerController.Instance;
  if(Key=="feed_the_beasts_name")
  {player.stats[StatType.SummonDamage].AddMultiplierBonus(FloatEffect);Accumulated+=FloatEffect;}
  else if(Key=="soul_drain_name")player.playerHealth.shp+=IntEffect;
  // Source Bloodsuckers supplies EnumDamage.Cure (0), represented by the
  // native None category. Its surviving-health notification keeps that kind.
  else AndroidPlayerCollision.Get(player.playerHealth).Change(IntEffect,DamageType.None);
 }
 void OnDestroy(){IntEffect=0;FloatEffect=0;}
}
[Serializable]
public sealed class AndroidKillEffectApplication:flanne.PerkSystem.Action
{
 public AndroidKillEffectAsset Asset;
 public override void Activate(GameObject target)=>Asset.Apply();
}
[HarmonyPatch]
class AndroidKillEffectRecipes
{
 internal static readonly string[] Keys={"feed_the_beasts_name","bloodsuckers_name","soul_drain_name"};
 static readonly FieldInfo Effects=AccessTools.Field(typeof(Powerup),"effects"),Action=AccessTools.Field(typeof(PerkEffect),"action");
 static Powerup Original(string key)
 {
  var profile=(PowerupPoolProfile)AccessTools.Field(typeof(PowerupGenerator),"profile").GetValue(PowerupGenerator.Instance);
  return profile.powerupPool.Single(p=>RunResume.Key(p)==key);
 }
 internal static AndroidKillEffectAsset Existing(string key)=>((PerkEffect[])Effects.GetValue(Original(key)))
  .Select(e=>Action.GetValue(e)).OfType<AndroidKillEffectApplication>().SingleOrDefault()?.Asset;
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);if(!Keys.Contains(key))return;
  var original=Original(key);var asset=Existing(key);
  if(asset==null)
  {asset=ScriptableObject.CreateInstance<AndroidKillEffectAsset>();asset.name="Android_"+key+"_State";asset.Key=key;Configure(original,asset);}
  Configure(__instance,asset);
 }
 static void Configure(Powerup powerup,AndroidKillEffectAsset asset)
 {
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidKillEffectApplication{Asset=asset});
  Plugin.SetField(powerup,"effects",new[]{effect});Plugin.SetField(powerup,"stackedEffects",new PerkEffect[0]);
 }
}
