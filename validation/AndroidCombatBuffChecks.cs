using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.PowerupSystem;
using flanne.PowerupSystems;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static readonly Dictionary<int,float> CombatWindFirstTimers=new Dictionary<int,float>();
 internal static bool CombatRefundControlled,CombatRefundSuccess;
 internal static int CombatRefundCalls;
 static int CombatCount(object owner,string field="_stacks")=>(int)WindField(owner,field);
 static void CombatClose(float actual,float expected,string contract)
 {Require(Mathf.Abs(actual-expected)<.002f,contract+" (actual="+actual+", expected="+expected+")");}
 static ResumeRuntime.SourceValue[] CombatValues(string key)=>ResumeRuntime.Capture(PlayerController.Instance).sourcePowerups.Single(s=>s.key==key).values.ToArray();
 IEnumerator CombatXP()
 {
  var xp=ObjectPooler.SharedInstance.GetPooledObject("SmallXP");xp.transform.position=PlayerController.Instance.transform.position+Vector3.right*30;xp.SetActive(true);
  AccessTools.Method(typeof(AndroidPickup),"TargetPick").Invoke(xp.GetComponent<AndroidPickup>(),new object[]{PlayerController.Instance.gameObject});
  while(xp.activeSelf)yield return null;
 }
 IEnumerator CombatCurseBatch(int count)
 {
  var victims=new List<Health>();
  for(int i=0;i<count;i++){var victim=HitEffectTarget(maximum:1,tag:"Untagged");victims.Add(victim);CurseSystem.Instance.Curse(victim.gameObject);}
  Time.timeScale=10;while(victims.Any(h=>!h.isDead))yield return null;Time.timeScale=1;
 }
 static void CombatShotEvent(bool successfulRoll)
 {
  CombatRefundSuccess=successfulRoll;CombatRefundControlled=true;int before=CombatRefundCalls;
  try{PlayerController.Instance.gun.OnShoot.Invoke();}finally{CombatRefundControlled=false;}
  Require(CombatRefundCalls==before+2,"one actual native gun shot event reaches both acquired Siege listeners");
 }
 IEnumerator CombatBuffContinue()
 {
  var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;
  Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
  GameController game;
  while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))
  {var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));if(error!=null)throw new InvalidOperationException(error.text);yield return null;}
 }
 IEnumerator CheckAndroidCombatBuffs(bool continuation)
 {
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("-siege-")){yield return CombatSiegeDiagnostic();yield break;}
  GameController game;PlayerController p;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;}
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();p.SetPosition(new Vector3(100,100,0));
   foreach(string name in new[]{"FreshClip","FreshClip","KillClip","KillClip","Seige","Seige","Excitement","Excitement","Ritual","Ritual","InTheWind","InTheWind"})yield return AndroidPick(game,name,game.playerXP.level);
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();p.SetPosition(new Vector3(100,100,0));
  var fresh=p.GetComponentsInChildren<DamageUpOnReload>();var kills=p.GetComponentsInChildren<ReloadRateUpOnKill>();var siege=p.GetComponentsInChildren<RefundAmmoWhenStanding>();var xp=p.GetComponentsInChildren<BuffOnXP>();
  var ritual=p.GetComponentsInChildren<DamageBuffOnCurseKill>();var wind=p.GetComponentsInChildren<DmgAndMSOnNotHurt>();
  yield return null;
  Require(new[]{fresh.Length,kills.Length,siege.Length,xp.Length,ritual.Length,wind.Length}.All(n=>n==2),"native repeated acquisitions attach two independent owners for each combat buff");
  Require(fresh.All(h=>(float)WindField(h,"duration")==1&&(float)WindField(h,"damageBonus")==.3f)&&siege.All(h=>(float)WindField(h,"chanceToRefund")==.4f)&&xp.All(h=>(float)WindField(h,"duration")==1),"recovered original Fresh Clip, Siege and Excitement parameters reach actual native helpers");
  Log("fixture-precondition","Real native level-up acquisitions, pooled Curse deaths and XP pickups; accelerated Unity time with stopped encounter/game clock. Explicit saved Wind residual timers and controlled native shot-event rolls test source boundaries, not naturally played encounters or refund frequencies.");
  float[] baseline;
  if(continuation)
  {
   baseline=File.ReadAllLines(Path.Combine(Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR"),"combat-baseline.txt")).Select(v=>float.Parse(v,CultureInfo.InvariantCulture)).ToArray();
   while(wind.Any(h=>!CombatWindFirstTimers.ContainsKey(h.GetInstanceID())))yield return null;
   foreach(var h in wind)h.enabled=false;
   Require(wind.Select(h=>CombatWindFirstTimers[h.GetInstanceID()]).SequenceEqual(new[]{3.25f,6.5f}),"source Continue restores both independent Wind timers before their first Update");
   Require(wind.Select(h=>CombatCount(h,"_ticks")).SequenceEqual(new[]{4,4}),"source Continue clamps the saved fifth Wind tick to four without changing saved accumulated amounts");
   Require(ritual.All(h=>CombatCount(h,"_killCounter")==0)&&kills.All(h=>CombatCount(h)==0),"source Continue omits partial Ritual kills and all Kill Clip stacks");
   CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0]+.94f,"source Continue keeps Wind0.9 and Ritual0.04 but omits active Fresh Clip phases");
   CombatClose(Bonus(p.stats[StatType.MoveSpeed]),baseline[3]+.9f,"source Continue restores exact accumulated Wind movement, independent of clamped ticks");
   CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1],"source Continue omits active Excitement phase");CombatClose(Bonus(p.stats[StatType.ReloadRate]),baseline[2],"source Continue omits unsaved Kill Clip reload contributions");
   Require(fresh.All(h=>(float)WindField(h,"_timer")<=0)&&xp.All(h=>(float)WindField(h,"_timer")<=0),"continued temporary helper clocks start fresh");
   if(p.playerHealth.isInvincible.value)p.playerHealth.isInvincible.UnFlip();
   AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(p.playerHealth,new object[]{1});
   Require(wind.All(h=>CombatCount(h,"_ticks")==0&&(float)WindField(h,"_timer")==0),"actual source red-health hurt clears each Wind counter, timer and accumulated float");
   CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0]+.04f,"Wind hurt removes recorded0.9 rather than clamped ticks0.8");
   CombatClose(Bonus(p.stats[StatType.MoveSpeed]),baseline[3],"continued hurt reverses all recorded Wind movement");
   yield return CombatXP();CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1]+1,"continued real XP pickup activates both fresh Excitement owners");
   while(xp.Any(h=>(float)WindField(h,"_timer")>0))yield return null;CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1],"continued Excitement expires without restored phase leftovers");
   yield return CombatCurseBatch(7);Require(ritual.All(h=>CombatCount(h,"_killCounter")==7),"seven actual continued Curse deaths do not inherit the omitted old three-kill partial progress");CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0]+.04f,"continued Ritual does not grant early from omitted partial history");
   yield return CombatCurseBatch(3);Require(ritual.All(h=>CombatCount(h,"_killCounter")==0)&&kills.All(h=>CombatCount(h)==10),"ten real continued Curse deaths complete both new Ritual thresholds and reach both Kill Clip listeners");CombatClose(Bonus(p.stats[StatType.ReloadRate]),baseline[2]+1,"continued untagged deaths add both Kill Clip reload contributions");
   CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0]+.06f,"new source Ritual grants accumulate above both restored floats");
   p.ammo.Reload();Require(kills.All(h=>CombatCount(h)==0),"continued actual reload clears both Kill Clip owners");CombatClose(Bonus(p.stats[StatType.ReloadRate]),baseline[2],"continued reload reverses the newly accrued Kill Clip contributions");CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0]+.66f,"continued actual reload activates both Fresh Clip owners once");
   while(fresh.Any(h=>(float)WindField(h,"_timer")>0))yield return null;CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0]+.06f,"continued Fresh Clip expires to the updated persistent Ritual total");
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
   Require(p.GetComponentInChildren<DamageUpOnReload>()==null&&p.GetComponentInChildren<ReloadRateUpOnKill>()==null&&p.GetComponentInChildren<DmgAndMSOnNotHurt>()==null&&p.GetComponentInChildren<DamageBuffOnCurseKill>()==null,"ordinary restart clears all source combat-buff owners");
   CombatClose(Bonus(p.stats[StatType.BulletDamage]),0,"ordinary restart clears all accumulated and temporary source damage");yield return CheckRevolver(game,p,"android-combat-buff-original-restart.png");yield return AchievementExit(game,false);
   SetPhase("PASS");Log("complete","Source combat-buff current Continue, independent Wind floats/timers/clamping, omitted temporary/kill phases, real post-Continue producers and ordinary restart");Application.Quit(0);yield break;
  }
  // Keep natural Wind growth out of unrelated setup. Resume it for its real40-second workflow below.
  foreach(var h in wind){h.enabled=false;Plugin.SetField(h,"_timer",0f);}
  while(fresh.Any(h=>(float)WindField(h,"_timer")>0))yield return null;
  baseline=new[]{Bonus(p.stats[StatType.BulletDamage]),Bonus(p.stats[StatType.FireRate]),Bonus(p.stats[StatType.ReloadRate]),Bonus(p.stats[StatType.MoveSpeed])};
  p.ammo.Reload();float first=Time.time;CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0]+.6f,"one native reload activates both independent30-percent Fresh Clips");
  while(Time.time-first<.6f)yield return null;p.ammo.Reload();float refreshed=Time.time;
  while(Time.time-first<1.1f)yield return null;CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0]+.6f,"each Fresh Clip refresh survives its first deadline without overlap");
  while(fresh.Any(h=>(float)WindField(h,"_timer")>0))yield return null;Require(Time.time-refreshed>=.99f,"actual Fresh Clip final duration is one second");CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0],"both refreshed Fresh Clips expire once");
  yield return CombatXP();CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1]+1,"actual pooled XP pickup activates two50-percent Excitement owners");
  first=Time.time;while(Time.time-first<.5f)yield return null;yield return CombatXP();refreshed=Time.time;
  while(Time.time-first<1.1f)yield return null;CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1]+1,"second real XP pickup refreshes each Excitement past its old deadline");
  while(xp.Any(h=>(float)WindField(h,"_timer")>0))yield return null;Require(Time.time-refreshed<1.2f,"source Excitement expires at one second rather than native two");CombatClose(Bonus(p.stats[StatType.FireRate]),baseline[1],"both Excitement owners remove one contribution at expiry");
  p.ammo.Reload();yield return CombatCurseBatch(23);
  Require(kills.All(h=>CombatCount(h)==23)&&ritual.All(h=>CombatCount(h,"_killCounter")==3),"23 actual source-pooled untagged Curse deaths reach both Kill Clips and both Ritual helpers");
  CombatClose(Bonus(p.stats[StatType.ReloadRate]),baseline[2]+2.3f,"two Kill Clips each grant5 percent on every untagged death");
  Require(CombatValues("ritual_name").All(v=>Mathf.Abs(v.a-.02f)<.001f),"actual Ritual notifications retain two independent1-percent grants and omit the partial three kills");
  p.ammo.Reload();Require(kills.All(h=>CombatCount(h)==0),"actual reload clears both Kill Clip counters");CombatClose(Bonus(p.stats[StatType.ReloadRate]),baseline[2],"actual reload reverses exactly all Kill Clip contributions");
  while(fresh.Any(h=>(float)WindField(h,"_timer")>0))yield return null;
  yield return null;int ammo=p.ammo.amount;p.ammo.UseAmmo(2);ammo=p.ammo.amount;
  Require(p.gun.OnShoot.GetPersistentTarget(0)==p.ammo&&p.gun.OnShoot.GetPersistentMethodName(0)=="UseAmmo","actual original native shot event consumes one ammo before source refund listeners, matching both owned asset bindings");
  CombatShotEvent(true);
  Require(p.ammo.amount==ammo+1,"actual shot consumes one ammo then both standing Siege consumers refund on controlled successful original40-percent rolls");
  p.ammo.UseAmmo(2);ammo=p.ammo.amount;
  CombatShotEvent(false);
  Require(p.ammo.amount==ammo-1,"standing Siege excludes rolls at/above its original threshold while the shot still consumes one ammo");
  ammo=p.ammo.amount;
  p.SetPosition(p.transform.position+Vector3.right);foreach(var owner in siege)AccessTools.Method(typeof(RefundAmmoWhenStanding),"Update").Invoke(owner,null);
  CombatShotEvent(true);
  Require(p.ammo.amount==ammo-1,"moving Siege skips refund even with a controlled success roll while the shot still consumes one ammo");
  foreach(var h in wind)h.enabled=true;Time.timeScale=40;while(wind.Any(h=>CombatCount(h,"_ticks")<4))yield return null;Time.timeScale=1;foreach(var h in wind)h.enabled=false;
  CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0]+.84f,"two real four-tick Wind helpers each add40 percent beside Ritual4 percent");CombatClose(Bonus(p.stats[StatType.MoveSpeed]),baseline[3]+.8f,"actual Wind40-second growth reaches both movement caps");
  Plugin.SetField(wind[0],"_timer",10f);AccessTools.Method(typeof(DmgAndMSOnNotHurt),"Update").Invoke(wind[0],null);
  Require(CombatCount(wind[0],"_ticks")==5,"source still consumes a retained full timer after reaching its tick limit");
  Plugin.SetField(wind[0],"_timer",3.25f);Plugin.SetField(wind[1],"_timer",6.5f);
  CombatClose(Bonus(p.stats[StatType.BulletDamage]),baseline[0]+.94f,"controlled source residual Wind tick preserves its extra recorded contribution");
  // Keep source temporary/kill state active in the checkpoint to prove its omission.
  HitEffectTarget(maximum:1,tag:"Untagged").TakeDamage(DamageType.Bullet,100);p.ammo.Reload();HitEffectTarget(maximum:1,tag:"Untagged").TakeDamage(DamageType.Bullet,100);
  yield return CombatXP();
  File.WriteAllLines(Path.Combine(ResultDir,"combat-baseline.txt"),baseline.Select(v=>v.ToString("R",CultureInfo.InvariantCulture)));
  var capture=ResumeRuntime.Capture(p);Require(capture.stats.Count==0&&capture.timed.Count==0&&capture.providers.Count==0&&CombatValues("in_the_wind_name").Select(v=>v.count).SequenceEqual(new[]{5,4}),"source checkpoint records Wind floats/counts and Ritual accumulators without raw stats or unsaved generic phases");
  yield return DragonSuspend(game,"Repeated Fresh Clip/Excitement source refresh and expiry, untagged Curse Ritual/Kill Clip consumers, controlled Siege and independent Wind source checkpoint");
 }
 IEnumerator CombatSiegeDiagnostic()
 {
  yield return CombatBuffContinue();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));yield return null;yield return null;
  var owners=p.GetComponentsInChildren<RefundAmmoWhenStanding>();p.ammo.Reload();p.ammo.UseAmmo(2);
  Log("siege-diagnostic","before="+p.ammo.amount+" max="+p.ammo.max+" owners="+owners.Length);
  foreach(var owner in owners)Log("siege-owner-diagnostic","last="+WindField(owner,"_lastFramePos")+" current="+WindField(owner,"_thisFramePos")+" parent="+owner.transform.position+" sameAmmo="+ReferenceEquals(WindField(owner,"ammo"),p.ammo));
  CombatShotEvent(true);Log("siege-diagnostic","after="+p.ammo.amount+" calls="+CombatRefundCalls);
  SetPhase("PASS");Log("complete","Bounded Siege listener/roll/ammo/position diagnostic only; no full-workflow acceptance");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(DmgAndMSOnNotHurt),"Update")]
class CombatBuffWindFirstFrameProbe
{
 static void Prefix(DmgAndMSOnNotHurt __instance,float ____timer)
 {if(!RuntimeChecks.CombatWindFirstTimers.ContainsKey(__instance.GetInstanceID()))RuntimeChecks.CombatWindFirstTimers.Add(__instance.GetInstanceID(),____timer);}
}
[HarmonyPatch(typeof(RefundAmmoWhenStanding),"ChangeToRefundAmmo")]
class CombatBuffRefundRollProbe
{
 static void Prefix()
 {
  if(!RuntimeChecks.CombatRefundControlled)return;RuntimeChecks.CombatRefundCalls++;
  RuntimeChecks.SeedFirstRoll(RuntimeChecks.CombatRefundSuccess?0:.4f,RuntimeChecks.CombatRefundSuccess?.4f:1);
  var state=UnityEngine.Random.state;float roll=UnityEngine.Random.Range(0f,1f);UnityEngine.Random.state=state;RuntimeChecks.Log("siege-roll-diagnostic",roll);
 }
}
