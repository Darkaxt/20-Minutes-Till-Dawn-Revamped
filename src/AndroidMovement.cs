using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source MoveComponent2D gains LateUpdate/IsInit and IMoveUpdate. The companion
// supplies that Unity message without editing the installed managed assembly.
public sealed class AndroidMoveRegistration:MonoBehaviour
{
 internal MoveComponent2D Move;internal bool Initialized;
 void LateUpdate()
 {
  if(!Move.enabled||Initialized)return;
  Initialized=true;MoveSystem2D.Register(Move);
 }
}
static class AndroidMovement
{
 internal static readonly List<AndroidProjectile> Updates=new List<AndroidProjectile>();
 internal static void Remove(MoveComponent2D move)
 {
  MoveSystem2D.UnRegister(move);
  var state=move.GetComponent<AndroidMoveRegistration>();if(state!=null)state.Initialized=false;
 }
}
[HarmonyPatch(typeof(MoveSystem2D),"Awake")]
class AndroidMovementInitialization{static void Postfix()=>AndroidMovement.Updates.Clear();}
[HarmonyPatch(typeof(MoveComponent2D),"Start")]
class AndroidMoveStart
{
 static bool Prefix(MoveComponent2D __instance)
 {__instance.gameObject.AddComponent<AndroidMoveRegistration>().Move=__instance;return false;}
}
[HarmonyPatch(typeof(MoveComponent2D),"OnDisable")]
class AndroidMoveDisable
{
 static bool Prefix(MoveComponent2D __instance)
 {AndroidMovement.Remove(__instance);__instance.vector=Vector2.zero;return false;}
}
[HarmonyPatch(typeof(MoveComponent2D),"OnDestroy")]
class AndroidMoveDestroy{static bool Prefix()=>false;}
[HarmonyPatch(typeof(MoveSystem2D),nameof(MoveSystem2D.Register))]
class AndroidMoveRegister
{
 static void Postfix(MoveComponent2D m)
 {var shot=m.GetComponent<AndroidProjectile>();if(shot!=null&&shot.HasMoveUpdate)AndroidMovement.Updates.Add(shot);}
}
[HarmonyPatch(typeof(MoveSystem2D),nameof(MoveSystem2D.UnRegister))]
class AndroidMoveUnregister
{
 static void Postfix(MoveComponent2D m)
 {var shot=m.GetComponent<AndroidProjectile>();if(shot!=null)AndroidMovement.Updates.Remove(shot);}
}
[HarmonyPatch(typeof(MoveSystem2D),"FixedUpdate")]
class AndroidMovementStep
{
 static bool Prefix()
 {
  float dt=Time.fixedDeltaTime;var actors=MoveSystem2D.moveComponents;
  for(int i=actors.Count-1;i>=0;i--)
  {
   var move=actors[i];if(!move.isActiveAndEnabled)continue;
   Vector2 before=move.vector;
   move.vector=before.normalized*(before.magnitude*Mathf.Exp(-move.drag*dt));
   ElementalWind.Steer(move,dt);
   var control=move.GetComponent<ElementalControl>();
   Vector2 velocity=control!=null&&control.AI!=null?control.Movement(move,dt):move.vector;
   move.transform.position+=(Vector3)(velocity*dt);
   if(!move.rotateTowardsMove)continue;
   move.transform.rotation=Quaternion.AngleAxis(Mathf.Atan2(move.vector.y,move.vector.x)*Mathf.Rad2Deg,Vector3.forward);
   move.transform.localScale=move.vector.x<0?new Vector3(1,-1,1):Vector3.one;
  }
  // Source does not SyncTransforms before callbacks. Both shipped players have
  // autoSyncTransforms=false; retain their normal physics-step boundary.
  for(int i=AndroidMovement.Updates.Count-1;i>=0;i--)AndroidMovement.Updates[i].Step(dt);
  return false;
 }
}
