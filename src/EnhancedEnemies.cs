using UnityEngine;
using flanne;
using HarmonyLib;
namespace TwentyMinutes.Modernization;

[System.Serializable]public sealed class EliteTierProgress
{
 public int eliteKills,plusClaims,plusKills,doubleClaims;
}
// Kill cadence belongs to this battle; scheduled minibosses retain their own
// source identities. Older checkpoints start at their next 200-kill boundary.
public sealed partial class EliteRun:MonoBehaviour
{
 internal static EliteRun Instance;internal int Claimed;internal EliteTierProgress Progress;
 bool Endless=>SelectedMap.MapData.endless&&!CustomMode.Active;
 void Awake(){Instance=this;Claimed=RunResume.Pending?.eliteClaims??(RunResume.Pending?.kills??0)/200;Progress=RunResume.Pending?.eliteTiers??new EliteTierProgress();}
 void OnDestroy(){ReleaseContours();if(Instance==this)Instance=null;}
 internal void Defeated(Health health)
 {
  if(!Endless)return;
  var elite=health.GetComponent<EnhancedEnemy>();if(elite==null||!elite.Enhanced)return;
  if(elite.Tier==1)Progress.eliteKills++;else if(elite.Tier==2)Progress.plusKills++;
 }
 internal void Spawned(GameObject actor,bool scheduledElite)
 {
  var health=actor.GetComponent<Health>();var source=health!=null?AndroidHealthEvents.Source(health):null;
  if(scheduledElite||source==null||source.CreatureType!=1||health.isDead||actor.GetComponent<AIComponent>()==null)return;
  int tier;
  if(Endless&&Progress.plusKills/5>Progress.doubleClaims)tier=3;
  else if(Endless&&Progress.eliteKills/10>Progress.plusClaims)tier=2;
  else if(ScoreCalculator.SharedInstance.GetScore().enemiesKilled/200>Claimed)tier=1;
  else return;
  var elite=actor.GetComponent<EnhancedEnemy>()??actor.AddComponent<EnhancedEnemy>();elite.Promote(tier);
  if(tier==3)Progress.doubleClaims++;else if(tier==2)Progress.plusClaims++;else Claimed++;
 }
}
public sealed class EnhancedEnemy:MonoBehaviour
{
 internal bool Enhanced,FasterEye;internal int Tier,XpCopies=1;int originalMaximum;Vector3 originalScale;Health health;SpriteRenderer aura;
 void Awake(){health=GetComponent<Health>();}
 void OnEnable(){Enhanced=false;Tier=0;XpCopies=1;}
 internal void Promote(int tier=1)
 {
  Tier=tier;float size=Mathf.Pow(1.25f,tier);XpCopies=(int)Mathf.Pow(3,tier);
  originalScale=transform.localScale;transform.localScale=new Vector3(originalScale.x*size,originalScale.y*size,originalScale.z);
  originalMaximum=health.maxHP;health.maxHP=originalMaximum*XpCopies;Enhanced=true;
  FasterEye=GetComponent<AIComponent>().specialSO!=null&&GetComponent<AIComponent>().specialSO.name=="EyeMonsterShoot";
  if(aura==null)
  {
   aura=ElementalRun.Instance.Status(transform);aura.name="EliteContour";aura.sprite=null;aura.transform.localScale=Vector3.one;
   aura.gameObject.AddComponent<EliteContour>().Initialize(this,GetComponent<SpriteRenderer>(),aura);
  }
  aura.enabled=true;
 }
 void OnDisable()
 {
  if(aura!=null)aura.enabled=false;
  if(!Enhanced)return;
  // Clear the owned spawn multiplier before any other pool consumer reuses
  // this actor. Do not emit health callbacks from an inactive pool member.
  transform.localScale=originalScale;
  Plugin.SetField(health,"_maxHP",originalMaximum);AndroidHealthEvents.HP(health)=Mathf.Min(health.HP,originalMaximum);Enhanced=false;FasterEye=false;Tier=0;
  // Source Death returns the actor before dispatching its serialized drops.
  // Retain XP copies for that dispatch; the next OnEnable resets them to one.
 }
}
[HarmonyPatch(typeof(AIController),"AILookTowards")]
class EliteFacingScale
{
 // Native facing writes a unit scale for Boomer. Preserve the actor's actual
 // proportions at that boundary, keeping the sign chosen by native facing.
 static void Prefix(AIComponent ai,ref Vector3? __state)
 {var elite=ai.GetComponent<EnhancedEnemy>();if(elite!=null&&elite.Enhanced)__state=ai.transform.localScale;}
 static void Postfix(AIComponent ai,Vector3? __state)
 {if(__state.HasValue){var before=__state.Value;ai.transform.localScale=new Vector3(Mathf.Sign(ai.transform.localScale.x)*Mathf.Abs(before.x),before.y,before.z);}}
}
