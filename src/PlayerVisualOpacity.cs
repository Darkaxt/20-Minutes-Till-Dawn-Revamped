using System.Linq;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization;

// Independent shader tint alpha preserves native sprite/particle animation,
// vertex colors and gameplay geometry. Each renderer has one tint owner.
public sealed class PlayerVisualOpacity:MonoBehaviour
{
 public enum Channel{Projectiles,Effects}
 public Channel Kind;
 Renderer view;Material material;MaterialPropertyBlock original,block;int property;float nativeAlpha;
 public float AppliedOpacity{get;private set;}=1;
 internal static float Factor(Channel kind)=>(kind==Channel.Projectiles?Plugin.Instance.ProjectileOpacity.Value:Plugin.Instance.EffectsOpacity.Value)/100f;
 internal static void Attach(GameObject root)
 {
  foreach(var renderer in root.GetComponentsInChildren<Renderer>(true))
  {
   if(renderer.GetComponentInParent<Canvas>()!=null||renderer.GetComponentsInParent<Transform>().Any(t=>t.name.Contains("Fog")||t.name.Contains("Revealer")))continue;
   if(renderer.GetComponent<PlayerVisualOpacity>()!=null)continue;
   var owner=renderer.gameObject.AddComponent<PlayerVisualOpacity>();owner.Kind=renderer is TrailRenderer||renderer is ParticleSystemRenderer||renderer.GetComponentInParent<Projectile>()==null?Channel.Effects:Channel.Projectiles;owner.Apply();
  }
 }
 internal static void CombatActor(GameObject root)
 {
  if(root==null)return;string source=AndroidPoolTables.PrefabName(root.name.Replace("(Clone)",""));
  if(root.GetComponent<AIComponent>()!=null||root.GetComponent<Health>()!=null||root.layer==LayerMask.NameToLayer("EnemyProjectile")||source=="EyeMonsterProjectile"||source=="PF_CultistProjectile"||source=="BoomerExplosion"||source=="BigBoomerExplosion"||source=="PF_DeathAnimation")return;
  if(root.GetComponent<flanne.Pickups.ChestPickup>()!=null||root.GetComponent<flanne.Pickups.XPPickup>()!=null||root.GetComponent<flanne.Pickups.HPPickup>()!=null||root.GetComponent<flanne.Pickups.SHPPickup>()!=null)return;
  bool combat=root.GetComponent<Projectile>()!=null||root.GetComponentInChildren<Summon>(true)!=null||root.GetComponentInChildren<SummonEgg>(true)!=null||root.GetComponentInChildren<Harmful>(true)!=null||root.GetComponentInChildren<SpriteFX>(true)!=null||root.name=="Revamped_ElementPulse"||root.name.Contains("Lightning")||root.name.Contains("Thunder")||root.name.Contains("Trail");
  if(combat)Attach(root);
 }
 void Awake(){view=GetComponent<Renderer>();original=new MaterialPropertyBlock();block=new MaterialPropertyBlock();}
 void OnEnable(){if(view!=null)Apply();}
 void LateUpdate()=>Apply();
 internal void Apply()
 {
  if(view==null||view.sharedMaterial==null)return;
  if(material!=view.sharedMaterial)
  {
   material=view.sharedMaterial;string name=material.HasProperty("_Color")?"_Color":material.HasProperty("_TintColor")?"_TintColor":material.HasProperty("_FaceColor")?"_FaceColor":null;
   property=name==null?0:Shader.PropertyToID(name);if(property==0)return;
   view.GetPropertyBlock(original);var tint=original.GetColor(property);nativeAlpha=tint==Color.clear?material.GetColor(property).a:tint.a;
  }
  if(property==0)return;
  view.GetPropertyBlock(block);var color=block.GetColor(property);if(color==Color.clear)color=material.GetColor(property);
  AppliedOpacity=Factor(Kind);color.a=nativeAlpha*AppliedOpacity;block.SetColor(property,color);view.SetPropertyBlock(block);
 }
 void OnDisable(){if(view!=null&&original!=null)view.SetPropertyBlock(original);}
}

// Legacy character helpers create/replace child effects during play. Discover
// their visual renderers without touching the character, gun art or fog mask.
public sealed class AttachedPlayerVisuals:MonoBehaviour
{
 PlayerController player;
 void Awake(){player=GetComponent<PlayerController>();}
 void LateUpdate()
 {
  if(player.playerSprite==null)return;var game=RunRecorder.Instance?.GetComponent<GameController>();
  foreach(var renderer in GetComponentsInChildren<Renderer>(true))
  {
   if(renderer==player.playerSprite||renderer.GetComponentInParent<Canvas>()!=null||renderer.GetComponent<PlayerVisualOpacity>()!=null)continue;
   if(game!=null&&renderer.transform.IsChildOf(game.playerFogRevealer.transform))continue;
   if(renderer.GetComponentsInParent<Transform>().Any(t=>t.name.Contains("Fog")||t.name.Contains("Revealer")))continue;
   if(player.gun!=null&&renderer.transform.IsChildOf(player.gun.gunObj.transform)&&!(renderer is ParticleSystemRenderer)&&!(renderer is TrailRenderer)&&renderer.GetComponentInParent<SpriteFX>()==null)continue;
   PlayerVisualOpacity.Attach(renderer.gameObject);
  }
 }
}
[HarmonyPatch(typeof(PlayerController),"Awake")]
class PlayerOpacityDiscovery{static void Postfix(PlayerController __instance)=>__instance.gameObject.AddComponent<AttachedPlayerVisuals>();}
[HarmonyPatch(typeof(AndroidPools),"Acquire")]
class PooledCombatOpacity{static void Postfix(GameObject __result)=>PlayerVisualOpacity.CombatActor(__result);}
[HarmonyPatch(typeof(Summon),"Start")]
class SummonVisualOpacity{static void Postfix(Summon __instance)=>PlayerVisualOpacity.Attach(__instance.gameObject);}
