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

// These three source callbacks belong to their original Tome assets, not each
// copied native PerkEffect or to PlayerHealth. Keep its Unity lifetime under
// the original devil-profile asset's recipe, without a static owning cache.
public sealed class AndroidDamageTomeAsset:ScriptableObject
{
 public string Key;public float Damage;
 internal void Apply()
 {
  this.AddObserver(OnDamage,Health.TweakDamageEvent);
  if(Key=="tome_of_glass_name")
  {AndroidPlayerCollision.Get(PlayerController.Instance.playerHealth).Fragile=true;Damage+=2;}
 }
 void OnDamage(object sender,object args)
 {
  var health=(Health)sender;
  var actor=health.gameObject;
  var modifiers=(List<ValueModifier>)args;
  if(Key=="tome_of_glass_name")modifiers.Add(new MultValueModifier(0,Damage));
  else if(Key=="tome_of_might_name")modifiers.Add(new AndroidMightDamage().GetMod());
  else
  {
   float movement=PlayerController.Instance.stats[StatType.MoveSpeed].Modify(1);
   modifiers.Add(new MultValueModifier(0,1-.15f));
   if(movement>0)modifiers.Add(new MultValueModifier(1,movement));
  }
 }
 void OnDestroy()
 {this.RemoveObserver(OnDamage,Health.TweakDamageEvent);Damage=0;}
}
[Serializable]
public sealed class AndroidDamageTomeApplication:flanne.PerkSystem.Action
{
 public AndroidDamageTomeAsset Asset;
 public override void Activate(GameObject target)=>Asset.Apply();
}
[HarmonyPatch]
class AndroidDamageTomeRecipe
{
 static readonly FieldInfo Effects=AccessTools.Field(typeof(Powerup),"effects");
 static readonly FieldInfo Action=AccessTools.Field(typeof(PerkEffect),"action");
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);
  if(key!="tome_of_glass_name"&&key!="tome_of_might_name"&&key!="tome_of_wind_name")return;
  var profile=(PowerupPoolProfile)AccessTools.Field(typeof(PowerupGenerator),"devilDealProfile").GetValue(PowerupGenerator.Instance);
  var original=profile.powerupPool.Single(p=>RunResume.Key(p)==key);
  var application=((PerkEffect[])Effects.GetValue(original)).Select(e=>Action.GetValue(e)).OfType<AndroidDamageTomeApplication>().SingleOrDefault();
  var asset=application?.Asset;
  if(asset==null)
  {
   asset=ScriptableObject.CreateInstance<AndroidDamageTomeAsset>();asset.name="Android_"+key+"_State";asset.Key=key;
   Configure(original,asset);
  }
  // Native Copy recursively copies managed recipes, including Unity wrappers.
  // Resolve the actual original asset here instead of using that copied owner.
  Configure(__instance,asset);
 }
 static void Configure(Powerup powerup,AndroidDamageTomeAsset asset)
 {
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());
  Plugin.SetField(effect,"action",new AndroidDamageTomeApplication{Asset=asset});
  Plugin.SetField(powerup,"effects",new[]{effect});Plugin.SetField(powerup,"stackedEffects",new PerkEffect[0]);
 }
}

// Android checks delegate equality before copying an invoking observer list
// or appending. Windows appends duplicates; repeated applications to one
// source asset must not multiply the number of its damage callbacks.
[HarmonyPatch(typeof(NotificationCenter),nameof(NotificationCenter.AddObserver),new[]{typeof(Action<object,object>),typeof(string),typeof(object)})]
class AndroidObserverIdentity
{
 static bool Prefix(NotificationCenter __instance,Action<object,object> handler,string notificationName,object sender,
  Dictionary<string,Dictionary<object,List<Action<object,object>>>> ____table)
 {
  if(handler==null||string.IsNullOrEmpty(notificationName))return true;
  return !____table.TryGetValue(notificationName,out var senders)||
   !senders.TryGetValue(sender??__instance,out var handlers)||!handlers.Contains(handler);
 }
}
