using HarmonyLib;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization;

internal static class RunExitPreparation
{
 internal static void Prepare()
 {
  // ShootingState.Exit calls the source Gun.StopShooting adapter, including cursor/camera
  // projection and charged release. Scene unload disables those dependencies
  // before PlayerController.OnDisable. End that state at the owning transition
  // while the complete shooting path is still available.
  var player=PlayerController.Instance;
  if(player!=null&&player.CurrentState is flanne.Player.ShootingState)player.ChangeState<flanne.Player.IdleState>();
 }
}
[HarmonyPatch(typeof(TransitionToTitleState),nameof(TransitionToTitleState.Enter))]
class PrepareCompletedRunExit
{
 static void Prefix()=>RunExitPreparation.Prepare();
}
