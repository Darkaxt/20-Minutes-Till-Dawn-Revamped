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
 Health weaponObserved;readonly List<int> weaponHits=new List<int>();
 int weaponTyped,weaponEvents;AndroidSummonCollisionData weaponPayload;
 void WeaponTyped(object sender,object args){if(ReferenceEquals(sender,weaponObserved))weaponTyped++;}
 void WeaponCollision(object sender,object args)
 {if(args is AndroidSummonCollisionData data&&data.OtherID==weaponObserved.gameObject.GetInstanceID()){weaponEvents++;weaponPayload=data;}}
 static void WeaponBonus(StatMod stat,float value)=>stat.AddMultiplierBonus(value-Bonus(stat));
 static List<Health> CursedActors()=>(List<Health>)AccessTools.Field(typeof(CurseSystem),"_cursedTargets").GetValue(CurseSystem.Instance);
 IEnumerator WeaponPhysicalHit(OrbitalWeapon weapon,int expected,bool cursed,string reason)
 {
  while(CursedActors().Contains(weaponObserved))yield return null;
  Plugin.SetField(weaponObserved,"<HP>k__BackingField",100000);
  weapon.transform.position=weaponObserved.transform.position+Vector3.left*4;
  Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
  weaponHits.Clear();weaponTyped=weaponEvents=0;
  weapon.transform.position=weaponObserved.transform.position;Physics2D.SyncTransforms();
  while(weaponHits.Count==0)yield return null;
  Log("weapon-contact",reason+" hits="+string.Join(",",weaponHits)+" typed="+weaponTyped+" events="+weaponEvents+" cursed="+CursedActors().Contains(weaponObserved));
  Require(weaponHits.Count==1&&weaponHits[0]==expected&&weaponTyped==1&&weaponEvents==1&&weaponPayload.SummonName=="Scythe"&&CursedActors().Contains(weaponObserved)==cursed,"actual source Scythe physical damage/payload: "+reason);
  weapon.transform.position=weaponObserved.transform.position+Vector3.left*4;Physics2D.SyncTransforms();
  while(CursedActors().Contains(weaponObserved))yield return null;
 }
 IEnumerator CheckAndroidWeaponContacts(bool continuation)
 {
  GameController game;PlayerController player;
  if(continuation)
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;
   Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))
   {var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));if(error!=null)throw new InvalidOperationException(error.text);yield return null;}
   player=PlayerController.Instance;
  }
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();
   yield return AndroidPick(game,"MagicScythe",game.playerXP.level);
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();player.SetPosition(new Vector3(100,100,0));
  var weapons=player.GetComponentsInChildren<OrbitalWeapon>().Where(s=>s.SummonTypeID=="Scythe").ToArray();
  while(weapons.Any(s=>!SummonStarted(s)))yield return null;
  Require(weapons.Length==(continuation?2:1)&&weapons.All(s=>s.GetComponent<Rigidbody2D>().useFullKinematicContacts&&(float)WindField(s,"baseRotationSpeed")==-160),"native Scythe groups use source full contacts and rotation on acquisition/Continue");
  foreach(var weapon in weapons){weapon.enabled=false;weapon.GetComponent<Orbital>().enabled=false;weapon.GetComponent<Knockback>().knockbackForce=0;}
  foreach(var weapon in weapons.Skip(1))weapon.transform.position=player.transform.position+Vector3.up*40;
  Log("fixture-precondition","Source-pooled bodyless Brain, stopped scheduler, zero knockback and controlled Scythe/stat inputs isolate real physics and source signed modifiers. Explicit manual stat/actor edits test omission on Continue; no acquired-upgrade claim for those edits.");
  weaponObserved=DurableEnemy(ObjectPooler.SharedInstance.GetPooledObject("BrainMonster"),player.transform.position+Vector3.right*35,false);weaponObserved.gameObject.tag="Untagged";weaponObserved.maxHP=100000;
  Require(weaponObserved.GetComponent<Rigidbody2D>()==null,"actual source Scythe target is bodyless, so the Android full-contact flag is required");
  ObserveDamage(weaponObserved,d=>weaponHits.Add(d));this.AddObserver(WeaponTyped,"Health.TweakSummonDamage");this.AddObserver(WeaponCollision,"WeaponSummon.CollisionEvent");
  var selected=weapons[0];
  if(!continuation)
  {
   yield return WeaponPhysicalHit(selected,40,false,"unupgraded base40 ignores Enemy tag");
   yield return AndroidPick(game,"WindCutter",game.playerXP.level);yield return AndroidPick(game,"ScytheMastery",game.playerXP.level);yield return AndroidPick(game,"ShadowBlade",game.playerXP.level);
   float curse=CurseSystem.Instance.curseDamageMultiplier;
   foreach(string key in new[]{"WindCutter","ScytheMastery","ShadowBlade"})yield return AndroidPick(game,key,game.playerXP.level);
   var rules=player.GetComponent<AndroidWeaponRules>();
   Require(rules.Bullet.Count==1&&rules.Movement.Count==1&&rules.Curse.Count==1&&Mathf.Abs(CurseSystem.Instance.curseDamageMultiplier-curse-.15f)<.001f,"repeated Scythe upgrades keep single source rule membership while Shadow Blade adds another15% curse damage");
   WeaponBonus(player.stats[StatType.SummonDamage],.25f);WeaponBonus(player.stats[StatType.BulletDamage],.5f);WeaponBonus(player.stats[StatType.MoveSpeed],.25f);
   yield return WeaponPhysicalHit(selected,94,true,"50 summon damage times1.25 movement times1.5 bullet rounds signed to94");
   player.stats[StatType.BulletDamage].AddFlatBonus(100);player.stats[StatType.MoveSpeed].AddFlatBonus(100);
   yield return WeaponPhysicalHit(selected,94,true,"source movement/bullet channels ignore flat additions");
   player.stats[StatType.BulletDamage].AddFlatBonus(-100);player.stats[StatType.MoveSpeed].AddFlatBonus(-100);
   WeaponBonus(player.stats[StatType.BulletDamage],-1);yield return WeaponPhysicalHit(selected,63,true,"zero bullet factor is skipped");
   WeaponBonus(player.stats[StatType.BulletDamage],.5f);WeaponBonus(player.stats[StatType.MoveSpeed],-1);yield return WeaponPhysicalHit(selected,75,true,"zero movement factor is skipped");
   WeaponBonus(player.stats[StatType.MoveSpeed],.25f);yield return NativeTomePick(game,false,"TomeOfMight");yield return WeaponPhysicalHit(selected,92,true,"Might follows the signed source weapon recipe");
   Plugin.SetField(selected,"baseDamage",77);yield return AndroidPick(game,"MagicScythe",game.playerXP.level);
   weapons=player.GetComponentsInChildren<OrbitalWeapon>().Where(s=>s.SummonTypeID=="Scythe").ToArray();while(weapons.Any(s=>!SummonStarted(s)))yield return null;
   Require(weapons.Length==2&&weapons.All(s=>s.GetComponent<Rigidbody2D>().useFullKinematicContacts)&&weapons.Skip(1).All(s=>s.baseDamage==40),"later native Scythe creation uses pristine base40 and source full contacts without copying the edited older actor");
   foreach(var weapon in weapons){weapon.enabled=false;weapon.GetComponent<Orbital>().enabled=false;weapon.transform.position=player.transform.position+Vector3.up*40;}
   Require(ResumeRuntime.Capture(player).summons.Count==0&&ResumeRuntime.Capture(player).stats.Count==0,"current source checkpoint omits unsaved Scythe actor edits and manual aggregate stat totals");
  }
  else
  {
   Require(weapons.All(s=>s.baseDamage==40)&&Mathf.Abs(Bonus(player.stats[StatType.SummonDamage]))<.001f&&Mathf.Abs(Bonus(player.stats[StatType.BulletDamage])-.2f)<.001f&&Mathf.Abs(Bonus(player.stats[StatType.MoveSpeed])-.2f)<.001f,"source Continue rebuilds obtained Scythe stats without the original manual stat/actor edits");
   yield return WeaponPhysicalHit(selected,56,true,"source obtained factors1.2x1.2 at base40 then signed Might");
  }
  ReturnFixtureActor(weaponObserved.gameObject);this.RemoveObserver(WeaponTyped,"Health.TweakSummonDamage");this.RemoveObserver(WeaponCollision,"WeaponSummon.CollisionEvent");
  yield return Capture(continuation?"android-scythe-continue.png":"android-scythe-save.png");
  if(!continuation)
  {
   PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
   while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
   File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Actual source Scythe acquisition/repeats/physics/rules/curse/signed rounding and native source checkpoint");Application.Quit(0);yield break;
  }
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();
  Require(PlayerController.Instance.GetComponentsInChildren<OrbitalWeapon>().Length==0&&PlayerController.Instance.GetComponent<AndroidWeaponRules>()==null,"ordinary restart removes source Scythe actors and rule owner");
  yield return CheckRevolver(game,PlayerController.Instance,"android-scythe-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Actual source Scythe grouped Continue/physics/curse/rule composition and original restart");Application.Quit(0);
 }
}
