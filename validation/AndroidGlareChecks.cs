using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization.Json;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.PerkSystem.Triggers;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool ForceGlareBurn;
 GameObject glareObserved;
 int glareImpacts,glareTypedHits;
 void GlareImpact(object sender,object args){if(args is GameObject target&&target==glareObserved)glareImpacts++;}
 void GlareTyped(object sender,object args){if(sender is Health health&&health.gameObject==glareObserved)glareTypedHits++;}
 static MonoBehaviour[] GlareHelpers()
 {
  var source=PlayerController.Instance.GetComponentsInChildren<MonoBehaviour>(true).Where(c=>c.GetType().Name=="AndroidGlare").ToArray();
  return source.Length>0?source:PlayerController.Instance.GetComponentsInChildren<flanne.PowerupSystem.AttachVisionDamage>().Where(c=>c.visionDamage!=null).Select(c=>(MonoBehaviour)c.visionDamage).ToArray();
 }
 static float GlareSeconds(MonoBehaviour helper)=>helper is PersistentHarm native?native.finalSecondsPerTick:(float)WindField(helper,"Seconds");
 static void GlareClock(MonoBehaviour helper,float value)=>Plugin.SetField(helper,helper is PersistentHarm?"_timer":"Timer",value);
 static void GlareHP(Health target,int hp=10000)=>Plugin.SetField(target,"<HP>k__BackingField",hp);
 IEnumerator CheckAndroidGlare(bool continuation)
 {
  GameController game;PlayerController p;
  if(continuation)
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;
   Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))
   {
    var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));
    if(error!=null)throw new InvalidOperationException(error.text);yield return null;
   }
   p=PlayerController.Instance;
   RunResume.Snapshot saved;using(var stream=File.OpenRead(Path.Combine(Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR"),"saved-run.json")))saved=(RunResume.Snapshot)new DataContractJsonSerializer(typeof(RunResume.Snapshot)).ReadObject(stream);
   var states=saved.runtime.providers.Where(s=>s.type=="TwentyMinutes.Modernization.AndroidGlare").OrderBy(s=>s.ordinal).ToArray();var restored=GlareHelpers();
   Require(states.Length==2&&restored.Length==2,"fresh-process Continue reconstructs both independently acquired Glare producers");
   for(int i=0;i<2;i++)Require(Mathf.Abs((float)WindField(restored[i],"Timer")-float.Parse(states[i].values.Single(v=>v.field=="Timer").value,CultureInfo.InvariantCulture))<.1f,"Continue preserves the individual Glare producer clock: "+i);
  }
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
   Object.FindObjectOfType<HordeSpawner>().enabled=false;
   foreach(string name in new[]{"Glare","IntenseGlare","Saccade","Saccade"})yield return AndroidPick(game,name,game.playerXP.level);
  }
  Object.FindObjectOfType<HordeSpawner>().enabled=false;p.playerHealth.isInvincible.Flip();
  var helpers=GlareHelpers();Require(helpers.Length==(continuation?2:1)&&helpers.All(h=>Mathf.Abs(GlareSeconds(h)-.5f)<.001f),"every repeated Saccade assigns a half second instead of accumulating Windows tick speed");
  this.AddObserver(GlareImpact,Projectile.ImpactEvent);this.AddObserver(GlareTyped,"Health.TweakGlareDamage");
  var pool=ObjectPooler.SharedInstance;var template=pool.itemDictionary.Values.First(i=>i.objectToPool.layer==(int)TagLayerUtil.Enemy&&i.objectToPool.GetComponent<Health>()!=null).objectToPool;pool.AddObject("Validation_Glare_Enemies",template,2);
  var insideObject=pool.GetPooledObject("Validation_Glare_Enemies");var outsideObject=pool.GetPooledObject("Validation_Glare_Enemies");float radius=p.stats[StatType.VisionRange].Modify(2);
  var inside=DurableEnemy(insideObject,p.transform.position+Vector3.right*(radius-.15f),false);insideObject.AddComponent<CircleCollider2D>().radius=.18f;insideObject.SetActive(true);glareObserved=insideObject;
  var outside=DurableEnemy(outsideObject,p.transform.position+Vector3.right*(radius+.3f),false);outsideObject.GetComponent<CircleCollider2D>().radius=.8f;outsideObject.SetActive(true);
  var unpooled=DurableEnemy(template,p.transform.position+Vector3.up*.5f,true);unpooled.gameObject.SetActive(true);
  foreach(var helper in helpers)GlareClock(helper,0);glareImpacts=glareTypedHits=0;float began=Time.time;
  Physics2D.SyncTransforms();while(inside.HP==10000)yield return null;
  Require(10000-inside.HP==(continuation?86:30)&&outside.HP==10000&&unpooled.HP==10000&&glareTypedHits==helpers.Length,"Glare applies ceil-composed typed damage once per pooled center, excluding overlapping outside colliders and unpooled actors");
  Require(Mathf.Abs(Time.time-began-.5f)<.08f&&glareImpacts==(continuation?2:0),"the actual source pulse uses its half-second clock and each producer's own Sight Magic flag");
  outsideObject.SetActive(false);unpooled.gameObject.SetActive(false);insideObject.SetActive(false);inside.transform.position=p.transform.position+Vector3.right;
  if(!continuation)
  {
   insideObject.SetActive(false);yield return AndroidPick(game,"IntenseGlare",game.playerXP.level);yield return AndroidPick(game,"SightMagic",game.playerXP.level);yield return AndroidPick(game,"Glare",game.playerXP.level);
   helpers=GlareHelpers();Require(helpers.Length==2,"repeated Glare attaches another independent source producer instead of a flat damage buff");
   insideObject.SetActive(true);GlareHP(inside);glareImpacts=0;GlareClock(helpers[0],0);GlareClock(helpers[1],.75f);
   while(inside.HP==10000)yield return null;
   Require(inside.HP==9985&&glareImpacts==0,"a fresh repeated Glare starts at fifteen damage and one second without inheriting old upgrades or hit triggers");
   while(inside.HP==9985)yield return null;
   Require(inside.HP==9940&&glareImpacts==1,"the older producer keeps its independent half-second clock, forty-five damage and Sight Magic callback");
   insideObject.SetActive(false);foreach(string name in new[]{"Saccade","SightMagic","IntenseGlare","PyroMage"})yield return AndroidPick(game,name,game.playerXP.level);
  }
  helpers=GlareHelpers();Require(helpers.All(h=>GlareSeconds(h)==.5f),"later Saccade upgrades both existing helpers without changing either beyond the source assignment");
  insideObject.SetActive(true);GlareHP(inside);foreach(var helper in helpers)GlareClock(helper,0);glareImpacts=glareTypedHits=0;
  ForceGlareBurn=true;try{while(inside.HP==10000)yield return null;}finally{ForceGlareBurn=false;}
  Require(10000-inside.HP==(continuation?86:90)&&glareTypedHits==2&&glareImpacts==2,"ordered repeated upgrades produce independent sixty/thirty damage Glare pulses before signed Might and one hit event per producer");
  Require(BurnSystem.SharedInstance.IsBurning(insideObject),"actual Sight Magic events reach the native Pyro Mage on-hit action and source burn system");insideObject.SetActive(false);
  if(!continuation)yield return NativeTomePick(game,false,"TomeOfMight");
  foreach(var helper in helpers)GlareClock(helper,0);insideObject.SetActive(true);GlareHP(inside);while(inside.HP==10000)yield return null;
  Require(inside.HP==9914,"signed Might modifies both source Glare producers separately to fifty-eight plus twenty-eight damage");insideObject.SetActive(false);
  this.RemoveObserver(GlareImpact,Projectile.ImpactEvent);this.RemoveObserver(GlareTyped,"Health.TweakGlareDamage");
  yield return Capture(continuation?"android-glare-continue.png":"android-glare-save.png");
  if(!continuation)
  {
   GlareClock(helpers[0],.11f);GlareClock(helpers[1],.31f);
   PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
   while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
   File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Android Glare native tree/repeats, source geometry, clock/damage ordering, real on-hit burn and checkpoint");Application.Quit(0);yield break;
  }
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  Require(GlareHelpers().Length==0,"normal restart has no retained Glare producer, modifiers or hit events");yield return CheckRevolver(game,p,"android-glare-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Android Glare independent clocks and ordered acquisitions survive fresh-process Continue and clear on original restart");Application.Quit(0);
 }
}
[HarmonyPatch]
class GlareBurnRollProbe
{
 static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(OnHitTrigger),"OnImpact"),AccessTools.Method(typeof(BurnOnHit),"OnImpact")};
 [HarmonyPriority(Priority.First)]
 static void Prefix(){if(RuntimeChecks.ForceGlareBurn)RuntimeChecks.SeedFirstRoll(0,.5f);}
}
