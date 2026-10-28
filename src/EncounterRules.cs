using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

public static partial class Progression
{
 public enum EncounterKind{Miniboss,Boss,SecretBoss,Clear}
 public enum RunAttribute{None,MaxRedHearts,SoulHearts,TotalHearts,BulletDamage,FireRate,MoveSpeed}
 public class Encounter
 {
  public EncounterKind Kind;public string Map,Character,Weapon;public int Darkness,Ordinal,MaxHP,SoulHP,TotalHP;
  public float Damage,FireRate,MoveSpeed;public bool Hitless;public HashSet<string> Perks;
 }
 public class Attempt{public string id,text;}
 public class Notice{public string id,goal;}
 internal const string Forest="20M_Forest",Temple="20M_Temple",Pumpkin="20M_PumpkinPatch";
 static readonly Dictionary<string,string> contentNames=new Dictionary<string,string>();
 static Gate RuleFor(string name)
 {
  switch(name)
  {
   case "Shana":case "RevolverData":case Forest:return null;
   case "Diamond":return new Gate{Attribute=RunAttribute.MaxRedHearts,Threshold=7};
   case "Scarlett":return new Gate{Perks=new[]{"pyro_mage_name","fire_starter_name","intense_burn_name","soothing_warmth_name"}};
   case "Hina":return new Gate{Attribute=RunAttribute.MoveSpeed,Threshold=4};
   case "Spark":return new Gate{Perks=new[]{"electro_mage_name","energized_name"}};
   case "Yuki":return new Gate{MinimumDarkness=1,Perks=new[]{"frost_mage_name","frostbite_name","ice_shards_name","shatter_name"}};
   case "Abby":return new Gate{MinimumDarkness=1,RequiredWeapon="ShotgunData"};
   case "Lilith":return new Gate{Attribute=RunAttribute.SoulHearts,Threshold=2};
   case "Luna":return new Gate{Map=Temple,MinimumDarkness=1,RequiredWeapon="MagicBowData"};
   case "Dasher":return new Gate{MinimumDarkness=2,Attribute=RunAttribute.MoveSpeed,Threshold=5};
   case "Katana":return new Gate{Map=Temple,MinimumDarkness=2,Attribute=RunAttribute.BulletDamage,Threshold=60};
   case "Hastur":return new Gate{Map=Temple,MinimumDarkness=1,Attribute=RunAttribute.SoulHearts,Threshold=3};
   case "Raven":return new Gate{Map=Pumpkin,MinimumDarkness=2,Perks=new[]{"dark_arts_name","wither_name"}};
   case "Revamped_Haruka":return new Gate{MinimumDarkness=2,RequiredCharacter="Hina",Attribute=RunAttribute.MoveSpeed,Threshold=4.5f};
   case "Revamped_Dimond_Portrait":return new Gate{Map=Temple,MinimumDarkness=2,RequiredCharacter="Diamond",Attribute=RunAttribute.TotalHearts,Threshold=10};
   case "ShotgunData":return new Gate{Kind=EncounterKind.Boss,Ordinal=0};
   case "CrossbowData":return new Gate{Kind=EncounterKind.Boss,Ordinal=0,RequireHitless=true};
   case "FlameCannon":return new Gate{Kind=EncounterKind.Boss,Perks=new[]{"pyro_mage_name","intense_burn_name"}};
   case "DualSMGsData":return new Gate{Kind=EncounterKind.Boss,Ordinal=0,Attribute=RunAttribute.FireRate,Threshold=6f};
   case "MagicBowData":return new Gate{Kind=EncounterKind.Boss,Ordinal=0,MinimumDarkness=1,RequiredWeapon="CrossbowData"};
   case "GrenadeLauncherData":return new Gate{Kind=EncounterKind.Boss,Ordinal=0,Attribute=RunAttribute.BulletDamage,Threshold=35};
   case "BatGunData":return new Gate{Kind=EncounterKind.Boss,MinimumDarkness=1,Perks=new[]{"ghost_friend_name","dragon_egg_name"}};
   case "SalvoKnivesData":return new Gate{Kind=EncounterKind.Boss,Ordinal=0,MinimumDarkness=1,RequiredCharacter="Hina"};
   case "SwordData":return new Gate{Kind=EncounterKind.Boss,Ordinal=1,Map=Temple,MinimumDarkness=3,RequiredCharacter="Katana"};
   case "SporeGunData":return new Gate{Kind=EncounterKind.SecretBoss};
   case "ModernizationChargeCannon_None":return new Gate{Kind=EncounterKind.Boss,Ordinal=1,MinimumDarkness=2};
   case "10M_Forest":return new Gate{Kind=EncounterKind.Boss,Ordinal=0};
   case Temple:return new Gate{Kind=EncounterKind.Clear,MinimumDarkness=1};
   case "Challenge_Forest":return new Gate{Kind=EncounterKind.Clear,MinimumDarkness=1};
   case Pumpkin:return new Gate{Kind=EncounterKind.Clear,Map=Temple,MinimumDarkness=2};
   case "Endless_Forest":return new Gate{Kind=EncounterKind.Clear,Map=null,MinimumDarkness=3};
   default:throw new InvalidOperationException("Unmapped native achievement content: "+name);
  }
 }
 static string Realm(string map)=>map==Forest?"Forest":map==Temple?"Temple":map==Pumpkin?"Pumpkin Patch":"a standard scenario";
 static string Number(float value)=>value.ToString("0.##",CultureInfo.InvariantCulture);
 static string AttributeName(RunAttribute attribute)
 {
  switch(attribute)
  {
   case RunAttribute.MaxRedHearts:return "maximum red hearts";case RunAttribute.SoulHearts:return "current soul hearts";
   case RunAttribute.TotalHearts:return "current hearts (red + soul)";case RunAttribute.BulletDamage:return "bullet damage";
   case RunAttribute.FireRate:return "shots/sec";case RunAttribute.MoveSpeed:return "movement speed";default:return "";
  }
 }
 static float AttributeValue(RunAttribute attribute,Encounter e)
 {
  switch(attribute)
  {
   case RunAttribute.MaxRedHearts:return e.MaxHP;case RunAttribute.SoulHearts:return e.SoulHP;case RunAttribute.TotalHearts:return e.TotalHP;
   case RunAttribute.BulletDamage:return e.Damage;case RunAttribute.FireRate:return e.FireRate;case RunAttribute.MoveSpeed:return e.MoveSpeed;default:return 0;
  }
 }
 static string GoalFor(Gate g)
 {
  string place=g.Map==null?"any standard scenario":"the "+Realm(g.Map);
  string goal=g.Kind==EncounterKind.Clear?"Clear "+place:g.Kind==EncounterKind.SecretBoss?"Defeat the secret boss in the Forest":
   "Defeat "+(g.Ordinal==0?"the first ":g.Ordinal==1?"the second ":"a ")+(g.Kind==EncounterKind.Miniboss?"miniboss":"boss")+" in "+place;
  if(g.MinimumDarkness>0)goal+=" on Darkness "+g.MinimumDarkness+" or higher";
  if(g.RequiredCharacter!=null)goal+=" as "+contentNames[g.RequiredCharacter];
  if(g.RequiredWeapon!=null)goal+=" using "+contentNames[g.RequiredWeapon];
  if(g.Attribute!=RunAttribute.None)goal+=" with at least "+Number(g.Threshold)+" "+(g.Attribute==RunAttribute.FireRate?"shots per second":AttributeName(g.Attribute));
  if(g.Perks.Length>0)
  {
   var skills=g.Perks.Select(LocalizationSystem.GetLocalizedValue).ToArray();
   goal+=" with "+(skills.Length==1?skills[0]:string.Join(", ",skills.Take(skills.Length-1))+" and "+skills.Last());
  }
  if(g.RequireHitless)goal+=" without taking damage before or during the fight";
  return goal+".";
 }
 static bool MatchesTarget(Gate g,Encounter e)=>g.Kind==e.Kind&&(g.Map==null||g.Map==e.Map)&&(g.Ordinal<0||g.Ordinal==e.Ordinal);
 static bool Meets(Gate g,Encounter e)=>e.Darkness>=g.MinimumDarkness&&(g.RequiredCharacter==null||g.RequiredCharacter==e.Character)&&
  (g.RequiredWeapon==null||g.RequiredWeapon==e.Weapon)&&(!g.RequireHitless||e.Hitless)&&g.Perks.All(e.Perks.Contains)&&
  (g.Attribute==RunAttribute.None||AttributeValue(g.Attribute,e)+.00001f>=g.Threshold);
 static string AttemptText(Gate g,Encounter e)
 {
  var parts=new List<string>{"Darkness "+e.Darkness+" / "+g.MinimumDarkness};
  if(g.RequiredCharacter!=null)parts.Add("character: "+contentNames[e.Character]);
  if(g.RequiredWeapon!=null)parts.Add("weapon: "+contentNames[e.Weapon]);
  if(g.Attribute!=RunAttribute.None)parts.Add(AttributeName(g.Attribute)+": "+Number(AttributeValue(g.Attribute,e))+" / "+Number(g.Threshold));
  if(g.Perks.Length>0)parts.Add("required skills: "+g.Perks.Count(e.Perks.Contains)+" / "+g.Perks.Length);
  if(g.RequireHitless)parts.Add(e.Hitless?"no damage taken":"damage taken; healing does not reset this");
  return "Last attempt: "+string.Join("; ",parts);
 }
 static bool Standard(MapData map)=>map!=null&&!map.achievementsDisabled&&!map.endless&&map.timeLimit==1200&&(map.name==Forest||map.name==Temple||map.name==Pumpkin);
 static Encounter Snapshot(EncounterKind kind,int ordinal)
 {
  var p=PlayerController.Instance;var map=SelectedMap.MapData;
  return new Encounter{Kind=kind,Ordinal=ordinal,Map=map.name,Darkness=map.darknessDisabled?0:Loadout.difficultyLevel,
   Character=Loadout.CharacterSelection.name,Weapon=Loadout.GunSelection.name,MaxHP=p.playerHealth.maxHP,SoulHP=p.playerHealth.shp,
   TotalHP=p.playerHealth.hp+p.playerHealth.shp,Damage=p.gun.damage,FireRate=1f/p.gun.shotCooldown,MoveSpeed=p.finalMoveSpeed,
   Hitless=RunRecorder.Instance.UnlockHitless,Perks=CurrentPerks()};
 }
 static void Record(Encounter e)
 {
  if(State==null)return;bool changed=false;
  foreach(var g in Gates.Where(g=>!State.earned.Contains(g.Id)&&MatchesTarget(g,e)))
  {
   var attempt=State.attempts.FirstOrDefault(a=>a.id==g.Id);if(attempt==null){attempt=new Attempt{id=g.Id};State.attempts.Add(attempt);}
   attempt.text=AttemptText(g,e);changed=true;
   if(!Meets(g,e))continue;
   State.earned.Add(g.Id);State.pending.Add(g.Id);State.notices.Add(new Notice{id=g.Id,goal=g.Goal});
   if(g.Index>=0)(g.Character?SaveSystem.data.characterUnlocks:SaveSystem.data.gunUnlocks).unlocks[g.Index]=true;
   Plugin.Trace("achievement-earned",g.Id);
  }
  Plugin.Trace("achievement-encounter",new{kind=e.Kind.ToString(),map=e.Map,darkness=e.Darkness,ordinal=e.Ordinal,character=e.Character,weapon=e.Weapon,
   maxHP=e.MaxHP,soulHP=e.SoulHP,totalHP=e.TotalHP,damage=e.Damage,fireRate=e.FireRate,moveSpeed=e.MoveSpeed,hitless=e.Hitless,perks=string.Join(",",e.Perks.OrderBy(k=>k))});
  if(changed){Save();Refresh();}
 }
 public static void EnemyDeath(object sender,object args)
 {
  var health=sender as Health;var map=SelectedMap.MapData;
  if(State==null||health==null||!Standard(map)||PlayerController.Instance==null||BossSpawnAchievementBoundary.Spawning)return;
  var game=Object.FindObjectOfType<GameController>();if(game==null||!(game.CurrentState is CombatState))return;
  string name=health.gameObject.name.Replace("(Clone)","").Trim();
  int boss=map.bossSpawns.FindIndex(s=>s.bossPrefab.name==name);
  if(boss>=0){Record(Snapshot(EncounterKind.Boss,boss));return;}
  var elites=map.spawnSessions.Where(s=>s.isElite).OrderBy(s=>s.startTime).ToList();int elite=elites.FindIndex(s=>s.monsterPrefab.name==name);
  if(elite>=0)Record(Snapshot(EncounterKind.Miniboss,elite));
 }
 public static void Survived(){if(State!=null&&Standard(SelectedMap.MapData))Record(Snapshot(EncounterKind.Clear,-1));}
 public static void SecretBoss()
 {
  var map=SelectedMap.MapData;if(State==null||map==null||map.endless||map.achievementsDisabled||(map.name!=Forest&&map.name!="10M_Forest"))return;
  var e=Snapshot(EncounterKind.SecretBoss,-1);e.Map=Forest;Record(e);
 }
}
