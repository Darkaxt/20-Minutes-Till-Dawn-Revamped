using System.Collections;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.CharacterPassives;
namespace TwentyMinutes.Modernization;

// Reconstruct DasherSpecial's complete Android lifecycle beside the native
// serialized component. Its only native roles are config/assets and the public
// projectile field used by the actual Hail Bringer recipe.
public sealed class AndroidDasher:MonoBehaviour
{
 DasherSpecial config;PlayerFlasher flasher;SpriteTrail trail;ObjectPooler pool;
 HarmfulOnContact hitboxPrefab;GameObject deerPrefab,humanPrefab;
 Projectile projectilePrefab;Transform firepoint;
 int baseDamage,baseKnockback;float duration,warning,interval,moveMultiplier,shootCD,projectileMultiplier;
 SoundEffectSO deerSound,humanSound,warningSound;
 internal HarmfulOnContact Hitbox;internal Knockback Knockback;
 internal GameObject DeerAnimation,HumanAnimation;
 internal bool Transformed;internal float Countdown,ShotTimer;
 static PlayerController Player=>PlayerController.Instance;
 static T Field<T>(DasherSpecial owner,string field)=>(T)AccessTools.Field(typeof(DasherSpecial),field).GetValue(owner);
 internal int Damage=>Mathf.FloorToInt(Player.stats[StatType.SummonDamage].Modify(Player.stats[StatType.MoveSpeed].Modify(baseDamage)));
 internal int KnockbackForce=>Mathf.Max(baseKnockback,Mathf.FloorToInt(Player.stats[StatType.CharacterSize].Modify(baseKnockback)));
 internal void Initialize(DasherSpecial owner)
 {
  config=owner;hitboxPrefab=Field<HarmfulOnContact>(owner,"hitboxPrefab");deerPrefab=Field<GameObject>(owner,"transformToDeerAnimPrefab");humanPrefab=Field<GameObject>(owner,"transformToHumanAnimPrefab");
  baseDamage=Field<int>(owner,"baseDamage");baseKnockback=Field<int>(owner,"baseKnockback");duration=Field<float>(owner,"baseDuration");warning=Field<float>(owner,"warningDuration");
  // Native timeToActivate and original source deformInterval are both10.
  interval=Field<float>(owner,"timeToActivate");moveMultiplier=Field<float>(owner,"moveSpeedMultiplier");shootCD=Field<float>(owner,"shootCD");projectileMultiplier=Field<float>(owner,"projDamageMultiplier");
  projectilePrefab=Field<Projectile>(owner,"projectilePrefab");firepoint=Field<Transform>(owner,"firepoint");deerSound=Field<SoundEffectSO>(owner,"transformToDeerSFX");humanSound=Field<SoundEffectSO>(owner,"transformToHumanSFX");warningSound=Field<SoundEffectSO>(owner,"transformWarningSFX");
  flasher=Player.GetComponentInChildren<PlayerFlasher>();trail=Player.playerSprite.GetComponentInChildren<SpriteTrail>();this.AddObserver(OnDeath,Health.DeathEvent);
  Hitbox=Instantiate(hitboxPrefab);Parent(Hitbox.transform);Hitbox.gameObject.SetActive(false);AndroidContacts.ConfigurePrefab(Hitbox.gameObject);Knockback=Hitbox.GetComponent<Knockback>();
  DeerAnimation=Instantiate(deerPrefab);Parent(DeerAnimation.transform);DeerAnimation.SetActive(false);
  HumanAnimation=Instantiate(humanPrefab);Parent(HumanAnimation.transform);HumanAnimation.SetActive(false);
  pool=ObjectPooler.SharedInstance;pool.GetComponent<AndroidPools>().Register(projectilePrefab.name,projectilePrefab.gameObject);Countdown=interval;
 }
 static void Parent(Transform actor){actor.SetParent(Player.playerSprite.transform);actor.localPosition=Vector3.zero;actor.localScale=Vector3.one;}
 void OnDeath(object sender,object args){} // Original body is empty; killThreshold is unused.
 void OnDestroy(){this.RemoveObserver(OnDeath,Health.DeathEvent);}
 internal void Step()
 {
  if(Player.playerHealth.hp==0)return;
  if(Transformed)
  {
   AndroidPlayerMovement.Move(Player,Time.deltaTime,moveMultiplier*(Vector2)Player.moveVec*Player.finalMoveSpeed,true);
   ShotTimer+=Time.deltaTime;
   if(ShotTimer>=shootCD){ShotTimer-=shootCD;for(int i=0;i<config.projectiles;i++)Shoot();}
  }
  else
  {
   Countdown-=Time.deltaTime;
   if(Countdown<0){Countdown=interval;StartCoroutine(Transform());}
  }
 }
 static bool Sword(GunData gun)=>gun.name=="SwordData"||gun.name=="SwordOfDeathGun"||gun.name=="SwordOfLightGun"||gun.name=="SwordOfWindGun";
 internal IEnumerator Transform()
 {
  // The source ID211 test covers the Sword family only, not every reload.
  if(Sword(Player.gun.gunData)&&Player.CurrentState!=null&&Player.CurrentState.GetType().Name=="ReloadState")yield return new WaitForSeconds(1.5f);
  Transformed=true;Player.gun.gunObj.SetActive(false);Hitbox.gameObject.SetActive(true);Hitbox.damageAmount=Damage;Knockback.knockbackForce=KnockbackForce;DeerAnimation.SetActive(true);
  AndroidPlayerCollision.Get(Player.playerHealth).EnableCollision.Flip();AndroidGunRuntime.Get(Player.gun).ShootEnable.Flip();Player.disableAction.Flip();Player.disableMove.Flip();Player.disableAnimation.Flip();deerSound.Play();
  yield return new WaitForSeconds(.2f);
  Player.playerAnimator.ResetTrigger("Idle");Player.playerAnimator.ResetTrigger("Run");Player.playerAnimator.ResetTrigger("Walk");Player.playerAnimator.SetTrigger("Special");trail.SetEnabled(true);
  yield return new WaitForSeconds(duration-warning);
  flasher.Flash();warningSound.Play();yield return new WaitForSeconds(warning);
  flasher.StopFlash();Player.gun.gunObj.SetActive(true);Hitbox.gameObject.SetActive(false);HumanAnimation.SetActive(true);
  AndroidGunRuntime.Get(Player.gun).ShootEnable.UnFlip();Player.disableAction.UnFlip();Player.disableMove.UnFlip();Player.disableAnimation.UnFlip();Player.playerAnimator.ResetTrigger("Special");Player.playerAnimator.SetTrigger("Idle");trail.SetEnabled(false);humanSound.Play();Transformed=false;
  yield return new WaitForSeconds(.5f);AndroidPlayerCollision.Get(Player.playerHealth).EnableCollision.UnFlip();
 }
 internal void Shoot()
 {
  var actor=pool.GetPooledObject(projectilePrefab.name);actor.transform.position=firepoint.position;actor.SetActive(true);actor.GetComponent<Projectile>().damage=Damage*projectileMultiplier;
 }
}
[HarmonyPatch(typeof(DasherSpecial),"Start")]
class AndroidDasherStart
{static bool Prefix(DasherSpecial __instance){__instance.gameObject.AddComponent<AndroidDasher>().Initialize(__instance);return false;}}
[HarmonyPatch(typeof(DasherSpecial),"Update")]
class AndroidDasherUpdate
{static bool Prefix(DasherSpecial __instance){__instance.GetComponent<AndroidDasher>().Step();return false;}}
[HarmonyPatch(typeof(DasherSpecial),"TransformCR")]
class AndroidDasherTransform
{static bool Prefix(DasherSpecial __instance,ref IEnumerator __result){__result=__instance.GetComponent<AndroidDasher>().Transform();return false;}}
[HarmonyPatch(typeof(DasherSpecial),"Shoot")]
class AndroidDasherShoot
{static bool Prefix(DasherSpecial __instance){__instance.GetComponent<AndroidDasher>().Shoot();return false;}}
[HarmonyPatch(typeof(DasherSpecial),"get_finalDamage")]
class AndroidDasherDamage
{static bool Prefix(DasherSpecial __instance,ref int __result){__result=__instance.GetComponent<AndroidDasher>().Damage;return false;}}
[HarmonyPatch(typeof(DasherSpecial),"get_finalKnockback")]
class AndroidDasherKnockback
{static bool Prefix(DasherSpecial __instance,ref int __result){__result=__instance.GetComponent<AndroidDasher>().KnockbackForce;return false;}}
