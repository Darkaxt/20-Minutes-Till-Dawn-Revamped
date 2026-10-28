using UnityEngine;
using flanne;
using HarmonyLib;
namespace TwentyMinutes.Modernization;

// Kill cadence belongs to this battle; scheduled minibosses retain their own
// source identities. Older checkpoints start at their next 200-kill boundary.
public sealed partial class EliteRun:MonoBehaviour
{
 internal static EliteRun Instance;internal int Claimed;
 void Awake(){Instance=this;Claimed=RunResume.Pending?.eliteClaims??(RunResume.Pending?.kills??0)/200;}
 void OnDestroy(){ReleaseContours();if(Instance==this)Instance=null;}
 internal void Spawned(GameObject actor,bool scheduledElite)
 {
  var health=actor.GetComponent<Health>();var source=health!=null?AndroidHealthEvents.Source(health):null;
  if(scheduledElite||source==null||source.CreatureType!=1||health.isDead||actor.GetComponent<AIComponent>()==null)return;
  if(ScoreCalculator.SharedInstance.GetScore().enemiesKilled/200<=Claimed)return;
  var elite=actor.GetComponent<EnhancedEnemy>()??actor.AddComponent<EnhancedEnemy>();elite.Promote();Claimed++;
 }
}
public sealed class EnhancedEnemy:MonoBehaviour
{
 internal bool Enhanced,FasterEye;internal int XpCopies=1;int originalMaximum;Vector3 originalScale;Health health;SpriteRenderer aura;
 void Awake(){health=GetComponent<Health>();}
 void OnEnable(){Enhanced=false;XpCopies=1;}
 internal void Promote()
 {
  originalScale=transform.localScale;transform.localScale=new Vector3(originalScale.x*1.25f,originalScale.y*1.25f,originalScale.z);
  originalMaximum=health.maxHP;health.maxHP=originalMaximum*3;Enhanced=true;XpCopies=3;
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
  Plugin.SetField(health,"_maxHP",originalMaximum);AndroidHealthEvents.HP(health)=Mathf.Min(health.HP,originalMaximum);Enhanced=false;FasterEye=false;
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
