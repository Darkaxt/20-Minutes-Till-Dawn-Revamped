using System;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.UI;
using PlayerXP=flanne.Player.PlayerXP;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckBurnField(bool baseline)
 {
  RunResume.Invalidate();Progression.State.earned.Add("weapon:FlameCannon");Progression.Refresh();
  yield return AchievementRun("Shana","FlameCannon","20M_Forest");var game=Object.FindObjectOfType<GameController>();var player=PlayerController.Instance;
  SetPhase("native-combustion-evolution");var xp=game.playerXP;AccessTools.Field(typeof(PlayerXP),"<level>k__BackingField").SetValue(xp,19);AccessTools.Field(typeof(PlayerXP),"xp").SetValue(xp,0f);
  xp.GainXP((float)AccessTools.Property(typeof(PlayerXP),"xpToLevel").GetValue(xp)+1f);
  while(!(game.CurrentState is GunEvoMenuState)||!(AccessTools.Field(typeof(flanne.UI.Menu),"ClickEvent").GetValue(game.gunEvoMenu) is Delegate))yield return null;
  var choice=Enumerable.Range(0,3).Select(i=>game.gunEvoMenu.GetEntry(i)).Single(b=>b.GetComponent<GunEvoUI>().data.name=="Combustion");Click(choice);
  while(!(game.CurrentState is CombatState)||player.gun.gunData.name!="CombustionGun")yield return null;
  Require(player.gun.gunData.name=="CombustionGun","ordinary native level-20 choice equips Flame Cannon's Combustion evolution");
  Health source;while((source=Object.FindObjectsOfType<Health>().FirstOrDefault(h=>h.name.StartsWith("BrainMonster")&&!h.isDead))==null)yield return null;
  Vector3 center=player.transform.position+new Vector3(40,0,0);
  var targets=Enumerable.Range(0,5).Select(i=>DurableEnemy(source.gameObject,center+new Vector3((i-2)*.45f,0,0),true)).ToArray();
  foreach(var target in targets)target.gameObject.layer=(int)TagLayerUtil.Enemy;
  var actorPool=ObjectPooler.SharedInstance.pooledObjectsDictionary.Values.Single(list=>list.Contains(source.gameObject));
  foreach(var target in targets)actorPool.Add(target.gameObject);
  targets[0].gameObject.AddComponent<CircleCollider2D>().radius=.3f;
  var edge=DurableEnemy(source.gameObject,center+Vector3.right*2.2f,true);actorPool.Add(edge.gameObject);
  edge.gameObject.AddComponent<CircleCollider2D>().radius=.5f;
  for(int i=0;i<3;i++){var victim=DurableEnemy(source.gameObject,center,true);yield return null;victim.HPChange(-100000);victim.gameObject.SetActive(false);}
  PeriodicallyBurnArea field;while((field=Object.FindObjectsOfType<PeriodicallyBurnArea>().FirstOrDefault(f=>f.gameObject.activeInHierarchy))==null)yield return null;
  Log("native-burning-field",string.Join(" / ",field.GetComponentsInParent<Transform>().Select(t=>t.name))+" position="+field.transform.position+" expected="+center);
  Require(field.GetComponentsInParent<Transform>().Any(t=>t.name.StartsWith("PF_BurningField"))&&Vector2.Distance(field.transform.position,center)<.1f,"three actual native kills spawn the reachable source burning-field prefab at the last kill");
  player.SetPosition(center+Vector3.down*4);
  // Observe the complete native first query pass, not a manually called Burn.
  while(targets.Count(t=>BurnSystem.SharedInstance.IsBurning(t.gameObject))==0)yield return null;
  int count=targets.Count(t=>BurnSystem.SharedInstance.IsBurning(t.gameObject));
  float radius=(float)AccessTools.Field(typeof(PeriodicallyBurnArea),"rangeRadius").GetValue(field);
  var all=Physics2D.OverlapCircleAll(field.transform.position,radius,1<<(int)TagLayerUtil.Enemy);
  Log("burn-coverage", "burning="+count+" all-collider-results="+all.Length+" targets="+string.Join(";",targets.Select(t=>t.GetInstanceID()+":"+t.transform.position+":"+BurnSystem.SharedInstance.IsBurning(t.gameObject))));
  Require(targets.All(t=>all.Any(c=>c.gameObject==t.gameObject)),"all five known live fixture enemies overlap the actual native field query");
  Require(baseline?count>0&&count<5:count==5,baseline?"native Combustion field truncates a query containing five live enemies":"native Combustion field burns all five overlapping live enemies in one query pass");
  if(!baseline)
  {
   Require(!BurnSystem.SharedInstance.IsBurning(edge.gameObject)&&all.Any(c=>c.gameObject==edge.gameObject),"Android strict center-radius query excludes an outside center despite collider overlap");
   field.enabled=false;int damage=0;targets[0].onHurt.AddListener(value=>damage=value);
   while(damage==0)yield return null;
   Require(damage==3,"one pooled actor with two colliders receives exactly one burn application");
  }
  Require((int)AccessTools.Field(typeof(PeriodicallyBurnArea),"burnDamage").GetValue(field)==3&&Mathf.Abs((float)AccessTools.Field(typeof(PeriodicallyBurnArea),"timePerTick").GetValue(field)-.5f)<.001f,"source burn amount and tick interval are retained");
  yield return Capture("combustion-field-coverage.png");
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete",baseline?"A13 real native evolution/kill/prefab path reproduces two-collider query truncation; not corrected-code acceptance":"A13 native Combustion area coverage and scene cleanup pass with unchanged burn amounts/timing");Application.Quit(0);
 }
}
