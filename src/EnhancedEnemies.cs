using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Kill cadence belongs to this battle; scheduled minibosses retain their own
// source identities. Older checkpoints start at their next 200-kill boundary.
public sealed class EliteRun:MonoBehaviour
{
 internal static EliteRun Instance;internal int Claimed;
 void Awake(){Instance=this;Claimed=RunResume.Pending?.eliteClaims??(RunResume.Pending?.kills??0)/200;}
 void OnDestroy(){if(Instance==this)Instance=null;}
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
  if(aura==null){aura=ElementalRun.Instance.Status(transform);aura.name="EliteAura";aura.transform.localScale*=1.5f;}
  aura.color=new Color(1,.18f,.25f,.3f);aura.enabled=true;
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
