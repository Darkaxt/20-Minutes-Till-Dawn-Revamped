using System.Collections;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Android's egg waits on a game-clock deadline. Success precedes the visual
// hatch, and its restore API can receive a pending value before native Start.
public sealed class AndroidDragonEgg:MonoBehaviour
{
 SummonEgg egg;
 Summon summon;
 ParticleSystem particles;
 FlashSprite flasher;
 SoundEffectSO sound;
 float deadline;
 bool initialized,success;
 float? pending;
 internal static AndroidDragonEgg Get(SummonEgg owner)
 {
  var state=owner.GetComponent<AndroidDragonEgg>();
  if(state==null){state=owner.gameObject.AddComponent<AndroidDragonEgg>();state.enabled=false;state.egg=owner;}
  return state;
 }
 internal void Initialize(Summon source,ParticleSystem hatchParticles,FlashSprite hatchFlasher,SoundEffectSO soundFX,float seconds)
 {
  summon=source;particles=hatchParticles;flasher=hatchFlasher;sound=soundFX;
  summon.gameObject.SetActive(false);deadline=GameTimer.SharedInstance.timer+seconds;initialized=true;enabled=true;
  if(pending.HasValue){ApplyRestore(pending.Value);pending=null;}
 }
 internal float GetRemainingHatchSeconds()=>success?-1:Mathf.Max(0,deadline-GameTimer.SharedInstance.timer);
 internal void RestoreRemainingHatchSeconds(float remaining)
 {if(initialized)ApplyRestore(remaining);else pending=remaining;}
 void ApplyRestore(float remaining)
 {
  if(remaining<0)
  {success=true;summon.gameObject.SetActive(true);egg.gameObject.SetActive(false);}
  else
  {success=false;summon.gameObject.SetActive(false);egg.gameObject.SetActive(true);deadline=GameTimer.SharedInstance.timer+remaining;}
 }
 void Update()
 {
  // This sibling supplies the Update absent from the older native egg. Its
  // native component's enabled state remains the source execution boundary.
  if(!egg.enabled||success||!(GameTimer.SharedInstance.timer>deadline))return;
  success=true;egg.StartCoroutine(Hatch());
 }
 IEnumerator Hatch()
 {
  for(int i=0;i<3;i++){flasher.Flash();yield return new WaitForSeconds(.2f);}
  particles.Play();sound?.Play();summon.gameObject.SetActive(true);egg.gameObject.SetActive(false);
 }
}
[HarmonyPatch(typeof(SummonEgg),"Start")]
class AndroidDragonEggInitialization
{
 static bool Prefix(SummonEgg __instance,Summon ___summon,ParticleSystem ___hatchParticles,FlashSprite ___hatchFlasher,SoundEffectSO ___soundFX,float ___secondsToHatch)
 {AndroidDragonEgg.Get(__instance).Initialize(___summon,___hatchParticles,___hatchFlasher,___soundFX,___secondsToHatch);return false;}
}
