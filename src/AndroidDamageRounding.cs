using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Android floors signed negative HP changes at each notification stage.
// Windows TakeDamage passes positive damage: the equivalent is a ceiling.
// Preserve that distinction explicitly at both native API boundaries.
[HarmonyPatch]
class AndroidDamageRounding
{
 [ThreadStatic]internal static bool PositiveDamageStage;
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Health),nameof(Health.TakeDamage)),AccessTools.Method(typeof(Health),nameof(Health.HPChange))};
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,MethodBase __originalMethod)
 {
  var original=AccessTools.Method(typeof(IntExtensions),nameof(IntExtensions.NotifyModifiers),new[]{typeof(int),typeof(string),typeof(object)});
  int count=0;
  foreach(var instruction in instructions)
  {
   if(instruction.Calls(original)){instruction.operand=AccessTools.Method(typeof(AndroidDamageRounding),__originalMethod.Name==nameof(Health.TakeDamage)?nameof(DamageModifiers):nameof(SignedModifiers));count++;}
   yield return instruction;
  }
  if(count!=(__originalMethod.Name==nameof(Health.TakeDamage)?2:1))throw new InvalidOperationException("Unsupported native health notification pipeline: "+__originalMethod.Name);
 }
 static int DamageModifiers(int value,string notification,object owner)=>Modifiers(value,notification,owner,true);
 internal static int SignedModifiers(int value,string notification,object owner)=>Modifiers(value,notification,owner,false);
 static int Modifiers(int value,string notification,object owner,bool positiveDamage)
 {
  var modifiers=new List<ValueModifier>();bool previous=PositiveDamageStage;PositiveDamageStage=positiveDamage;
  try{owner.PostNotification(notification,modifiers);}finally{PositiveDamageStage=previous;}
  modifiers.Sort((a,b)=>a.sortOrder.CompareTo(b.sortOrder));float current=value;
  foreach(var modifier in modifiers)current=modifier.Modify(value,current);
  return positiveDamage?Mathf.CeilToInt(current):Mathf.FloorToInt(current);
 }
}
