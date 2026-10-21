using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Json;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.PerkSystem;
using flanne.PerkSystem.Actions;
using flanne.PerkSystem.Triggers;
using flanne.Player;
using flanne.Player.Buffs;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

public static class FallbackPowerups
{
 public class Manifest{public Contract[] powers;}
 public class Contract{public string key,nameKey,name,description,icon;public float duration,ppu;public Change[] stats;}
 public class Change{public string type;public int flatValue,isFlatMod;public float value;}
 public static readonly Dictionary<string,Powerup> Powers=new Dictionary<string,Powerup>();
 public static Contract[] Contracts;
 static readonly AccessTools.FieldRef<Powerup,LocalizedString> Name=AccessTools.FieldRefAccess<Powerup,LocalizedString>("nameStrID");
 internal static readonly AccessTools.FieldRef<PowerupGenerator,List<PowerupPoolItem>> Pool=AccessTools.FieldRefAccess<PowerupGenerator,List<PowerupPoolItem>>("powerupPool");
 internal static readonly AccessTools.FieldRef<PowerupGenerator,List<PowerupPoolItem>> CharacterPool=AccessTools.FieldRefAccess<PowerupGenerator,List<PowerupPoolItem>>("characterPool");
 static readonly System.Reflection.MethodInfo Prerequisite=AccessTools.Method(typeof(PowerupGenerator),"PrereqsMet");
 public static bool IsFallback(Powerup power)=>Powers.Values.Any(p=>Name(p).key==Name(power).key);
 public static List<PowerupPoolItem> Eligible(PowerupGenerator generator)=>Pool(generator).Where(p=>(bool)Prerequisite.Invoke(generator,new object[]{p.powerup})).ToList();
 public static void Prepare()
 {
  if(Contracts!=null)return;
  string root=Path.Combine(Plugin.AssetRoot,"powerups");
  using(var input=File.OpenRead(Path.Combine(root,"manifest.json")))Contracts=((Manifest)new DataContractJsonSerializer(typeof(Manifest)).ReadObject(input)).powers;
  foreach(var c in Contracts)
  {
   var p=ScriptableObject.CreateInstance<Powerup>();p.name="Revamped_"+c.key;p.prereqs=new List<Powerup>();
   var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(Path.Combine(root,c.icon)));texture.filterMode=FilterMode.Point;
   p.icon=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),c.ppu);
   Plugin.Text[c.nameKey]=c.name;string desc=c.nameKey+"_description";Plugin.Text[desc]=c.description;
   Plugin.SetField(p,"nameStrID",new LocalizedString(c.nameKey));Plugin.SetField(p,"desStrID",new LocalizedString(desc));
   var stats=c.stats.Select(s=>new StatChange{type=(StatType)Enum.Parse(typeof(StatType),s.type),flatValue=s.flatValue,isFlatMod=s.isFlatMod!=0,value=s.value}).ToArray();
   Plugin.SetField(p,"statChanges",c.duration>0?new StatChange[0]:stats);
   var effects=new PerkEffect[0];
   if(c.duration>0)
   {
    var buff=new TemporaryStatBuff();Plugin.SetField(buff,"statChanges",stats);Plugin.SetField(buff,"duration",c.duration);
    var action=new ApplyPlayerBuffAction();Plugin.SetField(action,"buff",buff);
    var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",action);effects=new[]{effect};
   }
   Plugin.SetField(p,"effects",effects);Plugin.SetField(p,"stackedEffects",new PerkEffect[0]);
   Object.DontDestroyOnLoad(p);Powers.Add(c.key,p);
  }
 }
}
[HarmonyPatch(typeof(PowerupGenerator),nameof(PowerupGenerator.InitPowerupPool))]
class AddFallbackPowerups
{
 static void Postfix(PowerupGenerator __instance,int numTimesRepeatable)
 {
  FallbackPowerups.Prepare();
  foreach(var p in FallbackPowerups.Powers.Values)FallbackPowerups.Pool(__instance).Add(new PowerupPoolItem{powerup=p,numTimeRepeatable=numTimesRepeatable});
 }
}
[HarmonyPatch(typeof(PowerupGenerator),nameof(PowerupGenerator.GetRandom),typeof(int),typeof(List<PowerupPoolItem>))]
class SelectFallbackPowerups
{
 static void Prefix(PowerupGenerator __instance,int num,ref List<PowerupPoolItem> pool)
 {
  if(!ReferenceEquals(pool,FallbackPowerups.Pool(__instance)))return;
  var eligible=FallbackPowerups.Eligible(__instance);var ordinary=eligible.Where(p=>!FallbackPowerups.IsFallback(p.powerup)).ToList();
  // Preserve the original random-without-duplicates selection implementation;
  // supplying eligible candidates also makes its completion condition explicit.
  pool=ordinary.Count>=num?ordinary:eligible;
 }
}
[HarmonyPatch(typeof(PowerupMenuState),nameof(PowerupMenuState.Enter))]
class RemainingPowerupChoices
{
 class ChoiceCount{public int Value;}
 static readonly ConditionalWeakTable<PowerupMenuState,ChoiceCount> Counts=new ConditionalWeakTable<PowerupMenuState,ChoiceCount>();
 static readonly AccessTools.FieldRef<GameState,GameController> Owner=AccessTools.FieldRefAccess<GameState,GameController>("owner");
 static void Prefix(PowerupMenuState __instance)
 {
  var game=Owner(__instance);int remaining=FallbackPowerups.Eligible(game.powerupGenerator).Count;
  if(remaining>=game.numPowerupChoices)return;
  Counts.Add(__instance,new ChoiceCount{Value=game.numPowerupChoices});game.numPowerupChoices=remaining;
 }
 internal static void Restore(PowerupMenuState __instance)
 {if(Counts.TryGetValue(__instance,out var count)){Owner(__instance).numPowerupChoices=count.Value;Counts.Remove(__instance);}}
}
[HarmonyPatch(typeof(PowerupMenuState),nameof(PowerupMenuState.Exit))]
class RestorePowerupChoices{static void Postfix(PowerupMenuState __instance)=>RemainingPowerupChoices.Restore(__instance);}
[HarmonyPatch(typeof(CombatState),"OnLevelUP")]
class ExhaustedLevelup
{
 static readonly AccessTools.FieldRef<GameState,GameController> Owner=AccessTools.FieldRefAccess<GameState,GameController>("owner");
 static bool Prefix(CombatState __instance,int level)=>level==20||FallbackPowerups.Eligible(Owner(__instance).powerupGenerator).Count>0;
}
[HarmonyPatch(typeof(CombatState),"OnChestPickup")]
class ExhaustedFallbackChest
{
 static readonly AccessTools.FieldRef<GameState,GameController> Owner=AccessTools.FieldRefAccess<GameState,GameController>("owner");
 static bool Prefix(CombatState __instance)
 {
  var generator=Owner(__instance).powerupGenerator;
  return FallbackPowerups.CharacterPool(generator)?.Count>0||FallbackPowerups.Eligible(generator).Count>0;
 }
}
