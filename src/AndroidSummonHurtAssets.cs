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

// Each original source powerup asset owns one observer. Repeated applications
// still add their stats, but delegate equality keeps one callback per asset.
public sealed class AndroidSummonHurtAsset:ScriptableObject
{
 public string Key;
 internal void Apply()=>this.AddObserver(OnHurt,AndroidSurvivingHurt.Event);
 void OnHurt(object sender,object args)
 {
  if((DamageType)args!=DamageType.Summon)return;
  // The source lightning roll precedes resolving the Health sender.
  if(Key=="thunderspawns_name"&&UnityEngine.Random.Range(0f,1f)>=.15f)return;
  bool isPlayer=sender is PlayerHealth;
  var player=isPlayer?(PlayerHealth)sender:null;
  var health=isPlayer?null:(Health)sender;
  if(Key=="thunderspawns_name"){ThunderGenerator.SharedInstance.GenerateAt(isPlayer?player.gameObject:health.gameObject,22);return;}
  int creature=isPlayer?0:AndroidHealthEvents.Source(health).CreatureType;
  if(creature==2)return;
  if(Key=="hellspawns_name"){BurnSystem.SharedInstance.Burn(isPlayer?player.gameObject:health.gameObject,3);return;}
  if(UnityEngine.Random.Range(0f,1f)>=.15f)return;
  // Source Culling uses ordinary signed Health damage, including modifiers,
  // protection, feedback and typed death. It does not call AutoKill.
  if(isPlayer)AndroidPlayerCollision.Get(player).Change(-999999,AndroidHarmfulKind.Powerup);
  else AndroidHealthEvents.Source(health).Change(-999999,AndroidHarmfulKind.Powerup,popupText:"Culled!");
 }
}
[Serializable]
public sealed class AndroidSummonHurtApplication:flanne.PerkSystem.Action
{
 public AndroidSummonHurtAsset Asset;
 public override void Activate(GameObject target)=>Asset.Apply();
}
[HarmonyPatch]
class AndroidSummonHurtRecipes
{
 static readonly string[] Keys={"culling_name","hellspawns_name","thunderspawns_name"};
 static readonly FieldInfo Effects=AccessTools.Field(typeof(Powerup),"effects"),Action=AccessTools.Field(typeof(PerkEffect),"action");
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);if(!Keys.Contains(key))return;
  var profile=(PowerupPoolProfile)AccessTools.Field(typeof(PowerupGenerator),"profile").GetValue(PowerupGenerator.Instance);
  var original=profile.powerupPool.Single(p=>RunResume.Key(p)==key);
  var application=((PerkEffect[])Effects.GetValue(original)).Select(e=>Action.GetValue(e)).OfType<AndroidSummonHurtApplication>().SingleOrDefault();
  var asset=application?.Asset;
  if(asset==null)
  {asset=ScriptableObject.CreateInstance<AndroidSummonHurtAsset>();asset.name="Android_"+key+"_State";asset.Key=key;Configure(original,asset);}
  Configure(__instance,asset);
 }
 static void Configure(Powerup power,AndroidSummonHurtAsset asset)
 {
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidSummonHurtApplication{Asset=asset});
  Plugin.SetField(power,"effects",new[]{effect});Plugin.SetField(power,"stackedEffects",new PerkEffect[0]);
 }
}
