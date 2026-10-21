using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Actions;
using flanne.Player.Buffs;
namespace TwentyMinutes.Modernization;

// Keep native buff ownership and modifier order. These two perks document
// maximum health; other uses of AddHPToDamageMod deliberately retain current HP.
[HarmonyPatch]
class HolySpearMaximumHealth
{
 static readonly AccessTools.FieldRef<Powerup,LocalizedString> Key=AccessTools.FieldRefAccess<Powerup,LocalizedString>("nameStrID");
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=Key(__instance).key;
  if(key!="holy_spear_name"&&key!="tome_of_might_name")return;
  foreach(string field in new[]{"effects","stackedEffects"})
   foreach(var effect in (PerkEffect[])AccessTools.Field(typeof(Powerup),field).GetValue(__instance))
   {
    var action=AccessTools.Field(typeof(PerkEffect),"action").GetValue(effect) as ApplyPlayerBuffAction;if(action==null)continue;
    object buff=AccessTools.Field(typeof(ApplyPlayerBuffAction),"buff").GetValue(action);
    if(key=="holy_spear_name")
    {
     if(!(buff is SummonDamageBuff)||(string)AccessTools.Field(typeof(SummonDamageBuff),"summonTypeID").GetValue(buff)!="Spear")continue;
    }
    else if(!(buff is DamageBuff))continue;
    var modifierField=AccessTools.Field(buff.GetType(),"modifier");
    var modifier=modifierField.GetValue(buff) as AddHPToDamageMod;if(modifier==null)continue;
    float amount=(float)AccessTools.Field(typeof(AddHPToDamageMod),"multiplier").GetValue(modifier);
    modifierField.SetValue(buff,new MaximumHealthDamage{Multiplier=amount});
   }
 }
}
[Serializable]
public class MaximumHealthDamage:IDamageModifier
{
 public float Multiplier;
 public ValueModifier GetMod()=>new AddValueModifier(1,PlayerController.Instance.playerHealth.maxHP*Multiplier);
}
