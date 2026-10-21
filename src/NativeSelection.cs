using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using flanne;
using flanne.UI;
using flanne.UIExtensions;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// Menu indices and unlock-array indices are separate native concepts. Append
// each independently, keeping every original content/progression index stable.
public static class NativeSelection
{
 public static int GunIndex=-1,UnlockIndex=-1,GunCount;
 public static readonly AccessTools.FieldRef<UnlockablesManager,Unlockable[]> Unlocks=AccessTools.FieldRefAccess<UnlockablesManager,Unlockable[]>("unlockables");
 public static readonly AccessTools.FieldRef<VictoryAchievedUI,GameObject[]> VictoryObjects=AccessTools.FieldRefAccess<VictoryAchievedUI,GameObject[]>("uiObjs");
 public static readonly AccessTools.FieldRef<VictoryAchievedUI,TMP_Text[]> VictoryLabels=AccessTools.FieldRefAccess<VictoryAchievedUI,TMP_Text[]>("achievedUIs");
 public static List<DataUI<GunData>> GunItems(GunMenu menu)=>(List<DataUI<GunData>>)AccessTools.Field(typeof(DataListMenu<GunData>),"dataUIs").GetValue(menu);
 public static List<ButtonExtension> GunEntries(GunMenu menu)=>(List<ButtonExtension>)AccessTools.Field(typeof(flanne.UIExtensions.Menu),"entries").GetValue(menu);
 public static List<ButtonExtension> GunEntriesFor(flanne.UIExtensions.Menu menu)=>(List<ButtonExtension>)AccessTools.Field(typeof(flanne.UIExtensions.Menu),"entries").GetValue(menu);
 public static string Relative(Transform root,Transform child)
 {
  var names=new List<string>();while(child!=root){if(child==null)throw new InvalidOperationException("Native victory badge is outside its gun slot");names.Insert(0,child.name);child=child.parent;}return string.Join("/",names);
 }
 public static void Append(TitleScreenController title)
 {
  var items=GunItems(title.gunMenu);var entries=GunEntries(title.gunMenu);
  if(items.Any(x=>Plugin.TryMode(x.data,out var mode)&&mode==Evolution.None))return;
  var revolver=items.Single(x=>x.data.name=="RevolverData");Plugin.PrepareGun(revolver.data);
  var template=items.First(x=>x.GetComponent<Unlockable>()!=null);
  int templateIndex=items.IndexOf(template);GunIndex=items.Count;GunCount=GunIndex+1;
  var copy=Object.Instantiate(template.gameObject,template.transform.parent);copy.name="ModernizationChargeCannonSelection";
  // GunIconUI.SetProperties is empty in this build; each stock slot has its own
  // serialized sprite animator. Bind the added slot explicitly to its own art.
  var icon=copy.GetComponentsInChildren<Image>(true).Single(x=>x.sprite==template.data.icon);
  var iconAnimator=icon.GetComponent<Animator>();if(iconAnimator!=null)iconAnimator.enabled=false;
  icon.sprite=Plugin.BaseGun.icon;icon.preserveAspect=true;
  var ui=copy.GetComponent<DataUI<GunData>>();Plugin.SetField(ui,"defaultData",Plugin.BaseGun);ui.Set(Plugin.BaseGun);
  items.Add(ui);entries.Add(copy.GetComponent<ButtonExtension>());
  copy.transform.SetAsLastSibling();
  if(copy.transform.parent.GetComponent<LayoutGroup>()==null)
  {
   var last=items[GunIndex-1].GetComponent<RectTransform>();var prior=items[GunIndex-2].GetComponent<RectTransform>();
   copy.GetComponent<RectTransform>().anchoredPosition=last.anchoredPosition+(last.anchoredPosition-prior.anchoredPosition);
  }
  // Native Menu.Show installs the normal select/click/cancel listeners later.
  // Cloning the prefab retains its native button/pointer semantics and artwork.
  var unlocked=Unlocks(title.gunUnlocker);UnlockIndex=unlocked.Length;
  Unlocks(title.gunUnlocker)=unlocked.Concat(new[]{copy.GetComponent<Unlockable>()}).ToArray();
  var badges=VictoryObjects(title.gunVictories);
  if(badges.Length!=GunIndex)throw new InvalidOperationException("Unsupported native gun/victory index layout");
  var badge=copy.transform.Find(Relative(template.transform,badges[templateIndex].transform)).gameObject;
  VictoryObjects(title.gunVictories)=badges.Concat(new[]{badge}).ToArray();
  var labels=VictoryLabels(title.gunVictories);
  if(labels!=null)VictoryLabels(title.gunVictories)=labels.Concat(new[]{badge.GetComponentInChildren<TMP_Text>(true)}).ToArray();
  Plugin.Trace("native-selection-appended",new{gunIndex=GunIndex,unlockIndex=UnlockIndex,originalGuns=GunIndex,totalGuns=items.Count,revolverPreserved=revolver.data.name=="RevolverData",layout=copy.transform.parent.GetComponent<LayoutGroup>()?.GetType().Name});
 }
 public static void ExtendProgression(SaveData data)
 {
  if(GunIndex<0)return; // SaveSystem can be called before the title scene exists.
  if(data.gunUnlocks==null)data.gunUnlocks=new UnlockData(UnlockIndex+1);
  if(data.gunUnlocks.unlocks==null)data.gunUnlocks.unlocks=new bool[UnlockIndex+1];
  if(data.gunUnlocks.unlocks.Length<=UnlockIndex)Array.Resize(ref data.gunUnlocks.unlocks,UnlockIndex+1);
  int previous=data.gunHighestClear?.Length??0;
  if(previous<GunCount)
  {
   Array.Resize(ref data.gunHighestClear,GunCount);
   for(int i=previous;i<GunCount;i++)data.gunHighestClear[i]=-1;
  }
 }
}
[HarmonyPatch(typeof(TitleScreenController),"Start")]
class AppendChargeCannon{static void Prefix(TitleScreenController __instance){NativeSelection.Append(__instance);Characters.Append(__instance);ChallengeMode.Append(__instance.modeSelectMenu);Progression.Register(__instance);}}
[HarmonyPatch(typeof(SaveSystem),nameof(SaveSystem.Load))]
class ExtendNativeSave
{
 static void Prefix(out bool __state){__state=System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath,"gamedata.json"))||System.IO.File.Exists(System.IO.Path.Combine(Application.persistentDataPath,"gamedata_backup.json"));}
 static void Postfix(bool __state){NativeSelection.ExtendProgression(SaveSystem.data);Characters.Extend(SaveSystem.data);Progression.Load(__state);}
}
