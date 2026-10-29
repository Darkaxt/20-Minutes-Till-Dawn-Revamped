using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Pickups;
namespace TwentyMinutes.Modernization;

// Source Pickup uses a world-space frame update, not the native parent/tween
// coroutine. The shared pool supplies this owner to the matching native assets.
public sealed class AndroidPickup:MonoBehaviour
{
 const float ArrivalTime=.3f;
 static readonly FieldInfo Sound=AccessTools.Field(typeof(Pickup),"soundFX");
 Pickup pickup;CircleCollider2D circle;TrailRenderer trail;SpriteRenderer xpSprite;
 Transform target,moveTransform;Vector3 origin;float elapsed,magnetDelay;
 internal static void Attach(GameObject actor)
 {
  var pickup=actor.GetComponent<Pickup>();
  if(pickup is HPPickup||pickup is XPPickup)actor.AddComponent<AndroidPickup>();
 }
 void Awake()
 {
  pickup=GetComponent<Pickup>();circle=GetComponent<CircleCollider2D>();trail=GetComponentInChildren<TrailRenderer>();
  if(pickup is XPPickup)xpSprite=transform.Find("Bounce").GetComponent<SpriteRenderer>();
 }
 void OnEnable()
 {
  if(trail!=null)trail.enabled=false;
  if(pickup is XPPickup)xpSprite.enabled=true;
 }
 void OnDisable(){target=null;elapsed=magnetDelay=0;}
 internal void TargetPick(GameObject actor)
 {
  // Normal pickup range can take ownership while a distant magnet wave waits.
  if(actor==null||target!=null&&(magnetDelay==0||elapsed>=magnetDelay))return;
  BeginPick(actor,0);
 }
 internal bool MagnetPick(GameObject actor,float delay)
 {
  if(actor==null||target!=null)return false;
  BeginPick(actor,delay);return true;
 }
 void BeginPick(GameObject actor,float delay)
 {
  magnetDelay=delay;if(trail!=null)trail.enabled=delay==0;
  elapsed=0;target=actor.transform;origin=transform.position;moveTransform=transform;
 }
 void Update()
 {
  // The companion forwards the original Pickup's Unity message ownership.
  if(!pickup.enabled)return;
  if(circle!=null)circle.enabled=Vector2.Distance(transform.position,PlayerController.Instance.transform.position)<15;
  if(target==null)return;
  elapsed+=Time.deltaTime;if(elapsed<magnetDelay)return;
  if(trail!=null)trail.enabled=true;
  float flight=elapsed-magnetDelay;moveTransform.position=Vector3.Lerp(origin,target.position,flight/ArrivalTime);
  if(flight<ArrivalTime)return;
  AccessTools.Method(pickup.GetType(),"UsePickup").Invoke(pickup,new object[]{target.gameObject});
  var sound=(SoundEffectSO)Sound.GetValue(pickup);if(sound!=null)sound.Play();
  AndroidPools.Return(gameObject);target=null;
 }
}
[HarmonyPatch(typeof(Pickup),"OnTriggerEnter2D")]
class AndroidPickupTrigger
{
 static bool Prefix(Pickup __instance,Collider2D other)
 {
  if(!(__instance is HPPickup)&&!(__instance is XPPickup))return true;
  // Source XP is on Pick13, which cannot contact Player11. Windows merges
  // XP and red hearts on Pickup10; red hearts use VisiblePick20 on Android
  // and do admit that contact. Preserve the two authored matrix behaviors
  // at this shared native callback rather than disabling heart contacts.
  if(__instance is XPPickup&&other.gameObject.layer==(int)TagLayerUtil.Player)return false;
  __instance.GetComponent<AndroidPickup>().TargetPick(PlayerController.Instance.gameObject);return false;
 }
}
