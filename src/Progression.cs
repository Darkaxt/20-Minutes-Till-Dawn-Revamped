using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using flanne;
using flanne.Core;
using flanne.UI;
using flanne.PerkSystem;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// Native arrays remain the ownership records. Only facts absent from the
// original save and delivery of unlock notices need a companion journal.
public static partial class Progression
{
 public class Journal
 {
  public int version=2,bestLevel=1;public float bestSeconds;
  public List<Attempt> attempts=new List<Attempt>();public List<Notice> notices=new List<Notice>();
  public List<string> facts=new List<string>(),earned=new List<string>(),pending=new List<string>(),notified=new List<string>();
  public List<string> enabledRunes;
  public List<RuneProfile> characterRunes;
  public List<RunResume.RuneRank> runeSeed;
  public List<string> runeSeedEnabled;
  public RuneRewardHistory runeHistory;
  public List<int> sourceAchievements=new List<int>(),sourceAchievementRewards=new List<int>();
  public int challengeDifficultyUnlocked;
  public List<ModeClearRecord> modeClears=new List<ModeClearRecord>();
 }
 public class ModeClearRecord{public string mode;public int difficulty,count;}
 public class Gate
 {
  public string Id,Goal,Name;public Sprite Icon;public ScriptableObject Data;
  public EncounterKind Kind=EncounterKind.Miniboss;public string Map=Forest,RequiredCharacter,RequiredWeapon;
  public int MinimumDarkness,Ordinal=-1;public RunAttribute Attribute;public float Threshold;public bool RequireHitless;public string[] Perks=new string[0];
  public Unlockable Unlock;public int Index=-1;public bool Character;
 }
 public static readonly List<Gate> Gates=new List<Gate>();
 public static Journal State;public static TitleScreenController Title;
 internal static ScriptableObject InspectedData;
 public static string JournalPath=>Path.Combine(Application.persistentDataPath,"revamped-achievements.json");
 static readonly Dictionary<Unlockable,Gate> unlockGates=new Dictionary<Unlockable,Gate>();
 static readonly Dictionary<ScriptableObject,Gate> dataGates=new Dictionary<ScriptableObject,Gate>();
 static readonly AccessTools.FieldRef<PlayerPerks,List<Powerup>> Equipped=AccessTools.FieldRefAccess<PlayerPerks,List<Powerup>>("_equippedPerks");
 static readonly AccessTools.FieldRef<Powerup,LocalizedString> PowerKey=AccessTools.FieldRefAccess<Powerup,LocalizedString>("nameStrID");
 public static bool Has(string fact)=>State!=null&&State.facts.Contains(fact);
 public static void Register(TitleScreenController title)
 {
  Title=title;Gates.Clear();unlockGates.Clear();dataGates.Clear();contentNames.Clear();
  foreach(var item in Characters.Items(title.characterMenu))contentNames.Add(item.data.name,item.data.nameString);
  foreach(var item in NativeSelection.GunItems(title.gunMenu))contentNames.Add(item.data.name,item.data.nameString);
  foreach(var item in Characters.Items(title.characterMenu))
  {
   var rule=RuleFor(item.data.name);if(rule==null)continue;
   Add(rule,"character:"+item.data.name,item.data,item.data.nameString,item.GetComponent<Unlockable>(),true,item.data.icon);
  }
  foreach(var item in NativeSelection.GunItems(title.gunMenu))
  {
   var rule=RuleFor(item.data.name);if(rule==null)continue;
   Add(rule,"weapon:"+item.data.name,item.data,item.data.nameString,item.GetComponent<Unlockable>(),false,item.data.icon);
  }
  foreach(var menu in new[]{title.modeSelectMenu,title.mapSelectMenu})
  {
   var toggles=(Toggle[])AccessTools.Field(typeof(ToggleGroupMenu<MapData>),"toggles").GetValue(menu);
   foreach(var toggle in toggles)
   {
    var map=toggle.GetComponent<DataUIBinding<MapData>>()?.data;if(map==null||dataGates.ContainsKey(map))continue;
    if(map.name!=Forest&&map.name!=Temple&&map.name!=Pumpkin&&map.name!="10M_Forest"&&map.name!="Endless_Forest"&&map.name!="Challenge_Forest")continue;
    var rule=RuleFor(map.name);if(rule==null)continue;
    var boss=(map.bossSpawns?.FirstOrDefault()??map.endlessBossSpawn?.FirstOrDefault())?.bossPrefab;
    var artwork=boss?.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.sprite!=null&&r.sprite.name.StartsWith("T_"))?.sprite;
    var unlock=toggle.GetComponent<Unlockable>();
    if(map.name==Temple||map.name==Pumpkin)
     unlock=(Unlockable)AccessTools.Field(typeof(UnlockAtDarkness),"unlockable").GetValue(map.name==Temple?title.templeUnlocker:title.pumpkinPatchUnlocker);
    Add(rule,"scenario:"+map.name,map,map.nameString,unlock,false,artwork);
   }
  }
  Plugin.Trace("progression-registered",string.Join(";",Gates.Select(g=>g.Id+"="+g.Index)));
 }
 static void Add(Gate gate,string id,ScriptableObject data,string name,Unlockable unlock,bool character,Sprite icon)
 {
  gate.Id=id;gate.Data=data;gate.Name=name;gate.Unlock=unlock;gate.Character=character;gate.Icon=icon;gate.Goal=GoalFor(gate);
  if(unlock!=null)
  {
   gate.Index=Array.IndexOf(NativeSelection.Unlocks(character?Title.characterUnlocker:Title.gunUnlocker),unlock);
   unlockGates.Add(unlock,gate);
  }
  dataGates.Add(data,gate);Gates.Add(gate);
 }
 public static bool Managed(Unlockable unlock)=>unlock!=null&&unlockGates.ContainsKey(unlock);
 public static bool Allowed(ScriptableObject data)=>data==null||!dataGates.TryGetValue(data,out var gate)||State!=null&&State.earned.Contains(gate.Id);
 static bool NativeOwned(Gate g)
 {
  if(g.Index<0)return false;
  var values=(g.Character?SaveSystem.data.characterUnlocks:SaveSystem.data.gunUnlocks).unlocks;
  return g.Index<values.Length&&values[g.Index];
 }
 public static void Load(bool existingSave)
 {
  if(Title==null)return;
  bool journalExists=File.Exists(JournalPath);
  if(journalExists)using(var stream=File.OpenRead(JournalPath))State=(Journal)new DataContractJsonSerializer(typeof(Journal)).ReadObject(stream);
  else State=new Journal();
  // Version 1 stored only queued IDs. Retain their ownership and identify the
  // earlier award honestly; do not claim the revised condition was completed.
  if(State.attempts==null)State.attempts=new List<Attempt>();
  if(State.notices==null)State.notices=new List<Notice>();
  // Earlier journals never recorded per-mode/difficulty win totals. Start an
  // empty record rather than inventing counts from highest-clear arrays.
  if(State.modeClears==null)State.modeClears=new List<ModeClearRecord>();
  if(State.version<2)
   foreach(string id in State.pending)State.notices.Add(new Notice{id=id,goal="Earned under previous unlock requirements"});
  State.version=2;
  RuneProgression.Initialize();
  SourceAchievementRewards.Load();
  foreach(var gate in Gates)
  {
   bool legacyScenario=existingSave&&!journalExists&&gate.Id.StartsWith("scenario:")&&gate.Data.name!="Challenge_Forest"&&(gate.Data.name=="10M_Forest"||gate.Data.name=="Endless_Forest"||SaveSystem.data.difficultyUnlocked>0);
   if(NativeOwned(gate)||legacyScenario)
   {
    if(!State.earned.Contains(gate.Id))State.earned.Add(gate.Id);
    if(!State.pending.Contains(gate.Id)&&!State.notified.Contains(gate.Id))State.notified.Add(gate.Id);
   }
  }
  foreach(var gate in Gates.Where(g=>g.Index>=0&&State.earned.Contains(g.Id)))
   (gate.Character?SaveSystem.data.characterUnlocks:SaveSystem.data.gunUnlocks).unlocks[gate.Index]=true;
  // Old clear arrays cannot prove miniboss/stat/build conjunctions. Only actual
  // new encounters can earn the revised requirements.
  Save();Refresh();
 }
 public static void Save()
 {
  if(State==null)return;
  Directory.CreateDirectory(Application.persistentDataPath);string temp=JournalPath+".writing";
  using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
  {new DataContractJsonSerializer(typeof(Journal)).WriteObject(stream,State);stream.Flush(true);}
  if(File.Exists(JournalPath))File.Replace(temp,JournalPath,null);else File.Move(temp,JournalPath);
 }
 public static void Refresh()
 {
  if(State==null)return;
  foreach(var gate in Gates)
  {
   if(gate.Unlock==null)continue;
   if(State.earned.Contains(gate.Id))gate.Unlock.Unlock();else gate.Unlock.Lock();
   var purchase=(Button)AccessTools.Field(typeof(Unlockable),"unlockButton").GetValue(gate.Unlock);
   if(purchase!=null)purchase.gameObject.SetActive(false);
  }
  if(Title!=null)ModeLockVisual.Refresh(Title.modeSelectMenu);
 }
 public static void InspectLock(Unlockable unlock)
 {
  if(!Managed(unlock))return;
  ((Selectable)AccessTools.Field(typeof(Unlockable),"targetSelectable").GetValue(unlock)).interactable=true;
  var button=(Button)AccessTools.Field(typeof(Unlockable),"unlockButton").GetValue(unlock);if(button!=null)button.gameObject.SetActive(false);
 }
 public static string Description(ScriptableObject data,string description)
 {
  if(State==null||Allowed(data)||!dataGates.TryGetValue(data,out var gate))return description;
  string result=description+"<br><color=#FC525F>Unlock: "+gate.Goal+"</color>";
  if(data is MapData)return "<color=#FC525F>Unlock: "+gate.Goal+"</color>";
  if(data is CharacterData||data is GunData)
  {if(InspectedData!=data)return result;}
  var attempt=State.attempts.FirstOrDefault(a=>a.id==gate.Id);
  return result+"<br>"+(attempt?.text??"No qualifying attempt recorded yet.");
 }
 internal static bool Activate(ScriptableObject data,flanne.UIExtensions.Menu menu,int index)
 {
  if(Allowed(data))return true;
  InspectedData=data;menu.onSelect.Invoke(index);return false;
 }
 static HashSet<string> CurrentPerks()
 {
  var perks=PlayerController.Instance?.playerPerks;
  return perks==null?new HashSet<string>():new HashSet<string>((Equipped(perks)??new List<Powerup>()).Select(p=>PowerKey(p).key));
 }
}

