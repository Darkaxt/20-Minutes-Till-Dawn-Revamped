using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Runtime.Serialization;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
using PlayerXP=flanne.Player.PlayerXP;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

[Serializable]public sealed class RuneRewardDraw{public string rune;public int tier,rank;public bool capped;}
[Serializable]public sealed class RuneActivationChoice{public string rune;public int rank;public bool enabled;}
[Serializable,DataContract]public sealed class RuneBossReward
{
 [DataMember(IsRequired=true)]public int boss,phase,revealed;
 [DataMember(IsRequired=true)]public List<RuneRewardDraw> draws=new List<RuneRewardDraw>();
 [DataMember]public List<RuneActivationChoice> choices;
}
[Serializable]public sealed class RuneRewardSchedule{public int pair;public List<int> rows=new List<int>();}
[Serializable]public sealed class RuneRewardHistory{public string run;public List<RuneBossReward> bosses=new List<RuneBossReward>();}
[Serializable]public sealed class RuneRewardStateData
{public string id;public List<RuneRewardSchedule> schedules=new List<RuneRewardSchedule>();public List<RuneBossReward> rewards=new List<RuneBossReward>();public float xpLevels;public int levelEvent;}

public sealed class RuneRewardRun:MonoBehaviour
{
 internal static readonly string[][] Candidates={
  new[]{"PowerRuneData","AlacrityRuneData","MysticismRuneData","VigilanceRuneData","TenacityRuneData","CourageRuneData"},
  new[]{"ShockwaveRuneData","ElementalBarrageRuneData","CloseCombatRuneData","MomentumRuneData","AgilityRuneData","SeismicRuneData"},
  new[]{"GlassCannonRuneData","WildMagicRuneData","GunGlyphRuneData","BlessingRuneData","HealingRuneData","PremonitionData"},
  new[]{"CadenceRuneData","EtherealMagRuneData","DedicationRuneData","GrowthRuneData","BodyAndSoulData","WindWallData"}};
 public RuneRewardStateData State;GameController game;internal bool Applying{get;private set;}
 public static RuneRewardRun Instance;
 void Awake(){game=GetComponent<GameController>();Instance=this;}
 void OnDestroy(){if(Instance==this)Instance=null;}
 public static int[] Counts(string map,int darkness)
 {
  int offset=map==Progression.Temple?4:map==Progression.Pumpkin?2:0;
  int band=Math.Min(15,darkness+offset)/4;
  return Enumerable.Range(0,4).Select(row=>Math.Max(0,band+1-row)).ToArray();
 }
 internal bool Eligible=>!CustomMode.Active&&SelectedMap.MapData!=null&&new[]{Progression.Forest,Progression.Temple,Progression.Pumpkin,"10M_Forest","Endless_Forest","Challenge_Forest"}.Contains(SelectedMap.MapData.name);
 bool Repeating=>SelectedMap.MapData.name=="Endless_Forest";
 internal void Ensure()
 {
  if(State!=null||!Eligible)return;
  State=new RuneRewardStateData{id=Guid.NewGuid().ToString("N")};Schedule(0);
 }
 RuneRewardSchedule Schedule(int pair)
 {
  var saved=State.schedules.FirstOrDefault(s=>s.pair==pair);if(saved!=null)return saved;
  // The shuffle remains stable if a pre-boss checkpoint is reopened after
  // its permanent outcome was already committed to the profile journal.
  var bytes=Guid.ParseExact(State.id,"N").ToByteArray();int seed=BitConverter.ToInt32(bytes,0)^BitConverter.ToInt32(bytes,4)^pair;
  var random=new System.Random(seed);var rows=Counts(SelectedMap.MapData.name,Loadout.difficultyLevel).SelectMany((n,row)=>Enumerable.Repeat(row+1,n)).ToList();
  for(int i=rows.Count-1;i>0;i--){int j=random.Next(i+1);int old=rows[i];rows[i]=rows[j];rows[j]=old;}
  saved=new RuneRewardSchedule{pair=pair,rows=rows};State.schedules.Add(saved);return saved;
 }
 internal void Restore(RuneRewardStateData state)
 {State=state;Ensure();}
 internal static void Validate(RuneRewardStateData state)
 {
  if(state==null)return;
  if(!Guid.TryParseExact(state.id,"N",out _)||state.schedules==null||state.schedules.Count==0||state.schedules.Any(s=>s==null||s.pair<0||s.rows==null||s.rows.Any(t=>t<1||t>4))||state.schedules.Select(s=>s.pair).Distinct().Count()!=state.schedules.Count||state.rewards==null||state.xpLevels<0||float.IsNaN(state.xpLevels)||float.IsInfinity(state.xpLevels)||state.levelEvent<0||
   state.rewards.Any(r=>r==null||r.boss<0||r.phase<0||r.phase>2||r.draws==null||r.revealed<0||r.revealed>r.draws.Count||r.draws.Any(d=>d==null||d.tier<1||d.tier>4||!Candidates[d.tier-1].Contains(d.rune)||d.rank<1||d.rank>5))||state.rewards.Select(r=>r.boss).Distinct().Count()!=state.rewards.Count)
   throw new InvalidOperationException("Invalid rune reward checkpoint; it has been preserved.");
  foreach(var reward in state.rewards)if(reward.choices!=null&&(reward.choices.Any(c=>c==null)||reward.choices.Select(c=>c.rune).Distinct().Count()!=reward.choices.Count||reward.choices.Any(c=>!reward.draws.Any(d=>!d.capped&&d.rune==c.rune)||c.rank!=reward.draws.Where(d=>!d.capped&&d.rune==c.rune).Max(d=>d.rank))))throw new InvalidOperationException("Invalid rune activation choices in checkpoint; it has been preserved.");
 }
 internal int ActiveRank(string name)
 {
  return Loadout.RuneSelection?.Where(r=>r.name==name).Select(r=>r.level).FirstOrDefault()??0;
 }
 internal RuneRewardDraw PendingChoice(RuneBossReward reward)
 =>reward.draws.Where(d=>!d.capped).GroupBy(d=>d.rune).Select(g=>g.OrderByDescending(d=>d.rank).First()).FirstOrDefault(d=>ActiveRank(d.rune)==0&&!(reward.choices?.Any(c=>c.rune==d.rune)??false));
 internal IEnumerator Choose(RuneBossReward reward,RuneRewardDraw draw,bool enabled,Action finished)
 {
  if(Applying||PendingChoice(reward)?.rune!=draw.rune)yield break;
  Applying=true;
  try
  {
   if(enabled)
   {
    var copy=RunResume.CopyRune(RuneProgression.Definition(draw.rune));copy.level=draw.rank;
    if(Loadout.RuneSelection==null)Loadout.RuneSelection=new List<RuneData>();
    Loadout.RuneSelection.RemoveAll(r=>r.name==draw.rune);Loadout.RuneSelection.Add(copy);
    copy.Apply(PlayerController.Instance);
    // Native effect components initialize in Start before their runtime state
    // can be captured. This is the same lifecycle boundary as Continue rebuild.
    yield return null;
   }
   if(reward.choices==null)reward.choices=new List<RuneActivationChoice>();
   reward.choices.Add(new RuneActivationChoice{rune=draw.rune,rank=draw.rank,enabled=enabled});
  }
  finally{Applying=false;}
  StatsOverlay.Instance.Dirty();RunResume.Save();
  Plugin.Trace("rune-reward-choice",new{draw.rune,draw.rank,enabled});finished();
 }
 // Called only from the existing scheduled-encounter identity owner, before
 // the dead actor is returned to its pool. Elites/minibosses cannot enter here.
 internal void BossKilled(int boss)
 {
  if(!Eligible||boss<0||(!Repeating&&boss>=Math.Min(2,SelectedMap.MapData.bossSpawns.Count)))return;Ensure();
  if(State.rewards.Any(r=>r.boss==boss))return;
  var journal=Progression.State;
  if(journal.runeHistory==null||journal.runeHistory.run!=State.id)journal.runeHistory=new RuneRewardHistory{run=State.id};
  var result=journal.runeHistory.bosses.FirstOrDefault(r=>r.boss==boss);
  if(result==null)
  {
   result=new RuneBossReward{boss=boss};var random=new System.Random(Guid.NewGuid().GetHashCode());var profile=RuneProgression.Profile(Loadout.CharacterSelection.name);
   var rows=Schedule(boss/2).rows;int split=rows.Count/2;
   foreach(int tier in SelectedMap.MapData.name=="10M_Forest"?rows:boss%2==0?rows.Take(split):rows.Skip(split))
   {
    string name=Candidates[tier-1][random.Next(6)];var rank=profile.ranks.Single(r=>r.name==name);bool capped=rank.rank==5;
    if(!capped){rank.rank++;if(!profile.enabled.Contains(name))profile.enabled.Add(name);}
    result.draws.Add(new RuneRewardDraw{rune=name,tier=tier,rank=rank.rank,capped=capped});
   }
   journal.runeHistory.bosses.Add(result);Progression.Save();
  }
  // Presentation/delivery belongs to the run checkpoint, permanent grants to
  // the idempotent journal entry. Reopening an older checkpoint never rerolls.
  State.rewards.Add(new RuneBossReward{boss=boss,draws=result.draws.Select(d=>new RuneRewardDraw{rune=d.rune,tier=d.tier,rank=d.rank,capped=d.capped}).ToList()});
  // Health.DeathEvent precedes native drop creation. Snapshot only at the
  // existing stable frame/state boundary, after that synchronous death flow.
 }
 internal RuneBossReward Pending=>State?.rewards.FirstOrDefault(r=>r.phase<2&&r.draws.Count>0);
 void Update()
 {
  if(RunResume.Restoring||!(game.CurrentState is CombatState))return;Ensure();
  if(Pending!=null){game.ChangeState<RuneRewardState>();return;}
  if(State!=null&&(State.xpLevels>0||State.levelEvent>0))DeliverXP();
 }
 internal void GrantXP(RuneBossReward reward)
 {
  State.xpLevels+=.5f*reward.draws.Where(d=>d.capped).Sum(d=>d.tier);
 }
 void DeliverXP()
 {
  var xp=game.playerXP;
  if(State.levelEvent==0)
  {
   float requirement=AndroidExperience.Requirement(xp.level),stored=(float)AccessTools.Field(typeof(PlayerXP),"xp").GetValue(xp);
   float needed=1-stored/requirement;
   // Explicit level-equivalent rewards bypass pickup multipliers. Advance one
   // actual level at a time so the native Combat listener owns every regular
   // upgrade/evolution branch, rather than constructing a second choice queue.
   if(State.xpLevels>=needed)
   {State.xpLevels=Mathf.Max(0,State.xpLevels-needed);Plugin.SetField(xp,"<level>k__BackingField",xp.level+1);State.levelEvent=xp.level;Plugin.SetField(xp,"xp",0f);}
   else{Plugin.SetField(xp,"xp",stored+State.xpLevels*requirement);State.xpLevels=0;}
   xp.OnXPToLevelChanged.Invoke(AndroidExperience.Requirement(xp.level));xp.OnXPChanged.Invoke((float)AccessTools.Field(typeof(PlayerXP),"xp").GetValue(xp));
   // Persist the unconsumed level notification. If the process exits while a
   // choice is open, Continue reopens that choice without awarding XP twice.
   RunResume.Save();
  }
  if(State.levelEvent>0){int level=State.levelEvent;State.levelEvent=0;xp.OnLevelChanged.Invoke(level);}
 }
}

[HarmonyPatch(typeof(LoadoutInitializer),"Start")]
class RuneBattleCopies
{
 [HarmonyPriority(Priority.First)]static void Prefix()
 {
  if(RunResume.Restoring||Loadout.RuneSelection==null)return;
  Loadout.RuneSelection=Loadout.RuneSelection.Select(RunResume.CopyRune).ToList();
 }
}
