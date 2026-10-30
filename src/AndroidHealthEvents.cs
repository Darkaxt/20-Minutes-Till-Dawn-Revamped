using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using flanne;
using flanne.Audio;
namespace TwentyMinutes.Modernization;

// The shipped NPC Health.Hurt/Death methods call their feedback/cleanup
// directly. Their serialized onHurt/onDeath events are not that dispatch path.
// The latter remains a distinct path used by the maxHP setter.
public sealed class AndroidHealthEvents:MonoBehaviour
{
 internal static readonly AccessTools.FieldRef<Health,int> HP=AccessTools.FieldRefAccess<Health,int>("<HP>k__BackingField");
 static readonly AccessTools.FieldRef<Health,int> Maximum=AccessTools.FieldRefAccess<Health,int>("_maxHP");
 Health health;AndroidHealthTables.Recipe recipe;
 public bool Fragile;
 public int CreatureType;
 internal static AndroidHealthEvents Source(Health target)=>target.GetComponent<AndroidHealthEvents>();
 internal static void Bind(Health target)
 {
  string name=target.name;if(name.EndsWith("(Clone)"))name=name.Substring(0,name.Length-7);
  name=AndroidPoolTables.PrefabName(name);
  if(!AndroidHealthTables.Recipes.TryGetValue(name,out var source))return;
  var owner=Source(target);
  if(owner==null){Maximum(target)=source.MaxHP;owner=target.gameObject.AddComponent<AndroidHealthEvents>();owner.CreatureType=source.CreatureType;}
  owner.health=target;owner.recipe=source;
  // Reconstruct the original source event fields too: maxHP <= 0 invokes
  // this event even though ordinary damage and AutoKill do not.
  target.onDeath=new UnityEvent();
  foreach(string callback in source.Callbacks)
  {
   if(callback=="PoolRelease")target.onDeath.AddListener(()=>AndroidPools.Return(target.gameObject));
   else if(callback.StartsWith("Spawn:"))
   {string tag=callback.Substring(6);target.onDeath.AddListener(()=>target.GetComponent<SpawnFromObjectPool>().Spawn(tag));}
   else if(callback=="SpawnPrefab")target.onDeath.AddListener(()=>target.GetComponent<SpawnPrefabFromObjectPool>().Spawn());
   else if(callback=="Play")target.onDeath.AddListener(()=>target.GetComponentsInChildren<PlaySoundEffect>(true).Single(p=>p.name=="DeathSFX").Play());
   else if(callback=="Shake")target.onDeath.AddListener(()=>target.GetComponent<ExplosionShake2D>().Shake());
  }
 }
 internal void Change(int requested,DamageType kind,float finalMultiplier=1,string popupText=null)
 {
  if(requested<0&&health.HP<1)return;
  if(requested<0&&health.isInvincible.value){health.onDamagePrevented.Invoke();return;}
  if(requested<0&&Fragile){Death(true,DamageType.Bullet);return;}
  int change=AndroidDamageRounding.SignedModifiers(requested,"Health.Tweak"+kind+"Damage",health);
  change=AndroidDamageRounding.SignedModifiers(change,Health.TweakDamageEvent,health);
  // This argument belongs to the native API boundary. Source-shaped callers
  // already supply their final integer and keep the exact default-one path.
  if(finalMultiplier!=1)change=Mathf.FloorToInt(change*finalMultiplier);
  int previous=health.HP;HP(health)=Mathf.Clamp(previous+change,0,health.maxHP);
  health.onHealthChange.Invoke(health.HP);
  if(change<0)Hurt(change,popupText);else if(change>0)health.onHeal.Invoke(change);
  if(previous==0)return;
  if(health.HP>0)health.PostNotification(AndroidSurvivingHurt.Event,kind);
  else if(health.HP==0)Death(true,kind);
 }
 void Hurt(int change,string popupText)
 {
  health.GetComponentInChildren<PlaySoundEffect>().Play();
  health.GetComponent<FlashSprite>().Flash();
  AndroidDamagePopup.PopDamage(Mathf.Abs(change),health.transform,popupText);
 }
 internal void Death(bool notify,DamageType kind)
 {
  // Source observers see the actor before release and can synchronously
  // activate other trees or trigger upgrades. Arena owners poll separately.
  if(notify)health.PostNotification(Health.DeathEvent,kind);
  AndroidPools.Return(health.gameObject);
  health.GetComponentInChildren<PlaySoundEffect>().Play();
  foreach(string tag in recipe.Drops)health.GetComponent<SpawnFromObjectPool>().Spawn(tag);
  if(recipe.Shake)health.GetComponent<ExplosionShake2D>().Shake();
 }
}
[HarmonyPatch(typeof(Health),"Awake")]
class AndroidHealthEventOwner{static void Postfix(Health __instance)=>AndroidHealthEvents.Bind(__instance);}
[HarmonyPatch(typeof(Health),nameof(Health.TakeDamage))]
class AndroidHealthDamage
{
 static bool Prefix(Health __instance,DamageType damageType,int damage,float finalMultiplier)
 {var owner=AndroidHealthEvents.Source(__instance);if(owner==null)return true;owner.Change(-damage,damageType,finalMultiplier);return false;}
}
[HarmonyPatch(typeof(Health),nameof(Health.HPChange))]
class AndroidHealthChange
{
 static bool Prefix(Health __instance,int change)
 {var owner=AndroidHealthEvents.Source(__instance);if(owner==null)return true;owner.Change(change,DamageType.Bullet);return false;}
}
[HarmonyPatch(typeof(Health),nameof(Health.AutoKill))]
class AndroidHealthAutoKill
{
 static bool Prefix(Health __instance,bool notify)
 {var owner=AndroidHealthEvents.Source(__instance);if(owner==null)return true;if(__instance.HP>0){AndroidHealthEvents.HP(__instance)=0;owner.Death(notify,DamageType.Bullet);}return false;}
}
