using System;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// A battle owns its starting crown. Native stat recipes then compose this
// handling bonus with runes, powerups, Custom modifiers and weapon evolution.
public sealed class WeaponFamiliarity:MonoBehaviour
{
 public int Rank{get;private set;}
 public static int Crown(int index)=>Mathf.Clamp(SaveSystem.data.gunHighestClear[index],0,15);
 internal static int Crown(GunData data)
 {
  var items=NativeSelection.GunItems(Progression.Title.gunMenu);
  int index=items.FindIndex(item=>item.data==data);
  return index<0?0:Crown(index);
 }
 internal static void Apply(PlayerController player)
 {
  if(player.GetComponent<WeaponFamiliarity>()!=null)return;
  var owner=player.gameObject.AddComponent<WeaponFamiliarity>();
  owner.Rank=RunResume.Pending?.weaponFamiliarity??Crown(Loadout.GunIndex);
  float bonus=owner.Rank*.02f;
  foreach(var type in new[]{StatType.FireRate,StatType.ReloadRate,StatType.ProjectileSpeed})player.stats[type].AddMultiplierBonus(bonus);
  Plugin.Trace("weapon-familiarity",new{weapon=Loadout.GunSelection.name,rank=owner.Rank,bonus});
 }
 internal static string Description(GunData data,string description)
 {
  int rank=Crown(data);
  return rank==0?description:description+"<br><color=#A8D5BA>Familiarity +"+(rank*2)+"%: reload speed, fire rate, projectile speed.</color>";
 }
}
[HarmonyPatch(typeof(LoadoutInitializer),"Start")]
class WeaponFamiliarityStart
{static void Postfix()=>WeaponFamiliarity.Apply(PlayerController.Instance);}
