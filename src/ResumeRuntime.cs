using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.RuneSystem;
using flanne.PowerupSystem;
using flanne.PowerupSystems;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// Explicit native counterparts of the recovered Android runtime save handlers.
// Unity references, event subscriptions and arbitrary object graphs are rebuilt
// through the normal perk/rune lifecycle, never persisted by this reader.
public static partial class ResumeRuntime
{
 [Serializable]public class ModState{public int flat;public float bonus,reduction;}
 [Serializable]public class FieldValue{public string field,value;}
 [Serializable]public class ProviderState{public string type;public int ordinal;public List<FieldValue> values=new List<FieldValue>();}
 [Serializable]public class SummonState{public string id;public int ordinal,damage;public ModState speed;}
 [Serializable]public class EggState{public int ordinal;public float remaining;public bool hatched;}
 [Serializable]public class State
 {
  public List<ModState> stats=new List<ModState>();public ModState xpMultiplier;
  public List<ProviderState> providers=new List<ProviderState>();
  public List<SummonState> summons=new List<SummonState>();public List<EggState> eggs=new List<EggState>();
  public List<TimedEffect> timed=new List<TimedEffect>();public List<TemporaryEffect> temporary=new List<TemporaryEffect>();
  public List<string> liveOneUseRunes=new List<string>();public List<bool> singleShotActions=new List<bool>();
  public List<ShieldState> shields=new List<ShieldState>();
  public List<EffectState> effects=new List<EffectState>();
  public int? protectionFlips;
 }
 static readonly Dictionary<Type,string[]> Fields=new Dictionary<Type,string[]>
 {
  {typeof(AndroidSpearBonuses),new[]{"HpBonus","SoulBonus"}},
  {typeof(StabbingSummon),new[]{"_timer"}},
  {typeof(ShootingSummon),new[]{"_timer"}},
  {typeof(AndroidBestFriends),new[]{"Factor"}},
  {typeof(AndroidShootingFlags),new[]{"BulletEffect","BurnDamage"}},
  {typeof(AndroidReflex),new[]{"LastMove","Granted"}},
  {typeof(AndroidGlare),new[]{"Timer"}},
  {typeof(ThunderDuringHolyShield),new[]{"_timer"}},
  {typeof(AndroidBodySoul),new[]{"Grants","Remaining"}},
  {typeof(EtherealRune),new[]{"_counter"}},
  {typeof(DropHPOnKillRune),new[]{"_counter"}},
  {typeof(BuffSummonDamageOverTime),new[]{"_timer"}},
  {typeof(BuffSummonAttackSpeedOverTime),new[]{"_timer"}},
  {typeof(DmgAndMSOnNotHurt),new[]{"_ticks","_timer"}},
  {typeof(DamageBuffOnCurseKill),new[]{"_killCounter"}},
  {typeof(GainMaxHPOnSmiteKill),new[]{"_killCounter","_mhpGainCounter"}},
  {typeof(TripleNextPowerup),new[]{"used"}},
  {typeof(BuffPlayerStats),new[]{"_stacks"}}
  ,{typeof(FireRateAndPierceOnNotHurt),new[]{"_ticks","_timer"}}
  ,{typeof(HPThresholdStatRune),new[]{"statsActive"}}
  ,{typeof(MomentumRune),new[]{"_timer"}}
  ,{typeof(DamageUpOnReload),new[]{"_timer"}}
  ,{typeof(ReloadRateUpOnKill),new[]{"_stacks"}}
  ,{typeof(BuffOnXP),new[]{"_timer"}}
  ,{typeof(BuffWhenStanding),new[]{"_ticks","_timer"}}
  ,{typeof(FireRateBuffOnGainAmmo),new[]{"_timer"}}
  ,{typeof(TemporaryRefreshingBuff),new[]{"_timer"}}
  ,{typeof(ElementalBarrageRune),new[]{"inflictCounter","disableInflictGain"}}
 };
 static readonly Dictionary<SummonEgg,float> EggStarts=new Dictionary<SummonEgg,float>();
 public static void EggStarted(SummonEgg egg){EggStarts[egg]=Time.time;}
 public static void ForgetEggs(){EggStarts.Clear();ForgetEffects();}
 public static ModState Capture(StatMod stat)=>new ModState{flat=stat.FlatBonus,
  bonus=(float)AccessTools.Field(typeof(StatMod),"_multiplierBonus").GetValue(stat),
  reduction=(float)AccessTools.Field(typeof(StatMod),"_multiplierReduction").GetValue(stat)};
 public static void Restore(StatMod stat,ModState saved)
 {
  Plugin.SetField(stat,"_flatBonus",saved.flat);Plugin.SetField(stat,"_multiplierBonus",saved.bonus);Plugin.SetField(stat,"_multiplierReduction",saved.reduction);
  // Notify through the native mutation boundary after all three values agree.
  stat.AddFlatBonus(0);
 }
 static AttackingSummon[] OwnedAttackingSummons(PlayerController player)=>player.GetComponentsInChildren<ShootingSummon>(true).Cast<AttackingSummon>().Concat(player.GetComponent<AndroidSpearBonuses>()?.Spears.Where(s=>s!=null)??Enumerable.Empty<StabbingSummon>()).ToArray();
 static MonoBehaviour[] Providers(PlayerController player,Type type)=>type==typeof(StabbingSummon)?OwnedAttackingSummons(player).OfType<StabbingSummon>().Cast<MonoBehaviour>().ToArray():player.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null&&c.GetType()==type).ToArray();
 public static State Capture(PlayerController player)
 {
  var state=new State();
  for(int i=0;i<(int)StatType.Count;i++)state.stats.Add(Capture(player.stats[(StatType)i]));
  state.xpMultiplier=Capture(player.GetComponentInChildren<flanne.Player.PlayerXP>().xpMultiplier);
  foreach(var pair in Fields)
  {
   var providers=Providers(player,pair.Key);
   for(int i=0;i<providers.Length;i++)
   {
    var entry=new ProviderState{type=pair.Key.FullName,ordinal=i};
    foreach(string field in pair.Value)entry.values.Add(new FieldValue{field=field,value=Convert.ToString(AccessTools.Field(pair.Key,field).GetValue(providers[i]),CultureInfo.InvariantCulture)});
    state.providers.Add(entry);
   }
  }
  foreach(var group in OwnedAttackingSummons(player).GroupBy(s=>s.SummonTypeID))
  {int ordinal=0;foreach(var summon in group)state.summons.Add(new SummonState{id=group.Key,ordinal=ordinal++,damage=(int)AccessTools.Field(summon.GetType(),"baseDamage").GetValue(summon),speed=Capture(summon.attackSpeedMod)});}
  var eggs=player.GetComponentsInChildren<SummonEgg>(true);
  for(int i=0;i<eggs.Length;i++)
  {
   var egg=eggs[i];float duration=(float)AccessTools.Field(typeof(SummonEgg),"secondsToHatch").GetValue(egg);
   bool hatched=!egg.gameObject.activeSelf;
   if(!hatched&&!EggStarts.TryGetValue(egg,out _))throw new InvalidOperationException("Cannot save an egg before its native Start boundary");
   state.eggs.Add(new EggState{ordinal=i,hatched=hatched,remaining=hatched?0:Mathf.Max(0,duration-(Time.time-EggStarts[egg]))});
  }
  CaptureEffects(player,state);return state;
 }
 public static void Validate(State state)
 {
  if(state==null||state.stats==null||state.stats.Count!=(int)StatType.Count||state.xpMultiplier==null||state.providers==null||state.summons==null||state.eggs==null||state.timed==null||state.temporary==null||state.liveOneUseRunes==null||state.singleShotActions==null||state.shields==null)throw new InvalidOperationException("Run checkpoint has an unsupported native runtime contract");
  foreach(var provider in state.providers)
  {
   var type=Fields.Keys.SingleOrDefault(t=>t.FullName==provider.type);
   if(type==null||provider.ordinal<0||!provider.values.Select(v=>v.field).OrderBy(s=>s).SequenceEqual(Fields[type].OrderBy(s=>s)))throw new InvalidOperationException("Run checkpoint has an unsupported runtime provider");
   foreach(var value in provider.values)Convert.ChangeType(value.value,AccessTools.Field(type,value.field).FieldType,CultureInfo.InvariantCulture);
  }
  ValidateEffects(state);
 }
 public static void Restore(PlayerController player,State state)
 {
  Validate(state);
  foreach(var pair in Fields)
  {
   var providers=Providers(player,pair.Key);var saved=state.providers.Where(p=>p.type==pair.Key.FullName).ToArray();
   if(saved.Any(p=>p.ordinal>=providers.Length))throw new InvalidOperationException("Native runtime provider was not reconstructed: "+pair.Key.Name);
   foreach(var entry in saved)
    foreach(var value in entry.values)
    {var field=AccessTools.Field(pair.Key,value.field);field.SetValue(providers[entry.ordinal],Convert.ChangeType(value.value,field.FieldType,CultureInfo.InvariantCulture));}
   // An already-consumed Specialize must not become available again on resume.
   if(pair.Key==typeof(TripleNextPowerup))
    for(int i=0;i<providers.Length;i++)if(!saved.Any(s=>s.ordinal==i))Object.Destroy(providers[i]);
  }
  foreach(var saved in state.summons)
  {
   var summons=OwnedAttackingSummons(player).Where(s=>s.SummonTypeID==saved.id).ToArray();
   if(saved.ordinal>=summons.Length)throw new InvalidOperationException("Native summon was not reconstructed: "+saved.id);
   var summon=summons[saved.ordinal];Plugin.SetField(summon,"baseDamage",saved.damage);Restore(summon.attackSpeedMod,saved.speed);
  }
  var eggs=player.GetComponentsInChildren<SummonEgg>(true);
  foreach(var saved in state.eggs)
  {
   if(saved.ordinal>=eggs.Length)throw new InvalidOperationException("Native dragon egg was not reconstructed");
   var egg=eggs[saved.ordinal];egg.StopAllCoroutines();var summon=(Summon)AccessTools.Field(typeof(SummonEgg),"summon").GetValue(egg);
   if(saved.hatched){summon.gameObject.SetActive(true);egg.gameObject.SetActive(false);}
   else
   {
    egg.gameObject.SetActive(true);summon.gameObject.SetActive(false);Plugin.SetField(egg,"secondsToHatch",saved.remaining);EggStarted(egg);
    egg.StartCoroutine((System.Collections.IEnumerator)AccessTools.Method(typeof(SummonEgg),"WaitToHatchCR").Invoke(egg,null));
   }
  }
  RestoreEffects(player,state);
  // RunResume already assigned the snapshot's actual health. Its explicit
  // maximum can differ from the derived stat (for example Justice's20 cap).
  // Restore native stat notifications without assigning/healing health again.
  bool explicitMaximum=AndroidHealthStat.ExplicitMaximum;AndroidHealthStat.ExplicitMaximum=true;
  try{for(int i=0;i<state.stats.Count;i++)Restore(player.stats[(StatType)i],state.stats[i]);}
  finally{AndroidHealthStat.ExplicitMaximum=explicitMaximum;}
  Restore(player.GetComponentInChildren<flanne.Player.PlayerXP>().xpMultiplier,state.xpMultiplier);
  foreach(var buff in player.GetComponentsInChildren<DmgAndMSOnNotHurt>(true))
  {int ticks=(int)AccessTools.Field(typeof(DmgAndMSOnNotHurt),"_ticks").GetValue(buff),maximum=(int)AccessTools.Field(typeof(DmgAndMSOnNotHurt),"maxTicks").GetValue(buff);player.playerSprite.GetComponentInChildren<SpriteTrail>()?.SetEnabled(ticks>=maximum/2);}
 }
}
[HarmonyPatch(typeof(SummonEgg),"Start")]
class ResumeEggClock{static void Postfix(SummonEgg __instance){ResumeRuntime.EggStarted(__instance);}}
