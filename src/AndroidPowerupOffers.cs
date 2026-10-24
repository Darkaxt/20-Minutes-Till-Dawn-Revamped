using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PowerupSystem;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(PowerupGenerator),"Awake")]
class AndroidPowerupSingleton
{
 static bool Prefix(PowerupGenerator __instance)
 {
  if(PowerupGenerator.Instance!=__instance)UnityEngine.Object.Destroy(PowerupGenerator.Instance);
  PowerupGenerator.Instance=__instance;PowerupGenerator.CanReroll=false;return false;
 }
}

// Android retains the repeat dictionary after an entry leaves an offer list.
// Native pool items mirror these counts for the existing Custom/Continue UI.
public sealed class AndroidPowerupCounts:MonoBehaviour
{
 internal readonly Dictionary<Powerup,int> Counts=new Dictionary<Powerup,int>();
 internal static readonly string[] PoolFields={"powerupPool","devilPool","characterPool"};
 internal static List<PowerupPoolItem> Pool(PowerupGenerator owner,string field)=>(List<PowerupPoolItem>)AccessTools.Field(typeof(PowerupGenerator),field).GetValue(owner);
 internal void Initialize(PowerupGenerator owner)
 {
  var ordinary=Pool(owner,"powerupPool");AndroidOfferPrerequisites.Configure(ordinary);
  Counts.Clear();Register(ordinary);Register(Pool(owner,"devilPool"));
 }
 internal void Register(List<PowerupPoolItem> pool)
 {foreach(var item in pool)Counts.Add(item.powerup,item.numTimeRepeatable);}
 internal void Remove(PowerupGenerator owner,string field,Powerup powerup)
 {
  bool registered=Counts.TryGetValue(powerup,out int remaining);
  if(registered)Counts[powerup]=--remaining;
  if(!registered||remaining==0)
  {var pool=Pool(owner,field);var item=pool.Find(p=>p.powerup==powerup);if(item!=null)pool.Remove(item);}
  if(registered)
   foreach(string name in PoolFields)
    foreach(var item in Pool(owner,name)??new List<PowerupPoolItem>())if(item.powerup==powerup)item.numTimeRepeatable=remaining;
  Record(owner,powerup);
 }
 internal static void Record(PowerupGenerator owner,Powerup powerup)
 {
  if(powerup==null)return;
  var taken=(List<Powerup>)AccessTools.Field(typeof(PowerupGenerator),"takenPowerups").GetValue(owner);
  if(taken==null){taken=new List<Powerup>();Plugin.SetField(owner,"takenPowerups",taken);}
  if(!taken.Contains(powerup))taken.Add(powerup);
 }
 internal List<RunResume.PoolEntry> Capture()=>Counts.Select(p=>new RunResume.PoolEntry{key=RunResume.Key(p.Key),remaining=p.Value}).ToList();
 internal void Restore(PowerupGenerator owner,List<RunResume.PoolEntry> saved,Dictionary<string,Powerup> powers)
 {
  if(saved!=null){Counts.Clear();foreach(var item in saved)Counts.Add(powers[item.key],item.remaining);return;}
  // Version 3 recorded only remaining lists, not exhausted dictionary entries.
  // Preserve that recorded availability; full legacy recipe migration is separate.
  foreach(var powerup in Counts.Keys.ToArray())Counts[powerup]=0;
  foreach(string field in PoolFields)
   foreach(var item in Pool(owner,field)??new List<PowerupPoolItem>())
    if(Counts.ContainsKey(item.powerup))Counts[item.powerup]=item.numTimeRepeatable;
 }
}
[HarmonyPatch(typeof(PowerupGenerator),nameof(PowerupGenerator.SetCharacterPowerupPool))]
class AndroidCharacterOfferCounts
{
 static void Postfix(PowerupGenerator __instance)
 {
  var pool=AndroidPowerupCounts.Pool(__instance,"characterPool");
  // Source uses the selected map's count, independent of isRepeatable.
  foreach(var item in pool)item.numTimeRepeatable=SelectedMap.MapData.numPowerupsRepeat;
  __instance.GetComponent<AndroidPowerupCounts>().Register(pool);
  Plugin.SetField(__instance,"takenPowerups",new List<Powerup>());
 }
}
[HarmonyPatch]
class AndroidConsumeOffer
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{"RemoveFromPool","RemoveFromCharacterPool","RemoveFromDevilPool"}.Select(n=>AccessTools.Method(typeof(PowerupGenerator),n));
 static bool Prefix(PowerupGenerator __instance,Powerup powerup,MethodBase __originalMethod)
 {
  string field=__originalMethod.Name=="RemoveFromPool"?"powerupPool":__originalMethod.Name=="RemoveFromCharacterPool"?"characterPool":"devilPool";
  __instance.GetComponent<AndroidPowerupCounts>().Remove(__instance,field,powerup);return false;
 }
}
[HarmonyPatch(typeof(PowerupGenerator),nameof(PowerupGenerator.AddToPool))]
class AndroidAppendOffers
{
 static bool Prefix(PowerupGenerator __instance,List<Powerup> powerups)
 {
  var counts=__instance.GetComponent<AndroidPowerupCounts>().Counts;var pool=FallbackPowerups.Pool(__instance);
  foreach(var powerup in powerups)pool.Add(new PowerupPoolItem{powerup=powerup,numTimeRepeatable=counts.TryGetValue(powerup,out int value)?value:1});
  return false;
 }
}
[HarmonyPatch]
class AndroidAscensionPoolOrder
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  if(RunResume.Key(__instance)!="shana_perk_ascension_name")return;
  var effects=(flanne.PerkSystem.PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(__instance);
  var action=effects.Select(e=>AccessTools.Field(typeof(flanne.PerkSystem.PerkEffect),"action").GetValue(e)).OfType<flanne.PerkSystem.Actions.AddPerksToPoolAction>().Single();
  var pieces=(List<Powerup>)AccessTools.Field(typeof(flanne.PerkSystem.Actions.AddPerksToPoolAction),"powerupsToAdd").GetValue(action);
  // Source appends HaloPiece1/2/3. Native's reversed asset list changes the
  // candidate at a given random index, even though all three remain eligible.
  Plugin.SetField(action,"powerupsToAdd",new[]{"shana_halo_piece_1","shana_halo_piece_2","shana_halo_piece_3"}.Select(key=>pieces.Single(piece=>RunResume.Key(piece)==key)).ToList());
 }
}
[HarmonyPatch(typeof(PowerupGenerator),"PrereqsMet")]
class AndroidOfferPrerequisites
{
 internal static void Configure(List<PowerupPoolItem> pool)
 {
  Powerup Find(string key)=>pool.Single(p=>RunResume.Key(p.powerup)==key).powerup;
  Find("giant_name").anyPrereqFulfill=false;
  var moving=Find("run_and_gun_name");moving.anyPrereqFulfill=false;
  moving.prereqs=new List<Powerup>{Find("haste_name")};
  var frenzy=Find("frenzy_name");frenzy.anyPrereqFulfill=true;
  frenzy.prereqs=new List<Powerup>{Find("magic_scythe_name"),Find("holy_spear_name"),Find("ghost_friend_name"),Find("dragon_egg_name")};
 }
 static bool Prefix(Powerup powerup,List<Powerup> ___takenPowerups,ref bool __result)
 {
  __result=!(powerup.name=="Assassin"&&AndroidSpecialize.Count>0)&&
   (powerup.prereqs.Count==0||(powerup.anyPrereqFulfill?powerup.prereqs.Any(___takenPowerups.Contains):powerup.prereqs.All(___takenPowerups.Contains)));
  return false;
 }
}
[HarmonyPatch(typeof(PowerupGenerator),nameof(PowerupGenerator.GetRandom),typeof(int),typeof(List<PowerupPoolItem>))]
class AndroidOfferSelection
{
 static readonly MethodInfo Prerequisite=AccessTools.Method(typeof(PowerupGenerator),"PrereqsMet");
 static bool Meets(PowerupGenerator owner,Powerup powerup)=>(bool)Prerequisite.Invoke(owner,new object[]{powerup});
 static bool Prefix(PowerupGenerator __instance,int num,List<PowerupPoolItem> pool,ref List<Powerup> __result)
 {
  int ordinary=FallbackPowerups.Pool(__instance).Select(p=>p.powerup).Where(p=>!FallbackPowerups.IsFallback(p)&&Meets(__instance,p)).Distinct().Count();
  var candidates=pool.Select(p=>p.powerup).Where(p=>p!=null&&Meets(__instance,p)&&(!FallbackPowerups.IsFallback(p)||ordinary<num)).ToList();
  var result=new List<Powerup>();
  for(int i=0;i<num&&candidates.Count>0;i++)
  {
   var selected=candidates[UnityEngine.Random.Range(0,candidates.Count)];result.Add(selected);candidates.RemoveAll(p=>p==selected);
  }
  __result=result;return false;
 }
}

