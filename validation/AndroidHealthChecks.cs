using System.Collections;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckAndroidHealth()
 {
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest");
  var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;var health=p.playerHealth;
  Object.FindObjectOfType<HordeSpawner>().enabled=false;
  p.stats[StatType.MaxHP].AddMultiplierBonus(.1f);
  Require(health.maxHP==4,"Android health stat floors fractional hearts");
  p.stats[StatType.MaxHP].AddMultiplierBonus(-.1f);health.shp=3;
  p.stats[StatType.MaxHP].AddFlatBonus(30);
  Require(health.maxHP==34&&health.hp==34&&health.shp==3,"Android red health grows beyond twenty without deleting soul hearts");
  health.shp=4;Require(health.shp==3,"Android ignores a soul-heart assignment beyond its independent capacity");
  AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(health,new object[]{1});
  Require(health.hp==34&&health.shp==2,"native damage consumes independent soul health before red health");
  p.stats[StatType.MaxHP].AddFlatBonus(-30);p.stats[StatType.MaxHP].AddFlatBonus(-30);
  Require(health.maxHP==1&&health.hp==1,"Android health stat keeps at least one maximum heart");
  p.stats[StatType.MaxHP].AddFlatBonus(30);
  p.stats[StatType.VisionRange].AddMultiplierBonus(3);
  Require(Mathf.Abs(GameObject.FindGameObjectWithTag("PlayerVision").transform.localScale.x-4)<.001f,"Android vision transform follows its stat beyond the old two-and-a-half cap");
  yield return Capture("android-health-vision.png");yield return AchievementExit(game,false);
  yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  Require(p.playerHealth.hp==4&&p.playerHealth.maxHP==4&&p.playerHealth.shp==0,"native restart recreates base health without old modifiers or souls");
  yield return CheckRevolver(game,p,"android-health-original-restart.png");yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete","Android health rounding/minimum, independent red/soul capacity, damage, vision scaling and native restart");Application.Quit(0);
 }
}
