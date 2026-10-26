using System;
using System.Linq;
using UnityEngine;
namespace TwentyMinutes.Modernization;

// Texture sheets do not color procedural trails or particle vertex colors.
// Own only those channels; leave materials, geometry, timing and alpha intact.
public sealed class ColorfulProjectileVisuals:MonoBehaviour
{
 public string Template;
 TrailRenderer[] trails;Gradient[] originalTrails;
 ParticleSystem[] particles;ParticleSystem.MinMaxGradient[] originalParticles;
 ParticleSystemRenderer[] renderers;MaterialPropertyBlock[] originalBlocks;string[] colorProperties;
 SpriteRenderer[] sprites;Color[] originalSprites;
 internal static void Configure(GameObject prefab)
 {
  if(prefab.layer!=(int)TagLayerUtil.PlayerProjectile)return;
  if(prefab.GetComponentsInChildren<Renderer>(true).Length==0)return;
  var style=prefab.GetComponent<ColorfulProjectileVisuals>();if(style==null)style=prefab.AddComponent<ColorfulProjectileVisuals>();style.Template=prefab.name;
 }
 void Awake()
 {
  trails=GetComponentsInChildren<TrailRenderer>(true);originalTrails=trails.Select(t=>t.colorGradient).ToArray();
  particles=GetComponentsInChildren<ParticleSystem>(true);originalParticles=particles.Select(p=>p.main.startColor).ToArray();
  sprites=GetComponentsInChildren<SpriteRenderer>(true);originalSprites=sprites.Select(s=>s.color).ToArray();
  renderers=particles.Select(p=>p.GetComponent<ParticleSystemRenderer>()).ToArray();originalBlocks=new MaterialPropertyBlock[particles.Length];colorProperties=new string[particles.Length];
  for(int i=0;i<particles.Length;i++)
  {
   originalBlocks[i]=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(originalBlocks[i]);
   var material=renderers[i].sharedMaterial;colorProperties[i]=material.HasProperty("_Color")?"_Color":material.HasProperty("_TintColor")?"_TintColor":null;
  }
 }
 void OnEnable()=>Apply();
 internal void Apply()
 {
  if(trails==null)return;
  bool enabled=Plugin.Instance.ColorfulEnabled.Value;Color tint=Palette(Template??name);
  for(int i=0;i<trails.Length;i++)trails[i].colorGradient=enabled?Tint(originalTrails[i],tint):originalTrails[i];
  for(int i=0;i<sprites.Length;i++)sprites[i].color=enabled&&ColorfulStyle.Instance.NeedsProjectileTint(sprites[i].sprite)?Tint(originalSprites[i],tint):originalSprites[i];
  for(int i=0;i<particles.Length;i++)
  {
   renderers[i].SetPropertyBlock(originalBlocks[i]);
   // A shader tint affects particles already emitted, including one-shot
   // bursts. Changing startColor would affect only future particles.
   if(!enabled||colorProperties[i]==null)continue;
   var source=originalParticles[i];
   if(source.mode!=ParticleSystemGradientMode.Color||Tint(source.color,tint)==source.color)continue;
   var block=new MaterialPropertyBlock();renderers[i].GetPropertyBlock(block);
   Color color=block.GetColor(colorProperties[i]);if(color==Color.clear)color=renderers[i].sharedMaterial.GetColor(colorProperties[i]);
   block.SetColor(colorProperties[i],new Color(color.r*tint.r,color.g*tint.g,color.b*tint.b,color.a));renderers[i].SetPropertyBlock(block);
  }
 }
 static Color Palette(string name)
 {
  name=name.ToLowerInvariant();
  if(name.Contains("fire")||name.Contains("flame")||name.Contains("dragon"))return new Color(1,.55f,.28f);
  if(name.Contains("ice")||name.Contains("frost")||name.Contains("hail"))return new Color(.59f,.83f,.95f);
  if(name.Contains("thunder")||name.Contains("electro")||name.Contains("lightning"))return new Color(1,.86f,.49f);
  if(name.Contains("bat")||name.Contains("spirit")||name.Contains("ghost"))return new Color(.72f,.63f,.87f);
  if(name.Contains("shotgun"))return new Color(.96f,.76f,.46f);
  return new Color(.68f,.84f,.79f);
 }
 static Color Tint(Color source,Color tint)
 {
  // Colored source accents retain their hue; replace white/cream body colors.
  float high=Mathf.Max(source.r,Mathf.Max(source.g,source.b)),low=Mathf.Min(source.r,Mathf.Min(source.g,source.b));
  if(high-low>.3f||low<.5f)return source;
  return new Color(high*tint.r,high*tint.g,high*tint.b,source.a);
 }
 static Gradient Tint(Gradient source,Color tint)
 {
  var result=new Gradient{mode=source.mode};var colors=source.colorKeys;
  for(int i=0;i<colors.Length;i++)colors[i].color=Tint(colors[i].color,tint);
  result.SetKeys(colors,source.alphaKeys);return result;
 }
}
