using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.Pickups;
using flanne.UI;
using flanne.UIExtensions;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator NativeTomePick(GameController game,bool actualBossDrop,string tome="TomeOfMight")
 {
  var pool=(List<PowerupPoolItem>)AccessTools.Field(typeof(PowerupGenerator),"devilPool").GetValue(game.powerupGenerator);
  var desired=pool.Single(p=>p.powerup.name==tome);
  var choices=pool.Where(p=>p!=desired).Take(2).Concat(new[]{desired}).ToList();
  AccessTools.Field(typeof(PowerupGenerator),"devilPool").SetValue(game.powerupGenerator,choices);
  if(actualBossDrop)
  {
   yield return AchievementBoss(game,0);
   DevilDealPickup drop;while((drop=Object.FindObjectOfType<DevilDealPickup>())==null)yield return null;
   PlayerController.Instance.transform.position=drop.transform.position;
  }
  else this.PostNotification(DevilDealPickup.DevilDealPickupEvent);
  while(!(game.CurrentState is DevilDealState)||AccessTools.Field(typeof(ToggleGroupMenu<Powerup>),"ConfirmEvent").GetValue(game.devilDealMenu)==null)yield return null;
  var toggles=(Toggle[])AccessTools.Field(typeof(ToggleGroupMenu<Powerup>),"toggles").GetValue(game.devilDealMenu);
  toggles.Take(3).Single(t=>t.GetComponent<DataUIBinding<Powerup>>().data==desired.powerup).isOn=true;
  Button confirm=null;
  while(confirm==null||!confirm.IsInteractable())
  {
   confirm=Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(b=>b.gameObject.activeInHierarchy&&Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentTarget(i)==game.devilDealMenu&&b.onClick.GetPersistentMethodName(i)=="OnConfirmClicked"));yield return null;
  }
  yield return Capture("tome-might-native-offer.png");Click(confirm);
  while(!(game.CurrentState is CombatState))yield return null;
  AccessTools.Field(typeof(PowerupGenerator),"devilPool").SetValue(game.powerupGenerator,pool);
 }
 static void TomeHit(Health target,int expected,string reason)
 {
  int before=target.HP;target.TakeDamage(DamageType.Bullet,20);int damage=before-target.HP;
  Log("native-tome-damage",reason+": "+damage+" current="+PlayerController.Instance.playerHealth.hp+" max="+PlayerController.Instance.playerHealth.maxHP);
  Require(damage==expected,"native Health damage pipeline follows Tome of Might recipe: "+reason);
 }
 IEnumerator CheckTomeMight(bool baseline)
 {
  yield return AchievementRun("Shana","RevolverData","20M_Forest");var game=Object.FindObjectOfType<GameController>();var player=PlayerController.Instance;
  Health template;while((template=Object.FindObjectsOfType<Health>().FirstOrDefault(h=>h.name.StartsWith("BrainMonster")&&!h.isDead))==null)yield return null;
  var target=DurableEnemy(template.gameObject,player.transform.position+Vector3.right*30,true);
  Object.FindObjectOfType<HordeSpawner>().enabled=false;
  // Preserve this known target across the real boss's ordinary cleanup.
  target.gameObject.SetActive(false);
  yield return NativeTomePick(game,true);target.gameObject.SetActive(true);target=DurableEnemy(target.gameObject,player.transform.position+Vector3.right*30,false);
  Require(player.playerHealth.maxHP==5&&player.playerHealth.hp==5,"native boss tome adds its original one maximum/current heart");TomeHit(target,30,"full five hearts");
  AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(player.playerHealth,new object[]{1});TomeHit(target,baseline?28:30,"injured four of five");
  yield return AchievementPick(game,"Vitality",game.playerXP.level);TomeHit(target,baseline?30:32,"Vitality, five of six hearts");
  player.playerHealth.Heal(1);TomeHit(target,32,"healed six of six");
  if(!baseline)
  {
   yield return NativeTomePick(game,false);TomeHit(target,48,"second native tome, seven maximum hearts");
   AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(player.playerHealth,new object[]{1});TomeHit(target,48,"two native modifiers remain based on maximum after injury");
  }
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();
  Require(PlayerController.Instance.playerHealth.maxHP==4,"original restart has no carried tome health");yield return CheckRevolver(game,PlayerController.Instance,"tome-might-original-restart.png");yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete",baseline?"Tome of Might native boss reward reproduces current versus maximum HP mismatch":"Tome of Might native boss reward, injured/healed/max-health changes, repeated native choice and original restart pass");Application.Quit(0);
 }
}
