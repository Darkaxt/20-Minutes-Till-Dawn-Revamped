using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.TitleScreen;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckReadmeScreenshots(TitleScreenController title)
 {
  SetPhase("readme-selector-captures");RunResume.Invalidate();var pad=InputSystem.AddDevice<Gamepad>("ReadmeScreenshotController");
  while(!Active(title.mainMenu)||!title.mainMenu.interactable||title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
  var bulwark=Characters.Data["Dimond_Portrait"];var diamond=Progression.Gates.Single(g=>g.Id=="character:Diamond");
  var earned=Progression.State.earned.ToArray();
  Progression.State.earned.Add("character:"+bulwark.name);Progression.State.earned.RemoveAll(id=>id==diamond.Id);Progression.Refresh();
  yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);yield return HeaderReady(title,title.characterMenu);
  Screen.SetResolution(1280,720,false);yield return WaitDisplay(1280,720);
  if(!ColorfulStyle.Instance.Enabled)ColorfulStyle.Instance.Toggle();
  int index=Characters.Slots["Dimond_Portrait"];yield return SelectorMove(pad,title.characterMenu,Entries(title.characterMenu)[index].button);
  RequireCharacterPreview(title,index);HeaderContract(title);yield return Capture("android-bulwark.png");
  index=Characters.Items(title.characterMenu).FindIndex(x=>x.data==diamond.Data);yield return SelectorMove(pad,title.characterMenu,Entries(title.characterMenu)[index].button);
  RequireCharacterPreview(title,index);HeaderContract(title);
  Require(SelectorText().Contains(diamond.Goal)&&!SelectorText().Contains("Last attempt:"),"current Diamond preview shows its concise milestone condition");
  yield return Capture("unlock-diamond.png");
  Progression.State.earned.Clear();Progression.State.earned.AddRange(earned);Progression.Refresh();Progression.Save();
  SetPhase("PASS");Log("complete","Unretouched native README selectors from the published production DLL; isolated Bulwark ownership and locked Diamond fixture restored");Application.Quit(0);
 }
}
