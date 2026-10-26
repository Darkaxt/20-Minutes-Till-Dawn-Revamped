using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.UI;
namespace TwentyMinutes.Modernization;

// Campaigns have no native Unlockable artwork, although the same progression
// gates already block Play. Reuse the character lock image/material while
// retaining the campaign toggle and its controller description navigation.
public sealed class ModeLockVisual:MonoBehaviour
{
 Image lockIcon;TMP_Text label;Vector2 originalPosition;
 public bool Locked=>lockIcon!=null&&lockIcon.gameObject.activeSelf;
 internal static void Refresh(GameModeMenu menu)
 {
  var native=NativeSelection.Unlocks(Progression.Title.characterUnlocker).First();
  var source=(Image)AccessTools.Field(typeof(Unlockable),"lockSprite").GetValue(native);
  foreach(var toggle in ChallengeMode.Toggles(menu))
  {
   var view=toggle.GetComponent<ModeLockVisual>();if(view==null)
   {
    view=toggle.gameObject.AddComponent<ModeLockVisual>();
    view.label=toggle.GetComponentInChildren<TMP_Text>(true);view.originalPosition=view.label.rectTransform.anchoredPosition;
    var icon=new GameObject("NativeModeLock",typeof(RectTransform),typeof(Image));StatsOverlay.Rect(icon,toggle.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,12),new Vector2(18,18));
    view.lockIcon=icon.GetComponent<Image>();view.lockIcon.sprite=source.sprite;view.lockIcon.color=source.color;view.lockIcon.material=source.material;view.lockIcon.preserveAspect=true;view.lockIcon.raycastTarget=false;
   }
   bool locked=!Progression.Allowed(toggle.GetComponent<MapEntry>().data);
   // Campaign targetGraphic is TMP text, not the character's sprite. Its font
   // material remains native; only the lock image takes the source image style.
   view.lockIcon.gameObject.SetActive(locked);
   view.label.rectTransform.anchoredPosition=view.originalPosition+(locked?new Vector2(0,-10):Vector2.zero);
  }
  ModeClearRecords.Refresh();
 }
}
