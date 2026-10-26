using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using flanne;
using flanne.UI;
namespace TwentyMinutes.Modernization;

public static class ModeClearRecords
{
 static string Mode(MapData map)
 {
  if(map==null||CustomMode.Active&&map==SelectedMap.MapData)return null;
  if(map.name=="20M_Forest"||map.name=="20M_Temple"||map.name=="20M_PumpkinPatch")return "Standard";
  if(map.name=="10M_Forest")return "Quickplay";
  return map.name=="Challenge_Forest"?"Challenge":null;
 }
 public static int Count(string mode,int difficulty)=>Progression.State.modeClears.FirstOrDefault(r=>r.mode==mode&&r.difficulty==difficulty)?.count??0;
 internal static void Record()
 {
  var recorder=RunRecorder.Instance;var map=SelectedMap.MapData;string mode=Mode(map);
  if(mode==null||recorder.ModeClearRecorded)return;
  int difficulty=map.darknessDisabled?0:Loadout.difficultyLevel;
  var record=Progression.State.modeClears.FirstOrDefault(r=>r.mode==mode&&r.difficulty==difficulty);
  if(record==null){record=new Progression.ModeClearRecord{mode=mode,difficulty=difficulty};Progression.State.modeClears.Add(record);}
  record.count++;recorder.ModeClearRecorded=true;Progression.Save();
  Plugin.Trace("mode-clear-recorded",new{mode,difficulty,record.count});
 }
 public static void Refresh()
 {
  var title=Progression.Title;if(title==null||Progression.State?.modeClears==null)return;
  foreach(var toggle in ChallengeMode.Toggles(title.modeSelectMenu))
  {
   var map=toggle.GetComponent<MapEntry>().data;string mode=Mode(map);
   var badge=toggle.transform.Find("ModeClearCrown");
   if(mode==null){if(badge!=null)badge.gameObject.SetActive(false);continue;}
   if(badge==null)
   {
    var clone=Object.Instantiate(NativeSelection.VictoryObjects(title.gunVictories)[0],toggle.transform);clone.name="ModeClearCrown";badge=clone.transform;
    var rect=(RectTransform)badge;rect.anchorMin=rect.anchorMax=new Vector2(.5f,0);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=new Vector2(0,12);rect.localScale=Vector3.one;
    foreach(var graphic in clone.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))graphic.raycastTarget=false;
   }
   int difficulty=mode=="Quickplay"?0:mode=="Challenge"?(ChallengeMode.Selected?title.difficultyController.difficulty:Progression.State.challengeDifficultyUnlocked):ChallengeMode.NormalDifficulty;
   int count=Count(mode,difficulty);badge.GetComponentInChildren<TMP_Text>(true).text=count.ToString();badge.gameObject.SetActive(count>0);
  }
  ((TMP_Text)AccessTools.Field(typeof(DifficultyController),"unlockTMP").GetValue(title.difficultyController)).text="";
 }
}
[HarmonyPatch(typeof(DifficultyController),"SetDifficulty")]
class ModeClearDifficultyChange{static void Postfix(){ModeClearRecords.Refresh();}}
