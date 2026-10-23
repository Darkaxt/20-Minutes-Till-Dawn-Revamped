using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;
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
 [Serializable,DataContract]public class SummonState{[DataMember(IsRequired=true)]public string id;[DataMember(IsRequired=true)]public int ordinal,damage;[DataMember(IsRequired=true)]public ModState speed;[DataMember]public float? cooldown;}
 [Serializable]public class EggState{public int ordinal;public float remaining;public bool hatched;}
 [Serializable]public class GeneratorState{public int ordinal,stacks,contributions;}
 [Serializable]public class GrowthState{public string type;public int ordinal,count;public float remaining;}
 [Serializable,DataContract]public class State
 {
  [DataMember(IsRequired=true)]public List<ModState> stats=new List<ModState>();[DataMember(IsRequired=true)]public ModState xpMultiplier;
  [DataMember(IsRequired=true)]public List<ProviderState> providers=new List<ProviderState>();
  [DataMember(IsRequired=true)]public List<SummonState> summons=new List<SummonState>();[DataMember(IsRequired=true)]public List<EggState> eggs=new List<EggState>();
  [DataMember(IsRequired=true)]public List<TimedEffect> timed=new List<TimedEffect>();[DataMember(IsRequired=true)]public List<TemporaryEffect> temporary=new List<TemporaryEffect>();
  [DataMember(IsRequired=true)]public List<string> liveOneUseRunes=new List<string>();[DataMember(IsRequired=true)]public List<bool> singleShotActions=new List<bool>();
  [DataMember(IsRequired=true)]public List<ShieldState> shields=new List<ShieldState>();
  [DataMember(IsRequired=true)]public List<EffectState> effects=new List<EffectState>();
  [DataMember]public int? protectionFlips;
  [DataMember]public bool? summonDurationDamage;[DataMember]public string summonDurationData;
  // Null identifies a legacy checkpoint awaiting recipe/provider conversion.
  [DataMember]public List<GeneratorState> generators;
  [DataMember]public List<GrowthState> growth;
  [DataMember]public List<RitualState> rituals;[DataMember]public List<UnharmedState> unharmed;[DataMember]public List<JusticeState> justice;
  [DataMember]public float? feedTheBeastsDamage;
  [DataMember]public List<SourcePowerup> sourcePowerups;[DataMember]public int? sourceHealing,sourceBodySoul;
  [DataMember]public List<int> customRageAmmo,customFusilladeProjectiles;
  // Earlier aggregate writers predate these two source stat recipes. New
  // legacy saves already include them and must not migrate them a second time.
  [DataMember]public bool? characterSummonBonuses;
 }
 static readonly Dictionary<Type,string[]> Fields=new Dictionary<Type,string[]>
 {
  {typeof(AndroidSpearBonuses),new[]{"HpBonus","SoulBonus"}},
  {typeof(StabbingSummon),new[]{"_timer"}},
  {typeof(ShootingSummon),new[]{"_timer"}},
  {typeof(AndroidBestFriends),new[]{"Factor"}},
  {typeof(AndroidShootingFlags),new[]{"BulletEffect","BurnDamage"}},
  {typeof(AndroidSoulPowered),new[]{"Granted"}},
  {typeof(AndroidReloadSwords),new[]{"Amount","Counter"}},
  {typeof(AndroidSummonPulse),new[]{"Timer","Damage","Interval","Range"}},
  {typeof(ThunderOnAttack),new[]{"_counter"}},
  {typeof(BulletEnhanceSummon),new[]{"overallMultiplier","damageMultiplier","sizeMultiplier","additionalBounces","addBurn"}},
  {typeof(AndroidReflex),new[]{"LastMove","Granted"}},
  {typeof(AndroidGlare),new[]{"Timer"}},
  {typeof(ThunderDuringHolyShield),new[]{"_timer"}},
  {typeof(AndroidBodySoul),new[]{"Grants"}},
  {typeof(EtherealRune),new[]{"_counter"}},
  {typeof(DropHPOnKillRune),new[]{"_counter"}},
  {typeof(BuffSummonDamageOverTime),new[]{"_timer"}},
  {typeof(BuffSummonAttackSpeedOverTime),new[]{"_timer"}},
  {typeof(DmgAndMSOnNotHurt),new[]{"_ticks","_timer"}},
  {typeof(DamageBuffOnCurseKill),new[]{"_killCounter"}},
  {typeof(GainMaxHPOnSmiteKill),new[]{"_killCounter","_mhpGainCounter"}},
  {typeof(HealOnSmiteKill),new[]{"_killCounter"}},
  {typeof(TripleNextPowerup),new[]{"used"}},
  {typeof(ShootBulletsBehind),new[]{"numOfBullets","spread"}},
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
 public static void ForgetRuntime(){ForgetEffects();}
 public static ModState Capture(StatMod stat)=>new ModState{flat=stat.FlatBonus,
  bonus=(float)AccessTools.Field(typeof(StatMod),"_multiplierBonus").GetValue(stat),
  reduction=(float)AccessTools.Field(typeof(StatMod),"_multiplierReduction").GetValue(stat)};
 static MonoBehaviour[] Providers(PlayerController player,Type type)=>
  player.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null&&c.GetType()==type).ToArray();
 public static State Capture(PlayerController player)=>CaptureSource(player);
 public static void Validate(State state)
 {
  if(state?.sourcePowerups!=null){ValidateSource(state);return;}
  if(state==null||state.stats==null||state.stats.Count!=(int)StatType.Count||state.xpMultiplier==null||state.providers==null||state.summons==null||state.eggs==null||state.timed==null||state.temporary==null||state.liveOneUseRunes==null||state.singleShotActions==null||state.shields==null)throw new InvalidOperationException("Run checkpoint has an unsupported native runtime contract");
  foreach(var provider in state.providers)
  {
   var type=Fields.Keys.SingleOrDefault(t=>t.FullName==provider.type);
   bool legacySoul=type==typeof(AndroidBodySoul)&&provider.values.Select(v=>v.field).OrderBy(s=>s).SequenceEqual(new[]{"Grants","Remaining"});
    if(type==null||provider.ordinal<0||!legacySoul&&!provider.values.Select(v=>v.field).OrderBy(s=>s).SequenceEqual(Fields[type].OrderBy(s=>s)))throw new InvalidOperationException("Run checkpoint has an unsupported runtime provider");
   foreach(var value in provider.values)Convert.ChangeType(value.value,legacySoul&&value.field=="Remaining"?typeof(float):AccessTools.Field(type,value.field).FieldType,CultureInfo.InvariantCulture);
  }
  if(state.generators!=null&&(state.generators.Any(g=>g==null||g.ordinal<0)||state.generators.Select(g=>g.ordinal).Distinct().Count()!=state.generators.Count))throw new InvalidOperationException("Run checkpoint has an unsupported Generator identity");
  if(state.growth!=null&&(state.growth.Any(g=>g==null||g.ordinal<0||!AndroidSummonGrowth.Types.Any(t=>t.FullName==g.type)||g.remaining<0||float.IsNaN(g.remaining)||float.IsInfinity(g.remaining))||state.growth.Select(g=>g.type+"#"+g.ordinal).Distinct().Count()!=state.growth.Count))throw new InvalidOperationException("Run checkpoint has an unsupported summon-growth identity");
  ValidateAccumulated(state);ValidateEffects(state);
  if(state.feedTheBeastsDamage.HasValue&&(float.IsNaN(state.feedTheBeastsDamage.Value)||float.IsInfinity(state.feedTheBeastsDamage.Value)))throw new InvalidOperationException("Unsupported Feed the Beasts runtime value");
 }
 internal static void ConvertLegacyGenerators(State state)
 {
  if(state.generators!=null)return;
  // Released v3/v4 used our native gain-only Generator repair. Its provider
  // identity and active BuffCR distinguish one actual +35% application from
  // an inactive helper. Those saves never recorded Android's ammo-event count;
  // migrate the known application, without fabricating historical events.
  string legacy=typeof(FireRateBuffOnGainAmmo).FullName;
  state.generators=state.providers.Where(p=>p.type==legacy).Select(p=>
  {
   bool active=state.timed.Any(t=>t.type==legacy&&t.ordinal==p.ordinal&&t.method=="BuffCR");
   return new GeneratorState{ordinal=p.ordinal,stacks=active?1:0,contributions=active?1:0};
  }).ToList();
  state.providers.RemoveAll(p=>p.type==legacy);
  state.timed.RemoveAll(t=>t.type==legacy);
  foreach(var effect in state.effects)
   if(effect.id.StartsWith("generator_name#")&&effect.action=="flanne.PerkSystem.Actions.AttachPrefabToPlayerAction")
    effect.action=typeof(AndroidCombatAttachment).FullName;
 }
 public static void Restore(PlayerController player,State state)
 {
  Validate(state);
  RestoreSourceRunes(player,state);
 }
 internal static void StartRestoredAttachments(PlayerController player)
 {
  foreach(var generator in player.GetComponentsInChildren<AndroidGenerator>(true))generator.enabled=true;
  foreach(var type in AndroidSummonGrowth.Types)foreach(var growth in Providers(player,type))growth.enabled=true;
  foreach(var type in new[]{typeof(DamageBuffOnCurseKill),typeof(DmgAndMSOnNotHurt),typeof(GainMaxHPOnSmiteKill)})foreach(var helper in Providers(player,type))helper.enabled=true;
 }
}
