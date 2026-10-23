using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Steamworks;
using flanne;
using flanne.CharacterPassives;
namespace TwentyMinutes.Modernization;

// Source achievement flags and claimed reward IDs share the existing per-save
// companion journal. Native Steam ownership and currency storage stay in place.
public static class SourceAchievementRewards
{
 public class Manifest{public Entry[] entries;public Reward[] rewards;}
 public class Entry
 {
  public int type;public string steamId,name,description,icon,iconGray;
  internal Sprite Earned,Locked;
 }
 public class Reward{public int id,count,amount;public string icon;internal Sprite Icon;}
 internal static Manifest Data;
 static Dictionary<string,Entry> bySteam;
 internal static void Prepare()
 {
  if(Data!=null)return;
  string root=Path.Combine(Plugin.AssetRoot,"achievements");
  using(var stream=File.OpenRead(Path.Combine(root,"manifest.json")))Data=(Manifest)new DataContractJsonSerializer(typeof(Manifest)).ReadObject(stream);
  Sprite Read(string file)
  {
   if(file==null)return null;
   var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(Path.Combine(root,file)));texture.filterMode=FilterMode.Point;
   return Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
  }
  foreach(var entry in Data.entries){entry.Earned=Read(entry.icon);entry.Locked=Read(entry.iconGray);}
  foreach(var reward in Data.rewards)reward.Icon=Read(reward.icon);
  bySteam=Data.entries.ToDictionary(e=>e.steamId,StringComparer.Ordinal);
 }
 internal static void Load()
 {
  Prepare();
  // Existing journals predate these source fields.
  if(Progression.State.sourceAchievements==null)Progression.State.sourceAchievements=new List<int>();
  if(Progression.State.sourceAchievementRewards==null)Progression.State.sourceAchievementRewards=new List<int>();
  ImportSteam();
 }
 internal static void ImportSteam()
 {
  // This gates only a read of the native Steam API, never gameplay recording.
  // The menu also observes the actual stats-received event; there is no retry timer.
  if(!SteamClient.IsValid)return;
  bool changed=false;
  foreach(var entry in Data.entries)
   if(new Steamworks.Data.Achievement(entry.steamId).State&&!Progression.State.sourceAchievements.Contains(entry.type))
   {Progression.State.sourceAchievements.Add(entry.type);changed=true;}
  if(changed)Progression.Save();
 }
 internal static void Record(string steamId)
 {
  if(CustomMode.Active)return;
  if(!bySteam.TryGetValue(steamId,out var entry))return; // Three native-only badges are outside the source catalog.
  var finish=Progression.State.sourceAchievements;
  if(finish.Contains(entry.type))return;
  finish.Add(entry.type);SaveSystem.Save();
 }
 internal static int Count=>Progression.State.sourceAchievements.Count;
 internal static bool Finished(Entry entry)=>Progression.State.sourceAchievements.Contains(entry.type);
 internal static bool Claimed(Reward reward)=>Progression.State.sourceAchievementRewards.Contains(reward.id);
 internal static bool CanClaim(Reward reward)=>Count>=reward.count&&!Claimed(reward);
 internal static void Claim(Reward reward)
 {
  if(Count<reward.count||Claimed(reward))return;
  Progression.State.sourceAchievementRewards.Add(reward.id);
  // Source TakeAchieveReward requests a save here, but Save only marks its
  // shared data dirty. The subsequent Points request coalesces into that write.
  // Keep the native persistence owner and save both final values together.
  int amount=PointsTracker.pts+reward.amount;
  AccessTools.StaticFieldRefAccess<int>(typeof(PointsTracker),"_pts")=amount;
  SaveSystem.data.points=amount;
  SaveSystem.Save();
  PointsTracker.PointsChangedEvent?.Invoke(typeof(PointsTracker),SaveSystem.data.points);
 }
}
[HarmonyPatch(typeof(SteamIntegration),nameof(SteamIntegration.UnlockAchievement))]
class SourceAchievementRecording
{
 static bool Prefix(string id)
 {
  SourceAchievementRewards.Record(id);
  // Source flags/save are local. Dispatch to the native platform only when
  // its client interface exists; unavailable Steam never gates the reward.
  return SteamClient.IsValid;
 }
}

[HarmonyPatch(typeof(flanne.Core.PlayerSurvivedState),"CheckAchievmentUnlocks")]
class SourceVictoryAchievementConditions
{
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
 {
  var code=instructions.ToList();var field=AccessTools.Field(typeof(Loadout),nameof(Loadout.difficultyLevel));
  foreach(int threshold in new[]{1,5,10,15})
  {
   var matches=Enumerable.Range(0,code.Count-2).Where(i=>code[i].LoadsField(field)&&code[i+1].LoadsConstant(threshold)).ToArray();
   if(matches.Length!=1||(code[matches[0]+2].opcode!=OpCodes.Blt&&code[matches[0]+2].opcode!=OpCodes.Blt_S))throw new InvalidOperationException("Unsupported native Darkness achievement predicate: "+threshold);
   var branch=code[matches[0]+2];branch.opcode=branch.opcode==OpCodes.Blt?OpCodes.Bne_Un:OpCodes.Bne_Un_S;
  }
  int skill=code.FindIndex(c=>c.Calls(AccessTools.PropertyGetter(typeof(ShootDetector),nameof(ShootDetector.usedManualShooting))));
  if(skill<2||code[skill-2].opcode!=OpCodes.Ldarg_0||!code[skill-1].Calls(AccessTools.PropertyGetter(typeof(flanne.Core.GameState),"shootDetector"))||(code[skill+1].opcode!=OpCodes.Brtrue&&code[skill+1].opcode!=OpCodes.Brtrue_S))throw new InvalidOperationException("Unsupported native Reckless achievement predicate");
  code[skill-2].opcode=OpCodes.Ldsfld;code[skill-2].operand=AccessTools.Field(typeof(RunRecorder),nameof(RunRecorder.Instance));
  code[skill-1].opcode=OpCodes.Ldfld;code[skill-1].operand=AccessTools.Field(typeof(RunRecorder),nameof(RunRecorder.SourceRecklessSkillUsed));
  code[skill].opcode=OpCodes.Nop;code[skill].operand=null;
  code[skill+1].opcode=code[skill+1].opcode==OpCodes.Brtrue?OpCodes.Brfalse:OpCodes.Brfalse_S;
  return code;
 }
}

// Source SkillBtn.Init clears its private usedSkillBtn bit. UseSkill marks it
// before invoking the ready skill, even if that skill's inner guard does nothing.
// Keep this as button-initialization state, not saved acquisition history.
[HarmonyPatch(typeof(SkillPassive),"Start")]
class SourceSkillButtonInitialization
{static void Prefix()=>RunRecorder.Instance.SourceRecklessSkillUsed=false;}
[HarmonyPatch(typeof(DumpAmmoPassive),"PerformSkill")]
class SourceAbbySkillButtonUse
{static void Prefix()=>RunRecorder.Instance.SourceRecklessSkillUsed=true;}
