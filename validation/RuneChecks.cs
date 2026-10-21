using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckRuneContracts(bool baseline)
 {
  foreach(int rank in new[]{1,5})
  {
   SetPhase("native-rune-loadout-rank-"+rank);
   var seismic=Resources.FindObjectsOfTypeAll<RuneData>().Single(r=>r.name=="SeismicRuneData");
   var growth=Resources.FindObjectsOfTypeAll<RuneData>().Single(r=>r.name=="GrowthRuneData");
   int seismicBefore=seismic.level,growthBefore=growth.level;
   yield return AchievementRun("Shana","RevolverData","20M_Forest",0,()=>
   {
    // Rank is an explicit isolated fixture precondition. Actual native
    // LoadoutInitializer -> RuneData.Apply -> Rune.Attach owns initialization.
    seismic.level=rank;growth.level=rank;Loadout.RuneSelection=new List<RuneData>{seismic,growth};
   });
   seismic.level=seismicBefore;growth.level=growthBefore;
   var game=Object.FindObjectOfType<GameController>();Object.FindObjectOfType<HordeSpawner>().enabled=false;
   var wards=PlayerController.Instance.GetComponentsInChildren<SetGameObjectActiveSummon>().Where(w=>w.SummonTypeID=="SeismicWard").ToArray();
   Require(wards.Length==2,"native selected rune initializes exactly two Seismic Wards");
   float cooldown=baseline?10+rank:10-rank;
   foreach(var ward in wards)
   {
    float actual=(float)AccessTools.Property(typeof(AttackingSummon),"finalAttackCooldown").GetValue(ward);
    Log("native-seismic-cooldown","rank="+rank+" cooldown="+actual);Require(Mathf.Abs(actual-cooldown)<.0001f,"Seismic Ward native cooldown at rank "+rank+" is "+cooldown);
   }
   var xp=game.playerXP;float bonus=Bonus(xp.xpMultiplier);Log("native-growth-bonus","rank="+rank+" bonus="+bonus);
   Require(Mathf.Abs(bonus-rank*(baseline?.02f:.04f))<.0001f,"native Growth XP bonus follows its per-rank recipe");
   var xpField=AccessTools.Field(xp.GetType(),"xp");float xpBefore=(float)xpField.GetValue(xp);xp.GainXP(1f);
   float gained=(float)xpField.GetValue(xp)-xpBefore;
   Require(Mathf.Abs(gained-(1f+bonus))<.0001f,"actual native XP gain consumes the corrected Growth modifier");
   Require((float)AccessTools.Property(xp.GetType(),"xpToLevel").GetValue(xp)==15f,"native starting XP level requirement remains unchanged");
   var pulse=(GameObject)AccessTools.Field(typeof(SetGameObjectActiveSummon),"attackObj").GetValue(wards[0]);
   var times=new List<float>();bool previous=pulse.activeSelf;
   float speedBefore=Time.timeScale;Time.timeScale=4f;
   while(times.Count<2)
   {
    bool active=pulse.activeSelf;if(active&&!previous){times.Add(Time.time);Log("native-seismic-pulse","rank="+rank+" time="+Time.time);}previous=active;yield return null;
   }
   Time.timeScale=speedBefore;
   Require(Mathf.Abs(times[1]-times[0]-cooldown)<.25f,"actual native Seismic Ward pulse interval follows its calculated cooldown");
   yield return Capture("native-runes-rank-"+rank+".png");yield return AchievementExit(game,false);
  }
  yield return AchievementRun("Shana","RevolverData","20M_Forest");var original=Object.FindObjectOfType<GameController>();
  Require(PlayerController.Instance.GetComponentsInChildren<SeismicWardRune>().Length==0&&Mathf.Abs(Bonus(original.playerXP.xpMultiplier))<.0001f,"original restart has no carried rune helpers or XP bonus");
  yield return CheckRevolver(original,PlayerController.Instance,"runes-original-restart.png");yield return AchievementExit(original,false);
  SetPhase("PASS");Log("complete",baseline?"Native rank1/rank5 loadouts reproduce reversed Seismic Ward cooldown and half-documented Growth XP bonus":"Native rank1/rank5 loadouts follow documented Seismic Ward and Growth recipes, actual ward pulses and original restart pass");Application.Quit(0);
 }
}
