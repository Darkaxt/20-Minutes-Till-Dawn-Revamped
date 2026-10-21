using System;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.PowerupSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 const string GeneratorRoot="PF_FireRateBuffOnAmmoGain(Clone)";
 static IEnumerator GeneratorRoutine(FireRateBuffOnGainAmmo buff)=>(IEnumerator)AccessTools.Field(typeof(FireRateBuffOnGainAmmo),"_buffCoroutine").GetValue(buff);
 static void AmmoGain(PlayerController player){player.ammo.UseAmmo(1);player.ammo.GainAmmo(1);}
 static void GeneratorRate(PlayerController player,float expected,string criterion)
 {
  float value=Bonus(player.stats[StatType.FireRate]);Log("native-generator-rate",criterion+": "+value);
  Require(Mathf.Abs(value-expected)<.0002f,criterion);
 }
 IEnumerator CheckGenerator(bool baseline)
 {
  yield return AchievementRun("Shana","RevolverData","Endless_Forest");var game=Object.FindObjectOfType<GameController>();var player=PlayerController.Instance;
  Object.FindObjectOfType<HordeSpawner>().enabled=false;
  foreach(string perk in new[]{"ElectroMage","Magnetism","Recharge","Energized","Generator"})yield return AchievementPick(game,perk,game.playerXP.level);
  yield return null;
  var root=player.GetComponentsInChildren<Transform>().Single(t=>t.name==GeneratorRoot).gameObject;
  Log("native-generator-components",string.Join(",",root.GetComponents<Component>().Select(c=>c.GetType().FullName)));
  float before=Bonus(player.stats[StatType.FireRate]);
  var buff=root.GetComponent<FireRateBuffOnGainAmmo>();
  if(baseline)
  {
   Require(buff==null&&root.GetComponents<Component>().Length==1&&root.transform.childCount==0,"actual equipped Generator creates an empty source prefab");
   AmmoGain(player);yield return null;GeneratorRate(player,before,"original Generator has no ammo-gain bonus");
   // Separate diagnostic: exercise the existing compiled native helper with
   // the recovered source values, before production attaches/corrects it.
   buff=root.AddComponent<FireRateBuffOnGainAmmo>();Plugin.SetField(buff,"duration",2f);Plugin.SetField(buff,"buffAmount",.35f);yield return null;
  }
  else Require(buff!=null,"equipped Generator carries the restored native ammo-gain helper");
  AmmoGain(player);GeneratorRate(player,before+.35f,"first native ammo gain applies source +35% fire rate");
  while(GeneratorRoutine(buff)!=null)yield return null;
  GeneratorRate(player,before,"native two-second expiry removes the bonus");
  AmmoGain(player);yield return null;
  if(baseline)GeneratorRate(player,before,"uncorrected native helper's expired timer prevents a subsequent buff");
  else
  {
   GeneratorRate(player,before+.35f,"subsequent native ammo gain restarts the full buff");
   float started=Time.time;while(Time.time-started<1f)yield return null;
   AmmoGain(player);GeneratorRate(player,before+.35f,"another ammo gain refreshes without adding another bonus");
   while(Time.time-started<2.1f)yield return null;
   GeneratorRate(player,before+.35f,"refreshed native buff survives its earlier expiry boundary");
   while(GeneratorRoutine(buff)!=null)yield return null;GeneratorRate(player,before,"refreshed buff expires and restores the exact original bonus");
   yield return AchievementPick(game,"Generator",game.playerXP.level);yield return AchievementPick(game,"Generator",game.playerXP.level);yield return null;
   Require(player.GetComponentsInChildren<FireRateBuffOnGainAmmo>().Length==3,"three finite Endless Generator picks create three native independent helpers");
   AmmoGain(player);GeneratorRate(player,before+1.05f,"native repeated Generator helpers each contribute their source bonus");
   Require(!FallbackPool(game.powerupGenerator).Any(p=>p.powerup.name=="Generator"),"third native Generator pick consumes its finite pool allowance");
  }
  yield return Capture("generator-ammo-gain.png");yield return AchievementExit(game,false);
  yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;
  Require(player.GetComponentsInChildren<FireRateBuffOnGainAmmo>().Length==0,"native scene exit leaves no Generator helper in the original restart");
  GeneratorRate(player,0,"original restarted run has no carried Generator bonus");yield return CheckRevolver(game,player,"generator-original-revolver.png");yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete",baseline?"Generator empty-prefab defect and native helper restart defect reproduced through actual native choices/ammo events":"Generator native ammo gain/refresh/expiry/restart, finite independent Endless repeats and active-buff scene cleanup pass");Application.Quit(0);
 }
}
