using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 StabbingSummon controlledSpear;
 IEnumerator SpearHit(Health target,List<int> hits,int expected,string reason)
 {
  SetPhase("native-spear-hit-"+reason);hits.Clear();
  controlledSpear.transform.position=target.transform.position+Vector3.left*3;
  yield return new WaitForFixedUpdate();
  controlledSpear.transform.position=target.transform.position;
  Log("native-spear-contact", "spear="+controlledSpear.transform.position+" target="+target.transform.position+" spear-colliders="+controlledSpear.GetComponents<Collider2D>().Length+" target-colliders="+target.GetComponents<Collider2D>().Length+" scale="+Time.timeScale);
  yield return new WaitForFixedUpdate();
  var contact=controlledSpear.GetComponent<Collider2D>();var victim=target.GetComponent<Collider2D>();
  var sb=contact.attachedRigidbody;var tb=victim.attachedRigidbody;
  Log("native-spear-physics","spear active="+controlledSpear.gameObject.activeInHierarchy+" enabled="+contact.enabled+" trigger="+contact.isTrigger+" layer="+controlledSpear.gameObject.layer+" bounds="+contact.bounds+" body="+(sb==null?"none":sb.bodyType+" sim="+sb.simulated+" full="+sb.useFullKinematicContacts+" position="+sb.position)+" target active="+target.gameObject.activeInHierarchy+" enabled="+victim.enabled+" trigger="+victim.isTrigger+" layer="+target.gameObject.layer+" bounds="+victim.bounds+" body="+(tb==null?"none":tb.bodyType+" sim="+tb.simulated+" full="+tb.useFullKinematicContacts+" position="+tb.position)+" ignored="+Physics2D.GetIgnoreLayerCollision(contact.gameObject.layer,victim.gameObject.layer)+" hp="+target.HP+" hits="+hits.Count);
  while(hits.Count==0)yield return null;
  Log("native-spear-damage",reason+" hits="+string.Join(",",hits)+" currentHP="+PlayerController.Instance.playerHealth.hp+" maxHP="+PlayerController.Instance.playerHealth.maxHP);
  Require(hits.All(d=>d==expected),"actual native spear collision damage "+expected+": "+reason);
 }
 IEnumerator CheckHolySpear(bool baseline)
 {
  yield return AchievementRun("Shana","RevolverData",baseline?"20M_Forest":"Endless_Forest");var game=Object.FindObjectOfType<GameController>();var player=PlayerController.Instance;
  Health target;while((target=Object.FindObjectsOfType<Health>().FirstOrDefault(h=>h.name.StartsWith("BrainMonster")&&!h.isDead))==null)yield return null;
  Object.FindObjectOfType<HordeSpawner>().enabled=false;
  foreach(var other in Object.FindObjectsOfType<Health>().Where(h=>h!=target&&h.gameObject.layer==(int)TagLayerUtil.Enemy))other.gameObject.SetActive(false);
  target=DurableEnemy(target.gameObject,player.transform.position+Vector3.right*3,false);{var fixedBody=target.GetComponent<Rigidbody2D>();if(fixedBody!=null)fixedBody.constraints=RigidbodyConstraints2D.FreezeAll;}
  var hits=new List<int>();ObserveDamage(target,d=>hits.Add(d));
  yield return AchievementPick(game,"MagicSpear",game.playerXP.level);
  StabbingSummon[] spears;while((spears=Object.FindObjectsOfType<StabbingSummon>()).Length<2)yield return null;
  // Isolate the modifier from autonomous target choice; Unity still delivers
  // real physics collision messages to the initialized disabled behaviour.
  foreach(var spear in spears)spear.enabled=false;controlledSpear=spears[0];
  foreach(var extra in spears.Skip(1))extra.gameObject.SetActive(false);
  yield return AchievementPick(game,"HolySpear",game.playerXP.level);
  Require(player.playerHealth.maxHP==4&&player.playerHealth.hp==4&&Mathf.Abs(player.stats[StatType.SummonDamage].Modify(1)-1)<.001f,"controlled native four-heart Shana without summon-damage bonuses");
  yield return SpearHit(target,hits,60,"full health");
  AccessTools.Method(typeof(PlayerHealth),"TakeDamage").Invoke(player.playerHealth,new object[]{1});Require(player.playerHealth.hp==3&&player.playerHealth.maxHP==4,"actual native injury changes current hearts without maximum");
  yield return SpearHit(target,hits,baseline?50:60,"injured, same maximum");
  yield return AchievementPick(game,"Vitality",game.playerXP.level);
  Require(player.playerHealth.maxHP==5&&player.playerHealth.hp==4,"native Vitality increases maximum and current health by one");
  yield return SpearHit(target,hits,baseline?60:70,"increased maximum, still injured");
  if(!baseline)
  {
   yield return AchievementPick(game,"HolySpear",game.playerXP.level);yield return SpearHit(target,hits,120,"second native Endless pick");
   yield return AchievementPick(game,"HolySpear",game.playerXP.level);yield return SpearHit(target,hits,170,"third native Endless pick");
   Require(!((List<PowerupPoolItem>)AccessTools.Field(typeof(PowerupGenerator),"powerupPool").GetValue(game.powerupGenerator)).Any(e=>e.powerup.name=="HolySpear"),"native source allowance removes Holy Spear after third Endless pick");
  }
  yield return Capture("holy-spear-injured.png");yield return AchievementExit(game,false);
  yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();
  Require(Object.FindObjectsOfType<StabbingSummon>().Length==0,"new original run has no carried spear summons");yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete",baseline?"Native Holy Spear reproduces tooltip mismatch: 60 full, 50 injured, 60 after max-health increase":"Native Holy Spear follows documented Max HP: 60 full/injured, 70 after Vitality, 120/170 on finite native repeats; native exit/original restart clean");Application.Quit(0);
 }
}
