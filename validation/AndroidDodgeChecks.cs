using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 public static bool DodgeContactRoll;public static float DodgeContactMinimum,DodgeContactMaximum;public static int DodgeContacts,DodgePopups;
 static void DodgeSeed(float minimum,float maximum)
 {
  for(int seed=1;;seed++)
  {
   UnityEngine.Random.InitState(seed);var state=UnityEngine.Random.state;float roll=UnityEngine.Random.Range(0f,1f);
   if(roll<minimum||roll>=maximum)continue;UnityEngine.Random.state=state;Log("dodge-fixture-roll",roll);return;
  }
 }
 IEnumerator CheckAndroidDodge(bool continuation)
 {
  bool partial=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("-partial-");
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
   p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();var restored=p.GetComponents<AndroidReflex>();
   Require(restored.Length==1&&Mathf.Abs(restored[0].Granted-(partial?0:.1f))<.0001f,partial?"Continue rebuilds Reflex without its source-unsaved partial growth":"Continue rebuilds one Reflex owner; repeated recorded movement upgrades reach its source cap again");
   float previous=Bonus(p.stats[StatType.Dodge]);p.stats[StatType.MoveSpeed].AddMultiplierBonus(partial?.02f:.3f);
   Require(Mathf.Abs(Bonus(p.stats[StatType.Dodge])-previous-(partial?.02f:0))<.0001f,partial?"rebuilt uncapped Reflex reacts to new movement growth":"replayed capped Reflex does not add duplicate contributions");
   yield return Capture("android-dodge-continue.png");yield return AchievementExit(game,false);
   yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
   Require(p.GetComponent<AndroidReflex>()==null&&Bonus(p.stats[StatType.Dodge])==0,"native restart removes Reflex provider and accumulated dodge");
   yield return CheckRevolver(game,p,"android-dodge-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Source Reflex grouped acquisition replay, unsaved growth, new movement response and clean original restart");Application.Quit(0);yield break;
  }
  RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
  yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();
  yield return AndroidPick(game,"Nimble",game.playerXP.level);float baseline=Bonus(p.stats[StatType.Dodge]);
  yield return AndroidPick(game,"Reflex",game.playerXP.level);
  Require(Mathf.Abs(Bonus(p.stats[StatType.Dodge])-baseline)<.0001f,"Android Reflex excludes its own initial movement bonus from dodge growth");
  p.stats[StatType.MoveSpeed].AddMultiplierBonus(.04f);
  Require(Mathf.Abs(Bonus(p.stats[StatType.Dodge])-baseline-.04f)<.0001f,"Android Reflex adds subsequent movement factor growth directly to dodge");
  if(partial){yield return SaveAndroidDodge(game);yield break;}
  p.stats[StatType.MoveSpeed].AddMultiplierBonus(-.02f);
  Require(Mathf.Abs(Bonus(p.stats[StatType.Dodge])-baseline-.02f)<.0001f,"Android Reflex tracks movement loss before its cap");
  p.stats[StatType.MoveSpeed].AddMultiplierBonus(.1f);p.stats[StatType.MoveSpeed].AddMultiplierBonus(-.5f);
  Require(p.GetComponent<AndroidReflex>().Granted==.1f&&Mathf.Abs(Bonus(p.stats[StatType.Dodge])-baseline-.1f)<.0001f,"Android Reflex stops updating after its ten-percent growth cap, including later movement loss");
  yield return AndroidPick(game,"Reflex",game.playerXP.level);
  Require(p.GetComponents<AndroidReflex>().Length==1,"Android repeated Reflex adds movement without another provider");
  var roller=(DodgeRoller)AccessTools.Field(typeof(PlayerHealth),"dodger").GetValue(p.playerHealth);Plugin.SetField(roller,"_consecutiveHitCtr",1000);
  p.stats[StatType.Dodge].AddMultiplierBonus(5);DodgeSeed(.9f,1);
  Require(!roller.Roll(),"Android random dodge has no native consecutive-hit guarantee and respects the sixty-percent cap");
  p.stats[StatType.DodgeCapMod].AddFlatBonus(100);DodgeSeed(.9f,1);
  Require(roller.Roll(),"Android dodge uses the modified cap stat rather than a hardcoded ceiling");
  p.stats[StatType.Dodge].AddMultiplierBonus(-5);p.stats[StatType.DodgeCapMod].AddFlatBonus(-100);
  int nativeDodgeEvents=0;p.playerHealth.onDodged.AddListener(()=>nativeDodgeEvents++);p.playerHealth.isInvincible.UnFlip();
  var pool=ObjectPooler.SharedInstance;pool.AddObject("Validation_Dodge_Enemy",pool.itemDictionary["BrainMonster"].objectToPool,1);p.SetPosition(new Vector3(100,100,0));
  for(int sample=0;sample<2;sample++)
  {
   var actor=pool.GetPooledObject("Validation_Dodge_Enemy");DurableEnemy(actor,p.transform.position+Vector3.right*.25f,false);actor.tag="Enemy";
   int hp=p.playerHealth.hp,contacts=DodgeContacts,popups=DodgePopups;DodgeContactMinimum=sample==0?0:.9f;DodgeContactMaximum=sample==0?.01f:1;DodgeContactRoll=true;
   try{Physics2D.SyncTransforms();while(DodgeContacts==contacts)yield return null;ReturnFixtureActor(actor);}
   finally{DodgeContactRoll=false;}
   Require(nativeDodgeEvents==0,"actual Android contact omits the native onDodged event");
   Require(sample==0?p.playerHealth.hp==hp&&DodgePopups==popups+1:p.playerHealth.hp==hp-1&&DodgePopups==popups,sample==0?"real enemy contact uses the controlled successful source roll and localized dodge popup without health loss":"real enemy contact uses the controlled failed source roll and deals ordinary damage");
   while(p.playerHealth.isInvincible.value)yield return null;
  }
  Log("fixture-precondition","Source scheduler disabled and real contact rolls controlled at the actual source entry point; no natural dodge frequency or RNG distribution claim. Raw movement increments explicitly seed Reflex boundaries; only recorded acquisitions replay on Continue.");
  yield return SaveAndroidDodge(game);
 }
 IEnumerator SaveAndroidDodge(GameController game)
 {
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Source Reflex acquisition/growth/cap/repeats, capped random roll, real collision/popup/damage and unsaved-phase checkpoint");Application.Quit(0);
 }
}

[HarmonyPatch(typeof(AndroidPlayerCollision),"Enter")]
class DodgeContactRollObservation
{
 [HarmonyPriority(Priority.First)]
 static void Prefix(UnityEngine.Collision2D collision)
 {
  if(!RuntimeChecks.DodgeContactRoll||collision.gameObject.layer!=(int)TagLayerUtil.Enemy)return;
  RuntimeChecks.SeedFirstRoll(RuntimeChecks.DodgeContactMinimum,RuntimeChecks.DodgeContactMaximum);RuntimeChecks.DodgeContacts++;
 }
}
[HarmonyPatch(typeof(AndroidDamagePopup),"Pop")]
class DodgePopupObservation
{
 static void Prefix(string text,Transform target)
 {if(RuntimeChecks.DodgeContactRoll&&target==PlayerController.Instance.playerHealth.transform&&text==LocalizationSystem.GetLocalizedValue("dodge_label"))RuntimeChecks.DodgePopups++;}
}
