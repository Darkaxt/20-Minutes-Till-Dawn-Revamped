using HarmonyLib;
using flanne;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(ExtraDamageEveryNthThunder),"OnThunderHit")]
class AndroidChargedUp
{
 // Preserve the source ARM SDIV/MSUB remainder, including its integer edge
 // semantics, rather than importing the CLR remainder operator's exceptions.
 static int SourceRemainder(int value,int divisor)
 {
  int quotient=divisor==0?0:unchecked((int)((long)value/divisor));
  return unchecked(value-quotient*divisor);
 }
 static bool Prefix(int ___activationThreshold,float ___damageBonus,ThunderGenerator ___TG,Ammo ___ammo,ref int ____counter)
 {
  ____counter=unchecked(____counter+1);
  if(SourceRemainder(____counter,unchecked(___activationThreshold-1))==0)
   ___TG.damageMod.AddMultiplierBonus(___damageBonus);
  else if(SourceRemainder(____counter,___activationThreshold)==0)
  {
   ____counter=0;
   ___TG.damageMod.AddMultiplierBonus(-___damageBonus);
   ___ammo.GainAmmo(1);
  }
  return false;
 }
}
