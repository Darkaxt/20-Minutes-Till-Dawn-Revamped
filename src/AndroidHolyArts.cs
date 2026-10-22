using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

[Serializable]
public sealed class AndroidHolyArtsAcquire:flanne.PerkSystem.Action
{
 public override void Activate(GameObject target)
 {
  // The source prefab contains only Transform and SmitePassive. Its visual
  // payload is the matching original Windows PF_SmiteFX asset.
  var fx=Resources.FindObjectsOfTypeAll<GameObject>().Single(g=>g.name=="PF_SmiteFX"&&!g.scene.IsValid());
  var helper=new GameObject("PF_SmitePassive");helper.transform.SetParent(PlayerController.Instance.transform,false);
  var smite=helper.AddComponent<SmitePassive>();Plugin.SetField(smite,"smiteFXPrefab",fx);
  Plugin.SetField(smite,"range",4f);Plugin.SetField(smite,"baseDamage",20);Plugin.SetField(smite,"damageBonusPerHP",10);
  // The Android serialized soundFX is null; preserve that source behavior.
 }
}
[HarmonyPatch]
class AndroidHolyArtsRecipe
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  if(RunResume.Key(__instance)!="holy_arts_name")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidHolyArtsAcquire());
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
[HarmonyPatch]
class AndroidSmiteDamage
{
 static MethodBase TargetMethod()=>AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(SmitePassive),"SmiteCR"));
 static void ChangeHealth(Health health,int change)=>AndroidDamageRounding.ChangeHealth(health,change,DamageType.Smite);
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  int calls=0;var native=AccessTools.Method(typeof(Health),nameof(Health.HPChange));
  foreach(var instruction in instructions)
  {if(instruction.Calls(native)){instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(AndroidSmiteDamage),nameof(ChangeHealth));calls++;}yield return instruction;}
  if(calls!=1)throw new InvalidOperationException("Unsupported native Smite health boundary");
 }
}
