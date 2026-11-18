using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization;

// Appearance is run-local, including Continue. Native animators retain state,
// facing, timing, damage feedback and gameplay-event ownership.
public sealed class AppearanceRun:MonoBehaviour
{
 public static AppearanceRun Instance;
 public Cosmetics.Selection Character,Weapon;GameObject wings;
 internal static void Attach()
 {
  if(!Cosmetics.Available)return;var player=PlayerController.Instance;
  if(player.GetComponent<AppearanceRun>()!=null)return;
  var owner=player.gameObject.AddComponent<AppearanceRun>();Instance=owner;
  var saved=RunResume.Pending?.appearance;
  owner.Character=Copy(saved?.character??Cosmetics.For(Loadout.CharacterSelection.name));
  owner.Weapon=Copy(saved?.weapon??Cosmetics.For(Loadout.GunSelection.name));
  var skin=owner.Item(owner.Character.character);
  if(skin!=null)CosmeticAnimation.Bind(player.playerAnimator,skin.clips);
  var wing=owner.Item(owner.Character.wing);
  if(wing!=null)
  {
   var pose=Characters.PoseChain(player.transform,Cosmetics.WingPoses);var go=pose.gameObject;go.name="AppearanceWing";
   owner.wings=go;
   var renderer=go.AddComponent<SpriteRenderer>();renderer.sharedMaterial=player.playerSprite.sharedMaterial;renderer.sortingLayerID=player.playerSprite.sortingLayerID;renderer.sortingOrder=player.playerSprite.sortingOrder-1;
   var frames=go.AddComponent<CosmeticFrames>();frames.Renderer=renderer;frames.Frames=wing.wingFrames;frames.Interval=Cosmetics.WingInterval;
   PlayerVisualOpacity.Attach(go);
  }
  owner.ApplyGun();
 }
 static Cosmetics.Selection Copy(Cosmetics.Selection value)=>new Cosmetics.Selection{owner=value.owner,character=value.character,weapon=value.weapon,wing=value.wing,bullet=value.bullet};
 internal Cosmetics.Item Item(int id)=>id!=0&&Cosmetics.Items.TryGetValue(id,out var item)&&Cosmetics.Owned(id)?item:null;
 internal Sprite Portrait=>Item(Character.character) is Cosmetics.Item skin?Cosmetics.Sprite(skin.portrait):Cosmetics.Original(Character.owner,"character");
 internal void ApplyGun()
 {
  var gun=PlayerController.Instance.gun;var skin=Item(Weapon.weapon);if(skin==null)return;
  foreach(var animator in gun.gunAnimators)CosmeticAnimation.Bind(animator,skin.clips);
 }
 void OnDestroy(){if(Instance==this)Instance=null;}
 void Update(){if(wings!=null&&PlayerController.Instance.playerHealth.hp==0)wings.SetActive(false);}
 public sealed class Snapshot{public Cosmetics.Selection character,weapon;}
 internal Snapshot Capture()=>new Snapshot{character=Copy(Character),weapon=Copy(Weapon)};
}

