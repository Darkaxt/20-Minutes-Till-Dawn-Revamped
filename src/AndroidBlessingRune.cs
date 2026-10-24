using System.Collections;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.RuneSystem;
namespace TwentyMinutes.Modernization;

// Keep the original Init/Attach and Continue bootstrap observer. The whole
// source iterator reduces the shield through its deadline/reduction owner,
// rather than rewriting the native base cooldown field.
[HarmonyPatch(typeof(BlessingRune),"WaitToApplyCR")]
class AndroidBlessingRune
{
 static readonly AccessTools.FieldRef<BlessingRune,PowerupGenerator> Generator=AccessTools.FieldRefAccess<BlessingRune,PowerupGenerator>("powerupGenerator");
 static readonly AccessTools.FieldRef<Rune,PlayerController> Player=AccessTools.FieldRefAccess<Rune,PlayerController>("player");
 static readonly AccessTools.FieldRef<Rune,int> Level=AccessTools.FieldRefAccess<Rune,int>("level");
 static bool Prefix(BlessingRune __instance,Powerup ___holyShieldPowerup,float ___cdrPerLevel,ref IEnumerator __result)
 {__result=Run(__instance,___holyShieldPowerup,___cdrPerLevel);return false;}
 static IEnumerator Run(BlessingRune rune,Powerup shield,float reduction)
 {
  yield return new WaitForSeconds(.1f);
  PlayerController.Instance.playerPerks.Equip(shield);
  Generator(rune).RemoveFromPool(shield);
  AndroidShieldCooldown.Reduce(Player(rune).GetComponentInChildren<PreventDamage>(),reduction*Level(rune));
 }
}
