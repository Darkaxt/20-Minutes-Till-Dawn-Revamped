using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.RuneSystem;
using flanne.UI;
namespace TwentyMinutes.Modernization;

// Native RuneData levels and stock save arrays still own purchases. The
// existing companion journal stores only the additional enabled selections.
public static class RuneProgression
{
 public static RuneRowUI[] Rows(RuneTreeUI tree)=>(RuneRowUI[])AccessTools.Field(typeof(RuneTreeUI),"rows").GetValue(tree);
 public static RuneUnlocker[] Entries(RuneRowUI row)=>(RuneUnlocker[])AccessTools.Field(typeof(RuneRowUI),"runes").GetValue(row);
 public static Toggle Toggle(RuneUnlocker rune)=>(Toggle)AccessTools.Field(typeof(RuneUnlocker),"toggle").GetValue(rune);
 public static int Maximum(RuneUnlocker rune)=>(int)AccessTools.Field(typeof(RuneUnlocker),"maxLevel").GetValue(rune);
 public static int NextPrice(RuneData rune)
 {
  long price=rune.costPerLevel;
  for(int i=0;i<rune.level;i++)price=checked((price*6+4)/5);
  return checked((int)price);
 }
 static IEnumerable<RuneTreeUI> Trees()
 {
  if(Progression.Title==null)yield break;
  yield return Progression.Title.swordRuneTree;yield return Progression.Title.shieldRuneTree;
 }
 public static List<RuneUnlocker> MissingAbove(RuneUnlocker target)
 {
  foreach(var tree in Trees())
  {
   var rows=Rows(tree);
   for(int row=0;row<rows.Length;row++)
   {
    int column=Array.IndexOf(Entries(rows[row]),target);if(column<0)continue;
    return rows.Take(row).Select(r=>Entries(r)[column]).Where(r=>r.level<Maximum(r)).ToList();
   }
  }
  throw new InvalidOperationException("Rune is outside the supported native trees");
 }
 public static bool CanUpgrade(RuneUnlocker rune)=>rune.level<Maximum(rune)&&MissingAbove(rune).Count==0;
 public static void Configure(RuneTreeUI tree)
 {
  var rows=Rows(tree);
  for(int i=0;i<rows.Length;i++)
  {
   foreach(var rune in Entries(rows[i]))Toggle(rune).group=null;
   // The shared row labels are siblings of the row components, inside the
   // native rune panel. They are not children of RuneRowUI.
   if(Progression.Title!=null)
    foreach(var label in Progression.Title.runeMenuPanel.GetComponentsInChildren<TMP_Text>(true))
     if(label.text.Replace(" ","")=="Rank"+Math.Max(1,rows[i].levelRequirement)+"+")label.text="Tier "+(i+1);
  }
 }
 public static List<RuneData> Active(RuneTreeUI tree)=>Rows(tree).SelectMany(Entries).Where(r=>r.level>0&&r.toggleOn).Select(r=>r.rune.data).ToList();
 public static void Restore(RuneTreeUI tree)
 {
  if(Progression.State?.enabledRunes==null)return; // Native single selections migrate unchanged.
  foreach(var rune in Rows(tree).SelectMany(Entries))rune.toggleOn=rune.level>0&&Progression.State.enabledRunes.Contains(rune.rune.data.name);
 }
 public static void RememberSelections()
 {
  if(Progression.State==null||Progression.Title==null)return;
  Progression.State.enabledRunes=Trees().SelectMany(Active).Select(r=>r.name).ToList();
 }
 public static void RefreshDescriptions()
 {
  if(Progression.Title==null)return;
  foreach(var description in Progression.Title.runeMenuPanel.GetComponentsInChildren<RuneDescription>(true))if(description.data!=null)description.Refresh();
 }
 public static void Describe(RuneDescription description)
 {
  var data=description.data;if(data==null||Progression.Title==null)return;
  var rune=Trees().SelectMany(t=>Rows(t)).SelectMany(Entries).Single(r=>r.rune.data==data);
  ((TMP_Text)AccessTools.Field(typeof(RuneDescription),"costTMP").GetValue(description)).text=data.level>=Maximum(rune)?"MAX":NextPrice(data).ToString();
  var text=(TMP_Text)AccessTools.Field(typeof(RuneDescription),"descriptionTMP").GetValue(description);
  text.text+="<br><br>Rank "+data.level+" / "+Maximum(rune)+(rune.toggleOn?" · Enabled":" · Disabled");
  var missing=MissingAbove(rune);
  if(missing.Count>0)text.text+="<br><color=#FC525F>Next rank: fully upgrade "+string.Join(", ",missing.Select(r=>r.rune.data.nameString))+" above.</color>";
 }
}

