using HarmonyLib;
using flanne;
using flanne.TitleScreen;
using flanne.CharacterPassives;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
namespace TwentyMinutes.Modernization;

static class AndroidBaseValues
{
 internal static void Configure(TitleScreenController title)
 {
  foreach(var item in Characters.Items(title.characterMenu))if(item.data.name=="Abby")item.data.startHP=3;
  foreach(var item in NativeSelection.GunItems(title.gunMenu))
  {
   if(item.data.name=="GrenadeLauncherData")item.data.damage=45;
   foreach(var evolution in item.data.gunEvolutions)
    if(evolution.name=="FlashMag")
     foreach(var effect in (PerkEffect[])AccessTools.Field(typeof(GunEvolution),"effects").GetValue(evolution))
      if(AccessTools.Field(typeof(PerkEffect),"trigger").GetValue(effect) is OnShootTrigger trigger)Plugin.SetField(trigger,"shotsToTrigger",20);
  }
 }
}
[HarmonyPatch(typeof(LoadoutInitializer),"Start")]
class AndroidCharacterMovement
{
 static void Postfix()
 {
  var player=PlayerController.Instance;
  if(!Characters.TryContract(player.loadedCharacter,out _))player.movementSpeed=player.loadedCharacter.name=="Shana"?3.2f:3f;
 }
}
[HarmonyPatch(typeof(ShadowClonePassive),"Init")]
class AndroidHinaDash
{
 static void Prefix(ShadowClonePassive __instance,ref float ___dashSpeedMulti)
 {___dashSpeedMulti=6;Plugin.SetField(__instance,"cooldown",1f);}
}
