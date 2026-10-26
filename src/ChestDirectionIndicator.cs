using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using flanne.Pickups;
namespace TwentyMinutes.Modernization;

// Each actual pooled chest owns its marker. No independent drop inventory or
// saved marker state; native chest activation/removal drives the whole lifetime.
public sealed class ChestDirectionIndicator:MonoBehaviour
{
 RectTransform marker;Camera worldCamera;ChestPickup chest;
 void Awake(){chest=GetComponent<ChestPickup>();}
 void LateUpdate()
 {
  var hud=StatsOverlay.Instance;
  if(hud==null||hud.Panel==null)return;
  if(worldCamera==null)foreach(var camera in FindObjectsOfType<Camera>())if(camera.name=="Main Camera")worldCamera=camera;
  if(worldCamera==null)return;
  var projected=worldCamera.WorldToScreenPoint(transform.position);
  bool visible=projected.z>0&&projected.x>=0&&projected.x<=Screen.width&&projected.y>=0&&projected.y<=Screen.height;
  bool collecting=GetComponent<AndroidCollectIgnore>()!=null;
  if(visible||collecting){if(marker!=null)marker.gameObject.SetActive(false);return;}
  if(marker==null)
  {
   marker=StatsOverlay.Rect(new GameObject("BossChestDirection",typeof(RectTransform),typeof(Image)),hud.Panel.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(28,28));
   var image=marker.GetComponent<Image>();image.sprite=(Sprite)AccessTools.Field(typeof(ChestPickup),"chestClosed").GetValue(chest);image.preserveAspect=true;image.raycastTarget=false;
   var arrow=StatsOverlay.Text(marker,"Direction",hud.Font,"›",24,new Vector2(0,-26),new Vector2(24,24));arrow.alignment=TextAlignmentOptions.Center;
   StatsOverlay.Rect(arrow.gameObject,marker,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-24),new Vector2(24,24));
  }
  marker.gameObject.SetActive(true);
  var safe=Screen.safeArea;var center=safe.center;var direction=(Vector2)projected-center;
  if(projected.z<0)direction=-direction;
  var half=safe.size*.5f-Vector2.one*30;
  float factor=Mathf.Min(direction.x==0?float.PositiveInfinity:half.x/Mathf.Abs(direction.x),direction.y==0?float.PositiveInfinity:half.y/Mathf.Abs(direction.y));
  var point=center+direction*factor;
  RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hud.Panel.transform,point,null,out var local);marker.anchoredPosition=local;
  var arrowRect=(RectTransform)marker.GetChild(0);float angle=Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
  arrowRect.anchoredPosition=direction.normalized*23;arrowRect.localRotation=Quaternion.Euler(0,0,angle);
 }
 void OnDisable(){if(marker!=null)marker.gameObject.SetActive(false);}
 void OnDestroy(){if(marker!=null)Destroy(marker.gameObject);}
}