public sealed class CosmeticAnimation:MonoBehaviour
{
 Animator animator;Cosmetics.Clip[] clips;readonly Dictionary<uint,SpriteRenderer> renderers=new Dictionary<uint,SpriteRenderer>();readonly Dictionary<string,Cosmetics.Clip> aliases=new Dictionary<string,Cosmetics.Clip>();
 internal static void Bind(Animator animator,Cosmetics.Clip[] clips)
 {
  if(animator==null||clips==null||clips.Length==0)return;
  var view=animator.GetComponent<CosmeticAnimation>()??animator.gameObject.AddComponent<CosmeticAnimation>();view.animator=animator;view.clips=clips;view.renderers.Clear();
  foreach(var renderer in animator.GetComponentsInChildren<SpriteRenderer>(true))view.renderers[PathHash(Relative(animator.transform,renderer.transform))]=renderer;
  view.aliases.Clear();foreach(var clip in clips)view.aliases[clip.source]=clip;
  if(animator.runtimeAnimatorController is AnimatorOverrideController native)
  {
   var overrides=new List<KeyValuePair<AnimationClip,AnimationClip>>();native.GetOverrides(overrides);
   foreach(var pair in overrides)if(view.aliases.TryGetValue(pair.Key.name,out var clip))view.aliases[(pair.Value??pair.Key).name]=clip;
  }
  Plugin.Trace("appearance-animation-bound",new{controller=animator.runtimeAnimatorController?.name,aliases=string.Join(",",view.aliases.Keys),paths=string.Join(",",view.renderers.Keys)});
 }
 internal static string Relative(Transform root,Transform target)
 {var parts=new List<string>();while(target!=root){parts.Add(target.name);target=target.parent;}parts.Reverse();return string.Join("/",parts);}
 internal static uint PathHash(string path)
 {uint crc=0xffffffff;foreach(byte value in System.Text.Encoding.UTF8.GetBytes(path)){crc^=value;for(int i=0;i<8;i++)crc=(crc>>1)^((crc&1)!=0?0xedb88320u:0);}return crc^0xffffffff;}
 void LateUpdate()
 {
  if(animator==null||!animator.isActiveAndEnabled||clips==null)return;
  var active=animator.GetCurrentAnimatorClipInfo(0);if(active.Length==0)return;
  if(!aliases.TryGetValue(active[0].clip.name,out var clip))return;
  float normalized=animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
  float time=(clip.loop?normalized-Mathf.Floor(normalized):Mathf.Clamp01(normalized))*clip.duration;
  foreach(var binding in renderers)
  {var frame=clip.frames.LastOrDefault(f=>f.path==binding.Key&&f.time<=time);if(frame!=null)binding.Value.sprite=Cosmetics.Sprite(frame.sprite);}
 }
}

public sealed class CosmeticFrames:MonoBehaviour
{
 public SpriteRenderer Renderer;public string[] Frames;public float Interval;float elapsed;
 void OnEnable(){elapsed=0;}
 void LateUpdate(){if(Renderer==null||Frames==null||Frames.Length==0)return;if(!PauseController.isPaused)elapsed+=Time.deltaTime;Renderer.sprite=Cosmetics.Sprite(Frames[(int)(elapsed/Interval)%Frames.Length]);}
}

