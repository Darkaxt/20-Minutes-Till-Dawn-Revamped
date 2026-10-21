using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 TimeToLive[] BossArenas()=>Object.FindObjectsOfType<TimeToLive>().Where(t=>t.name=="ArenaMonster(Clone)").ToArray();
 IEnumerator SpawnArenaBoss(int index,int hp)
 {
  var source=SelectedMap.MapData.bossSpawns[index];
  Object.FindObjectOfType<BossSpawner>().LoadSpawners(new List<BossSpawn>{new BossSpawn{bossPrefab=source.bossPrefab,timeToSpawn=0,maxHP=hp,spawnPosition=source.spawnPosition,dontSpawnArena=false}});
  while(!Object.FindObjectsOfType<Health>().Any(h=>h.name==source.bossPrefab.name+"(Clone)"&&!h.isDead))yield return null;
 }
 IEnumerator CheckBossArenas(bool baseline)
 {
  SetPhase("native-boss-arena-lifetime");yield return AchievementRun("Shana","RevolverData","20M_Forest");
  var game=Object.FindObjectOfType<GameController>();Object.FindObjectOfType<HordeSpawner>().enabled=false;
  Require(BossArenas().Length==0,"ordinary run starts without a boss arena");yield return SpawnArenaBoss(0,20);
  var first=Object.FindObjectsOfType<Health>().Single(h=>h.name==SelectedMap.MapData.bossSpawns[0].bossPrefab.name+"(Clone)"&&!h.isDead);
  var arena=BossArenas().Single();
  Require((float)AccessTools.Field(typeof(TimeToLive),"lifetime").GetValue(arena)==60f,"native arena retains its existing60-second lifetime");
  yield return null;Require(arena!=null&&arena.gameObject.activeInHierarchy,"arena remains while its boss is alive");
  first.TakeDamage(DamageType.Bullet,100000);Require(first.isDead,"actual native boss Health.TakeDamage reaches death");
  yield return new WaitForEndOfFrame();yield return null;
  Require((arena!=null)==baseline,baseline?"baseline arena remains after its boss dies":"arena is destroyed at the native destruction boundary after its own boss dies");
  yield return Capture(baseline?"arena-after-boss-baseline.png":"arena-after-boss-corrected.png");
  if(!baseline)
  {
   yield return SpawnArenaBoss(0,25);yield return SpawnArenaBoss(1,30);
   var owners=Object.FindObjectsOfType<Health>().Where(h=>!h.isDead&&SelectedMap.MapData.bossSpawns.Any(s=>h.name==s.bossPrefab.name+"(Clone)")).ToArray();
   var arenas=BossArenas();Require(owners.Length==2&&arenas.Length==2,"two live native bosses create two independent arenas");
   var bindings=arenas.Select(a=>a.GetComponent("BossArenaLifetime")).ToArray();
   Require(bindings.All(b=>b!=null),"each actual native arena has a lifetime owner");
   var linked=bindings.Select(b=>(Health)AccessTools.Field(b.GetType(),"owner").GetValue(b)).ToArray();
   Require(linked.Distinct().Count()==2&&owners.All(linked.Contains),"each arena is linked to the exact boss instance that created it");
   owners[0].TakeDamage(DamageType.Bullet,100000);yield return new WaitForEndOfFrame();yield return null;
   Require(BossArenas().Length==1&&(Health)AccessTools.Field(BossArenas()[0].GetComponent("BossArenaLifetime").GetType(),"owner").GetValue(BossArenas()[0].GetComponent("BossArenaLifetime"))==owners[1],"killing one boss clears only its own arena");
  }
  yield return AchievementExit(game,false);
  yield return AchievementRun("Shana","RevolverData","20M_Forest");game=Object.FindObjectOfType<GameController>();
  Require(BossArenas().Length==0,"native scene exit and fresh run retain no arena or boss subscription");
  yield return CheckRevolver(game,PlayerController.Instance,"arena-original-restart.png");yield return AchievementExit(game,false);
  SetPhase("PASS");Log("complete",baseline?"Native boss death leaves its arena alive under the original60-second lifetime":"Native boss death clears its owned arena, overlapping bosses stay independent, active-arena exit and original restart pass");Application.Quit(0);
 }
}
