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
 int LightningBulletStages,LightningThunderStages,LightningNotifications;
 void OnLightningBullet(object sender,object args)=>LightningBulletStages++;
 void OnLightningThunder(object sender,object args)=>LightningThunderStages++;
 void OnLightningNotification(object sender,object args)=>LightningNotifications++;
 void GenerateAreaWithRoll(Vector3 position,int damage)
 {
  ForceEnergizedRoll=true;EnergizedMinimum=0;EnergizedMaximum=.2f;
  try{ThunderGenerator.SharedInstance.GenerateAt(position,damage);}finally{ForceEnergizedRoll=false;}
 }
 IEnumerator CheckAndroidLightning(bool continuation)
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
  }
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
   yield return AndroidPick(game,"Energized",game.playerXP.level);yield return AndroidPick(game,"ElectroMastery",game.playerXP.level);
   yield return NativeTomePick(game,false,"TomeOfElements");
  }
  Object.FindObjectOfType<HordeSpawner>().enabled=false;
  var thunder=ThunderGenerator.SharedInstance;var pool=ObjectPooler.SharedInstance;
  Require(thunder.damageMod.FlatBonus==12&&Mathf.Abs(thunder.sizeMultiplier-1.75f)<.001f,"Android Electro Mastery adds twelve before thunder damage composition and multiplies size by one point seven five");
  Require(Mathf.Abs(Bonus(thunder.damageMod)-.35f)<.001f,"the actual Elements tome composes its additive elemental bonus with Electro Mastery's flat modifier");
  this.AddObserver(OnLightningBullet,"Health.TweakBulletDamage");this.AddObserver(OnLightningThunder,"Health.TweakThunderDamage");this.AddObserver(OnLightningNotification,ThunderGenerator.ThunderHitEvent);
  var template=pool.itemDictionary.Values.First(item=>item.objectToPool.layer==(int)TagLayerUtil.Enemy&&item.objectToPool.GetComponent<Health>()!=null).objectToPool;
  var position=p.transform.position+Vector3.right*35;var target=DurableEnemy(template,position,true);target.gameObject.SetActive(true);
  int hp=target.HP,notifications=LightningNotifications;thunder.GenerateAt(target.gameObject,22);
  Require(target.HP==hp&&LightningNotifications==notifications+1,"targeted Android thunder posts its notification before any physics-delivered damage");
  var prefab=(GameObject)AccessTools.Field(typeof(ThunderGenerator),"thunderPrefab").GetValue(thunder);
  var visual=pool.pooledObjectsDictionary[prefab.name].Single(o=>o.activeSelf);
  Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
  Require(hp-target.HP==(continuation?43:45)&&LightningBulletStages>0&&LightningThunderStages==0,"real Android thunder collision applies mastery before Elements, floors captured damage and uses Bullet classification, with signed Might when equipped");
  while(visual.activeSelf)yield return null;target.gameObject.SetActive(false);
  pool.AddObject("Validation_Thunder_Enemies",template,3);var actors=new Health[3];float[] distances={0,.25f,.8f};
  for(int i=0;i<actors.Length;i++)
  {
   var actor=pool.GetPooledObject("Validation_Thunder_Enemies");actors[i]=DurableEnemy(actor,position+Vector3.right*distances[i],false);actor.SetActive(true);Plugin.SetField(actors[i],"<HP>k__BackingField",9000);
  }
  actors[0].gameObject.AddComponent<CircleCollider2D>().radius=.18f;
  notifications=LightningNotifications;int bullets=LightningBulletStages;Plugin.SetField(p.ammo,"<amount>k__BackingField",0);GenerateAreaWithRoll(position,22);
  int expected=continuation?9024:9022;
  Require(actors[0].HP==expected&&actors[1].HP==expected&&actors[2].HP==9000,"Android positional thunder applies its positive signed input once per pooled center, ignores damageMod and excludes an outside-center overlapping collider");
  Require(LightningNotifications==notifications+2&&p.ammo.amount==6,"each positional lightning target independently delivers the real Energized callback and native ammo cap");
  var harmful=visual.GetComponentsInChildren<MonoBehaviour>(true).Single(c=>c.GetType().Name=="AndroidThunderHitbox");
  Require(visual.activeSelf&&!harmful.enabled&&(int)AccessTools.Field(harmful.GetType(),"Damage").GetValue(harmful)==45,"positional pool reuse disables only the source behaviour and retains its captured damage");
  Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
  Require(LightningBulletStages>bullets&&actors[0].HP<expected,"the real disabled-behaviour collision callback remains active, matching both Unity engine contracts");
  while(visual.activeSelf)yield return null;
  if(!continuation)yield return NativeTomePick(game,false,"TomeOfMight");
  foreach(var actor in actors)Plugin.SetField(actor,"<HP>k__BackingField",9000);
  GenerateAreaWithRoll(position,22);Require(actors[0].HP==9024&&actors[1].HP==9024,"Android signed Might adds two to positional lightning's positive HP change");
  actors[2].transform.position=position+Vector3.left*.25f;Plugin.SetField(actors[2],"<HP>k__BackingField",0);actors[2].isInvincible.Flip();
  GenerateAreaWithRoll(position,22);Require(actors[2].HP==24,"the source positive-area change can restore a seeded active zero-HP actor and bypasses damage-only invulnerability");actors[2].isInvincible.UnFlip();
  int beforeNull=LightningNotifications;thunder.GenerateAt((GameObject)null,22);Require(LightningNotifications==beforeNull,"Android targeted null input returns without a lightning notification");
  yield return Capture(continuation?"android-lightning-continue.png":"android-lightning-save.png");
  this.RemoveObserver(OnLightningBullet,"Health.TweakBulletDamage");this.RemoveObserver(OnLightningThunder,"Health.TweakThunderDamage");this.RemoveObserver(OnLightningNotification,ThunderGenerator.ThunderHitEvent);
  if(!continuation)
  {
   PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
   while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
   File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Android native thunder collision/pool/signed-area/notifications, mastery and Energized checkpoint");Application.Quit(0);yield break;
  }
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;thunder=ThunderGenerator.SharedInstance;
  Require(thunder.damageMod.FlatBonus==0&&thunder.sizeMultiplier==1,"normal restart clears Electro Mastery and source modifier owners");
  yield return CheckRevolver(game,p,"android-lightning-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Android lightning/mastery fresh-process Continue and original restart");Application.Quit(0);
 }
}
