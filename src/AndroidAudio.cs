using System;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using flanne;
using flanne.Audio;
namespace TwentyMinutes.Modernization;

// These two actual gameplay sounds have no clip/SoundEffectSO counterpart in
// the original native player. The owned-input builder supplies their source PCM.
internal static class AndroidPerkSounds
{
 static SoundEffectSO icicle,overload;
 internal static SoundEffectSO Icicle()=>icicle!=null?icicle:icicle=Load("icicle-whoosh","Spell_Ice_Small_Whoosh_01","IcicleWoosh_SFX",new Vector2Int(0,4),new Vector2(1,1.2599065f));
 internal static SoundEffectSO Overload()=>overload!=null?overload:overload=Load("overload","Fire Spelll 27","Overload_SFX",new Vector2Int(2,6),new Vector2(1.1224556f,1.4141891f));
 static SoundEffectSO Load(string stem,string clipName,string soundName,Vector2Int semitones,Vector2 pitch)
 {
  using(var reader=new BinaryReader(File.OpenRead(Path.Combine(Plugin.AssetRoot,"audio",stem+".pcm"))))
  {
   int channels=reader.ReadInt32(),frequency=reader.ReadInt32(),frames=reader.ReadInt32();
   var samples=new float[frames*channels];for(int i=0;i<samples.Length;i++)samples[i]=reader.ReadSingle();
   var clip=AudioClip.Create(clipName,frames,channels,frequency,false);clip.SetData(samples,0);
   var sound=ScriptableObject.CreateInstance<SoundEffectSO>();sound.name=soundName;
   Plugin.SetField(sound,"clips",new[]{clip});Plugin.SetField(sound,"volume",Vector2.one);
   Plugin.SetField(sound,"useSemitones",true);Plugin.SetField(sound,"semitones",semitones);Plugin.SetField(sound,"pitch",pitch);
   UnityEngine.Object.DontDestroyOnLoad(clip);UnityEngine.Object.DontDestroyOnLoad(sound);return sound;
  }
 }
}

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

// Keep native title presentation. Battle SoundEffectSO.Play uses the source
// shared pool, whose exhaustion occurs before any clip/volume/pitch RNG draws.
[HarmonyPatch(typeof(SoundEffectSO),nameof(SoundEffectSO.Play))]
class AndroidGameplaySound
{
 static readonly Func<SoundEffectSO,AudioClip> Clip=(Func<SoundEffectSO,AudioClip>)Delegate.CreateDelegate(typeof(Func<SoundEffectSO,AudioClip>),AccessTools.Method(typeof(SoundEffectSO),"GetAudioClip"));
 internal static GameObject Template(Transform parent)
 {
  var root=new GameObject("Audio Source",typeof(AudioSource));root.transform.SetParent(parent,false);root.SetActive(false);
  // The original prefab uses Unity's default AudioSource values, including
  // playOnAwake=true, 2D blend, no clip/mixer and no loop/mute/bypass flags.
  return root;
 }
 static bool Prefix(SoundEffectSO __instance,AudioSource audioSourceParam,AudioClip[] ___clips,Vector2 ___volume,bool ___useSemitones,Vector2Int ___semitones,Vector2 ___pitch,ref AudioSource __result)
 {
  if(SceneManager.GetActiveScene().name!="Battle")return true;
  var audio=audioSourceParam;
  try
  {
   if(___clips.Length==0){Debug.LogError("Missing sound clips for "+__instance.name);__result=null;return false;}
   if(audio==null)
   {
    var actor=ObjectPooler.SharedInstance.GetPooledObject("Sound");
    if(actor==null){__result=null;return false;}
    audio=actor.GetComponent<AudioSource>();
   }
   audio.clip=Clip(__instance);
   var manager=AudioManager.Instance;
   audio.volume=manager!=null?manager.SFXVolume*UnityEngine.Random.Range(___volume.x,___volume.y):UnityEngine.Random.Range(___volume.x,___volume.y);
   audio.pitch=___useSemitones?Mathf.Pow(1.05946f,UnityEngine.Random.Range(___semitones.x,___semitones.y)):UnityEngine.Random.Range(___pitch.x,___pitch.y);
   audio.Play();
   ObjectPooler.SharedInstance.GetComponent<AndroidPools>().TimeOutRelease(audio.transform,Mathf.FloorToInt(audio.clip.length*1000/audio.pitch));
  }
  // This catch/return is in the shipped Play method. It is not a new fallback.
  catch(Exception){}
  __result=audio;return false;
 }
}
