using UnityEngine;
namespace TwentyMinutes.Modernization;

// Attach only to gameplay HUD roots. Children inherit one factor while their
// native colors, animations and visibility owners remain unchanged.
public sealed class GameplayHudOpacity:MonoBehaviour
{
 internal static void WrapNative(RectTransform target)
 {
  // HUDPanel already has a native CanvasGroup controlling show/hide/fades.
  // A full-parent wrapper multiplies that alpha without taking its ownership.
  var parent=(RectTransform)target.parent;int index=target.GetSiblingIndex();
  var root=new GameObject("GameplayHudOpacity",typeof(RectTransform));var rect=root.GetComponent<RectTransform>();
  rect.SetParent(parent,false);rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=parent.pivot;rect.offsetMin=rect.offsetMax=Vector2.zero;rect.SetSiblingIndex(index);
  target.SetParent(rect,false);root.AddComponent<GameplayHudOpacity>();
 }
 CanvasGroup group;
 void Awake(){group=gameObject.AddComponent<CanvasGroup>();Apply();}
 void LateUpdate()=>Apply();
 void Apply(){group.alpha=Plugin.Instance.HudOpacity.Value/100f;}
}
