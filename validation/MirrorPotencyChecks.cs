using System;
using System.Linq;
using System.IO;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.PerkSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckMirrorPotency()
 {
  RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
  yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  var definitions=((System.Collections.Generic.Dictionary<Powerup,int>)WindField(PowerupGenerator.Instance.GetComponent<AndroidPowerupCounts>(),"Counts")).Keys.ToArray();
  foreach(string name in new[]{"DragonEgg","GhostFriend","MagicSpear","MagicScythe","MagicLens","ElectroBug"})
  {var power=definitions.Single(p=>p.name==name);Log("summon-root-recipe",name+" stats="+string.Join(",",((StatChange[])WindField(power,"statChanges")).Select(s=>s.type+":"+s.value))+" actions="+string.Join(",",((PerkEffect[])WindField(power,"effects")).Select(e=>WindField(e,"action").GetType().Name)));}
  yield return AndroidPick(game,"FireStarter",game.playerXP.level);yield return SpecializePick(game,definitions.Single(p=>p.name=="MagicLens"));
  var lenses=PlayerController.Instance.GetComponentsInChildren<BulletEnhanceSummon>();
  foreach(var lens in lenses)Log("mirror-parameters","overall="+lens.overallMultiplier+" damage="+lens.damageMultiplier+" size="+lens.sizeMultiplier+" bounces="+lens.additionalBounces+" burn="+lens.addBurn);
  Require(lenses.Length==3,"Specialize produces exactly three mirrors for the reported Fireball combination");
  MoveLensesAway();var origin=PlayerController.Instance.transform.position;
  for(int i=0;i<lenses.Length;i++)lenses[i].transform.position=origin+Vector3.right*(2+i*3);Physics2D.SyncTransforms();
  var recipe=ProjectileRecipe(damage:40,speed:15,pool:"PF_Fireball");var shot=ProjectileFactory.SharedInstance.SpawnProjectile(recipe,Vector2.right,origin);
  Log("fireball-before-mirrors","damage="+shot.damage+" scale="+shot.transform.localScale);
  while(shot.gameObject.activeSelf&&shot.transform.position.x<origin.x+10)yield return new WaitForFixedUpdate();
  Log("fireball-after-mirrors","active="+shot.gameObject.activeSelf+" damage="+shot.damage+" scale="+shot.transform.localScale+" changed="+WindField(shot.GetComponent<AndroidProjectile>(),"changed"));
  yield return PoiCapture("mirror-fireball-audit.png");ReturnFixtureActor(shot.gameObject);
  SetPhase("PROBE");Log("complete","Actual chest/upgrade mirrors, per-actor fields and real Fireball passage through three lens colliders recorded.");Application.Quit(0);
 }
}
