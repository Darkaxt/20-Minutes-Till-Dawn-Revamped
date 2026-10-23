// Generated from Android8.6 original MapData by tools/export_android_maps.py.
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;
static class AndroidMapTables
{
 static readonly HashSet<MapData> Configured=new HashSet<MapData>();
 static readonly Dictionary<string,List<SpawnSession>> NativeWaves=new Dictionary<string,List<SpawnSession>>();
 static readonly Dictionary<string,List<BossSpawn>> NativeBosses=new Dictionary<string,List<BossSpawn>>();
 internal static List<SpawnSession> LegacyWaves(MapData map,int cycle)=>NativeWaves.ContainsKey(map.name)?NativeWaves[map.name]:cycle>0?map.endlessSpawnSessions:map.spawnSessions;
 internal static List<BossSpawn> LegacyBosses(MapData map,int cycle)=>NativeBosses.ContainsKey(map.name)?NativeBosses[map.name]:cycle>0?map.endlessBossSpawn:map.bossSpawns;
 internal static void ConfigureLoaded()
 {
  foreach(var map in Resources.FindObjectsOfTypeAll<MapData>())
  {
   if(Configured.Contains(map))continue;
   if(map.name=="Endless_Forest"){map.numPowerupsRepeat=3;map.timeLimit=-1;map.runesDisabled=true;map.achievementsDisabled=true;Configured.Add(map);continue;}
   if(map.name!="10M_Forest"&&map.name!="20M_Forest"&&map.name!="20M_Temple"&&map.name!="20M_PumpkinPatch")continue;
   var prefabs=Resources.FindObjectsOfTypeAll<Health>().Where(h=>!h.gameObject.scene.IsValid()).GroupBy(h=>h.gameObject.name).ToDictionary(g=>g.Key,g=>g.First().gameObject);
   if(!NativeWaves.ContainsKey(map.name)){NativeWaves.Add(map.name,map.spawnSessions);NativeBosses.Add(map.name,map.bossSpawns);}
   Configure(map,n=>prefabs[n]);Configured.Add(map);
  }
 }
 static void Configure(MapData map,Func<string,GameObject> prefab)
 {
  switch(map.name)
  {
   case "10M_Forest":
    map.spawnSessions=new List<SpawnSession>{
     new SpawnSession{monsterPrefab=prefab("BrainMonster"),HP=24,maximum=20,numPerSpawn=4,startTime=0f,duration=60f,spawnCooldown=3f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("BrainMonster"),HP=24,maximum=50,numPerSpawn=10,startTime=60f,duration=60f,spawnCooldown=4f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Boomer"),HP=30,maximum=2,numPerSpawn=1,startTime=60f,duration=60f,spawnCooldown=4f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("BrainMonster"),HP=30,maximum=200,numPerSpawn=7,startTime=120f,duration=240f,spawnCooldown=2f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Boomer"),HP=30,maximum=10,numPerSpawn=2,startTime=120f,duration=240f,spawnCooldown=5f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("ElderBrain"),HP=1000,maximum=1,numPerSpawn=1,startTime=180f,duration=1f,spawnCooldown=2f,isElite=true},
     new SpawnSession{monsterPrefab=prefab("BrainMonster"),HP=60,maximum=400,numPerSpawn=12,startTime=360f,duration=120f,spawnCooldown=2f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("EyeMonster"),HP=400,maximum=2,numPerSpawn=2,startTime=360f,duration=120f,spawnCooldown=10f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Boomer"),HP=60,maximum=1,numPerSpawn=1,startTime=420f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("BrainMonster"),HP=100,maximum=500,numPerSpawn=20,startTime=480f,duration=119f,spawnCooldown=1f,isElite=false},
    };
    map.bossSpawns=new List<BossSpawn>{
     new BossSpawn{bossPrefab=prefab("ShubNiggurath"),maxHP=2000,timeToSpawn=300f,spawnPosition=new Vector3(-15f,0f,0f)},
    };
    break;
   case "20M_Forest":
    map.spawnSessions=new List<SpawnSession>{
     new SpawnSession{monsterPrefab=prefab("BrainMonster"),HP=24,maximum=20,numPerSpawn=4,startTime=0f,duration=60f,spawnCooldown=3f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("BrainMonster"),HP=24,maximum=50,numPerSpawn=10,startTime=60f,duration=60f,spawnCooldown=4f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Boomer"),HP=30,maximum=2,numPerSpawn=1,startTime=60f,duration=60f,spawnCooldown=4f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("BrainMonster"),HP=30,maximum=200,numPerSpawn=7,startTime=120f,duration=240f,spawnCooldown=2f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Boomer"),HP=30,maximum=10,numPerSpawn=2,startTime=120f,duration=240f,spawnCooldown=5f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("ElderBrain"),HP=1000,maximum=1,numPerSpawn=1,startTime=180f,duration=5f,spawnCooldown=10f,isElite=true},
     new SpawnSession{monsterPrefab=prefab("BrainMonster"),HP=60,maximum=400,numPerSpawn=12,startTime=360f,duration=120f,spawnCooldown=2f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("EyeMonster"),HP=400,maximum=2,numPerSpawn=2,startTime=360f,duration=120f,spawnCooldown=10f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Boomer"),HP=60,maximum=1,numPerSpawn=1,startTime=420f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("BrainMonster"),HP=80,maximum=600,numPerSpawn=18,startTime=480f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Lamprey"),HP=400,maximum=30,numPerSpawn=3,startTime=605f,duration=55f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("EyeMonster"),HP=400,maximum=12,numPerSpawn=2,startTime=660f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Lamprey"),HP=500,maximum=100,numPerSpawn=5,startTime=660f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Boomer"),HP=240,maximum=4,numPerSpawn=2,startTime=660f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("SpawnerMonster"),HP=10000,maximum=1,numPerSpawn=1,startTime=680f,duration=5f,spawnCooldown=10f,isElite=true},
     new SpawnSession{monsterPrefab=prefab("WingedMonster"),HP=18000,maximum=1,numPerSpawn=1,startTime=960f,duration=5f,spawnCooldown=10f,isElite=true},
     new SpawnSession{monsterPrefab=prefab("EyeMonster"),HP=500,maximum=16,numPerSpawn=2,startTime=900f,duration=60f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Boomer"),HP=400,maximum=20,numPerSpawn=2,startTime=900f,duration=60f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Lamprey"),HP=600,maximum=300,numPerSpawn=14,startTime=780f,duration=115f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("BrainMonster"),HP=400,maximum=300,numPerSpawn=30,startTime=960f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Lamprey"),HP=600,maximum=150,numPerSpawn=20,startTime=1080f,duration=119f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("EyeMonster"),HP=600,maximum=10,numPerSpawn=1,startTime=1080f,duration=119f,spawnCooldown=1f,isElite=false},
    };
    map.bossSpawns=new List<BossSpawn>{
     new BossSpawn{bossPrefab=prefab("ShubNiggurath"),maxHP=2000,timeToSpawn=300f,spawnPosition=new Vector3(-15f,0f,0f)},
     new BossSpawn{bossPrefab=prefab("Shoggoth"),maxHP=30000,timeToSpawn=900f,spawnPosition=new Vector3(0f,10f,0f)},
    };
    break;
   case "20M_Temple":
    map.spawnSessions=new List<SpawnSession>{
     new SpawnSession{monsterPrefab=prefab("PF_SquidMonster"),HP=24,maximum=20,numPerSpawn=4,startTime=0f,duration=60f,spawnCooldown=3f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_SquidMonster"),HP=30,maximum=50,numPerSpawn=10,startTime=60f,duration=60f,spawnCooldown=4f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Boomer"),HP=30,maximum=2,numPerSpawn=1,startTime=60f,duration=60f,spawnCooldown=4f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Boomer"),HP=30,maximum=200,numPerSpawn=7,startTime=120f,duration=180f,spawnCooldown=2f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("BigBoomer"),HP=2000,maximum=10,numPerSpawn=2,startTime=120f,duration=240f,spawnCooldown=5f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("ElderBrain"),HP=1500,maximum=1,numPerSpawn=1,startTime=180f,duration=5f,spawnCooldown=10f,isElite=true},
     new SpawnSession{monsterPrefab=prefab("PF_SquidMonster"),HP=100,maximum=400,numPerSpawn=12,startTime=360f,duration=180f,spawnCooldown=2f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("EyeMonster"),HP=400,maximum=5,numPerSpawn=6,startTime=300f,duration=120f,spawnCooldown=5f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("BigBoomer"),HP=2000,maximum=1,numPerSpawn=1,startTime=420f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_SquidMonster"),HP=80,maximum=600,numPerSpawn=16,startTime=480f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_Cultist"),HP=600,maximum=4,numPerSpawn=1,startTime=605f,duration=355f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_SquidMonster"),HP=200,maximum=60,numPerSpawn=10,startTime=630f,duration=90f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("BigBoomer"),HP=2500,maximum=2,numPerSpawn=1,startTime=720f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("Boomer"),HP=240,maximum=4,numPerSpawn=2,startTime=660f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("SpawnerMonster"),HP=10000,maximum=1,numPerSpawn=1,startTime=680f,duration=5f,spawnCooldown=10f,isElite=true},
     new SpawnSession{monsterPrefab=prefab("Lamprey"),HP=400,maximum=300,numPerSpawn=8,startTime=780f,duration=115f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("WingedMonster"),HP=18000,maximum=1,numPerSpawn=1,startTime=1080f,duration=5f,spawnCooldown=10f,isElite=true},
     new SpawnSession{monsterPrefab=prefab("PF_SquidMonster"),HP=300,maximum=600,numPerSpawn=14,startTime=990f,duration=209f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("BigBoomer"),HP=500,maximum=4,numPerSpawn=2,startTime=960f,duration=239f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_Cultist"),HP=500,maximum=4,numPerSpawn=2,startTime=960f,duration=239f,spawnCooldown=1f,isElite=false},
    };
    map.bossSpawns=new List<BossSpawn>{
     new BossSpawn{bossPrefab=prefab("PF_Yog"),maxHP=10000,timeToSpawn=300f,spawnPosition=new Vector3(16f,0f,0f)},
     new BossSpawn{bossPrefab=prefab("PF_HasturBoss"),maxHP=30000,timeToSpawn=900f,spawnPosition=new Vector3(0f,10f,0f)},
    };
    break;
   case "20M_PumpkinPatch":
    map.spawnSessions=new List<SpawnSession>{
     new SpawnSession{monsterPrefab=prefab("PF_PumpkinMonster"),HP=24,maximum=20,numPerSpawn=4,startTime=0f,duration=60f,spawnCooldown=3f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_PumpkinMonster"),HP=24,maximum=50,numPerSpawn=10,startTime=60f,duration=60f,spawnCooldown=4f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_GhostMonster"),HP=30,maximum=2,numPerSpawn=1,startTime=60f,duration=60f,spawnCooldown=4f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_PumpkinMonster"),HP=30,maximum=200,numPerSpawn=7,startTime=120f,duration=240f,spawnCooldown=2f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_GhostMonster"),HP=30,maximum=10,numPerSpawn=2,startTime=120f,duration=240f,spawnCooldown=5f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("ElderBrain"),HP=1000,maximum=1,numPerSpawn=1,startTime=180f,duration=5f,spawnCooldown=10f,isElite=true},
     new SpawnSession{monsterPrefab=prefab("PF_PumpkinMonster"),HP=60,maximum=400,numPerSpawn=12,startTime=360f,duration=120f,spawnCooldown=2f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_EyeBat"),HP=400,maximum=2,numPerSpawn=2,startTime=360f,duration=120f,spawnCooldown=10f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_GhostMonster"),HP=60,maximum=1,numPerSpawn=1,startTime=420f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_PumpkinMonster"),HP=80,maximum=600,numPerSpawn=16,startTime=480f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_WitchMonster"),HP=200,maximum=30,numPerSpawn=3,startTime=605f,duration=55f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_EyeBat"),HP=200,maximum=12,numPerSpawn=2,startTime=660f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_WitchMonster"),HP=250,maximum=100,numPerSpawn=5,startTime=660f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_GhostMonster"),HP=240,maximum=4,numPerSpawn=2,startTime=660f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("SpawnerMonster"),HP=10000,maximum=1,numPerSpawn=1,startTime=680f,duration=5f,spawnCooldown=10f,isElite=true},
     new SpawnSession{monsterPrefab=prefab("WingedMonster"),HP=18000,maximum=1,numPerSpawn=1,startTime=960f,duration=5f,spawnCooldown=10f,isElite=true},
     new SpawnSession{monsterPrefab=prefab("PF_PumpkinMonster"),HP=300,maximum=16,numPerSpawn=2,startTime=900f,duration=60f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_GhostMonster"),HP=400,maximum=20,numPerSpawn=2,startTime=900f,duration=60f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_WitchMonster"),HP=400,maximum=300,numPerSpawn=14,startTime=780f,duration=115f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_PumpkinMonster"),HP=250,maximum=600,numPerSpawn=26,startTime=960f,duration=120f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_WitchMonster"),HP=500,maximum=300,numPerSpawn=20,startTime=1080f,duration=119f,spawnCooldown=1f,isElite=false},
     new SpawnSession{monsterPrefab=prefab("PF_EyeBat"),HP=500,maximum=10,numPerSpawn=1,startTime=1080f,duration=119f,spawnCooldown=1f,isElite=false},
    };
    map.bossSpawns=new List<BossSpawn>{
     new BossSpawn{bossPrefab=prefab("ShubNiggurath"),maxHP=2500,timeToSpawn=300f,spawnPosition=new Vector3(-15f,0f,0f)},
     new BossSpawn{bossPrefab=prefab("PF_Reaper"),maxHP=35000,timeToSpawn=900f,spawnPosition=new Vector3(0f,10f,0f)},
    };
    break;
  }
 }
 internal static string[] PoolTags(MapData map)
 {
  switch(map.name)
  {
   case "10M_Forest": return new[]{"BrainMonster","BrainMonster","Boomer","BrainMonster","Boomer","ElderBrain","BrainMonster","EyeMonster","Boomer","BrainMonster"};
   case "20M_Forest": return new[]{"BrainMonster","BrainMonster","Boomer","BrainMonster","Boomer","ElderBrain","BrainMonster","EyeMonster","Boomer","BrainMonster","Lamprey","EyeMonster","Lamprey","Boomer","SpawnerMonster","WingedMonster","EyeMonster","Boomer","Lamprey","BrainMonster","Lamprey","EyeMonster"};
   case "20M_Temple": return new[]{"PF_SquidMonster","PF_SquidMonster","Boomer","Boomer","BigBoomer","ElderBrain","PF_SquidMonster","EyeMonster","BigBoomer","PF_SquidMonster","PF_Cultist","PF_SquidMonster","BigBoomer","Boomer","SpawnerMonster","Lamprey","WingedMonster","PF_SquidMonster","BigBoomer","PF_Cultist"};
   case "20M_PumpkinPatch": return new[]{"PumpkinMonster","PumpkinMonster","GhostMonster","PumpkinMonster","GhostMonster","ElderBrain","PumpkinMonster","EyeBat","GhostMonster","PumpkinMonster","WitchMonster","EyeBat","WitchMonster","GhostMonster","SpawnerMonster","WingedMonster","PumpkinMonster","GhostMonster","WitchMonster","PumpkinMonster","WitchMonster","EyaBat"};
   default: throw new InvalidOperationException("No source map pool table: "+map.name);
  }
 }
}
