using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// A battle owns its starting character/weapon rank. Native recipes compose this
// handling bonus with runes, powerups, Custom modifiers and weapon evolution.
public sealed class WeaponFamiliarity:MonoBehaviour
{
 public int Rank{get;private set;}
 public sealed class Record{public string character,weapon;public int rank;}
 internal static void Initialize()
 {
  var state=Progression.State;if(state.weaponFamiliarity==null)state.weaponFamiliarity=new List<Record>();
  if(state.weaponFamiliarityRecovered)return;
  // User-selected recovery: legacy global weapon history belongs to Shana.
  // Commit ranks and the one-time marker together in Progression.Load's Save.
  var items=NativeSelection.GunItems(Progression.Title.gunMenu);
  for(int i=0;i<items.Count;i++)
  {int rank=Mathf.Clamp(SaveSystem.data.gunHighestClear[i],0,15);if(rank>0)Grant("Shana",items[i].data.name,rank);}
  state.weaponFamiliarityRecovered=true;
 }
 static void Grant(string character,string weapon,int rank)
 {
  var record=Progression.State.weaponFamiliarity.FirstOrDefault(r=>r.character==character&&r.weapon==weapon);
  if(record==null){record=new Record{character=character,weapon=weapon};Progression.State.weaponFamiliarity.Add(record);}
  record.rank=Math.Max(record.rank,rank);
 }
 internal static int Crown(string character,string weapon)=>Mathf.Clamp(Progression.State.weaponFamiliarity.FirstOrDefault(r=>r.character==character&&r.weapon==weapon)?.rank??0,0,15);
 public static int Crown(int index)=>Crown(Progression.Title.gunMenu[index]);
 internal static int Crown(GunData data)=>Crown(Loadout.CharacterSelection.name,data.name);
 internal static void RecordVictory()
 {
  var map=SelectedMap.MapData;if(CustomMode.Active||map.achievementsDisabled)return;
  string character=Loadout.CharacterSelection.name,weapon=Loadout.GunSelection.name;
  int rank=Mathf.Clamp(map.darknessDisabled?0:Loadout.difficultyLevel,0,15);
  Grant(character,weapon,rank);Progression.Save();
  Plugin.Trace("character-weapon-familiarity",new{character,weapon,rank=Crown(character,weapon)});
 }
 internal static void Apply(PlayerController player)
 {
  if(player.GetComponent<WeaponFamiliarity>()!=null)return;
  var owner=player.gameObject.AddComponent<WeaponFamiliarity>();
  int current=Crown(Loadout.CharacterSelection.name,Loadout.GunSelection.name);
  owner.Rank=RunResume.Pending?.characterWeaponFamiliarity==true?RunResume.Pending.weaponFamiliarity??current:current;
  float bonus=owner.Rank*.02f;
  foreach(var type in new[]{StatType.FireRate,StatType.ReloadRate,StatType.ProjectileSpeed})player.stats[type].AddMultiplierBonus(bonus);
  Plugin.Trace("weapon-familiarity",new{character=Loadout.CharacterSelection.name,weapon=Loadout.GunSelection.name,rank=owner.Rank,bonus});
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
[HarmonyPatch(typeof(flanne.Core.PlayerSurvivedState),"CheckDifficultyUnlock")]
class CharacterWeaponFamiliarityVictory
{static void Postfix()=>WeaponFamiliarity.RecordVictory();}
