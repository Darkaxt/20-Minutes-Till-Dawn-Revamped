using System;
using System.IO;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
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
   p=PlayerController.Instance;var restored=p.GetComponents<AndroidReflex>();
   Require(restored.Length==1&&Mathf.Abs(restored[0].Granted-(partial?.04f:.1f))<.0001f,"fresh-process Continue restores the exact Android Reflex provider and accumulated growth");
   float previous=Bonus(p.stats[StatType.Dodge]);p.stats[StatType.MoveSpeed].AddMultiplierBonus(partial?.02f:.3f);
   Require(Mathf.Abs(Bonus(p.stats[StatType.Dodge])-previous-(partial?.02f:0))<.0001f,partial?"restored uncapped Reflex continues from the saved movement baseline":"restored Reflex remains capped without duplicate contributions");
   yield return Capture("android-dodge-continue.png");yield return AchievementExit(game,false);
   yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
   Require(p.GetComponent<AndroidReflex>()==null&&Bonus(p.stats[StatType.Dodge])==0,"native restart removes Reflex provider and accumulated dodge");
   yield return CheckRevolver(game,p,"android-dodge-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Android Reflex fresh-process provider/cap persistence and clean original restart");Application.Quit(0);yield break;
  }
  RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
  yield return AchievementRun("Shana","RevolverData","Endless_Forest");game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;
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
  bool dodged=false;p.playerHealth.onDodged.AddListener(()=>dodged=true);int hp=p.playerHealth.hp;p.playerHealth.isInvincible.UnFlip();DodgeSeed(0,.01f);
  var enemy=new GameObject("DodgeNativeCollisionFixture");enemy.tag="Enemy";AccessTools.Method(typeof(PlayerHealth),"CheckCollision").Invoke(p.playerHealth,new object[]{enemy});Object.Destroy(enemy);
  Require(dodged&&p.playerHealth.hp==hp,"native collision consumer dispatches a successful Android dodge without losing health");
  yield return SaveAndroidDodge(game);
 }
 IEnumerator SaveAndroidDodge(GameController game)
 {
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Android Reflex acquisition/growth/cap/repeats, random dodge/cap/native collision and checkpoint");Application.Quit(0);
 }
}