// Only source-associated hero actors and player bullets receive this adapter.
// It is attached before pooling; each activation resolves the current run.
public sealed class CosmeticActor:MonoBehaviour
{
 public string Template;SpriteRenderer[] renderers;Sprite[] original;Cosmetics.Item skin;string groundSprite;bool bound;GameObject appearanceRoot;
 readonly List<SpriteRenderer> suppressed=new List<SpriteRenderer>();
 internal static void Configure(GameObject prefab)
 {
  bool associated=new[]{"PF_Crow","PF_LunaBlackHole","PF_LunaBlackHoleDamage","SpiritSummon"}.Contains(prefab.name);
  if(!associated&&prefab.layer!=(int)TagLayerUtil.PlayerProjectile&&prefab.GetComponent<ArrowRetrievalPoint>()==null&&!Cosmetics.ColorBindings.Any(b=>b.template==prefab.name))return;
  var owner=prefab.GetComponent<CosmeticActor>()??prefab.AddComponent<CosmeticActor>();owner.Template=prefab.name;
 }
 void Awake(){renderers=GetComponentsInChildren<SpriteRenderer>(true);original=renderers.Select(r=>r.sprite).ToArray();}
 void OnEnable(){bound=false;groundSprite=null;}
 void LateUpdate()
 {
  var run=AppearanceRun.Instance;if(run==null)return;
  if(!bound)
  {
   skin=run.Item(run.Character.character);string character=run.Character.owner;
   bool heroActor=Template=="PF_Crow"&&character=="Raven"||Template.StartsWith("PF_LunaBlackHole")&&character=="Luna"||Template=="SpiritSummon"&&character=="Lilith";
   if(!heroActor)skin=null;
   if(skin!=null)
   {
    foreach(var animator in GetComponentsInChildren<Animator>(true))CosmeticAnimation.Bind(animator,skin.summonClips);
   }
   var weapon=run.Item(run.Weapon.weapon);var bullet=run.Item(run.Weapon.bullet);
   var sourceDrop=Loadout.GunSelection.bullet.GetComponent<SpawnOnDisableProjectile>();
   if(weapon?.groundSprite!=null&&sourceDrop!=null&&((GameObject)AccessTools.Field(typeof(SpawnOnDisableProjectile),"prefab").GetValue(sourceDrop)).name==Template)groundSprite=weapon.groundSprite;
   if(weapon?.projectileVisuals!=null&&weapon.projectileVisuals.Length!=0&&Template==Loadout.GunSelection.bullet.name&&PlayerController.Instance.gun.gunData.bullet==Loadout.GunSelection.bullet)BuildProjectile(weapon);
   if(bullet!=null)ApplyColor(bullet.colorIndex);
   bound=true;
  }
  if(skin?.summonSprite!=null)foreach(var renderer in renderers)renderer.sprite=Cosmetics.Sprite(skin.summonSprite);
  if(groundSprite!=null)foreach(var renderer in renderers)renderer.sprite=Cosmetics.Sprite(groundSprite);
 }
 void BuildProjectile(Cosmetics.Item item)
 {
  appearanceRoot=new GameObject("CosmeticProjectileArtwork");appearanceRoot.transform.SetParent(transform,false);
  foreach(var renderer in renderers){renderer.forceRenderingOff=true;suppressed.Add(renderer);}
  foreach(var part in item.projectileVisuals)
  {
   var target=Characters.PoseChain(appearanceRoot.transform,part.poses);var renderer=target.gameObject.AddComponent<SpriteRenderer>();
   renderer.sprite=Cosmetics.Sprite(part.sprite);renderer.color=part.color;renderer.sharedMaterial=renderers[0].sharedMaterial;renderer.sortingLayerID=renderers[0].sortingLayerID;renderer.sortingOrder=renderers[0].sortingOrder+part.order;
   var frames=part.frames.Length!=0?part.frames:part.clips.FirstOrDefault()?.frames.Select(f=>f.sprite).ToArray();
   if(frames!=null&&frames.Length!=0){var animation=target.gameObject.AddComponent<CosmeticFrames>();animation.Renderer=renderer;animation.Frames=frames;animation.Interval=part.interval;}
  }
  PlayerVisualOpacity.Attach(appearanceRoot);
 }
 void ApplyColor(int index)
 {
  foreach(var binding in Cosmetics.ColorBindings.Where(b=>b.template==Template))
  {
   var target=binding.path.Length==0?transform:transform.Find(binding.path);if(target==null)continue;var color=binding.palette[index];
   var tint=target.GetComponent<CosmeticTint>()??target.gameObject.AddComponent<CosmeticTint>();tint.Configure(binding.channel,color);
  }
 }
 void OnDisable()
 {
  if(renderers!=null)for(int i=0;i<renderers.Length;i++)renderers[i].sprite=original[i];
  foreach(var renderer in suppressed)if(renderer!=null)renderer.forceRenderingOff=false;suppressed.Clear();
  if(appearanceRoot!=null){appearanceRoot.SetActive(false);Destroy(appearanceRoot);appearanceRoot=null;}
 }
}

