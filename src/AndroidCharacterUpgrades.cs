using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.CharacterPassives;
using flanne.PerkSystem;
using flanne.PerkSystem.Actions;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

public sealed class AndroidReloadSwords:MonoBehaviour
{public int Amount=1,Counter;}
[HarmonyPatch(typeof(SpawnSummonOnReload),"Start")]
class AndroidSwordReloadOwner
{static void Prefix(SpawnSummonOnReload __instance)=>__instance.gameObject.AddComponent<AndroidReloadSwords>();}
[HarmonyPatch(typeof(SpawnSummonOnReload),"OnReload")]
class AndroidSwordReload
{
 static readonly MethodInfo Spawn=AccessTools.Method(typeof(SpawnSummonOnReload),"Spawn");
 static bool Prefix(SpawnSummonOnReload __instance)
 {var state=__instance.GetComponent<AndroidReloadSwords>();if(state.Counter++!=0)for(int i=0;i<state.Amount;i++)Spawn.Invoke(__instance,null);return false;}
}

public sealed class AndroidFireEdge:ScriptableObject
{
 internal void Apply(){this.AddObserver(OnImpact,Projectile.ImpactEvent);this.AddObserver(OnSummon,AndroidSummonCollisionData.Event);}
 // Source removes the projectile callback but ADDS the summon callback here.
 void OnDestroy(){this.RemoveObserver(OnImpact,Projectile.ImpactEvent);this.AddObserver(OnSummon,AndroidSummonCollisionData.Event);}
 void OnImpact(object sender,object args)=>Impact(((GameObject)args).GetInstanceID());
 void Impact(int id)
 {
  var target=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetFromID(id).gameObject;
  if(target==null)return;
  // Both source monster visibility layers map to the native Enemy layer.
  if(target.layer==(int)TagLayerUtil.Enemy&&UnityEngine.Random.Range(0f,1f)<.5f)BurnSystem.SharedInstance.Burn(target,6);
 }
 void OnSummon(object sender,object args){var hit=(AndroidSummonCollisionData)args;if(hit.SummonName=="MagicSword")Impact(hit.OtherID);}
}
[HarmonyPatch(typeof(CurseOnCollision),"OnCollisionEnter2D")]
class AndroidCrowCollision
{
 // Source Crow owns one static value, assigned by Shadow Flame and cleared
 // after pool release on retry/title. It is not owned by the current player.
 internal static float BurnMultiplier;
 internal static void Clear()=>BurnMultiplier=0;
 static bool Prefix(CurseOnCollision __instance,Collision2D __0)
 {
  var summon=__instance.GetComponent<SeekerWeapon>();if(summon==null||summon.SummonTypeID!="Crow")return true;
  var target=__0.gameObject;if(target.layer!=(int)TagLayerUtil.Enemy)return false;
  var curse=CurseSystem.Instance;curse.Curse(target);float multiplier=BurnMultiplier;
  if(multiplier>0)BurnSystem.SharedInstance.Burn(target,Mathf.FloorToInt(multiplier*(PlayerController.Instance.gun.damage*curse.curseDamageMultiplier)));return false;
 }
}

