using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 EliteTierProgress TierProgress=>(EliteTierProgress)WindField(Object.FindObjectOfType<EliteRun>(),"Progress");
 int Tier(Health h)=>Enhanced(h)?(int)WindField(h.GetComponent<EnhancedEnemy>(),"Tier"):0;
 void TierOpportunity()=>Plugin.SetField(ScoreCalculator.SharedInstance,"_enemiesKilled",(EliteClaims+1)*200);
 void TierDeath(Health health,int tier)
 {
  ClearEliteXP();int kills=NativeKills,first=TierProgress.eliteKills,second=TierProgress.plusKills;
  health.TakeDamage(DamageType.Bullet,100000000);
  Require(NativeKills==kills+1&&LiveXP().Length==(tier==1?3:tier==2?9:27),"tier"+tier+" actual death is one native kill with proportional XP drops");
  Require(TierProgress.eliteKills==first+(tier==1?1:0)&&TierProgress.plusKills==second+(tier==2?1:0),"only the actual defeated tier advances its next-tier cadence");
 }
 IEnumerator TierEye(GameController game,Health eye,int tier,Vector3 pose,Vector3 collider,int hp)
 {
  int copies=tier==1?3:tier==2?9:27;float scale=tier==1?1.25f:tier==2?1.5625f:1.953125f;
  Require(Tier(eye)==tier&&eye.HP==hp*copies&&eye.maxHP==hp*copies,"tier"+tier+" repeats the health multiplier from final spawned HP");
  var ai=eye.GetComponent<AIComponent>();
  foreach(var direction in new[]{Vector2.left,Vector2.right})
  {AccessTools.Method(typeof(AIController),"AILookTowards").Invoke(AIController.SharedInstance,new object[]{ai,direction});CombatClose(Mathf.Abs(eye.transform.localScale.x),Mathf.Abs(pose.x)*scale,"tier"+tier+" native facing preserves compounded width");CombatClose(eye.transform.localScale.y,pose.y*scale,"tier"+tier+" native facing preserves compounded height");Require(eye.transform.localScale.z==pose.z,"tier growth preserves native Z");}
  Physics2D.SyncTransforms();CombatClose(eye.GetComponent<Collider2D>().bounds.size.x,collider.x*scale,"tier"+tier+" collider width follows artwork");CombatClose(eye.GetComponent<Collider2D>().bounds.size.y,collider.y*scale,"tier"+tier+" collider height follows artwork");
  eye.transform.position=PlayerController.Instance.transform.position+Vector3.right*2;eye.GetComponent<MoveComponent2D>().enabled=false;ai.specialTimer=0;EliteShots[ai]=new List<float>();
  while(EliteShots[ai].Count<4)yield return null;
  float expected=ai.specialCooldown*(tier==1?2f/3:tier==2?4f/9:8f/27);float interval=(EliteShots[ai][3]-EliteShots[ai][0])/3;
  Require(Mathf.Abs(interval-expected)<.12f,"tier"+tier+" actual Eye interval repeats the 1.5 frequency multiplier");
  ai.enabled=false;yield return null;var contour=eye.transform.Find("EliteContour").GetComponent<SpriteRenderer>();
  Require(contour.enabled&&contour.sprite.name=="EliteContour_"+eye.GetComponent<SpriteRenderer>().sprite.name,"tier"+tier+" cue follows actual animated silhouette");
  if(tier==2)Require(contour.color.r>contour.color.g&&contour.color.g>.4f&&contour.color.b<.15f,"Elite+ has an amber contour");
  if(tier==3)Require(contour.color.b>contour.color.r&&contour.color.r>contour.color.g,"Elite++ has a violet contour");
 }
 IEnumerator CheckEliteTiers(string mode)
 {
  if(mode=="elites-tiers-continue")yield return CombatBuffContinue();
  else{RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());}
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var source=Object.FindObjectOfType<AndroidEndlessRun>();WorldClearSchedule(source);
  Require(SelectedMap.MapData.endless&&!CustomMode.Active,"tier checks use the real Endless scenario");Plugin.SetField(game.playerXP,"<level>k__BackingField",50);
  if(!PlayerController.Instance.playerHealth.isInvincible.value)PlayerController.Instance.playerHealth.isInvincible.Flip();
  if(mode=="elites-tiers-fresh")
  {
   Require(EliteClaims==0&&TierProgress.eliteKills==0&&TierProgress.plusClaims==0&&TierProgress.plusKills==0&&TierProgress.doubleClaims==0,"new Endless starts with no earned tier opportunities");
   var eye=EliteSpawn("EyeMonster");int hp=eye.maxHP;var pose=eye.transform.localScale;Physics2D.SyncTransforms();var size=eye.GetComponent<Collider2D>().bounds.size;ReturnFixtureActor(eye.gameObject);
   var ordinary=EliteSpawn();ordinary.TakeDamage(DamageType.Bullet,100000000);Require(TierProgress.eliteKills==0&&TierProgress.plusKills==0,"ordinary deaths do not earn a higher tier");ClearEliteXP();
   // Seed only native kill boundaries to avoid a ten-thousand-kill playthrough.
   // Every elite defeat and every higher-tier entitlement below is actual.
   for(int i=1;i<=50;i++)
   {
    TierOpportunity();var first=EliteSpawn();Require(Tier(first)==1,"owed 200-kill opportunity promotes tier1 before defeat "+i);TierDeath(first,1);ClearEliteXP();
    Require(TierProgress.eliteKills==i,"actual tier1 defeat count is exact at "+i);
    if(i%10!=0){Require(Tier(EliteSpawn())==0,"no premature Elite+ at "+i);ElementClear();}
    else
    {
     TierOpportunity();int claimed=EliteClaims;var excluded=EliteSpawn("ElderBrain",1000,true);Require(Tier(excluded)==0&&EliteClaims==claimed,"scheduled miniboss leaves all owed promotions intact");ReturnFixtureActor(excluded.gameObject);
     ElementCall(source,"SpawnBoss",new object[]{null});var boss=Object.FindObjectsOfType<Health>().First(h=>h.GetComponent<AndroidHealthEvents>()?.CreatureType==2);Require(!Enhanced(boss)&&EliteClaims==claimed,"actual Endless boss excludes ordinary tier promotion");ReturnFixtureActor(boss.gameObject);((IDictionary)WindField(source,"liveBosses")).Clear();
     var plus=EliteSpawn("EyeMonster");Require(Tier(plus)==2&&EliteClaims==claimed&&TierProgress.plusClaims==i/10,"Elite+ takes priority without consuming the pending 200-kill claim");
     if(i==10)yield return TierEye(game,plus,2,pose,size,hp);
     TierDeath(plus,2);Require(plus.transform.localScale==pose&&!plus.transform.Find("EliteContour").GetComponent<SpriteRenderer>().enabled,"Elite+ death restores exact ordinary geometry and hides its contour");
     yield return EliteCollect(game,LiveXP(),9);plus.GetComponent<AIComponent>().enabled=true;plus.GetComponent<MoveComponent2D>().enabled=true;
     if(i<50){var reuse=EliteSpawn("EyeMonster");Require(Tier(reuse)==1&&EliteClaims==claimed+1,"the retained lower-tier opportunity is claimed by the following eligible actor");int count=TierProgress.eliteKills;ReturnFixtureActor(reuse.gameObject);Require(TierProgress.eliteKills==count,"despawn/pool return is not a defeated Elite");}
    }
    yield return null;
   }
   Require(TierProgress.eliteKills==50&&TierProgress.plusClaims==5&&TierProgress.plusKills==5&&TierProgress.doubleClaims==0,"fifty actual tier1 and five actual Elite+ defeats earn exactly one pending Elite++");
   TierOpportunity();RunResume.Save();var snapshot=RunResume.Read();Require(snapshot.eliteTiers.eliteKills==50&&snapshot.eliteTiers.plusKills==5&&snapshot.eliteTiers.doubleClaims==0&&snapshot.kills/200>snapshot.eliteClaims,"actual checkpoint records all counters and pending lower/higher claims");
   Log("fixture-precondition","Native kills seeded to each200 boundary; all50 Elite and5 Elite+ defeats, claims, XP and native death paths are actual. Scheduler disabled; no natural pacing/balance claim.");
   yield return DragonSuspend(game,"Endless repeated tier cadence, actual defeats/XP, priority, boss exclusion, compounded Eye geometry/frequency and exact pending checkpoint");yield break;
  }
  Require(TierProgress.eliteKills==50&&TierProgress.plusClaims==5&&TierProgress.plusKills==5&&TierProgress.doubleClaims==0,"fresh-process Continue restores every exact earned tier counter");
  var normal=EliteSpawn("EyeMonster",24,true);int baseHp=normal.maxHP;var ordinaryPose=normal.transform.localScale;Physics2D.SyncTransforms();var ordinarySize=normal.GetComponent<Collider2D>().bounds.size;ReturnFixtureActor(normal.gameObject);
  // The controlled saved clock remains in the initial Endless health band.
  baseHp=Mathf.FloorToInt(Object.FindObjectOfType<HordeSpawner>().healthMultiplier*24*1);
  int before=EliteClaims;var highest=EliteSpawn("EyeMonster");Require(Tier(highest)==3&&TierProgress.doubleClaims==1&&EliteClaims==before,"resumed Elite++ claim wins priority and preserves the lower claim");
  yield return TierEye(game,highest,3,ordinaryPose,ordinarySize,baseHp);
  var lowest=EliteSpawn();Require(Tier(lowest)==1&&EliteClaims==before+1,"pending ordinary Elite is still available after Elite++");lowest.GetComponent<AIComponent>().enabled=false;lowest.GetComponent<MoveComponent2D>().enabled=false;
  // Earn the sixth Elite+ while retaining the two other living tiers for an
  // actual three-tier visual comparison.
  for(int i=0;i<10;i++){TierOpportunity();var next=EliteSpawn();Require(Tier(next)==1,"new 200-kill claims retain tier1 cadence");TierDeath(next,1);ClearEliteXP();yield return null;}
  var middle=EliteSpawn("EyeMonster");Require(Tier(middle)==2&&TierProgress.plusClaims==6,"the next ten actual Elite defeats earn the next Elite+");middle.GetComponent<AIComponent>().enabled=false;middle.GetComponent<MoveComponent2D>().enabled=false;
  Vector3 origin=PlayerController.Instance.transform.position;lowest.transform.position=origin+new Vector3(-3,1);middle.transform.position=origin+new Vector3(0,1);highest.transform.position=origin+new Vector3(3,1);
  foreach(bool reborn in new[]{false,true})
  {
   if(ColorfulStyle.Instance.Enabled!=reborn)ColorfulStyle.Instance.Toggle();yield return null;
   foreach(var viewport in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})
   {Screen.SetResolution(viewport.x,viewport.y,false);yield return WaitDisplay(viewport.x,viewport.y);yield return PoiCapture("elite-tiers-"+(reborn?"reborn":"original")+"-"+viewport.x+".png");}
  }
  var glow=highest.transform.Find("EliteContour").GetComponent<SpriteRenderer>();game.pauseController.Pause();game.ChangeState<PauseState>();yield return null;var paused=glow.color;yield return null;Require(glow.color==paused,"native pause stops higher-tier contour animation");yield return MenuPointer(game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;
  TierDeath(highest,3);Require(highest.transform.localScale==ordinaryPose&&!glow.enabled,"Elite++ death restores exact pre-promotion scale and removes glow");yield return EliteCollect(game,LiveXP(),27);
  highest.GetComponent<AIComponent>().enabled=true;highest.GetComponent<MoveComponent2D>().enabled=true;
  var reused=EliteSpawn("EyeMonster");Require(reused==highest&&Tier(reused)==0&&reused.maxHP==baseHp&&reused.transform.localScale==ordinaryPose,"tier3 pool member reuses ordinary health/size without retained tier");
  var ai=reused.GetComponent<AIComponent>();reused.transform.position=origin+Vector3.right*2;reused.GetComponent<MoveComponent2D>().enabled=false;ai.specialTimer=0;EliteShots[ai]=new List<float>();while(EliteShots[ai].Count<3)yield return null;
  Require(Mathf.Abs((EliteShots[ai][2]-EliteShots[ai][0])/2-ai.specialCooldown)<.12f,"ordinary reuse restores native Eye frequency after tier3");ElementClear();
  yield return AchievementExit(game,false);RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();
  Require(EliteClaims==0&&TierProgress.eliteKills==0&&TierProgress.plusClaims==0&&TierProgress.plusKills==0&&TierProgress.doubleClaims==0&&!Enhanced(EliteSpawn()),"next Endless run resets every tier entitlement");ElementClear();yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete","Exact pending Continue, Elite++ health/XP/geometry/frequency, all-tier cues/styles/viewports, pool reuse, actual next Elite+ and fresh-run reset");Application.Quit(0);
 }
}
