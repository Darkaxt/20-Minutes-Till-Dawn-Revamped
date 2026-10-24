using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source StateMachine.Transition (0x192bb24) prevents nested Enter/Exit
// transitions. Keep the native desktop menu state machine: those deliberately
// retained UI states have different navigation/initialization lifecycles.
public sealed class AndroidPlayerStateMachine:MonoBehaviour
{
 internal bool InTransition;
}
[HarmonyPatch(typeof(StateMachine),"Transition")]
class AndroidPlayerTransition
{
 static bool Prefix(StateMachine __instance,State value,ref State ____currentState)
 {
  if(!(__instance is PlayerController))return true;
  var owner=__instance.GetComponent<AndroidPlayerStateMachine>();
  if(owner==null)owner=__instance.gameObject.AddComponent<AndroidPlayerStateMachine>();
  if(____currentState==value||owner.InTransition)return false;
  owner.InTransition=true;
  if(____currentState!=null)____currentState.Exit();
  ____currentState=value;
  if(____currentState!=null)____currentState.Enter();
  owner.InTransition=false;return false;
 }
}
