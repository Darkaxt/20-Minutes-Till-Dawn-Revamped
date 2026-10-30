using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Pickups;
namespace TwentyMinutes.Modernization;

// The world object owns its marker's lifecycle. One shared layout arranges
// native boss and exploration chest targets on the same safe perimeter.
public sealed class ChestDirectionIndicator:MonoBehaviour
{
 internal static readonly List<ChestDirectionIndicator> Active=new List<ChestDirectionIndicator>();
 public Sprite Icon;public Color IconTint=Color.white;public bool Exploration;public bool Radar=true;
 public RectTransform Marker;internal TMP_Text DistanceText;internal RectTransform Arrow;
 ChestPickup chest;
 void Awake(){chest=GetComponent<ChestPickup>();if(chest!=null)Icon=(Sprite)AccessTools.Field(typeof(ChestPickup),"chestClosed").GetValue(chest);}
 void OnEnable(){Active.Add(this);}
 void OnDisable(){Active.Remove(this);if(Marker!=null)Marker.gameObject.SetActive(false);}
 void OnDestroy(){Active.Remove(this);if(Marker!=null)Destroy(Marker.gameObject);}
 internal bool Collecting=>chest!=null&&GetComponent<AndroidCollectIgnore>()!=null;
 // Reward menus disable the HUD root, including its layout. The world target
 // still observes collection and clears its own marker during that menu.
 void LateUpdate(){if(Collecting&&Marker!=null)Marker.gameObject.SetActive(false);}
 internal void Build(StatsOverlay hud)
 {
  Marker=StatsOverlay.Rect(new GameObject(Exploration?"ExplorationChestDirection":"BossChestDirection",typeof(RectTransform),typeof(Image)),hud.Panel.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(28,28));
  var image=Marker.GetComponent<Image>();image.sprite=Icon;image.preserveAspect=true;image.raycastTarget=false;image.color=IconTint;
  var arrow=StatsOverlay.Text(Marker,"Direction",hud.Font,"›",20,Vector2.zero,new Vector2(20,20));arrow.alignment=TextAlignmentOptions.Center;arrow.color=Exploration?ExplorationChestVisual.RewardColor(GetComponent<ExplorationChest>().ShownReward):Color.white;
  Arrow=StatsOverlay.Rect(arrow.gameObject,Marker,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(20,20));
  DistanceText=StatsOverlay.Text(Marker,"Distance",hud.Font,"",12,Vector2.zero,new Vector2(64,16));DistanceText.alignment=TextAlignmentOptions.Center;DistanceText.color=StatsOverlay.Cream;
  StatsOverlay.Rect(DistanceText.gameObject,Marker,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-43),new Vector2(64,16));
 }
}
public sealed class PickupDirectionLayout:MonoBehaviour
{
 void OnEnable(){Camera.onPreRender+=Place;}
 void OnDisable(){Camera.onPreRender-=Place;}
 public static Vector2 EdgePoint(Vector2 direction,Rect edge)
 {
  var half=edge.size*.5f;var center=edge.center;
  bool horizontal=Mathf.Abs(direction.y)*half.x>=Mathf.Abs(direction.x)*half.y;
  float factor=horizontal?half.y/Mathf.Abs(direction.y):half.x/Mathf.Abs(direction.x);
  return center+direction*factor;
 }
 void Place(Camera camera)
 {
  // PixelPerfectCamera finalizes projection/viewport during OnPreCull, after
  // LateUpdate. Align markers with the actual rendering projection.
  if(camera!=Camera.main)return;
  var hud=StatsOverlay.Instance;var player=PlayerController.Instance;
  if(hud==null||hud.Panel==null||player==null||camera==null)return;
  float scale=((RectTransform)hud.Panel.transform).lossyScale.x;var safe=Screen.safeArea;var center=safe.center;
  var edge=Rect.MinMaxRect(safe.xMin+56*scale,safe.yMin+84*scale,safe.xMax-56*scale,safe.yMax-84*scale);
  foreach(var target in ChestDirectionIndicator.Active.Where(t=>t!=null&&t.gameObject.scene.IsValid()).OrderBy(t=>Vector3.Distance(t.transform.position,player.transform.position)))
  {
   if(!target.Radar){if(target.Marker!=null)target.Marker.gameObject.SetActive(false);continue;}
   // PixelPerfectCamera renders into a smaller pixel viewport, then blits it
   // to the display. Internal render pixels are not overlay screen pixels.
   var viewport=camera.WorldToViewportPoint(target.transform.position);
   var projected=new Vector3(viewport.x*Screen.width,viewport.y*Screen.height,viewport.z);
   if(target.Collecting){if(target.Marker!=null)target.Marker.gameObject.SetActive(false);continue;}
   if(target.Marker==null)target.Build(hud);target.Marker.gameObject.SetActive(true);
   var direction=(Vector2)projected-center;if(projected.z<0)direction=-direction;
   if(direction.sqrMagnitude==0)direction=Vector2.up;
   // Track continuously from the perimeter to the real chest. No HUD or
   // marker reservation displaces the target, including across the north axis.
   bool inside=projected.z>0&&edge.Contains(projected);
   var point=inside?(Vector2)projected:EdgePoint(direction,edge);
   var parent=(RectTransform)hud.Panel.transform;RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,point,null,out var local);
   // Screen-to-local is relative to the parent's pivot. The marker anchors are
   // at its center; the HUD root itself uses a bottom-left pivot.
   target.Marker.anchoredPosition=local-parent.rect.center;
   target.Arrow.gameObject.SetActive(!inside);target.Arrow.anchoredPosition=direction.normalized*21;target.Arrow.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
   target.DistanceText.text=Mathf.CeilToInt(Vector2.Distance(target.transform.position,player.transform.position)).ToString();
  }
 }
}
