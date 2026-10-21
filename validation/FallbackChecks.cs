using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.UI;
using flanne.Player;
using flanne.PerkSystem;
using flanne.Pickups;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
using TwentyMinutes.Modernization;
public partial class RuntimeChecks
{
 static readonly AccessTools.FieldRef<PowerupGenerator,List<PowerupPoolItem>> FallbackPool=AccessTools.FieldRefAccess<PowerupGenerator,List<PowerupPoolItem>>("powerupPool");
 static readonly AccessTools.FieldRef<StatMod,float> Bonus=AccessTools.FieldRefAccess<StatMod,float>("_multiplierBonus");
 static readonly AccessTools.FieldRef<StatMod,float> Reduction=AccessTools.FieldRefAccess<StatMod,float>("_multiplierReduction");
 static readonly AccessTools.FieldRef<Powerup,LocalizedString> PerkKey=AccessTools.FieldRefAccess<Powerup,LocalizedString>("nameStrID");
 Powerup lastFallbackChoice;
 IEnumerator ChooseFallback(GameController game,Func<Powerup[],Powerup> select)
 {
  SetPhase("native-fallback-levelup");var xp=game.playerXP;xp.GainXP((float)AccessTools.Property(typeof(PlayerXP),"xpToLevel").GetValue(xp)+1);
  while(!(game.CurrentState is PowerupMenuState)||AccessTools.Field(typeof(ToggleGroupMenu<Powerup>),"ConfirmEvent").GetValue(game.powerupMenu)==null)yield return null;
  var toggles=((Toggle[])AccessTools.Field(typeof(ToggleGroupMenu<Powerup>),"toggles").GetValue(game.powerupMenu)).Take(game.numPowerupChoices).ToArray();
  var offers=toggles.Select(t=>t.GetComponent<DataUIBinding<Powerup>>().data).ToArray();Log("native-fallback-offers",string.Join(",",offers.Select(p=>p.name)));
  lastFallbackChoice=select(offers);toggles.Single(t=>t.GetComponent<DataUIBinding<Powerup>>().data==lastFallbackChoice).isOn=true;
  Button confirm=null;while(confirm==null||!confirm.IsInteractable())
  {confirm=Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(b=>b.gameObject.activeInHierarchy&&Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentTarget(i)==game.powerupMenu&&b.onClick.GetPersistentMethodName(i)=="OnConfirmClicked"));yield return null;}
  yield return Capture("fallback-"+lastFallbackChoice.name+".png");Click(confirm);while(!(game.CurrentState is CombatState))yield return null;
 }
 IEnumerator CheckFallbackPowerups()
 {
  yield return AchievementRun("Shana","RevolverData","Endless_Forest");var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;var generator=game.powerupGenerator;
  var sourceProfile=(PowerupPoolProfile)AccessTools.Field(typeof(PowerupGenerator),"profile").GetValue(generator);
  File.WriteAllText(Path.Combine(ResultDir,"windows-default-profile.txt"),string.Join("\n",sourceProfile.powerupPool.Select(x=>x.name+"="+PerkKey(x).key)));
  Require(sourceProfile.powerupPool.Count==112&&sourceProfile.powerupPool.Count(x=>PerkKey(x).key=="frenzy_name")==1&&!sourceProfile.powerupPool.Any(x=>PerkKey(x).key=="hastur_perk_frenzy_name"),"real Windows default profile resolves ordinary Frenzy identity without its character exclusive");
  var pool=FallbackPool(generator);Require(pool.Count==118&&FallbackPowerups.Powers.Count==6,"all original default perks and exactly six new source fallback entries");
  var entries=pool.Where(x=>FallbackPowerups.IsFallback(x.powerup)).ToDictionary(x=>x.powerup.name.Substring("Revamped_".Length));
  Require(entries.Values.All(e=>e.numTimeRepeatable==3),"native Endless map supplies three finite picks per new source entry");
  for(int i=0;i<20;i++)Require(generator.GetRandom(5).All(x=>!FallbackPowerups.IsFallback(x)),"fallbacks excluded while five eligible ordinary choices remain");
  // Controlled remaining-pool fixtures represent late-run depletion; all
  // subsequent choices and applications use the real native XP/menu path.
  var regular=FallbackPowerups.Eligible(generator).Where(x=>!FallbackPowerups.IsFallback(x.powerup)).OrderBy(x=>x.powerup.name=="Haste"?0:1).Take(5).ToList();Require(regular.Count==5&&regular.Any(e=>e.powerup.name=="Haste"),"five actually eligible source ordinary candidates including Haste");
  foreach(var e in regular)e.numTimeRepeatable=1;
  FallbackPool(generator)=regular.Concat(entries.Where(e=>e.Key!="Adrenaline").Select(e=>e.Value)).ToList();
  yield return ChooseFallback(game,offers=>{Require(offers.All(x=>!FallbackPowerups.IsFallback(x)),"actual full native offer contains ordinary perks only");return offers.Single(x=>x.name=="Haste");});
  yield return ChooseFallback(game,offers=>{Require(offers.Any(FallbackPowerups.IsFallback),"real depleted native offer contains a source fallback");return offers.First(FallbackPowerups.IsFallback);});
  foreach(var c in FallbackPowerups.Contracts.Where(c=>c.key!="Adrenaline"))
  {
   var e=entries[c.key];FallbackPool(generator)=new List<PowerupPoolItem>{e};
   var before=c.stats.Select(s=>{var m=p.stats[(StatType)Enum.Parse(typeof(StatType),s.type)];return new{flat=m.FlatBonus,bonus=Bonus(m),reduction=Reduction(m)};}).ToArray();
   int nativeCount=game.numPowerupChoices;yield return ChooseFallback(game,offers=>{Require(offers.Length==1&&offers[0]==e.powerup,"remaining single native choice is shown without rejection-sampling loop");return offers[0];});
   Require(game.numPowerupChoices==nativeCount,"native choice count restored after depleted offer");
   for(int i=0;i<c.stats.Length;i++)
   {
    var s=c.stats[i];var m=p.stats[(StatType)Enum.Parse(typeof(StatType),s.type)];var b=before[i];
    Require(s.isFlatMod!=0?m.FlatBonus==b.flat+s.flatValue:s.value>0?Mathf.Abs(Bonus(m)-b.bonus-s.value)<.0001f:Mathf.Abs(Reduction(m)-b.reduction*(1+s.value))<.0001f,"source native stat application: "+c.key+" / "+s.type);
   }
  }
  var adrenaline=entries["Adrenaline"];FallbackPool(generator)=new List<PowerupPoolItem>{adrenaline};
  float damageBefore=Bonus(p.stats[StatType.BulletDamage]),rateBefore=Bonus(p.stats[StatType.FireRate]);
  yield return ChooseFallback(game,offers=>offers.Single());float first=Time.time;
  Require(Mathf.Abs(Bonus(p.stats[StatType.BulletDamage])-damageBefore-.5f)<.0001f&&Mathf.Abs(Bonus(p.stats[StatType.FireRate])-rateBefore-.5f)<.0001f,"native temporary buff adds source fifty-percent damage and fire rate");
  Time.timeScale=10;while(Time.time-first<10)yield return null;
  yield return ChooseFallback(game,offers=>offers.Single());float second=Time.time;Time.timeScale=10;
  Require(Mathf.Abs(Bonus(p.stats[StatType.BulletDamage])-damageBefore-1)<.0001f,"independent repeated Adrenaline effects overlap additively");
  while(Bonus(p.stats[StatType.BulletDamage])>damageBefore+.51f)yield return null;
  Require(Time.time-first>=59.9f&&Time.time-second<60&&Mathf.Abs(Bonus(p.stats[StatType.BulletDamage])-damageBefore-.5f)<.0001f,"first sixty-second native expiry leaves the later independent effect active");
  while(Bonus(p.stats[StatType.BulletDamage])>damageBefore+.001f)yield return null;Time.timeScale=1;
  Require(Time.time-second>=59.9f&&Mathf.Abs(Bonus(p.stats[StatType.FireRate])-rateBefore)<.0001f,"later native expiry restores original damage/fire-rate bonus exactly");
  yield return ChooseFallback(game,offers=>offers.Single());Require(adrenaline.numTimeRepeatable==0&&FallbackPool(generator).Count==0,"third native Endless choice consumes the finite source allowance");
  int level=game.playerXP.level;game.playerXP.GainXP((float)AccessTools.Property(typeof(PlayerXP),"xpToLevel").GetValue(game.playerXP)+1);yield return null;
  Require(game.playerXP.level==level+1&&game.CurrentState is CombatState&&!PauseController.isPaused,"fully exhausted native level-up continues combat without an impossible menu");
  AccessTools.Field(typeof(PowerupGenerator),"characterPool").SetValue(generator,new List<PowerupPoolItem>());this.PostNotification(ChestPickup.ChestPickupEvent);yield return null;
  Require(game.CurrentState is CombatState&&!PauseController.isPaused,"fully exhausted exclusive/fallback chest does not enter an empty native menu");
  yield return AchievementExit(game,false);
  yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  Require(Mathf.Abs(p.gun.damage-20)<.001f&&Mathf.Abs(1/p.gun.shotCooldown-4)<.001f,"scene exit during active native timed buff leaves the next original run unchanged");
  Require(FallbackPool(game.powerupGenerator).Where(e=>FallbackPowerups.IsFallback(e.powerup)).All(e=>e.numTimeRepeatable==1),"native standard map restores one source pick per fallback");
  yield return CheckRevolver(game,p,"fallback-original-revolver.png");yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete","Six source fallback entries, native normal/depleted offers, stats, finite repeats, independent sixty-second buffs, empty-pool continuation, native cleanup and original restart; remaining-pool and game-time acceleration explicitly controlled");Application.Quit(0);
 }
}
