using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.PerkSystem;
using flanne.PerkSystem.Actions;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool SpecializeTrace=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST")?.StartsWith("specialize-")==true;
 static IEnumerable<AttachPrefabToPlayerAction> SummonAttachments(Powerup power)
 {
  foreach(var effect in (PerkEffect[])WindField(power,"effects"))
   if(WindField(effect,"action") is AttachPrefabToPlayerAction attach)
   {var prefab=(GameObject)WindField(attach,"prefab");if(prefab.GetComponentInChildren<Summon>(true)!=null||prefab.GetComponentInChildren<SummonEgg>(true)!=null)yield return attach;}
 }
 static int Spawned(string prefab)=>Resources.FindObjectsOfTypeAll<Transform>().Count(t=>t.gameObject.scene.IsValid()&&t.name==prefab+"(Clone)");
 void SpecializeActors(int lances=6)
 {
  Require(Object.FindObjectsOfType<StabbingSummon>().Length==lances,"lance quantity matches triple reward and recorded ordinary repeats: "+lances);
  Require(Object.FindObjectsOfType<OrbitalWeapon>().Count(w=>w.SummonTypeID=="Scythe")==3,"tripled scythe creates three actual actors");
  Require(Object.FindObjectsOfType<ShootingSummon>().Count(w=>w.SummonTypeID=="GhostFriend")==3,"tripled ghost creates three actual actors");
  Require(Object.FindObjectsOfType<BulletEnhanceSummon>().Length==3,"tripled mirror creates three actual actors");
  Require(Object.FindObjectsOfType<SummonEgg>().Length==3,"tripled dragon creates three actual eggs");
 }
 IEnumerator SpecializePrerequisites(GameController game,Powerup power)
 {
  // Acquire prerequisites before opening the chest: Specialize correctly
  // applies to the first upgrade chosen, including a prerequisite.
  foreach(var prerequisite in power.prereqs)
  {if(ShanaMeets(game,power))break;yield return AndroidPick(game,prerequisite.name,game.playerXP.level);}
 }
 IEnumerator SpecializePick(GameController game,Powerup power)
 {
  yield return SpecializePrerequisites(game,power);
  yield return AndroidWhirlwindChest(game,"shana_perk_specialize_name");Require(ShanaCount==1,"exactly one chest multiplier is pending before "+power.name);
  var expected=SummonAttachments(power).GroupBy(a=>((GameObject)WindField(a,"prefab")).name).ToDictionary(g=>g.Key,g=>new[]{Spawned(g.Key),g.Sum(a=>(int)WindField(a,"amountToAttach"))});
  int before=ShanaApplications(RunResume.Key(power));yield return AndroidPick(game,power.name,game.playerXP.level);yield return null;
  Require(ShanaCount==0&&ShanaApplications(RunResume.Key(power))==before+3,"one chest reward creates exactly three journaled applications: "+power.name);
  foreach(var pair in expected)Require(Spawned(pair.Key)==pair.Value[0]+3*pair.Value[1],"triple attachment count for "+power.name+" / "+pair.Key+" actual="+Spawned(pair.Key));
 }
 IEnumerator CheckSpecialize(string mode)
 {
  RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.State.pending.Clear();Progression.Refresh();
  int index=NativeSelection.GunItems(Progression.Title.gunMenu).FindIndex(i=>i.data.name=="RevolverData");SaveSystem.data.gunHighestClear[index]=0;
  yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  if(mode=="specialize-fresh")
  {
   var counts=(Dictionary<Powerup,int>)WindField(game.powerupGenerator.GetComponent<AndroidPowerupCounts>(),"Counts");var definitions=counts.Keys.ToArray();
   var reward=definitions.Single(p=>RunResume.Key(p)=="shana_perk_specialize_name");counts[reward]=20;
   ((List<PowerupPoolItem>)WindField(game.powerupGenerator,"characterPool")).Single(i=>i.powerup==reward).numTimeRepeatable=20;
   Log("fixture-precondition","Increase only QA Specialize offer availability for one compact native chest/choice sweep; production map repeats unchanged. No runes, no weapon familiarity, stopped scheduler/clock.");
   var sourceType=AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidSpecialize");
   foreach(var definition in definitions)Require(ReferenceEquals(AccessTools.Method(sourceType,"Definition").Invoke(null,new object[]{definition}),definition),"pristine registry resolves "+RunResume.Key(definition));
   var roots=definitions.Where(p=>SummonAttachments(p).Any()).ToArray();File.WriteAllLines(Path.Combine(ResultDir,"summon-definition-audit.tsv"),roots.Select(p=>p.name+"\t"+RunResume.Key(p)+"\t"+string.Join(",",SummonAttachments(p).Select(a=>((GameObject)WindField(a,"prefab")).name+":"+WindField(a,"amountToAttach")))));
   var requested=new[]{"MagicSpear","MagicScythe","GhostFriend","MagicLens","DragonEgg"}.Select(name=>definitions.Single(p=>p.name==name)).ToArray();
   foreach(var power in requested.Concat(roots.Where(p=>!requested.Contains(p))))yield return SpecializePick(game,power);
   SpecializeActors();yield return PoiCapture("specialize-all-summons.png");
   float speed=PlayerController.Instance.stats[StatType.ReloadRate].Modify(1);var quick=definitions.Single(p=>p.name=="QuickHands");float bonus=((StatChange[])WindField(quick,"statChanges")).Where(s=>s.type==StatType.ReloadRate).Sum(s=>s.value);
   yield return SpecializePick(game,quick);CombatClose(PlayerController.Instance.stats[StatType.ReloadRate].Modify(1),speed+3*bonus,"stat-only choice retains the actual threefold bonus");
   SetPhase("PASS");Log("complete","All direct summon roots create triple actors through the real chest and upgrade flow; stat-only choice retains three applications.");Application.Quit(0);yield break;
  }
  foreach(string name in new[]{"MagicSpear","MagicScythe","Vitality"})
  {
   string key=RunResume.Key(Resources.FindObjectsOfTypeAll<Powerup>().First(p=>p.name==name));
   yield return AndroidWhirlwindChest(game,"shana_perk_specialize_name");
   Log("specialize-before",name+" pending="+ShanaCount+" used="+string.Join(",",ShanaSpecializes().Select(ShanaSpent)));
   yield return AndroidPick(game,name,game.playerXP.level);yield return null;
   Log("specialize-observed",name+" key="+key+" pending="+ShanaCount+" spears="+Object.FindObjectsOfType<StabbingSummon>().Length+" scythes="+Object.FindObjectsOfType<OrbitalWeapon>().Count(w=>w.SummonTypeID=="Scythe")+" hp="+PlayerController.Instance.playerHealth.maxHP+" applications="+ShanaApplications(key));
   yield return PoiCapture("specialize-"+name+".png");
  }
  SetPhase("PROBE");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(TripleNextPowerup),"OnPowerupApplied")]
