using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using flanne;
using flanne.PerkSystem.Actions;
namespace TwentyMinutes.Modernization;

// Android owns contact handling on PlayerCollider and signed HP changes on
// Health. Windows puts both on PlayerHealth; keep that native owner and events.
public sealed class AndroidPlayerCollision:MonoBehaviour
{
 internal readonly BoolToggle EnableCollision=new BoolToggle(true);
 internal readonly HashSet<int> InvincibleLayers=new HashSet<int>();
 readonly ContactPoint2D[] contacts=new ContactPoint2D[1];
 internal bool OnHitInvicibility,Fragile;
 PlayerController player;PlayerHealth health;FlashSprite flasher;ScreenFlash screenFlash;
 bool deathPlayed;
 internal static readonly int ExplosionLayer=LayerMask.NameToLayer("HarmEverythingExplosion");
 internal static readonly int BoundaryMask=LayerMask.GetMask("EnemyProjectile","Wall");
 internal static AndroidPlayerCollision Get(PlayerHealth health)=>health.GetComponent<AndroidPlayerCollision>();
 internal static bool IsBoundary(Collider2D collider)
 {
  // Windows has neither the electron nor Wall tag. Original inventories map
  // every source Wall-layer object (all tagged Wall) to the native Wall layer.
  return collider.GetComponent<AndroidElectroEdge>()!=null||collider.gameObject.layer==LayerMask.NameToLayer("Wall");
 }
 internal void Bind(PlayerController owner)
 {
  player=owner;health=owner.playerHealth;health.onDeath.AddListener(OnDeath);
  // Original source level-up pulse is radius2; the native scene uses radius3.
  player.transform.Find("LevelupFX/Knockback").GetComponent<CircleCollider2D>().radius=2f;
  // The original evolution animator keeps its radius3 collider/body but has
  // no Knockback script. Follow the serialized animator, not a guessed path
  // through the different source/native player hierarchy.
  var evolution=(Animator)AccessTools.Field(typeof(Gun),"gunEvoAnimator").GetValue(player.gun);
  Object.Destroy(evolution.transform.Find("Knockback").GetComponent<Knockback>());
  var body=GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.useFullKinematicContacts=true;
  GetComponent<BoxCollider2D>().enabled=false;
  var circle=gameObject.AddComponent<CircleCollider2D>();circle.radius=.35f;circle.isTrigger=false;
  var invun=GetComponentInChildren<PlayerFlasher>(true);
  var template=(FlashSprite)AccessTools.Field(typeof(PlayerFlasher),"flasher").GetValue(invun);
  for(int i=0;i<health.onHurt.GetPersistentEventCount();i++)
  {
   var target=health.onHurt.GetPersistentTarget(i);
   if(target is FlashSprite flash)flasher=flash;
   // The original source onHurt has Hurt_SFX only. Windows' minipause,
   // radial knockback, particles and repeated red flash are not source hits.
   if(!(target is flanne.Audio.PlaySoundEffect))health.onHurt.SetPersistentListenerState(i,UnityEventCallState.Off);
  }
  Plugin.SetField(flasher,"duration",.2f);Plugin.SetField(flasher,"numRepeats",1);
  Plugin.SetField(flasher,"flashMaterial",AccessTools.Field(typeof(FlashSprite),"flashMaterial").GetValue(template));
  // Android has no persistent hit-stop callback on player death/dodge. Hurt
  // already keeps only its original sound callback above. Retain the native
  // death/game-state listeners, but do not keep these Windows timing calls.
  foreach(var callbacks in new[]{health.onDeath,health.onDodged})
   for(int i=0;i<callbacks.GetPersistentEventCount();i++)
    if(callbacks.GetPersistentTarget(i) is flanne.Core.Minipause)callbacks.SetPersistentListenerState(i,UnityEventCallState.Off);
  screenFlash=Resources.FindObjectsOfTypeAll<ScreenFlash>().Single(s=>s.name=="DamageScreenFlash"&&s.gameObject.scene==gameObject.scene);
  // The source DespawnRange object is inactive. Its callback body otherwise
  // matches Windows; do not keep native range-based enemy deactivation alive.
  player.GetComponentInChildren<flanne.UI.DespawnRange>(true).gameObject.SetActive(false);
 }
 void OnDeath(){deathPlayed=true;}
 void OnDestroy(){health.onDeath.RemoveListener(OnDeath);}
 internal void Enter(Collision2D collision)
 {
  var other=collision.gameObject;int layer=other.layer;
  if(!EnableCollision.value||deathPlayed||InvincibleLayers.Contains(layer))return;
  bool projectile=layer==(int)TagLayerUtil.EnemyProjectile;
  bool electron=other.GetComponent<AndroidElectroEdge>()!=null;
  if(health.isInvincible.value&&other.tag.Contains("HarmfulToPlayer"))
  {collision.GetContacts(contacts);player.transform.position+=(Vector3)(contacts[0].normal*.4f);}
  if(layer!=(int)TagLayerUtil.Enemy&&layer!=ExplosionLayer&&!projectile)return;
  // Source consumes this roll before invoking Health, including invulnerable
  // contacts. It displays the popup and returns without the native dodge FX.
  float roll=Random.Range(0f,1f),chance=player.stats[StatType.Dodge].Modify(1)-1;
  float cap=player.stats[StatType.DodgeCapMod].Modify(60)/100;
  if(roll<Mathf.Clamp(chance,0,cap)){AndroidDamagePopup.Pop(LocalizationSystem.GetLocalizedValue("dodge_label"),transform);return;}
  var ai=other.GetComponent<AIComponent>();Damage(ai!=null?ai.damageToPlayer:1);
  if(health.isInvincible.value)
  {
   if(!projectile)return;
   if(electron){collision.GetContacts(contacts);StartCoroutine(Knockback(contacts[0].normal,.2f,false));}
  }
  else if(health.hp>0)
  {
   collision.GetContacts(contacts);StartCoroutine(Knockback(contacts[0].normal,.2f,!electron));
   StartCoroutine(Invincibility(1));FlashScreen();
  }
  if(!projectile||electron)return;
  if(other.name.Contains("PF_Laser"))
  {var collider=other.GetComponent<Collider2D>();if(collider!=null)collider.enabled=false;}
  else AndroidPools.Return(collision.transform.gameObject);
 }
 void OnCollisionStay2D(Collision2D collision)
 {
  if(!EnableCollision.value||deathPlayed||collision.gameObject.layer!=(int)TagLayerUtil.EnemyProjectile||collision.gameObject.GetComponent<AndroidElectroEdge>()==null)return;
  if(health.isInvincible.value||health.hp<1)return;
  collision.GetContacts(contacts);StartCoroutine(Knockback(contacts[0].normal,.2f,false));
  StartCoroutine(Invincibility(1));FlashScreen();
  // Source Stay has no Hurt/HPChange call. Damage belongs to Enter.
 }
 internal void Damage(int amount)
 {
  for(int i=0;i<amount;i++)Change(-1,AndroidProjectile.MonsterDamage);
 }
 internal void Change(int change,DamageType kind)
 {
  if(change<0&&health.hp<1)return;
  if(change<0&&health.isInvincible.value){health.onDamagePrevented.Invoke();return;}
  if(change<0&&Fragile)
  {
   // Source player Death sets zero before shield absorption or callbacks.
   // Native death listeners supply Windows' existing game-state transition.
   RunRecorder.Instance.UnlockHitless=false;
   Plugin.SetField(health,"_hp",0);health.onDeath.Invoke();return;
  }
  int previous=health.hp;
  if(change<0&&health.shp>0)
  {
   int next=health.shp+change;
   // M10 counts soul-heart damage before its callbacks; source onHurt is
   // still reserved for a residual negative red-health change.
   if(Mathf.Max(next,0)<=health.maxSHP)RunRecorder.Instance.UnlockHitless=false;
   health.shp=Mathf.Max(next,0);change=Mathf.Min(next,0);
  }
  if(change<0)RunRecorder.Instance.UnlockHitless=false;
  health.hp+=change;
  if(change<0)health.onHurt.Invoke();else if(change>0)health.onHealed.Invoke();
  if(previous==0)return;
  if(health.hp>0)health.PostNotification(AndroidSurvivingHurt.Event,kind);
  else if(health.hp==0)health.onDeath.Invoke();
 }
 IEnumerator Knockback(Vector3 direction,float duration,bool checkTrigger)
 {
  player.disableMove.Flip();
  for(float timer=0;timer<duration;timer+=Time.deltaTime)
  {
   Vector3 delta=direction*7*Time.deltaTime;
   if(checkTrigger)delta=ElectronDirection(delta);
   player.transform.position+=delta;yield return null;
  }
  player.disableMove.UnFlip();
 }
 Vector3 ElectronDirection(Vector2 delta)
 {
  // The source tests the current hitbox center, not a predicted destination,
  // and considers only the single OverlapCircle result.
  var overlap=Physics2D.OverlapCircle(transform.position,.35f,BoundaryMask);
  return overlap!=null&&IsBoundary(overlap)?Vector3.zero:(Vector3)delta;
 }
 IEnumerator Invincibility(float duration)
 {
  OnHitInvicibility=true;health.isInvincible.Flip();
  for(float timer=0;timer<duration;timer+=.4f){flasher.Flash();yield return new WaitForSeconds(.4f);}
  OnHitInvicibility=false;health.isInvincible.UnFlip();
 }
 void FlashScreen()
 {
  if(screenFlash==null)return;
  if(AccessTools.Field(typeof(ScreenFlash),"flashhCoroutine").GetValue(screenFlash)==null)screenFlash.gameObject.SetActive(true);
  screenFlash.Flash(1);
 }
}
[HarmonyPatch(typeof(PlayerHealth),"Start")]
class AndroidPlayerContactInitialization
{static void Postfix(PlayerHealth __instance,PlayerController ___player)=>__instance.gameObject.AddComponent<AndroidPlayerCollision>().Bind(___player);}
[HarmonyPatch(typeof(PlayerHealth),"OnCollisionEnter2D")]
class AndroidPlayerContactEnter
{static bool Prefix(PlayerHealth __instance,Collision2D other){AndroidPlayerCollision.Get(__instance).Enter(other);return false;}}
[HarmonyPatch(typeof(PlayerHealth),"OnTriggerEnter2D")]
class AndroidPlayerContactTrigger{static bool Prefix()=>false;}
[HarmonyPatch(typeof(PlayerHealth),"TakeDamage")]
class AndroidPlayerDamage
{static bool Prefix(PlayerHealth __instance,int damage){AndroidPlayerCollision.Get(__instance).Damage(damage);return false;}}
[HarmonyPatch(typeof(PlayerHealth),nameof(PlayerHealth.Heal))]
class AndroidPlayerHealing
{
 // The reached source pickup/perk producers call HPChange(value,Cure), not
 // Android's empty Health.Heal method. Native producers use this Heal API.
 static bool Prefix(PlayerHealth __instance,int amount)
 {AndroidPlayerCollision.Get(__instance).Change(amount,DamageType.None);return false;}
}
[HarmonyPatch(typeof(ExplosionImmunityAction),nameof(ExplosionImmunityAction.Activate))]
class AndroidPlayerExplosionImmunity
{static bool Prefix(){AndroidPlayerCollision.Get(PlayerController.Instance.playerHealth).InvincibleLayers.Add(AndroidPlayerCollision.ExplosionLayer);return false;}}
[HarmonyPatch(typeof(PlayerController),"MovePlayer")]
class AndroidPlayerMovement
{
 static bool Prefix(PlayerController __instance)
 {
  Move(__instance,Time.deltaTime,(Vector2)__instance.moveVec*__instance.moveSpeedMultiplier*__instance.finalMoveSpeed,false);return false;
 }
 // Source MovePlayer(dt, velocity, forced) is also called by Dasher. Both
 // paths use the same source Rectify/native boundary-layer mapping.
 internal static void Move(PlayerController player,float dt,Vector2 realMove,bool forced)
 {
  var move=player.moveVec;if(move==Vector3.zero||(!forced&&player.disableMove.value))return;
  float distance=(realMove*dt).magnitude;
  var hit=Physics2D.Raycast(player.transform.position,move,distance+.35f,AndroidPlayerCollision.BoundaryMask);
  Vector2 delta=hit.collider!=null&&AndroidPlayerCollision.IsBoundary(hit.collider)?
   (Vector2)move.normalized*(Vector3.Distance(player.transform.position,hit.point)-.35f):realMove*dt;
  player.transform.Translate(delta);
 }
}
