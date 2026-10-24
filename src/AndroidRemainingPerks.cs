using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.CharacterPassives;
using flanne.PowerupSystem;
using flanne.PowerupSystems;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

// Rebuild the original source helper recipes, including Generator's paired
// stat helper. Matching native helpers retain their subscriptions/coroutines;
// changed source consumers and missing helpers are adapted explicitly.
[Serializable]
public sealed class AndroidCombatAttachment:flanne.PerkSystem.Action
{
 public string Key;
 public AndroidSoulLink SoulLink;
 public AndroidSoulPowered SoulPowered;
 public AndroidTomeElectro TomeElectro;
 public override void Activate(GameObject target)
 {
  if(Key=="pulsing_summons_name"){AndroidSummonPulse.Activate(this);return;}
  if(Key=="soul_link_name"){SoulLink.Apply();return;}
  if(Key=="soul_power_name"){SoulPowered.Apply();return;}
  if(Key=="tome_of_electro_name"){TomeElectro.Apply();return;}
  var root=new GameObject("Android_"+Key);root.transform.SetParent(PlayerController.Instance.transform,false);
  switch(Key)
  {
   case "pyro_mage_name":
    root.name="PF_BurnOnHit";
    var burn=root.AddComponent<BurnOnHit>();burn.burnDamge=3;burn.chanceToHit=.5f;break;
   case "splinter_name":
    var splinter=root.AddComponent<ShootBulletsOnDeath>();Plugin.SetField(splinter,"bulletOPTag","Projectile");Plugin.SetField(splinter,"numOfBullets",3);Plugin.SetField(splinter,"damageMultiplier",.15f);break;
   case "siege_name":Plugin.SetField(root.AddComponent<RefundAmmoWhenStanding>(),"chanceToRefund",.4f);break;
   case "ritual_name":
    root.name="PF_DamageBoostOnCurseKill";
    var ritual=root.AddComponent<DamageBuffOnCurseKill>();Plugin.SetField(ritual,"damageBuff",.01f);Plugin.SetField(ritual,"numKillsPerBuff",10f);ritual.enabled=!RunResume.Restoring;break;
   case "in_the_wind_name":
    root.name="PF_DmgMSBuffOnNoHurt";
    var wind=root.AddComponent<DmgAndMSOnNotHurt>();Plugin.SetField(wind,"damageBoostPerTick",.1f);Plugin.SetField(wind,"movespeedBoostPerTick",.1f);Plugin.SetField(wind,"secsPerTick",10f);Plugin.SetField(wind,"maxTicks",4);wind.enabled=!RunResume.Restoring;break;
   case "aged_dragon":
    root.name="PF_BuffDragonDamageOverTime";
    var dragonDamage=root.AddComponent<BuffSummonDamageOverTime>();Plugin.SetField(dragonDamage,"SummonTypeID","Dragon");Plugin.SetField(dragonDamage,"damageBuff",15);Plugin.SetField(dragonDamage,"secondsPerBuff",60f);dragonDamage.enabled=!RunResume.Restoring;break;
   case "trained_dragon_name":
    root.name="PF_BuffDragonAttackSpeedOverTime";
    var dragonSpeed=root.AddComponent<BuffSummonAttackSpeedOverTime>();Plugin.SetField(dragonSpeed,"SummonTypeID","Dragon");Plugin.SetField(dragonSpeed,"attackSpeedBuff",.1f);Plugin.SetField(dragonSpeed,"secondsPerBuff",60f);dragonSpeed.enabled=!RunResume.Restoring;break;
   case "generator_name":
    root.name="PF_FireRateBuffOnAmmoGain";
    Plugin.SetField(root.AddComponent<BuffPlayerStats>(),"statChanges",new[]{new StatChange{type=StatType.FireRate,value=.35f}});
    // Android restores attached helpers synchronously before their first Start.
    // Native reconstruction can yield for other components; keep this source
    // listener's first Start after the actual ammo restoration boundary.
    root.AddComponent<AndroidGenerator>().enabled=!RunResume.Restoring;break;
   case "diamond_perk_berserk_name":
    root.name="PF_BuffOnHurt";
    var berserk=root.AddComponent<BuffPlayerStats>();Plugin.SetField(berserk,"statChanges",new[]{new StatChange{type=StatType.FireRate,value=.1f},new StatChange{type=StatType.ReloadRate,value=.1f}});
    var hurt=new UnityEngine.Events.UnityEvent();hurt.AddListener(berserk.ApplyBuff);Plugin.SetField(root.AddComponent<OnPlayerHurtEvent>(),"onHurt",hurt);break;
   case "diamond_perk_bulk_up_name":
    root.name="PF_BuffOnHeal";
    var bulk=root.AddComponent<BuffPlayerStats>();Plugin.SetField(bulk,"statChanges",new[]{new StatChange{type=StatType.BulletDamage,value=.15f},new StatChange{type=StatType.ProjectileSize,value=.15f}});
    var healed=new UnityEngine.Events.UnityEvent();healed.AddListener(bulk.ApplyBuff);Plugin.SetField(root.AddComponent<OnPlayerHealedEvent>(),"onHealed",healed);break;
   case "kill_clip_name":Plugin.SetField(root.AddComponent<ReloadRateUpOnKill>(),"bonusPerStack",.05f);break;
   case "dragon_bond_name":Plugin.SetField(root.AddComponent<DragonBonusDamage>(),"percentOfDragonsDamage",.1f);break;
   case "electro_mage_name":
    var thunder=root.AddComponent<ThunderOnAttack>();Plugin.SetField(thunder,"baseDamage",22);Plugin.SetField(thunder,"numThunderPerAttack",1);Plugin.SetField(thunder,"shotsPerAttack",2);break;
   case "angelic_name":
    root.name="PF_HealOnSmiteKill";
    var angelic=root.AddComponent<HealOnSmiteKill>();Plugin.SetField(angelic,"killsToHeal",500);break;
   case "frost_mage_name":var freeze=root.AddComponent<FreezeOnHit>();freeze.chanceToHit=.35f;freeze.freezeDuration=1.5f;break;
   default:throw new InvalidOperationException("Unknown source combat attachment: "+Key);
  }
 }
}
[HarmonyPatch]
class AndroidCombatRecipes
{
 static readonly HashSet<string> Keys=new HashSet<string>{"splinter_name","siege_name","generator_name","diamond_perk_berserk_name","diamond_perk_bulk_up_name","ritual_name","in_the_wind_name","kill_clip_name","dragon_bond_name","aged_dragon","trained_dragon_name","electro_mage_name","tome_of_electro_name","soul_power_name","soul_link_name","angelic_name","pulsing_summons_name","frost_mage_name","pyro_mage_name"};
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);if(!Keys.Contains(key))return;
  var action=new AndroidCombatAttachment{Key=key};
  if(key=="soul_link_name"||key=="soul_power_name"||key=="tome_of_electro_name")
  {
   // Native Copy recursively duplicates managed recipes/Unity wrappers.
   // Resolve the callback state through the actual original profile asset.
   var profile=(PowerupPoolProfile)AccessTools.Field(typeof(PowerupGenerator),key=="tome_of_electro_name"?"devilDealProfile":"profile").GetValue(PowerupGenerator.Instance);
   var original=profile.powerupPool.Single(p=>RunResume.Key(p)==key);
   var effects=(PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(original);
   var source=effects.Select(e=>AccessTools.Field(typeof(PerkEffect),"action").GetValue(e)).OfType<AndroidCombatAttachment>().SingleOrDefault(a=>a.Key==key);
   if(key=="tome_of_electro_name")
   {
    var owner=source?.TomeElectro;
    if(owner==null)
    {owner=ScriptableObject.CreateInstance<AndroidTomeElectro>();owner.name="Android_TomeElectro_State";Configure(original,new AndroidCombatAttachment{Key=key,TomeElectro=owner});}
    action.TomeElectro=owner;
   }
   else if(key=="soul_power_name")
   {
    var owner=source?.SoulPowered;
    if(owner==null)
    {owner=ScriptableObject.CreateInstance<AndroidSoulPowered>();owner.name="Android_SoulPowered_State";Configure(original,new AndroidCombatAttachment{Key=key,SoulPowered=owner});}
    action.SoulPowered=owner;
   }
   else
   {
    var owner=source?.SoulLink;
    if(owner==null)
    {owner=ScriptableObject.CreateInstance<AndroidSoulLink>();owner.name="Android_SoulLink_State";Configure(original,new AndroidCombatAttachment{Key=key,SoulLink=owner});}
    action.SoulLink=owner;
   }
  }
  Configure(__instance,action);
 }
 static void Configure(Powerup powerup,AndroidCombatAttachment action)
 {
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",action);
  Plugin.SetField(powerup,"effects",new[]{effect});Plugin.SetField(powerup,"stackedEffects",new PerkEffect[0]);
 }
}
[HarmonyPatch(typeof(ReloadRateUpOnKill),"OnDeath")]
class AndroidKillClip
{
 static bool Prefix(float ___bonusPerStack,ref int ____stacks)
 {PlayerController.Instance.stats[StatType.ReloadRate].AddMultiplierBonus(___bonusPerStack);____stacks++;return false;}
}
[HarmonyPatch]
class AndroidDragonBondLifecycle
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(DragonBonusDamage),"Start"),AccessTools.Method(typeof(DragonBonusDamage),"OnDestroy")};
 static bool Prefix(DragonBonusDamage __instance,ref ShootingSummon ____dragon,MethodBase __originalMethod)
 {
  var callback=(Action<object,object>)Delegate.CreateDelegate(typeof(Action<object,object>),__instance,AccessTools.Method(typeof(DragonBonusDamage),"OnImpact"));
  if(__originalMethod.Name=="Start")
  {
   foreach(var summon in PlayerController.Instance.GetComponentsInChildren<ShootingSummon>(true))
    if(summon.SummonTypeID=="Dragon"){____dragon=summon;break;}
   __instance.AddObserver(callback,Projectile.ImpactEvent);
  }
  else __instance.RemoveObserver(callback,Projectile.ImpactEvent);
  return false;
 }
}
[HarmonyPatch(typeof(DragonBonusDamage),"OnImpact")]
class AndroidDragonBond
{
 static bool Prefix(ShootingSummon ____dragon,float ___percentOfDragonsDamage,object args)
 {
  if(____dragon==null)return false;var target=(GameObject)args;
  if(target.layer==(int)TagLayerUtil.Enemy)
  {var health=target.GetComponent<Health>();if(health!=null)AndroidDamageRounding.ChangeHealth(health,-Mathf.FloorToInt(____dragon.baseDamage*___percentOfDragonsDamage),DamageType.Summon);}
  return false;
 }
}
[HarmonyPatch(typeof(HealOnSmiteKill),"OnSmiteKill")]
class AndroidAngelicHealing
{
 static bool Prefix(int ___killsToHeal,ref int ____killCounter)
 {
  if(++____killCounter<___killsToHeal)return false;
  ____killCounter=0;
  // The original source helper's recover field is one. Its HPChange uses
  // Holy classification, unlike the Cure category of ordinary heart grants.
  AndroidPlayerCollision.Get(PlayerController.Instance.playerHealth).Change(1,DamageType.Smite);return false;
 }
}
[HarmonyPatch(typeof(ThunderOnAttack),nameof(ThunderOnAttack.Attack))]
class AndroidElectroMageTarget
{
 static bool Prefix(int ___baseDamage,int ___numThunderPerAttack)
 {for(int i=0;i<___numThunderPerAttack;i++){var target=AndroidSummonTargets.Random(7,4);if(target!=null)ThunderGenerator.SharedInstance.GenerateAt(target,___baseDamage);}return false;}
}
public sealed class AndroidTomeElectro:ScriptableObject
{
 // UnityEvent appends on every source Apply. Do not use NotificationCenter's
 // different delegate-deduplication rule for this original asset callback.
 internal void Apply()=>PlayerController.Instance.ammo.OnAmmoChanged.AddListener(Changed);
 void Changed(int amount){if(amount==0)ThunderGenerator.SharedInstance.GenerateAt(AndroidSummonTargets.Random(2,2),22);}
 void OnDestroy(){PlayerController.Instance.ammo.OnAmmoChanged.RemoveListener(Changed);}
}
public sealed class AndroidSoulPowered:ScriptableObject
{
 // Retain the old field name for typed checkpoint decoding. This is now the
 // original source asset's lastEffectValue, not a per-attachment grant.
 public float Granted;float effectValue;
 internal void Apply()
 {
  Revert();Granted=0;effectValue=.1f;
  PlayerController.Instance.playerHealth.shp+=0;
  this.AddObserver(Changed,AndroidSoulHealth.ChangeEvent);Changed(null,null);
 }
 void Revert()=>PlayerController.Instance.stats[StatType.BulletDamage].AddMultiplierBonus(-Granted);
 void Changed(object sender,object args)
 {Revert();Granted=effectValue*PlayerController.Instance.playerHealth.shp;PlayerController.Instance.stats[StatType.BulletDamage].AddMultiplierBonus(Granted);}
 void OnDestroy(){Revert();this.RemoveObserver(Changed,AndroidSoulHealth.ChangeEvent);}
}
[HarmonyPatch(typeof(ShootBulletsOnDeath),"ShootOnDeathCR")]
class AndroidSplinterDeaths
{
 static readonly FieldInfo Count=AccessTools.Field(typeof(ShootBulletsOnDeath),"currentBullets");
 static bool Prefix(GameObject deathObj,int ___numOfBullets,float ___damageMultiplier,ref IEnumerator __result)
 {__result=Fire(deathObj,___numOfBullets,___damageMultiplier);return false;}
 static IEnumerator Fire(GameObject death,int amount,float multiplier)
 {
  Count.SetValue(null,(int)Count.GetValue(null)+1);yield return null;
  Vector2 initial=Vector2.zero;while(initial==Vector2.zero)initial=new Vector2(UnityEngine.Random.Range(-1f,1f),UnityEngine.Random.Range(-1f,1f));
  for(int i=0;i<amount;i++)
  {var recipe=PlayerController.Instance.gun.GetProjectileRecipe();recipe.size=.5f;recipe.knockback*=.1f;recipe.projectileSpeed*=.35f;ProjectileFactory.SharedInstance.SpawnProjectile(recipe,initial.Rotate((float)i/amount*360),death.transform.position,multiplier,true);}
  yield return null;yield return null;Count.SetValue(null,(int)Count.GetValue(null)-1);
 }
}
[HarmonyPatch(typeof(OrbitalWeapon),"Init")]
class AndroidScytheRotation
{
 static void Prefix(OrbitalWeapon __instance,ref float ___baseRotationSpeed)
 {
  if(__instance.SummonTypeID!="Scythe")return;
  ___baseRotationSpeed=-160;
  // Original source Rigidbody2D2470 contacts bodyless monster colliders.
  __instance.GetComponent<Rigidbody2D>().useFullKinematicContacts=true;
 }
}

