using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
using flanne.Pickups;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static readonly List<int> GrowthLevelCallbacks=new List<int>();
 internal static int GrowthProducedHearts;
 internal static void ObserveGrowthHeart(HashSet<int> before)
 {
  var added=Object.FindObjectsOfType<HPPickup>().Where(h=>!before.Contains(h.GetInstanceID())).ToArray();
  Require(added.Length==1,"actual completed Growth coroutine creates one new active native heart pickup");
  Require(Vector2.Distance(added[0].transform.position,PlayerController.Instance.transform.position)<.001f,"Growth heart uses its actual owned-player position");
  GrowthProducedHearts++;Log("growth-heart-produced","count="+GrowthProducedHearts+" position="+added[0].transform.position);
 }
 IEnumerator CheckAndroidGrowthRune(bool continuation)
 {
  bool repeated=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("-repeated-");GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();
   yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>
   {
    Loadout.RuneSelection=new[]{"GrowthRuneData","SeismicRuneData"}.Select(name=>
    {var copy=Object.Instantiate(Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.name==name));copy.name=name;copy.level=3;return copy;}).ToList();
   });
   game=Object.FindObjectOfType<GameController>();
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));
  var growth=p.GetComponentInChildren<GrowthRune>();var wards=p.GetComponentsInChildren<SetGameObjectActiveSummon>().Where(s=>s.SummonTypeID=="SeismicWard").ToArray();
  Require(growth!=null&&wards.Length==2,"actual selected Growth/Seismic rank-three loadout creates its native rune and two ward actors");while(wards.Any(w=>!SummonStarted(w)))yield return null;
  CombatClose(Bonus(game.playerXP.xpMultiplier),.12f,"source rank-three Growth is rebuilt exactly once at12-percent XP");
  Require(wards.All(w=>StartedSummonTimers[w.GetInstanceID()]==0&&Mathf.Abs((float)AccessTools.Property(typeof(AttackingSummon),"finalAttackCooldown").GetValue(w)-7)<.001f),"fresh/restored wards start at source zero phase and seven-second rank-three cooldown");
  Log("fixture-precondition","Two explicitly selected rank-three native rune copies; no purchase claim. Real XP level-up choices/source Continue callbacks and actual delayed heart pickup creation. Source encounter/game clock stopped; accelerated Unity time only for continued ward pulses. No manual Growth event, counter or heart grant.");
  if(!continuation)
  {
   Require(GrowthProducedHearts==0&&game.playerXP.level==1,"new ordinary source run has no initial Growth heart award");
   foreach(string name in new[]{"RapidFire","PowerShot","QuickHands","LightBullets"})yield return AndroidPick(game,name,game.playerXP.level);
   while(GrowthProducedHearts==0)yield return null;
   Require(game.playerXP.level==5&&GrowthProducedHearts==1&&GrowthLevelCallbacks.Count(l=>l==5)==1,"four actual native level-up choices reach five and issue one original Growth callback/heart award");
   foreach(var ward in wards){ward.enabled=false;Plugin.SetField(ward,"_timer",6.25f);}
   var runtime=ResumeRuntime.Capture(p);Require(runtime.stats.Count==0&&runtime.timed.Count==0&&runtime.providers.Count==0&&runtime.summons.Count==0&&runtime.sourceHealing==null&&runtime.sourceBodySoul==null,"source checkpoint omits Growth earned-level guards and generic ward/rune phases");
   yield return DragonSuspend(game,"Actual rank-three Growth XP/level-five heart award with partial Seismic clocks and source checkpoint");yield break;
  }
  Require(game.playerXP.level==5&&GrowthProducedHearts==0&&GrowthLevelCallbacks.Count(l=>l==5)==1,"Continue republishes saved level five without awarding a duplicate Growth heart");
  var input=RunResume.Read();Require(input.level==5&&input.runes.Count==2&&input.runes.All(r=>r.rank==3),"actual checkpoint retains level five and both selected rank-three rune identities");
  var pulse=(GameObject)WindField(wards[0],"attackObj");var times=new List<float>();bool previous=pulse.activeSelf;float largestDelta=0;Time.timeScale=4;
  while(times.Count<2)
  {
   largestDelta=Mathf.Max(largestDelta,Time.deltaTime);bool active=pulse.activeSelf;
   if(active&&!previous){times.Add(Time.time);Log("growth-continued-ward-pulse",Time.time);}previous=active;yield return null;
  }
  Time.timeScale=1;Require(Mathf.Abs(times[1]-times[0]-7)<=largestDelta+.01f,"actual continued ward pulse spacing follows its rebuilt seven-second source cooldown within measured frame quantization");
  Require(GrowthProducedHearts==0,"real continued ward pulses and elapsed frames do not create a delayed restore-only Growth heart");
  if(!repeated){yield return DragonSuspend(game,"Corrected level-five Growth restore without award, pristine ward clock/actual pulse spacing and same-level resave for repeated Continue");yield break;}
  Require(GrowthLevelCallbacks.All(l=>l==1||l==5),"repeated Continue did not fabricate newly earned levels");
  foreach(string name in new[]{"Vitality","Haste","FrostMage","ElectroMage","TakeAim"})yield return AndroidPick(game,name,game.playerXP.level);
  while(GrowthProducedHearts==0)yield return null;Require(game.playerXP.level==10&&GrowthProducedHearts==1,"five genuinely earned new levels still produce the qualifying level-ten Growth heart once");
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  Require(p.GetComponentInChildren<GrowthRune>()==null&&p.GetComponentsInChildren<SetGameObjectActiveSummon>().All(s=>s.SummonTypeID!="SeismicWard"),"ordinary restart removes both rune families");CombatClose(Bonus(game.playerXP.xpMultiplier),0,"ordinary restart clears Growth XP contribution");
  yield return CheckRevolver(game,p,"android-growth-rune-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Repeated Growth Continue suppresses duplicate reward, earned level-ten reward remains, rebuilt XP/ward phases and native original restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(GrowthRune),"OnLevelChanged")]
class ObserveSourceGrowthLevel
{
 static void Prefix(int level)
 {if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-growth-rune-")){RuntimeChecks.GrowthLevelCallbacks.Add(level);RuntimeChecks.Log("growth-level-callback",level);}}
}
[HarmonyPatch]
class ObserveSourceGrowthProducedHeart
{
 static MethodBase TargetMethod()=>AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(GrowthRune),"WaitToDropHP"));
 static void Prefix(out HashSet<int> __state)
 {__state=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-growth-rune-")?new HashSet<int>(Object.FindObjectsOfType<HPPickup>().Select(h=>h.GetInstanceID())):null;}
 static void Postfix(bool __result,HashSet<int> __state)
 {if(__state!=null&&!__result)RuntimeChecks.ObserveGrowthHeart(__state);}
}
