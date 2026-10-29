using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
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
 IEnumerator CheckExplorationContents()
 {
  Log("native-pickup-types",string.Join(",",typeof(HPPickup).Assembly.GetTypes().Where(t=>t.Namespace=="flanne.Pickups"&&typeof(MonoBehaviour).IsAssignableFrom(t)).Select(t=>t.Name).OrderBy(n=>n)));
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var run=game.GetComponent<ExplorationRun>();while(run.State==null)yield return null;
  var counts=Enumerable.Range(0,100).Select(ExplorationRun.RewardForRoll).GroupBy(r=>r).ToDictionary(g=>g.Key,g=>g.Count());
  Require(!counts.ContainsKey(ExplorationReward.Credits)&&counts.Count==7&&counts[ExplorationReward.Experience]==45,"all100 weighted rolls exclude credits and transfer their20-percent share to XP");
  Require(counts[ExplorationReward.Healing]==15&&counts[ExplorationReward.Magnet]==12&&counts[ExplorationReward.Freeze]==10&&counts[ExplorationReward.Frenzy]==8&&counts[ExplorationReward.Upgrade]==8&&counts[ExplorationReward.CharacterUpgrade]==2,"all other reward weights retain their exact prior shares");
  var powers=Resources.FindObjectsOfTypeAll<Powerup>();var upgrade=((List<Powerup>)AccessTools.Method(typeof(ExplorationRun),"Eligible").Invoke(run,new object[]{false})).First();var character=((List<Powerup>)AccessTools.Method(typeof(ExplorationRun),"Eligible").Invoke(run,new object[]{true})).First();
  var xp=ObjectPooler.SharedInstance.pooledObjectsDictionary["SmallXP"][0].GetComponentInChildren<SpriteRenderer>(true).sprite;
  PlayerController.Instance.playerHealth.hp=PlayerController.Instance.playerHealth.maxHP-1;
  var heart=Resources.FindObjectsOfTypeAll<HPPickup>().Single(p=>!p.gameObject.scene.IsValid()&&p.name=="PF_HeartPickup").transform.Find("Bounce").GetComponent<SpriteRenderer>().sprite;
  var kinds=new[]{ExplorationReward.Experience,ExplorationReward.Healing,ExplorationReward.Magnet,ExplorationReward.Freeze,ExplorationReward.Frenzy,ExplorationReward.Upgrade,ExplorationReward.CharacterUpgrade};
  var expected=new[]{xp,heart,powers.First(p=>p.name=="Magnetism").icon,powers.First(p=>p.name=="FrostMage").icon,powers.First(p=>p.name=="RapidFire").icon,upgrade.icon,character.icon};
  Vector3 origin=PlayerController.Instance.transform.position;var directions=new[]{new Vector2(80,0),new Vector2(-60,60),new Vector2(-50,-80)};
  var boss=ObjectPooler.SharedInstance.GetPooledObject("Chest");boss.transform.position=origin+new Vector3(70,-10);
  var bossPointer=boss.GetComponent<ChestDirectionIndicator>();var bossIcon=(Sprite)AccessTools.Field(typeof(ChestPickup),"chestClosed").GetValue(boss.GetComponent<ChestPickup>());
  for(int batch=0;batch<3;batch++)
  {
   var chests=new List<ExplorationChest>();for(int i=0;i<3&&batch*3+i<kinds.Length;i++)
   {
    int index=batch*3+i;string key=kinds[index]==ExplorationReward.Upgrade?RunResume.Key(upgrade):kinds[index]==ExplorationReward.CharacterUpgrade?RunResume.Key(character):null;
    Require(expected[index]!=null,"native content artwork is resolved for "+kinds[index]);
    chests.Add(ExplorationFixture(run,kinds[index],(Vector2)origin+directions[i],key));
   }
   foreach(bool reborn in new[]{false,true})
   {
    if(ColorfulStyle.Instance.Enabled!=reborn)ColorfulStyle.Instance.Toggle();yield return null;yield return null;
    for(int i=0;i<chests.Count;i++)
    {
     var marker=chests[i].GetComponent<ChestDirectionIndicator>();Require(marker.Marker!=null&&marker.Marker.gameObject.activeSelf,"offscreen content has a live marker");var image=marker.Marker.GetComponent<Image>();
     Require(marker.Icon==expected[batch*3+i]&&image.sprite==marker.Icon&&image.color==marker.IconTint,"actual edge image shows the exact native content sprite and tint: "+chests[i].Drop.reward);
     Require(marker.Icon!=chests[i].GetComponent<SpriteRenderer>().sprite,"non-boss pointer no longer uses the chest artwork");
    }
    Require(bossPointer.Icon==bossIcon&&bossPointer.Marker.GetComponent<Image>().sprite==bossIcon,"native boss treasure retains its distinct chest pointer");
    ExplorationMarkerBounds(run);yield return PoiCapture("exploration-content-"+batch+"-"+(reborn?"reborn":"original")+".png");
   }
   if(batch==0)foreach(var size in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})
   {Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);foreach(int zoom in new[]{0,100}){CameraZoom.Value=zoom;FeedbackCameras();yield return null;yield return null;ExplorationMarkerBounds(run);yield return PoiCapture("exploration-content-"+size.x+"-zoom"+zoom+".png");}}
   RemovePoiFixture(run);yield return null;
  }
  CameraZoom.Value=0;FeedbackCameras();ReturnFixtureActor(boss);
  int credits=run.State.credits;var legacy=ExplorationFixture(run,ExplorationReward.Credits,(Vector2)origin+Vector2.right*2);var converted=legacy.Drop;
  Require(converted.reward==ExplorationReward.Experience&&legacy.GetComponent<ChestDirectionIndicator>().Icon==xp,"legacy credit drop converts before world/marker creation");
  int level=game.playerXP.level;float beforeXP=(float)WindField(game.playerXP,"xp");yield return ExplorationContact(legacy,game);
  Require(run.State.credits==credits&&(game.playerXP.level>level||(float)WindField(game.playerXP,"xp")>beforeXP),"converted credit chest actually grants XP and zero credits");
  yield return AchievementExit(game,false);Require(!Object.FindObjectsOfType<ChestDirectionIndicator>().Any(),"scene exit clears content and boss markers");SetPhase("PASS");Log("complete","Seven real content sprites, credit-free weights/collection, legacy creation, boss separation, both styles/viewports/zoom, real geometry and teardown");Application.Quit(0);
 }
}