// Android uses set membership for the three Scythe flags, so repeated picks do
// not add repeated downstream damage modifiers. The player owns this build.
public sealed class AndroidWeaponRules:MonoBehaviour
{
 public readonly HashSet<string> Bullet=new HashSet<string>(),Movement=new HashSet<string>(),Curse=new HashSet<string>();
 internal static AndroidWeaponRules Get()
 {var player=PlayerController.Instance;var value=player.GetComponent<AndroidWeaponRules>();return value!=null?value:player.gameObject.AddComponent<AndroidWeaponRules>();}
}
[Serializable]
public sealed class AndroidScytheUpgrade:flanne.PerkSystem.Action
{
 public string Key;
 public override void Activate(GameObject target)
 {var rules=AndroidWeaponRules.Get();if(Key=="scythe_mastery_name")rules.Bullet.Add("Scythe");else if(Key=="wind_cutter_name")rules.Movement.Add("Scythe");else{CurseSystem.Instance.curseDamageMultiplier+=.15f;rules.Curse.Add("Scythe");}}
}
[HarmonyPatch]
class AndroidScytheRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  string key=RunResume.Key(__instance);if(key!="scythe_mastery_name"&&key!="wind_cutter_name"&&key!="shadowblade_name")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidScytheUpgrade{Key=key});
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
public struct AndroidSummonCollisionData
{
 internal const string Event="WeaponSummon.CollisionEvent";
 internal static AndroidSummonCollisionData Current;
 public int OtherID;public string SummonName;
}
[HarmonyPatch(typeof(WeaponSummon),"OnCollisionEnter2D")]
class AndroidWeaponContact
{
 static readonly FieldInfo Bonus=AccessTools.Field(typeof(StatMod),"_multiplierBonus"),Reduction=AccessTools.Field(typeof(StatMod),"_multiplierReduction");
 static float Multiplier(StatMod stat)=>(1+(float)Bonus.GetValue(stat))*(float)Reduction.GetValue(stat)*CustomMode.StatFactor(stat);
 static bool Prefix(WeaponSummon __instance,Collision2D other)
 {
  var target=other.gameObject;var health=target.GetComponent<Health>();if(health==null)return false;
  float damage=__instance.summonDamageMod.Modify(__instance.baseDamage);var rules=PlayerController.Instance.GetComponent<AndroidWeaponRules>();
  if(rules!=null)
  {
   string id=__instance.SummonTypeID;if(rules.Curse.Contains(id))CurseSystem.Instance.Curse(target);
   float movement=Multiplier(PlayerController.Instance.stats[StatType.MoveSpeed]),bullet=Multiplier(PlayerController.Instance.stats[StatType.BulletDamage]);
   if(rules.Movement.Contains(id)&&movement>0)damage*=movement;if(rules.Bullet.Contains(id)&&bullet>0)damage*=bullet;
  }
  AndroidDamageRounding.ChangeHealth(health,Mathf.FloorToInt(-damage),DamageType.Summon);
  // Source captures the name and collider ID after damage, then boxes the
  // static value. Reentrant hits cannot rewrite an already posted payload.
  AndroidSummonCollisionData.Current.SummonName=__instance.SummonTypeID;
  AndroidSummonCollisionData.Current.OtherID=other.gameObject.GetInstanceID();
  __instance.PostNotification(AndroidSummonCollisionData.Event,AndroidSummonCollisionData.Current);return false;
 }
}
[Serializable]
public sealed class AndroidEnhancedShadow:flanne.PerkSystem.Action
{
 public override void Activate(GameObject target)
 {var passive=PlayerController.Instance.GetComponentInChildren<ShadowClonePassive>();var clone=(GameObject)AccessTools.Field(typeof(ShadowClonePassive),"shadowClone").GetValue(passive);var summon=clone.GetComponent<ShootingSummon>();var cooldown=AccessTools.Field(typeof(AttackingSummon),"attackCooldown");cooldown.SetValue(summon,(float)cooldown.GetValue(summon)*.33f);}
}
[HarmonyPatch]
class AndroidEnhancedShadowRecipe
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  if(RunResume.Key(__instance)!="hina_perk_enhanced_shadow_name")return;
  var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new AndroidEnhancedShadow());
  Plugin.SetField(__instance,"effects",new[]{effect});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
[HarmonyPatch(typeof(BulletGlyph),"ShootBullets")]
class AndroidGlyphDamage
{
 static bool Prefix(BulletGlyph __instance,int ___numOfBullets)
 {
  Vector2 initial=Vector2.zero;while(initial==Vector2.zero)initial=new Vector2(UnityEngine.Random.Range(-1f,1f),UnityEngine.Random.Range(-1f,1f));
  var player=PlayerController.Instance;
  for(int i=0;i<___numOfBullets;i++)
  {var recipe=player.gun.GetProjectileRecipe();recipe.size*=.5f;recipe.damage=player.stats[StatType.SummonDamage].Modify(recipe.damage)*.1f;ProjectileFactory.SharedInstance.SpawnProjectile(recipe,initial.Rotate((float)i/___numOfBullets*360),__instance.transform.position,1,true);}
  return false;
 }
}
