using HarmonyLib;
using flanne;
using flanne.Audio;
namespace TwentyMinutes.Modernization;

// Source PlaySoundEffect.Play uses Unity object inequality before playback.
// Its separate ClickSFX analytics branch is outside native gameplay/audio.
[HarmonyPatch(typeof(PlaySoundEffect),nameof(PlaySoundEffect.Play))]
class AndroidSoundPlayback
{
 static bool Prefix(SoundEffectSO ___soundFX)
 {
  if(___soundFX!=null)___soundFX.Play();
  return false;
 }
}
