using System.Collections;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.CharacterPassives;
namespace TwentyMinutes.Modernization;

// Keep native keyboard/controller binding and the existing root-owned caches.
// Reconstruct the whole source spray and shot dispatch, including its final
// shot after releasing the action/animation locks.
internal static class AndroidAbby
{
 static readonly AccessTools.FieldRef<DumpAmmoPassive,bool> Spraying=AccessTools.FieldRefAccess<DumpAmmoPassive,bool>("_isSpraying");
 internal static IEnumerator Spray(DumpAmmoPassive owner,PlayerController player,Ammo ammo,Gun gun,float multiplier,Shooter shooter)
 {
  Spraying(owner)=true;player.disableAction.Flip();gun.SetVisible(false);player.disableAnimation.Flip();
  player.playerAnimator.ResetTrigger("Idle");player.playerAnimator.ResetTrigger("Run");player.playerAnimator.ResetTrigger("Walk");player.playerAnimator.SetTrigger("Special");
  while(ammo.amount>1)
  {
   Shoot(gun,shooter);
   yield return new WaitForSeconds(gun.shotCooldown*multiplier);
  }
  player.disableAnimation.UnFlip();player.playerAnimator.ResetTrigger("Special");player.playerAnimator.SetTrigger("Idle");
  gun.SetVisible(true);Spraying(owner)=false;player.disableAction.UnFlip();
  Shoot(gun,shooter);
 }
 internal static void Shoot(Gun gun,Shooter shooter)
 {
  Vector2 direction=Vector2.zero;
  while(direction==Vector2.zero)direction=new Vector2(Random.Range(-1f,1f),Random.Range(-1f,1f));
  int count=gun.shooters.Count;
  for(int i=0;i<count;i++)
   shooter.Shoot(gun.GetProjectileRecipe(),direction.Rotate(i*10),gun.numOfProjectiles,gun.spread,0);
  gun.gunData.gunshotSFX?.Play();gun.OnShoot.Invoke();
 }
}
[HarmonyPatch(typeof(DumpAmmoPassive),"PerformSkill")]
class AndroidAbbySkill
{
 static bool Prefix(DumpAmmoPassive __instance,PlayerController ___player,Ammo ___ammo,Gun ___myGun,bool ____isSpraying,Shooter ___shooter)
 {
  if(!___ammo.outOfAmmo&&!____isSpraying&&___player.playerHealth.hp>0)
   __instance.StartCoroutine(AndroidAbby.Spray(__instance,___player,___ammo,___myGun,__instance.shotCDMultiplier,___shooter));
  // Source SetCd updates the mobile SkillBtn, not SkillPassive._timer. Native
  // input uses the same source keyboard gate; do not add a second cooldown.
  return false;
 }
}
[HarmonyPatch(typeof(DumpAmmoPassive),"SprayCR")]
class AndroidAbbySpray
{
 static bool Prefix(DumpAmmoPassive __instance,PlayerController ___player,Ammo ___ammo,Gun ___myGun,Shooter ___shooter,ref IEnumerator __result)
 {__result=AndroidAbby.Spray(__instance,___player,___ammo,___myGun,__instance.shotCDMultiplier,___shooter);return false;}
}
[HarmonyPatch(typeof(DumpAmmoPassive),"ShootRandom")]
class AndroidAbbyShot
{
 static bool Prefix(Gun ___myGun,Shooter ___shooter){AndroidAbby.Shoot(___myGun,___shooter);return false;}
}
