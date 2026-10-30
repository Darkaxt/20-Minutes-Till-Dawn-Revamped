using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace TwentyMinutes.Modernization;

// Presentation only: the EventSystem remains the single selection owner.
public sealed class MenuFocusArrow:MonoBehaviour
{
 public TMP_Text Arrow{get;private set;}
 public GameObject Target{get;private set;}
 TMP_FontAsset font;
 void LateUpdate()
 {
  var selected=EventSystem.current?.currentSelectedGameObject;var control=selected==null?null:selected.GetComponent<Selectable>();
  bool visible=control!=null&&control.gameObject.activeInHierarchy&&control.IsInteractable()&&control.GetComponentInParent<Canvas>()!=null&&control.GetComponentsInParent<CanvasGroup>().All(g=>g.alpha>.01f&&g.interactable);
  Target=visible?selected:null;
  if(!visible){if(Arrow!=null)Arrow.gameObject.SetActive(false);return;}
  var label=control.GetComponentsInChildren<TMP_Text>().FirstOrDefault(t=>t!=Arrow&&t.gameObject.activeInHierarchy&&t.text.Length>1);
  if(label!=null)font=label.font;
  if(font==null)font=Resources.FindObjectsOfTypeAll<TMP_Text>().First(t=>t.font!=null&&t.gameObject.scene.IsValid()).font;
  var parent=label==null?(RectTransform)control.transform:label.rectTransform;
  // Inherit the native canvas/label coordinate system and destruction boundary.
  // World-camera menu units are not overlay pixels; no screen conversion here.
  if(Arrow==null){Arrow=StatsOverlay.Text(parent,"FocusChevron",font,">",18,Vector2.zero,new Vector2(24,32));Arrow.alignment=TextAlignmentOptions.Center;Arrow.color=StatsOverlay.Cream;}
  Arrow.gameObject.SetActive(true);Arrow.rectTransform.SetParent(parent,false);Arrow.font=font;
  Vector2 at;float units;
  if(label!=null){label.ForceMeshUpdate();var bounds=label.textBounds;units=label.fontSize/24;at=new Vector2(bounds.min.x,bounds.center.y);Arrow.fontSize=label.fontSize*.75f;}
  else{units=parent.rect.height/64;at=new Vector2(parent.rect.xMin+32*units,parent.rect.center.y);Arrow.fontSize=18*units;}
  Arrow.rectTransform.sizeDelta=new Vector2(24,32)*units;Arrow.rectTransform.anchorMin=Arrow.rectTransform.anchorMax=parent.pivot;Arrow.rectTransform.pivot=new Vector2(1,.5f);Arrow.rectTransform.anchoredPosition=at-new Vector2(8*units,0);
 }
 void OnDestroy(){if(Arrow!=null)Destroy(Arrow.gameObject);}
}
