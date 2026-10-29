using System;
using System.Linq;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.Pickups;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 static ExplorationReward RewardShown(ExplorationChest chest)=>(ExplorationReward)AccessTools.Property(typeof(ExplorationChest),"ShownReward").GetValue(chest);
 static List<Powerup> RewardEligible(ExplorationRun run,bool character=false)=>(List<Powerup>)AccessTools.Method(typeof(ExplorationRun),"Eligible").Invoke(run,new object[]{character});
 IEnumerator RewardIconReady(ExplorationChest chest,Sprite sprite,Color tint)
 {
  var marker=chest.GetComponent<ChestDirectionIndicator>();
  Log("await-reward-icon",chest.Drop.reward+" expected="+sprite.name+" tint="+tint+" actual="+marker.Icon?.name+" preview="+RewardShown(chest));
  while(marker.Marker==null||marker.Marker.GetComponent<Image>().sprite!=sprite||marker.Marker.GetComponent<Image>().color!=tint)yield return null;
  Require(marker.Icon==sprite&&marker.IconTint==tint,"actual exploration marker uses its native source sprite and tint");
 }
 void RewardPreviewColor(ExplorationChest chest,ExplorationReward expected)
 {
  Require(RewardShown(chest)==expected,"current preview category is "+expected);
  var marker=chest.GetComponent<ChestDirectionIndicator>();var color=ExplorationChestVisual.RewardColor(expected);
  Require(((RectTransform)WindField(marker,"Arrow")).GetComponent<TMP_Text>().color==color,"direction arrow follows the available reward");
  foreach(var glow in chest.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name.StartsWith("RewardGlow")))Require(glow.color.r==color.r&&glow.color.g==color.g&&glow.color.b==color.b,"nearby glow follows the available reward without changing its fog channels");
 }
 IEnumerator CheckRewardIcons(bool continuation)
 {
  var input=continuation?RunResume.Read():null;GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else{RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();}
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var run=game.GetComponent<ExplorationRun>();while(run.State==null)yield return null;
  var player=PlayerController.Instance;var health=player.playerHealth;
  var xp=ObjectPooler.SharedInstance.pooledObjectsDictionary["SmallXP"][0].transform.Find("Bounce").GetComponent<SpriteRenderer>();
  var heart=Resources.FindObjectsOfTypeAll<HPPickup>().Single(p=>!p.gameObject.scene.IsValid()&&p.name=="PF_HeartPickup").transform.Find("Bounce").GetComponent<SpriteRenderer>();
  var soul=Resources.FindObjectsOfTypeAll<SHPPickup>().Single(p=>!p.gameObject.scene.IsValid()&&p.name=="PF_SoulHeartPickup").transform.Find("Bounce").GetComponent<SpriteRenderer>();
  var powers=Resources.FindObjectsOfTypeAll<Powerup>();var vitality=powers.First(p=>p.name=="Vitality");
  Require(heart.sprite!=soul.sprite&&heart.sprite!=vitality.icon,"red-heart pickup, soul-heart pickup and Vitality upgrade have distinct native artwork");
  foreach(string name in new[]{"Vitality","Magnetism","FrostMage","RapidFire"})Log("reward-symbol-audit",name+"="+powers.First(p=>p.name==name).icon.name);
  Log("reward-pickup-audit","red="+heart.sprite.name+" soul="+soul.sprite.name+" XP="+xp.sprite.name+" tint="+xp.color);
  if(continuation)
  {
   Require(input.exploration.drops.Count==2&&run.State.drops.Count==2&&run.State.random==input.exploration.random,"fresh process retains the two fixed rewards and their RNG");
   foreach(var before in input.exploration.drops){var now=run.State.drops.Single(d=>d.id==before.id);Require(now.reward==before.reward&&now.x==before.x&&now.y==before.y&&now.power==before.power,"Continue preserves primary reward, exact position and selected upgrade without rerolling");}
   var healing=run.Chests.Single(c=>c.Drop.reward==ExplorationReward.Healing);var exhausted=run.Chests.Single(c=>c.Drop.reward==ExplorationReward.Upgrade);
   Require(health.hp==health.maxHP,"saved full red health restores");yield return RewardIconReady(healing,xp.sprite,xp.color);yield return RewardIconReady(exhausted,xp.sprite,xp.color);RewardPreviewColor(healing,ExplorationReward.Experience);RewardPreviewColor(exhausted,ExplorationReward.Experience);
   health.hp--;yield return RewardIconReady(healing,heart.sprite,heart.color);RewardPreviewColor(healing,ExplorationReward.Healing);
   int max=health.maxHP,soulHP=health.shp;yield return ExplorationContact(healing,game);
   Require(health.hp==max&&health.maxHP==max&&health.shp==soulHP,"continued healing restores one missing red heart without max/soul-heart changes");
   yield return ExplorationContact(exhausted,game);Require(game.CurrentState is CombatState,"continued exhausted upgrade grants XP rather than opening an empty upgrade menu");
   yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Fresh-process fixed reward identity, full-health/exhausted XP previews, damaged-heart preview and real collection");Application.Quit(0);yield break;
  }
  health.hp=health.maxHP-1;var origin=(Vector2)player.transform.position;
  var power=RewardEligible(run).First(p=>FallbackPool(game.powerupGenerator).Any(i=>i.powerup==p&&i.numTimeRepeatable==1));
  Log("fixed-upgrade-fixture",power.name+"/"+RunResume.Key(power));SetPhase("reward-icons-native-markers");
  var healingChest=ExplorationFixture(run,ExplorationReward.Healing,origin+Vector2.left*65);
  var xpChest=ExplorationFixture(run,ExplorationReward.Experience,origin+Vector2.up*80);
  var upgradeChest=ExplorationFixture(run,ExplorationReward.Upgrade,origin+Vector2.right*70,RunResume.Key(power));
  uint random=run.State.random;int nextId=run.State.nextId,wallet=PointsTracker.pts,maxHP=health.maxHP,soulHealth=health.shp;
  foreach(bool reborn in new[]{false,true})
  {
   if(ColorfulStyle.Instance.Enabled!=reborn)ColorfulStyle.Instance.Toggle();
   health.hp=maxHP-1;yield return RewardIconReady(healingChest,heart.sprite,heart.color);yield return RewardIconReady(xpChest,xp.sprite,xp.color);yield return RewardIconReady(upgradeChest,power.icon,Color.white);RewardPreviewColor(healingChest,ExplorationReward.Healing);
   ExplorationMarkerBounds(run);yield return Capture("reward-icons-damaged-"+(reborn?"reborn":"original")+".png");
   health.Heal(1);yield return RewardIconReady(healingChest,xp.sprite,xp.color);RewardPreviewColor(healingChest,ExplorationReward.Experience);
   Require(healingChest.Drop.reward==ExplorationReward.Healing,"full-health XP preview does not rewrite the stored healing roll");
   yield return Capture("reward-icons-full-"+(reborn?"reborn":"original")+".png");
  }
  SetPhase("reward-icons-viewports");foreach(var size in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})
  {
   Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);
   foreach(int zoom in new[]{0,100}){CameraZoom.Value=zoom;FeedbackCameras();yield return null;ExplorationMarkerBounds(run);yield return Capture("reward-icons-"+size.x+"-zoom"+zoom+".png");}
  }
  CameraZoom.Value=0;player.SetPosition(healingChest.transform.position+Vector3.right*2);health.hp=maxHP-1;yield return RewardIconReady(healingChest,heart.sprite,heart.color);yield return null;
  Require(healingChest.GetComponent<ExplorationChestVisual>().Strength>.99f,"nearby healing chest reaches its full content glow");RewardPreviewColor(healingChest,ExplorationReward.Healing);PoiVisualContract(healingChest);yield return Capture("reward-glow-healing.png");
  health.Heal(1);yield return RewardIconReady(healingChest,xp.sprite,xp.color);RewardPreviewColor(healingChest,ExplorationReward.Experience);PoiVisualContract(healingChest);yield return Capture("reward-glow-full-xp.png");
  player.playerPerks.Equip(power);game.powerupGenerator.RemoveFromPool(power);yield return RewardIconReady(upgradeChest,xp.sprite,xp.color);RewardPreviewColor(upgradeChest,ExplorationReward.Experience);
  Require(upgradeChest.Drop.reward==ExplorationReward.Upgrade&&upgradeChest.Drop.power==RunResume.Key(power),"exhausted upgrade preview preserves its original fixed upgrade and primary roll");
  Require(run.State.random==random&&run.State.nextId==nextId&&run.State.drops.Count==3&&PointsTracker.pts==wallet,"presentation changes do not draw RNG, spawn/claim rewards or alter credits");
  SetPhase("reward-icons-collections");maxHP=health.maxHP;soulHealth=health.shp;health.hp=maxHP;
  float requirement=(float)AccessTools.Property(game.playerXP.GetType(),"xpToLevel").GetValue(game.playerXP);float amount=game.playerXP.xpMultiplier.Modify(.3f*requirement);int level=game.playerXP.level;
  foreach(var chest in new[]{healingChest,xpChest,upgradeChest})
  {Plugin.SetField(game.playerXP,"xp",0f);yield return ExplorationContact(chest,game);Require(game.playerXP.level==level,"controlled 30-percent XP checks stay inside the starting level");CombatClose((float)WindField(game.playerXP,"xp"),amount,"real contact grants the original 30-percent XP amount and native multiplier");}
  Require(health.hp==maxHP&&health.maxHP==maxHP&&health.shp==soulHealth,"XP conversions do not change red/soul health or maximum hearts");
  var heal=ExplorationFixture(run,ExplorationReward.Healing,(Vector2)player.transform.position+Vector2.right*3);health.hp=maxHP-1;float prior=(float)WindField(game.playerXP,"xp");yield return ExplorationContact(heal,game);
  Require(health.hp==maxHP&&health.maxHP==maxHP&&health.shp==soulHealth&&(float)WindField(game.playerXP,"xp")==prior,"actual damaged-health collection heals one red heart with no XP or capacity change");
  Require(((TMP_Text)WindField(run,"notice")).text=="Healed 1 heart","collection notice describes healing rather than additional maximum hearts");
  var available=RewardEligible(run).First();RunResume.Save();int acquired=RunResume.Read().acquired.Count;
  var menu=ExplorationFixture(run,ExplorationReward.Upgrade,(Vector2)player.transform.position+Vector2.right*3,RunResume.Key(available));yield return ExplorationContact(menu,game,false);
  RunResume.Save();Require(RunResume.Read().acquired.Count==acquired+1&&RunResume.Read().acquired.Last().key==RunResume.Key(available),"eligible fixed reward retains the native controller-accessible upgrade menu and acquisition");
  health.hp=health.maxHP;
  ExplorationFixture(run,ExplorationReward.Healing,(Vector2)player.transform.position+Vector2.left*65);
  ExplorationFixture(run,ExplorationReward.Upgrade,(Vector2)player.transform.position+Vector2.right*70,RunResume.Key(power));
  yield return DragonSuspend(game,"Complete native pickup icon/tint audit, dynamic health and exhausted-upgrade previews, true collection amounts, unchanged ownership and saved fixed rewards");
 }
 IEnumerator CheckRewardIconAudit()
 {
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  var pickups=Resources.FindObjectsOfTypeAll<MonoBehaviour>().Where(p=>p.GetType().Namespace=="flanne.Pickups"&&!p.gameObject.scene.IsValid()).OrderBy(p=>p.name).ToArray();
  var hud=StatsOverlay.Instance;var panel=StatsOverlay.Rect(new GameObject("RewardIconAudit",typeof(RectTransform),typeof(Image)),hud.Panel.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(760,430));panel.GetComponent<Image>().color=new Color(.13f,.11f,.17f,1);
  int index=0;
  foreach(var pickup in pickups)
  {
   Log("native-pickup",pickup.GetType().FullName+"/"+pickup.name);
   foreach(var field in pickup.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).Where(f=>f.FieldType==typeof(bool)||f.FieldType==typeof(int)||f.FieldType==typeof(Sprite)))Log("native-pickup-field",pickup.name+"/"+field.Name+"="+field.GetValue(pickup));
   foreach(var renderer in pickup.GetComponentsInChildren<SpriteRenderer>(true))
   {
    Log("native-pickup-art",pickup.name+"/"+renderer.name+" sprite="+renderer.sprite?.name+" color="+renderer.color+" material="+renderer.sharedMaterial?.name);
    if(renderer.sprite==null||renderer.name.Contains("Fog")||renderer.name.Contains("Shadow"))continue;
    float x=-315+(index%5)*155,y=160-(index/5)*95;index++;
    var icon=StatsOverlay.Rect(new GameObject(pickup.name+"Icon",typeof(RectTransform),typeof(Image)),panel,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(x,y),new Vector2(40,40));icon.GetComponent<Image>().sprite=renderer.sprite;icon.GetComponent<Image>().color=renderer.color;icon.GetComponent<Image>().preserveAspect=true;
    var text=StatsOverlay.Text(panel,pickup.name+"Label",hud.Font,pickup.name+"\n"+renderer.sprite.name,10,new Vector2(x-67,y-52),new Vector2(140,40));text.alignment=TMPro.TextAlignmentOptions.Center;
   }
  }
  yield return Capture("native-pickup-icons.png");Object.Destroy(panel.gameObject);
  SetPhase("PASS");Log("complete","Native pickup artwork inventory only; diagnostic, not corrected-feature acceptance");Application.Quit(0);
 }
}
