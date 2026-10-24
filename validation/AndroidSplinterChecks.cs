using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool SplinterMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-splinter-");
 internal sealed class SplinterEmission{internal Projectile Shot;internal Vector2 Direction;internal Vector3 Position;internal float Size,Speed,Knockback,RecipeDamage,Damage,Multiplier;internal bool Secondary;}
 internal static readonly List<SplinterEmission> SplinterShots=new List<SplinterEmission>();
 static int SplinterCount=>(int)AccessTools.Field(typeof(ShootBulletsOnDeath),"currentBullets").GetValue(null);
 static void SplinterReturnShots(){foreach(var shot in SplinterShots.Select(s=>s.Shot).Distinct().Where(p=>p!=null&&p.gameObject.activeSelf))ReturnFixtureActor(shot.gameObject);}
 void SplinterRecipes(ProjectileRecipe basis,int start)
 {
  var emissions=SplinterShots.Skip(start).ToArray();Require(emissions.Length>0&&basis.size>1,"actual Big Shot creates a larger gun recipe before source Splinter's absolute size assignment");
  foreach(var e in emissions)
  {
   Require(e.Size==.5f&&(float)WindField(e.Shot.GetComponent<AndroidProjectile>(),"Size")==.5f&&e.Secondary&&e.Multiplier==.15f,"actual Splinter emits absolute half-size secondary shots at source15% damage");
   CombatClose(e.Speed,basis.projectileSpeed*.35f,"source Splinter reduces current recipe speed to35%");CombatClose(e.Knockback,basis.knockback*.1f,"source Splinter reduces current recipe knockback to10%");
   CombatClose(e.RecipeDamage,basis.damage,"Splinter uses the current gun recipe damage");Require(e.Damage==Mathf.FloorToInt(Mathf.Max(1,basis.damage*.15f)),"real Splinter factory output retains source floored/minimum-one damage");
  }
  for(int i=0;i<emissions.Length;i+=3)for(int j=0;j<3;j++)Require(Vector2.Angle(emissions[i].Direction.Rotate(j*120),emissions[i+j].Direction)<.02f,"actual three-shot Splinter burst retains source120-degree radial spacing");
 }
 IEnumerator SplinterCombat(GameController game)
 {
  var p=PlayerController.Instance;var basis=p.gun.GetProjectileRecipe();Require(SplinterCount==0,"Splinter source shared coroutine counter is idle before actual deaths");SplinterShots.Clear();
  var first=StatusEnemy("Untagged",1);var second=StatusEnemy("Enemy",1);var third=StatusEnemy("Untagged",1);
  Vector3 a=p.transform.position+Vector3.right*10,b=p.transform.position+Vector3.right*20,c=p.transform.position+Vector3.right*30;
  first.transform.position=a;second.transform.position=b;third.transform.position=c;
  first.TakeDamage(DamageType.Curse,1);Require(SplinterCount==2,"one actual untagged Curse death starts both Splinter helpers synchronously");
  second.TakeDamage(DamageType.Summon,1);Require(SplinterCount==3,"second actual Summon death starts only one more helper at the shared three-coroutine cap");
  third.TakeDamage(DamageType.Bullet,1);Require(SplinterCount==3&&SplinterShots.Count==0,"third actual death is suppressed while source counter3 precedes first-yield emissions");
  while(SplinterShots.Count<9)yield return null;Require(SplinterShots.Count==9&&SplinterCount==3,"three source coroutines emit nine shots while all remain counted during their two final yields");
  Require(SplinterShots.Count(e=>Vector3.Distance(e.Position,a)<.01f)==6&&SplinterShots.Count(e=>Vector3.Distance(e.Position,b)<.01f)==3&&SplinterShots.All(e=>Vector3.Distance(e.Position,c)>.01f),"actual emissions retain each admitted death's position, including untagged deaths");
  SplinterRecipes(basis,0);while(SplinterCount!=0)yield return null;
  var shot=SplinterShots[0].Shot;foreach(var other in SplinterShots.Skip(1).Select(e=>e.Shot))if(other.gameObject.activeSelf)ReturnFixtureActor(other.gameObject);
  var child=StatusEnemy("Untagged",1);child.transform.position=p.transform.position+Vector3.right*8;shot.transform.position=child.transform.position+Vector3.left*2;var movement=shot.GetComponent<MoveComponent2D>();movement.enabled=true;movement.vector=Vector2.right*20;Physics2D.SyncTransforms();
  while(!child.isDead&&shot.gameObject.activeSelf)yield return null;Require(child.isDead,"an actual source Splinter projectile physically kills an untagged pooled NPC");
  while(SplinterShots.Count<15)yield return null;Require(SplinterShots.Count==15,"actual Splinter projectile death starts two further three-shot bursts after the shared counter returns to zero");SplinterRecipes(basis,9);while(SplinterCount!=0)yield return null;
  Log("splinter-combat","initial-coroutines=3 initial-shots=9 origins=6/3/0 chain-shots=6 damage="+SplinterShots[0].Damage+" size="+SplinterShots[0].Size+" base-size="+basis.size+" counter="+SplinterCount);
 }
 IEnumerator CheckAndroidSplinter(bool continuation)
 {
  GameController game;if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
   foreach(string name in new[]{"Splinter","Splinter","BigShot"}){Plugin.SetField(game.playerXP,"<level>k__BackingField",1);Plugin.SetField(game.playerXP,"xp",0f);yield return AndroidPick(game,name,game.playerXP.level);}
  }
  var p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();p.SetPosition(new Vector3(100,100,0));var helpers=p.GetComponentsInChildren<ShootBulletsOnDeath>();while(helpers.Any(h=>!StatusSubscribed(h)))yield return null;
  Require(helpers.Length==2&&helpers.All(h=>(int)WindField(h,"numOfBullets")==3&&(float)WindField(h,"damageMultiplier")==.15f),"actual native repeated acquisition/source Continue owns two independent source Splinter listeners with original recipe parameters");
  if(continuation)Require(SplinterShots.Count==0&&SplinterCount==0,"fresh source Continue recreates the acquired owners without saved world shots or coroutine phase");
  Log("fixture-precondition","Owned Endless access/no runes, actual prerequisite/repeated native choices, stopped scheduler/clock, stationary source-pooled NPCs. Three immediate real deaths exercise the source cap; emitted shots are parked for recipe inspection, and one is repositioned for actual physical chain contact. No natural encounter/trajectory/full-run claim.");
  yield return SplinterCombat(game);
  if(!continuation)
  {
   Require(SplinterShots.Skip(9).All(e=>e.Shot.gameObject.activeSelf)&&SplinterCount==0,"explicit source checkpoint contains six live secondary shots after all source coroutines finish");
   yield return DragonSuspend(game,"Splinter repeated native acquisition, actual typed/untagged deaths/shared yielding cap, source radial recipes/physical chain kill and active-projectile source checkpoint");yield break;
  }
  SplinterReturnShots();yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();
  Require(p.GetComponentsInChildren<ShootBulletsOnDeath>().Length==0&&SplinterCount==0,"ordinary native restart clears both Splinter owners after completed source coroutines");SplinterShots.Clear();var victim=StatusEnemy("Untagged",1);victim.TakeDamage(DamageType.Bullet,1);yield return null;yield return null;Require(SplinterCount==0&&SplinterShots.Count==0,"actual NPC death after ordinary restart reaches no old Splinter listener");
  yield return CheckRevolver(game,p,"android-splinter-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Splinter complete native acquisition, actual death/shared coroutine cap/radial recipe/physical chain, exact current Continue and ordinary restart");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class SplinterProjectileProbe
{
 static void Postfix(ProjectileRecipe __0,Vector2 __1,Vector3 __2,float __3,bool __4,Projectile __result)
 {
  if(!RuntimeChecks.SplinterMode||__3!=.15f||!__4)return;
  RuntimeChecks.SplinterShots.Add(new RuntimeChecks.SplinterEmission{Shot=__result,Direction=__1,Position=__2,Size=__0.size,Speed=__0.projectileSpeed,Knockback=__0.knockback,RecipeDamage=__0.damage,Damage=__result.damage,Multiplier=__3,Secondary=__4});RuntimeChecks.EmitterPark(__result);
 }
}
