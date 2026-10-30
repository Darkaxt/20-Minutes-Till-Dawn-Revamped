using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 static int PairRank(string character,string weapon)=>(int)AccessTools.Method(typeof(WeaponFamiliarity),"Crown",new[]{typeof(string),typeof(string)}).Invoke(null,new object[]{character,weapon});
 static void SeedPairRank(string character,string weapon,int rank)
 {
  var record=Progression.State.weaponFamiliarity.SingleOrDefault(r=>r.character==character&&r.weapon==weapon);
  if(record==null){record=new WeaponFamiliarity.Record{character=character,weapon=weapon};Progression.State.weaponFamiliarity.Add(record);}record.rank=rank;
 }
 IEnumerator PairRun(string character,string weapon,int rank,int darkness=0,string map="20M_Forest")
 {
  yield return AchievementRun(character,weapon,map,darkness,()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();FamiliarityContract(rank);
 }
 IEnumerator CheckCharacterWeaponFamiliarity(string mode)
 {
  if(mode.EndsWith("continue"))
  {
   var saved=RunResume.Read();Require(saved.character=="Shana"&&saved.weapon=="RevolverData"&&saved.weaponFamiliarity==8&&saved.characterWeaponFamiliarity==true,"exact new checkpoint identifies its scoped Shana/Revolver rank8");
   Require(Progression.State.weaponFamiliarityRecovered&&PairRank("Shana","RevolverData")==15&&PairRank("Diamond","RevolverData")==2&&PairRank("Shana","ShotgunData")==4,"fresh process reloads recovered and independently earned pair records");
   yield return CombatBuffContinue();var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();FamiliarityContract(8);RunResume.Save();Require(RunResume.Read().weaponFamiliarity==8&&RunResume.Read().characterWeaponFamiliarity==true,"new scoped Continue preserves one battle bonus despite later pair progress");
   yield return AchievementExit(game,false);yield return PairRun("Shana","RevolverData",15);game=Object.FindObjectOfType<GameController>();yield return AchievementExit(game,false);
   yield return PairRun("Diamond","RevolverData",2,15);game=Object.FindObjectOfType<GameController>();Require(Loadout.GunSelection.description.Contains("Familiarity +4%"),"native description switches to Diamond's independent handling bonus");RunResume.Save();var legacy=RunResume.Read();yield return AchievementExit(game,false);Require(PairRank("Diamond","RevolverData")==2,"losing at higher Darkness does not advance pair familiarity");
   // The old global writer used the same optional integer in schema7. A
   // controlled legacy-shaped checkpoint must remain bound to that run only.
   legacy.weaponFamiliarity=9;legacy.characterWeaponFamiliarity=null;legacy.valid=true;AccessTools.Method(typeof(RunResume),"Write").Invoke(null,new object[]{legacy});yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();FamiliarityContract(2);RunResume.Save();Require(RunResume.Read().weaponFamiliarity==2&&RunResume.Read().characterWeaponFamiliarity==true,"old global-rank Continue is corrected immediately and saved with the pair marker");
   Require(PairRank("Shana","RevolverData")==15&&PairRank("Diamond","RevolverData")==2,"legacy Continue does not grant its old global bonus to any pair record");yield return AchievementExit(game,false);
   legacy.weaponFamiliarity=null;legacy.valid=true;AccessTools.Method(typeof(RunResume),"Write").Invoke(null,new object[]{legacy});yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();FamiliarityContract(2);yield return AchievementExit(game,false);
   SetPhase("PASS");Log("complete","Persisted character/weapon isolation, changed-profile Continue and fresh next-character/new-run ranks verified.");Application.Quit(0);yield break;
  }
  RunResume.Invalidate();SaveSystem.data.difficultyUnlocked=15;
  foreach(var gate in Progression.Gates)if(!Progression.State.earned.Contains(gate.Id))Progression.State.earned.Add(gate.Id);
  Progression.State.pending.Clear();Progression.State.challengeDifficultyUnlocked=4;
  var guns=NativeSelection.GunItems(Progression.Title.gunMenu);int revolver=guns.FindIndex(g=>g.data.name=="RevolverData"),shotgun=guns.FindIndex(g=>g.data.name=="ShotgunData");
  for(int i=0;i<guns.Count;i++)SaveSystem.data.gunHighestClear[i]=i%15+1;
  SaveSystem.data.gunHighestClear[revolver]=5;SaveSystem.data.gunHighestClear[shotgun]=3;var legacyRanks=SaveSystem.data.gunHighestClear.ToArray();SaveSystem.Save();
  // Explicit old-journal shape and global crowns are QA fixtures. The real
  // journal loader, battle/native stat owners and victory writes are exercised.
  Progression.State.weaponFamiliarity=null;Progression.State.weaponFamiliarityRecovered=false;Progression.Save();Progression.Load(true);
  Require(Progression.State.weaponFamiliarityRecovered&&SaveSystem.data.gunHighestClear.SequenceEqual(legacyRanks),"legacy recovery preserves all global crowns and writes its marker");
  for(int i=0;i<guns.Count;i++)Require(PairRank("Shana",guns[i].data.name)==legacyRanks[i]&&PairRank("Diamond",guns[i].data.name)==0,"each legacy weapon rank is recovered only onto Shana: "+guns[i].data.name);
  SaveSystem.data.gunHighestClear[revolver]=SaveSystem.data.gunHighestClear[shotgun]=15;SaveSystem.Save();Progression.Load(true);
  Require(PairRank("Shana","RevolverData")==5&&PairRank("Shana","ShotgunData")==3,"later global crowns cannot rerun recovery or leak into pair records");
  yield return PairRun("Shana","RevolverData",5,8);var current=Object.FindObjectOfType<GameController>();yield return AchievementWin(current);
  Require(PairRank("Shana","RevolverData")==8&&PairRank("Diamond","RevolverData")==0&&PairRank("Shana","ShotgunData")==3,"actual Shana victory advances only her Revolver familiarity");
  yield return PairRun("Diamond","RevolverData",0,2);current=Object.FindObjectOfType<GameController>();yield return AchievementWin(current);
  Require(PairRank("Diamond","RevolverData")==2&&PairRank("Shana","RevolverData")==8,"second character earns a separate rank on the same weapon");
  yield return PairRun("Diamond","RevolverData",2,1);current=Object.FindObjectOfType<GameController>();yield return AchievementWin(current);
  Require(PairRank("Diamond","RevolverData")==2,"lower repeat victory does not stack or reduce familiarity");
  yield return PairRun("Shana","ShotgunData",3,4,"Challenge_Forest");current=Object.FindObjectOfType<GameController>();Loadout.difficultyLevel=4;ChallengeRun.Instance.Tier=4;
  Log("fixture-victory","Enter native Challenge victory state at controlled tier4; no natural quota/combat claim.");current.ChangeState<PlayerSurvivedState>();yield return AchievementExit(current,true);
  Require(PairRank("Shana","ShotgunData")==4&&PairRank("Shana","RevolverData")==8&&PairRank("Diamond","ShotgunData")==0,"Challenge records only the winning character/weapon pair");
  Require(SaveSystem.data.gunHighestClear[revolver]==15&&SaveSystem.data.gunHighestClear[shotgun]==15,"native legacy weapon crowns remain unchanged");
  var title=Progression.Title;Loadout.GunSelection=title.gunMenu[revolver];Loadout.GunIndex=revolver;Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>();Loadout.difficultyLevel=15;
  CustomMode.Prepare(new CustomMode.Rules{map="20M_Forest"},Resources.FindObjectsOfTypeAll<MapData>().First(m=>m.name=="20M_Forest"));title.ChangeState<WaitToLoadIntoBattleState>();
  while((current=Object.FindObjectOfType<GameController>())==null||!(current.CurrentState is CombatState))yield return null;
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();FamiliarityContract(8);Require(CustomMode.Active,"Custom startup uses the selected character/weapon pair");current.ChangeState<PlayerSurvivedState>();yield return AchievementExit(current,true);
  Require(PairRank("Shana","RevolverData")==8,"Custom victory cannot award higher familiarity");
  yield return PairRun("Shana","RevolverData",8);current=Object.FindObjectOfType<GameController>();
  Require(Loadout.GunSelection.description.Contains("Familiarity +16%"),"native description uses the selected Shana/Revolver pair despite global crown15");
  yield return FamiliarityHud("pair-familiarity-shana.png");RunResume.Save();Require(RunResume.Read().weaponFamiliarity==8&&RunResume.Read().characterWeaponFamiliarity==true,"new pair checkpoint captures its scoped battle rank");
  SeedPairRank("Shana","RevolverData",15);Progression.Save();FamiliarityContract(8);
  yield return DragonSuspend(current,"All legacy weapon ranks recovered only to Shana once; independent character/weapon victories, Challenge, Custom, description/HUD and scoped checkpoint rank8 with later profile rank15.");
 }
}
