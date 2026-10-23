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
using flanne.PerkSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 static AndroidKillEffectAsset[] AndroidKillCounters()
 {var equipped=(List<Powerup>)AccessTools.Field(typeof(PlayerPerks),"_equippedPerks").GetValue(PlayerController.Instance.playerPerks);return Resources.FindObjectsOfTypeAll<AndroidKillEffectAsset>().Where(a=>a.Key!="feed_the_beasts_name"&&equipped.Any(p=>RunResume.Key(p)==a.Key)).ToArray();}
 IEnumerator CheckAndroidHealthPerks(bool continuation)
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
   p=PlayerController.Instance;Require(AndroidKillCounters().Length==2&&AndroidKillCounters().All(t=>t.Kills==0),"fresh-process Continue omits the two source asset partial-kill counters");
   Require(Mathf.Abs(Bonus(BurnSystem.SharedInstance.burnDamageMultiplier)-.7f)<.001f&&Mathf.Abs(Bonus(ThunderGenerator.SharedInstance.damageMod)-.7f)<.001f,"Continue reconstructs additive element modifiers once per tome");
   var enemyPrefab=ObjectPooler.SharedInstance.itemsToPool.First(item=>item.objectToPool.layer==(int)TagLayerUtil.Enemy&&item.objectToPool.GetComponent<Health>()!=null).objectToPool;
   var restoredTarget=DurableEnemy(enemyPrefab,p.transform.position+Vector3.right*35,true);restoredTarget.gameObject.SetActive(true);
   int restoredHP=restoredTarget.HP;restoredTarget.TakeDamage(DamageType.Bullet,20);Require(restoredHP-restoredTarget.HP==18,"continued Might retains Android damage sign at TakeDamage");
   restoredHP=restoredTarget.HP;restoredTarget.HPChange(-20);Require(restoredHP-restoredTarget.HP==18,"continued Might retains Android damage sign at direct HPChange");
   yield return Capture("android-health-perks-continue.png");yield return AchievementExit(game,false);
   yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
   Require(AndroidKillCounters().Length==0&&p.stats[StatType.BulletDamage].FlatBonus==0&&Bonus(BurnSystem.SharedInstance.burnDamageMultiplier)==0,"new run has no acquired Android heart owners or old numeric damage bonuses");
   yield return CheckRevolver(game,p,"android-health-perks-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Android heart/element/damage fresh-process Continue and clean original restart");Application.Quit(0);yield break;
  }
  RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
  yield return AchievementRun("Shana","RevolverData","Endless_Forest");game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  Object.FindObjectOfType<HordeSpawner>().enabled=false;
  foreach(string name in new[]{"ReaperRounds","LightBullets"})yield return AndroidPick(game,name,game.playerXP.level);
  int bullet=p.stats[StatType.BulletDamage].FlatBonus,summon=p.stats[StatType.SummonDamage].FlatBonus;
  yield return AndroidPick(game,"DeathRounds",game.playerXP.level);
  Require(p.stats[StatType.BulletDamage].FlatBonus==bullet+10&&p.stats[StatType.SummonDamage].FlatBonus==summon+10,"Android Death Rounds adds flat damage before both bullet and summon stat multipliers");
  yield return AndroidPick(game,"Bloodsuckers",game.playerXP.level);yield return AndroidPick(game,"SoulDrain",game.playerXP.level);
  var counters=AndroidKillCounters();Require(counters.Length==2,"each acquired heart perk resolves one source asset kill counter");
  var template=ObjectPooler.SharedInstance.itemsToPool.First(item=>item.objectToPool.layer==(int)TagLayerUtil.Enemy&&item.objectToPool.GetComponent<Health>()!=null).objectToPool;
  var target=DurableEnemy(template,p.transform.position+Vector3.right*35,true);target.gameObject.SetActive(true);
  p.playerHealth.hp=p.playerHealth.maxHP-1;p.playerHealth.shp=0;foreach(var t in counters)t.Kills=499;
  target.TakeDamage(DamageType.Bullet,1000000);yield return null;
  Require(counters.All(t=>t.Kills==499)&&p.playerHealth.hp==p.playerHealth.maxHP-1&&p.playerHealth.shp==0,"heart perks do not count a native bullet kill");
  target=DurableEnemy(template,p.transform.position+Vector3.right*35,true);target.gameObject.SetActive(true);target.TakeDamage(DamageType.Summon,1000000);yield return null;
  Require(counters.All(t=>t.Kills==0)&&p.playerHealth.hp==p.playerHealth.maxHP&&p.playerHealth.shp==1,"five-hundredth native summon kill grants both hearts immediately despite distant kill position");
  int capacity=p.playerHealth.maxSHP;yield return AndroidPick(game,"SoulExpand",game.playerXP.level);
  Require(p.playerHealth.maxSHP==capacity+2&&p.playerHealth.shp==2,"Android Soul Conversion increases capacity then grants the heart directly");
  target=DurableEnemy(template,p.transform.position+Vector3.right*35,true);target.gameObject.SetActive(true);int hp=target.HP;
  target.TakeDamage(DamageType.Thunder,20);Require(hp-target.HP==20,"unmodified native thunder damage baseline");
  yield return NativeTomePick(game,false,"TomeOfMight");hp=target.HP;target.TakeDamage(DamageType.Thunder,20);
  Require(hp-target.HP==18,"Android Tome of Might signed plus-two HP modifier reduces incoming damage by two across damage categories");
  p.playerHealth.hp--;hp=target.HP;target.TakeDamage(DamageType.Bullet,20);
  Require(hp-target.HP==18,"Android Tome of Might fixed signed contribution does not scale with current or maximum hearts");
  hp=target.HP;target.HPChange(-20);Require(hp-target.HP==18,"Android Might preserves its signed contribution for direct HP-change attacks as well as TakeDamage");
  hp=target.HP;target.HPChange(1);Require(target.HP-hp==3,"Android Might also adds two to a positive enemy HP change in the shared source pipeline");
  float reduction=Reduction(p.stats[StatType.BulletDamage]);
  yield return NativeTomePick(game,false,"TomeOfElements");yield return NativeTomePick(game,false,"TomeOfElements");
  Require(Mathf.Abs(Reduction(p.stats[StatType.BulletDamage])-reduction*.25f)<.001f&&Mathf.Abs(Bonus(BurnSystem.SharedInstance.burnDamageMultiplier)-.7f)<.001f&&Mathf.Abs(Bonus(ThunderGenerator.SharedInstance.damageMod)-.7f)<.001f&&Mathf.Abs(Bonus(FreezeSystem.SharedInstance.durationMod)-.7f)<.001f,"Android Elements applies each bullet penalty and adds elemental modifiers rather than multiplying independent damage buffs");
  foreach(var t in counters)t.Kills=317;
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Android native health-perk acquisition/direct grants, flat damage/Might/Elements composition and checkpoint");Application.Quit(0);
 }
}
