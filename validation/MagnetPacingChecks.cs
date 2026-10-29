using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.Pickups;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 GameObject PacingOrb(Vector2 position)
 {var orb=ObjectPooler.SharedInstance.GetPooledObject("SmallXP");orb.transform.position=position;return orb;}
 IEnumerator CheckMagnetPacing(string mode)
 {
  bool continuation=mode=="magnet-pacing-continue";
  if(continuation)yield return CombatBuffContinue();else{RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());}
  var game=Object.FindObjectOfType<GameController>();var player=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();var run=game.GetComponent<ExplorationRun>();while(run.State==null)yield return null;
  foreach(var pickup in Object.FindObjectsOfType<XPPickup>())ReturnFixtureActor(pickup.gameObject);
  if(continuation)
  {
   var chest=run.Chests.Single(c=>c.Drop.reward==ExplorationReward.Experience);var symbol=run.ContentIcon(chest.Drop);
   Require(symbol.name=="ExplorationXPOrbs","Continue rebuilds the orb-cluster preview without changing the reward");yield return null;yield return null;
   Require(chest.GetComponent<ChestDirectionIndicator>().Marker.GetComponent<Image>().sprite==symbol,"restored native edge image uses the same XP cluster");
   yield return ExplorationContact(chest,game);Require(!run.State.drops.Any(d=>d.id==chest.Drop.id),"restored XP reward still collects through native contact");yield return AchievementExit(game,false);SetPhase("PASS");Application.Quit(0);yield break;
  }
  Plugin.SetField(game.playerXP,"<level>k__BackingField",1000);Plugin.SetField(game.playerXP,"xp",0f);
  var origin=(Vector2)player.transform.position;var far=PacingOrb(origin+Vector2.right*72);var near=PacingOrb(origin+Vector2.right*12);
  var ring=new List<GameObject>();for(int i=0;i<120;i++){float angle=i*Mathf.PI*2/120;ring.Add(PacingOrb(origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*36));}
  var orbs=new[]{near,far}.Concat(ring).ToArray();var positions=orbs.Select(o=>o.transform.position).ToArray();float start=Time.time;
  var magnet=ExplorationFixture(run,ExplorationReward.Magnet,origin);yield return ExplorationContact(magnet,game);
  if(mode=="magnet-pacing-baseline")
  {while(Time.time-start<.6f)yield return null;Require(far.activeSelf,"a distant chest-magnet orb remains queued after the old simultaneous 0.3-second burst");SetPhase("PASS");Application.Quit(0);yield break;}
  Require((float)WindField(far.GetComponent<AndroidPickup>(),"magnetDelay")>5.9f,"magnet wave assigns distance-proportional delay to the far orb");
  float beforePause=(float)WindField(far.GetComponent<AndroidPickup>(),"elapsed");game.pauseController.Pause();yield return null;yield return null;
  Require((float)WindField(far.GetComponent<AndroidPickup>(),"elapsed")==beforePause&&far.transform.position==positions[1],"native pause stops the waiting attraction wave");game.pauseController.UnPause();
  var log=new List<string>{"combat_time,remaining,collected_this_frame,frame_delta"};var remaining=new HashSet<GameObject>(orbs);int worst=0;float nearAt=0,farAt=0;
  while(remaining.Count>0)
  {
   yield return null;var collected=remaining.Where(o=>!o.activeSelf).ToArray();foreach(var o in collected){remaining.Remove(o);if(o==near)nearAt=Time.time-start;if(o==far)farAt=Time.time-start;}
   worst=Mathf.Max(worst,collected.Length);log.Add(string.Join(",",Time.time-start,remaining.Count,collected.Length,Time.deltaTime));
   Require(collected.Length<=Mathf.CeilToInt(Time.deltaTime*60)+3,"native magnet arrivals remain paced even for an equal-distance ring");
  }
  Require(nearAt>1&&nearAt<2&&farAt>6&&farAt<7,"near/far real XP arrives in distance order at the specified wave speed");
  Require(worst<orbs.Length/4,"magnet does not deliver the entire native XP field in one burst");File.WriteAllLines(Path.Combine(ResultDir,"magnet-arrivals.csv"),log);
  float total=(float)WindField(game.playerXP,"xp");Require(total>0&&game.playerXP.level==1000,"all XP arrives without triggering a controlled fixture level transition");
  var normal=PacingOrb(origin+Vector2.right*60);float old=total;normal.GetComponent<AndroidPickup>().GetType().GetMethod("TargetPick",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(normal.GetComponent<AndroidPickup>(),new object[]{player.gameObject});
  while(normal.activeSelf)yield return null;float one=(float)WindField(game.playerXP,"xp")-old;CombatClose(total,one*orbs.Length,"every original orb grants its unchanged XP exactly once");
  var waiting=PacingOrb(origin+Vector2.right*72);ElementCall(waiting.GetComponent<AndroidPickup>(),"MagnetPick",player.gameObject,6f);ElementCall(waiting.GetComponent<AndroidPickup>(),"TargetPick",player.gameObject);Require((float)WindField(waiting.GetComponent<AndroidPickup>(),"magnetDelay")==0,"ordinary pickup can claim a waiting wave orb immediately");
  float immediate=Time.time;while(waiting.activeSelf)yield return null;Require(Time.time-immediate<.5f,"ordinary pickup retains the native 0.3-second flight");
  var pooled=PacingOrb(origin+Vector2.right*72);ElementCall(pooled.GetComponent<AndroidPickup>(),"MagnetPick",player.gameObject,6f);ReturnFixtureActor(pooled);pooled.SetActive(true);yield return null;Require(WindField(pooled.GetComponent<AndroidPickup>(),"target")==null&&(float)WindField(pooled.GetComponent<AndroidPickup>(),"magnetDelay")==0,"pool reactivation has no delayed old attraction");ReturnFixtureActor(pooled);
  var xpChest=ExplorationFixture(run,ExplorationReward.Experience,origin+Vector2.right*70);var healing=ExplorationFixture(run,ExplorationReward.Healing,origin+Vector2.left*70);player.playerHealth.hp=player.playerHealth.maxHP;
  foreach(bool reborn in new[]{false,true})
  {
   if(ColorfulStyle.Instance.Enabled!=reborn)ColorfulStyle.Instance.Toggle();
   foreach(var size in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})
   {
    Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);yield return null;yield return null;
    var marker=xpChest.GetComponent<ChestDirectionIndicator>();var native=ObjectPooler.SharedInstance.pooledObjectsDictionary["SmallXP"][0].transform.Find("Bounce").GetComponent<SpriteRenderer>();
    Require(marker.Icon.name=="ExplorationXPOrbs"&&marker.Icon!=native.sprite&&marker.Marker.GetComponent<Image>().sprite==marker.Icon,"XP marker is the new orb cluster rather than the native cross");
    Require(healing.GetComponent<ChestDirectionIndicator>().Icon==marker.Icon,"full-health conversion previews the same truthful XP cluster");ExplorationMarkerBounds(run);yield return PoiCapture("xp-orb-marker-"+size.x+"-"+(reborn?"reborn":"original")+".png");
   }
  }
  run.State.drops.Remove(healing.Drop);run.Chests.Remove(healing);Object.Destroy(healing.gameObject);RunResume.Save();
  yield return DragonSuspend(game,"Paced XP field fully collected; native orb-cluster reward checkpoint");
 }
}