[HarmonyPatch(typeof(RuneTreeUI),"Start")]
class IndependentRuneToggles{static void Prefix(RuneTreeUI __instance)=>RuneProgression.Configure(__instance);}
[HarmonyPatch(typeof(RuneTreeUI),nameof(RuneTreeUI.SetSelections))]
class RestoreMultipleRunes
{
 static void Prefix(RuneTreeUI __instance)
 {
  RuneProgression.Configure(__instance);
  foreach(var rune in RuneProgression.Rows(__instance).SelectMany(RuneProgression.Entries))rune.toggleOn=false;
 }
 static void Postfix(RuneTreeUI __instance)=>RuneProgression.Restore(__instance);
}
[HarmonyPatch(typeof(RuneTreeUI),nameof(RuneTreeUI.GetActiveRunes))]
class AllEnabledRunes{static bool Prefix(RuneTreeUI __instance,ref List<RuneData> __result){__result=RuneProgression.Active(__instance);return false;}}
[HarmonyPatch(typeof(RuneTreeUI),nameof(RuneTreeUI.Refresh))]
class RuneColumnAvailability
{
 static bool Prefix(RuneTreeUI __instance)
 {
  // Every entry remains focusable so its price and column requirement can be
  // inspected. Purchase eligibility is checked at press and again at commit.
  foreach(var rune in RuneProgression.Rows(__instance).SelectMany(RuneProgression.Entries))rune.locked=false;
  return false;
 }
}
[HarmonyPatch(typeof(RuneUnlocker),"get_costPerLevel")]
class CompoundingRunePrice{static bool Prefix(RuneUnlocker __instance,ref int __result){__result=RuneProgression.NextPrice(__instance.rune.data);return false;}}
[HarmonyPatch(typeof(RuneUnlocker),"StartPress")]
class RunePurchasePress
{
 static bool Prefix(RuneUnlocker __instance,bool ____isSelected,ref IEnumerator ____pressAndHoldCoroutine)
 {
  AccessTools.Method(typeof(RuneUnlocker),"ReleasePress").Invoke(__instance,null);
  if(!____isSelected)return false;
  var toggle=RuneProgression.Toggle(__instance);
  if(toggle.interactable)toggle.isOn=!toggle.isOn;
  RuneProgression.RefreshDescriptions();
  if(RuneProgression.CanUpgrade(__instance)&&PointsTracker.pts>=__instance.costPerLevel)
  {
   ____pressAndHoldCoroutine=(IEnumerator)AccessTools.Method(typeof(RuneUnlocker),"PressAndHoldCR").Invoke(__instance,null);
   __instance.StartCoroutine(____pressAndHoldCoroutine);
  }
  return false;
 }
}
[HarmonyPatch(typeof(RuneUnlocker),"LevelUp")]
class CommitRunePurchase
{
 static bool Prefix(RuneUnlocker __instance,Slider ___levelupSlider,ParticleSystem ___levelupParticles)
 {
  int price=__instance.costPerLevel;
  // Recheck the actual balance and ancestors when the native hold completes.
  if(!RuneProgression.CanUpgrade(__instance)||PointsTracker.pts<price){___levelupSlider.value=0;return false;}
  PointsTracker.pts-=price;__instance.level++;__instance.toggleOn=true;
  ___levelupSlider.value=0;___levelupParticles.Play();RuneProgression.RefreshDescriptions();return false;
 }
}
[HarmonyPatch(typeof(RuneDescription),nameof(RuneDescription.Refresh))]
class RunePriceDescription{static void Postfix(RuneDescription __instance)=>RuneProgression.Describe(__instance);}
