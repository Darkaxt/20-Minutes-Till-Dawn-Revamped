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
 static float SpearCooldown(StabbingSummon spear)=>(float)AccessTools.Property(typeof(AttackingSummon),"finalAttackCooldown").GetValue(spear);
 static bool SpearAttack(StabbingSummon spear)=>(bool)AccessTools.Method(typeof(StabbingSummon),"Attack").Invoke(spear,null);
 void ControlSpears(StabbingSummon[] spears,StabbingSummon selected)
 {
  foreach(var spear in spears){spear.enabled=false;spear.gameObject.SetActive(spear==selected);var orbit=spear.GetComponentInParent<Orbital>();if(orbit!=null)orbit.enabled=false;}
  controlledSpear=selected;
 }
 IEnumerator CheckAndroidSpears(bool continuation)
 {
  GameController game;PlayerController player;
  if(!continuation)
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();yield return OpenCustomConfiguration();
   var map=CustomMode.Maps().Single(m=>m.name=="Endless_Forest");Click(CustomButton("CustomLoadDefaults"));Click(CustomButton("CustomAction"+Array.IndexOf(CustomMode.Maps(),map)));
   Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>();Click(CustomButton("CustomStart"));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
   player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;player.SetPosition(new Vector3(100,100,0));GameTimer.SharedInstance.Stop();
   yield return AndroidPick(game,"MagicSpear",game.playerXP.level);
  }
  else
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;
   Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))
   {
    var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));if(error!=null)throw new InvalidOperationException(error.text);yield return null;
   }
   player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;GameTimer.SharedInstance.Stop();
  }
  Require(CustomMode.Active&&CustomMode.Current.map=="Endless_Forest"&&SelectedMap.MapData.achievementsDisabled,"spear workflow uses actual obtained-mode Custom defaults with progression disabled");
  var spears=player.GetComponentsInChildren<StabbingSummon>(true);Require(spears.Length==(continuation?4:2),"actual Magic Spear applications reconstruct the complete two-per-pick owned group");
  while(spears.Any(s=>WindField(s,"stats")==null))yield return null;
  if(continuation)
  {
   Require(player.playerHealth.maxHP==5&&player.playerHealth.shp==3&&Mathf.Abs(Bonus(player.stats[StatType.SummonDamage])-.5f)<.001f&&player.stats[StatType.SummonDamage].FlatBonus==10,"Continue retains the actual source health and summon-stat inputs");
   Require(CustomMode.Get("summon:Spear:damage")==2&&CustomMode.Get("summon:Spear:speed")==1.5f,"Continue restores the real native Spear Custom controls");
   float[] clocks={.12f,.22f,.32f,.42f};for(int i=0;i<spears.Length;i++)Require(Mathf.Abs((float)WindField(spears[i],"_timer")-clocks[i])<.1f,"continued owned spear retains its independent source interval, ordinal "+i);
   Require(Mathf.Abs(Bonus(spears[2].attackSpeedMod)-.25f)<.001f,"the source spear's per-instance attack effect survives reconstruction");
   ControlSpears(spears,spears[2]);
  }
  else
  {
   ControlSpears(spears,spears[0]);Plugin.SetField(controlledSpear,"_timer",3f);float delta=Time.deltaTime,cd=SpearCooldown(controlledSpear);
   AccessTools.Method(typeof(AttackingSummon),"Update").Invoke(controlledSpear,null);
   Require(Mathf.Abs((float)WindField(controlledSpear,"_timer")-(3+delta-cd))<.001f&&WindField(controlledSpear,"_stabbingCoroutine")==null,"source spear consumes one attack interval even when no pooled target is available");
   player.stats[StatType.SummonAttackSpeed].AddMultiplierBonus(100);Require(Mathf.Abs(SpearCooldown(controlledSpear)-.2f)<.001f,"source spear attack cooldown has the recovered0.2-second minimum");player.stats[StatType.SummonAttackSpeed].AddMultiplierBonus(-100);
  }
  var pool=ObjectPooler.SharedInstance;var template=pool.itemsToPool.First(i=>i.objectToPool.CompareTag("Enemy")&&i.objectToPool.GetComponent<Health>()!=null&&i.objectToPool.GetComponent<MoveComponent2D>()!=null).objectToPool;
  pool.AddObject("Validation_Spear_Enemy",template,2);var targetActor=pool.GetPooledObject("Validation_Spear_Enemy");var target=DurableEnemy(targetActor,player.transform.position+Vector3.right*3,false);targetActor.tag="Untagged";targetActor.SetActive(true);target.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;
  var hits=new List<int>();target.onHurt.AddListener(d=>hits.Add(d));
  if(continuation)
  {
   yield return SpearHit(target,hits,378,"continued Custom coefficient/stat/Might composition");player.playerHealth.shp=0;yield return SpearHit(target,hits,288,"continued soul-heart loss removes the current-heart contribution");
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;
   Require(!CustomMode.Active&&Object.FindObjectsOfType<StabbingSummon>().Length==0&&player.GetComponents<MonoBehaviour>().All(c=>c.GetType().Name!="AndroidSpearBonuses"),"original restart clears Custom controls, detached/parented spear actors and the player-owned source coefficients");
   yield return CheckRevolver(game,player,"android-spear-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Android spear acquisition order, detached-owner save, source intervals, Custom controls, live health damage and original restart survive fresh-process Continue");Application.Quit(0);yield break;
  }
  targetActor.SetActive(false);DurableEnemy(targetActor,player.transform.position+new Vector3(4.5f,4.5f),false);targetActor.SetActive(true);
  var outside=pool.GetPooledObject("Validation_Spear_Enemy");DurableEnemy(outside,player.transform.position+Vector3.right*5,false);outside.GetComponent<CircleCollider2D>().radius=.8f;outside.SetActive(true);
  var unpooled=DurableEnemy(template,player.transform.position+Vector3.right*2,true);unpooled.gameObject.SetActive(true);
  controlledSpear.transform.position=player.transform.position+Vector3.left;Vector2 origin=controlledSpear.transform.position,expected=origin+((Vector2)target.transform.position-origin).normalized*5;
  int stabs=SpearNativeProbe.Calls;Require(SpearAttack(controlledSpear)&&SpearNativeProbe.Calls==stabs+1&&Vector2.Distance(SpearNativeProbe.Destination,expected)<.001f,"actual stab selects the one strict pooled target beyond the native radial limit, ignoring tagged unpooled and boundary-center targets");
  Require(!SpearAttack(controlledSpear)&&SpearNativeProbe.Calls==stabs+1&&controlledSpear.transform.parent==null,"source busy guard starts only one native stab coroutine and detaches its persistent spear");
  Require(ResumeRuntime.Capture(player).summons.Count(s=>s.id=="Spear")==2,"semantic checkpoint capture includes a persistent spear while its native outbound tween has detached it");
  while(WindField(controlledSpear,"_stabbingCoroutine")!=null)yield return null;
  Require(controlledSpear.transform.parent!=null&&controlledSpear.transform.localPosition.sqrMagnitude<.001f,"the complete actual native outbound/return tween restores its owner and local origin");outside.SetActive(false);unpooled.gameObject.SetActive(false);
  targetActor.SetActive(false);DurableEnemy(targetActor,player.transform.position+Vector3.right*3,false);targetActor.SetActive(true);
  yield return AndroidPick(game,"HolySpear",game.playerXP.level);yield return SpearHit(target,hits,60,"first Holy Spear at four maximum hearts");
  player.playerHealth.maxSHP=4;player.playerHealth.shp=0;Log("fixture-precondition","Soul capacity4 seeded for direct multi-heart assignments; no claim that a single acquired upgrade grants that capacity");
  yield return AndroidPick(game,"SoulKnight",game.playerXP.level);Require(player.playerHealth.shp==0,"source Soul Knight acquisition does not spawn the native extra soul-heart pickup");
  player.playerHealth.shp=2;yield return SpearHit(target,hits,90,"one assignment of two held soul hearts contributes30");
  player.playerHealth.shp=0;yield return SpearHit(target,hits,60,"soul-heart loss removes the bonus instead of retaining gain history");
  player.playerHealth.shp=3;yield return SpearHit(target,hits,105,"three currently held soul hearts contribute45");
  yield return AndroidPick(game,"SoulKnight",game.playerXP.level);yield return SpearHit(target,hits,105,"repeat Soul Knight assigns15 rather than adding another coefficient");
  yield return AndroidPick(game,"HolySpear",game.playerXP.level);yield return SpearHit(target,hits,145,"repeat Holy Spear adds another ten-per-maximum-heart coefficient");
  player.stats[StatType.SummonDamage].AddMultiplierBonus(.5f);yield return SpearHit(target,hits,155,"health contributions are added after the summon stat multiplier");player.playerHealth.hp=1;yield return SpearHit(target,hits,155,"injury does not alter the maximum-heart contribution");
  yield return AndroidPick(game,"DeathRounds",game.playerXP.level);yield return SpearHit(target,hits,170,"actual Death Rounds flat summon damage is transformed before health additions");
  yield return NativeTomePick(game,false,"TomeOfMight");Require(player.playerHealth.maxHP==5,"actual Might increases maximum health by one");yield return SpearHit(target,hits,188,"Might's signed minus-two stage follows the source health additions");
  yield return AndroidPick(game,"MagicSpear",game.playerXP.level);spears=player.GetComponentsInChildren<StabbingSummon>(true);while(spears.Length<4||spears.Any(s=>WindField(s,"stats")==null)){spears=player.GetComponentsInChildren<StabbingSummon>(true);yield return null;}
  ControlSpears(spears,spears[2]);yield return SpearHit(target,hits,188,"a later-created spear inherits the source coefficients without double-counting old buffs");
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="CustomSettings"));Click(CustomButton("CustomTab4"));
  Require(CustomMenu.Instance.GetComponentsInChildren<TMPro.TMP_Text>().Any(t=>t.text=="Spear damage multiplier"),"actual native Custom Summons panel exposes the collected spear");CustomValue(0,"2");CustomValue(1,"1.5");yield return Capture("android-spear-native-custom-controls.png");Click(CustomButton("CustomBack"));Click(game.pauseResumeButton);
  yield return SpearHit(target,hits,378,"Custom doubles the source damage before the signed Might penalty");
  for(int i=0;i<spears.Length;i++)Plugin.SetField(spears[i],"_timer",new[]{.12f,.22f,.32f,.42f}[i]);spears[2].attackSpeedMod.AddMultiplierBonus(.25f);
  Require(Mathf.Abs(SpearCooldown(spears[2])-1.3f/1.25f/1.5f)<.001f,"actual Custom speed composes with the source per-spear attack effect");
  controlledSpear.transform.position=player.transform.position+Vector3.left;Require(SpearAttack(controlledSpear)&&controlledSpear.transform.parent==null,"the actual saved native spear is mid-stab and detached");
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();var saved=ResumeRuntime.Capture(player);Require(saved.summons.Count(s=>s.id=="Spear")==4,"the complete four-spear group remains captured while one owner is detached");yield return Capture("android-spear-detached-checkpoint.png");
  Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Android spear source target/interval/motion, live health coefficients, signed damage, native Custom controls and detached-owner checkpoint");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(StabbingSummon),"StabCR")]
class SpearNativeProbe
{
 internal static int Calls;internal static Vector2 Destination;
 static void Prefix(Vector2 destination){Calls++;Destination=destination;}
}
