using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using flanne;
namespace TwentyMinutes.Modernization;

// Android keeps these accumulated floats, rather than reconstructing them from
// the tick counter when removing a buff. They are absent from the native types.
internal static class AndroidAccumulatedBonuses
{
 internal sealed class Accumulated{internal float Damage,Movement;}
 static readonly ConditionalWeakTable<MonoBehaviour,Accumulated> Values=new ConditionalWeakTable<MonoBehaviour,Accumulated>();
 internal static Accumulated Get(MonoBehaviour owner)=>Values.GetValue(owner,_=>new Accumulated());
 internal static readonly AccessTools.FieldRef<DmgAndMSOnNotHurt,int> Ticks=AccessTools.FieldRefAccess<DmgAndMSOnNotHurt,int>("_ticks");
 internal static readonly AccessTools.FieldRef<DmgAndMSOnNotHurt,float> Timer=AccessTools.FieldRefAccess<DmgAndMSOnNotHurt,float>("_timer");
 internal static readonly AccessTools.FieldRef<DamageBuffOnCurseKill,int> CurseKills=AccessTools.FieldRefAccess<DamageBuffOnCurseKill,int>("_killCounter");
 internal static readonly AccessTools.FieldRef<GainMaxHPOnSmiteKill,int> SmiteKills=AccessTools.FieldRefAccess<GainMaxHPOnSmiteKill,int>("_killCounter");
 internal static readonly AccessTools.FieldRef<GainMaxHPOnSmiteKill,int> GainedHealth=AccessTools.FieldRefAccess<GainMaxHPOnSmiteKill,int>("_mhpGainCounter");
 internal static SpriteTrail Trail(DmgAndMSOnNotHurt owner)=>(SpriteTrail)AccessTools.Field(typeof(DmgAndMSOnNotHurt),"spriteTrail").GetValue(owner);
 internal static void RestoreTrail(DmgAndMSOnNotHurt owner)
 {
  var trail=Trail(owner);
  // This lookup and its player check are part of the source restore method.
  if(trail==null&&PlayerController.Instance!=null)
  {trail=PlayerController.Instance.playerSprite.GetComponentInChildren<SpriteTrail>();Plugin.SetField(owner,"spriteTrail",trail);}
  int maximum=(int)AccessTools.Field(typeof(DmgAndMSOnNotHurt),"maxTicks").GetValue(owner);
  trail.SetEnabled(Ticks(owner)>=maximum/2);
 }
}
[HarmonyPatch]
class AndroidAccumulatedNotificationLifetime
{
 static IEnumerable<MethodBase> TargetMethods()
 {
  foreach(var type in new[]{typeof(DamageBuffOnCurseKill),typeof(GainMaxHPOnSmiteKill)})
  {yield return AccessTools.Method(type,"Start");yield return AccessTools.Method(type,"OnDestroy");}
 }
 static bool Prefix(MonoBehaviour __instance,MethodBase __originalMethod)
 {
  bool curse=__instance is DamageBuffOnCurseKill;
  var handler=(Action<object,object>)Delegate.CreateDelegate(typeof(Action<object,object>),__instance,AccessTools.Method(__instance.GetType(),curse?"OnCurseKill":"OnSmiteKill"));
  string notification=curse?CurseSystem.CurseKillNotification:(string)AccessTools.Field(typeof(SmitePassive),"SmiteKillNotification").GetValue(null);
  if(__originalMethod.Name=="Start")__instance.AddObserver(handler,notification);else __instance.RemoveObserver(handler,notification);
  return false;
 }
}
[HarmonyPatch(typeof(DamageBuffOnCurseKill),"OnCurseKill")]
class AndroidRitualAccumulation
{
 static bool Prefix(DamageBuffOnCurseKill __instance,ref int ____killCounter,float ___damageBuff,float ___numKillsPerBuff)
 {
  unchecked{____killCounter++;}
  if((float)____killCounter>=___numKillsPerBuff)
  {
   ____killCounter=0;PlayerController.Instance.stats[StatType.BulletDamage].AddMultiplierBonus(___damageBuff);
   AndroidAccumulatedBonuses.Get(__instance).Damage+=___damageBuff;
  }
  return false;
 }
}
[HarmonyPatch]
class AndroidUnharmedLifetime
{
 static IEnumerable<MethodBase> TargetMethods()
 {yield return AccessTools.Method(typeof(DmgAndMSOnNotHurt),"Start");yield return AccessTools.Method(typeof(DmgAndMSOnNotHurt),"OnDestroy");}
 static bool Prefix(DmgAndMSOnNotHurt __instance,MethodBase __originalMethod)
 {
  var handler=(UnityAction)Delegate.CreateDelegate(typeof(UnityAction),__instance,AccessTools.Method(typeof(DmgAndMSOnNotHurt),"OnHurt"));
  if(__originalMethod.Name=="Start")
  {Plugin.SetField(__instance,"spriteTrail",PlayerController.Instance.playerSprite.GetComponentInChildren<SpriteTrail>());PlayerController.Instance.playerHealth.onHurt.AddListener(handler);}
  else PlayerController.Instance.playerHealth.onHurt.RemoveListener(handler);
  return false;
 }
}
[HarmonyPatch(typeof(DmgAndMSOnNotHurt),"Update")]
class AndroidUnharmedAccumulation
{
 static bool Prefix(DmgAndMSOnNotHurt __instance,ref int ____ticks,ref float ____timer,int ___maxTicks,float ___secsPerTick,float ___damageBoostPerTick,float ___movespeedBoostPerTick,SpriteTrail ___spriteTrail)
 {
  if(____ticks<___maxTicks)____timer+=Time.deltaTime;
  if(____timer>=___secsPerTick)
  {
   ____timer-=___secsPerTick;unchecked{____ticks++;}
   PlayerController.Instance.stats[StatType.BulletDamage].AddMultiplierBonus(___damageBoostPerTick);
   PlayerController.Instance.stats[StatType.MoveSpeed].AddMultiplierBonus(___movespeedBoostPerTick);
   var accumulated=AndroidAccumulatedBonuses.Get(__instance);accumulated.Damage+=___damageBoostPerTick;accumulated.Movement+=___movespeedBoostPerTick;
   if(____ticks>=___maxTicks/2)___spriteTrail.SetEnabled(true);
  }
  return false;
 }
}
[HarmonyPatch(typeof(DmgAndMSOnNotHurt),"OnHurt")]
class AndroidUnharmedRemoval
{
 static bool Prefix(DmgAndMSOnNotHurt __instance,ref int ____ticks,ref float ____timer,SpriteTrail ___spriteTrail)
 {
  var accumulated=AndroidAccumulatedBonuses.Get(__instance);
  PlayerController.Instance.stats[StatType.BulletDamage].AddMultiplierBonus(-accumulated.Damage);
  PlayerController.Instance.stats[StatType.MoveSpeed].AddMultiplierBonus(-accumulated.Movement);
  ___spriteTrail.SetEnabled(false);____ticks=0;____timer=0;accumulated.Damage=0;accumulated.Movement=0;
  return false;
 }
}
[HarmonyPatch(typeof(GainMaxHPOnSmiteKill),"OnSmiteKill")]
class AndroidJusticeLiveOwner
{
 static PlayerController Player(GainMaxHPOnSmiteKill owner)=>PlayerController.Instance;
 static StatsHolder Stats(GainMaxHPOnSmiteKill owner)=>PlayerController.Instance.stats;
 static PlayerHealth Health(GainMaxHPOnSmiteKill owner)=>PlayerController.Instance.playerHealth;
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  int reads=0;
  foreach(var instruction in instructions)
  {
   if(instruction.opcode==OpCodes.Ldfld&&instruction.operand is FieldInfo field&&field.DeclaringType==typeof(GainMaxHPOnSmiteKill))
   {
    string target=field.Name=="player"?nameof(Player):field.Name=="playerStats"?nameof(Stats):field.Name=="playerHealth"?nameof(Health):null;
    if(target!=null){instruction.opcode=OpCodes.Call;instruction.operand=AccessTools.Method(typeof(AndroidJusticeLiveOwner),target);reads++;}
   }
   yield return instruction;
  }
  if(reads!=4)throw new InvalidOperationException("Unsupported native Justice player-owner boundary");
 }
}