[Serializable]
public sealed class AndroidCharacterUpgrade:flanne.PerkSystem.Action
{
 public string Key;public flanne.PerkSystem.Action Attachment;public AndroidFireEdge FireEdge;
 public override void Init(){Attachment?.Init();}
 public override void Activate(GameObject target)
 {
  if(Key=="luna_perk_parallel_star_name")
  {
   var prefab=(GameObject)AccessTools.Field(typeof(AttachPrefabToPlayerAction),"prefab").GetValue(Attachment);
   Plugin.SetField(prefab.GetComponent<SpawnObjectTowardsCursorOnReload>(),"distanceFromPlayerToSpawn",-4f);
  }
  Attachment?.Activate(target);
  switch(Key)
  {
   case "katana_perk_fire_edge_name":FireEdge.Apply();break;
   case "katana_perk_sword_mastery_name":AndroidWeaponRules.Get().Bullet.Add("MagicSword");break;
   case "katana_perk_thousand_blades_name":foreach(var owner in UnityEngine.Object.FindObjectsOfType<AndroidReloadSwords>())owner.Amount++;break;
   case "raven_perk_flock_of_crows_name":AndroidWeaponRules.Get().Bullet.Add("Crow");break;
   case "raven_perk_shadow_flame_name":AndroidCrowCollision.BurnMultiplier=.35f;break;
   case "scarlett_perk_pyro_affinity_name":BurnSystem.SharedInstance.burnDamageMultiplier.AddMultiplierBonus(.67f);break;
  }
 }
}
[HarmonyPatch]
class AndroidCharacterUpgradeRecipes
{
 static readonly HashSet<string> Keys=new HashSet<string>{"katana_perk_fire_edge_name","katana_perk_sword_mastery_name","katana_perk_thousand_blades_name","raven_perk_flock_of_crows_name","raven_perk_shadow_flame_name","scarlett_perk_pyro_affinity_name","luna_perk_parallel_star_name","dasher_perk_deer_friend_name"};
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);if(!Keys.Contains(key))return;
  if(key=="katana_perk_fire_edge_name")
  {
   var original=Loadout.CharacterSelection.exclusivePowerups.powerupPool.Single(p=>RunResume.Key(p)==key);
   var recipe=((PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(original)).Select(e=>AccessTools.Field(typeof(PerkEffect),"action").GetValue(e)).OfType<AndroidCharacterUpgrade>().SingleOrDefault();
   var owner=recipe?.FireEdge;
   if(owner==null){owner=ScriptableObject.CreateInstance<AndroidFireEdge>();owner.name="Android_FireEdge_State";Configure(original,new AndroidCharacterUpgrade{Key=key,FireEdge=owner});}
   Configure(__instance,new AndroidCharacterUpgrade{Key=key,FireEdge=owner});return;
  }
  var effects=(PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(__instance);
  if(effects.Any(e=>AccessTools.Field(typeof(PerkEffect),"action").GetValue(e) is AndroidCharacterUpgrade))return;
  var action=new AndroidCharacterUpgrade{Key=key};
  if(key=="raven_perk_flock_of_crows_name"||key=="luna_perk_parallel_star_name"||key=="dasher_perk_deer_friend_name")action.Attachment=effects.Select(e=>(flanne.PerkSystem.Action)AccessTools.Field(typeof(PerkEffect),"action").GetValue(e)).OfType<AttachPrefabToPlayerAction>().Single();
  Configure(__instance,action);
 }
 static void Configure(Powerup powerup,AndroidCharacterUpgrade action)
 {var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",action);Plugin.SetField(powerup,"effects",new[]{effect});Plugin.SetField(powerup,"stackedEffects",new PerkEffect[0]);}
}
[HarmonyPatch(typeof(DasherSpecialProjectileAction),nameof(DasherSpecialProjectileAction.Activate))]
class AndroidHailBringer
{
 static bool Prefix(){PlayerController.Instance.GetComponentInChildren<DasherSpecial>().projectiles=1;return false;}
}
[HarmonyPatch(typeof(SpawnObjectTowardsCursorOnReload),"OnReload")]
class AndroidReloadObjectPlacement
{
 static bool Prefix(SpawnObjectTowardsCursorOnReload __instance,ShootingCursor ___SC,GameObject ___prefab,float ___distanceFromPlayerToSpawn,SoundEffectSO ___soundFX)
 {
  // Preserve native mouse/controller aiming in place of Android's joystick.
  Vector2 origin=PlayerController.Instance.transform.position;
  Vector2 aim=Camera.main.ScreenToWorldPoint(___SC.cursorPosition);
  Vector2 position=origin+(aim-origin).normalized*___distanceFromPlayerToSpawn;
  // Android supplies the final position before the pool activates the actor.
  ObjectPooler.SharedInstance.GetComponent<AndroidPools>().Get(___prefab.name,position,null);
  ___soundFX?.Play();return false;
 }
}
