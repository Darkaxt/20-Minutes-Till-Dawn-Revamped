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
 [Serializable]public class SummonState{public string id;public int ordinal,damage;public ModState speed;public float? cooldown;}
 [Serializable]public class EggState{public int ordinal;public float remaining;public bool hatched;}
 [Serializable]public class GeneratorState{public int ordinal,stacks,contributions;}
 [Serializable]public class GrowthState{public string type;public int ordinal,count;public float remaining;}
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
  public bool? summonDurationDamage;public string summonDurationData;
  // Null identifies a legacy checkpoint awaiting recipe/provider conversion.
  public List<GeneratorState> generators;
  public List<GrowthState> growth;
  public List<RitualState> rituals;public List<UnharmedState> unharmed;public List<JusticeState> justice;
  public float? feedTheBeastsDamage;
  public List<SourcePowerup> sourcePowerups;public int? sourceHealing,sourceBodySoul;
  public List<int> customRageAmmo,customFusilladeProjectiles;
  // Earlier aggregate writers predate these two source stat recipes. New
  // legacy saves already include them and must not migrate them a second time.
  public bool? characterSummonBonuses;
  [NonSerialized]internal int LegacyShieldBaseReductions;
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
 // These values are rebuilt by source actors/perk applications. They are not
 // among AttachPrefabPowerup's runtime save families. Keep their old schema
 // identities readable, without importing old defaults, upgrades or phases.
 static readonly HashSet<Type> UnsavedSummonProviders=new HashSet<Type>
 {
  typeof(AndroidSpearBonuses),typeof(StabbingSummon),typeof(ShootingSummon),
  typeof(AndroidBestFriends),typeof(AndroidShootingFlags),typeof(BulletEnhanceSummon),
  typeof(AndroidSummonPulse),typeof(AndroidReloadSwords),typeof(ThunderOnAttack),
  // Soul Powered now belongs to its original powerup asset. Old typed Granted
  // records remain readable; acquisition/health notifications reconstruct the
  // source-unsaved value. Legacy raw stats retain their known aggregate, with
  // general stat attribution/order still required under G3.
  typeof(AndroidSoulPowered)
 };
 public static void ForgetRuntime(){ForgetEffects();}
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
 static MonoBehaviour[] Providers(PlayerController player,Type type)
 {
  if(type==typeof(StabbingSummon))return OwnedAttackingSummons(player).OfType<StabbingSummon>().Cast<MonoBehaviour>().ToArray();
  if(type==typeof(AndroidSummonPulse))return player.GetComponentsInChildren<AndroidSummonPulse>(true).Concat(OwnedAttackingSummons(player).Select(s=>s.GetComponent<AndroidSummonPulse>()).Where(s=>s!=null)).Distinct().Cast<MonoBehaviour>().ToArray();
  return player.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c!=null&&c.GetType()==type).ToArray();
 }
 public static State Capture(PlayerController player)
 {
  if(!RunRecorder.Instance.LegacyContinuation)return CaptureSource(player);
  var state=new State{generators=new List<GeneratorState>(),growth=new List<GrowthState>(),characterSummonBonuses=true,
   customRageAmmo=CustomMode.Active?RunRecorder.Instance.CustomRageAmmo?.ToList():null,
   customFusilladeProjectiles=CustomMode.Active?RunRecorder.Instance.CustomFusilladeProjectiles?.ToList():null};
  for(int i=0;i<(int)StatType.Count;i++)state.stats.Add(Capture(player.stats[(StatType)i]));
  state.xpMultiplier=Capture(player.GetComponentInChildren<flanne.Player.PlayerXP>().xpMultiplier);
  foreach(var pair in Fields)
  {
   if(UnsavedSummonProviders.Contains(pair.Key))continue;
   var providers=Providers(player,pair.Key);
   for(int i=0;i<providers.Length;i++)
   {
    // Generator's source save contract contains the paired buff count, not
    // Applied/AppliedAt. Its raw-stat contribution is translated below.
    if(AndroidSummonGrowth.IsGrowth(pair.Key)||AccumulatedProvider(pair.Key)||pair.Key==typeof(BuffPlayerStats)&&providers[i].GetComponent<AndroidGenerator>()!=null)continue;
    var entry=new ProviderState{type=pair.Key.FullName,ordinal=i};
    foreach(string field in pair.Value)entry.values.Add(new FieldValue{field=field,value=Convert.ToString(AccessTools.Field(pair.Key,field).GetValue(providers[i]),CultureInfo.InvariantCulture)});
    state.providers.Add(entry);
   }
  }
  var generators=player.GetComponentsInChildren<AndroidGenerator>(true);
  for(int i=0;i<generators.Length;i++)state.generators.Add(new GeneratorState{ordinal=i,stacks=AndroidPlayerBuffCounters.Count(generators[i].GetComponent<BuffPlayerStats>()),contributions=generators[i].Contributions});
  foreach(var type in AndroidSummonGrowth.Types)
  {var owners=Providers(player,type);for(int i=0;i<owners.Length;i++)state.growth.Add(AndroidSummonGrowth.Capture(owners[i],i));}
  var eggs=player.GetComponentsInChildren<SummonEgg>(true);
  for(int i=0;i<eggs.Length;i++)
  {
   float remaining=AndroidDragonEgg.Get(eggs[i]).GetRemainingHatchSeconds();bool hatched=remaining<0;
   state.eggs.Add(new EggState{ordinal=i,hatched=hatched,remaining=hatched?0:remaining});
  }
  CaptureAccumulated(player,state);CaptureEffects(player,state);return state;
 }
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
 public static void Restore(PlayerController player,State state,RunResume.Snapshot snapshot)
 {
  Validate(state);
  if(state.sourcePowerups!=null){RestoreSourceRunes(player,state);return;}
  foreach(var pair in Fields)
  {
   if(UnsavedSummonProviders.Contains(pair.Key))continue;
   var providers=Providers(player,pair.Key);var saved=state.providers.Where(p=>p.type==pair.Key.FullName).ToArray();
   if(saved.Any(p=>p.ordinal>=providers.Length))throw new InvalidOperationException("Native runtime provider was not reconstructed: "+pair.Key.Name);
   foreach(var entry in saved)
    foreach(var value in entry.values)
    {
     var field=AccessTools.Field(pair.Key,value.field);object restored=Convert.ChangeType(value.value,field.FieldType,CultureInfo.InvariantCulture);
     if(pair.Key==typeof(DropHPOnKillRune)&&value.field=="_counter"||pair.Key==typeof(AndroidBodySoul)&&value.field=="Grants")restored=Mathf.Max(0,(int)restored);
     field.SetValue(providers[entry.ordinal],restored);
    }
   // Windows destroyed spent helpers; source retains them as used. The old
   // provider list contains only the surviving roots (plus any pending
   // destruction captured that frame), in its own compact ordinal order.
   if(pair.Key==typeof(TripleNextPowerup))
    for(int i=0;i<providers.Length;i++)
    {
     if(!saved.Any(s=>s.ordinal==i))AndroidSpecialize.Used.SetValue(providers[i],true);
     AndroidSpecialize.Restored((TripleNextPowerup)providers[i]);
    }
  }
  if(state.growth!=null)foreach(var saved in state.growth)
  {
   var type=AndroidSummonGrowth.Types.Single(t=>t.FullName==saved.type);var owners=Providers(player,type);
   if(saved.ordinal>=owners.Length)throw new InvalidOperationException("Source summon growth was not reconstructed");
  }
  foreach(var saved in state.summons)
  {
   var summons=OwnedAttackingSummons(player).Where(s=>s.SummonTypeID==saved.id).ToArray();
    if(saved.ordinal>=summons.Length)throw new InvalidOperationException("Native summon was not reconstructed: "+saved.id);
   // Validate the old recorded owner, but retain the rebuilt source actor.
   // Generic damage/local-speed/cooldown values are not source save data.
   // Known growth counts below replay against its actual source baseline.
  }
  if(state.growth!=null)foreach(var saved in state.growth)
  {
   var type=AndroidSummonGrowth.Types.Single(t=>t.FullName==saved.type);
   AndroidSummonGrowth.Restore(Providers(player,type)[saved.ordinal],saved.remaining,saved.count);
  }
  var eggs=player.GetComponentsInChildren<SummonEgg>(true);
  foreach(var saved in state.eggs)
  {
   if(saved.ordinal>=eggs.Length)throw new InvalidOperationException("Native dragon egg was not reconstructed");
   AndroidDragonEgg.Get(eggs[saved.ordinal]).RestoreRemainingHatchSeconds(saved.hatched?-1:saved.remaining);
  }
  RestoreEffects(player,state);
  RestoreAccumulatedData(player,state);
  // Preserve the source asset's saved accumulator. The aggregate stat path
  // below still supplies its numerical contribution; per-owner additive replay
  // and its observer order remain part of the existing P2-G3 blocker.
  if(state.feedTheBeastsDamage.HasValue)
   AndroidKillEffectRecipes.Existing("feed_the_beasts_name").Accumulated=state.feedTheBeastsDamage.Value;
  // RunResume already assigned the snapshot's actual health. Its explicit
  // maximum can differ from the derived stat (for example Justice's20 cap).
  // Restore native stat notifications without assigning/healing health again.
  // These six channels have only static acquisition contributors. Replay
  // already applies their source values and current Custom perk scales,
  // including recorded extra picks. Keep all other saved channels: in
  // particular, earned fire-rate bonuses and Tiny's separate size reduction.
  // No writer-version guess, aggregate division or provenance flag is needed.
  var rebuiltStatic=new Dictionary<StatType,ModState>{
   {StatType.CharacterSize,Capture(player.stats[StatType.CharacterSize])},
   {StatType.ProjectileSpeed,Capture(player.stats[StatType.ProjectileSpeed])},
   {StatType.FireRate,Capture(player.stats[StatType.FireRate])},
   {StatType.ReloadRate,Capture(player.stats[StatType.ReloadRate])},
   {StatType.BulletDamage,Capture(player.stats[StatType.BulletDamage])},
   {StatType.Knockback,Capture(player.stats[StatType.Knockback])}};
  // Ordinary weapon/evolution inputs are recorded. In Custom, all flat-ammo
  // recipes are likewise reconstructable except Rage's application-time
  // weapon capacity, which older snapshots did not record independently.
  if(snapshot.custom==null||state.customRageAmmo!=null||!snapshot.acquired.Any(a=>!a.evolution&&a.key=="tome_of_rage"))
   rebuiltStatic.Add(StatType.MaxAmmo,Capture(player.stats[StatType.MaxAmmo]));
  bool explicitMaximum=AndroidHealthStat.ExplicitMaximum;AndroidHealthStat.ExplicitMaximum=true;
  try{for(int i=0;i<state.stats.Count;i++)
  {
   var saved=state.stats[i];
   if(rebuiltStatic.TryGetValue((StatType)i,out var rebuilt))
   {
    bool additive=i==(int)StatType.CharacterSize||i==(int)StatType.ProjectileSpeed;
    bool flat=i==(int)StatType.MaxAmmo;
    saved=new ModState{flat=flat?rebuilt.flat:saved.flat,bonus=additive?rebuilt.bonus:saved.bonus,
     reduction=additive||flat?saved.reduction:rebuilt.reduction};
   }
   if(i==(int)StatType.FireRate&&state.generators!=null)
   {
    // Raw saved stats include the actual live contribution, which can differ
    // from Android's saved application count. Remove that contribution from
    // the raw baseline before invoking the source count-replay consumer.
    saved=new ModState{flat=saved.flat,bonus=saved.bonus,reduction=saved.reduction};
    foreach(var generator in state.generators)
    {
     for(int n=0;n<generator.contributions;n++)saved.bonus-=.35f;
     for(int n=0;n>generator.contributions;n--)saved.bonus+=.35f;
    }
   }
   Restore(player.stats[(StatType)i],saved);
  }}
  finally{AndroidHealthStat.ExplicitMaximum=explicitMaximum;}
  if(state.generators!=null)
  {
   var generators=player.GetComponentsInChildren<AndroidGenerator>(true);
   int target=0;
   foreach(var saved in state.generators.OrderBy(g=>g.ordinal))
   {
    if(saved.ordinal>=generators.Length)throw new InvalidOperationException("Source Generator was not reconstructed");
    // AttachPrefabPowerup emits only positive counts without an original-root
    // index. Its reader consumes successive matching helpers, so zero-count
    // holes in the old roots are not retained as target identities.
    if(saved.stacks>0)AndroidPlayerBuffCounters.Restore(generators[target++].GetComponent<BuffPlayerStats>(),saved.stacks);
   }
  }
  Restore(player.GetComponentInChildren<flanne.Player.PlayerXP>().xpMultiplier,state.xpMultiplier);
 }
 internal static void StartRestoredAttachments(PlayerController player)
 {
  foreach(var generator in player.GetComponentsInChildren<AndroidGenerator>(true))generator.enabled=true;
  foreach(var type in AndroidSummonGrowth.Types)foreach(var growth in Providers(player,type))growth.enabled=true;
  foreach(var type in new[]{typeof(DamageBuffOnCurseKill),typeof(DmgAndMSOnNotHurt),typeof(GainMaxHPOnSmiteKill)})foreach(var helper in Providers(player,type))helper.enabled=true;
 }
}
