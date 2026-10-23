using System;
using System.Linq;
using System.Collections.Generic;
using flanne;
using flanne.PerkSystem.Triggers;
using flanne.PerkSystem.Actions;
namespace TwentyMinutes.Modernization;

public static partial class ResumeRuntime
{
 // These layouts come from the original Steam 12679152 powerup recipes.
 // Convert only their exact acquisition/slot identities, after extracting
 // the legacy counters needed by the separate runtime-owner translations.
 sealed class LegacyRecipe
 {
  internal readonly string First,Repeat;internal readonly Type Action,Trigger;
  internal LegacyRecipe(string first,string repeat,Type action,Type trigger=null){First=first;Repeat=repeat??first;Action=action;Trigger=trigger??typeof(InstantTrigger);}
 }
 static readonly Dictionary<string,LegacyRecipe> LegacyRecipes=new Dictionary<string,LegacyRecipe>
 {
  ["culling_name"]=new LegacyRecipe("SummonOnHitTrigger>AutoKillAction",null,typeof(AndroidSummonHurtApplication)),
  ["aero_magic_name"]=new LegacyRecipe("PeriodicTrigger>ThrowProjectileTowardsCursorAction;PeriodicTrigger>PlaySoundAction",null,typeof(AndroidGaleAcquire),typeof(InstantTrigger)),
  ["regeneration_name"]=new LegacyRecipe("InstantTrigger>SpawnPrefabAction;PeriodicTrigger>SpawnPrefabAction",null,typeof(AndroidRegenerationAcquire),typeof(InstantTrigger)),
  ["energized_name"]=new LegacyRecipe("OnThunderTrigger>RefillAmmoAction;OnThunderTrigger>SpawnPrefabAction;OnThunderTrigger>PlaySoundAction",null,typeof(AndroidEnergizedAttach)),
  ["hellspawns_name"]=new LegacyRecipe("SummonOnHitTrigger>ApplyBurnAction",null,typeof(AndroidSummonHurtApplication)),
  ["thunderspawns_name"]=new LegacyRecipe("SummonOnHitTrigger>ApplyThunderAction",null,typeof(AndroidSummonHurtApplication)),
  ["abby_perk_whirlwind_name"]=new LegacyRecipe("OnReloadTrigger>ApplyPlayerBuffAction",null,typeof(AndroidRefreshAttachment)),
  ["aero_mastery_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidWindUpgrade)),
  ["aged_dragon"]=new LegacyRecipe("PeriodicTrigger>ModShootingSummonDamageAction",null,typeof(AndroidCombatAttachment)),
  ["angelic_name"]=new LegacyRecipe("OnKillTrigger>HealPlayerAction",null,typeof(AndroidCombatAttachment)),
  ["anger_point_name"]=new LegacyRecipe("OnPlayerHurtTrigger>ApplyPlayerBuffAction",null,typeof(AndroidRefreshAttachment)),
  ["assassin_name"]=new LegacyRecipe("OnHPBelowPercentTrigger>AutoKillAction",null,typeof(AndroidColdAcquire)),
  ["best_friends_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidGhostUpgrade)),
  ["bloodsuckers_name"]=new LegacyRecipe("OnKillTrigger>SpawnPrefabAction",null,typeof(AndroidKillEffectApplication)),
  ["dasher_perk_deer_friend_name"]=new LegacyRecipe("InstantTrigger>AttachPrefabToPlayerAction;InstantTrigger>ApplyPlayerBuffAction","InstantTrigger>AttachPrefabToPlayerAction",typeof(AndroidCharacterUpgrade)),
  ["death_rounds_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,null),
  ["diamond_perk_berserk_name"]=new LegacyRecipe("OnPlayerHurtTrigger>ModStatAction",null,typeof(AndroidCombatAttachment)),
  ["diamond_perk_bulk_up_name"]=new LegacyRecipe("OnPlayerHealTrigger>ModStatAction",null,typeof(AndroidCombatAttachment)),
  ["dragon_bond_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidCombatAttachment)),
  ["electro_mage_name"]=new LegacyRecipe("OnShootTrigger>ThunderNearCursorAction",null,typeof(AndroidCombatAttachment)),
  ["electro_mastery_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction;InstantTrigger>ModThunderAreaAction",null,typeof(AndroidElectroMastery)),
  ["eye_of_the_storm_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction;PeriodicTrigger>AreaTargetAction;PeriodicTrigger>PlaySoundAction;PeriodicTrigger>SpawnPrefabAction",null,typeof(AndroidWindUpgrade)),
  ["feed_the_beasts_name"]=new LegacyRecipe("OnKillTrigger>ModStatAction",null,typeof(AndroidKillEffectApplication)),
  ["focal_point_name"]=new LegacyRecipe("InstantTrigger>ModSummonScaleAction;InstantTrigger>ModMagicLensMultiplierAction","InstantTrigger>ModMagicLensMultiplierAction",typeof(AndroidLensUpgrade)),
  ["fresh_clip_name"]=new LegacyRecipe("OnReloadTrigger>ApplyPlayerBuffAction",null,typeof(AndroidRefreshAttachment)),
  ["frost_mage_name"]=new LegacyRecipe("OnHitTrigger>ApplyFreezeAction",null,typeof(AndroidCombatAttachment)),
  ["pyro_mage_name"]=new LegacyRecipe("OnHitTrigger>ApplyBurnAction",null,typeof(AndroidCombatAttachment)),
  ["frostbite_name"]=new LegacyRecipe("OnFreezeTrigger>PercentDamageAction",null,typeof(AndroidColdAcquire)),
  ["generator_name"]=new LegacyRecipe("InstantTrigger>AttachPrefabToPlayerAction",null,typeof(AndroidCombatAttachment)),
  ["ghost_wizard_name"]=new LegacyRecipe("SummonOnHitTrigger>TriggerOnHitAction;SummonOnHitTrigger>ApplyBurnAction",null,typeof(AndroidGhostWizardAction)),
  ["glare_name"]=new LegacyRecipe("InstantTrigger>AttachPrefabToPlayerAction","InstantTrigger>ApplyPlayerBuffAction",typeof(AndroidGlareAcquire)),
  ["hastur_perk_frenzy_name"]=new LegacyRecipe("InstantTrigger>ModShootingSummonAttackSpeed",null,typeof(AndroidHasturUpgrade)),
  ["hastur_perk_smash_name"]=new LegacyRecipe("InstantTrigger>ModHitboxSummonBaseDamageAction",null,typeof(AndroidHasturUpgrade)),
  ["hina_perk_enhanced_shadow_name"]=new LegacyRecipe("InstantTrigger>ModShootingSummonAttackSpeed",null,typeof(AndroidEnhancedShadow)),
  ["holy_arts_name"]=new LegacyRecipe("OnLastAmmoTrigger>DelayActivateAction;OnLastAmmoTrigger>PlaySoundAction;OnLastAmmoTrigger>DelayActivateAction","OnLastAmmoTrigger>DelayActivateAction;OnLastAmmoTrigger>DelayActivateAction",typeof(AndroidHolyArtsAcquire)),
  ["holy_spear_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidSpearUpgrade)),
  ["igniting_lens_name"]=new LegacyRecipe("InstantTrigger>ModMagicLensBurn",null,typeof(AndroidLensUpgrade)),
  ["in_the_wind_name"]=new LegacyRecipe("InstantTrigger>AttachPrefabToPlayerAction",null,typeof(AndroidCombatAttachment)),
  ["intense_glare_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidGlareUpgrade)),
  ["justice_name"]=new LegacyRecipe("OnKillTrigger>ModStatAction",null,typeof(AndroidJusticeAcquire)),
  ["katana_perk_fire_edge_name"]=new LegacyRecipe("OnHitTrigger>ApplyBurnAction;SummonOnHitTrigger>TriggerOnHitAction",null,typeof(AndroidCharacterUpgrade)),
  ["katana_perk_sword_mastery_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidCharacterUpgrade)),
  ["katana_perk_thousand_blades_name"]=new LegacyRecipe("OnReloadTrigger>AttachPrefabToPlayerAction",null,typeof(AndroidCharacterUpgrade)),
  ["kill_clip_name"]=new LegacyRecipe("InstantTrigger>AttachPrefabToPlayerAction",null,typeof(AndroidCombatAttachment)),
  ["luna_perk_parallel_star_name"]=new LegacyRecipe("InstantTrigger>AttachPrefabToPlayerAction",null,typeof(AndroidCharacterUpgrade)),
  ["pulsing_summons_name"]=new LegacyRecipe("PeriodicTrigger>TargetSummonsAction;PeriodicTrigger>TargetSummonsAction;PeriodicTrigger>PlaySoundAction",null,typeof(AndroidCombatAttachment)),
  ["raven_perk_flock_of_crows_name"]=new LegacyRecipe("InstantTrigger>AttachPrefabToPlayerAction;SummonOnHitTrigger>DealBulletDamageAction",null,typeof(AndroidCharacterUpgrade)),
  ["raven_perk_shadow_flame_name"]=new LegacyRecipe("OnCurseTrigger>ApplyBurnEqualToCurseDamageAction",null,typeof(AndroidCharacterUpgrade)),
  ["reflex_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidReflexAction)),
  ["refraction_name"]=new LegacyRecipe("InstantTrigger>ModMagicLensBounceAction",null,typeof(AndroidLensUpgrade)),
  ["ritual_name"]=new LegacyRecipe("OnKillTrigger>ModStatAction",null,typeof(AndroidCombatAttachment)),
  ["saccade_name"]=new LegacyRecipe("InstantTrigger>ModGlareTickRateAction",null,typeof(AndroidGlareUpgrade)),
  ["scarlett_perk_pyro_affinity_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidCharacterUpgrade)),
  ["scythe_mastery_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidScytheUpgrade)),
  ["shadowblade_name"]=new LegacyRecipe("SummonOnHitTrigger>ApplyCurse;InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidScytheUpgrade)),
  ["shatter_name"]=new LegacyRecipe("OnKillTrigger>ShatterFrozenAction",null,typeof(AndroidColdAcquire)),
  ["siege_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidCombatAttachment)),
  ["sight_magic_name"]=new LegacyRecipe("InstantTrigger>SetGlareOnHitAction",null,typeof(AndroidGlareUpgrade)),
  ["soul_drain_name"]=new LegacyRecipe("OnKillTrigger>SpawnPrefabAction",null,typeof(AndroidKillEffectApplication)),
  ["soul_expand_name"]=new LegacyRecipe("InstantTrigger>SpawnPrefabAction;InstantTrigger>IncreaseSoulHeartMaxAction",null,typeof(AndroidGrantHeart)),
  ["soul_knight_name"]=new LegacyRecipe("OnSHPGainedTrigger>ApplyPlayerBuffAction;InstantTrigger>SpawnPrefabAction",null,typeof(AndroidSpearUpgrade)),
  ["soul_link_name"]=new LegacyRecipe("InstantTrigger>SpawnPrefabAction;OnSHPLostTrigger>AreaTargetAction;OnSHPLostTrigger>PlaySoundAction","InstantTrigger>SpawnPrefabAction;OnSHPLostTrigger>AreaTargetAction",typeof(AndroidCombatAttachment)),
  ["soul_power_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidCombatAttachment)),
  ["splinter_name"]=new LegacyRecipe("OnKillTrigger>RadiallyShootBulletAction",null,typeof(AndroidCombatAttachment)),
  ["tome_of_electro_name"]=new LegacyRecipe("OnLastAmmoTrigger>AreaTargetAction",null,typeof(AndroidCombatAttachment)),
  ["tome_of_elements_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction;InstantTrigger>ApplyPlayerBuffAction;InstantTrigger>ModFreezeDurationAction",null,typeof(AndroidElementStats)),
  ["tome_of_glass_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction;OnPlayerHurtTrigger>KillPlayerAction",null,typeof(AndroidDamageTomeApplication)),
  ["tome_of_might_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidDamageTomeApplication)),
  ["tome_of_wind_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction;InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidDamageTomeApplication)),
  ["trained_dragon_name"]=new LegacyRecipe("PeriodicTrigger>ModShootingSummonAttackSpeed",null,typeof(AndroidCombatAttachment)),
  ["vengeful_ghost_name"]=new LegacyRecipe("InstantTrigger>ModShootingSummonNumProjectilesAction",null,typeof(AndroidGhostUpgrade)),
  ["wind_cutter_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidScytheUpgrade)),
  ["windborne_name"]=new LegacyRecipe("InstantTrigger>ApplyPlayerBuffAction",null,typeof(AndroidWindUpgrade)),
 };
 static bool MatchesLegacyRecipe(EffectState[] saved,string scope,string layout)
 {
  var slots=string.IsNullOrEmpty(layout)?Array.Empty<string>():layout.Split(';');
  if(saved.Length!=slots.Length)return false;
  for(int i=0;i<slots.Length;i++)
  {
   var entry=saved.SingleOrDefault(e=>e.id==scope+"@"+i);var pair=slots[i].Split('>');
   if(entry==null||entry.trigger!="flanne.PerkSystem.Triggers."+pair[0]||entry.action!="flanne.PerkSystem.Actions."+pair[1])return false;
  }
  return true;
 }
 // Earlier source adapters still represented these two owners as periodic
 // effects. Their known phase records are superseded by unsaved source clocks.
 static bool MatchesPreviousTimedRecipe(string key,EffectState[] saved,string scope)
 {
  if(saved.Length!=1||saved[0].id!=scope+"@0"||saved[0].trigger!=typeof(PeriodicTrigger).FullName)return false;
  return key=="aero_magic_name"&&saved[0].action==typeof(AndroidGaleCast).FullName||
   key=="regeneration_name"&&saved[0].action==typeof(SpawnPrefabAction).FullName;
 }
 // The previous source adapter used a per-pick surviving-hurt trigger. Its
 // exact element layouts move to the original-asset observer owner; the old
 // Energized action wrapper moves to its source MonoBehaviour attachment.
 static bool MatchesPreviousSummonHurt(string key,EffectState[] saved,string scope)
 {
  if(saved.Length!=1||saved[0].id!=scope+"@0")return false;
  if(key=="energized_name")return saved[0].trigger=="TwentyMinutes.Modernization.AndroidRandomThunder"&&saved[0].action=="TwentyMinutes.Modernization.AndroidEnergizedActions";
  if(saved[0].trigger!="TwentyMinutes.Modernization.AndroidSummonHurtTrigger")return false;
  return key=="hellspawns_name"&&saved[0].action==typeof(ApplyBurnAction).FullName||
   key=="thunderspawns_name"&&saved[0].action==typeof(ApplyThunderAction).FullName;
 }
 internal static void ConvertLegacyCharacterStats(RunResume.Snapshot snapshot)
 {
  var state=snapshot.runtime;if(state.characterSummonBonuses==true)return;
  // Both original native recipes had no direct stat changes. The supported
  // previous writers also lacked these source bonuses. Preserve their known
  // aggregate remainder while allowing the newly replayed source recipes to
  // survive the legacy aggregate assignment. Do not infer any kill/hurt history.
  var summonDamage=state.stats[(int)StatType.SummonDamage];
  foreach(var acquired in snapshot.acquired.Where(a=>!a.evolution))
  {
   float scale=snapshot.custom?.values.FirstOrDefault(v=>v.key=="perk:"+acquired.key+":scale")?.value??1;
   if(acquired.key=="hina_perk_enhanced_shadow_name")summonDamage.bonus+=.25f*scale;
   else if(acquired.key=="lilith_perk_necromancer_name")summonDamage.bonus+=.2f*scale;
  }
  state.characterSummonBonuses=true;
 }
 internal static void ConvertLegacyRecipes(RunResume.Snapshot snapshot)
 {
  var state=snapshot.runtime;
  foreach(var acquired in snapshot.acquired.Where(a=>!a.evolution).GroupBy(a=>a.key))
  {
   if(!LegacyRecipes.TryGetValue(acquired.Key,out var recipe))continue;
   for(int application=0;application<acquired.Count();application++)
   {
    string scope=acquired.Key+"#"+application;
    var saved=state.effects.Where(e=>e.id.StartsWith(scope+"@",StringComparison.Ordinal)).ToArray();
    // Already translated/current layouts retain their actual saved fields.
    bool current=recipe.Action==null?saved.Length==0:saved.Length==1&&saved[0].id==scope+"@0"&&
     saved[0].trigger==recipe.Trigger.FullName&&saved[0].action==recipe.Action.FullName;
    if(current||!MatchesLegacyRecipe(saved,scope,application==0?recipe.First:recipe.Repeat)&&!MatchesPreviousTimedRecipe(acquired.Key,saved,scope)&&!MatchesPreviousSummonHurt(acquired.Key,saved,scope))continue;
    // These replaced source recipes own their runtime in attached helpers or
    // the original powerup asset, not in the obsolete native effect slots.
    // Unknown layouts remain untouched for the existing strict effect reader.
    state.effects.RemoveAll(e=>e.id.StartsWith(scope+"@",StringComparison.Ordinal));
    if(recipe.Action!=null)state.effects.Add(new EffectState{id=scope+"@0",trigger=recipe.Trigger.FullName,action=recipe.Action.FullName});
   }
  }
 }
}
