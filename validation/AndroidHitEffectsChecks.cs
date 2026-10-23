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
 internal static bool ControlSummonHurtRolls;
 internal static float SummonThunderMinimum=.5f,SummonThunderMaximum=1,CullingMinimum=.5f,CullingMaximum=1;
 Health hitEffectDeathTarget;int hitEffectDeaths;DamageType hitEffectDeathKind;
 void HitEffectDeath(object sender,object args)
 {if(ReferenceEquals(sender,hitEffectDeathTarget)){hitEffectDeaths++;hitEffectDeathKind=(DamageType)args;}}
 Health HitEffectTarget(string poolTag="BrainMonster",int maximum=10000,string tag="Enemy")
 {
  var actor=ObjectPooler.SharedInstance.GetPooledObject(poolTag);
  var burn=AndroidBurnState.Get(BurnSystem.SharedInstance).Find(actor);if(burn!=null)AccessTools.Method(typeof(AndroidBurnStatus),"Release").Invoke(burn,null);
  var target=DurableEnemy(actor,PlayerController.Instance.transform.position+Vector3.right*35,false);
  actor.tag=tag;target.maxHP=maximum;Plugin.SetField(target,"<HP>k__BackingField",maximum);return target;
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
  }
  StopFixtureSpawning();
  ControlSummonHurtRolls=true;SummonThunderMinimum=CullingMinimum=.5f;SummonThunderMaximum=CullingMaximum=1;
  Log("fixture-precondition","Original source-pooled NPCs, controlled health/position and individual source-helper RNG rolls isolate proc contracts. Target BurnBuff is released between cases; no natural spawn, statistical chance or NPC status-reuse claim.");
  try
  {
   if(!continuation)yield return AndroidPick(game,"Hellspawns",game.playerXP.level);
   var target=HitEffectTarget(tag:"Untagged");target.TakeDamage(DamageType.Bullet,20);
   Require(!BurnSystem.SharedInstance.IsBurning(target.gameObject),"source Hellspawns ignores non-Summon damage on an actual pooled Health");
   target.TakeDamage(DamageType.Summon,20);
   Require(BurnSystem.SharedInstance.IsBurning(target.gameObject),"source surviving Summon Health damage triggers Hellspawns without a summon-producer event or Enemy tag");ReturnFixtureActor(target.gameObject);
   target=HitEffectTarget();target.isInvincible.Flip();target.TakeDamage(DamageType.Summon,20);
   Require(!BurnSystem.SharedInstance.IsBurning(target.gameObject),"protected source Health damage does not trigger Hellspawns");target.isInvincible.UnFlip();
   target.TakeDamage(DamageType.Summon,1000000);Require(!BurnSystem.SharedInstance.IsBurning(target.gameObject),"lethal source Summon damage has no surviving-hurt Hellspawns proc");
   target=HitEffectTarget(tag:"EnemyChampion");target.TakeDamage(DamageType.Summon,20);
   Require(BurnSystem.SharedInstance.IsBurning(target.gameObject),"source CreatureType1 remains Hellspawns-eligible with the actual Champion tag");ReturnFixtureActor(target.gameObject);
   string bossTag=SelectedMap.MapData.bossSpawns[0].bossPrefab.name;var boss=HitEffectTarget(bossTag);
   Require((int)WindField(boss.GetComponent<AndroidHealthEvents>(),"CreatureType")==2,"fixture uses the original pooled source CreatureType2 boss");
   while(boss.isInvincible.value)yield return null;
   boss.TakeDamage(DamageType.Summon,20);Require(!BurnSystem.SharedInstance.IsBurning(boss.gameObject),"source CreatureType2 boss is excluded from Hellspawns");
   if(!continuation)yield return AndroidPick(game,"Thunderspawns",game.playerXP.level);
   int calls=HitEffectThunderCalls;SummonThunderMinimum=.15f;SummonThunderMaximum=.3f;boss.TakeDamage(DamageType.Summon,1);
   Require(HitEffectThunderCalls==calls,"source Thunderspawns rejects a controlled roll between fifteen and thirty percent");
   SummonThunderMinimum=0;SummonThunderMaximum=.15f;boss.TakeDamage(DamageType.Summon,1);
   Require(HitEffectThunderCalls==calls+1&&HitEffectThunderTarget==boss.gameObject,"actual surviving source boss damage triggers one target-owned Thunderspawns strike below fifteen percent");
   calls=HitEffectThunderCalls;for(int i=0;i<3;i++)boss.TakeDamage(DamageType.Summon,1);
   Require(HitEffectThunderCalls==calls+3,"each controlled qualifying source Summon hit reaches the actual lightning producer");
   calls=HitEffectThunderCalls;boss.TakeDamage(DamageType.Summon,1000000);
   Require(HitEffectThunderCalls==calls,"lethal source Summon damage does not invoke Thunderspawns");SummonThunderMinimum=.5f;SummonThunderMaximum=1;
   if(!continuation){yield return AndroidPick(game,"Energized",game.playerXP.level);yield return AndroidPick(game,"Energized",game.playerXP.level);}
   while(p.GetComponentsInChildren<AndroidAmmoOnThunder>().Any(helper=>WindField(helper,"ammo")==null))yield return null;
   Require(p.GetComponentsInChildren<AndroidAmmoOnThunder>().Length==2&&p.ammo.max>=6,"source repeat/Continue creates two independent initialized Energized helpers");
   target=HitEffectTarget();int particles=ObjectPooler.SharedInstance.pooledObjectsDictionary["StaticFX"].Count(o=>o!=null&&o.activeSelf);
   Plugin.SetField(p.ammo,"<amount>k__BackingField",0);GenerateThunderWithRoll(target.gameObject,0,.2f);
   Require(p.ammo.amount==6&&ObjectPooler.SharedInstance.pooledObjectsDictionary["StaticFX"].Count(o=>o!=null&&o.activeSelf)==particles+2,"one actual lightning event lets both repeated source Energized helpers grant three ammo and separate pooled feedback");
   Plugin.SetField(p.ammo,"<amount>k__BackingField",1);for(int i=0;i<5;i++)GenerateThunderWithRoll(target.gameObject,.2f,1);
   Require(p.ammo.amount==1,"failed source Energized rolls do not grant cadence ammo");ReturnFixtureActor(target.gameObject);
   if(!continuation)
   {
    float speed=Bonus(p.stats[StatType.SummonAttackSpeed]);yield return AndroidPick(game,"Culling",game.playerXP.level);
    Require(Mathf.Abs(Bonus(p.stats[StatType.SummonAttackSpeed])-speed-.3f)<.001f,"actual source Culling acquisition adds thirty percent attack speed");
    yield return AndroidPick(game,"Culling",game.playerXP.level);
    Require(Mathf.Abs(Bonus(p.stats[StatType.SummonAttackSpeed])-speed-.6f)<.001f,"repeated source Culling adds its second stat bonus");
   }
   CullingMinimum=0;CullingMaximum=.15f;
   target=HitEffectTarget(maximum:2000000);var damage=new List<int>();UnityEngine.Events.UnityAction<int> hurt=amount=>damage.Add(amount);ObserveDamage(target,hurt);
   target.TakeDamage(DamageType.Bullet,1);Require(target.HP==1999999&&damage.SequenceEqual(new[]{1}),"Culling ignores actual non-Summon hurt despite a forced passing roll");damage.Clear();Plugin.SetField(target,"<HP>k__BackingField",2000000);
   target.TakeDamage(DamageType.Summon,1);UnobserveDamage(target,hurt);
   Require(target.HP==1000000&&!target.isDead&&damage.SequenceEqual(new[]{1,999999}),"repeated Culling owns one callback and deals ordinary signed999999 source damage rather than AutoKill");ReturnFixtureActor(target.gameObject);
   target=HitEffectTarget();hitEffectDeathTarget=target;hitEffectDeaths=0;this.AddObserver(HitEffectDeath,Health.DeathEvent);
   try{target.TakeDamage(DamageType.Summon,1);Require(target.isDead&&hitEffectDeaths==1&&hitEffectDeathKind==(DamageType)11,"source Culling execution reaches actual typed Powerup death exactly once");}
   finally{this.RemoveObserver(HitEffectDeath,Health.DeathEvent);hitEffectDeathTarget=null;}
   boss=HitEffectTarget(bossTag);while(boss.isInvincible.value)yield return null;int hp=boss.HP;boss.TakeDamage(DamageType.Summon,1);
   Require(boss.HP==hp-1&&!boss.isDead,"source CreatureType2 boss remains excluded from Culling despite its forced passing roll");ReturnFixtureActor(boss.gameObject);
   CullingMinimum=.5f;CullingMaximum=1;
   yield return Capture(continuation?"android-hit-effects-continue.png":"android-hit-effects-save.png");
   if(!continuation)
   {
    PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
    while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
    File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Source Hellspawns/Thunderspawns typed surviving damage, boss filters, repeated Culling ordinary damage/death and independent Energized acquisition checkpoint");Application.Quit(0);yield break;
   }
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();target=HitEffectTarget();calls=HitEffectThunderCalls;
   target.TakeDamage(DamageType.Summon,1);Plugin.SetField(p.ammo,"<amount>k__BackingField",1);GenerateThunderWithRoll(target.gameObject,0,.2f);
   Require(target.HP==9999&&!BurnSystem.SharedInstance.IsBurning(target.gameObject)&&HitEffectThunderCalls==calls&&p.ammo.amount==1,"ordinary restart clears summon-hit/Culling observers and Energized ownership");ReturnFixtureActor(target.gameObject);
   yield return CheckRevolver(game,p,"android-hit-effects-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Source surviving-hit effects, repeated Culling/Energized, fresh-process Continue and original restart");Application.Quit(0);
  }
  finally{ControlSummonHurtRolls=false;ForceEnergizedRoll=false;}
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
 [HarmonyPriority(Priority.First)]
 static void Prefix(){if(RuntimeChecks.ForceEnergizedRoll)RuntimeChecks.SeedFirstRoll(RuntimeChecks.EnergizedMinimum,RuntimeChecks.EnergizedMaximum);}
}

[HarmonyPatch(typeof(AndroidSummonHurtAsset),"OnHurt")]
class SummonHurtRollProbe
{
 [HarmonyPriority(Priority.First)]
 static void Prefix(AndroidSummonHurtAsset __instance)
 {
  if(!RuntimeChecks.ControlSummonHurtRolls)return;
  if(__instance.Key=="thunderspawns_name")RuntimeChecks.SeedFirstRoll(RuntimeChecks.SummonThunderMinimum,RuntimeChecks.SummonThunderMaximum);
  else if(__instance.Key=="culling_name")RuntimeChecks.SeedFirstRoll(RuntimeChecks.CullingMinimum,RuntimeChecks.CullingMaximum);
 }
}
