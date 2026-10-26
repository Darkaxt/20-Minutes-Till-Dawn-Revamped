using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TMPro;
using flanne;
using flanne.Core;
using flanne.Pickups;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckExplorationContract()
 {
  SetPhase("exploration-weight-contract");
  var type=AccessTools.TypeByName("TwentyMinutes.Modernization.ExplorationRun");
  Require(type!=null,"exploration rewards are integrated into the native run");
  var roll=AccessTools.Method(type,"RewardForRoll");var expected=new[]{"Experience","Credits","Healing","Magnet","Freeze","Frenzy","Upgrade","CharacterUpgrade"};var weights=new[]{25,20,15,12,10,8,8,2};
  int from=0;for(int i=0;i<weights.Length;i++){for(int n=from;n<from+weights[i];n++)Require(roll.Invoke(null,new object[]{n}).ToString()==expected[i],"weighted reward interval "+expected[i]+" roll "+n);from+=weights[i];}
  Require(from==100,"reward weights cover the whole draw exactly");
  SetPhase("PASS");Application.Quit(0);yield break;
 }
 ExplorationChest ExplorationFixture(ExplorationRun run,ExplorationReward reward,Vector2 position,string power=null)
 {
  Require(run.State.drops.Count<ExplorationRun.Maximum,"fixture remains inside the actual three-chest cap");
  var drop=new ExplorationDrop{id=run.State.nextId++,reward=reward,x=position.x,y=position.y,power=power};run.State.drops.Add(drop);
  return (ExplorationChest)AccessTools.Method(typeof(ExplorationRun),"Create").Invoke(run,new object[]{drop});
 }
 IEnumerator ExplorationContact(ExplorationChest chest,GameController game,bool pointer=true)
 {
  PlayerController.Instance.SetPosition(chest.transform.position);Physics2D.SyncTransforms();
  while(chest!=null&&!chest.Claimed)yield return new WaitForFixedUpdate();
  Require(chest==null||chest.Claimed,"real player/chest physics contact claims the reward");
  yield return null;
  yield return ExplorationConfirmXP(game);
  if(game.CurrentState is ExplorationRewardState)
  {
   var take=(Button)WindField(game.chestUIController,"takeButton");while(!take.IsInteractable()||take.GetComponentsInParent<CanvasGroup>().Any(g=>g.alpha<.99f))yield return null;
   yield return Capture("exploration-native-upgrade.png");
   if(pointer)yield return MenuPointer(take);else{var pad=InputSystem.AddDevice<Gamepad>("ExplorationPad");yield return PadClick(pad,game.chestUIController.transform,take);}
   while(!(game.CurrentState is CombatState))yield return null;
  }
 }
 IEnumerator ExplorationConfirmXP(GameController game)
 {
  if(game.CurrentState is PowerupMenuState)
  {
   while(WindField(game.powerupMenu,"ConfirmEvent")==null)yield return null;
   while(game.powerupMenu.GetComponentsInParent<CanvasGroup>().Any(g=>g.alpha<.99f))yield return null;
   var confirm=Resources.FindObjectsOfTypeAll<Button>().First(b=>b.gameObject.activeInHierarchy&&Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentTarget(i)==game.powerupMenu&&b.onClick.GetPersistentMethodName(i)=="OnConfirmClicked"));
   yield return MenuPointer(confirm);while(!(game.CurrentState is CombatState))yield return null;
  }
 }
 void ExplorationMarkerBounds(ExplorationRun run)
 {
  var safe=Screen.safeArea;var markers=Object.FindObjectsOfType<ChestDirectionIndicator>().Where(t=>t.Marker!=null&&t.Marker.gameObject.activeSelf).ToArray();
  foreach(var target in markers)
  {
   var label=target.Marker.Find("Distance").GetComponent<TMP_Text>();int expected=Mathf.CeilToInt(Vector2.Distance(target.transform.position,PlayerController.Instance.transform.position));
   Require(label.text==expected.ToString(),"edge distance is the actual rounded player/world-target distance");
   foreach(var graphic in target.Marker.GetComponentsInChildren<Graphic>())
   {var corners=new Vector3[4];graphic.rectTransform.GetWorldCorners(corners);var points=corners.Select(p=>RectTransformUtility.WorldToScreenPoint(null,p)).ToArray();Log("marker-geometry",graphic.name+" safe="+safe+" bounds="+string.Join(";",points.Select(p=>p.ToString()))+" raycast="+graphic.raycastTarget+" root="+StatsOverlay.Instance.Panel.GetComponent<RectTransform>().rect);Require(points.All(p=>safe.Contains(p))&&!graphic.raycastTarget,"complete icon/arrow/distance fits safe area without taking input");}
   Require(!target.Exploration||target.Marker.GetComponent<Image>().color==ExplorationRun.Mint,"exploration and boss marker palettes remain distinguishable");
  }
  for(int a=0;a<markers.Length;a++)for(int b=a+1;b<markers.Length;b++)Require(Vector2.Distance(HudPoint(markers[a].Marker),HudPoint(markers[b].Marker))>=65*markers[a].Marker.lossyScale.x,"simultaneous edge markers are separated, including coincident directions");
 }
 IEnumerator CheckExploration(string mode)
 {
  SetPhase("exploration-integrated");RunResume.Snapshot input=null;
  if(mode!="exploration-fresh")input=RunResume.Read();
  if(input!=null)yield return CombatBuffContinue();else{RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());}
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var player=PlayerController.Instance;var run=game.GetComponent<ExplorationRun>();
  while(run.State==null||StatsOverlay.Instance.Panel==null)yield return null;
  // Native boss drops are restored near the saved player. These tests exercise
  // exploration persistence, so move before their next physical pickup step.
  if(input!=null){player.SetPosition(player.transform.position+new Vector3(100,100,0));Physics2D.SyncTransforms();}
  if(mode=="exploration-legacy")
  {
   Require(input.exploration==null&&run.State.drops.Count==0&&run.State.credits==0&&run.State.frenzy==0&&run.State.remaining>=59&&run.State.remaining<=90,"original1.1.7 checkpoint gains an empty valid exploration state without rewriting acquisitions");
   Require(run.State.random!=0,"legacy feature initializes its separate RNG");yield return DragonSuspend(game,"Old source checkpoint initializes exploration without changing native acquisition replay");yield break;
  }
  if(mode=="exploration-continue")
  {
   Require(input.exploration!=null&&run.State.random==input.exploration.random&&run.State.credits==input.exploration.credits&&run.State.drops.Count==input.exploration.drops.Count,"Continue preserves exploration RNG, earned credits and unclaimed inventory");
   foreach(var before in input.exploration.drops){var now=run.State.drops.Single(d=>d.id==before.id);Require(now.reward==before.reward&&now.x==before.x&&now.y==before.y&&now.power==before.power,"Continue preserves exact reward, upgrade key and world position");}
   Require(Mathf.Abs(run.State.remaining-input.exploration.remaining)<.5f&&Mathf.Abs(run.State.frenzy-input.exploration.frenzy)<.5f,"Continue retains spawn/frenzy remaining combat time");
   float expectedDamage=float.Parse(File.ReadAllText(Path.Combine(Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR"),"exploration-damage.txt")),System.Globalization.CultureInfo.InvariantCulture);
   CombatClose(Bonus(player.stats[StatType.BulletDamage]),expectedDamage,"Continue applies exactly the saved source bonuses and one exploration frenzy");
   float priorDamage=Bonus(player.stats[StatType.BulletDamage]);run.State.frenzy=.001f;while(run.State.frenzy>0)yield return null;CombatClose(Bonus(player.stats[StatType.BulletDamage]),priorDamage-.25f,"restored frenzy expires through its one native stat contribution");
   int wallet=PointsTracker.pts,score=ScoreCalculator.SharedInstance.GetScore().totalScore,credits=run.State.credits;
   Require(credits>=50&&score>=credits,"saved exploration credits contribute once to native score");
   yield return AchievementExit(game,false);Require(PointsTracker.pts-wallet==score,"native end-of-run wallet pays exploration credits exactly once with normal score");
   Require(ExplorationRun.Instance==null&&!Object.FindObjectsOfType<ChestDirectionIndicator>().Any(),"scene teardown removes exploration owner, chests and marker targets");SetPhase("PASS");Application.Quit(0);yield break;
  }
  Log("fixture-precondition","Controlled combat clock/RNG, native Forest geometry, real contact/reward menus and source methods. No natural spawn frequency, balance or phone-runtime claim.");
  CameraZoom.Value=0;FeedbackCameras();run.State.random=3;var sourceRandom=JsonUtility.ToJson(UnityEngine.Random.state);AccessTools.Method(typeof(ExplorationRun),"SpawnWave").Invoke(run,null);
  Require(JsonUtility.ToJson(UnityEngine.Random.state)==sourceRandom,"exploration spawn does not consume source gameplay RNG");
  Require(run.Chests.Count>=2&&run.Chests.Count<=3,"common wave produces simultaneous chests within the three target cap on actual ground");
  var positions=run.Chests.Select(c=>(Vector2)c.transform.position-(Vector2)player.transform.position).ToArray();
  foreach(var position in positions){float travel=position.magnitude/player.finalMoveSpeed;Log("exploration-travel",travel.ToString("R")+" seconds at spawn walking speed");Require(travel>=19.99f&&travel<=30.01f,"open Forest exploration requires20–30 seconds of unobstructed travel");}
  for(int a=0;a<positions.Length;a++)for(int b=a+1;b<positions.Length;b++)Require(Vector2.Angle(positions[a],positions[b])>=90,"common wave rewards occupy different directions");
  foreach(var chest in run.Chests){var at=Camera.main.WorldToViewportPoint(chest.transform.position);Require(at.x<0||at.x>1||at.y<0||at.y>1,"new exploration chest lies beyond the real camera view");Require(chest.GetComponent<SpriteRenderer>().color==ExplorationRun.Mint&&chest.GetComponent<ChestPickup>()==null,"mint chest uses native art without a boss-reward trigger");}
  yield return null;yield return null;yield return Capture("exploration-markers-spread.png");
  var initial=run.Chests.ToArray();while(run.Chests.Count<3){var last=initial.Last().transform.position+(Vector3)Vector2.up*3;ExplorationFixture(run,ExplorationReward.Experience,last);}
  run.State.remaining=0;yield return null;yield return null;Require(run.State.drops.Count==3&&run.State.remaining>=59&&run.State.remaining<=90,"actual scheduler skips a full wave and schedules one future interval");
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return null;float paused=run.State.remaining;yield return null;yield return null;CombatClose(run.State.remaining,paused,"native pause does not advance exploration timer");yield return MenuPointer(game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;
  var boss=ObjectPooler.SharedInstance.GetPooledObject("Chest");boss.transform.position=player.transform.position+Vector3.right*100;
  foreach(var viewport in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})
  {
   Screen.SetResolution(viewport.x,viewport.y,false);yield return WaitDisplay(viewport.x,viewport.y);
   foreach(int zoom in new[]{0,25,50,75,100}){CameraZoom.Value=zoom;FeedbackCameras();yield return null;yield return null;ExplorationMarkerBounds(run);}
   yield return Capture("exploration-markers-"+viewport.x+".png");
  }
  CameraZoom.Value=0;FeedbackCameras();
  // Exact same direction must still produce readable independent markers.
  var stored=run.Chests.Select(c=>c.transform.position).ToArray();for(int i=0;i<run.Chests.Count;i++)run.Chests[i].transform.position=player.transform.position+Vector3.right*(80+i*10);yield return null;yield return null;ExplorationMarkerBounds(run);yield return Capture("exploration-markers-same-direction.png");for(int i=0;i<stored.Length;i++)run.Chests[i].transform.position=stored[i];
  foreach(var chest in run.Chests.ToArray())yield return ExplorationContact(chest,game);Require(run.State.drops.Count==0,"all common-wave rewards collect through native player physics");
  // Each catalog family runs through the same actual contact boundary.
  foreach(var reward in new[]{ExplorationReward.Credits,ExplorationReward.Healing,ExplorationReward.Magnet,ExplorationReward.Freeze,ExplorationReward.Frenzy,ExplorationReward.Upgrade,ExplorationReward.CharacterUpgrade})
  {
   player=PlayerController.Instance;var origin=(Vector2)player.transform.position;int hp=player.playerHealth.hp,score=run.State.credits;float damage=Bonus(player.stats[StatType.BulletDamage]);int acquired=RunResume.Read().acquired.Count;
   Health target=null;GameObject xp=null;string power=null;
   if(reward==ExplorationReward.Healing){player.playerHealth.hp=player.playerHealth.maxHP-1;hp=player.playerHealth.hp;}
   if(reward==ExplorationReward.Magnet){xp=ObjectPooler.SharedInstance.GetPooledObject("SmallXP");xp.transform.position=player.transform.position+Vector3.right*8;}
   if(reward==ExplorationReward.Freeze){target=HitEffectTarget(maximum:10000);target.transform.position=player.transform.position+Vector3.right*5;}
   if(reward==ExplorationReward.Upgrade||reward==ExplorationReward.CharacterUpgrade)power=RunResume.Key(((List<Powerup>)AccessTools.Method(typeof(ExplorationRun),"Eligible").Invoke(run,new object[]{reward==ExplorationReward.CharacterUpgrade})).First());
   var chest=ExplorationFixture(run,reward,origin+Vector2.right*2,power);yield return ExplorationContact(chest,game,reward!=ExplorationReward.CharacterUpgrade);
   if(reward==ExplorationReward.Credits){Require(run.State.credits==score+50,"credit reward adds50 to the saved native-payout score");yield return null;var notice=(TMP_Text)WindField(run,"notice");var at=HudPoint(notice.rectTransform);Require(at.y>30&&at.y<Screen.height*.25f,"real reward notice fits below playfield without covering native level/timer");yield return Capture("exploration-credit-notice.png");}
   if(reward==ExplorationReward.Healing)Require(player.playerHealth.hp==hp+1,"healing reward restores one real red heart");
   if(reward==ExplorationReward.Magnet){while(xp.activeSelf)yield return null;yield return ExplorationConfirmXP(game);Require(!xp.activeSelf,"magnet uses real XP pickup flight and collection");}
   if(reward==ExplorationReward.Freeze){Require(FreezeSystem.SharedInstance.IsFrozen(target.gameObject),"freeze reward invokes native enemy freeze");ReturnFixtureActor(target.gameObject);}
   if(reward==ExplorationReward.Frenzy){CombatClose(Bonus(player.stats[StatType.BulletDamage]),damage+.25f,"frenzy grants one25-percent native damage modifier");var repeat=ExplorationFixture(run,reward,(Vector2)player.transform.position+Vector2.up*2);yield return ExplorationContact(repeat,game);CombatClose(Bonus(player.stats[StatType.BulletDamage]),damage+.25f,"second frenzy refreshes duration without stacking");run.State.frenzy=.001f;while(run.State.frenzy>0)yield return null;CombatClose(Bonus(player.stats[StatType.BulletDamage]),damage,"frenzy expiry removes only its own modifier");}
   if(power!=null){RunResume.Save();var acquisition=RunResume.Read().acquired;Require(acquisition.Count==acquired+1&&acquisition.Last().key==power,"native upgrade menu applies and records its fixed eligible reward");}
  }
  var full=ExplorationFixture(run,ExplorationReward.Healing,(Vector2)player.transform.position+Vector2.left*2);player.playerHealth.hp=player.playerHealth.maxHP;yield return ExplorationContact(full,game);
  var exhausted=ExplorationFixture(run,ExplorationReward.Upgrade,(Vector2)player.transform.position+Vector2.left*2,"unavailable-fixture-power");yield return ExplorationContact(exhausted,game);Require(game.CurrentState is CombatState,"unavailable fixed upgrade grants XP without an empty reward menu");
  var finalFrenzy=ExplorationFixture(run,ExplorationReward.Frenzy,(Vector2)player.transform.position+Vector2.up*2);yield return ExplorationContact(finalFrenzy,game);
  foreach(var pair in new[]{new{reward=ExplorationReward.Credits,offset=Vector2.right*80},new{reward=ExplorationReward.Experience,offset=Vector2.up*90},new{reward=ExplorationReward.Upgrade,offset=Vector2.left*100}})
  {string power=pair.reward==ExplorationReward.Upgrade?RunResume.Key(((List<Powerup>)AccessTools.Method(typeof(ExplorationRun),"Eligible").Invoke(run,new object[]{false})).First()):null;ExplorationFixture(run,pair.reward,(Vector2)player.transform.position+pair.offset,power);}
  run.State.remaining=47;RunResume.Save();var saved=RunResume.Read();Require(saved.exploration.drops.Count==3&&saved.exploration.credits==run.State.credits&&saved.exploration.credits>=50&&saved.exploration.frenzy>0,"real checkpoint includes exact active exploration state");
  File.WriteAllText(Path.Combine(ResultDir,"exploration-checkpoint.json"),JsonUtility.ToJson(saved.exploration,true));
  File.WriteAllText(Path.Combine(ResultDir,"exploration-damage.txt"),Bonus(player.stats[StatType.BulletDamage]).ToString("R",System.Globalization.CultureInfo.InvariantCulture));
  yield return DragonSuspend(game,"Complete weighted exploration, common waves/cap/geometry, all rewards/native menus, shared markers, combat timers and source checkpoint");
 }
 IEnumerator CheckExplorationMaps()
 {
  SetPhase("exploration-maps-custom");RunResume.Invalidate();
  foreach(string map in new[]{"20M_Temple","20M_PumpkinPatch","10M_Forest","Endless_Forest","Challenge_Forest"})
  {
   string gate="scenario:"+map;if(!Progression.State.earned.Contains(gate))Progression.State.earned.Add(gate);Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData",map,prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var run=game.GetComponent<ExplorationRun>();while(run.State==null)yield return null;FeedbackCameras();
   CameraZoom.Value=0;FeedbackCameras();run.State.random=3;AccessTools.Method(typeof(ExplorationRun),"SpawnWave").Invoke(run,null);
   if(run.Chests.Count==0)
   {
    var camera=Camera.main;var probeOrigin=(Vector2)PlayerController.Instance.transform.position;float diagonal=camera.orthographicSize*Mathf.Sqrt(1+camera.aspect*camera.aspect);Log("placement-camera",map+" origin="+probeOrigin+" ortho="+camera.orthographicSize+" aspect="+camera.aspect+" zoom="+CameraZoom.Value);
    for(int angle=0;angle<360;angle+=30){float r=angle*Mathf.Deg2Rad;var probePoint=probeOrigin+new Vector2(Mathf.Cos(r),Mathf.Sin(r))*diagonal*1.25f;foreach(var hit in Physics2D.LinecastAll(probeOrigin,probePoint))Log("placement-collider",angle+" "+hit.collider.name+" tag="+hit.collider.tag+" layer="+LayerMask.LayerToName(hit.collider.gameObject.layer)+" trigger="+hit.collider.isTrigger+" bounds="+hit.collider.bounds+" ai="+hit.collider.GetComponentInParent<AIComponent>()+" ignored="+Physics2D.GetIgnoreLayerCollision(PlayerController.Instance.gameObject.layer,hit.collider.gameObject.layer));}
    yield return Capture("exploration-map-no-placement-"+map+".png");
   }
   Require(run.Chests.Count>=1&&run.Chests.Count<=3,"native map supports off-screen reachable exploration without exceeding cap: "+map);
   foreach(var chest in run.Chests){var at=Camera.main.WorldToViewportPoint(chest.transform.position);Require(at.x<0||at.x>1||at.y<0||at.y>1,"map exploration starts beyond visible ground: "+map);}
   var first=run.Chests.First();first.Drop.reward=ExplorationReward.Credits;yield return ExplorationContact(first,game);Require(run.State.credits==50,"native map collects exploration credits: "+map);
   yield return Capture("exploration-map-"+map+".png");yield return AchievementExit(game,false);Require(ExplorationRun.Instance==null,"map exit clears the exploration owner: "+map);
  }
  RunResume.Invalidate();yield return OpenCustomConfiguration();Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>();yield return MenuPointer(CustomButton("CustomStart"));GameController custom;
  while((custom=Object.FindObjectOfType<GameController>())==null||!(custom.CurrentState is CombatState))yield return null;
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var player=PlayerController.Instance;var owner=custom.GetComponent<ExplorationRun>();while(owner.State==null)yield return null;string ownership=Ownership();
  Require(CustomMode.Active&&SelectedMap.MapData.achievementsDisabled,"exploration uses the actual no-award Custom scenario");
  var credit=ExplorationFixture(owner,ExplorationReward.Credits,(Vector2)player.transform.position+Vector2.right*2);yield return ExplorationContact(credit,custom);
  var choices=(List<Powerup>)AccessTools.Method(typeof(ExplorationRun),"Eligible").Invoke(owner,new object[]{false});string key=RunResume.Key(choices.First());var upgrade=ExplorationFixture(owner,ExplorationReward.Upgrade,(Vector2)player.transform.position+Vector2.up*2,key);yield return ExplorationContact(upgrade,custom,false);
  Require(Ownership()==ownership&&owner.State.credits==50,"Custom exploration preserves every unlock/achievement owner while allowing run rewards");
  FeedbackCameras();var search=AccessTools.Method(typeof(ExplorationRun),"FindPosition");object[] probe={0f,Vector2.zero};Require((bool)search.Invoke(owner,probe),"native ground has a valid clear placement ray");
  var origin=player.transform.position;var wall=new GameObject("ExplorationFixtureWall",typeof(BoxCollider2D));wall.transform.position=origin+Vector3.right*2;wall.GetComponent<BoxCollider2D>().size=new Vector2(2,200);Physics2D.SyncTransforms();
  probe=new object[]{0f,Vector2.zero};Require(!(bool)search.Invoke(owner,probe),"solid world geometry blocks the whole searched sector");wall.GetComponent<BoxCollider2D>().isTrigger=true;wall.tag="MapBounds";Physics2D.SyncTransforms();probe=new object[]{0f,Vector2.zero};Require(!(bool)search.Invoke(owner,probe),"native MapBounds trigger also blocks exploration placement");Object.Destroy(wall);yield return null;
  int wallet=PointsTracker.pts,score=ScoreCalculator.SharedInstance.GetScore().totalScore;yield return AchievementExit(custom,false);Require(PointsTracker.pts-wallet==score,"Custom native payout includes exploration credits once");
  SetPhase("PASS");Log("complete","All native scenarios, Custom obtained/no-award ownership, real rewards/menu/payout and explicit solid/MapBounds geometry");Application.Quit(0);
 }
}
