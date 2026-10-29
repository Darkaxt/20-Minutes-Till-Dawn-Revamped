using System;
using System.IO;
using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static AIComponent ElementAimed;internal static Vector2 ElementAimPoint;internal static int ElementStrikes;
 int ElementChainNotices;void ElementChainHit(object sender,object args)=>ElementChainNotices++;
 IEnumerator CheckElements(string mode)
 {
  if(mode=="elements-save"||mode=="elements-continue"||mode=="elements-prior"){yield return ElementSave(mode);yield break;}
  if(mode=="elements-maps"){yield return ElementMaps();yield break;}
  if(mode=="elements-producers"){yield return ElementProducers();yield break;}
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;
  if(mode!="elements-inspect")
  {
   if(mode=="elements-lightning")yield return ElementLightning(game);
   else if(mode=="elements-fire")yield return ElementFire(game);
   else if(mode=="elements-wind")yield return ElementWind(game);
   else throw new InvalidOperationException("Unknown elemental workflow "+mode);
   yield return AchievementExit(game,false);SetPhase("PASS");Log("complete",mode+" complete native elemental workflow");Application.Quit(0);yield break;
  }
  var a=HitEffectTarget();var b=HitEffectTarget();Vector3 origin=p.transform.position+Vector3.right*5;
  float radius=(float)WindField(ThunderGenerator.SharedInstance,"baseAoE");a.transform.position=origin;b.transform.position=origin+Vector3.up*(radius*.75f);Physics2D.SyncTransforms();
  var art=a.GetComponent<SpriteRenderer>();Log("element-native-reference","BrainMonster sprite="+art.sprite.name+" bounds="+art.sprite.bounds+" scale="+art.transform.lossyScale+" lightning-radius="+radius);
  int before=a.HP,other=b.HP;ThunderGenerator.SharedInstance.GenerateAt(origin,22);yield return null;
  Log("element-baseline","center="+(before-a.HP)+" edge="+(other-b.HP));
  Require(before-a.HP>other-b.HP&&other>b.HP,"lightning applies radial falloff rather than equal flat area damage");
  SetPhase("PASS");Application.Quit(0);
 }
 IEnumerator ElementProducers()
 {
  RunResume.Invalidate();Progression.State.earned.Add("character:Scarlett");Progression.Refresh();yield return AchievementRun("Scarlett","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;StopFixtureSpawning();
  foreach(string power in new[]{"ElectroMage","FireStarter","Thunderspawns"})yield return AndroidPick(game,power,game.playerXP.level);
  foreach(var summon in p.GetComponentsInChildren<AttackingSummon>())summon.enabled=false;
  yield return NativeTomePick(game,false,"TomeOfElectro");
  var target=ElementEnemy((Vector2)p.transform.position+Vector2.right*3);target.GetComponent<MoveComponent2D>().enabled=false;EmitterResetRecords();int strikes=ElementStrikes;
  yield return MothShots(game,4);Require(ElementStrikes>=strikes+2&&Control(target).Stun>0,"actual Electro Mage shots reach the shared electrical damage and stun owner");
  var flaming=ElementEnemy((Vector2)p.transform.position+Vector2.left*3);flaming.GetComponent<MoveComponent2D>().enabled=false;
  var wave=Emitted.First(e=>e.Name=="PF_FireWave").Shot;wave.transform.position=flaming.transform.position;ElementCall(wave.GetComponent<AndroidProjectile>(),"Contact",flaming.gameObject);Require(AndroidBurnState.Get(BurnSystem.SharedInstance).Find(flaming.gameObject)!=null&&Control(flaming).Fear>0,"actual Scarlett fire wave reaches shared burn and panic");
  target.transform.position=p.transform.position+new Vector3(.8f,.8f);strikes=ElementStrikes;int hp=target.HP;p.ammo.UseAmmo(p.ammo.amount);Require(ElementStrikes>strikes&&target.HP<hp,"actual zero-ammo Tome callback reaches new lightning owner and damages its selected target");
  strikes=ElementStrikes;ControlSummonHurtRolls=true;SummonThunderMinimum=0;SummonThunderMaximum=.15f;target.TakeDamage(DamageType.Summon,1);ControlSummonHurtRolls=false;Require(ElementStrikes==strikes+1,"actual surviving Summon damage dispatches Thunderspawns into the shared lightning owner");
  yield return PoiCapture("element-character-tome-synergy.png");ElementClear();yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Actual Scarlett wave, Electro Mage shooting, zero-ammo Tome and Thunderspawns surviving-hit producers with acquired recipes");Application.Quit(0);
 }
 IEnumerator ElementMaps()
 {
  foreach(string map in new[]{"20M_Forest","20M_Temple","20M_PumpkinPatch"})
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:"+map);Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData",map,prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
   var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;StopFixtureSpawning();
   foreach(string power in new[]{"ElectroMage","PyroMage","AeroMagic"})yield return AndroidPick(game,power,game.playerXP.level);
   var target=ElementEnemy((Vector2)p.transform.position+Vector2.right*2);ThunderGenerator.SharedInstance.GenerateAt(target.gameObject,22);BurnSystem.SharedInstance.Burn(target.gameObject,3);
   Require(Control(target).Stun>0&&Control(target).Fear>0,"mixed control operates on actual scenario "+map);yield return MothShots(game,2);
   while(Object.FindObjectOfType<AndroidWindActor>()==null)yield return null;Require(Object.FindObjectOfType<AndroidWindActor>()!=null,"actual Gale producer operates in "+map);yield return PoiCapture("element-map-"+map+".png");
   ElementClear();yield return AchievementExit(game,false);
  }
  SetPhase("PASS");Log("complete","Three real scenario scenes, actual elemental offers/shot/Gale producers and mixed status; controlled enemies, no full-run balance claim");Application.Quit(0);
 }
 IEnumerator ElementSave(string mode)
 {
  bool restore=mode!="elements-save";
  if(restore)yield return CombatBuffContinue();else{RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());}
  var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  if(!restore)foreach(string name in new[]{"Energized","ElectroMastery","IntenseFlame","FireStarter","AeroMagic","Windborne","AeroMastery","EyeOfTheStorm","QuickHands"})yield return AndroidPick(game,name,game.playerXP.level);
  yield return null;yield return null;
  if(mode=="elements-prior")
  {
   Require(p.playerHealth.hp>0&&game.CurrentState is CombatState,"previous released checkpoint restores into combat with new element owner");
   yield return AndroidPick(game,"Energized",game.playerXP.level);
  }
  else
  {
   Require(p.GetComponentsInChildren<EnergizedReload>().Length==1,"acquisition/Continue creates exactly one Energized owner");CombatClose(ThunderGenerator.SharedInstance.sizeMultiplier,1,"Continue rebuilds chain Mastery without old size bonus");
   Require((bool)WindField(Object.FindObjectOfType<ElementalRun>(),"Chain")&&p.GetComponent<AndroidWind>()!=null&&(bool)WindField(p.GetComponent<AndroidWind>(),"Mastery"),"mixed elemental owners reconstruct through native acquisitions");
  }
  float baseline=Bonus(p.stats[StatType.ReloadRate]);var target=ElementEnemy((Vector2)p.transform.position+Vector2.right*3);ThunderGenerator.SharedInstance.GenerateAt(target.gameObject,22);yield return null;yield return null;
  CombatClose(Bonus(p.stats[StatType.ReloadRate]),baseline+.01f,"restored Energized counts a current visible stun once, preserving other reload bonuses");
  BurnSystem.SharedInstance.Burn(target.gameObject,3);Require(Control(target).Stun>0&&Control(target).Fear>0,"burn and electrical status coexist without replacing either owner");
  FreezeSystem.SharedInstance.Freeze(target.gameObject);yield return null;Require(FreezeSystem.SharedInstance.IsFrozen(target.gameObject),"native ice remains an independent mixed-effect owner");
  yield return PoiCapture("element-mixed-status.png");ElementClear();yield return null;yield return null;CombatClose(Bonus(p.stats[StatType.ReloadRate]),baseline,"pool return removes transient reload count without losing Quick Hands");
  if(!restore){yield return DragonSuspend(game,"Complete mixed elemental acquisition checkpoint; transient electrical counts are not serialized");yield break;}
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;
  Require(p.GetComponentsInChildren<EnergizedReload>().Length==0&&p.GetComponent<AndroidWind>()==null&&!(bool)WindField(Object.FindObjectOfType<ElementalRun>(),"Chain"),"ordinary restart clears elemental upgrade owners and chains");CombatClose(ThunderGenerator.SharedInstance.damageMod.FlatBonus,0,"restart clears Mastery damage");
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete",mode+" mixed elemental current/prior Continue and restart");Application.Quit(0);
 }
 float ElementWidth=>(float)WindField(Object.FindObjectOfType<ElementalRun>(),"BodyWidth");
 ElementalControl Control(Health h)=>h.GetComponent<ElementalControl>();
 void ElementCall(object owner,string method,params object[] args)=>AccessTools.Method(owner.GetType(),method).Invoke(owner,args);
 Health ElementEnemy(Vector2 position,string pool="BrainMonster",string tag="Enemy")
 {var h=HitEffectTarget(pool,10000,tag);h.transform.position=position;return h;}
 void ElementClear()
 {foreach(var h in Object.FindObjectsOfType<Health>().Where(h=>h.gameObject.layer==(int)TagLayerUtil.Enemy).ToArray())ReturnFixtureActor(h.gameObject);}
 IEnumerator ElementLightning(GameController game)
 {
  var p=PlayerController.Instance;CameraZoom.Value=0;FeedbackCameras();yield return AndroidPick(game,"Energized",game.playerXP.level);yield return AndroidPick(game,"Frostbite",game.playerXP.level);
  yield return null;yield return null;float baseline=Bonus(p.stats[StatType.ReloadRate]);float r=ElementWidth*1.1f;Vector2 origin=(Vector2)p.transform.position+Vector2.right*3;
  var center=ElementEnemy(origin);var edge=ElementEnemy(origin+Vector2.up*r*.5f);var outside=ElementEnemy(origin+Vector2.up*r*1.05f);Physics2D.SyncTransforms();int ammo=p.ammo.amount;
  ThunderGenerator.SharedInstance.GenerateAt(center.gameObject,22);yield return null;yield return null;
  Require(center.HP==9978&&edge.HP==9986&&outside.HP==10000,"targeted strike has exact22/14/0 radial damage without a second physical hit");
  Require(!FreezeSystem.SharedInstance.IsFrozen(center.gameObject)&&Control(center).Stun>1.8f,"electrical stun remains separate from ice and doesn't trigger acquired Frostbite");
  CombatClose(Bonus(p.stats[StatType.ReloadRate]),baseline+.02f,"two distinct visible electrical stuns supply two-percent Energized");Require(p.ammo.amount==ammo,"Energized never refunds ammunition");
  edge.transform.position=p.transform.position+Vector3.up*100;yield return null;yield return null;CombatClose(Bonus(p.stats[StatType.ReloadRate]),baseline+.01f,"off-screen stunned enemy contributes no reload bonus");
  ThunderGenerator.SharedInstance.GenerateAt(center.gameObject,22);yield return null;Require(Control(center).Stun<=2&&Control(center).Stun>1.8f,"another strike refreshes rather than adds stun duration");
  game.pauseController.Pause();game.ChangeState<PauseState>();yield return null;float paused=Control(center).Stun;yield return null;yield return null;CombatClose(Control(center).Stun,paused,"native pause stops electrical status clock");yield return MenuPointer(game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;
  ElementClear();yield return null;yield return null;CombatClose(Bonus(p.stats[StatType.ReloadRate]),baseline,"death/pool return removes only Energized's owned contribution");
  var crowd=new System.Collections.Generic.List<Health>();for(int i=0;i<40;i++)crowd.Add(ElementEnemy(origin));ThunderGenerator.SharedInstance.GenerateAt((Vector3)origin,22);yield return null;yield return null;CombatClose(Bonus(p.stats[StatType.ReloadRate]),baseline+.4f,"forty visible enemies establish uncapped count rather than an arbitrary limit");ElementClear();yield return null;
  var boss=ElementEnemy(origin,tag:"EnemyChampion");ThunderGenerator.SharedInstance.GenerateAt(boss.gameObject,22);yield return null;Require(Control(boss).Stun>.3f&&Control(boss).Stun<=.4f,"Champion electrical control uses twenty-percent duration");ElementClear();
  yield return AndroidPick(game,"ElectroMastery",game.playerXP.level);yield return null;yield return null;
  CombatClose(ThunderGenerator.SharedInstance.sizeMultiplier,1,"Mastery replaces size multiplier with chaining");
  p.stats[StatType.Piercing].AddFlatBonus(2);p.stats[StatType.Bounce].AddFlatBonus(1);
  var targets=new System.Collections.Generic.List<Health>();origin=(Vector2)p.transform.position+Vector2.left*4;
  for(int i=0;i<8;i++)targets.Add(ElementEnemy(origin+Vector2.right*(ElementWidth*2*i)));
  ThunderGenerator.SharedInstance.GenerateAt(targets[0].gameObject,22);yield return null;yield return null;
  int[] expected={34,23,16,11,8,5,4,0};for(int i=0;i<targets.Count;i++)Require(10000-targets[i].HP==expected[i],"chain uses live Pierce+Bounce, per-jump falloff and one hit per enemy at index"+i);
  yield return PoiCapture("element-lightning-chain.png");
  foreach(bool style in new[]{false,true}){if(ColorfulStyle.Instance.Enabled!=style)ColorfulStyle.Instance.Toggle();ThunderGenerator.SharedInstance.GenerateAt(targets[0].gameObject,22);yield return PoiCapture("element-lightning-"+(style?"reborn":"original")+".png");}
  ElementClear();var data=p.gun.gunData;int originalPierce=data.piercing,originalBounce=data.bounce;
  Log("chain-weapon-bases",string.Join(";",Resources.FindObjectsOfTypeAll<GunData>().Select(d=>d.name+":"+d.piercing+"/"+d.bounce).Distinct()));
  try
  {
   data.piercing=1;data.bounce=1;var current=p.gun.GetProjectileRecipe();Require(current.piercing==3&&current.bounce==2,"native HUD recipe includes weapon-base and acquired Pierce/Bounce");targets.Clear();
   for(int i=0;i<10;i++)targets.Add(ElementEnemy(origin+Vector2.right*(ElementWidth*2*i)));
   ThunderGenerator.SharedInstance.GenerateAt(targets[0].gameObject,22);yield return null;yield return null;int[] combined={34,23,16,11,8,5,4,2,1,0};
   for(int i=0;i<targets.Count;i++)Require(10000-targets[i].HP==combined[i],"chain follows full HUD metrics including weapon-base values at "+i);
   data.piercing=999;this.AddObserver(ElementChainHit,ThunderGenerator.ThunderHitEvent);
   foreach(bool positional in new[]{false,true})foreach(int input in new[]{22,24})
   {
    ElementClear();while(Object.FindObjectsOfType<ElementalArc>().Length>0)yield return null;ElementChainNotices=0;targets.Clear();for(int i=0;i<14;i++)targets.Add(ElementEnemy(origin+Vector2.right*(ElementWidth*2*i)));
    if(positional)ThunderGenerator.SharedInstance.GenerateAt((Vector3)origin,input);else ThunderGenerator.SharedInstance.GenerateAt(targets[0].gameObject,input);
    yield return null;yield return null;int victims=input==22?10:11;
    Require((int)AccessTools.Field(typeof(AndroidLightning),"LastVictims").GetValue(null)==victims,"999-Pierce chain ends at damage floor rather than a fixed target cap: positional="+positional+" input="+input);
    Require(ElementChainNotices==victims&&Object.FindObjectsOfType<ElementalArc>().Length==victims-1,"below-one jump creates no lightning notification or visual arc");
    for(int i=0;i<targets.Count;i++)
    {
     int amount=Mathf.FloorToInt((input+12)*Mathf.Pow(.7f,i));Require(10000-targets[i].HP==(i<victims?amount:0),"999-Pierce chain retains unrounded falloff through its last damaging jump");
     Require(i<victims?Control(targets[i])!=null&&Control(targets[i]).Stun>0:Control(targets[i])==null||Control(targets[i]).Stun==0,"below-one chain targets receive no electrical stun");
    }
   }
  }
  finally{this.RemoveObserver(ElementChainHit,ThunderGenerator.ThunderHitEvent);data.piercing=originalPierce;data.bounce=originalBounce;}
  p.stats[StatType.Piercing].AddFlatBonus(-2);p.stats[StatType.Bounce].AddFlatBonus(-1);ElementClear();yield return null;
  var reused=ElementEnemy(origin);Require(Control(reused)==null||Control(reused).Stun==0,"pooled NPC reuse carries no electrical stun");ElementClear();
  yield return AndroidPick(game,"ElectroBug",game.playerXP.level);yield return null;yield return null;
  var bug=p.GetComponentInChildren<ThunderAttackSummon>();Require(bug!=null,"acquired Electro Bug creates its real summon");
  var victim=ElementEnemy((Vector2)p.transform.position+Vector2.right*3);yield return null;yield return null;int hp=victim.HP;ElementCall(bug,"Attack");yield return null;Require(victim.HP<hp&&Control(victim).Stun>0,"real Electro Bug producer reaches new shared lightning damage and stun");ElementClear();
  Log("element-balance","Controlled: three-target splash uses22/14/0 at0/.5R/1.05R; Mastery+2Pierce+1Bounce yields34/23/16/11/8/5/4 across7 targets. Forty visible stuns add40% ReloadRate. Not natural crowd encounter balance.");
 }
 IEnumerator ElementFire(GameController game)
 {
  var p=PlayerController.Instance;CameraZoom.Value=0;FeedbackCameras();Vector2 origin=(Vector2)p.transform.position+Vector2.right*3;float w=ElementWidth;
  var first=ElementEnemy(origin);var next=ElementEnemy(origin+Vector2.up*w*1.5f);var leaf=ElementEnemy(origin+Vector2.up*w*3);
  foreach(var h in new[]{first,next,leaf})h.GetComponent<MoveComponent2D>().enabled=false;
  BurnSystem.SharedInstance.Burn(first.gameObject,30);yield return null;var state=AndroidBurnState.Get(BurnSystem.SharedInstance);var burn=state.Find(first.gameObject);
  Require(Control(first).Fear>0&&Control(first).SlowFraction==.25f,"fresh ignition starts local panic and slow");float fear=Control(first).Fear;BurnSystem.SharedInstance.Burn(first.gameObject,3);Require(Control(first).Fear==fear&&burn.Damage==33,"reapplication accumulates burn without restarting panic");
  while(state.Find(next.gameObject)==null)yield return null;CombatClose(state.Find(next.gameObject).Damage,23.1f,"first spread retains70-percent underlying accumulated potency");
  while(state.Find(leaf.gameObject)==null)yield return null;CombatClose(state.Find(leaf.gameObject).Damage,16.17f,"second propagation keeps fractional potency until final damage rounding");
  Require(Control(first).Fear==0&&Control(first).Slow<=.1f,"panic and slow expire independently of an ongoing burn");
  ElementClear();yield return null;
  var low=ElementEnemy(origin);var spared=ElementEnemy(origin+Vector2.up*w);low.GetComponent<MoveComponent2D>().enabled=false;BurnSystem.SharedInstance.Burn(low.gameObject,1);var lowBurn=state.Find(low.gameObject);int hp=low.HP;while(low.HP==hp)yield return null;Require(state.Find(spared.gameObject)==null,"propagation stops when inherited final tick damage is below one");ElementClear();yield return null;
  yield return AndroidPick(game,"IntenseFlame",game.playerXP.level);var boosted=ElementEnemy(origin);var spread=ElementEnemy(origin+Vector2.up*w);boosted.GetComponent<MoveComponent2D>().enabled=false;spread.GetComponent<MoveComponent2D>().enabled=false;
  BurnSystem.SharedInstance.Burn(boosted.gameObject,30);while(Control(boosted).Fear>0)yield return null;var slowedMove=boosted.GetComponent<MoveComponent2D>();slowedMove.vector=Vector2.right*4;for(int i=0;i<2;i++){var v=(Vector2)AccessTools.Method(typeof(ElementalControl),"Movement").Invoke(Control(boosted),new object[]{slowedMove,.03f});CombatClose(v.x,3,"fire slow changes displacement without repeatedly multiplying stored velocity");}slowedMove.vector=Vector2.zero;
  while(state.Find(spread.gameObject)==null)yield return null;CombatClose(state.Find(spread.gameObject).Damage,21,"propagation stores potency before global burn multiplier");int old=spread.HP;while(spread.HP==old)yield return null;Require(old-spread.HP==28,"global35-percent modifier is applied once to inherited21 potency");ElementClear();yield return null;
  var boss=ElementEnemy(origin,tag:"EnemyChampion");BurnSystem.SharedInstance.Burn(boss.gameObject,3);yield return null;Require(Control(boss).Fear==0&&Control(boss).SlowFraction==.1f&&Control(boss).Slow<=.4f,"boss ignition slows briefly instead of forcing arena retreat");ElementClear();
  yield return AndroidPick(game,"FireStarter",game.playerXP.level);yield return null;
  var shotsBefore=new System.Collections.Generic.HashSet<int>(Object.FindObjectsOfType<Projectile>().Select(s=>s.GetInstanceID()));yield return MothShots(game,4);
  var ball=Object.FindObjectsOfType<Projectile>().First(s=>s.name.StartsWith("PF_Fireball")&&s.gameObject.activeSelf&&!shotsBefore.Contains(s.GetInstanceID()));
  var radius=(float)AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.ElementalFire"),"Radius").Invoke(null,new object[]{ball});
  var renderer=ball.GetComponent<SpriteRenderer>();CombatClose(radius,2*Mathf.Max(renderer.sprite.bounds.extents.x*Mathf.Abs(renderer.transform.lossyScale.x),renderer.sprite.bounds.extents.y*Mathf.Abs(renderer.transform.lossyScale.y)),"fire explosion radius follows current visible fireball radius times two");
  ball.GetComponent<MoveComponent2D>().enabled=false;ball.transform.position=origin;
  var direct=ElementEnemy(origin);var side=ElementEnemy(origin+Vector2.up*radius*.5f);var beyond=ElementEnemy(origin+Vector2.up*radius*1.1f);foreach(var h in new[]{direct,side,beyond})h.GetComponent<MoveComponent2D>().enabled=false;
  int full=(int)ball.damage;ElementCall(ball.GetComponent<AndroidProjectile>(),"Contact",direct.gameObject);
  Require(10000-direct.HP==full&&10000-side.HP==Mathf.FloorToInt(full*.675f)&&beyond.HP==10000,"actual fireball hit dispatch explodes once with center/edge/outside damage");Require(state.Find(direct.gameObject)!=null&&state.Find(side.gameObject)!=null&&!ball.gameObject.activeSelf,"blast victims ignite and projectile returns after its one explosion");
  yield return PoiCapture("element-fire-explosion.png");ElementClear();yield return null;
  ColorfulStyle.Instance.Toggle();yield return MothShots(game,4);var otherBall=Emitted.Last(e=>e.Name=="PF_Fireball").Shot;otherBall.transform.position=origin;
  var standalone=Object.Instantiate(ObjectPooler.SharedInstance.itemDictionary["BrainMonster"].objectToPool);var otherVictim=DurableEnemy(standalone,origin,false);otherVictim.GetComponent<MoveComponent2D>().enabled=false;ElementCall(otherBall.GetComponent<AndroidProjectile>(),"Contact",otherVictim.gameObject);Require(otherVictim.HP==10000-(int)otherBall.damage&&state.Find(standalone)!=null,"fireball direct impact also damages and ignites a non-pooled native actor");yield return PoiCapture("element-fire-other-style.png");Object.Destroy(standalone);ElementClear();ColorfulStyle.Instance.Toggle();
  yield return AndroidPick(game,"SoothingWarmth",game.playerXP.level);var healing=p.GetComponentInChildren<AndroidHealOnBurn>();healing.Chance=1;p.playerHealth.hp=p.playerHealth.maxHP-1;
  var h1=ElementEnemy(origin);var h2=ElementEnemy(origin+Vector2.up*w);h1.GetComponent<MoveComponent2D>().enabled=false;h2.GetComponent<MoveComponent2D>().enabled=false;BurnSystem.SharedInstance.Burn(h1.gameObject,3);p.playerHealth.hp=p.playerHealth.maxHP-1;
  while(state.Find(h2.gameObject)==null)yield return null;Require(p.playerHealth.hp==p.playerHealth.maxHP,"controlled successful Warmth roll also receives propagated ignition");healing.Chance=.0005f;ElementClear();
 }
 IEnumerator ElementWind(GameController game)
 {
  var p=PlayerController.Instance;CameraZoom.Value=0;FeedbackCameras();foreach(string name in new[]{"AeroMagic","Windborne","EyeOfTheStorm","AeroMastery"})yield return AndroidPick(game,name,game.playerXP.level);
  foreach(var producer in p.GetComponentsInChildren<AndroidGaleProducer>())producer.enabled=false;
  foreach(var existing in Object.FindObjectsOfType<AndroidWindActor>())ReturnFixtureActor(existing.gameObject);
  var owner=p.GetComponent<AndroidWind>();ElementCall(owner,"Cast");var gale=Object.FindObjectOfType<AndroidWindActor>();gale.GetComponent<MoveComponent2D>().enabled=false;gale.transform.position=p.transform.position+Vector3.right*4;
  var center=gale.transform.position;var eye=ElementEnemy((Vector2)center+Vector2.up*2,"EyeMonster");var control=(ElementalControl)AccessTools.Method(typeof(ElementalControl),"Get").Invoke(null,new object[]{eye.gameObject});
  ElementCall(control,"Disorient");yield return null;Require(control.Dizzy>1.3f,"Wind Mastery disorientation is a distinct1.5-second status");Vector2 remembered=p.transform.position;p.SetPosition(p.transform.position+Vector3.up*3);
  var perceived=(Vector2)AccessTools.Method(typeof(ElementalControl),"Perceived").Invoke(control,new object[]{(Vector2)p.transform.position});Require(perceived==remembered,"disorientation retains last known player position while the player moves");
  var ai=eye.GetComponent<AIComponent>();var aim=(Transform)AccessTools.Method(typeof(ElementalControl),"Aim").Invoke(control,new object[]{p.transform});Require((Vector2)aim.position==remembered,"native ranged special receives the owned perceived target");
  ElementAimed=null;ai.enabled=true;ai.frozen=false;ai.specialTimer=0;
  while(ElementAimed!=ai&&control.Dizzy>0)yield return null;
  Require(ElementAimed==ai&&ElementAimPoint==remembered,"actual AI update fires Eye special at the remembered position while disoriented");ai.enabled=false;AIController.SharedInstance.UnRegister(ai);
  var shot=ObjectPooler.SharedInstance.GetPooledObject("EyeMonsterProjectile");shot.transform.position=center+Vector3.up*.05f;var movement=shot.GetComponent<MoveComponent2D>();while(!MoveSystem2D.moveComponents.Contains(movement))yield return null;shot.transform.position=center+Vector3.up*.05f;movement.vector=Vector2.right*4;
  var wind=AccessTools.TypeByName("TwentyMinutes.Modernization.ElementalWind");AccessTools.Method(wind,"Steer").Invoke(null,new object[]{movement,.25f});CombatClose(movement.vector.magnitude,4,"wind bends enemy projectiles without slowing or damaging them");Require(movement.vector.y>0,"enemy projectile bends outward from Gale center");
  float angle=Vector2.SignedAngle(Vector2.right,movement.vector);CombatClose(angle,22.5f,"continuous wind deflection is bounded by90 degrees per second");
  var friendly=ProjectileFactory.SharedInstance.SpawnProjectile(p.gun.GetProjectileRecipe(),Vector2.right,shot.transform.position);var friendMove=friendly.GetComponent<MoveComponent2D>();friendMove.vector=Vector2.right*4;AccessTools.Method(wind,"Steer").Invoke(null,new object[]{friendMove,.25f});Require(friendMove.vector==Vector2.right*4,"friendly projectile trajectory is preserved");
  movement.vector=Vector2.right*4;AccessTools.Method(wind,"Pulse").Invoke(null,new object[]{gale});CombatClose(Vector2.SignedAngle(Vector2.right,movement.vector),60,"Eye pulse scatters an enemy bullet by at most60 degrees");
  yield return PoiCapture("element-wind-deflection.png");ReturnFixtureActor(shot);ReturnFixtureActor(friendly.gameObject);
  var enemy=ElementEnemy((Vector2)center+Vector2.left*.1f);var c=(ElementalControl)AccessTools.Method(typeof(ElementalControl),"Get").Invoke(null,new object[]{enemy.gameObject});var move=enemy.GetComponent<MoveComponent2D>();move.vector=Vector2.zero;
  Vector2 velocity=(Vector2)AccessTools.Method(typeof(ElementalControl),"Movement").Invoke(c,new object[]{move,.03f});Require(velocity.x>0,"Gale gently gathers a nearby NPC instead of ejecting every contact hit");
  ElementCall(c,"Push",Vector2.left,100f);velocity=(Vector2)AccessTools.Method(typeof(ElementalControl),"Movement").Invoke(c,new object[]{move,.03f});Require(velocity.x<0,"damage-based Eye impulse overcomes gathering and pushes outward");
  ElementCall(c,"Electrocute");Require((Vector2)AccessTools.Method(typeof(ElementalControl),"Movement").Invoke(c,new object[]{move,.03f})==Vector2.zero,"electrical stun takes priority over wind displacement");
  foreach(bool style in new[]{false,true}){if(ColorfulStyle.Instance.Enabled!=style)ColorfulStyle.Instance.Toggle();Plugin.SetField(gale,"AreaTimer",2f);ElementCall(gale,"Update");yield return PoiCapture("element-wind-"+(style?"reborn":"original")+".png");}
  ElementClear();ReturnFixtureActor(gale.gameObject);yield return null;
  var reused=ElementEnemy((Vector2)p.transform.position+Vector2.right*4,"EyeMonster");Require(Control(reused)==null||(Control(reused).Dizzy==0&&Control(reused).Stun==0),"pooled enemy has neither stale wind target nor electrical control");ElementClear();
 }
}
 [HarmonyPatch(typeof(flanne.AISpecials.AIShootSpecial),nameof(flanne.AISpecials.AIShootSpecial.Use))]
 class ElementAimProbe
 {
  static void Prefix(AIComponent ai,Transform __1)
  {if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")!="elements-wind")return;RuntimeChecks.ElementAimed=ai;RuntimeChecks.ElementAimPoint=__1.position;}
 }
 [HarmonyPatch(typeof(ThunderGenerator),nameof(ThunderGenerator.GenerateAt),new[]{typeof(GameObject),typeof(int)})]
 class ElementStrikeProbe
 {static void Prefix(){if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="elements-producers")RuntimeChecks.ElementStrikes++;}}
