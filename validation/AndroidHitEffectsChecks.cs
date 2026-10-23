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
 internal static int HitEffectThunderCalls;
 internal static GameObject HitEffectThunderTarget;
 internal static bool ForceEnergizedRoll;
 internal static float EnergizedMinimum,EnergizedMaximum;
 internal static void SeedFirstRoll(float minimum,float maximum)
 {
  for(int seed=0;;seed++)
  {UnityEngine.Random.InitState(seed);var state=UnityEngine.Random.state;float roll=UnityEngine.Random.Range(0f,1f);if(roll>=minimum&&roll<maximum){UnityEngine.Random.state=state;return;}}
 }
 static void GenerateThunderWithRoll(GameObject target,float minimum,float maximum)
 {
  ForceEnergizedRoll=true;EnergizedMinimum=minimum;EnergizedMaximum=maximum;
  try{ThunderGenerator.SharedInstance.GenerateAt(target,1);}finally{ForceEnergizedRoll=false;}
 }
 IEnumerator CheckAndroidHitEffects(bool continuation)
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
   RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
   yield return AndroidPick(game,"Hellspawns",game.playerXP.level);
  }
  Object.FindObjectOfType<HordeSpawner>().enabled=false;
  var template=ObjectPooler.SharedInstance.itemDictionary.Values.First(item=>item.objectToPool.layer==(int)TagLayerUtil.Enemy&&item.objectToPool.GetComponent<Health>()!=null).objectToPool;
  var target=DurableEnemy(template,p.transform.position+Vector3.right*35,true);target.gameObject.tag="Untagged";target.gameObject.SetActive(true);
  target.TakeDamage(DamageType.Bullet,20);Require(!BurnSystem.SharedInstance.IsBurning(target.gameObject),"Android Hellspawns ignores non-summon damage");
  target.TakeDamage(DamageType.Summon,20);Require(BurnSystem.SharedInstance.IsBurning(target.gameObject),"Android Hellspawns responds to actual surviving Summon-classified Health damage without a Summon producer notification");
  target.gameObject.SetActive(false);target.gameObject.SetActive(true);target.isInvincible.Flip();target.TakeDamage(DamageType.Summon,20);
  Require(!BurnSystem.SharedInstance.IsBurning(target.gameObject),"invulnerable native Health damage does not trigger Android summon-hit effects");target.isInvincible.UnFlip();
  target.TakeDamage(DamageType.Summon,1000000);Require(!BurnSystem.SharedInstance.IsBurning(target.gameObject),"lethal native summon damage has no Android surviving-hurt proc");
  target=DurableEnemy(template,p.transform.position+Vector3.right*35,true);target.gameObject.SetActive(true);target.gameObject.tag="EnemyChampion";Require(target.CompareTag("EnemyChampion"),"fixture uses the actual declared Windows Champion tag");target.TakeDamage(DamageType.Summon,20);
  Require(BurnSystem.SharedInstance.IsBurning(target.gameObject),"a Champion-tagged non-boss remains eligible for Android Hellspawns");
  var source=SelectedMap.MapData.bossSpawns[0];var spawner=Object.FindObjectOfType<BossSpawner>();
  spawner.LoadSpawners(new List<BossSpawn>{new BossSpawn{bossPrefab=source.bossPrefab,timeToSpawn=0,maxHP=10000,spawnPosition=new Vector2(15,0),dontSpawnArena=true,killAllOnSpawn=false}});
  Health boss;while((boss=Object.FindObjectsOfType<Health>().FirstOrDefault(h=>!h.isDead&&h.name.Replace("(Clone)","").Trim()==source.bossPrefab.name&&h.gameObject.activeInHierarchy))==null)yield return null;
  boss.TakeDamage(DamageType.Summon,20);Require(!BurnSystem.SharedInstance.IsBurning(boss.gameObject),"actual native BossSpawner actor is excluded from Android Hellspawns");
  if(!continuation)yield return AndroidPick(game,"Thunderspawns",game.playerXP.level);
  int calls=HitEffectThunderCalls;SeedFirstRoll(.15f,.3f);boss.PostNotification("Revamped.AndroidSurvivingHurt",DamageType.Summon);
  Require(HitEffectThunderCalls==calls,"Android Thunderspawns rejects a roll between fifteen and thirty percent");
  SeedFirstRoll(0,.15f);boss.PostNotification("Revamped.AndroidSurvivingHurt",DamageType.Summon);
  Require(HitEffectThunderCalls==calls+1&&HitEffectThunderTarget==boss.gameObject,"Android Thunderspawns keeps bosses eligible and strikes the hurt Health target");
  calls=HitEffectThunderCalls;for(int i=0;i<32;i++)boss.TakeDamage(DamageType.Summon,1);
  Require(HitEffectThunderCalls>calls,"actual surviving native summon damage reaches the Android lightning proc");
  calls=HitEffectThunderCalls;boss.TakeDamage(DamageType.Summon,1000000);Require(HitEffectThunderCalls==calls,"lethal native summon damage does not roll Android Thunderspawns");
  if(!continuation)yield return AndroidPick(game,"Energized",game.playerXP.level);
  target.transform.position=p.transform.position+Vector3.right*35;target.gameObject.SetActive(false);target.gameObject.SetActive(true);Physics2D.SyncTransforms();
  int particles=ObjectPooler.SharedInstance.pooledObjectsDictionary["StaticFX"].Count(o=>o!=null&&o.activeSelf);
  Plugin.SetField(p.ammo,"<amount>k__BackingField",1);GenerateThunderWithRoll(target.gameObject,0,.2f);
  Require(p.ammo.amount==4&&ObjectPooler.SharedInstance.pooledObjectsDictionary["StaticFX"].Count(o=>o!=null&&o.activeSelf)==particles+1,"Android Energized can grant three ammo and its native pooled feedback on the first actual native lightning hit");
  Plugin.SetField(p.ammo,"<amount>k__BackingField",1);for(int i=0;i<5;i++)GenerateThunderWithRoll(target.gameObject,.2f,1);
  Require(p.ammo.amount==1,"five failed Android Energized rolls do not grant deterministic cadence ammo");
  yield return Capture(continuation?"android-hit-effects-continue.png":"android-hit-effects-save.png");
  if(!continuation)
  {
   PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
   while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
   File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Android native surviving summon-hit effects, boss restriction, source chance and random Energized checkpoint");Application.Quit(0);yield break;
  }
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  template=ObjectPooler.SharedInstance.itemDictionary.Values.First(item=>item.objectToPool.layer==(int)TagLayerUtil.Enemy&&item.objectToPool.GetComponent<Health>()!=null).objectToPool;
  target=DurableEnemy(template,p.transform.position+Vector3.right*35,true);target.gameObject.SetActive(true);calls=HitEffectThunderCalls;
  for(int i=0;i<32;i++)target.TakeDamage(DamageType.Summon,1);
  Plugin.SetField(p.ammo,"<amount>k__BackingField",1);GenerateThunderWithRoll(target.gameObject,0,.2f);
  Require(!BurnSystem.SharedInstance.IsBurning(target.gameObject)&&HitEffectThunderCalls==calls&&p.ammo.amount==1,"normal scene restart removes both summon-hit observers and Energized ownership");
  yield return CheckRevolver(game,p,"android-hit-effects-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Android hit-effect fresh-process Continue and original restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(ThunderGenerator),nameof(ThunderGenerator.GenerateAt),new[]{typeof(GameObject),typeof(int)})]
class AndroidHitEffectsThunderProbe
{
 static void Prefix(GameObject target,int damage){if(damage==22){RuntimeChecks.HitEffectThunderCalls++;RuntimeChecks.HitEffectThunderTarget=target;}}
}
[HarmonyPatch(typeof(AndroidAmmoOnThunder),"OnThunderHit")]
class AndroidHitEffectsRollProbe
{
 // Native thunder visuals and SoundEffectSO consume their own random draws.
 // Control only the tested perk roll at the actual delivered callback boundary.
 static void Prefix(){if(RuntimeChecks.ForceEnergizedRoll)RuntimeChecks.SeedFirstRoll(RuntimeChecks.EnergizedMinimum,RuntimeChecks.EnergizedMaximum);}
}
