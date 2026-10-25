using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.UI;
namespace TwentyMinutes.Modernization;

// Native fullscreen disables Resolution and adopts the virtual desktop size.
// Retain that default until a resolution is selected; then honor its native
// saved index in both window modes, through one display request.
static class DisplayOptions
{
 static T Read<T>(OptionsSetter owner,string field)=>(T)AccessTools.Field(typeof(OptionsSetter),field).GetValue(owner);
 static string Label(OptionsSetter owner,string field)=>LocalizationSystem.GetLocalizedValue(Read<LocalizedString>(owner,field).key);
 static void Apply(OptionsSetter owner,Vector2Int size,bool fullscreen)
 {
  Screen.SetResolution(size.x,size.y,fullscreen);
  Read<TMP_Text>(owner,"resolutionTMP").text=Label(owner,"resolutionLabel")+" "+size.x+"x"+size.y;
 }
 internal static void SetFullscreen(OptionsSetter owner,bool fullscreen)
 {
  Read<TMP_Text>(owner,"fullscreenTMP").text=Label(owner,"fullscreenLabel")+" "+Label(owner,fullscreen?"onString":"offString");
  Read<Button>(owner,"resolutionButton").interactable=true;
  PlayerPrefs.SetInt("Fullscreen",fullscreen?1:0);
  if(fullscreen&&!PlayerPrefs.HasKey("ResolutionIndex"))
  {var desktop=Screen.currentResolution;Apply(owner,new Vector2Int(desktop.width,desktop.height),true);}
  else SetResolution(owner,Read<int>(owner,"_resolutionIndex"));
 }
 internal static void SetResolution(OptionsSetter owner,int index)
 {Apply(owner,Read<Vector2Int[]>(owner,"SupportedResolutions")[index],PlayerPrefs.GetInt("Fullscreen",1)==1);}
}
[HarmonyPatch(typeof(OptionsSetter),"SetFullscreen")]
class NativeFullscreenResolution
{static bool Prefix(OptionsSetter __instance,bool isFS){DisplayOptions.SetFullscreen(__instance,isFS);return false;}}
[HarmonyPatch(typeof(OptionsSetter),"SetResolution")]
class NativeSelectedResolution
{static bool Prefix(OptionsSetter __instance,int resolutionIndex){DisplayOptions.SetResolution(__instance,resolutionIndex);return false;}}
