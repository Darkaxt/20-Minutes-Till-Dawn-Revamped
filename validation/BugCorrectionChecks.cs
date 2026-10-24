using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.RuneSystem;
using flanne.Pickups;
using flanne.PowerupSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 static object BugCall(object owner,string method,params object[] args)=>AccessTools.Method(owner.GetType(),method).Invoke(owner,args);
 int bugSmiteKills,bugBurns;
 void BugSmiteKill(object sender,object args)=>bugSmiteKills++;
 void BugBurned(object sender,object args)=>bugBurns++;
 static bool BugSaved(string key)=>ResumeRuntime.Capture(PlayerController.Instance).sourcePowerups.Any(p=>p.key==key);
 IEnumerator BugPickKey(GameController game,string key)
 {yield return AndroidPick(game,FallbackPool(game.powerupGenerator).Single(i=>RunResume.Key(i.powerup)==key).powerup.name,game.playerXP.level);}
 void BugPoolEdges()
 {
  var owner=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();var prototype=new GameObject("BugPoolPrototype");prototype.SetActive(false);prototype.AddComponent<SpriteRenderer>();
  BugCall(owner,"Register","BugCorrectionPool",prototype,1);
  var map=(System.Collections.IDictionary)WindField(owner,"pools");var pool=map["BugCorrectionPool"];Plugin.SetField(pool,"Maximum",3);
  var native=ObjectPooler.SharedInstance;var a=native.GetPooledObject("BugCorrectionPool");var b=native.GetPooledObject("BugCorrectionPool");b.GetComponent<SpriteRenderer>().enabled=false;var c=native.GetPooledObject("BugCorrectionPool");
  Require(!a.GetComponent<SpriteRenderer>().enabled,"visibility eviction disables the exact oldest renderer");
  foreach(var types in new[]{new[]{typeof(string)},new[]{typeof(string),typeof(Vector3)},new[]{typeof(string),typeof(Vector3),typeof(Transform)}})
  {
   object[] args=types.Length==1?new object[]{"BugCorrectionPool"}:types.Length==2?new object[]{"BugCorrectionPool",Vector3.zero}:new object[]{"BugCorrectionPool",Vector3.zero,owner.transform};
   Require(AccessTools.Method(typeof(AndroidPools),"Get",types).Invoke(owner,args)==null,"all pool overloads reject the exact maximum: "+types.Length);
  }
  ReturnFixtureActor(a);Require(native.GetPooledObject("BugCorrectionPool")==a&&a.GetComponent<SpriteRenderer>().enabled,"return/reacquire at cap restores the visibility-evicted renderer");
  ReturnFixtureActor(b);Require(native.GetPooledObject("BugCorrectionPool")==b&&!b.GetComponent<SpriteRenderer>().enabled,"reacquire preserves an authored/otherwise disabled renderer");
  foreach(var actor in new[]{a,b,c})ReturnFixtureActor(actor);Object.Destroy(prototype);
  var large=native.GetPooledObject("LargeXP");Require(large!=null&&large.transform.Find("Bounce").GetComponent<SpriteRenderer>().enabled,"LargeXP binds its original Bounce renderer before pool initialization");ReturnFixtureActor(large);
  var edge=new GameObject("BugArenaSegment");var segments=edge.AddComponent<AndroidLineSegments>();BugCall(segments,"Set",0,Vector2.zero,Vector2.right,1f);var collider=edge.GetComponentInChildren<PolygonCollider2D>();collider.gameObject.SetActive(false);collider.enabled=false;BugCall(segments,"Set",0,Vector2.zero,Vector2.up,1f);Require(collider.gameObject.activeSelf&&collider.enabled,"cached arena segment is reactivated when its geometry is rebuilt");Object.Destroy(edge);
 }
 IEnumerator BugBurnEdges(bool might)
 {
  var system=BurnSystem.SharedInstance;var state=AndroidBurnState.Get(system);bool enabled=system.enabled;system.enabled=false;this.AddObserver(BugBurned,BurnSystem.InflictBurnEvent);
  var noHealth=new GameObject("BugInvalidBurn");int notices=bugBurns;system.Burn(noHealth,1);Require(state.Find(noHealth)==null&&bugBurns==notices,"invalid target cannot register a burn or publish successful infliction");Object.Destroy(noHealth);
  var first=StatusEnemy();system.Burn(first.gameObject,1);var buff=state.Find(first.gameObject);BugCall(buff,"Tick",.6f);BugCall(buff,"Release");yield return StatusReturn(first);
  var second=StatusEnemy();int hp=second.HP;system.Burn(second.gameObject,1);var reused=state.Find(second.gameObject);Require(ReferenceEquals(buff,reused),"burn regression exercises the exact released status object");BugCall(reused,"Tick",.5f);Require(second.HP==hp,"reused burn does not inherit the previous target's tick phase");system.Burn(second.gameObject,2);BugCall(reused,"Tick",.5f);Require(second.HP==hp-(might?5:3),"same-target refresh preserves elapsed cadence and accumulates damage, including the continued Might bonus");BugCall(reused,"Release");
  system.burnDurationMultiplier.AddMultiplierBonus(-1);notices=bugBurns;system.Burn(second.gameObject,1);Require(state.Find(second.gameObject)==null&&bugBurns==notices,"zero-duration burn cannot register FX/state or announce success");system.burnDurationMultiplier.AddMultiplierBonus(1);
  system.enabled=enabled;this.RemoveObserver(BugBurned,BurnSystem.InflictBurnEvent);yield return StatusReturn(second);
 }
 IEnumerator BugSmite(GameController game)
 {
  var p=PlayerController.Instance;var helper=p.GetComponentInChildren<SmitePassive>();while(!StatusSubscribed(helper))
  {if(WindField(helper,"ammo")!=null)break;yield return null;}
  this.AddObserver(BugSmiteKill,SmitePassive.SmiteKillNotification);
  var justice=p.GetComponentInChildren<GainMaxHPOnSmiteKill>();var angelic=p.GetComponentInChildren<HealOnSmiteKill>();while(!StatusSubscribed(justice)||!StatusSubscribed(angelic))yield return null;
  Plugin.SetField(justice,"_killCounter",499);Plugin.SetField(angelic,"_killCounter",499);int max=p.playerHealth.maxHP;p.playerHealth.hp=max-2;
  foreach(bool protectedHit in new[]{false,true})
  {
   var target=StatusEnemy(maximum:100000);target.transform.position=p.transform.position+Vector3.right*2;if(protectedHit)target.isInvincible.Flip();Physics2D.SyncTransforms();int hp=target.HP,kills=bugSmiteKills;
   p.ammo.Reload();p.ammo.UseAmmo(p.ammo.amount);while(JusticeSmiteFlowProbe.Running)yield return null;yield return null;
   Require(bugSmiteKills==kills&&!target.isDead&&(protectedHit?target.HP==hp:target.HP<hp),"real surviving/protected Smite does not publish a kill");Require(CombatCount(justice,"_killCounter")==499&&CombatCount(angelic,"_killCounter")==499&&p.playerHealth.maxHP==max&&p.playerHealth.hp==max-2,"false Smite contacts cannot advance Justice or Angelic thresholds");if(protectedHit)target.isInvincible.UnFlip();yield return StatusReturn(target);
  }
  var victim=StatusEnemy(maximum:1);victim.transform.position=p.transform.position+Vector3.right*2;Physics2D.SyncTransforms();int before=bugSmiteKills,growth=CombatCount(justice,"_mhpGainCounter");p.ammo.Reload();p.ammo.UseAmmo(p.ammo.amount);while(JusticeSmiteFlowProbe.Running)yield return null;
  Log("bugfix-lethal-smite",new{hp=victim.HP,invincible=victim.isInvincible.value,before,kills=bugSmiteKills,justice=CombatCount(justice,"_killCounter"),angelic=CombatCount(angelic,"_killCounter"),max=p.playerHealth.maxHP});Require(victim.isDead&&bugSmiteKills==before+1&&CombatCount(justice,"_killCounter")==0&&CombatCount(angelic,"_killCounter")==0&&CombatCount(justice,"_mhpGainCounter")==growth+1,"actual lethal Smite publishes once and advances both consumers at their seeded threshold");this.RemoveObserver(BugSmiteKill,SmitePassive.SmiteKillNotification);yield return StatusReturn(victim);
 }
 IEnumerator BugShatter()
 {
  var native=ObjectPooler.SharedInstance;const string tag="Revamped_AndroidShatter";var p=PlayerController.Instance;
  while(native.pooledObjectsDictionary[tag].Any(a=>a.activeSelf))yield return null;
  var victim=StatusEnemy(maximum:100);victim.AutoKill();Require(!native.pooledObjectsDictionary[tag].Any(a=>a.activeSelf),"unfrozen death does not activate Shatter");yield return StatusReturn(victim);
  victim=StatusEnemy(maximum:100);victim.transform.position=p.transform.position+Vector3.right*100;FreezeSystem.SharedInstance.Freeze(victim.gameObject);victim.AutoKill();Require(native.pooledObjectsDictionary[tag].Count(a=>a.activeSelf)==1,"actual frozen death activates Shatter once");yield return StatusReturn(victim);while(native.pooledObjectsDictionary[tag].Any(a=>a.activeSelf))yield return null;
  for(int i=0;i<51;i++){var h=StatusEnemy(maximum:100);h.transform.position=p.transform.position+Vector3.right*100;FreezeSystem.SharedInstance.Freeze(h.gameObject);h.AutoKill();}
  Require(native.pooledObjectsDictionary[tag].Count(a=>a.activeSelf)==50,"same-frame frozen deaths respect the corrected Shatter maximum50");while(native.pooledObjectsDictionary[tag].Any(a=>a.activeSelf))yield return null;
 }
 IEnumerator BugOffers(GameController game)
 {
  var generator=game.powerupGenerator;var field=AccessTools.Field(typeof(PowerupGenerator),"powerupPool");var original=(List<PowerupPoolItem>)field.GetValue(generator);var candidate=original.First(i=>i.powerup.prereqs.Count==0&&!FallbackPowerups.IsFallback(i.powerup));
  var one=new List<PowerupPoolItem>{candidate,candidate};field.SetValue(generator,one);Require(generator.GetRandom(5).SequenceEqual(new[]{candidate.powerup}),"finite weighted offers return one unique candidate when only duplicates remain");field.SetValue(generator,original);
  // Exercise the complete real native level-up menu with a short list, then
  // an empty list. This checks its indices, stale entries and empty exit.
  field.SetValue(generator,new List<PowerupPoolItem>{candidate});game.playerXP.GainXP((float)AccessTools.Property(typeof(flanne.Player.PlayerXP),"xpToLevel").GetValue(game.playerXP)+1);
  while(!(game.CurrentState is PowerupMenuState)||WindField(game.powerupMenu,"ConfirmEvent")==null)yield return null;
  var toggles=(Toggle[])WindField(game.powerupMenu,"toggles");Require(toggles.Count(t=>t.gameObject.activeSelf)==1&&game.powerupMenu.toggledData==candidate.powerup,"short native menu hides stale choices and selects the available reward");game.powerupMenu.OnConfirmClicked();while(!(game.CurrentState is CombatState))yield return null;
  field.SetValue(generator,new List<PowerupPoolItem>());game.playerXP.GainXP((float)AccessTools.Property(typeof(flanne.Player.PlayerXP),"xpToLevel").GetValue(game.playerXP)+1);while(!(game.CurrentState is PowerupMenuState))yield return null;while(!(game.CurrentState is CombatState))yield return null;Require(!PauseController.isPaused,"empty native level-up menu exits without equipping null or leaving combat paused");
  var characterField=AccessTools.Field(typeof(PowerupGenerator),"characterPool");var characterPool=characterField.GetValue(generator);characterField.SetValue(generator,new List<PowerupPoolItem>());this.PostNotification(ChestPickup.ChestPickupEvent);yield return null;Require(game.CurrentState is CombatState&&!PauseController.isPaused,"exhausted native chest returns to combat without indexing an absent reward");characterField.SetValue(generator,characterPool);
  var devilField=AccessTools.Field(typeof(PowerupGenerator),"devilPool");var devilPool=(List<PowerupPoolItem>)devilField.GetValue(generator);devilField.SetValue(generator,new List<PowerupPoolItem>{devilPool[0]});this.PostNotification(DevilDealPickup.DevilDealPickupEvent);while(!(game.CurrentState is DevilDealState)||WindField(game.devilDealMenu,"ConfirmEvent")==null)yield return null;Require(((Toggle[])WindField(game.devilDealMenu,"toggles")).Count(t=>t.gameObject.activeSelf)==1&&game.devilDealMenu.toggledData==devilPool[0].powerup,"short native devil menu hides old choices and selects its one valid reward");Click(game.devilDealLeaveButton);while(!(game.CurrentState is CombatState))yield return null;devilField.SetValue(generator,new List<PowerupPoolItem>());this.PostNotification(DevilDealPickup.DevilDealPickupEvent);yield return null;Require(game.CurrentState is CombatState&&!PauseController.isPaused,"empty native devil menu returns to combat without a null reward");devilField.SetValue(generator,devilPool);field.SetValue(generator,original.Where(i=>i.numTimeRepeatable!=0).ToList());
 }
 IEnumerator CheckBugCorrections(string mode)
 {
  if(mode.Contains("-buff-")){yield return BugCharacterBuffs(mode);yield break;}
  if(mode.Contains("-characters")){yield return BugCharacterRuns(mode);yield break;}
  if(mode.Contains("-late-"))
  {
   yield return CombatBuffContinue();var restored=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var restoredHorde=Object.FindObjectOfType<HordeSpawner>();var restoredLate=restoredHorde.GetComponent<AndroidLateEnemyDamage>();
   Require(restoredLate!=null&&restoredHorde.enemyDamage==2,"D10 Continue reconstructs the live owner and single late contribution from its fresh baseline and saved clock");BugCall(restoredLate,"Sync");Require(restoredHorde.enemyDamage==2,"repeated D10 synchronization after Continue does not duplicate damage");
   GeneratorClock(899);BugCall(restoredLate,"Sync");Require(restoredHorde.enemyDamage==2,"activated one-shot D10 damage remains applied if a Custom clock is moved backward");GeneratorClock(901);
   if(mode.Contains("-contact-"))
   {
    var run=Object.FindObjectOfType<AndroidEndlessRun>();int index=Enumerable.Range(0,WorldWaves(run).Count).First(i=>!WorldSession(run,i).isElite);var existing=new HashSet<int>(Object.FindObjectsOfType<Health>().Select(h=>h.GetInstanceID()));BugCall(run,"Spawn",index);
    var npc=Object.FindObjectsOfType<Health>().Single(h=>!existing.Contains(h.GetInstanceID())&&h.GetComponent<AIComponent>()!=null);Require(npc.GetComponent<AIComponent>().damageToPlayer==2,"actual source-spawned ordinary NPC receives the late D10 damage value2");
    var player=PlayerController.Instance;int contactHP=player.playerHealth.hp;if(player.playerHealth.isInvincible.value)player.playerHealth.isInvincible.UnFlip();DurableEnemy(npc.gameObject,player.transform.position+Vector3.right*.25f,false);Physics2D.SyncTransforms();while(player.playerHealth.hp==contactHP)yield return new WaitForFixedUpdate();Require(player.playerHealth.hp==contactHP-2,"real spawned-NPC physical contact subtracts the corrected two hearts");ReturnFixtureActor(npc.gameObject);
    Log("fixture-precondition","D10 contact uses a real source Spawn call and NPC/player physics with seeded clock, target position and disabled target AI; no natural fifteen-minute encounter claim.");
   }
   yield return CheckRevolver(restored,PlayerController.Instance,"bugfix-late-continue.png");yield return AchievementExit(restored,false);SetPhase("PASS");Log("complete","D10 fresh/current Continue/idempotent one-shot clock damage and native firing");Application.Quit(0);yield break;
  }
  bool continuation=mode.EndsWith("continue"),repeated=mode.Contains("repeated");GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));
  Log("fixture-precondition","Corrected23-family batch: actual native acquisitions, source pooled actors, native menus, ammo-zero Smite, real health events and current Continue/restart. Explicit target geometry, boundary counters/clocks and direct status ticks isolate root causes; no natural-frequency/full-run claim.");
  BugPoolEdges();yield return BugBurnEdges(continuation);
  if(!continuation)
  {
   Require(Object.FindObjectsOfType<AndroidSummonPulse>().Length==0,"Pulsing configuration edge starts with no eligible listener");new AndroidCombatAttachment{Key="pulsing_summons_name"}.Activate(p.gameObject);Require((string)AccessTools.Field(typeof(AndroidSummonPulse),"DurationData").GetValue(null)=="50,2,3","actual Pulsing action stores its payload even when no listener exists");AccessTools.Method(typeof(AndroidSummonPulse),"Clear").Invoke(null,null);
   foreach(string name in new[]{"HolyArts","Justice","Angelic","Shatter","Generator","Generator","SoulLink"})yield return AndroidPick(game,name,game.playerXP.level);
   float soulBaseline=Bonus(p.stats[StatType.BulletDamage]);int souls=p.playerHealth.shp;yield return AndroidPick(game,"SoulPowered",game.playerXP.level);yield return AndroidPick(game,"SoulPowered",game.playerXP.level);CombatClose(Bonus(p.stats[StatType.BulletDamage]),soulBaseline+souls*.1f,"two actual Soul Powered acquisitions retain one tracked contribution without a leaked repeat bonus");
   yield return BugPickKey(game,"pulsing_summons_name");yield return AndroidPick(game,"GhostFriend",game.playerXP.level);
  }
  var pulse=p.GetComponentInChildren<AndroidSummonPulse>();Require(pulse!=null&&pulse.enabled&&pulse.Damage==50&&pulse.Interval==2&&pulse.Range==3,"summon acquired after Pulsing Summons inherits stored original configuration, including Continue replay");pulse.enabled=false;
  foreach(var summon in Object.FindObjectsOfType<Summon>())summon.gameObject.SetActive(false);
  foreach(var activePulse in Object.FindObjectsOfType<AndroidSummonPulse>())activePulse.enabled=false;
  Log("fixture-precondition","Deactivate all acquired summon actors, including detached Scythe, after checking Pulsing configuration so another real producer cannot kill the one-HP Smite target during its windup.");
  var generators=p.GetComponentsInChildren<AndroidGenerator>();while(generators.Any(g=>WindField(g,"buff")==null))yield return null;Require(generators.Length==2,"two acquired Generator owners survive reconstruction");
  if(continuation)Require(generators.All(g=>!g.Applied&&CombatCount(g.GetComponent<BuffPlayerStats>())==0)&&!BugSaved("generator_name"),"Continue omits temporary Generator counts and cannot create a permanent contribution");
  float rate=Bonus(p.stats[StatType.FireRate]);p.ammo.Reload();p.ammo.UseAmmo(1);p.ammo.GainAmmo(1);Require(generators.All(g=>g.Applied&&CombatCount(g.GetComponent<BuffPlayerStats>())==1),"repeated actual ammo events retain one live contribution per Generator");CombatClose(Bonus(p.stats[StatType.FireRate]),rate+.7f,"two live Generator contributions are exactly seventy-percent");GeneratorClock(generators.Max(g=>g.AppliedAt)+2.01f);while(generators.Any(g=>g.Applied))yield return null;Require(generators.All(g=>CombatCount(g.GetComponent<BuffPlayerStats>())==0),"Generator expiry decrements live contribution counts");CombatClose(Bonus(p.stats[StatType.FireRate]),rate,"Generator expiry leaves no counted replay residue");
  p.playerHealth.shp=0;float soulBase=Bonus(p.stats[StatType.BulletDamage]);p.playerHealth.shp=3;CombatClose(Bonus(p.stats[StatType.BulletDamage]),soulBase+.3f,"repeated Soul Powered tracks only the current three-soul contribution");p.playerHealth.shp=1;CombatClose(Bonus(p.stats[StatType.BulletDamage]),soulBase+.1f,"soul changes reverse the whole tracked prior contribution without a leak");
  yield return BugSmite(game);yield return BugShatter();
  var lightning=StatusEnemy();lightning.transform.position=p.transform.position+Vector3.right*6;int hp=lightning.HP;ThunderGenerator.SharedInstance.GenerateAt(lightning.transform.position,7);Require(lightning.HP==hp-(continuation?9:7),"positional Thunder applies negative signed damage, including continued Might, rather than healing");yield return StatusReturn(lightning);
  if(!continuation)yield return NativeTomePick(game,false,"TomeOfMight");
  var target=StatusEnemy();hp=target.HP;target.TakeDamage(DamageType.Bullet,10);Require(target.HP==hp-12,"Might adds two at the native positive damage boundary");hp=target.HP;AndroidDamageRoundingCall(target,-10,DamageType.Bullet);Require(target.HP==hp-12,"Might adds two at the signed source health boundary");hp=target.HP;target.HPChange(2);Require(target.HP==hp+2,"Might preserves healing");hp=target.HP;target.HPChange(0);Require(target.HP==hp,"Might preserves zero signed changes");yield return StatusReturn(target);
  // Asset-owned listener teardown must remove both observer roots.
  var fire=ScriptableObject.CreateInstance<AndroidFireEdge>();BugCall(fire,"Apply");Require(StatusSubscribed(fire),"Fire Edge test acquires its real two-event asset listener");Object.Destroy(fire);yield return null;Require(!StatusSubscribed(fire),"Fire Edge destruction removes all of its notification registrations");
  yield return BugOffers(game);
  if(!repeated){p.ammo.Reload();yield return DragonSuspend(game,"Corrected pool/status/Smite/lightning/Might/summon/Soul Powered and live Generator boundary; temporary buff deliberately active at save");yield break;}
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",10,()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var horde=Object.FindObjectOfType<HordeSpawner>();var late=horde.GetComponent<AndroidLateEnemyDamage>();Require(late!=null,"D10 has a live run-scoped clock owner");
  int baseline=horde.enemyDamage;Require(baseline==1,"fresh D10 retains the actual native ordinary enemy-damage baseline1 before the late threshold");GeneratorClock(GameTimer.SharedInstance.timeLimit-300);BugCall(late,"Sync");Require(horde.enemyDamage==baseline,"D10 strict threshold excludes equality");GeneratorClock(GameTimer.SharedInstance.timeLimit-299);BugCall(late,"Sync");BugCall(late,"Sync");Require(horde.enemyDamage==baseline+1,"D10 adds the original one damage exactly once after its threshold");
  yield return DragonSuspend(game,"Corrected D10 active late-damage checkpoint for independent Continue verification");
 }
 static void AndroidDamageRoundingCall(Health target,int change,DamageType kind)=>AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidDamageRounding"),"ChangeHealth").Invoke(null,new object[]{target,change,kind});
 IEnumerator BugCharacterBuffs(string mode)
 {
  bool continuation=mode.EndsWith("continue"),repeated=mode.Contains("repeated");string character=mode.Contains("diamond")?"Diamond":mode.Contains("scarlett")?"Scarlett":mode.Contains("lilith")?"Lilith":"Abby";GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();foreach(string fact in new[]{"scenario:Endless_Forest","character:"+character})Progression.State.earned.Add(fact);Progression.Refresh();yield return AchievementRun(character,"RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();
   foreach(string key in character=="Diamond"?new[]{"diamond_perk_strong_will_name","diamond_perk_berserk_name","diamond_perk_bulk_up_name"}:new[]{character=="Scarlett"?"scarlett_perk_pyromaniac_name":character=="Lilith"?"lilith_perk_necrophile_name":"abby_perk_whirlwind_name"})yield return AndroidWhirlwindChest(game,key);
  }
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));yield return null;yield return null;
  if(character=="Diamond")
  {
   bool active=mode.Contains("-active-");var will=CharacterBuff("PF_BuffOnBelowHalfHP");var hurt=CharacterBuff("PF_BuffOnHurt");var heal=CharacterBuff("PF_BuffOnHeal");Require(CombatCount(will)==(active&&continuation?1:0)&&!BugSaved("diamond_perk_strong_will_name"),"Strong Will reconstructs exactly the current saved-health condition without replaying historical counts");
   int hurtBefore=CombatCount(hurt),healBefore=CombatCount(heal);
   if(continuation)
   {
    var saved=RunResume.Read().runtime;Require(hurtBefore==saved.sourcePowerups.Single(s=>s.key=="diamond_perk_berserk_name").values.Single().count&&healBefore==saved.sourcePowerups.Single(s=>s.key=="diamond_perk_bulk_up_name").values.Single().count,"Continue restores earned permanent Berserk/Bulk Up counts exactly");DiamondStats(hurtBefore,healBefore,active?2:0,active?.25f:0);
    if(active){Require(p.playerHealth.hp==3,"active conditional checkpoint restores the actual low HP");p.playerHealth.Heal(4);healBefore++;Require(CombatCount(will)==0,"real continued healing removes the reconstructed conditional contribution");DiamondStats(hurtBefore,healBefore,0,0);}
   }
   for(int cycle=0;cycle<2;cycle++)
   {
    CharacterHurt(4);Require(p.playerHealth.hp==3&&CombatCount(will)==1,"actual below-half hurt grants exactly one live Strong Will contribution");DiamondStats(hurtBefore+(cycle+1)*4,healBefore+cycle,2,.25f);p.playerHealth.Heal(4);Require(p.playerHealth.hp==7&&CombatCount(will)==0,"healing above half removes the conditional count");DiamondStats(hurtBefore+(cycle+1)*4,healBefore+cycle+1,0,0);
   }
   Require(!BugSaved("diamond_perk_strong_will_name")&&BugSaved("diamond_perk_berserk_name")&&BugSaved("diamond_perk_bulk_up_name"),"writer saves only permanent earned buff stacks");
   if(active&&!repeated){CharacterHurt(4);Require(p.playerHealth.hp==3&&CombatCount(will)==1&&!BugSaved("diamond_perk_strong_will_name"),"save while Strong Will is genuinely active still omits its conditional count");}
  }
  else if(character=="Scarlett")
  {
   var buff=CharacterBuff("BuffOnBurn");Require(CombatCount(buff)==0&&!BugSaved("scarlett_perk_pyromaniac_name"),"Pyromaniac Continue/fresh state omits the previous reload-bound phase");CombatClose(Bonus(p.stats[StatType.FireRate]),0,"Pyromaniac has no restored residue");var target=StatusEnemy();
   for(int i=1;i<=3;i++){BurnSystem.SharedInstance.Burn(target.gameObject,1);Require(CombatCount(buff)==i,"actual valid burn increments the current Pyromaniac phase");}p.ammo.Reload();Require(CombatCount(buff)==0,"RemoveStacks clears all Pyromaniac applications despite decrementing counts");CombatClose(Bonus(p.stats[StatType.FireRate]),0,"RemoveStacks reverses all three contributions");BurnSystem.SharedInstance.Burn(target.gameObject,1);BurnSystem.SharedInstance.Burn(target.gameObject,1);Require(CombatCount(buff)==2&&!BugSaved("scarlett_perk_pyromaniac_name"),"active reload-bound Pyromaniac phase is excluded from saved permanent stacks");yield return StatusReturn(target);
  }
  else
  {
   string key=character=="Lilith"?"lilith_perk_necrophile_name":"abby_perk_whirlwind_name";var buff=(TemporaryRefreshingBuff)CharacterBuff(character=="Lilith"?"PF_BuffOnDeath":"PF_BuffOnReload");Require(CombatCount(buff)==0&&!BugSaved(key),"temporary buff starts with zero replayed contribution after fresh/current/repeated Continue");
   for(int cycle=0;cycle<2;cycle++)
   {
    if(character=="Lilith"){NecrophileKill(DamageType.Bullet);NecrophileKill(DamageType.Summon);}else{p.ammo.Reload();p.ammo.Reload();}
    Require(CombatCount(buff)==1,"real repeated producer refreshes one temporary contribution");while(WindField(buff,"buffCoroutine")!=null)yield return null;Require(CombatCount(buff)==0,"actual temporary expiry returns the live counter to zero");CombatClose(Bonus(p.stats[StatType.MoveSpeed]),0,"temporary expiry leaves no movement residue");CombatClose(Bonus(p.stats[StatType.FireRate]),0,"temporary expiry leaves no Fire Rate residue");
   }
   if(character=="Lilith")NecrophileKill(DamageType.Bullet);else p.ammo.Reload();Require(CombatCount(buff)==1&&!BugSaved(key),"active temporary contribution is deliberately omitted at the real save boundary");
  }
  if(!repeated){yield return DragonSuspend(game,"Corrected "+character+" permanent-versus-transient buff capture and real repeated producers/expiry");yield break;}
  yield return CharacterBuffRestart(game,"bugfix-"+character+"-restart.png");SetPhase("PASS");Log("complete","Corrected "+character+" repeated current Continue, actual buff producers/expiry/permanent persistence and native restart");Application.Quit(0);
 }
 IEnumerator BugCharacterRuns(string mode)
 {
  bool continuation=mode.EndsWith("continue");GameController game;
  if(continuation){yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  else
  {
   RunResume.Invalidate();foreach(string fact in new[]{"scenario:Endless_Forest","character:Revamped_Dimond_Portrait","character:Revamped_Haruka"})Progression.State.earned.Add(fact);Progression.Refresh();yield return AchievementRun("Revamped_Dimond_Portrait","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();
  }
  var skill=PlayerController.Instance.GetComponentInChildren<PortedCharacterSkill>();while(!skill.Ready)yield return null;StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  if(!continuation)
  {
   foreach(string key in new[]{"PerpetualFighterPowerup","SteelAccelerationPowerup","PerpetualFighterPowerup","SteelAccelerationPowerup"})yield return AndroidWhirlwindChest(game,"revamped_power_"+key);
   Require(skill.PerpetualLimit==6&&skill.Steel&&skill.SteelDamageAdded==2,"repeated native Bulwark acquisitions preserve within-run accumulation and one speed guard");yield return DragonSuspend(game,"Repeated Bulwark state checkpoint before replay/reset regression");yield break;
  }
  Require(skill.PerpetualLimit==6&&skill.Steel&&skill.SteelDamageAdded==2,"Continue replays Bulwark accumulators from clean run-local state");yield return AchievementExit(game,false);
  yield return AchievementRun("Revamped_Dimond_Portrait","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();skill=PlayerController.Instance.GetComponentInChildren<PortedCharacterSkill>();while(!skill.Ready)yield return null;
  yield return AndroidWhirlwindChest(game,"revamped_power_PerpetualFighterPowerup");yield return AndroidWhirlwindChest(game,"revamped_power_SteelAccelerationPowerup");Require(skill.PerpetualLimit==3&&skill.Steel&&skill.SteelDamageAdded==1,"new Bulwark run has no asset accumulator or speed-guard residue");yield return AchievementExit(game,false);
  for(int cycle=0;cycle<2;cycle++)
  {
   yield return AchievementRun("Revamped_Haruka","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();skill=PlayerController.Instance.GetComponentInChildren<PortedCharacterSkill>();while(!skill.Ready)yield return null;CombatClose(skill.BaseRange,1.960000038f,"fresh/restarted Gale uses the same original Whirlwind baseline");if(cycle==0){yield return AndroidWhirlwindChest(game,"revamped_power_UltimatePowerup");CombatClose(skill.BaseRange,3,"actual in-run Whirlwind range upgrade remains effective");}yield return AchievementExit(game,false);
  }
  SetPhase("PASS");Log("complete","Bulwark repeats/current Continue/new run and Whirlwind upgraded teardown/new-run baseline");Application.Quit(0);
 }
}
