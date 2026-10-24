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
 Transform target,moveTransform;Vector3 origin;float elapsed;
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
 void OnDisable(){target=null;}
 internal void TargetPick(GameObject actor)
 {
  if(actor==null||target!=null)return;
  if(trail!=null)trail.enabled=true;
  elapsed=0;target=actor.transform;origin=transform.position;moveTransform=transform;
 }
 void Update()
 {
  // The companion forwards the original Pickup's Unity message ownership.
  if(!pickup.enabled)return;
  if(circle!=null)circle.enabled=Vector2.Distance(transform.position,PlayerController.Instance.transform.position)<15;
  if(target==null)return;
  elapsed+=Time.deltaTime;moveTransform.position=Vector3.Lerp(origin,target.position,elapsed/ArrivalTime);
  if(elapsed<ArrivalTime)return;
  AccessTools.Method(pickup.GetType(),"UsePickup").Invoke(pickup,new object[]{target.gameObject});
  var sound=(SoundEffectSO)Sound.GetValue(pickup);if(sound!=null)sound.Play();
  AndroidPools.Return(gameObject);target=null;
 }
}
[HarmonyPatch(typeof(Pickup),"OnTriggerEnter2D")]
class AndroidPickupTrigger
{
 static bool Prefix(Pickup __instance)
 {
  if(!(__instance is HPPickup)&&!(__instance is XPPickup))return true;
  __instance.GetComponent<AndroidPickup>().TargetPick(PlayerController.Instance.gameObject);return false;
 }
}
