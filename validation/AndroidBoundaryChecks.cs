using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.AISpecials;
using flanne.RuneSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool BoundaryMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-boundaries-");
 internal static int BoundaryLineSteps,BoundaryEnters,BoundaryStays,BoundaryStayRenewals,BoundaryShieldContacts,BoundaryLaserUses;
 internal static GameObject BoundaryContactActor;
 internal static bool BoundaryObserveStay,BoundaryObserveMove;
 internal sealed class BoundaryMove{internal Vector3 Before,Expected;internal bool Hit;internal float Allowed;}
 internal static readonly List<BoundaryMove> BoundaryMoves=new List<BoundaryMove>();
 static BossArenaLifetime[] BoundaryArenas()=>Object.FindObjectsOfType<BossArenaLifetime>().Where(a=>a.owner!=null&&a.gameObject.activeInHierarchy).ToArray();
 static List<PolygonCollider2D> BoundarySegments(LineCollider line)=>(List<PolygonCollider2D>)WindField(line.GetComponent<AndroidLineSegments>(),"segments");
 static GameObject[] BoundaryTargets(LineController line)=>(GameObject[])WindField(line,"targets");
 static ProjectileShieldRune BoundaryShield()=>PlayerController.Instance.GetComponentInChildren<ProjectileShieldRune>();
 static AndroidPlayerCollision BoundaryPlayer()=>PlayerController.Instance.playerHealth.GetComponent<AndroidPlayerCollision>();
 static BoolToggle BoundaryCollisionToggle=>(BoolToggle)WindField(BoundaryPlayer(),"EnableCollision");
 static void BoundaryPauseWaves()
 {Object.FindObjectOfType<HordeSpawner>().enabled=false;Object.FindObjectOfType<AndroidEndlessRun>().enabled=false;GameTimer.SharedInstance.Stop();foreach(var ai in Object.FindObjectsOfType<AIComponent>()){ai.frozen=true;ai.moveComponent.vector=Vector2.zero;}}
 IEnumerator BoundaryLineFrame(){int prior=BoundaryLineSteps;while(BoundaryLineSteps==prior)yield return null;}
 IEnumerator BoundarySpawnBoss(int index)
 {
  SetPhase("boundary-scheduler-boss-"+index);
  int prior=BoundaryArenas().Length;var run=Object.FindObjectOfType<AndroidEndlessRun>();WorldTime(SelectedMap.MapData.bossSpawns[index].timeToSpawn);run.enabled=true;
  while(BoundaryArenas().Length==prior)yield return null;BoundaryPauseWaves();
  Require(BoundaryArenas().Length==prior+1,"actual source scheduler creates one boss-owned pooled arena at its native map boundary");
  yield return BoundaryLineFrame();
 }
 IEnumerator BoundaryGeometry(BossArenaLifetime arena,bool initial)
 {
  SetPhase("boundary-line-geometry");
  var line=arena.GetComponentInChildren<LineController>();var collider=line.GetComponent<LineCollider>();
  while(line.GetComponent<AndroidLineSegments>()==null||BoundarySegments(collider).Count!=4)yield return null;
  var targets=BoundaryTargets(line);var points=line.GetPositions();var again=line.GetPositions();
  Require(ReferenceEquals(points,again)&&points.Length==4,"actual line reads reuse one source position buffer");
  var original=new[]{new Vector3(15,-10),new Vector3(-15,-10),new Vector3(-15,10),new Vector3(15,10)};
  if(initial)for(int i=0;i<4;i++)CombatClose(Vector3.Distance(targets[i].transform.localPosition,original[i]),0,"native arena source OnEnable corner "+i);
  var shapes=BoundarySegments(collider);float width=line.GetWidth();
  Require(!line.GetComponent<PolygonCollider2D>().enabled,"source separate segments replace the native renderer-root collider");
  for(int i=0;i<4;i++)
  {
   var shape=shapes[i];Vector2 first=points[i],second=points[(i+1)%4];float angle=Mathf.Atan2(second.y-first.y,second.x-first.x);Vector2 offset=new Vector2(-width*Mathf.Sin(angle),width*Mathf.Cos(angle));
   var expected=new[]{first+offset,first-offset,second-offset,second+offset};var actual=shape.GetPath(0);
   Require(shape.GetComponent<AndroidElectroEdge>()!=null&&shape.attachedRigidbody.bodyType==RigidbodyType2D.Kinematic&&shape.attachedRigidbody.useFullKinematicContacts,"each actual segment owns source electron identity and full-contact kinematic physics");
   for(int j=0;j<4;j++)CombatClose(Vector2.Distance(actual[j],expected[j]),0,"source full-width finite world-space quad "+i+"/"+j);
   CombatClose(shape.transform.position.sqrMagnitude,0,"actual separate line quad is positioned at world origin");
  }
  Plugin.SetField(collider,"loop",false);yield return BoundaryLineFrame();Require(BoundarySegments(collider).Count==4&&shapes[3].gameObject.activeSelf,"source cached closing segment remains after the explicit loop=false operand");Plugin.SetField(collider,"loop",true);
  var renderer=line.GetComponent<LineRenderer>();var frames=new HashSet<Texture>();while(frames.Count<6)
  {
   // A persisted boss reward can physically reach the restored player after
   // Continue's Combat transition. Its real menu pauses scaled animation.
   var game=Object.FindObjectOfType<GameController>();
   if(game.CurrentState is DevilDealState&&game.devilDealLeaveButton.IsInteractable())
   {Log("fixture-restored-reward","Decline the actually collected saved devil reward through its native Leave button before observing scaled arena animation.");Click(game.devilDealLeaveButton);}
   frames.Add(renderer.material.mainTexture);yield return null;
  }
  Require(frames.Count==6,"actual native Start-owned arena animation plays all six original frames");
  Log("boundary-geometry","Actual scheduler arena has four full-width finite source quads, retained closing segment, cached positions and six native animation frames");
 }
 IEnumerator BoundaryMovement(GameController game,LineController line)
 {
  SetPhase("boundary-native-keyboard");
  var p=PlayerController.Instance;var points=line.GetPositions();Vector3 middle=(points[3]+points[0])*.5f;float width=line.GetWidth();var center=line.transform.position;
  BoundaryShield().GetComponent<Collider2D>().enabled=false;BoundaryCollisionToggle.Flip();Require(!BoundaryCollisionToggle.value,"fixture disables contact callbacks only while isolating real keyboard/raycast movement");
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  BoundaryMoves.Clear();BoundaryObserveMove=true;p.SetPosition(middle-Vector3.right*(width+.35f+.05f));Physics2D.SyncTransforms();InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.D));
  while(!BoundaryMoves.Any(m=>m.Hit&&m.Allowed>=0))yield return null;InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());while(p.moveVec!=Vector3.zero)yield return null;
  Require(Physics2D.queriesStartInColliders,"native raycast setting exposes the source inside-collider subtraction branch");
  BoundaryMoves.Clear();p.SetPosition(middle);Physics2D.SyncTransforms();InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.D));
  while(!BoundaryMoves.Any(m=>m.Hit&&m.Allowed<0))yield return null;InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());while(p.moveVec!=Vector3.zero)yield return null;BoundaryObserveMove=false;
  Require(BoundaryMoves.Any(m=>m.Allowed<0),"actual native movement preserves source negative displacement when the first boundary hit is inside the circle");
  p.SetPosition(center);Physics2D.SyncTransforms();BoundaryCollisionToggle.UnFlip();Require(BoundaryCollisionToggle.value,"fixture restores source player collision after native movement observations");
 }
 IEnumerator BoundaryElectricContact(LineController line)
 {
  SetPhase("boundary-electric-enter-stay");
  var p=PlayerController.Instance;var health=p.playerHealth;var renderer=line.GetComponent<LineRenderer>();float width=renderer.startWidth;var center=p.transform.position;
  var edge=BoundarySegments(line.GetComponent<LineCollider>())[3];var playerCollider=health.GetComponent<CircleCollider2D>();
  p.SetPosition(center);Physics2D.SyncTransforms();while(playerCollider.IsTouching(edge))yield return new WaitForFixedUpdate();
  var targets=BoundaryTargets(line);var originalPoses=targets.Select(t=>t.transform.localPosition).ToArray();
  for(int i=0;i<targets.Length;i++){var point=targets[i].transform.localPosition;point.y=point.y<0?-100:100;targets[i].transform.localPosition=point;}
  renderer.startWidth=10;yield return BoundaryLineFrame();var points=line.GetPositions();Vector3 middle=(points[3]+points[0])*.5f;
  health.isInvincible.UnFlip();Require(!health.isInvincible.value,"actual electric contact has no fixture health protection");
  int hp=health.hp,enters=BoundaryEnters,renewals=BoundaryStayRenewals;BoundaryObserveStay=true;BoundaryContactActor=edge.gameObject;p.SetPosition(middle);Physics2D.SyncTransforms();
  while(BoundaryEnters==enters||BoundaryStayRenewals==renewals)yield return null;
  Require(health.hp==hp-1&&BoundaryStays>0&&edge.gameObject.activeSelf,"actual electron Enter deals one heart, Stay refreshes knockback/invulnerability without another HP change or releasing the edge");
  BoundaryObserveStay=false;BoundaryContactActor=null;p.SetPosition(center);Physics2D.SyncTransforms();renderer.startWidth=width;for(int i=0;i<targets.Length;i++)targets[i].transform.localPosition=originalPoses[i];
  while((bool)WindField(BoundaryPlayer(),"OnHitInvicibility"))yield return null;health.isInvincible.Flip();yield return BoundaryLineFrame();
  Log("boundary-electric-contact","Actual thick-strip geometry isolates persistent Enter/Stay; source one-heart damage and repeated invulnerability retain the active edge");
 }
 IEnumerator BoundaryWindShield(LineController line)
 {
  SetPhase("boundary-wind-shield-contact");
  var p=PlayerController.Instance;var center=p.transform.position;var edge=BoundarySegments(line.GetComponent<LineCollider>())[3];var points=line.GetPositions();Vector3 middle=(points[3]+points[0])*.5f;
  var shield=BoundaryShield();var hitbox=shield.GetComponent<Collider2D>();int contacts=BoundaryShieldContacts;BoundaryCollisionToggle.Flip();hitbox.enabled=true;
  p.SetPosition(middle);Physics2D.SyncTransforms();while(BoundaryShieldContacts==contacts)yield return null;
  Require(!hitbox.enabled,"actual native Wind Shield contact releases an electric edge and enters its native cooldown");
  p.SetPosition(center);Physics2D.SyncTransforms();BoundaryCollisionToggle.UnFlip();yield return BoundaryLineFrame();yield return BoundaryLineFrame();
  Require(edge.gameObject.activeSelf&&edge.enabled,"real subsequent geometry updates reactivate the cached edge released by Wind Shield");
  while(!hitbox.enabled)yield return null;
  Require(((SpriteRenderer)WindField(shield,"sprite")).enabled,"actual rank-three Wind Shield cooldown restores its collider and sprite");
  Log("boundary-wind-shield","Actual Wind Shield releases the edge; the next geometry rebuild reactivates it and native cooldown completes");
 }
 IEnumerator BoundaryArenaReuse(BossArenaLifetime arena)
 {
  SetPhase("boundary-owned-arena-reuse");
  var line=arena.GetComponentInChildren<LineController>();var oldShapes=BoundarySegments(line.GetComponent<LineCollider>()).ToArray();var renderer=line.GetComponent<LineRenderer>();
  var boss=arena.owner;while(boss.isInvincible.value)yield return null;boss.TakeDamage(DamageType.Bullet,boss.maxHP);while(arena.gameObject.activeSelf)yield return null;
  var texture=renderer.material.mainTexture;
  Require(boss.isDead,"real boss death releases its owned arena through source Update polling");
  yield return BoundarySpawnBoss(1);var restored=BoundaryArenas().Single();Require(restored==arena&&restored.owner!=boss,"actual later scheduler boss reuses the source arena and assigns its exact new owner");
  yield return BoundaryLineFrame();Require(oldShapes.All(s=>s.gameObject.activeSelf&&s.enabled)&&BoundarySegments(line.GetComponent<LineCollider>()).SequenceEqual(oldShapes),"actual arena pool reuse rebuilds the same cached segments with every collider active");
  var animator=line.GetComponent<LineAnimator>();float interval=(float)WindField(animator,"secPerFrame");var textures=(Texture[])WindField(animator,"textures");float until=Time.time+interval*textures.Length;
  while(Time.time<until){Require(renderer.material.mainTexture==texture,"pooled arena does not invent a restart for native Start-only animation");yield return null;}
  Log("boundary-arena-reuse","Source owner polling/pool reuse/new owner, corrected cached collider activation and stopped Start-only animation all observed");
 }
 IEnumerator BoundaryLaser(GameController game)
 {
  SetPhase("boundary-native-laser");
  var p=PlayerController.Instance;var shield=BoundaryShield();shield.GetComponent<Collider2D>().enabled=false;int before=BoundaryLaserUses;var actor=EnemyActor("Shoggoth",Vector3.right*2);var ai=actor.GetComponent<AIComponent>();
  CombatClose(ai.specialCooldown,4,"actual Shoggoth imports source four-second AI cooldown");
  while(BoundaryLaserUses==before)yield return null;
  Collider2D laser=null;while(laser==null){laser=actor.GetComponentsInChildren<Collider2D>(true).FirstOrDefault(c=>c.gameObject.name.Contains("PF_Laser")&&c.enabled&&c.gameObject.activeInHierarchy);yield return null;}
  int hp=p.playerHealth.hp,contacts=BoundaryEnters;BoundaryContactActor=laser.gameObject;laser.transform.position+=p.playerHealth.transform.position-laser.bounds.center;Physics2D.SyncTransforms();
  while(BoundaryEnters==contacts)yield return null;BoundaryContactActor=null;
  Require(p.playerHealth.hp==hp&&laser.gameObject.activeInHierarchy&&!laser.enabled,"actual protected laser contact disables only its collider, retaining the original laser object");
  Log("boundary-laser","Real Shoggoth special/windup/active laser/player contact uses the source collider-only disposal branch");
  yield return DragonSuspend(game,"Real source arena/line/native keyboard/electron Stay/Wind Shield/pool reuse and Shoggoth laser contact; active arena/laser scene exit source checkpoint");
 }
 IEnumerator CheckAndroidBoundaries(bool continuation)
 {
  Log("fixture-precondition","Actual source scheduler bosses/pooled arenas, native line Update/animation, keyboard MovePlayer, physical electron/Wind Shield and real Shoggoth laser producer. Native rank-three Wind Shield selected explicitly. Scheduler/time boundaries, geometry/contact positions, temporary width10, movement-only contact gating and final boss damage are declared operands. No natural full-run/difficulty/frequency/device claim.");
  GameController game;
  if(continuation)
  {
   yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();BoundaryPauseWaves();PlayerController.Instance.playerHealth.isInvincible.Flip();
   var arena=BoundaryArenas().Single();while(arena.GetComponentInChildren<AndroidLineSegments>()==null)yield return null;
   Require(BoundaryShield()!=null&&BoundaryShield().GetComponent<Collider2D>().enabled,"exact source Continue rebuilds the selected Wind Shield with its unsaved cooldown ready");
   Require(BoundarySegments(arena.GetComponentInChildren<LineCollider>()).Count==4&&BoundarySegments(arena.GetComponentInChildren<LineCollider>()).All(s=>s.gameObject.activeSelf),"restored real scheduled encounter owns fresh source segments without omitted Wind Shield damage");
   Require(!Object.FindObjectsOfType<Collider2D>().Any(c=>c.gameObject.name.Contains("PF_Laser")&&c.gameObject.activeInHierarchy),"source-unsaved laser/AI phase is omitted on exact fresh-process Continue");yield return BoundaryGeometry(arena,true);
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<RuneData>());game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();
   Require(BoundaryArenas().Length==0&&Object.FindObjectsOfType<AndroidElectroEdge>().Length==0&&BoundaryShield()==null,"ordinary native restart clears arenas, source cached edges and Wind Shield owner");yield return CheckRevolver(game,PlayerController.Instance,"android-boundaries-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Native line/electric/laser exact source Continue and ordinary restart");Application.Quit(0);yield break;
  }
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>
  {var original=Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.runePrefab!=null&&r.runePrefab.GetComponent<ProjectileShieldRune>()!=null);var copy=Object.Instantiate(original);copy.name=original.name;copy.level=3;Loadout.RuneSelection=new List<RuneData>{copy};});game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();
  Require(BoundaryShield()!=null,"actual native loadout attaches the selected Wind Shield");yield return BoundarySpawnBoss(0);var first=BoundaryArenas().Single();yield return BoundaryGeometry(first,true);
  var nativeLine=first.GetComponentInChildren<LineController>();yield return BoundaryMovement(game,nativeLine);yield return BoundaryElectricContact(nativeLine);yield return BoundaryWindShield(nativeLine);yield return BoundaryArenaReuse(first);yield return BoundaryLaser(game);
 }
}
[HarmonyPatch(typeof(LineCollider),"LateUpdate")]
class BoundaryLineProbe{static void Postfix(){if(RuntimeChecks.BoundaryMode)RuntimeChecks.BoundaryLineSteps++;}}
[HarmonyPatch(typeof(AndroidPlayerCollision),"Enter")]
class BoundaryEnterProbe
{[HarmonyPriority(Priority.First)]static void Prefix(Collision2D collision){if(RuntimeChecks.BoundaryMode&&collision.gameObject==RuntimeChecks.BoundaryContactActor){RuntimeChecks.SeedFirstRoll(.99f,1);RuntimeChecks.BoundaryEnters++;RuntimeChecks.Log("boundary-enter",new{actor=collision.gameObject.name,hp=PlayerController.Instance.playerHealth.hp,protectedHit=PlayerController.Instance.playerHealth.isInvincible.value});}}}
[HarmonyPatch(typeof(AndroidPlayerCollision),"OnCollisionStay2D")]
class BoundaryStayProbe
{
 [HarmonyPriority(Priority.First)]static void Prefix(Collision2D collision,out Vector2 __state){var hp=PlayerController.Instance.playerHealth;__state=new Vector2(hp.hp,hp.isInvincible.value?1:0);}
 static void Postfix(Collision2D collision,Vector2 __state)
 {
  if(!RuntimeChecks.BoundaryObserveStay||collision.gameObject!=RuntimeChecks.BoundaryContactActor)return;
  var hp=PlayerController.Instance.playerHealth;RuntimeChecks.EnemyCheck(hp.hp==__state.x,"actual electric Stay does not change HP");RuntimeChecks.BoundaryStays++;
  if(__state.y==0&&hp.isInvincible.value){RuntimeChecks.BoundaryStayRenewals++;RuntimeChecks.Log("boundary-stay-renewal",new{hp=hp.hp,enterCount=RuntimeChecks.BoundaryEnters,stayCount=RuntimeChecks.BoundaryStays});}
 }
}
[HarmonyPatch(typeof(ProjectileShieldRune),"OnCollisionEnter2D")]
class BoundaryShieldProbe{static void Postfix(Collision2D collision){if(RuntimeChecks.BoundaryMode&&collision.gameObject.GetComponent<AndroidElectroEdge>()!=null)RuntimeChecks.BoundaryShieldContacts++;}}
[HarmonyPatch(typeof(AILasersSpecial),nameof(AILasersSpecial.Use))]
class BoundaryLaserProbe{static void Postfix(AIComponent ai){if(!RuntimeChecks.BoundaryMode)return;RuntimeChecks.BoundaryLaserUses++;ai.frozen=true;ai.moveComponent.vector=Vector2.zero;}}
[HarmonyPatch(typeof(PlayerController),"MovePlayer")]
class BoundaryMoveProbe
{
 [HarmonyPriority(Priority.First)]static void Prefix(PlayerController __instance,out RuntimeChecks.BoundaryMove __state)
 {
  __state=null;if(!RuntimeChecks.BoundaryObserveMove||__instance.moveVec==Vector3.zero||__instance.disableMove.value)return;
  var move=__instance.moveVec;Vector2 velocity=(Vector2)move*__instance.moveSpeedMultiplier*__instance.finalMoveSpeed;float distance=(velocity*Time.deltaTime).magnitude;
  var hit=Physics2D.Raycast(__instance.transform.position,move,distance+.35f,LayerMask.GetMask("EnemyProjectile","Wall"));bool boundary=hit.collider!=null&&(hit.collider.GetComponent<AndroidElectroEdge>()!=null||hit.collider.gameObject.layer==LayerMask.NameToLayer("Wall"));float allowed=boundary?Vector3.Distance(__instance.transform.position,hit.point)-.35f:distance;
  Vector2 delta=boundary?(Vector2)move.normalized*allowed:velocity*Time.deltaTime;__state=new RuntimeChecks.BoundaryMove{Before=__instance.transform.position,Expected=__instance.transform.position+__instance.transform.TransformDirection(delta),Hit=boundary,Allowed=allowed};
 }
 static void Postfix(PlayerController __instance,RuntimeChecks.BoundaryMove __state){if(__state==null)return;RuntimeChecks.EnemyClose(Vector3.Distance(__instance.transform.position,__state.Expected),0,"actual keyboard MovePlayer uses the source first-hit circle subtraction without clamping");RuntimeChecks.BoundaryMoves.Add(__state);}
}
