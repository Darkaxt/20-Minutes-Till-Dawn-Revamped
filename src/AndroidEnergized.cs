using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Actions;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

// Source Energized attaches a distinct AmmoOnThunder helper per pick. It has
// no source runtime-save handler; Start reconstructs its cached owner/pool.
public sealed class AndroidAmmoOnThunder:MonoBehaviour
{
 public GameObject StaticPrefab;
 public SoundEffectSO Sound;
 Ammo ammo;AndroidPools pool;
 void Start()
 {
  ammo=transform.parent.GetComponentInChildren<Ammo>();
  pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();
  pool.Register("StaticFX",StaticPrefab,-1);
  transform.localPosition=Vector3.zero;
  this.AddObserver(OnThunderHit,ThunderGenerator.ThunderHitEvent);
 }
 void OnDestroy()=>this.RemoveObserver(OnThunderHit,ThunderGenerator.ThunderHitEvent);
 void OnThunderHit(object sender,object args)
 {
  if(UnityEngine.Random.Range(0f,1f)>=.2f)return;
  if(ammo!=null)
  {
   ammo.GainAmmo(3);
   pool.Get("StaticFX",transform.position,transform).transform.localPosition=Vector3.zero;
   Sound.Play();
  }
  else Debug.LogWarning("No ammo component found");
 }
}
[Serializable]
public sealed class AndroidEnergizedAttach:flanne.PerkSystem.Action
{
 public GameObject StaticPrefab;
 public SoundEffectSO Sound;
 public override void Activate(GameObject target)
 {
  var root=new GameObject("PF_AmmoOnThunder");root.transform.SetParent(target.transform,false);
  var helper=root.AddComponent<AndroidAmmoOnThunder>();helper.StaticPrefab=StaticPrefab;helper.Sound=Sound;
 }
}
[HarmonyPatch]
class AndroidEnergizedRecipe
{
 static readonly FieldInfo Effects=AccessTools.Field(typeof(Powerup),"effects"),Action=AccessTools.Field(typeof(PerkEffect),"action");
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  if(RunResume.Key(__instance)!="energized_name")return;
  var profile=(PowerupPoolProfile)AccessTools.Field(typeof(PowerupGenerator),"profile").GetValue(PowerupGenerator.Instance);
  var original=profile.powerupPool.Single(p=>RunResume.Key(p)=="energized_name");
  var actions=((PerkEffect[])Effects.GetValue(original)).Select(e=>(flanne.PerkSystem.Action)Action.GetValue(e)).ToArray();
  var recipe=actions.OfType<AndroidEnergizedAttach>().SingleOrDefault();
  if(recipe==null)
  {
   recipe=new AndroidEnergizedAttach{
    StaticPrefab=(GameObject)AccessTools.Field(typeof(SpawnPrefabAction),"prefab").GetValue(actions.OfType<SpawnPrefabAction>().Single()),
    Sound=(SoundEffectSO)AccessTools.Field(typeof(PlaySoundAction),"soundFX").GetValue(actions.OfType<PlaySoundAction>().Single())};
   Configure(original,recipe);
  }
  // Resolve original Unity references instead of retaining a copied wrapper.
  Configure(__instance,recipe);
 }
 static void Configure(Powerup power,AndroidEnergizedAttach recipe)
 {
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());
  Plugin.SetField(effect,"action",new AndroidEnergizedAttach{StaticPrefab=recipe.StaticPrefab,Sound=recipe.Sound});
  Plugin.SetField(power,"effects",new[]{effect});Plugin.SetField(power,"stackedEffects",new PerkEffect[0]);
 }
}
