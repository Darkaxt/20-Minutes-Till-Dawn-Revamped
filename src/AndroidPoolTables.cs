using System.Collections.Generic;
using System.Linq;
namespace TwentyMinutes.Modernization;
// Original Android8.6 ObjectPooler scene table; source Get rejects count > max.
internal static class AndroidPoolTables
{
 internal static string PrefabName(string tag)=>Items.FirstOrDefault(i=>i.Tag==tag)?.Prefab??tag;
 internal sealed class Entry{internal string Tag,Prefab;internal int Visible,Maximum,Initial;internal Entry(string tag,string prefab,int visible,int maximum,int initial){Tag=tag;Prefab=prefab;Visible=visible;Maximum=maximum;Initial=initial;}}
 internal static readonly Entry[] Items={
  new Entry("BulletImpact","PF_BulletImact",-1,25,0),
  new Entry("EnemyDeathFX","PF_DeathAnimation",-1,25,0),
  new Entry("SmallXP","PF_SmallXPPickup",800,99999,6000),
  new Entry("LargeXP","PF_LargeXPPickup",-1,99999,0),
  new Entry("Chest","PF_Chest",-1,99999,0),
  new Entry("DevilDeal","PF_DevilDeal",-1,99999,0),
  new Entry("ExplosionFX","BoomerExplosion",-1,99999,0),
  new Entry("Burn","PF_BurnFX",-1,99999,0),
  new Entry("FireExplosion","PF_FireExplosion",-1,99999,0),
  new Entry("FireExplosionSmall","PF_FireExplosionSmall",-1,99999,0),
  new Entry("DamagePopup","PF_DamagePoppup",-1,20,0),
  new Entry("BrainMonster","BrainMonster",-1,99999,0),
  new Entry("Boomer","Boomer",-1,99999,0),
  new Entry("ElderBrain","ElderBrain",-1,99999,0),
  new Entry("EyeMonster","EyeMonster",-1,99999,0),
  new Entry("EyeMonsterProjectile","EyeMonsterProjectile",-1,99999,0),
  new Entry("Lamprey","Lamprey",-1,99999,0),
  new Entry("WingedMonster","WingedMonster",-1,99999,0),
  new Entry("SpawnerMonster","SpawnerMonster",-1,99999,0),
  // Same source tag/data; the native owned prefab retains its PF_ name.
  new Entry("GrenadeExplosion","PF_GrenadeExplosion",-1,99999,0),
  new Entry("Sound","Audio Source",-1,250,0),
  new Entry("BigBoomer","BigBoomer",-1,99999,0),
  new Entry("PF_Cultist","PF_Cultist",-1,99999,0),
  new Entry("PF_SquidMonster","PF_SquidMonster",-1,99999,0),
  new Entry("CultistProjectile","PF_CultistProjectile",-1,99999,0),
  new Entry("EyeBat","PF_EyeBat",-1,99999,0),
  new Entry("GhostMonster","PF_GhostMonster",-1,99999,0),
  new Entry("PumpkinMonster","PF_PumpkinMonster",-1,99999,0),
  new Entry("WitchMonster","PF_WitchMonster",-1,99999,0),
  new Entry("EyaBat","PF_EyeBat",-1,99999,0),
  new Entry("ChestMark","ChestMark",-1,999999,0),
  new Entry("ShubNiggurath","ShubNiggurath",-1,999999,0),
  new Entry("Shoggoth","Shoggoth",-1,999999,0),
  new Entry("Yog","PF_Yog",-1,999999,0),
  new Entry("HasturBoss","PF_HasturBoss",-1,999999,0),
 };
}
