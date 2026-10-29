using System.Collections.Generic;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// These are the three new elemental restrictions on a pooled NPC. Native ice
// continues to own frozen; electrical control never enters FreezeSystem.
public sealed class ElementalControl:MonoBehaviour
{
 internal static readonly HashSet<ElementalControl> Active=new HashSet<ElementalControl>();
 public float Stun{get;private set;}public float Fear{get;private set;}public float Slow{get;private set;}public float Dizzy{get;private set;}
 public float SlowFraction{get;private set;}
 const float PanicDuration=.75f;
 static float nextPanicTurn=1;
 internal AIComponent AI;Health health;Vector2 lastSeen,push,panicHeading;float pushRemaining,panicTurn;Transform aim;ElementalStatusVisual status;
 internal bool Boss=>gameObject.tag.Contains("Champion");
 internal static ElementalControl Get(GameObject actor)=>actor.GetComponent<ElementalControl>()??actor.AddComponent<ElementalControl>();
 void Awake(){AI=GetComponent<AIComponent>();health=GetComponent<Health>();}
 void OnEnable()=>Active.Add(this);
 void OnDisable(){Active.Remove(this);Stun=Fear=Slow=Dizzy=pushRemaining=0;SlowFraction=0;push=panicHeading=Vector2.zero;panicTurn=0;if(aim!=null)aim.gameObject.SetActive(false);if(status!=null)status.Hide();var burn=BurnSystem.SharedInstance!=null?AndroidBurnState.Get(BurnSystem.SharedInstance)?.Find(gameObject):null;if(burn!=null)burn.Release();}
 void OnDestroy(){Active.Remove(this);if(aim!=null)Destroy(aim.gameObject);}
 public bool Electrified=>isActiveAndEnabled&&health!=null&&!health.isDead&&Stun>0;
 internal void Electrocute(){if(AI!=null){Stun=2*(Boss?.2f:1);RuneMechanics.ElementalInflict(gameObject);}}
 internal void Ignite()
 {
  if(AI==null)return;Fear=Boss?0:PanicDuration;Slow=2*(Boss?.2f:1);SlowFraction=Boss?.1f:.25f;
  panicHeading=((Vector2)(transform.position-PlayerController.Instance.transform.position)).normalized;
  if(panicHeading==Vector2.zero)panicHeading=Vector2.right;
  if(Fear>0){panicTurn=nextPanicTurn;nextPanicTurn=-nextPanicTurn;}
 }
 internal void Disorient()
 {if(AI==null)return;Dizzy=1.5f*(Boss?.2f:1);lastSeen=PlayerController.Instance.transform.position;if(aim!=null){aim.position=lastSeen;aim.gameObject.SetActive(true);}RuneMechanics.ElementalInflict(gameObject);}
 internal Vector2 Perceived(Vector2 actual)=>Dizzy>0?lastSeen:actual;
 internal Transform Aim(Transform actual)
 {
  if(Dizzy<=0)return actual;
  if(aim==null){aim=new GameObject("WindAimSnapshot").transform;aim.SetParent(ElementalRun.Instance.transform,false);}
  aim.position=lastSeen;aim.gameObject.SetActive(true);return aim;
 }
 internal void Push(Vector2 direction,float damage)
 {
  if(AI==null||damage<=0)return;
  float distance=ElementalRun.Instance.BodyWidth*(.5f+Mathf.Clamp01(damage/health.maxHP))*(Boss?.25f:1);
  push=direction.normalized*distance/.2f;pushRemaining=.2f;
 }
 void Update()
 {
  float dt=Time.deltaTime;Stun=Mathf.Max(0,Stun-dt);Fear=Mathf.Max(0,Fear-dt);Slow=Mathf.Max(0,Slow-dt);Dizzy=Mathf.Max(0,Dizzy-dt);pushRemaining=Mathf.Max(0,pushRemaining-dt);
  if(Dizzy==0&&aim!=null)aim.gameObject.SetActive(false);
  if(status==null&&(Stun>0||Dizzy>0||Fear>0||Slow>0))status=gameObject.AddComponent<ElementalStatusVisual>();
 }
 internal Vector2 Movement(MoveComponent2D move,float dt)
 {
  if(Stun>0||AI.frozen){move.vector=Vector2.zero;return Vector2.zero;}
  Vector2 velocity=move.vector;
  if(Fear>0)
  {
   // One uneven local loop, anchored to ignition rather than chasing a moving
   // player outwards. Preserve the native pursuit vector for immediate recovery.
   float phase=(1-Fear/PanicDuration)*Mathf.PI*2;
   float angle=panicTurn*(phase+.25f*Mathf.Sin(phase*2));
   float sine=Mathf.Sin(angle),cosine=Mathf.Cos(angle);
   velocity=new Vector2(panicHeading.x*cosine-panicHeading.y*sine,panicHeading.x*sine+panicHeading.y*cosine)*AI.maxMoveSpeed;
  }
  velocity*=Slow>0?1-SlowFraction:1;
  Vector2 extra=pushRemaining>0?push:Vector2.zero;extra+=ElementalWind.Drift(transform.position);
  if(extra!=Vector2.zero)
  {
   Vector2 from=(Vector2)transform.position+velocity*dt,to=from+extra*dt;bool blocked=false;
   foreach(var hit in Physics2D.LinecastAll(from,to))
    if(hit.collider!=null&&(hit.collider.CompareTag("MapBounds")||(!hit.collider.isTrigger&&hit.collider.GetComponentInParent<MoveComponent2D>()==null&&hit.collider.GetComponentInParent<Health>()==null))){blocked=true;break;}
   if(!blocked)velocity+=extra;
  }
  return velocity;
 }
 internal static int VisibleStunned()
 {
  var camera=Camera.main;int count=0;if(camera==null)return 0;
  foreach(var actor in Active)if(actor.Electrified){var p=camera.WorldToViewportPoint(actor.transform.position);if(p.z>0&&p.x>=0&&p.x<=1&&p.y>=0&&p.y<=1)count++;}
  return count;
 }
}
