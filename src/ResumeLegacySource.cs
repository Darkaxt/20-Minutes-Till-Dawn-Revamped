using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using flanne;
using flanne.RuneSystem;
namespace TwentyMinutes.Modernization;

public static partial class ResumeRuntime
{
 // User-selected migration: rebuild Android state, using the source reader's
 // ordered acquisition/runtime families. Unattributed old aggregate stats and
 // source-unsaved phases do not overwrite the newly reconstructed owners.
 internal static void RebuildLegacySource(RunResume.Snapshot snapshot)
 {
  var old=snapshot.runtime;
  if(old.sourcePowerups!=null)return;
  // The original native baseline was three, not source zero, and Soul Reap
  // only spawned pickups. Preserve the saved contributions of other capacity
  // producers while rebuilding this known baseline/acquisition difference.
  snapshot.maxSoulHP+=3*(snapshot.acquired.Count(pick=>!pick.evolution&&pick.key=="soul_shield_name")-1);
  // Extract component order before old recipe conversions rename Generator's
  // native action. Its original helper did not contain BuffPlayerStats.
  var buffOwners=LegacyBuffOwners(snapshot);
  if(snapshot.version<5)
  {ConvertLegacyGenerators(old);ConvertLegacyDragonGrowth(old);ConvertLegacyAccumulated(old);Validate(old);}
  var rebuilt=new State{sourcePowerups=new List<SourcePowerup>(),
   customRageAmmo=old.customRageAmmo,customFusilladeProjectiles=old.customFusilladeProjectiles,
   sourceHealing=LegacyCounter(old,typeof(DropHPOnKillRune),"_counter"),
   sourceBodySoul=LegacyCounter(old,typeof(AndroidBodySoul),"Grants")};
  var keys=snapshot.acquired.Where(a=>!a.evolution).Select(a=>a.key).Distinct().ToArray();
  foreach(string key in keys)
  {
   var power=new SourcePowerup{key=key};
   if(key=="dragon_egg_name")
    foreach(var value in old.eggs.OrderBy(v=>v.ordinal))
     power.values.Add(new SourceValue{kind="egg",a=value.hatched?-1:value.remaining});
   else if(key=="aged_dragon"||key=="trained_dragon_name")
    foreach(var value in old.growth.Where(v=>v.type==SourceOwners[key].FullName).OrderBy(v=>v.ordinal))
     power.values.Add(new SourceValue{kind="growth",count=value.count,a=value.remaining});
   else if(key=="ritual_name")
    foreach(var value in old.rituals.OrderBy(v=>v.ordinal))
     power.values.Add(new SourceValue{kind="ritual",a=value.damage});
   else if(key=="in_the_wind_name")
    foreach(var value in old.unharmed.OrderBy(v=>v.ordinal))
     power.values.Add(new SourceValue{kind="wind",count=value.ticks,a=value.timer,b=value.damage,c=value.movement});
   else if(key=="justice_name")
    foreach(var value in old.justice.OrderBy(v=>v.ordinal))
     power.values.Add(new SourceValue{kind="justice",count=value.gainedHealth});
   else if(key=="shana_perk_specialize_name")
   {
    int count=snapshot.acquired.Count(a=>!a.evolution&&a.key==key);
    for(int i=0;i<count;i++)
    {
     var owner=old.providers.SingleOrDefault(v=>v.type==typeof(TripleNextPowerup).FullName&&v.ordinal==i);
     bool used=owner==null||bool.Parse(owner.values.Single(v=>v.field=="used").value);
     power.values.Add(new SourceValue{kind="specialize",count=used?0:1});
    }
   }
   else if(key=="generator_name")
    foreach(var value in old.generators.OrderBy(v=>v.ordinal).Where(v=>v.stacks>0))
     power.values.Add(new SourceValue{kind="buff",count=value.stacks});
   else if(BuffRoots.ContainsKey(key))
    foreach(var owner in old.providers.Where(v=>v.type==typeof(BuffPlayerStats).FullName&&v.ordinal<buffOwners.Length&&buffOwners[v.ordinal]==key).OrderBy(v=>v.ordinal))
    {
     int count=int.Parse(owner.values.Single(v=>v.field=="_stacks").value,CultureInfo.InvariantCulture);
     if(count>0)power.values.Add(new SourceValue{kind="buff",count=count});
    }
   else if(key=="feed_the_beasts_name"&&old.feedTheBeastsDamage.HasValue)
    power.values.Add(new SourceValue{kind="feed",a=old.feedTheBeastsDamage.Value});
   if(power.values.Count>0)rebuilt.sourcePowerups.Add(power);
  }
  // Keep the original envelope version for the separate encounter-schedule
  // translations. The normal source capture writes version 7 on the next save.
  snapshot.runtime=rebuilt;
 }
 static string[] LegacyBuffOwners(RunResume.Snapshot snapshot)
 {
  var owners=new List<string>();var applications=new Dictionary<string,int>();
  foreach(var pick in snapshot.acquired.Where(a=>!a.evolution))
  {
   applications.TryGetValue(pick.key,out int application);applications[pick.key]=application+1;
   if(!BuffRoots.ContainsKey(pick.key)||SourceOwners[pick.key]!=typeof(BuffPlayerStats))continue;
   string scope=pick.key+"#"+application+"@";
   var effects=snapshot.runtime.effects.Where(e=>e.id.StartsWith(scope,StringComparison.Ordinal));
   // Original Windows has only the BuffOnBurn BuffPlayerStats prefab.
   // Source adapters add one new root for each of the other three actions.
   // Recorded action identities distinguish those roots from native
   // Berserk/Bulk Up's direct stat mutations and Generator's old helper.
   // Legacy providers used exact types and did not record inherited stacks
   // for NonStackingBuff/TemporaryRefreshingBuff. Do not invent those counts
   // from their unsaved active flags, partial timers or aggregate stats.
   foreach(var effect in effects)
    if(pick.key=="scarlett_perk_pyromaniac_name"&&effect.action=="flanne.PerkSystem.Actions.AttachPrefabToPlayerAction"||
     pick.key!="scarlett_perk_pyromaniac_name"&&effect.action==typeof(AndroidCombatAttachment).FullName)
     owners.Add(pick.key);
  }
  return owners.ToArray();
 }
 static int? LegacyCounter(State state,Type type,string field)
 {
  var owner=state.providers.SingleOrDefault(v=>v.type==type.FullName&&v.ordinal==0);
  return owner==null?(int?)null:int.Parse(owner.values.Single(v=>v.field==field).value,CultureInfo.InvariantCulture);
 }
}
