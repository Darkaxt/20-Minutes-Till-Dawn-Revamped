using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using flanne;
using flanne.Player;
using flanne.Player.Buffs;
using flanne.RuneSystem;
using flanne.PowerupSystem;
using flanne.PerkSystem.Actions;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

public static partial class ResumeRuntime
{
 [Serializable]public class TimedEffect{public string type,method;public int ordinal;public float remaining;}
 [Serializable]public class SavedChange{public string type;public bool flat;public int flatValue;public float value;}
 [Serializable]public class TemporaryEffect{public float remaining;public List<SavedChange> changes=new List<SavedChange>();}
 [Serializable,DataContract]public class ShieldState{[DataMember(IsRequired=true)]public int ordinal;[DataMember(IsRequired=true)]public bool active;[DataMember(IsRequired=true)]public float timer,flashRemaining,cooldown;[DataMember(IsRequired=true)]public ModState rate;
  [DataMember]public bool source,contributes;[DataMember]public int count,lastCount;[DataMember]public float reduction,applyAt;}
 // Old runtime DTOs remain readable; they never reinstall unsaved phases.
 static readonly HashSet<MonoBehaviour> Reviving=new HashSet<MonoBehaviour>();
 internal static bool RevivalInProgress=>Reviving.Count>0;
 static readonly Dictionary<Type,string[]> FlowMethods=new Dictionary<Type,string[]>
 {
  {typeof(MomentumRune),new[]{"StartBuffCR"}},{typeof(BuffOnXP),new[]{"StartBuffCR"}},
  {typeof(FireRateBuffOnGainAmmo),new[]{"BuffCR"}},{typeof(TemporaryRefreshingBuff),new[]{"BuffCR"}},{typeof(FireRateOnHurt),new[]{"StatBoostCR"}},
  {typeof(ElementalBarrageRune),new[]{"StartBonusCR","WaitForCooldownCR"}},{typeof(LastStandRune),new[]{"InvulnCR"}},
  {typeof(EtherealRune),new[]{"EtherealStateCR"}},{typeof(ProjectileShieldRune),new[]{"WaitForCooldownCR"}}
 };
 internal static IEnumerator TrackRevival(MonoBehaviour owner,IEnumerator native)
 {
  Reviving.Add(owner);
  try{while(native.MoveNext())yield return native.Current;}finally{Reviving.Remove(owner);}
 }
 static void ForgetEffects()=>Reviving.Clear();
 static void ValidateEffects(State state)
 {
  if(state.shields.Any(s=>s.source&&(!state.protectionFlips.HasValue||float.IsNaN(s.reduction)||float.IsInfinity(s.reduction)||float.IsNaN(s.applyAt)||float.IsInfinity(s.applyAt))))throw new InvalidOperationException("Unsupported counted shield checkpoint");
  if(state.effects==null||state.effects.Select(s=>s.id).Distinct().Count()!=state.effects.Count)throw new InvalidOperationException("Unsupported native perk effect checkpoint");
  foreach(var flow in state.timed)
  {var type=FlowMethods.Keys.SingleOrDefault(t=>t.FullName==flow.type);if(type==null||flow.ordinal<0||!FlowMethods[type].Contains(flow.method)||float.IsNaN(flow.remaining)||float.IsInfinity(flow.remaining)||flow.remaining<0)throw new InvalidOperationException("Unsupported native effect checkpoint");}
  foreach(var temp in state.temporary)
   if(temp.remaining<0||float.IsNaN(temp.remaining)||float.IsInfinity(temp.remaining)||temp.changes.Any(s=>!Enum.TryParse<StatType>(s.type,out var type)||type<0||type>=StatType.Count))throw new InvalidOperationException("Unsupported native temporary effect checkpoint");
 }
}
[HarmonyPatch(typeof(SecondWindRune),"HealAnimationCR")]
class ResumeRevivalBoundary
{static void Postfix(SecondWindRune __instance,ref IEnumerator __result){__result=ResumeRuntime.TrackRevival(__instance,__result);}}
