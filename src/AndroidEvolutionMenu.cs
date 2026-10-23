using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using flanne.Core;
using flanne.UI;
namespace TwentyMinutes.Modernization;

// Android GunEvoPanel.Show receives its callback during GenerateEvolutions.
// Native shows the panel then waits another0.2s before subscribing its menu.
// Move that existing native binding before Show; preserve its input adapter,
// both source waits and the unchanged closing/evolution coroutine.
[HarmonyPatch]
class AndroidEvolutionMenuBinding
{
 static MethodBase TargetMethod()=>AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(GunEvoMenuState),"PlayGunEvoAnimationCR"));
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  var code=instructions.ToList();var subscribe=AccessTools.Method(typeof(Menu),"add_ClickEvent");
  int end=Enumerable.Range(0,code.Count).Single(i=>code[i].Calls(subscribe));
  var binding=code.GetRange(end-5,6);code.RemoveRange(end-5,6);
  var generate=AccessTools.Method(typeof(GunEvoMenuState),"GenerateEvolutions");
  int generated=Enumerable.Range(0,code.Count).Single(i=>code[i].Calls(generate));
  code.InsertRange(generated+1,binding);return code;
 }
}
