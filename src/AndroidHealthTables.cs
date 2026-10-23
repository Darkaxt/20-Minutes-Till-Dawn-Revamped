// Generated from the original Android 8.6 Health fields; do not retune.
// Inventory SHA256: dec5ef5a54ad06ba5c9712ada7f8935757bfc512d46c2d8f7aa47b7280fc8bdf
using System.Collections.Generic;
namespace TwentyMinutes.Modernization;
internal static class AndroidHealthTables
{
 internal sealed class Recipe
 {
  internal readonly int MaxHP,CreatureType;internal readonly string[] Drops,Callbacks;internal readonly bool Shake;
  internal Recipe(int maxHP,int creatureType,string[] drops,bool shake,string[] callbacks){MaxHP=maxHP;CreatureType=creatureType;Drops=drops;Shake=shake;Callbacks=callbacks;}
 }
 internal static readonly Dictionary<string,Recipe> Recipes=new Dictionary<string,Recipe>{
  {"BigBoomer",new Recipe(30,1,new string[]{"BigBoomerExplosion","SmallXP"},false,new string[]{"PoolRelease","SpawnPrefab","Spawn:SmallXP","Play","Shake"})},
  {"Boomer",new Recipe(30,1,new string[]{"ExplosionFX","SmallXP"},true,new string[]{"PoolRelease","Spawn:ExplosionFX","Spawn:SmallXP","Play","Shake"})},
  {"BrainMonster",new Recipe(24,1,new string[]{"SmallXP","EnemyDeathFX"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"ElderBrain",new Recipe(1000,2,new string[]{"Chest","EnemyDeathFX"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:Chest","Play"})},
  {"EyeMonster",new Recipe(24,1,new string[]{"SmallXP","EnemyDeathFX"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"Lamprey",new Recipe(24,1,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_Cultist",new Recipe(24,1,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_EyeBat",new Recipe(24,1,new string[]{"SmallXP","EnemyDeathFX"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_GhostMonster",new Recipe(24,1,new string[]{"SmallXP","EnemyDeathFX"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_HasturBoss",new Recipe(2500,2,new string[]{"EnemyDeathFX","DevilDeal"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Play","Spawn:DevilDeal"})},
  {"PF_PumpkinMonster",new Recipe(24,1,new string[]{"SmallXP","EnemyDeathFX"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_Reaper",new Recipe(35000,2,new string[]{"EnemyDeathFX","DevilDeal"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Play","Spawn:DevilDeal"})},
  {"PF_SpawnedBug",new Recipe(400,0,new string[]{"EnemyDeathFX"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Play"})},
  {"PF_SquidMonster",new Recipe(24,1,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_TentacleEnemy",new Recipe(1200,0,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_TreeMonster",new Recipe(90000,1,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_TreeMonster (1)",new Recipe(90000,1,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_TreeMonster (12)",new Recipe(90000,1,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_TreeMonster (14)",new Recipe(90000,1,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_TreeMonster (15)",new Recipe(90000,1,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_TreeMonster (4)",new Recipe(90000,1,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_TreeMonster (8)",new Recipe(90000,1,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_TreeMonster (9)",new Recipe(90000,1,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_WalkingTree",new Recipe(45000,0,new string[]{"EnemyDeathFX","SmallXP"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_WitchMonster",new Recipe(24,1,new string[]{"SmallXP","EnemyDeathFX"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:SmallXP","Play"})},
  {"PF_Yog",new Recipe(35000,2,new string[]{"EnemyDeathFX","DevilDeal"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Play","Spawn:DevilDeal"})},
  {"PF_YogsBoomer",new Recipe(80,1,new string[]{"ExplosionFX","SmallXP"},true,new string[]{"PoolRelease","Spawn:ExplosionFX","Spawn:SmallXP","Play","Shake"})},
  {"Rhogog",new Recipe(150000,0,new string[]{"EnemyDeathFX"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Play"})},
  {"Shoggoth",new Recipe(30000,2,new string[]{"EnemyDeathFX","DevilDeal"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Play","Spawn:DevilDeal"})},
  {"ShubNiggurath",new Recipe(2000,2,new string[]{"EnemyDeathFX","DevilDeal"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Play","Spawn:DevilDeal"})},
  {"SpawnerMonster",new Recipe(1000,2,new string[]{"EnemyDeathFX","Chest"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:Chest","Play"})},
  {"WingedMonster",new Recipe(1000,2,new string[]{"EnemyDeathFX","Chest"},false,new string[]{"PoolRelease","Spawn:EnemyDeathFX","Spawn:Chest","Play"})},
 };
}
