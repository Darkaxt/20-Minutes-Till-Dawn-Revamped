using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source GameTimer has two independent flags and advances in FixedUpdate.
// The static CDData clock survives scene changes and uses integer milliseconds.
public sealed class AndroidGameClock:MonoBehaviour
{
 internal static int Milliseconds;
 internal bool CDEnabled=true;
 GameTimer owner;
 static readonly AccessTools.FieldRef<GameTimer,bool> Playing=AccessTools.FieldRefAccess<GameTimer,bool>("_isPlaying");
 static readonly AccessTools.FieldRef<GameTimer,bool> Warning=AccessTools.FieldRefAccess<GameTimer,bool>("oneSecondWarningSent");
 static readonly AccessTools.FieldRef<GameTimer,float> Timer=AccessTools.FieldRefAccess<GameTimer,float>("<timer>k__BackingField");
 internal void Initialize(GameTimer value){owner=value;Playing(owner)=true;Timer(owner)=0;}
 internal static void Stop(GameTimer timer,bool cdEnable)
 {timer.Stop();timer.GetComponent<AndroidGameClock>().CDEnabled=cdEnable;}
 void FixedUpdate()
 {
  // This helper follows the native component's Unity message eligibility.
  if(!owner.enabled)return;
  if(CDEnabled)Milliseconds=unchecked(Milliseconds+Mathf.FloorToInt(Time.fixedDeltaTime*1000));
  if(!Playing(owner))return;
  Timer(owner)+=Time.fixedDeltaTime;
  if(Timer(owner)>=owner.timeLimit-1&&!Warning(owner))
  {Warning(owner)=true;owner.PostNotification(GameTimer.OneSecondLeftNotification);Timer(owner)=owner.timeLimit-1;}
  if(Timer(owner)>=owner.timeLimit)
  {owner.PostNotification(GameTimer.TimeLimitNotification);Timer(owner)=owner.timeLimit;}
 }
}
[HarmonyPatch(typeof(GameTimer),"Awake")]
class AndroidGameClockOwner
{static void Postfix(GameTimer __instance)=>__instance.gameObject.AddComponent<AndroidGameClock>().Initialize(__instance);}
[HarmonyPatch(typeof(GameTimer),"Update")]
class AndroidGameClockRenderedTick
{static bool Prefix()=>false;}
[HarmonyPatch(typeof(GameTimer),nameof(GameTimer.Start))]
class AndroidGameClockStart
{static void Postfix(GameTimer __instance)=>__instance.GetComponent<AndroidGameClock>().CDEnabled=true;}
[HarmonyPatch(typeof(GameTimer),nameof(GameTimer.Stop))]
class AndroidGameClockStop
{static void Postfix(GameTimer __instance)=>__instance.GetComponent<AndroidGameClock>().CDEnabled=false;}