class SpecializeNotificationTrace
{
 [HarmonyPriority(Priority.First)]
 static void Prefix(TripleNextPowerup __instance,ref object sender)
 {
  if(!RuntimeChecks.SpecializeTrace)return;
  var power=(Powerup)sender;bool used=(bool)AccessTools.Field(typeof(TripleNextPowerup),"used").GetValue(__instance);var key=RunResume.Key(power);
  if(!used)
  {
   foreach(string list in new[]{"effects","stackedEffects"})foreach(var effect in (PerkEffect[])AccessTools.Field(typeof(Powerup),list).GetValue(power))
   {var trigger=AccessTools.Field(typeof(PerkEffect),"trigger").GetValue(effect);var action=AccessTools.Field(typeof(PerkEffect),"action").GetValue(effect);RuntimeChecks.Log("specialize-recipe",key+" "+list+" "+trigger.GetType().Name+" -> "+action.GetType().Name+" trigger="+JsonUtility.ToJson(trigger));}
   if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="specialize-pristine-probe")
   {var counts=PowerupGenerator.Instance.GetComponent<AndroidPowerupCounts>();var originals=(Dictionary<Powerup,int>)AccessTools.Field(typeof(AndroidPowerupCounts),"Counts").GetValue(counts);sender=originals.Keys.Single(p=>RunResume.Key(p)==key);RuntimeChecks.Log("controlled-pristine-sender",key+" same="+ReferenceEquals(power,sender));}
  }
  RuntimeChecks.Log("specialize-notification","key="+key+" used="+used);
 }
}
[HarmonyPatch(typeof(AttachPrefabToPlayerAction),nameof(AttachPrefabToPlayerAction.Activate))]
class SpecializeAttachmentTrace
{static void Prefix(GameObject ___prefab,int ___amountToAttach){if(RuntimeChecks.SpecializeTrace)RuntimeChecks.Log("specialize-attachment",___prefab.name+" amount="+___amountToAttach);}}
