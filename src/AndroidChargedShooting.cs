using System;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// The original Crossbow models keep their serialized ChargedShooter and its
// native Gun callbacks. Own its complete source charge/release contract here;
// Charge Cannon remains a separate shooter and never enters these methods.
[HarmonyPatch(typeof(ChargedShooter))]
class AndroidChargedShooting
{
 static readonly Action<Shooter,ProjectileRecipe,Vector2,int,float,float> BaseShoot=
  AccessTools.MethodDelegate<Action<Shooter,ProjectileRecipe,Vector2,int,float,float>>(
   AccessTools.Method(typeof(Shooter),nameof(Shooter.Shoot)),virtualCall:false);

 [HarmonyPrefix,HarmonyPatch(nameof(ChargedShooter.Shoot))]
 static bool Shoot(ChargedShooter __instance,ProjectileRecipe recipe,Vector2 pointDirection,int numProjectiles,float spread,float inaccuracy,
  ref int ____charge,int ___maxCharge,Transform ___chargeUpSprite,GameObject ___maxChargeAnimationObj,SoundEffectSO ___chargeUpSFX,SoundEffectSO ___maxChargeSFX)
 {
  if(____charge==___maxCharge)
  {
   __instance.PostNotification(Gun.ShootEvent,recipe);
   __instance.OnStopShoot(recipe,pointDirection,numProjectiles,spread,inaccuracy);
   return false;
  }
  ____charge++;
  ___chargeUpSprite.localScale=Vector3.one*((float)____charge/___maxCharge);
  if(____charge==0)___chargeUpSFX?.Play();
  if(____charge==___maxCharge){___maxChargeAnimationObj.SetActive(true);___maxChargeSFX?.Play();}
  return false;
 }

 [HarmonyPrefix,HarmonyPatch(nameof(ChargedShooter.OnStopShoot))]
 static bool Release(ChargedShooter __instance,ProjectileRecipe recipe,Vector2 pointDirection,int numProjectiles,float spread,float inaccuracy,
  ref int ____charge,int ___maxCharge,Transform ___chargeUpSprite,GameObject ___maxChargeAnimationObj)
 {
  // The source empty-ammo return deliberately leaves the charge/visual state.
  if(PlayerController.Instance.ammo.outOfAmmo)return false;
  int charge=Mathf.Max(0,____charge);
  recipe.damage*=1+charge*.5f;
  recipe.size*=1+charge*.25f;
  recipe.piercing+=charge==___maxCharge?999:charge;
  // Nonvirtual call to the matching base emission, preserving this instance's
  // recipe/spawn point/pool while avoiding another virtual charge dispatch.
  BaseShoot(__instance,recipe,pointDirection,numProjectiles,spread,inaccuracy);
  ____charge=-1;
  ___chargeUpSprite.localScale=Vector3.zero;
  ___maxChargeAnimationObj.SetActive(false);
  __instance.onShoot?.Invoke();
  return false;
 }
}
