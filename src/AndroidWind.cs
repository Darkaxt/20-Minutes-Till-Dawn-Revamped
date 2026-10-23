using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
using flanne.PerkSystem.Actions;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

public sealed class AndroidWind:MonoBehaviour
{
 internal const string PoolTag="Revamped_AndroidGale";
 internal GameObject prototype,original;
 internal int BonusDamage;
 internal bool ScaleMovement,Eye;
 PlayerController player;
 public int Damage=>Mathf.FloorToInt((20+BonusDamage)*(ScaleMovement?player.stats[StatType.MoveSpeed].Modify(1):1));
 internal static AndroidWind Current
 {
  get{var p=PlayerController.Instance;var wind=p.GetComponent<AndroidWind>();return wind!=null?wind:p.gameObject.AddComponent<AndroidWind>();}
 }
 void Awake(){player=GetComponent<PlayerController>();}
 internal void Prepare(GameObject source)
 {
  if(prototype!=null)return;
  original=source;prototype=Object.Instantiate(source);prototype.SetActive(false);prototype.name=PoolTag;
  // Only the owned inactive template is changed. Android's prefab has neither
  // the Windows Projectile callbacks nor its HashSet-based PersistentHarm.
  Object.DestroyImmediate(prototype.GetComponent<Projectile>());
  Object.DestroyImmediate(prototype.GetComponent<PersistentHarm>());
  Plugin.SetField(prototype.GetComponent<TimeToLive>(),"lifetime",4.5f);
  prototype.AddComponent<AndroidWindActor>();ObjectPooler.SharedInstance.AddObject(PoolTag,prototype,30);
 }
 internal void Cast()
 {
  var gale=ObjectPooler.SharedInstance.GetPooledObject(PoolTag);
  int angle=UnityEngine.Random.Range(-60,60);
  Vector2 aim=(Vector2)Camera.main.ScreenToWorldPoint(ShootingCursor.Instance.cursorPosition)-(Vector2)player.transform.position;
  gale.GetComponent<MoveComponent2D>().vector=3*aim.Rotate(angle).normalized;
  var actor=gale.GetComponent<AndroidWindActor>();actor.Owner=this;actor.Damage=Damage;actor.AreaEnabled=Eye;
  gale.transform.position=player.transform.position;gale.SetActive(true);
 }
 void OnDestroy(){if(prototype!=null)Object.Destroy(prototype);}
}

public sealed class AndroidWindActor:MonoBehaviour
{
 internal AndroidWind Owner;
 internal int Damage;
 internal bool AreaEnabled;
 internal float ContactTimer,AreaTimer;
 readonly List<int> contacts=new List<int>();
 readonly List<int> areaTargets=new List<int>();
 void OnCollisionEnter2D(Collision2D collision)
 {
  var target=collision.gameObject;if(AndroidPersistentTargets.Eligible(target))contacts.Add(target.GetInstanceID());
 }
 void OnCollisionExit2D(Collision2D collision)
 {var target=collision.gameObject;if(AndroidPersistentTargets.Eligible(target))contacts.Remove(target.GetInstanceID());}
 void Update()
 {
  ContactTimer+=Time.deltaTime;
  if(ContactTimer>=.5f)
  {
   ContactTimer-=.5f;
   if(contacts.Count>0)
   {
    // Source resolves retained IDs at each tick; it does not require current
    // ActiveList membership or clear duplicate contacts on pooled reuse.
    for(int i=contacts.Count-1;i>=0;i--)
    {var target=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetFromID(contacts[i]);if(target==null)continue;var health=target.GetComponent<Health>();if(health!=null)AndroidDamageRounding.ChangeHealth(health,-Damage,DamageType.Gale);}
   }
  }
  if(!AreaEnabled)return;
  AreaTimer+=Time.deltaTime;if(AreaTimer<2)return;AreaTimer-=2;
  areaTargets.Clear();ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetMonsterInRange(areaTargets,3,transform.position);
  for(int i=areaTargets.Count-1;i>=0;i--)
  {var target=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetFromID(areaTargets[i]);if(target==null)continue;var health=target.GetComponent<Health>();if(health!=null)AndroidDamageRounding.ChangeHealth(health,-Owner.Damage*2,DamageType.Gale);}
 }
 // Source has no enable/disable timer or contact-list reset. Its pooled state
 // deliberately survives until collision exit, later ticks or scene teardown.
}

[Serializable]
public sealed class AndroidGaleCast:flanne.PerkSystem.Action
{
 public GameObject Prefab;
 public SoundEffectSO Sound;
 public override void Init()=>AndroidWind.Current.Prepare(Prefab);
 public override void Activate(GameObject target){AndroidWind.Current.Cast();if(Sound!=null)Sound.Play();}
}
[Serializable]
public sealed class AndroidWindUpgrade:flanne.PerkSystem.Action
{
 public int FlatDamage;
 public bool Movement,Eye;
 public override void Activate(GameObject target)
 {var wind=AndroidWind.Current;wind.BonusDamage+=FlatDamage;wind.ScaleMovement|=Movement;wind.Eye|=Eye;}
}
[HarmonyPatch]
class AndroidWindRecipes
{
 static IEnumerable<MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(Powerup),nameof(Powerup.Apply)),AccessTools.Method(typeof(Powerup),nameof(Powerup.ApplyStack))};
 static void Prefix(Powerup __instance)
 {
  Trigger trigger=new InstantTrigger();flanne.PerkSystem.Action action;
  switch(RunResume.Key(__instance))
  {
   case "aero_magic_name":
    var native=(PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(__instance);
     if(native.Any(effect=>AccessTools.Field(typeof(PerkEffect),"action").GetValue(effect) is AndroidGaleAcquire))return;
    var actions=native.Select(effect=>(flanne.PerkSystem.Action)AccessTools.Field(typeof(PerkEffect),"action").GetValue(effect)).ToArray();
    var cast=actions.OfType<ThrowProjectileTowardsCursorAction>().Single();var sound=actions.OfType<PlaySoundAction>().Single();
     action=new AndroidGaleAcquire{Cast=new AndroidGaleCast{Prefab=(GameObject)AccessTools.Field(typeof(ThrowProjectileTowardsCursorAction),"projectilePrefab").GetValue(cast),Sound=(SoundEffectSO)AccessTools.Field(typeof(PlaySoundAction),"soundFX").GetValue(sound)}};break;
   case "windborne_name":action=new AndroidWindUpgrade{Movement=true};break;
   case "aero_mastery_name":action=new AndroidWindUpgrade{FlatDamage=15};break;
   case "eye_of_the_storm_name":action=new AndroidWindUpgrade{FlatDamage=5,Eye=true};break;
   default:return;
  }
  var replacement=new PerkEffect();Plugin.SetField(replacement,"trigger",trigger);Plugin.SetField(replacement,"action",action);
  Plugin.SetField(__instance,"effects",new[]{replacement});Plugin.SetField(__instance,"stackedEffects",new PerkEffect[0]);
 }
}
