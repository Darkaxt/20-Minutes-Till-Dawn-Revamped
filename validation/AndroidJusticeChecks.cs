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
 int justiceSmiteNotices;
 void JusticeSmiteSeen(object sender,object args)=>justiceSmiteNotices++;
 IEnumerator JusticeHit(PlayerController player,Health target,bool protectedHit=false)
 {
  target.gameObject.SetActive(false);DurableEnemy(target.gameObject,player.transform.position+Vector3.right,false);target.gameObject.SetActive(true);
  if(protectedHit)target.isInvincible.Flip();Physics2D.SyncTransforms();int previous=justiceSmiteNotices;
  player.ammo.UseAmmo(player.ammo.amount);while(justiceSmiteNotices==previous)yield return null;yield return null;
  Log("justice-smite-result",new{previous,notices=justiceSmiteNotices,hp=target.HP,protectedHit});
  Require(justiceSmiteNotices==previous+1&&target.HP==(protectedHit?10000:9980),"actual last-ammo Smite emits one source notification after a "+(protectedHit?"protected":"surviving")+" target hit");
  if(protectedHit)target.isInvincible.UnFlip();target.gameObject.SetActive(false);
 }
 IEnumerator CheckAndroidJustice(bool continuation)
 {
  GameController game;PlayerController player;
  if(!continuation)
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;
   yield return AndroidPick(game,"Justice",game.playerXP.level);
  }
  else
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;
   Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))
   {
    var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));
    if(error!=null)throw new InvalidOperationException(error.text);yield return null;
   }
   player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;
  }
  var helpers=player.GetComponentsInChildren<GainMaxHPOnSmiteKill>();
  Require(helpers.Length==(continuation?2:1),"each actual Justice application attaches an independent source growth helper");
  while(helpers.Any(h=>WindField(h,"playerHealth")==null))yield return null;
  Require(helpers.All(h=>(int)WindField(h,"killsToGainMHP")==500&&(int)WindField(h,"mhpGainCap")==3),"source Justice keeps the shipped500-event threshold and three-growth limit per owner");
  player.SetPosition(new Vector3(100,100,0));GameTimer.SharedInstance.Stop();
  // Source RestoreAmmo also invokes OnAmmoChanged. An empty restored clip can
  // already have a real Smite coroutine pending; do not overlap a new test hit.
  while(JusticeSmiteFlowProbe.Running)yield return null;
  var pool=ObjectPooler.SharedInstance;var template=pool.itemsToPool.First(i=>i.objectToPool.CompareTag("Enemy")&&i.objectToPool.GetComponent<Health>()!=null&&i.objectToPool.GetComponent<MoveComponent2D>()!=null).objectToPool;
  pool.AddObject("Validation_Justice_Enemy",template,1);var target=pool.GetPooledObject("Validation_Justice_Enemy").GetComponent<Health>();
  this.AddObserver(JusticeSmiteSeen,SmitePassive.SmiteKillNotification);
  if(continuation)
  {
   Require(player.playerHealth.maxHP==20&&player.playerHealth.hp==2&&player.stats[StatType.MaxHP].Modify(player.loadedCharacter.startHP)==25,"Continue preserves the explicit20-heart Justice clamp and injury even when the saved HP stat evaluates to25");
   Require((int)WindField(helpers[0],"_mhpGainCounter")==3&&(int)WindField(helpers[1],"_mhpGainCounter")==2&&(int)WindField(helpers[1],"_killCounter")==497,"Continue restores exhausted and partially progressed independent Justice owners");
   yield return JusticeHit(player,target);yield return JusticeHit(player,target);
   Require((int)WindField(helpers[1],"_killCounter")==499&&(int)WindField(helpers[1],"_mhpGainCounter")==2,"actual continued Smite hits advance the saved counter without an early growth");
   yield return JusticeHit(player,target);
   Require(player.playerHealth.maxHP==20&&player.playerHealth.hp==2&&(int)WindField(helpers[1],"_killCounter")==0&&(int)WindField(helpers[1],"_mhpGainCounter")==3&&player.stats[StatType.MaxHP].Modify(player.loadedCharacter.startHP)==26,"the continued final source threshold grants one stat increment while retaining the clamp and injury");
   yield return JusticeHit(player,target);Require((int)WindField(helpers[1],"_killCounter")==0,"an exhausted source helper stops counting after its third growth");
   this.RemoveObserver(JusticeSmiteSeen,SmitePassive.SmiteKillNotification);
   yield return AndroidPick(game,"Vitality",game.playerXP.level);Require(player.playerHealth.maxHP==27,"ordinary native Vitality remains uncapped after Justice's explicit capped assignment");
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;
   Require(player.GetComponentsInChildren<GainMaxHPOnSmiteKill>().Length==0&&player.GetComponentsInChildren<SmitePassive>().Length==0&&player.playerHealth.maxHP==4,"original restart removes source Justice/Smite owners and their health growth");
   yield return CheckRevolver(game,player,"android-justice-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Android Justice counters, explicit capped injury, final growth, ordinary uncapped Vitality and original restart survive fresh-process Continue");Application.Quit(0);yield break;
  }
  var first=helpers[0];Plugin.SetField(first,"_killCounter",498);player.playerHealth.hp=2;
  Log("fixture-precondition","Justice counters seeded immediately before source thresholds; actual last-ammo Smite notifications perform every tested growth, not500 full-play kills");
  yield return JusticeHit(player,target,true);Require((int)WindField(first,"_killCounter")==499&&player.playerHealth.maxHP==4,"source Justice counts the protected-target notification without an early growth");
  yield return JusticeHit(player,target);Require((int)WindField(first,"_killCounter")==0&&(int)WindField(first,"_mhpGainCounter")==1&&player.playerHealth.maxHP==5&&player.playerHealth.hp==3,"the actual500th source notification grants one maximum/current heart from injured health");
  for(int growth=2;growth<=3;growth++){Plugin.SetField(first,"_killCounter",499);yield return JusticeHit(player,target);Require((int)WindField(first,"_mhpGainCounter")==growth&&player.playerHealth.maxHP==4+growth,"source Justice completes growth "+growth);}
  yield return JusticeHit(player,target);Require((int)WindField(first,"_killCounter")==0&&player.playerHealth.maxHP==7,"first Justice owner stops at three increments");
  yield return AndroidPick(game,"Justice",game.playerXP.level);helpers=player.GetComponentsInChildren<GainMaxHPOnSmiteKill>();while(helpers.Any(h=>WindField(h,"playerHealth")==null))yield return null;
  Require(helpers.Length==2&&(int)WindField(helpers[1],"_mhpGainCounter")==0,"repeat acquisition creates a fresh capped source Justice owner");
  player.stats[StatType.MaxHP].AddFlatBonus(16);Require(player.playerHealth.maxHP==23,"ordinary HP stat remains uncapped before the explicit source Justice assignment");player.playerHealth.hp=2;
  for(int growth=1;growth<=2;growth++){Plugin.SetField(helpers[1],"_killCounter",499);yield return JusticeHit(player,target);Require(player.playerHealth.maxHP==20&&player.playerHealth.hp==2&&(int)WindField(helpers[1],"_mhpGainCounter")==growth,"source Justice assigns its20-heart cap once without an intermediate uncapped heal, growth "+growth);}
  Plugin.SetField(helpers[1],"_killCounter",497);this.RemoveObserver(JusticeSmiteSeen,SmitePassive.SmiteKillNotification);
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return Capture("android-justice-source-counters-and-injury.png");
  Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Android Justice actual protected/surviving Smite notifications, separate capped owners and injured explicit maximum checkpoint");Application.Quit(0);
 }
[HarmonyPatch(typeof(SmitePassive),"SmiteCR")]
class JusticeSmiteFlowProbe
{
 static readonly Dictionary<IEnumerator,SmitePassive> Active=new Dictionary<IEnumerator,SmitePassive>();
 internal static bool Running=>Active.Values.Any(owner=>owner!=null&&owner.gameObject.activeInHierarchy);
 static void Postfix(SmitePassive __instance,ref IEnumerator __result)=>__result=Observe(__instance,__result);
 static IEnumerator Observe(SmitePassive owner,IEnumerator native)
 {Active.Add(native,owner);try{while(native.MoveNext())yield return native.Current;}finally{Active.Remove(native);}}
}
}
