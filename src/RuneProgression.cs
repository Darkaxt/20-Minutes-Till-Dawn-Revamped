using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.UI;
using flanne.RuneSystem;
using flanne.TitleScreen;
namespace TwentyMinutes.Modernization;

[Serializable]public sealed class RuneProfile
{public string character;public List<RunResume.RuneRank> ranks=new List<RunResume.RuneRank>();public List<string> enabled=new List<string>();}

// Character profiles own permanent ranks. Native UI data is their projection;
// battles use independent RuneData clones and never gain retroactive ranks.
public static class RuneProgression
{
 static string projected;
 public static RuneRowUI[] Rows(RuneTreeUI tree)=>(RuneRowUI[])AccessTools.Field(typeof(RuneTreeUI),"rows").GetValue(tree);
 public static RuneUnlocker[] Entries(RuneRowUI row)=>(RuneUnlocker[])AccessTools.Field(typeof(RuneRowUI),"runes").GetValue(row);
 public static Toggle Toggle(RuneUnlocker rune)=>(Toggle)AccessTools.Field(typeof(RuneUnlocker),"toggle").GetValue(rune);
 public static int Maximum(RuneUnlocker rune)=>(int)AccessTools.Field(typeof(RuneUnlocker),"maxLevel").GetValue(rune);
 internal static IEnumerable<RuneTreeUI> Trees()=>new[]{Progression.Title.swordRuneTree,Progression.Title.shieldRuneTree};
 internal static IEnumerable<RuneUnlocker> All()=>Trees().SelectMany(Rows).SelectMany(Entries);
 internal static string Character=>Progression.Title.characterMenu[(int)AccessTools.Field(typeof(flanne.UIExtensions.Menu),"index").GetValue(Progression.Title.characterMenu)].name;
 internal static void Initialize()
 {
  var state=Progression.State;projected=null;
  if(state.characterRunes==null)state.characterRunes=new List<RuneProfile>();
  if(state.runeSeed==null)
  {
   // SaveSystem.Load runs before InitState projects saved levels into the
   // rune UI. Read the serialized manager's native index contract directly.
   var entries=(TieredUnlockable[])AccessTools.Field(typeof(TieredUnlockManager),"unlockables").GetValue(Progression.Title.runeUnlocker);
   var levels=SaveSystem.data.runeUnlocks?.unlocks??new int[entries.Length];if(levels.Length!=entries.Length)Array.Resize(ref levels,entries.Length);
   state.runeSeed=entries.Select((r,i)=>new RunResume.RuneRank{name=((RuneUnlocker)r).rune.data.name,rank=levels[i]}).ToList();
   state.runeSeedEnabled=state.enabledRunes==null?LegacySelections():new List<string>(state.enabledRunes);
  }
 }
 static List<string> LegacySelections()
 {
  var result=new List<string>();var trees=Trees().ToArray();var selections=new[]{SaveSystem.data.swordRuneSelections,SaveSystem.data.shieldRuneSelections};
  for(int tree=0;tree<2;tree++)for(int row=0;row<(selections[tree]?.Length??0);row++)
  {int column=selections[tree][row];if(column>=0){var entry=Entries(Rows(trees[tree])[row])[column];if(Progression.State.runeSeed.Any(r=>r.name==entry.rune.data.name&&r.rank>0))result.Add(entry.rune.data.name);}}
  return result;
 }
 public static RuneProfile Profile(string character)
 {
  var state=Progression.State;var profile=state.characterRunes.FirstOrDefault(p=>p.character==character);
  if(profile==null)
  {
   profile=new RuneProfile{character=character,ranks=state.runeSeed.Select(r=>new RunResume.RuneRank{name=r.name,rank=r.rank}).ToList(),enabled=new List<string>(state.runeSeedEnabled)};
   state.characterRunes.Add(profile);
  }
  return profile;
 }
 public static int Rank(string character,string name)=>Profile(character).ranks.Single(r=>r.name==name).rank;
 internal static void Project()
 {
  if(Progression.State==null||Progression.Title==null)return;
  string character=Character;if(projected==character)return;
  RememberSelections();var profile=Profile(character);projected=character;
  foreach(var rune in All()){rune.level=profile.ranks.Single(r=>r.name==rune.rune.data.name).rank;rune.toggleOn=rune.level>0&&profile.enabled.Contains(rune.rune.data.name);}
 }
 public static void Configure(RuneTreeUI tree)
 {
  foreach(var row in Rows(tree))foreach(var rune in Entries(row))Toggle(rune).group=null;
  if(Progression.Title==null)return;
  foreach(var label in Progression.Title.runeMenuPanel.GetComponentsInChildren<TMP_Text>(true))
   for(int i=0;i<4;i++)if(label.text.Replace(" ","")=="Rank"+new[]{1,5,10,15}[i]+"+")label.text="Tier "+(i+1);
 }
 public static List<RuneData> Active(RuneTreeUI tree)
 {
  Project();var selected=Rows(tree).SelectMany(Entries).Where(r=>r.level>0&&r.toggleOn).ToArray();
  // Native selection transfers the unlocker's rank into the RuneData consumed
  // by the battle. The menu's rank projection alone is not that transfer.
  foreach(var rune in selected)rune.rune.data.level=rune.level;
  return selected.Select(r=>r.rune.data).ToList();
 }
 public static void Restore(RuneTreeUI tree)
 {
  if(Progression.State==null)return;Project();var profile=Profile(Character);
  foreach(var rune in Rows(tree).SelectMany(Entries))rune.toggleOn=rune.level>0&&profile.enabled.Contains(rune.rune.data.name);
 }
 public static void RememberSelections()
 {
  if(Progression.State==null||Progression.Title==null||projected==null)return;
  Profile(projected).enabled=All().Where(r=>r.level>0&&r.toggleOn).Select(r=>r.rune.data.name).ToList();
 }
 public static void RefreshDescriptions()
 {
  if(Progression.Title==null)return;
  foreach(var description in Progression.Title.runeMenuPanel.GetComponentsInChildren<RuneDescription>(true))if(description.data!=null)description.Refresh();
 }
 public static void Describe(RuneDescription description)
 {
  var data=description.data;if(data==null||Progression.Title==null)return;
  var cost=(TMP_Text)AccessTools.Field(typeof(RuneDescription),"costTMP").GetValue(description);cost.text="";cost.transform.parent.gameObject.SetActive(false);
  var text=(TMP_Text)AccessTools.Field(typeof(RuneDescription),"descriptionTMP").GetValue(description);
  var rune=All().Single(r=>r.rune.data==data);
  text.text+="<br><br>Rank "+data.level+" / "+Maximum(rune)+(data.level==0?" · Earn from arena bosses":rune.toggleOn?" · Enabled":" · Disabled");
 }
}
[HarmonyPatch(typeof(RuneTreeUI),"Start")]
class IndependentRuneToggles{static void Prefix(RuneTreeUI __instance)=>RuneProgression.Configure(__instance);}
[HarmonyPatch(typeof(RuneTreeUI),nameof(RuneTreeUI.SetSelections))]
class RestoreMultipleRunes
{
 static void Prefix(RuneTreeUI __instance){RuneProgression.Configure(__instance);foreach(var rune in RuneProgression.Rows(__instance).SelectMany(RuneProgression.Entries))rune.toggleOn=false;}
 static void Postfix(RuneTreeUI __instance)=>RuneProgression.Restore(__instance);
}
[HarmonyPatch(typeof(RuneTreeUI),nameof(RuneTreeUI.GetActiveRunes))]
class AllEnabledRunes{static bool Prefix(RuneTreeUI __instance,ref List<RuneData> __result){__result=RuneProgression.Active(__instance);return false;}}
[HarmonyPatch(typeof(RuneTreeUI),nameof(RuneTreeUI.Refresh))]
class RuneRewardAvailability
{static bool Prefix(RuneTreeUI __instance){RuneProgression.Project();foreach(var rune in RuneProgression.Rows(__instance).SelectMany(RuneProgression.Entries))rune.locked=false;return false;}}
[HarmonyPatch(typeof(RuneUnlocker),"StartPress")]
class RuneRewardToggle
{
 static bool Prefix(RuneUnlocker __instance,bool ____isSelected)
 {
  AccessTools.Method(typeof(RuneUnlocker),"ReleasePress").Invoke(__instance,null);
  if(____isSelected&&__instance.level>0){var toggle=RuneProgression.Toggle(__instance);if(toggle.interactable)toggle.isOn=!toggle.isOn;RuneProgression.RememberSelections();Progression.Save();RuneProgression.RefreshDescriptions();}
  return false;
 }
}
[HarmonyPatch(typeof(RuneUnlocker),"LevelUp")]
class NoRunePurchase{static bool Prefix()=>false;}
[HarmonyPatch(typeof(RuneDescription),nameof(RuneDescription.Refresh))]
class RuneRewardDescription{static void Postfix(RuneDescription __instance)=>RuneProgression.Describe(__instance);}
[HarmonyPatch]
class CharacterRuneMenu
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(RuneMenuCharacterState),"Enter"),AccessTools.Method(typeof(RuneMenuGunState),"Enter")};
 static void Prefix()=>RuneProgression.Project();
}
