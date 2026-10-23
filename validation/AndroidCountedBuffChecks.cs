using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.PowerupSystem;
using flanne.PowerupSystems;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckAndroidCountedBuff(bool continuation)
 {
  GameController game;PlayerController p;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;}
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.State.earned.Add("character:Abby");Progression.Refresh();
   yield return AchievementRun("Abby","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();
   foreach(string name in new[]{"Vitality","AngerPoint"})yield return AndroidPick(game,name,game.playerXP.level);
   yield return AndroidWhirlwindChest(game);
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();p.SetPosition(new Vector3(100,100,0));yield return null;
  var buff=p.GetComponentsInChildren<TemporaryRefreshingBuff>().Single(b=>b.name.Replace("(Clone)","")=="PF_BuffOnReload");
  var anger=p.GetComponentInChildren<FireRateOnHurt>();Require(anger!=null,"native Anger Point attaches its source hurt helper beside Abby's actual chest upgrade");
  float[] baseline=continuation?File.ReadAllLines(Path.Combine(Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR"),"counted-baseline.txt")).Select(v=>float.Parse(v,CultureInfo.InvariantCulture)).ToArray():new[]{Bonus(p.stats[StatType.BulletDamage]),Bonus(p.stats[StatType.FireRate]),Bonus(p.stats[StatType.MoveSpeed])};
  Log("fixture-precondition","Real native Abby/chest/reload/hurt/Continue workflow. Encounter/game clock stopped; accelerated Unity time for the source30-second Anger Point expiry. Counts are observed through native helpers/source saves, not imposed.");
  if(continuation)
  {
   Require(CombatCount(buff)==2&&WindField(buff,"buffCoroutine")==null,"Continue replays two counted Whirlwind applications without restoring its expired coroutine");
   CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1]+.7f,"source replay retains two35-percent Whirlwind counts even though the saved temporary buff had expired");CombatClose(Bonus(p.stats[StatType.MoveSpeed]),baseline[2]+.7f,"source count replay also retains movement contributions");
   CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0],"source Continue omits the active Anger Point damage phase");
   float started=Time.time;while(Time.time-started<2.1f)yield return null;CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1]+.7f,"restored Whirlwind contributions do not acquire an invented expiry");
   p.ammo.Reload();p.ammo.Reload();Require(CombatCount(buff)==3,"continued reload starts one fresh Whirlwind application and the next reload only refreshes");
   CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1]+1.05f,"new Whirlwind phase adds one contribution above source replayed counts");
   while(WindField(buff,"buffCoroutine")!=null)yield return null;
   CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1]+.7f,"continued Whirlwind expiry removes only the newly active contribution");CombatClose(Bonus(p.stats[StatType.MoveSpeed]),baseline[2]+.7f,"continued expiry preserves source replayed movement leftovers");
   Require(CombatValues("abby_perk_whirlwind_name").Single().count==3,"next source capture retains all three applications after expiry");
  }
  else
  {
   Require(CombatCount(buff)==0,"fresh inherited Whirlwind starts without fabricated saved applications");
   for(int cycle=1;cycle<=2;cycle++)
   {
    p.ammo.Reload();p.ammo.Reload();Require(CombatCount(buff)==cycle,"actual repeated reload refresh does not add another active Whirlwind count in cycle"+cycle);
    CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1]+.35f,"actual Whirlwind cycle adds35-percent Fire Rate");CombatClose(Bonus(p.stats[StatType.MoveSpeed]),baseline[2]+.35f,"actual Whirlwind cycle adds35-percent movement");
    while(WindField(buff,"buffCoroutine")!=null)yield return null;
    Require(CombatCount(buff)==cycle,"source Whirlwind expiry leaves its application counter unchanged");CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1],"fresh Whirlwind expiry reverses its actual Fire Rate contribution");CombatClose(Bonus(p.stats[StatType.MoveSpeed]),baseline[2],"fresh Whirlwind expiry reverses its actual movement contribution");
   }
  }
  if(p.playerHealth.isInvincible.value)p.playerHealth.isInvincible.UnFlip();
  p.playerHealth.Heal(p.playerHealth.maxHP);
  int hp=p.playerHealth.hp;float reload=Bonus(p.stats[StatType.ReloadRate]);
  AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(p.playerHealth,new object[]{1});AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(p.playerHealth,new object[]{1});
  Require(p.playerHealth.hp==hp-2,"both controlled actual red-health hits reach source Anger Point without contact invulnerability");
  CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0]+.7f,"repeated native hurt refreshes one70-percent Anger Point damage bonus");CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1]+.7f+(continuation?.7f:0),"Anger Point refreshes Fire Rate without overlapping its own bonuses");CombatClose(Bonus(p.stats[StatType.ReloadRate]),reload,"source Anger Point does not grant native reload speed");
  if(!continuation)
  {
   File.WriteAllLines(Path.Combine(ResultDir,"counted-baseline.txt"),baseline.Select(v=>v.ToString("R",CultureInfo.InvariantCulture)));
   var capture=ResumeRuntime.Capture(p);Require(capture.timed.Count==0&&capture.providers.Count==0&&CombatValues("abby_perk_whirlwind_name").Single().count==2,"source checkpoint captures the positive inherited count but neither expired Whirlwind nor active Anger Point phase");
   yield return DragonSuspend(game,"Native Abby chest, inherited Whirlwind counted refresh/expiry and active Anger Point source checkpoint");yield break;
  }
  float began=Time.time;Time.timeScale=40;while(Time.time-began<31)yield return null;Time.timeScale=1;
  CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0],"actual source Anger Point damage expires after30 Unity seconds");CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1]+.7f,"Anger Point expiry preserves the source count-replayed Whirlwind contributions");
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  Require(p.GetComponentInChildren<TemporaryRefreshingBuff>()==null&&p.GetComponentInChildren<FireRateOnHurt>()==null,"ordinary restart clears both source refreshing helper families");CombatClose(Bonus(p.stats[StatType.FireRate]),0,"ordinary restart clears source counted replay leftovers");
  yield return CheckRevolver(game,p,"android-counted-buff-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Inherited source buff count replay/unsaved coroutine mismatch, real resumed refresh/expiry, Anger Point30-second refresh/expiry and ordinary restart");Application.Quit(0);
 }
}
