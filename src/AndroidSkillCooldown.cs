using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using flanne.CharacterPassives;
namespace TwentyMinutes.Modernization;

// Android adds cooldown to the live timer after PerformSkill returns. Native
// does it before the virtual call. Keep the existing gate, input and sound.
[HarmonyPatch(typeof(SkillPassive),"PerformSkillCallback")]
class AndroidSkillCooldownOrder
{
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  var code=instructions.ToList();var timer=AccessTools.Field(typeof(SkillPassive),"_timer");
  int stored=Enumerable.Range(0,code.Count).Single(i=>code[i].opcode==OpCodes.Stfld&&Equals(code[i].operand,timer));
  var increment=code.GetRange(stored-6,7);code.RemoveRange(stored-6,7);
  var perform=AccessTools.Method(typeof(SkillPassive),"PerformSkill");
  int performed=Enumerable.Range(0,code.Count).Single(i=>code[i].Calls(perform));
  code.InsertRange(performed+1,increment);return code;
 }
}
