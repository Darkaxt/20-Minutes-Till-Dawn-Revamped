using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using flanne;
using flanne.PerkSystem;
namespace TwentyMinutes.Modernization;

// Equip stores a deep copy but receives original pool assets on every choice.
// Match their native serialized identity before taking the intended repeat path;
// retain the native fresh copy per repeat so triggered effects remain distinct.
[HarmonyPatch(typeof(PlayerPerks),nameof(PlayerPerks.Equip),typeof(Powerup))]
class RepeatedPowerups
{
 static readonly AccessTools.FieldRef<Powerup,LocalizedString> Name=AccessTools.FieldRefAccess<Powerup,LocalizedString>("nameStrID");
 static readonly AccessTools.FieldRef<PlayerPerks,List<Powerup>> Equipped=AccessTools.FieldRefAccess<PlayerPerks,List<Powerup>>("_equippedPerks");
 static readonly AccessTools.FieldRef<PlayerPerks,PlayerController> Player=AccessTools.FieldRefAccess<PlayerPerks,PlayerController>("player");
 static bool Prefix(PlayerPerks __instance,Powerup perk)
 {
  string key=Name(perk).key;
  if(!Equipped(__instance).Any(x=>Name(x).key==key))return true;
  perk.Copy().ApplyStack(Player(__instance));return false;
 }
}
