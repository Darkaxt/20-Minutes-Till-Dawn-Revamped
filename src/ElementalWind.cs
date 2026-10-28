using System.Collections.Generic;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

internal static class ElementalWind
{
 internal static readonly HashSet<AndroidWindActor> Gales=new HashSet<AndroidWindActor>();
 static float Radius(AndroidWindActor gale)=>gale.GetComponent<Collider2D>().bounds.extents.x;
 internal static Vector2 Drift(Vector2 at)
 {
  float closest=float.MaxValue;Vector2 direction=Vector2.zero;
  foreach(var gale in Gales){float distance=Vector2.Distance(at,gale.transform.position);if(distance<Radius(gale)&&distance<closest){closest=distance;direction=((Vector2)gale.transform.position-at).normalized;}}
  return direction*ElementalRun.Instance.BodyWidth*.5f;
 }
 internal static Vector2 Deflect(Vector2 velocity,Vector2 at,float degrees)
 {
  if(velocity==Vector2.zero)return velocity;
  if(at==Vector2.zero)return velocity;
  float turn=Mathf.Clamp(Vector2.SignedAngle(velocity,at),-degrees,degrees);return Quaternion.Euler(0,0,turn)*velocity;
 }
 internal static void Steer(MoveComponent2D move,float dt)
 {
  if(move.gameObject.layer!=LayerMask.NameToLayer("EnemyProjectile"))return;
  float closest=float.MaxValue;AndroidWindActor owner=null;
  foreach(var gale in Gales){float distance=Vector2.Distance(move.transform.position,gale.transform.position);if(distance<Radius(gale)&&distance<closest){owner=gale;closest=distance;}}
  if(owner!=null)move.vector=Deflect(move.vector,(Vector2)(move.transform.position-owner.transform.position),90*dt);
 }
 internal static void Pulse(AndroidWindActor gale)
 {
  var moves=MoveSystem2D.moveComponents;
  foreach(var move in moves)if(move!=null&&move.isActiveAndEnabled&&move.gameObject.layer==LayerMask.NameToLayer("EnemyProjectile")&&Vector2.Distance(move.transform.position,gale.transform.position)<=3)
   move.vector=Deflect(move.vector,(Vector2)(move.transform.position-gale.transform.position),60);
  ElementalRun.Instance.Pulse(gale.transform.position,3,new Color(.65f,.92f,.8f));
 }
}