static class AndroidSpecialize
{
 static readonly HashSet<TripleNextPowerup> Pending=new HashSet<TripleNextPowerup>();
 internal static int Count=>Pending.Count;
 internal static readonly FieldInfo Used=AccessTools.Field(typeof(TripleNextPowerup),"used");
 internal static Action<object,object> Handler(TripleNextPowerup owner)=>(Action<object,object>)Delegate.CreateDelegate(typeof(Action<object,object>),owner,AccessTools.Method(typeof(TripleNextPowerup),"OnPowerupApplied"));
 internal static void Started(TripleNextPowerup owner){if(!(bool)Used.GetValue(owner))Pending.Add(owner);}
 internal static void Finished(TripleNextPowerup owner)=>Pending.Remove(owner);
 internal static void Restored(TripleNextPowerup owner){if((bool)Used.GetValue(owner))Finished(owner);}
}
[HarmonyPatch(typeof(TripleNextPowerup),"Start")]
class AndroidSpecializeStart
{static bool Prefix(TripleNextPowerup __instance){AndroidSpecialize.Started(__instance);__instance.AddObserver(AndroidSpecialize.Handler(__instance),Powerup.AppliedNotifcation);return false;}}
[HarmonyPatch(typeof(TripleNextPowerup),"OnDestroy")]
class AndroidSpecializeDestroy
{static bool Prefix(TripleNextPowerup __instance){AndroidSpecialize.Finished(__instance);__instance.RemoveObserver(AndroidSpecialize.Handler(__instance),Powerup.AppliedNotifcation);return false;}}
[HarmonyPatch(typeof(TripleNextPowerup),"OnPowerupApplied")]
class AndroidSpecializeApply
{
 static bool Prefix(TripleNextPowerup __instance,object sender,ref bool ___used)
 {
  // Continue replays the already recorded applications, including any extra
  // Specialize applications. Do not produce them a second time during replay.
  if(RunRecorder.Instance?.Rebuilding==true)return false;
  if(___used)return false;___used=true;AndroidSpecialize.Finished(__instance);var powerup=(Powerup)sender;var player=PlayerController.Instance;
  if(RunResume.Key(powerup)=="split_fire_name")
  {
   var rear=player.GetComponentInChildren<ShootBulletsBehind>();
   if(rear!=null){Plugin.SetField(rear,"numOfBullets",3);Plugin.SetField(rear,"spread",60f);}
  }
  else{player.playerPerks.Equip(powerup);player.playerPerks.Equip(powerup);}
  return false;
 }
}

