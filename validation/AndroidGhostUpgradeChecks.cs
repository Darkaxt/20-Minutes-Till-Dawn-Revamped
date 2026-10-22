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
 static ShootingSummon[] Ghosts()=>PlayerController.Instance.GetComponentsInChildren<ShootingSummon>(true).Where(s=>s.SummonTypeID=="GhostFriend").ToArray();
 static MonoBehaviour GhostFactor(ShootingSummon ghost)=>ghost.GetComponents<MonoBehaviour>().Single(c=>c.GetType().Name=="AndroidBestFriends");
 static float GhostFactorValue(ShootingSummon ghost)=>(float)WindField(GhostFactor(ghost),"Factor");
 IEnumerator CheckAndroidGhostUpgrades(bool continuation)
 {
  GameController game;PlayerController player;
  if(!continuation)
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();yield return OpenCustomConfiguration();var map=CustomMode.Maps().Single(m=>m.name=="Endless_Forest");Click(CustomButton("CustomLoadDefaults"));Click(CustomButton("CustomAction"+Array.IndexOf(CustomMode.Maps(),map)));
   Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>();Click(CustomButton("CustomStart"));while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
   player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;player.SetPosition(new Vector3(100,100,0));GameTimer.SharedInstance.Stop();
   yield return AndroidPick(game,"GhostFriend",game.playerXP.level);var first=Ghosts().Single();first.enabled=false;
   yield return AndroidPick(game,"BestFriends",game.playerXP.level);yield return AndroidPick(game,"VengefulGhost",game.playerXP.level);
   Require(Mathf.Abs(Bonus(player.stats[StatType.SummonDamage])-.15f)<.001f&&first.numProjectiles==2,"actual Vengeful Ghost grants the source global15% damage alongside one additional current-Ghost projectile");
   Require(Mathf.Abs(GhostFactorValue(first)-1)<.001f,"source Best Friends starts at factor one without an eager callback");
   player.stats[StatType.FireRate].AddMultiplierBonus(.5f);Require(Mathf.Abs(GhostFactorValue(first)-1)<.001f,"direct stat mutation does not invent the source EventStatChange notification");
   while(!(bool)WindField(GhostFactor(first),"Subscribed"))yield return null;
   Log("fixture-precondition","Explicit source EventStatChange dispatch tests the recovered consumer; this is not proof of an Android gameplay emitter");this.PostNotification("EventStatChange",null);
   Require(Mathf.Abs(GhostFactorValue(first)-2.6f)<.001f&&Mathf.Abs(ShootingCooldown(first)-.8f/2.6f)<.001f,"source named notification assigns one plus the selected FireRate modifier");
   yield return AndroidPick(game,"BestFriends",game.playerXP.level);Require(first.GetComponents<MonoBehaviour>().Count(c=>c.GetType().Name=="AndroidBestFriends")==1&&Mathf.Abs(GhostFactorValue(first)-2.6f)<.001f,"repeat Best Friends reuses its single stat type/consumer and preserves the held factor until notification");
   this.PostNotification("EventStatChange",null);Require(Mathf.Abs(GhostFactorValue(first)-2.7f)<.001f,"repeat Best Friends retains its source10% FireRate while the selected type does not duplicate");
   yield return AndroidPick(game,"GhostFriend",game.playerXP.level);var later=Ghosts()[1];later.enabled=false;
   Require(later.numProjectiles==1&&later.GetComponents<MonoBehaviour>().All(c=>c.GetType().Name!="AndroidBestFriends"),"later-created Ghost does not inherit old source per-instance upgrades");
   later.targetMouse=true;yield return AndroidPick(game,"VengefulGhost",game.playerXP.level);Require(first.numProjectiles==3&&later.numProjectiles==2&&!later.targetMouse&&Mathf.Abs(Bonus(player.stats[StatType.SummonDamage])-.3f)<.001f,"repeat Vengeful Ghost increments currently present Ghosts, assigns automatic targeting and repeats its global15% damage");
   yield return AndroidPick(game,"BestFriends",game.playerXP.level);Require(Mathf.Abs(GhostFactorValue(first)-2.7f)<.001f&&Mathf.Abs(GhostFactorValue(later)-1)<.001f,"repeated source acquisition reuses an old factor and initializes only the new Ghost consumer");
   while(!(bool)WindField(GhostFactor(later),"Subscribed"))yield return null;
   this.PostNotification("EventStatChange",null);Require(Ghosts().All(g=>Mathf.Abs(GhostFactorValue(g)-2.8f)<.001f),"one explicitly dispatched source notification updates both independent Ghost consumers");
   yield return AndroidPick(game,"MagicSpear",game.playerXP.level);
  }
  else
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState)){var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));if(error!=null)throw new InvalidOperationException(error.text);yield return null;}
   player=PlayerController.Instance;Object.FindObjectOfType<HordeSpawner>().enabled=false;GameTimer.SharedInstance.Stop();
   Require(Ghosts().Length==2&&Ghosts()[0].numProjectiles==3&&Ghosts()[1].numProjectiles==2,"fresh-process Continue replays exact per-instance Ghost upgrade order");
   float[] clocks={.03f,.08f};for(int i=0;i<2;i++)Require(Mathf.Abs((float)WindField(Ghosts()[i],"_timer")-clocks[i])<.025f,"Continue restores the independent Ghost interval "+i);
   Require(Ghosts().All(g=>Mathf.Abs(GhostFactorValue(g)-2.8f)<.001f),"Continue restores held source callback factors without eagerly recalculating them");
  }
  var ghosts=Ghosts();foreach(var ghost in ghosts)ghost.enabled=false;var spears=player.GetComponentsInChildren<StabbingSummon>(true);while(spears.Any(s=>WindField(s,"stats")==null))yield return null;ControlSpears(spears,spears[0]);controlledSpear.gameObject.SetActive(false);
  Require(Mathf.Abs(player.stats[StatType.SummonDamage].Modify(20)-26)<.001f,"source Vengeful Ghost global bonus also changes another actual acquired summon");
  var pool=ObjectPooler.SharedInstance;var template=pool.itemsToPool.First(i=>i.objectToPool.CompareTag("Enemy")&&i.objectToPool.GetComponent<Health>()!=null&&i.objectToPool.GetComponent<MoveComponent2D>()!=null).objectToPool;pool.AddObject("Validation_Ghost_Enemy",template,1);var actor=pool.GetPooledObject("Validation_Ghost_Enemy");var target=DurableEnemy(actor,ghosts[0].transform.position+Vector3.right*3,false);actor.SetActive(true);target.GetComponent<Rigidbody2D>().constraints=RigidbodyConstraints2D.FreezeAll;actor.GetComponent<CircleCollider2D>().radius=1.5f;Log("fixture-precondition","Radius1.5 stationary target receives all three native spread projectiles; no whole-run accuracy claim");
  foreach(var projectile in Object.FindObjectsOfType<Projectile>())if(projectile.owner!=null)projectile.gameObject.SetActive(false);
  int hits=0,last=0;target.onHurt.AddListener(d=>{hits++;last=d;});Require(ShootingAttack(ghosts[0]),"actual upgraded Ghost attacks its pooled target");
  Require(Object.FindObjectsOfType<Projectile>().Count(p=>p.owner==ghosts[0].gameObject)==3,"actual native shooter creates the source upgraded three-projectile spread");while(hits<3)yield return null;
  Require(last==(continuation?20:10),"actual upgraded Ghost projectile contact uses the global source bonus and any saved Custom factor");actor.SetActive(false);
  foreach(var projectile in Object.FindObjectsOfType<Projectile>())if(ghosts.Any(g=>projectile.owner==g.gameObject))projectile.gameObject.SetActive(false);
  controlledSpear.gameObject.SetActive(true);var spearHits=new List<int>();target.onHurt.AddListener(d=>spearHits.Add(d));actor.SetActive(true);yield return SpearHit(target,spearHits,26,"source Vengeful global damage also reaches the acquired Magic Spear");actor.SetActive(false);controlledSpear.gameObject.SetActive(false);
  if(continuation)
  {
   player.stats[StatType.FireRate].AddMultiplierBonus(.2f);Require(Ghosts().All(g=>Mathf.Abs(GhostFactorValue(g)-2.8f)<.001f),"continued source factors hold across direct stat changes");this.PostNotification("EventStatChange",null);Require(Ghosts().All(g=>Mathf.Abs(GhostFactorValue(g)-3f)<.001f),"continued source observers still respond once to the named notification");
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();Require(!CustomMode.Active&&Object.FindObjectsOfType<ShootingSummon>().Length==0&&Object.FindObjectsOfType<MonoBehaviour>().All(c=>c.GetType().Name!="AndroidBestFriends"),"ordinary restart clears all Ghost consumers and Custom overrides");yield return CheckRevolver(game,PlayerController.Instance,"android-ghost-upgrades-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Source Vengeful actual upgraded shots/global damage and recovered named-event Best Friends consumer survive Continue/restart; source emitter remains unproven");Application.Quit(0);yield break;
  }
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="CustomSettings"));Click(CustomButton("CustomTab4"));CustomValue(0,"2");CustomValue(1,"1.5");yield return Capture("android-ghost-upgrades-native-custom-controls.png");Click(CustomButton("CustomBack"));Click(game.pauseResumeButton);
  Require(Mathf.Abs(ShootingCooldown(ghosts[0])-.8f/2.8f/1.5f)<.001f,"real Custom speed composes with the held source attack effect");
  for(int i=0;i<2;i++)Plugin.SetField(ghosts[i],"_timer",new[]{.03f,.08f}[i]);PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Source Vengeful natural effects and Best Friends consumer/callback/order, native Custom controls and independent clocks saved; source event producer not claimed");Application.Quit(0);
 }
}
