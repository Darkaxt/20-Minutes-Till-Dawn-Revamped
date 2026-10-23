// Generated from supplied Android8.6 Endless_Forest MapData.
using System.Collections.Generic;
using flanne;
namespace TwentyMinutes.Modernization;
static class AndroidEndlessTables
{
 internal sealed class Wave{internal SpawnSession Session;internal string[] Pools;}
 internal static List<Wave> Waves()=>new List<Wave>{
  new Wave{Session=new SpawnSession{HP=24,maximum=20,numPerSpawn=4,startTime=0f,duration=60f,spawnCooldown=3f,isElite=false},Pools=new[]{"BrainMonster","PF_SquidMonster"}},
  new Wave{Session=new SpawnSession{HP=24,maximum=50,numPerSpawn=10,startTime=60f,duration=60f,spawnCooldown=4f,isElite=false},Pools=new[]{"BrainMonster","PF_SquidMonster"}},
  new Wave{Session=new SpawnSession{HP=30,maximum=2,numPerSpawn=1,startTime=60f,duration=60f,spawnCooldown=4f,isElite=false},Pools=new[]{"Boomer"}},
  new Wave{Session=new SpawnSession{HP=30,maximum=200,numPerSpawn=7,startTime=120f,duration=240f,spawnCooldown=2f,isElite=false},Pools=new[]{"BrainMonster"}},
  new Wave{Session=new SpawnSession{HP=30,maximum=10,numPerSpawn=2,startTime=120f,duration=240f,spawnCooldown=5f,isElite=false},Pools=new[]{"Boomer"}},
  new Wave{Session=new SpawnSession{HP=6000,maximum=1,numPerSpawn=1,startTime=180f,duration=5f,spawnCooldown=10f,isElite=true},Pools=new[]{"ElderBrain"}},
  new Wave{Session=new SpawnSession{HP=60,maximum=400,numPerSpawn=12,startTime=360f,duration=120f,spawnCooldown=2f,isElite=false},Pools=new[]{"BrainMonster","PF_SquidMonster"}},
  new Wave{Session=new SpawnSession{HP=400,maximum=2,numPerSpawn=2,startTime=360f,duration=120f,spawnCooldown=10f,isElite=false},Pools=new[]{"EyeMonster","PF_Cultist"}},
  new Wave{Session=new SpawnSession{HP=300,maximum=5,numPerSpawn=2,startTime=420f,duration=120f,spawnCooldown=1f,isElite=false},Pools=new[]{"Boomer","BigBoomer"}},
  new Wave{Session=new SpawnSession{HP=80,maximum=200,numPerSpawn=18,startTime=480f,duration=120f,spawnCooldown=1f,isElite=false},Pools=new[]{"BrainMonster","PF_SquidMonster"}},
  new Wave{Session=new SpawnSession{HP=400,maximum=200,numPerSpawn=15,startTime=605f,duration=55f,spawnCooldown=1f,isElite=false},Pools=new[]{"Lamprey","PF_SquidMonster"}},
  new Wave{Session=new SpawnSession{HP=800,maximum=2,numPerSpawn=2,startTime=660f,duration=120f,spawnCooldown=1f,isElite=false},Pools=new[]{"EyeMonster","PF_Cultist"}},
  new Wave{Session=new SpawnSession{HP=800,maximum=100,numPerSpawn=12,startTime=660f,duration=120f,spawnCooldown=1f,isElite=false},Pools=new[]{"Lamprey","PF_SquidMonster"}},
  new Wave{Session=new SpawnSession{HP=800,maximum=10,numPerSpawn=5,startTime=660f,duration=120f,spawnCooldown=1f,isElite=false},Pools=new[]{"Boomer","BigBoomer"}},
  new Wave{Session=new SpawnSession{HP=50000,maximum=1,numPerSpawn=1,startTime=680f,duration=5f,spawnCooldown=10f,isElite=true},Pools=new[]{"SpawnerMonster"}},
  new Wave{Session=new SpawnSession{HP=80000,maximum=1,numPerSpawn=1,startTime=960f,duration=5f,spawnCooldown=10f,isElite=true},Pools=new[]{"WingedMonster"}},
  new Wave{Session=new SpawnSession{HP=1200,maximum=10,numPerSpawn=4,startTime=900f,duration=60f,spawnCooldown=1f,isElite=false},Pools=new[]{"EyeMonster"}},
  new Wave{Session=new SpawnSession{HP=1200,maximum=20,numPerSpawn=2,startTime=900f,duration=60f,spawnCooldown=1f,isElite=false},Pools=new[]{"Boomer","BigBoomer"}},
  new Wave{Session=new SpawnSession{HP=1000,maximum=300,numPerSpawn=14,startTime=780f,duration=115f,spawnCooldown=1f,isElite=false},Pools=new[]{"Lamprey","PF_SquidMonster"}},
  new Wave{Session=new SpawnSession{HP=1200,maximum=300,numPerSpawn=30,startTime=960f,duration=120f,spawnCooldown=1f,isElite=false},Pools=new[]{"BrainMonster","PF_SquidMonster"}},
  new Wave{Session=new SpawnSession{HP=1800,maximum=150,numPerSpawn=20,startTime=1080f,duration=120f,spawnCooldown=1f,isElite=false},Pools=new[]{"Lamprey","PF_SquidMonster"}},
  new Wave{Session=new SpawnSession{HP=2400,maximum=20,numPerSpawn=6,startTime=1080f,duration=120f,spawnCooldown=1f,isElite=false},Pools=new[]{"EyeMonster"}},
  new Wave{Session=new SpawnSession{HP=300,maximum=3,numPerSpawn=1,startTime=480f,duration=120f,spawnCooldown=3f,isElite=false},Pools=new[]{"EyeMonster","PF_Cultist"}},
  new Wave{Session=new SpawnSession{HP=1000,maximum=200,numPerSpawn=15,startTime=900f,duration=60f,spawnCooldown=1f,isElite=false},Pools=new[]{"Lamprey","PF_SquidMonster"}},
 };
 internal static readonly AndroidEndlessCurve HordeMaxNumsUp=new AndroidEndlessCurve(1f,1.35f,0f,30f,34);
 internal static readonly AndroidEndlessCurve HordeMovespeedUp=new AndroidEndlessCurve(1f,1.5f,0f,30f,34);
 internal static readonly AndroidEndlessCurve HordeNumPerWpawnUp=new AndroidEndlessCurve(1f,2f,0f,30f,34);
 internal static readonly AndroidEndlessCurve HordeSpawnCdUp=new AndroidEndlessCurve(1f,0.5f,0f,30f,34);
 internal static readonly AndroidEndlessCurve HordehealthUp=new AndroidEndlessCurve(1f,40f,0f,30f,34);
 internal static readonly AndroidEndlessCurve BossAffixUp=new AndroidEndlessCurve(0f,4f,0f,10f,0);
 internal static readonly AndroidEndlessCurve BossSpawnCdUp=new AndroidEndlessCurve(240f,30f,0f,7f,0);
 internal static readonly AndroidEndlessCurve BosshealthUp=new AndroidEndlessCurve(0f,10000000f,0f,30f,34);
 internal static readonly AndroidEndlessCurve BossspecialcooldownUp=new AndroidEndlessCurve(1f,0.4f,0f,10f,34);
 internal const float LevelSeconds=60f;
 internal const float FirstBoss=300f;
 internal const float BossRadius=15f;
 internal const int BossHP=10000;
 internal static readonly string[] BossPools=new[]{"ShubNiggurath","Shoggoth","Yog","HasturBoss"};
}