[HarmonyPatch(typeof(SaveSystem),nameof(SaveSystem.Save))]
class AchievementSave{static void Postfix(){RuneProgression.RememberSelections();Progression.Save();}}
[HarmonyPatch(typeof(Unlockable),nameof(Unlockable.Lock))]
class BrowseAchievementLock{static void Postfix(Unlockable __instance){Progression.InspectLock(__instance);}}
[HarmonyPatch]
class AchievementBrowseControls
{
 static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(flanne.UIExtensions.ButtonExtension),"OnSelect"),AccessTools.Method(typeof(flanne.UIExtensions.ButtonExtension),"OnPointerEnter")};
 static void Prefix(flanne.UIExtensions.ButtonExtension __instance)
 {if(__instance.GetComponentInParent<CharacterMenu>()!=null||__instance.GetComponentInParent<GunMenu>()!=null)Progression.InspectedData=null;}
 static void Postfix(flanne.UIExtensions.ButtonExtension __instance){Progression.InspectLock(__instance.GetComponent<Unlockable>());}
}
[HarmonyPatch(typeof(Unlockable),nameof(Unlockable.UnlockWithPoints))]
class NoAchievementPurchase{static bool Prefix(Unlockable __instance)=>!Progression.Managed(__instance);}
[HarmonyPatch(typeof(UnlockAtDarkness),nameof(UnlockAtDarkness.CheckUnlock))]
class AchievementMapLock
{
 static bool Prefix(UnlockAtDarkness __instance)
 {
  var unlock=(Unlockable)AccessTools.Field(typeof(UnlockAtDarkness),"unlockable").GetValue(__instance);
  if(!Progression.Managed(unlock))return true;
  ((TMP_Text)AccessTools.Field(typeof(UnlockAtDarkness),"unlockConTMP").GetValue(__instance)).enabled=false;
  Progression.Refresh();return false;
 }
}
[HarmonyPatch(typeof(CharacterSelectState),"OnClick",new[]{typeof(int)})]
class CharacterGateGuard{static bool Prefix(int i)=>Progression.Activate(Progression.Title.characterMenu[i],Progression.Title.characterMenu,i);}
[HarmonyPatch(typeof(GunSelectState),"OnClick",new[]{typeof(int)})]
class WeaponGateGuard{static bool Prefix(int i)=>Progression.Activate(Progression.Title.gunMenu[i],Progression.Title.gunMenu,i);}
[HarmonyPatch(typeof(ModeSelectState),nameof(ModeSelectState.OnClickPlay))]
class ModeGateGuard{static bool Prefix()=>Progression.Title.modeSelectMenu.currIndex==0||Progression.Allowed(Progression.Title.modeSelectMenu.toggledData);}
[HarmonyPatch(typeof(MapSelectState),nameof(MapSelectState.OnClickPlay))]
class MapGateGuard{static bool Prefix()=>Progression.Allowed(Progression.Title.mapSelectMenu.toggledData);}
[HarmonyPatch(typeof(CharacterData),"get_description")]
class CharacterGoal{static void Postfix(CharacterData __instance,ref string __result){__result=Progression.Description(__instance,__result);}}
[HarmonyPatch(typeof(GunData),"get_description")]
class WeaponGoal{static void Postfix(GunData __instance,ref string __result){__result=Progression.Description(__instance,WeaponFamiliarity.Description(__instance,__result));}}
[HarmonyPatch(typeof(MapData),"get_description")]
class MapGoal{static void Postfix(MapData __instance,ref string __result){__result=Progression.Description(__instance,__result);}}
[HarmonyPatch]
class BossSpawnAchievementBoundary
{
 internal static bool Spawning;
 static System.Reflection.MethodBase TargetMethod()=>AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(BossSpawner),"WaitToSpawnCR"));
 static void Prefix(){Spawning=true;}
 static Exception Finalizer(Exception __exception){Spawning=false;return __exception;}
}

// The original one-off unlock panels interrupt the same achievement with a
// second confirmation. The win screen already owns the paused end-of-run flow;
// retain that native transition and use the shared saved notification queue.
[HarmonyPatch]
class UnifiedClearNotifications
{
 static System.Collections.Generic.IEnumerable<System.Reflection.MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(HasturUnlockedState),"Enter"),AccessTools.Method(typeof(RavenUnlockedState),"Enter")};
 static bool Prefix()
 {
  if(Progression.State==null)return true;
  Object.FindObjectOfType<GameController>().ChangeState<PlayerSurvivedState>();return false;
 }
}
