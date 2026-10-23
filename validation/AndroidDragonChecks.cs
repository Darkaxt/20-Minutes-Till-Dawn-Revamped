using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
using flanne.PowerupSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static readonly Dictionary<int,float> DragonRegenMaxDelta=new Dictionary<int,float>();
 internal static readonly Dictionary<int,float> DragonRegenStarts=new Dictionary<int,float>();
 internal static readonly Dictionary<int,List<float>> DragonRegenTicks=new Dictionary<int,List<float>>();
 internal static readonly Dictionary<int,float> DragonFirstTimers=new Dictionary<int,float>();
 internal static readonly Dictionary<int,float> DragonBodyStarts=new Dictionary<int,float>(),DragonBodyFirstGrants=new Dictionary<int,float>(),DragonBodyGrantDelta=new Dictionary<int,float>();
 static ResumeRuntime.SourceValue DragonValue(string key,int ordinal=0)=>ResumeRuntime.Capture(PlayerController.Instance).sourcePowerups.Single(p=>p.key==key).values[ordinal];
 static AndroidDragonEgg EggState(SummonEgg egg)=>egg.GetComponent<AndroidDragonEgg>();
 static float EggRemaining(SummonEgg egg)=>(float)AccessTools.Method(typeof(AndroidDragonEgg),"GetRemainingHatchSeconds").Invoke(EggState(egg),null);
 static ShootingSummon EggDragon(SummonEgg egg)=>(ShootingSummon)WindField(egg,"summon");
 static float DragonCooldown(ShootingSummon dragon)=>(float)WindField(dragon,"attackCooldown");
 static void DragonTick(MonoBehaviour helper,float timer){Plugin.SetField(helper,"_timer",timer);AccessTools.Method(helper.GetType(),"Update").Invoke(helper,null);}
 static AndroidSoulPowered DragonSoulSource()
 {
  var profile=(PowerupPoolProfile)AccessTools.Field(typeof(PowerupGenerator),"profile").GetValue(PowerupGenerator.Instance);
  var power=profile.powerupPool.Single(p=>RunResume.Key(p)=="soul_power_name");
  return ((flanne.PerkSystem.PerkEffect[])WindField(power,"effects")).Select(e=>WindField(e,"action")).OfType<AndroidCombatAttachment>().Single(a=>a.Key=="soul_power_name").SoulPowered;
 }
 Health dragonObserved;readonly List<int> dragonHits=new List<int>();
 void DragonHurt(int amount)=>dragonHits.Add(amount);
 IEnumerator DragonPhysicalShot(ShootingSummon dragon,int bulletDamage,int bondDamage=0)
 {
  var player=PlayerController.Instance;dragonObserved=HitEffectTarget(maximum:100000,tag:"Untagged");dragonObserved.transform.position=player.transform.position+Vector3.right*3;
  dragonObserved.GetComponent<CircleCollider2D>().radius=1.25f;Plugin.SetField(dragonObserved.GetComponent<MoveComponent2D>(),"knockbackImmune",true);
  ObserveDamage(dragonObserved,DragonHurt);dragonHits.Clear();Physics2D.SyncTransforms();
  if(dragon!=null)
  {
   dragon.enabled=false;dragon.transform.position=player.transform.position;
   var game=Object.FindObjectOfType<GameController>();game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
   var screen=(Vector2)Camera.main.WorldToScreenPoint(dragonObserved.transform.position);InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=screen});
   while(Vector2.Distance(ShootingCursor.Instance.cursorPosition,screen)>.1f)yield return null;
   Require(ShootingAttack(dragon),"actual source Dragon mouse-target attack creates its native projectile group");
  }
  else
  {
   var recipe=player.gun.GetProjectileRecipe();recipe.knockback=0;
   ProjectileFactory.SharedInstance.SpawnProjectile(recipe,Vector2.right,player.transform.position,1,true);
  }
  var shots=Object.FindObjectsOfType<Projectile>().Where(p=>p.owner==(dragon!=null?dragon.gameObject:player.gun.gameObject)||dragon==null&&p.owner==player.gameObject).ToArray();
  Require(shots.Length==(dragon!=null?3:1),"actual source shot owns the expected native projectile count");
  int wanted=dragon!=null?3:bondDamage>0?2:1;
  while(dragonHits.Count<wanted&&shots.Any(p=>p.gameObject.activeSelf))yield return null;
  Log("dragon-physical-damage",(dragon!=null?"Dragon":"player/Bond")+" hits="+string.Join(",",dragonHits));
  Require(dragon!=null?dragonHits.SequenceEqual(new[]{bulletDamage,bulletDamage,bulletDamage}):dragonHits.SequenceEqual(bondDamage>0?new[]{bondDamage,bulletDamage}:new[]{bulletDamage}),"source Dragon/player/Bond physical damage and absence of recursive Dragon hit procs");
  UnobserveDamage(dragonObserved,DragonHurt);
  foreach(var shot in shots)if(shot.gameObject.activeSelf)ReturnFixtureActor(shot.gameObject);ReturnFixtureActor(dragonObserved.gameObject);
 }
 void DragonRuneKill(int expected)
 {
  var target=HitEffectTarget();target.gameObject.tag="Untagged";target.TakeDamage(DamageType.Bullet,1000000);
  Require((int)WindField(PlayerController.Instance.GetComponentInChildren<DropHPOnKillRune>(),"_counter")==expected,"actual source Health death advances the positive Healing counter to"+expected);
 }
 IEnumerator DragonSuspend(GameController game,string scope)
 {
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.pauseMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="ResumeSaveAndReturn"));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete",scope);Application.Quit(0);
 }
 IEnumerator CheckAndroidDragon(bool continuation)
 {
  if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("-rune-")){yield return CheckDragonRuneContinue();yield break;}
  bool hatched=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("-hatched-");GameController game;PlayerController player;
  if(!continuation)
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());
   game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();
   foreach(string name in new[]{"DragonEgg","AgedDragon","TrainedDragon","AgedDragon","TrainedDragon","SoulPowered","SoulLink","DragonBond"})yield return AndroidPick(game,name,game.playerXP.level);
  }
  else
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState)){var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));if(error!=null)throw new InvalidOperationException(error.text);yield return null;}player=PlayerController.Instance;
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();player.SetPosition(new Vector3(100,100,0));
  var eggs=player.GetComponentsInChildren<SummonEgg>(true);var damage=player.GetComponentsInChildren<BuffSummonDamageOverTime>();var speed=player.GetComponentsInChildren<BuffSummonAttackSpeedOverTime>();
  while(damage.Concat<MonoBehaviour>(speed).Any(h=>!DragonFirstTimers.ContainsKey(h.GetInstanceID())))yield return null;
  var dragon=EggDragon(eggs[0]);
  Log("fixture-precondition","Native Endless keeps runes disabled. Source-pooled controlled NPCs and stopped encounter/game clock isolate actual Dragon growth while eggs retain their game-clock deadline. Accelerated Unity time and explicit growth boundary ticks/partial clocks are diagnostic inputs, not naturally played minutes.");
  Require(damage.Length==2&&speed.Length==2&&eggs.Length==(continuation?2:1),"actual repeated Dragon tree creates two independent damage/speed helpers and the recorded egg groups");
  if(!continuation)
  {
   Require(dragon.baseDamage==10&&DragonCooldown(dragon)==3&&Mathf.Abs(EggRemaining(eggs[0])-180)<.01f&&!dragon.gameObject.activeSelf,"source inactive Dragon starts at10 damage/three seconds with a180-second game-clock egg");
   Require(damage.Concat<MonoBehaviour>(speed).All(h=>((List<ShootingSummon>)WindField(h,"_summons")).SequenceEqual(new[]{dragon})),"growth helpers cache the existing inactive Dragon through their actual Start");
   float frozenClock=GameTimer.SharedInstance.timer;Time.timeScale=40;
   while(damage.Any(h=>DragonValue("aged_dragon",Array.IndexOf(damage,h)).count<5)||speed.Any(h=>DragonValue("trained_dragon_name",Array.IndexOf(speed,h)).count<5))yield return null;Time.timeScale=1;
   Require(SelectedMap.MapData.runesDisabled&&player.GetComponentInChildren<AndroidBodySoul>()==null&&player.GetComponentInChildren<DropHPOnKillRune>()==null&&player.playerHealth.shp==3&&player.playerHealth.maxSHP==3&&!dragon.gameObject.activeSelf&&GameTimer.SharedInstance.timer==frozenClock,"real Endless disables runes while Soul Reap/Link and actual90-second regeneration fill three souls while the stopped egg clock stays pending");
   var recovery=player.GetComponentInChildren<AndroidSoulShieldRegeneration>();var ticks=DragonRegenTicks[recovery.GetInstanceID()];
   Log("dragon-regen-clock","ticks="+string.Join(",",ticks.Select(t=>t-DragonRegenStarts[recovery.GetInstanceID()]))+" maxDelta="+DragonRegenMaxDelta[recovery.GetInstanceID()]+" configuredMaximum="+Time.maximumDeltaTime+" duration="+recovery.Duration);
   Require(ticks.Count>=3&&Mathf.Abs(ticks[0]-DragonRegenStarts[recovery.GetInstanceID()]-90)<=DragonRegenMaxDelta[recovery.GetInstanceID()]+.02f,"actual Soul Reap regeneration first fires after90 seconds and repeats while soul capacity remains capped");
   foreach(var helper in damage.Concat<MonoBehaviour>(speed))helper.enabled=false;
   Require(dragon.baseDamage==160&&damage.All(h=>DragonValue("aged_dragon",Array.IndexOf(damage,h)).count==5),"both actual uncapped growth helpers add15 on each of five elapsed60-second ticks to the cached inactive Dragon");
   DragonTick(damage[0],60);Require(DragonValue("aged_dragon").count==5,"source growth checks strict greater-than before adding the frame delta");DragonTick(damage[0],61);Require(DragonValue("aged_dragon").count==6&&dragon.baseDamage==175,"next source damage-growth boundary grants15 and one count");
   while(DragonValue("trained_dragon_name").count<25)DragonTick(speed[0],61);
   float limited=DragonCooldown(dragon);DragonTick(speed[0],61);
   Require(limited<.33f&&limited>.3f&&DragonCooldown(dragon)==limited&&DragonValue("trained_dragon_name").count==26,"source speed growth crosses0.33 once, then stops reducing cooldown while its applied count keeps increasing");
   Require(Mathf.Abs(Bonus(player.stats[StatType.BulletDamage])-.3f)<.001f&&Mathf.Abs(DragonSoulSource().Granted-.3f)<.001f,"actual soul gain updates the singular Soul Powered callback to30% Bullet Damage");
   yield return AndroidPick(game,"SoulPowered",game.playerXP.level);Require(Mathf.Abs(Bonus(player.stats[StatType.BulletDamage])-.6f)<.001f&&Mathf.Abs(DragonSoulSource().Granted-.3f)<.001f,"source Soul Powered repeat keeps the prior30% contribution while tracking only the new30%");
   var bond=player.GetComponentInChildren<DragonBonusDamage>();Require(ReferenceEquals(WindField(bond,"_dragon"),dragon),"native Dragon Bond caches the original inactive Dragon");yield return DragonPhysicalShot(null,32,17);
   yield return AndroidPick(game,"DragonEgg",game.playerXP.level);eggs=player.GetComponentsInChildren<SummonEgg>(true);Require(eggs.Length==2&&EggDragon(eggs[1]).baseDamage==10&&DragonCooldown(EggDragon(eggs[1]))==3,"later egg creates a pristine Dragon without joining the old helpers' cached target lists");
   float[] clocks={17.25f,5.5f};for(int i=0;i<2;i++){Plugin.SetField(damage[i],"_timer",clocks[i]);Plugin.SetField(speed[i],"_timer",i==0?42.5f:8.5f);}
   var captured=ResumeRuntime.Capture(player);Require(captured.sourceHealing==null&&captured.sourceBodySoul==null&&captured.stats.Count==0&&captured.summons.Count==0,"native Endless source payload saves typed growth/egg records without rune handlers or aggregate actor stats");
   yield return DragonSuspend(game,"Native Endless pending eggs, repeated elapsed/boundary Dragon growth, Soul Powered repeat quirk and physical Dragon Bond checkpoint");yield break;
  }
  foreach(var helper in damage.Concat<MonoBehaviour>(speed))helper.enabled=false;
  Require(damage.Select(h=>DragonFirstTimers[h.GetInstanceID()]).SequenceEqual(new[]{17.25f,5.5f})&&speed.Select(h=>DragonFirstTimers[h.GetInstanceID()]).SequenceEqual(new[]{42.5f,8.5f}),"source Continue restores each growth helper's exact independent partial timer before its first Update");
  Require(eggs.All(e=>EggDragon(e).baseDamage==175&&Mathf.Abs(DragonCooldown(EggDragon(e))-.304576f)<.001f)&&damage.Concat<MonoBehaviour>(speed).All(h=>((List<ShootingSummon>)WindField(h,"_summons")).Count==2),"grouped source replay creates both eggs before growth, so both Dragons inherit the old saved growth and source cooldown floor");
  Require(SelectedMap.MapData.runesDisabled&&player.GetComponentInChildren<AndroidBodySoul>()==null&&player.GetComponentInChildren<DropHPOnKillRune>()==null&&player.playerHealth.shp==3&&Mathf.Abs(Bonus(player.stats[StatType.BulletDamage])-(hatched?.5f:.4f))<.001f,"native Endless source continuation preserves actual souls and grouped Soul Powered contributions without enabling runes");
  if(!hatched)
  {
   Require(eggs.All(e=>!EggDragon(e).gameObject.activeSelf&&Mathf.Abs(EggRemaining(e)-180)<.01f),"both saved eggs retain independent pending180-second source game-clock deadlines");
   AccessTools.Method(typeof(AndroidDragonEgg),"RestoreRemainingHatchSeconds").Invoke(EggState(eggs[0]),new object[]{0f});yield return null;
   Require(!dragon.gameObject.activeSelf&&EggRemaining(eggs[0])==0,"source egg does not hatch at equality with its game-clock deadline");
   float began=Time.time;GameTimer.SharedInstance.Start();while(EggRemaining(eggs[0])>=0)yield return null;
   RunResume.Save();Require(DragonValue("dragon_egg_name").a==-1&&!dragon.gameObject.activeSelf,"source hatch success is serialized before the original visual coroutine enables its Dragon");
   while(!dragon.gameObject.activeSelf)yield return null;GameTimer.SharedInstance.Stop();Require(Time.time-began>=.59f&&!eggs[0].gameObject.activeSelf&&!EggDragon(eggs[1]).gameObject.activeSelf,"actual native three-flash hatch completes after0.6 seconds without hatching the second egg");
  }
  else Require(dragon.gameObject.activeSelf&&!EggDragon(eggs[1]).gameObject.activeSelf&&EggRemaining(eggs[0])==-1&&EggRemaining(eggs[1])>0,"source Continue restores the mixed hatched/pending egg states independently");
  while(!SummonStarted(dragon))yield return null;dragon.enabled=false;
  Require(dragon.numProjectiles==3&&dragon.pierce==1&&dragon.projectileSpeed==5&&dragon.targetMouse,"native hatched Dragon retains its actual source three-projectile/mouse/pierce/speed recipe");
  yield return DragonPhysicalShot(dragon,175);yield return DragonPhysicalShot(null,hatched?30:28,17);
  if(!hatched)
  {
   player.playerHealth.shp=1;Require(Mathf.Abs(Bonus(player.stats[StatType.BulletDamage])-.2f)<.001f,"soul loss changes the current Soul Powered portion while retaining its source orphaned contribution");player.playerHealth.shp=3;
   yield return AndroidPick(game,"SoulPowered",game.playerXP.level);Require(Mathf.Abs(Bonus(player.stats[StatType.BulletDamage])-.7f)<.001f,"another actual source repeat at three souls retains another30% contribution");
   for(int i=0;i<2;i++){Plugin.SetField(damage[i],"_timer",i==0?17.25f:5.5f);Plugin.SetField(speed[i],"_timer",i==0?42.5f:8.5f);}
   yield return DragonSuspend(game,"Native mixed hatched/pending eggs, actual Dragon/player-Bond projectiles and grouped growth/Soul Powered quirk resave");yield break;
  }
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new[]{"HealingRuneData","BodyAndSoulData"}.Select(name=>{var copy=Object.Instantiate(Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.name==name));copy.name=name;copy.level=3;return copy;}).ToList());game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  var body=player.GetComponentInChildren<AndroidBodySoul>();
  Require(!SelectedMap.MapData.runesDisabled&&body!=null&&player.GetComponentsInChildren<ShootingSummon>(true).Length==0&&player.GetComponentsInChildren<SummonEgg>(true).Length==0&&Mathf.Abs(Bonus(player.stats[StatType.BulletDamage]))<.001f&&player.playerHealth.maxSHP==6,"native Standard restart clears Dragon/Soul Powered and applies its selected rank-three runes under the real mode rule");
  Time.timeScale=40;while(body.Grants<1)yield return null;
  float delay=DragonBodyFirstGrants[body.GetInstanceID()]-DragonBodyStarts[body.GetInstanceID()];
  Require(Mathf.Abs(delay-300)<=DragonBodyGrantDelta[body.GetInstanceID()]+.02f&&body.Grants==1&&player.playerHealth.shp==1,"actual native Standard Body and Soul first grant waits300 Unity game seconds");
  float partial=DragonBodyFirstGrants[body.GetInstanceID()]+100;while(Time.time<partial)yield return null;Time.timeScale=1;
  Require(body.Grants==1,"the second five-minute grant remains pending after another100 seconds");DragonRuneKill(1);
  yield return DragonSuspend(game,"Native mixed Dragon source Continue and actual projectiles; real Standard restart,300-second BodySoul grant, partial next schedule and positive Healing checkpoint");
 }
 IEnumerator CheckDragonRuneContinue()
 {
  var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
  GameController game;while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState)){var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));if(error!=null)throw new InvalidOperationException(error.text);yield return null;}
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var player=PlayerController.Instance;var body=player.GetComponentInChildren<AndroidBodySoul>();
  Require(body.Grants==1&&player.playerHealth.shp==1&&player.playerHealth.maxSHP==6&&(int)WindField(player.GetComponentInChildren<DropHPOnKillRune>(),"_counter")==1,"fresh Standard continuation restores actual souls and both positive source rune counters");
  Time.timeScale=40;while(body.Grants<2)yield return null;Time.timeScale=1;
  float delay=DragonBodyFirstGrants[body.GetInstanceID()]-DragonBodyStarts[body.GetInstanceID()];
  Require(Mathf.Abs(delay-300)<=DragonBodyGrantDelta[body.GetInstanceID()]+.02f&&body.Grants==2&&player.playerHealth.shp==2,"continued BodySoul waits a fresh300 seconds rather than importing the old200-second remaining delay");DragonRuneKill(2);
  RunResume.Save();var saved=RunResume.Read();Require(saved.runtime.sourceHealing==2&&saved.runtime.sourceBodySoul==2&&saved.runtime.providers.Count==0,"native resave records both updated positive rune counters without an unsaved timer phase");File.Copy(RunCheckpoint,Path.Combine(ResultDir,"resaved-runes.json"));
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;
  Require(player.GetComponentsInChildren<ShootingSummon>(true).Length==0&&player.GetComponentsInChildren<SummonEgg>(true).Length==0&&player.GetComponentInChildren<AndroidBodySoul>()==null&&Mathf.Abs(Bonus(player.stats[StatType.BulletDamage]))<.001f&&player.playerHealth.maxSHP==0,"ordinary empty-rune restart has no Dragon/growth/rune/Soul Powered owners or contributions");yield return CheckRevolver(game,player,"android-dragon-runes-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Native Dragon/Standard scene isolation, positive rune counter continuation/resave, fresh300-second BodySoul schedule and original restart");Application.Quit(0);
 }
}
[HarmonyPatch]
class DragonGrowthFirstClock
{
 static IEnumerable<System.Reflection.MethodBase> TargetMethods()=>new[]{AccessTools.Method(typeof(BuffSummonDamageOverTime),"Update"),AccessTools.Method(typeof(BuffSummonAttackSpeedOverTime),"Update")};
 [HarmonyPriority(Priority.First)]static void Prefix(MonoBehaviour __instance){if(!RuntimeChecks.DragonFirstTimers.ContainsKey(__instance.GetInstanceID()))RuntimeChecks.DragonFirstTimers.Add(__instance.GetInstanceID(),(float)AccessTools.Field(__instance.GetType(),"_timer").GetValue(__instance));}
}
[HarmonyPatch(typeof(AndroidBodySoul),"Init")]
class DragonBodyStartObservation
{static void Postfix(AndroidBodySoul __instance)=>RuntimeChecks.DragonBodyStarts[__instance.GetInstanceID()]=Time.time;}
[HarmonyPatch(typeof(AndroidBodySoul),"DropHP")]
class DragonBodyGrantObservation
{static void Postfix(AndroidBodySoul __instance){if(!RuntimeChecks.DragonBodyFirstGrants.ContainsKey(__instance.GetInstanceID())){RuntimeChecks.DragonBodyFirstGrants.Add(__instance.GetInstanceID(),Time.time);RuntimeChecks.DragonBodyGrantDelta.Add(__instance.GetInstanceID(),Time.deltaTime);}}}

[HarmonyPatch(typeof(AndroidSoulShieldRegeneration),"Start")]
class DragonRegenStartObservation
{static void Postfix(AndroidSoulShieldRegeneration __instance){RuntimeChecks.DragonRegenStarts[__instance.GetInstanceID()]=Time.time;RuntimeChecks.DragonRegenTicks[__instance.GetInstanceID()]=new List<float>();RuntimeChecks.DragonRegenMaxDelta[__instance.GetInstanceID()]=0;}}
[HarmonyPatch(typeof(AndroidSoulShieldRegeneration),"Update")]
class DragonRegenTickObservation
{
 [HarmonyPriority(Priority.First)]static void Prefix(AndroidSoulShieldRegeneration __instance,out float __state){__state=__instance.Remaining;int id=__instance.GetInstanceID();RuntimeChecks.DragonRegenMaxDelta[id]=Mathf.Max(RuntimeChecks.DragonRegenMaxDelta[id],Time.deltaTime);}
 static void Postfix(AndroidSoulShieldRegeneration __instance,float __state){if(__state>0&&__instance.Remaining>__state)RuntimeChecks.DragonRegenTicks[__instance.GetInstanceID()].Add(Time.time);}
}