// Android ApplyAndNotify applies the perk before publishing the notification.
// Windows publishes at entry, before a first Split Fire emitter even exists.
[HarmonyPatch]
class AndroidPowerupApplicationOrder
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  var code=instructions.ToList();var field=AccessTools.Field(typeof(Powerup),nameof(Powerup.AppliedNotifcation));
  int at=code.FindIndex(c=>c.opcode==OpCodes.Ldsfld&&Equals(c.operand,field));
  if(at<1||at+1>=code.Count||code[at-1].opcode!=OpCodes.Ldarg_0||!(code[at+1].operand is MethodInfo post)||post.Name!="PostNotification"||post.GetParameters().Length!=2)
   throw new InvalidOperationException("Unsupported native Powerup notification sequence");
  for(int i=at-1;i<=at+1;i++){code[i].opcode=OpCodes.Nop;code[i].operand=null;}
  foreach(var instruction in code)
  {
   if(instruction.opcode==OpCodes.Ret)
   {
    var sender=new CodeInstruction(OpCodes.Ldarg_0);instruction.MoveLabelsTo(sender);
    yield return sender;yield return new CodeInstruction(OpCodes.Ldsfld,field);yield return new CodeInstruction(OpCodes.Call,post);
   }
   yield return instruction;
  }
 }
}
