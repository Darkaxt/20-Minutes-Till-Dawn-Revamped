using System.Collections;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.UI;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckAeroDescription()
 {
  yield return AchievementRun("Shana","RevolverData","20M_Forest");var game=Object.FindObjectOfType<GameController>();
  Object.FindObjectOfType<HordeSpawner>().enabled=false;
  var pick=AchievementPick(game,"AeroMagic",game.playerXP.level);
  bool inspected=false;
  while(pick.MoveNext())
  {
   if(!inspected&&game.CurrentState is PowerupMenuState)
   {
    foreach(var description in Object.FindObjectsOfType<PowerupDescription>())
    {
     if(description.data==null||description.data.name!="AeroMagic")continue;
     var label=(TMP_Text)AccessTools.Field(typeof(PowerupDescription),"descriptionTMP").GetValue(description);
     Log("native-aero-description",label.text);
     Require(!Plugin.Text.ContainsKey("aero_magic_description_2")&&label.text==LocalizationSystem.GetDictionaryForEditor()["aero_magic_description_2"],"native offered Aero Magic retains its already-correct original suffixed description without a mod override");
     inspected=true;yield return Capture("aero-native-description.png");
    }
   }
   yield return pick.Current;
  }
  Require(inspected,"original description inspected in the actual native level-up offer");
  yield return CheckRevolver(game,PlayerController.Instance,"aero-native-combat.png");yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete","Aero Magic original native description already correct; no mod override; normal choice/combat and exit pass");Application.Quit(0);
 }
}
