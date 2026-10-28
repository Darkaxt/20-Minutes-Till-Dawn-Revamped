using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 Gamepad runePad;
 IEnumerator RuneConfirm(GameController game)
 {
  while(!Object.FindObjectOfType<RuneConversionVisual>().Ready)yield return null;
  var button=game.GetComponentsInChildren<Button>().Single(b=>b.name=="RuneRewardContinue");
  Require(EventSystem.current.currentSelectedGameObject==button.gameObject,"rune reward owns actual native focus");
  if(runePad==null)runePad=InputSystem.AddDevice<Gamepad>("RuneRewardController");game.playerInput.SwitchCurrentControlScheme("Gamepad",runePad);game.playerInput.neverAutoSwitchControlSchemes=true;
  yield return SelectorPad(runePad,GamepadButton.South);
 }
 Health RuneBoss(GameController game,int index)=>((Dictionary<Health,Tuple<bool,int>>)WindField(game.GetComponent(AccessTools.TypeByName("TwentyMinutes.Modernization.RunRecorder")),"Encounters")).Single(e=>e.Value.Item1&&e.Value.Item2==index).Key;
 void RuneSpawnBoss(int index)
 {
  var source=Object.FindObjectOfType<AndroidEndlessRun>();WorldClearSchedule(source);((List<int>)WindField(source,"pendingBosses")).Add(index);WorldTime(SelectedMap.MapData.bossSpawns[index].timeToSpawn);ElementCall(source,"Update");
 }
 void RuneDefeatBoss(GameController game,int index)
 {var boss=RuneBoss(game,index);boss.transform.position=PlayerController.Instance.transform.position+Vector3.right*18;Physics2D.SyncTransforms();boss.TakeDamage(DamageType.Bullet,100000000);}
 IEnumerator CheckRuneRewards(string mode)
 {
  if(mode=="rune-rewards-menu"||mode=="rune-rewards-legacy")
  {
   if(mode.EndsWith("legacy"))
   {
    var legacy=RuneProgression.Profile("Shana");var copied=RuneProgression.Profile("Diamond");
    Require(legacy.ranks.Count==24&&legacy.ranks.All(r=>r.rank==2)&&copied.ranks.All(r=>r.rank==2),"desktop legacy purchased ranks seed every character without losing earned power");
    Require(legacy.enabled.SequenceEqual(new[]{"PowerRuneData","AlacrityRuneData"})&&copied.enabled.SequenceEqual(legacy.enabled),"legacy multiple selections seed each character once");
   }
   yield return CheckRuneProgressionUI(Progression.Title);yield break;
  }
  if(mode=="rune-rewards-maps"){yield return RuneRewardMaps();yield break;}
  if(mode=="rune-rewards-modes"){yield return RuneRewardMaps(false,true);yield break;}
  if(mode=="rune-rewards-standard-maps"){yield return RuneRewardMaps(true);yield break;}
  if(mode=="rune-rewards-tome-first"){yield return RuneRewardTomeFirst();yield break;}
  if(mode=="rune-rewards-continue")yield return CombatBuffContinue();
  else
  {
   RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",8,prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());
  }
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  var run=RuneRewardRun.Instance;
  if(mode.StartsWith("rune-rewards-mechanics")){bool original=mode.EndsWith("original");if(ColorfulStyle.Instance.Enabled==original)ColorfulStyle.Instance.Toggle();yield return RuneMechanicsWorkflow(game);yield break;}
  if(mode=="rune-rewards-evolution"){yield return RuneRewardEvolution(game);yield break;}
  if(mode=="rune-rewards-continue")
  {
   Require(run.State.rewards.Count==1&&run.State.rewards[0].phase==0&&run.State.rewards[0].revealed==1,"fresh-process Continue retains the first revealed slot and pending lottery");
   int before=game.playerXP.level;float levels=.5f*run.State.rewards[0].draws.Sum(d=>d.tier);
   while(!(game.CurrentState is RuneRewardState))yield return null;
   while(Object.FindObjectOfType<RuneConversionVisual>()==null||Object.FindObjectOfType<RuneConversionVisual>().Progress<.4f)yield return null;
   Require(!game.GetComponentsInChildren<TMP_Text>().Any(t=>t.name=="RuneRewardExperience"),"conversion has no debug XP readout");yield return PoiCapture("rune-xp-continued.png");
   yield return RuneConfirm(game);
   while(game.CurrentState is RuneRewardState)yield return null;
   int count=0;
   while(run.State.xpLevels>0||run.State.levelEvent>0||game.CurrentState is PowerupMenuState)
   {
    if(game.CurrentState is PowerupMenuState&&WindField(game.powerupMenu,"ConfirmEvent")!=null)
    {game.powerupMenu.OnConfirmClicked();count++;while(game.CurrentState is PowerupMenuState&&WindField(game.powerupMenu,"ConfirmEvent")!=null)yield return null;}
    else yield return null;
   }
   Require(game.playerXP.level==before+Mathf.FloorToInt(levels)&&count==Mathf.FloorToInt(levels),"converted tiers yield exact levels and every earned native upgrade choice");
   Require(run.State.rewards[0].phase==2,"XP phase committed once");
   int after=game.playerXP.level;ElementCall(run,"BossKilled",0);yield return null;Require(game.playerXP.level==after&&run.State.rewards.Count==1,"duplicate scheduled identity does not grant XP again");
   // Second boss now exercises uncapped permanent ranks and the rune window.
   foreach(var rank in RuneProgression.Profile("Shana").ranks)rank.rank=0;
   RuneSpawnBoss(1);
   RuneDefeatBoss(game,1);while(!(game.CurrentState is RuneRewardState))yield return null;
   Require(run.State.rewards[1].draws.Count==3&&run.State.rewards[1].draws.All(d=>!d.capped),"second real arena boss awards the other half as permanent ranks");
   Require(RuneProgression.Profile("Shana").ranks.Sum(r=>r.rank)==3,"three lottery draws award exactly three permanent ranks with replacement");
   while(!Object.FindObjectOfType<RuneConversionVisual>().Ready)yield return null;yield return PoiCapture("rune-awarded-icons.png");yield return RuneConfirm(game);while(!(game.CurrentState is CombatState))yield return null;
   Require(!Loadout.RuneSelection.Any(),"permanent awards do not retroactively change the active run loadout");
   ElementClear();yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Exact pending rune Continue, level compensation/native choices, duplicate rejection and second arena boss ranks");Application.Quit(0);yield break;
  }
  foreach(string map in new[]{"20M_Forest","20M_Temple","20M_PumpkinPatch"})for(int d=0;d<=15;d++)
  {
   int offset=map=="20M_Temple"?4:map=="20M_PumpkinPatch"?2:0,b=Math.Min(15,d+offset)/4;
   Require(RuneRewardRun.Counts(map,d).SequenceEqual(Enumerable.Range(0,4).Select(row=>Math.Max(0,b+1-row))),"all Forest table bands and map offsets: "+map+" D"+d);
  }
  var profile=RuneProgression.Profile("Shana");foreach(var rank in profile.ranks)rank.rank=5;
  var other=RuneProgression.Profile("Diamond");other.ranks[0].rank=1;Require(profile.ranks[0].rank==5,"character rune profiles have independent rank objects");
  run.State=null;ElementCall(run,"Ensure");var rows=run.State.schedules[0].rows;Require(rows.Count==6&&rows.Count(t=>t==1)==3&&rows.Count(t=>t==2)==2&&rows.Count(t=>t==3)==1,"D8 full six-draw schedule retains row totals after shuffling");
  RuneSpawnBoss(0);
  RuneDefeatBoss(game,0);while(!(game.CurrentState is RuneRewardState))yield return null;
  Require(run.State.rewards.Count==1&&run.State.rewards[0].draws.Count==3&&run.State.rewards[0].draws.All(d=>d.capped),"first actual arena boss grants three capped draws and never rerolls them");
  Require(run.State.rewards[0].phase==0,"all-capped lottery first reveals each slot inside the popup");
  Require(ObjectPooler.SharedInstance.pooledObjectsDictionary["DevilDeal"].Any(o=>o.activeSelf),"ordinary arena-boss Tome drop still spawns alongside rune rewards");
  while(run.State.rewards[0].revealed<1)yield return null;
  Require(run.State.rewards[0].phase==0&&run.State.rewards[0].revealed==1,"first slot stops before the remaining slots and conversion");
  RunResume.Save();File.Copy(RunResume.PathName,Path.Combine(ResultDir,"saved-run.json"),true);yield return PoiCapture("rune-lottery-reveal.png");
  while(Object.FindObjectOfType<RuneConversionVisual>()==null||Object.FindObjectOfType<RuneConversionVisual>().Progress<.4f)yield return null;
  var visual=Object.FindObjectOfType<RuneConversionVisual>();visual.enabled=false;bool previousStyle=ColorfulStyle.Instance.Enabled;
  foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(960,720),new Vector2Int(720,720)})
  {
   Screen.SetResolution(size.x,size.y,false);while(Screen.width!=size.x||Screen.height!=size.y)yield return null;Canvas.ForceUpdateCanvases();yield return null;
   foreach(bool style in new[]{false,true})
   {if(ColorfulStyle.Instance.Enabled!=style)ColorfulStyle.Instance.Toggle();yield return PoiCapture("rune-conversion-"+size.x+"x"+size.y+"-"+(style?"reborn":"original")+".png");
    var panel=visual.GetComponent<RectTransform>();var panelCorners=new Vector3[4];panel.GetWorldCorners(panelCorners);Require(panelCorners.All(c=>c.x>=0&&c.x<=Screen.width&&c.y>=0&&c.y<=Screen.height),"opaque popup stays on-screen at "+size);
    foreach(var icon in visual.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("RuneSlot")||i.name.StartsWith("ConvertedXP"))){var corners=new Vector3[4];icon.rectTransform.GetWorldCorners(corners);Require(corners.All(c=>c.x>=panelCorners[0].x&&c.x<=panelCorners[2].x&&c.y>=panelCorners[0].y&&c.y<=panelCorners[2].y),"rune/XP artwork remains inside the popup at "+size);}}
  }
  if(ColorfulStyle.Instance.Enabled!=previousStyle)ColorfulStyle.Instance.Toggle();visual.enabled=true;
  File.WriteAllText(Path.Combine(ResultDir,"rune-profiles.json"),UnityEngine.JsonUtility.ToJson(profile,true));
  SetPhase("PASS");Log("complete","Three-area table, character isolation, actual first boss split/capped lottery and pending XP checkpoint");Application.Quit(0);
 }
 IEnumerator RuneMechanicsWorkflow(GameController game)
 {
  var p=PlayerController.Instance;var types=new[]{"ElementalBarrageRuneData","WildMagicRuneData","DedicationRuneData","SeismicRuneData"};
  foreach(string key in types){var rune=Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.name==key);var copy=Object.Instantiate(rune);copy.name=key;copy.level=5;copy.Apply(p);}
  yield return null;
  var barrage=p.GetComponentInChildren<ElementalBarrageRune>();int start=(int)WindField(barrage,"inflictCounter");var target=EliteSpawn();target.maxHP=10000;
  var control=target.GetComponent<ElementalControl>()??target.gameObject.AddComponent<ElementalControl>();ElementCall(control,"Electrocute");ElementCall(control,"Disorient");
  Require((int)WindField(barrage,"inflictCounter")==start+2,"electrical stun and wind disorientation count once each without emitting cold notifications");
  var generator=PowerupGenerator.Instance;var ghost=Resources.FindObjectsOfTypeAll<Powerup>().First(power=>RunResume.Key(power)=="ghost_friend_name");p.playerPerks.Equip(ghost);yield return null;
  Require((bool)WindField(p.GetComponentInChildren<DedicationRune>(),"active"),"Seismic Ward pair does not disable Dedication's one offensive family");
  var spear=Resources.FindObjectsOfTypeAll<Powerup>().First(power=>RunResume.Key(power)=="magic_spear_name");p.playerPerks.Equip(spear);yield return null;
  Require(!(bool)WindField(p.GetComponentInChildren<DedicationRune>(),"active"),"adding another offensive family removes Dedication");
  foreach(var s in Object.FindObjectsOfType<Summon>().Where(s=>s.SummonTypeID=="GhostFriend"))s.gameObject.SetActive(false);
  Require((bool)WindField(p.GetComponentInChildren<DedicationRune>(),"active"),"actual Ghost disable leaves paired Magic Spears as one family and refreshes Dedication");
  var spread=p.GetComponentInChildren<SpreadStatusRune>();var dead=EliteSpawn();dead.transform.position=p.transform.position+Vector3.right*4;var near=EliteSpawn();near.transform.position=dead.transform.position+Vector3.right*.3f;near.maxHP=10000;Physics2D.SyncTransforms();
  CurseSystem.Instance.Curse(dead.gameObject);SpreadSuccess=true;SpreadRollControl=true;SeedFirstRoll(0,.2f);dead.TakeDamage(DamageType.Bullet,1000000);SpreadRollControl=false;
  var cursed=(List<Health>)WindField(CurseSystem.Instance,"_cursedTargets");Require(cursed.Contains(near),"Wild Magic restarts the actual Curse chain on a victim killed by a bullet");
  var holy=Resources.FindObjectsOfTypeAll<Powerup>().First(power=>RunResume.Key(power)=="holy_arts_name");p.playerPerks.Equip(holy);yield return null;
  var victim=EliteSpawn();victim.transform.position=p.transform.position+Vector3.left*4;var next=EliteSpawn();next.transform.position=victim.transform.position+Vector3.left*.3f;next.maxHP=10000;Physics2D.SyncTransforms();int hp=next.HP;
  SpreadRollControl=true;SeedFirstRoll(0,.2f);victim.TakeDamage(DamageType.Smite,1000000);SpreadRollControl=false;
  while(next.HP==hp)yield return null;Require(next.HP<hp,"Smite kill echoes native modified Smite damage to a nearby target");
  yield return PoiCapture("rune-mechanics.png");ElementClear();yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Actual new elemental applications, paired-family Dedication and Curse/Smite death chains");Application.Quit(0);
 }
 IEnumerator RuneRewardEvolution(GameController game)
 {
  var xp=game.playerXP;Plugin.SetField(xp,"<level>k__BackingField",19);Plugin.SetField(xp,"xp",AndroidXPRequirement(19)*.5f);var run=RuneRewardRun.Instance;ElementCall(run,"Ensure");run.State.xpLevels=2.5f;
  int evolutions=0,choices=0;
  while(run.State.xpLevels>0||run.State.levelEvent>0||!(game.CurrentState is CombatState))
  {
   if(game.CurrentState is GunEvoMenuState&&WindField(game.gunEvoMenu,"ClickEvent")!=null&&game.gunEvoMenu.GetEntry(0).IsInteractable())
   {Click(game.gunEvoMenu.GetEntry(0));evolutions++;while(game.CurrentState is GunEvoMenuState)yield return null;}
   else if(game.CurrentState is PowerupMenuState&&WindField(game.powerupMenu,"ConfirmEvent")!=null)
   {game.powerupMenu.OnConfirmClicked();choices++;while(game.CurrentState is PowerupMenuState&&WindField(game.powerupMenu,"ConfirmEvent")!=null)yield return null;}
   else yield return null;
  }
  Require(xp.level==22&&evolutions==1&&choices==2,"level-based conversion preserves the real level20 weapon evolution and later two upgrade choices");
  ElementClear();yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Rune XP follows actual native level-up/evolution branching");Application.Quit(0);
 }
 float AndroidXPRequirement(int level){int n=level+1;return n<21?n*10-5:n<41?n*13-6:n*16-8;}
 IEnumerator RuneRewardMaps(bool standardOnly=false,bool modesOnly=false)
 {
  foreach(var pair in new[]{Tuple.Create("20M_Forest",0),Tuple.Create("20M_Forest",4),Tuple.Create("20M_Forest",12),Tuple.Create("20M_Temple",0),Tuple.Create("20M_PumpkinPatch",2),Tuple.Create("10M_Forest",0),Tuple.Create("Endless_Forest",0),Tuple.Create("Challenge_Forest",0)})
  {
   if(standardOnly&&!pair.Item1.StartsWith("20M_"))continue;
   if(modesOnly&&pair.Item1.StartsWith("20M_"))continue;
   RunResume.Invalidate();if(!Progression.State.earned.Contains("scenario:"+pair.Item1))Progression.State.earned.Add("scenario:"+pair.Item1);Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData",pair.Item1,pair.Item2,prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());
   var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var run=RuneRewardRun.Instance;ElementCall(run,"Ensure");
   var profile=RuneProgression.Profile("Shana");foreach(var r in profile.ranks)r.rank=0;
   {
    int total=RuneRewardRun.Counts(pair.Item1,pair.Item2).Sum();
    int bosses=pair.Item1=="Endless_Forest"?4:pair.Item1=="10M_Forest"?1:2;
    for(int boss=0;boss<bosses;boss++)
    {
     if(pair.Item1=="Endless_Forest")
     {
      var source=Object.FindObjectOfType<AndroidEndlessRun>();WorldClearSchedule(source);Plugin.SetField(source,"nextBoss",0f);Plugin.SetField(source,"bossLevel",boss);ElementCall(source,"Update");
      var live=(Dictionary<Health,AndroidEndlessBossState>)WindField(source,"liveBosses");var actor=live.Single(p=>p.Value.level==boss).Key;actor.transform.position=PlayerController.Instance.transform.position+Vector3.right*6;Physics2D.SyncTransforms();actor.TakeDamage(DamageType.Bullet,100000000);
     }
     else{RuneSpawnBoss(boss);RuneDefeatBoss(game,boss);}yield return null;
     int expected=bosses==1?total:boss%2==0?total/2:total-total/2;Require(run.State.rewards.Single(r=>r.boss==boss).draws.Count==expected,"actual arena split "+pair.Item1+" D"+pair.Item2+" boss"+boss+" = "+expected);
     foreach(var drop in ObjectPooler.SharedInstance.pooledObjectsDictionary["DevilDeal"].Where(o=>o.activeSelf).ToArray())ReturnFixtureActor(drop);
     if(expected>0){while(!(game.CurrentState is RuneRewardState))yield return null;yield return RuneConfirm(game);while(!(game.CurrentState is CombatState))yield return null;}
    }
    Require(profile.ranks.Sum(r=>r.rank)==total*(bosses==4?2:1),"actual arena bosses grant exact permanent ranks for "+pair.Item1+" D"+pair.Item2);
    if(pair.Item1=="Endless_Forest")Require(run.State.schedules.Count==2,"Endless creates a distinct persisted schedule for each pair");
    if(pair.Item1=="Challenge_Forest"){ElementCall(run,"BossKilled",2);Require(run.State.rewards.Count==2,"Challenge awards only its first pair");}
   }
   ElementClear();yield return AchievementExit(game,false);
  }
  RunResume.Invalidate();yield return OpenCustomConfiguration();Loadout.RuneSelection=new List<RuneData>();yield return MenuPointer(CustomButton("CustomStart"));GameController custom;
  while((custom=Object.FindObjectOfType<GameController>())==null||!(custom.CurrentState is CombatState))yield return null;StopFixtureSpawning();var customRun=RuneRewardRun.Instance;string owned=Ownership();ElementCall(customRun,"BossKilled",0);Require(CustomMode.Active&&customRun.State==null&&Ownership()==owned,"actual Custom run remains excluded from permanent rune rewards");
  ElementClear();yield return AchievementExit(custom,false);SetPhase("PASS");Log("complete",standardOnly?"All Standard odd/even arena splits, map offsets and actual Custom exclusion":"All area/mode splits, recurring actual Endless boss pairs, Challenge and Custom exclusion");Application.Quit(0);
 }
 IEnumerator RuneRewardTomeFirst()
 {
  RunResume.Invalidate();Progression.State.earned.Add("scenario:20M_Temple");Progression.Refresh();yield return AchievementRun("Shana","RevolverData","20M_Temple",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var run=RuneRewardRun.Instance;foreach(var rank in RuneProgression.Profile("Shana").ranks)rank.rank=0;
  // Hold only the presentation owner across real pickup contact to exercise
  // the Tome-first ordering deterministically, without faking a native menu.
  run.enabled=false;RunResume.Save();RuneSpawnBoss(0);RuneBoss(game,0).TakeDamage(DamageType.Bullet,100000000);
  var drop=ObjectPooler.SharedInstance.pooledObjectsDictionary["DevilDeal"].First(o=>o.activeSelf);PlayerController.Instance.SetPosition(drop.transform.position);Physics2D.SyncTransforms();
  while(!(game.CurrentState is DevilDealState))yield return null;run.enabled=true;
  var checkpoint=RunResume.Read();Require(!checkpoint.killedBosses.Contains(0)||checkpoint.devilDeals>0,"no death-callback checkpoint loses the ordinary Tome drop");
  Require(run.State.rewards.Count==1&&run.State.rewards[0].phase==0&&Object.FindObjectOfType<RuneConversionVisual>()==null,"permanent rune outcome waits behind the actual native Tome menu");
  while(WindField(game.devilDealMenu,"ConfirmEvent")==null)yield return null;game.devilDealMenu.OnConfirmClicked();
  while(!(game.CurrentState is RuneRewardState))yield return null;yield return RuneConfirm(game);while(!(game.CurrentState is CombatState))yield return null;
  Require(run.State.rewards[0].phase==2&&RuneProgression.Profile("Shana").ranks.Sum(r=>r.rank)==1,"ordinary Tome choice completes before the one first-boss permanent rank");
  ElementClear();yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Actual Tome-first collision/reward ordering and stable checkpoint boundary");Application.Quit(0);
 }
}