public sealed class CosmeticTint:MonoBehaviour
{
 int channel;Color tint;Renderer renderer;MaterialPropertyBlock block,originalBlock;string property;TrailRenderer trail;ParticleSystem particles;Color original;Gradient originalTrail;ParticleSystem.MinMaxGradient originalParticles;bool configured;
 internal void Configure(int kind,Color color)
 {
  channel=kind;tint=color;renderer=GetComponent<Renderer>();trail=GetComponent<TrailRenderer>();particles=GetComponent<ParticleSystem>();
  if(block==null)block=new MaterialPropertyBlock();
  if(!configured){originalBlock=new MaterialPropertyBlock();renderer?.GetPropertyBlock(originalBlock);if(trail!=null)originalTrail=trail.colorGradient;if(particles!=null)originalParticles=particles.main.startColor;configured=true;}
  if(renderer!=null){property=renderer.sharedMaterial.HasProperty("_Color")?"_Color":renderer.sharedMaterial.HasProperty("_TintColor")?"_TintColor":null;if(property!=null)original=renderer.sharedMaterial.GetColor(property);}
  Apply();
 }
 void LateUpdate()=>Apply();
 void Apply()
 {
  if(!configured||renderer==null||property==null)return;
  renderer.GetPropertyBlock(block);var current=block.GetColor(property);float alpha=current==Color.clear?original.a:current.a;
  block.SetColor(property,new Color(tint.r,tint.g,tint.b,alpha));renderer.SetPropertyBlock(block);
  if(channel==1&&trail!=null){var gradient=trail.colorGradient;var colors=gradient.colorKeys;for(int i=0;i<colors.Length;i++)colors[i].color=new Color(tint.r,tint.g,tint.b,colors[i].color.a);gradient.SetKeys(colors,gradient.alphaKeys);trail.colorGradient=gradient;}
  if(channel==2&&particles!=null){var main=particles.main;main.startColor=tint;}
 }
 void OnDisable(){if(!configured)return;renderer?.SetPropertyBlock(originalBlock);if(trail!=null)trail.colorGradient=originalTrail;if(particles!=null){var main=particles.main;main.startColor=originalParticles;}configured=false;}
}
[HarmonyPatch(typeof(LoadoutInitializer),"Start")]
class AppearanceStart{[HarmonyPriority(Priority.Last)]static void Postfix()=>AppearanceRun.Attach();}
[HarmonyPatch(typeof(Gun),nameof(Gun.LoadGun))]
class AppearanceGun{[HarmonyPriority(Priority.Last)]static void Postfix()=>AppearanceRun.Instance?.ApplyGun();}

[HarmonyPatch(typeof(SpriteFX),"OnEnable")]
class AppearanceLunaEffect
{
 [HarmonyPriority(Priority.First)]static void Prefix(SpriteFX __instance,ref Sprite[] ___sprites)
 {
  var run=AppearanceRun.Instance;if(run==null||run.Character.owner!="Luna")return;
  var actor=__instance.GetComponentInParent<CosmeticActor>();if(actor==null||actor.Template!="PF_LunaBlackHoleDamage")return;
  var skin=run.Item(run.Character.character);if(skin==null||skin.summonFrames.Length==0)return;
  var restore=__instance.GetComponent<CosmeticLunaFrames>()??__instance.gameObject.AddComponent<CosmeticLunaFrames>();if(restore.Original==null)restore.Original=___sprites;
  ___sprites=skin.summonFrames.Select(Cosmetics.Sprite).ToArray();
 }
}
public sealed class CosmeticLunaFrames:MonoBehaviour
{public Sprite[] Original;void OnDisable(){if(Original!=null)Plugin.SetField(GetComponent<SpriteFX>(),"sprites",Original);}}

public sealed class CosmeticSlotImage:MonoBehaviour
{
 UnityEngine.UI.Image image;Sprite original;string owner,kind;float elapsed;bool applied;
 internal static void Bind(GameObject slot,string owner,string kind)
 {
  var image=slot.GetComponentsInChildren<UnityEngine.UI.Image>(true).FirstOrDefault(i=>i.name=="LoadoutItem");if(image==null)return;
  var view=image.GetComponent<CosmeticSlotImage>()??image.gameObject.AddComponent<CosmeticSlotImage>();
  if(view.image==null){view.image=image;view.original=image.sprite;}view.owner=owner;view.kind=kind;
 }
 void LateUpdate()
 {
  var skin=Cosmetics.Selected(owner,kind);if(skin==null){if(applied){image.sprite=original;applied=false;}return;}applied=true;elapsed+=Time.unscaledDeltaTime;
  var clips=kind=="weapon"?skin.previewClips:skin.clips;var clip=clips?.FirstOrDefault(c=>c.source.IndexOf("Idle",StringComparison.OrdinalIgnoreCase)>=0);
  if(clip==null){image.sprite=Cosmetics.Sprite(skin.icon);return;}
  var frame=clip.frames.LastOrDefault(f=>f.time<=(clip.duration>0?elapsed%clip.duration:0));if(frame!=null)image.sprite=Cosmetics.Sprite(frame.sprite);
 }
}
