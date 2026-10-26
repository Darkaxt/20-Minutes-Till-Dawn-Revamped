using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.Player;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator StillnessMove(PlayerController p,string boundary)
 {
  Require(p.finalMoveSpeed>0&&p.moveSpeedMultiplier>0,"Stillness preserves ordinary movement "+boundary);
  Vector3 start=p.transform.position;
  for(int i=0;i<5;i++)yield return new WaitForFixedUpdate();
  Require(p.transform.position.x>start.x+.01f,"real held movement advances player "+boundary);
 }
 IEnumerator CheckStillness(string mode)
 {
  SetPhase("stillness-shooting-only");
  if(mode=="stillness-fresh")
  {
   RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
  }
  else yield return CombatBuffContinue();
  var game=Object.FindObjectOfType<GameController>();var p=PlayerController.Instance;StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  if(mode=="stillness-fresh")
  {
   float damage=p.gun.damage,size=p.gun.GetProjectileRecipe().size;int count=p.gun.numOfProjectiles;
   yield return NativeTomePick(game,false,"TomeOfStillness");
   CombatClose(p.gun.damage,damage*2,"Stillness retains100-percent bullet damage");CombatClose(p.gun.GetProjectileRecipe().size,size*2,"Stillness retains100-percent bullet size");Require(p.gun.numOfProjectiles==count+1,"Stillness retains one extra projectile");
  }
  Require(StatsOverlay.Instance.Collected.Values.Any(e=>RunResume.Key(e.Power)=="tome_of_stillness_name"),"actual acquired Stillness exists after pick/Continue");
  var tome=Resources.FindObjectsOfTypeAll<Powerup>().First(t=>t.name=="TomeOfStillness");
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  while(!(p.CurrentState is IdleState))yield return null;
  // The actual historical checkpoint contains a boss pickup restored at the
  // origin. Keep this movement-only exercise away from its collection trigger.
  p.SetPosition(p.transform.position+new Vector3(100,100,0));Physics2D.SyncTransforms();p.ammo.Reload();InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState(Key.D));while(p.moveVec.x<.9f)yield return null;
  yield return StillnessMove(p,"before firing");
  Require(tome.description.Contains("while shooting"),"Stillness description explicitly scopes movement penalty to shooting");
  var aim=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*5);int shots=0;UnityEngine.Events.UnityAction shot=()=>shots++;p.gun.OnShoot.AddListener(shot);
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(shots==0||!(p.CurrentState is ShootingState))yield return null;
  CombatClose(p.moveSpeedMultiplier,0,"Stillness blocks shooting movement only");Vector3 stopped=p.transform.position;
  for(int i=0;i<3;i++)yield return new WaitForFixedUpdate();
  Require(Vector3.Distance(stopped,p.transform.position)<.001f,"held movement cannot move player while firing");
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});while(!(p.CurrentState is ReloadState))yield return null;
  yield return StillnessMove(p,"after release during reload");while(!(p.CurrentState is IdleState))yield return null;
  yield return StillnessMove(p,"after reload");
  p.gun.OnShoot.RemoveListener(shot);InputSystem.QueueStateEvent(fixtureKeyboard,new KeyboardState());while(p.moveVec!=Vector3.zero)yield return null;
  ReturnPlayerShots();StatsOverlay.Instance.Refresh();yield return Capture("stillness-movement-restored.png");
  if(mode!="stillness-continue"){yield return DragonSuspend(game,"Stillness keeps its bonuses, restricts only shooting and restores real movement on release/reload; corrected checkpoint");yield break;}
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Stillness native shooting/release/reload and fresh-process acquisition replay preserve the corrected movement channels");Application.Quit(0);
 }
}
