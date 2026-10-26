using System.Linq;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator InspectNativeLock()
 {
  SetPhase("native-lock-inspection");RunResume.Invalidate();var title=Object.FindObjectOfType<TitleScreenController>();
  foreach(var owner in new[]{title.characterUnlocker,title.gunUnlocker})
  {
   var unlock=NativeSelection.Unlocks(owner).First();unlock.Lock();
   var lockImage=(Image)WindField(unlock,"lockSprite");var target=(Image)WindField(unlock,"targetSprite");var material=(Material)WindField(unlock,"lockMaterial");
   Log("native-lock-owner",unlock.name+" lock="+NativeSelection.Relative(unlock.transform,lockImage.transform)+" size="+lockImage.rectTransform.rect+" sprite="+lockImage.sprite.name+" spriteRect="+lockImage.sprite.rect+" texture="+lockImage.sprite.texture.name+" material="+material.name+" shader="+material.shader.name);
   foreach(var image in unlock.GetComponentsInChildren<Image>(true))Log("native-lock-hierarchy",NativeSelection.Relative(unlock.transform,image.transform)+" active="+image.gameObject.activeSelf+" enabled="+image.enabled+" sprite="+image.sprite?.name+" size="+image.rectTransform.rect+" position="+image.rectTransform.anchoredPosition+" rotation="+image.transform.localEulerAngles+" material="+image.material?.name);
   foreach(string property in material.GetTexturePropertyNames())Log("native-lock-texture",material.name+"/"+property+"="+material.GetTexture(property)?.name);
   Log("mode-graphics",string.Join(";",ChallengeMode.Toggles(title.modeSelectMenu)[0].GetComponentsInChildren<Graphic>(true).Select(g=>g.name+":"+g.GetType().Name)));
  }
  Click(Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu)||title.characterMenu.GetComponent<CanvasGroup>().alpha<.99f)yield return null;
  yield return Capture("native-character-full-lock.png");Click(Entries(title.characterMenu)[0].button);while(!Active(title.gunMenu)||title.gunMenu.GetComponent<CanvasGroup>().alpha<.99f)yield return null;
  yield return Capture("native-weapon-full-lock.png");SetPhase("PASS");Log("complete","Bounded native lock hierarchy/material/render inspection; no acquisition or gameplay change");Application.Quit(0);
 }
}
