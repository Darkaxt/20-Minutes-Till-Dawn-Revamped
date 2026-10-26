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
 public Sprite Icon;public bool Exploration;
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
  var image=Marker.GetComponent<Image>();image.sprite=Icon;image.preserveAspect=true;image.raycastTarget=false;image.color=Exploration?ExplorationRun.Mint:Color.white;
  var arrow=StatsOverlay.Text(Marker,"Direction",hud.Font,"›",20,Vector2.zero,new Vector2(20,20));arrow.alignment=TextAlignmentOptions.Center;arrow.color=image.color;
  Arrow=StatsOverlay.Rect(arrow.gameObject,Marker,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(20,20));
  DistanceText=StatsOverlay.Text(Marker,"Distance",hud.Font,"",12,Vector2.zero,new Vector2(64,16));DistanceText.alignment=TextAlignmentOptions.Center;DistanceText.color=StatsOverlay.Cream;
  StatsOverlay.Rect(DistanceText.gameObject,Marker,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-43),new Vector2(64,16));
 }
}
[DefaultExecutionOrder(200)]
public sealed class PickupDirectionLayout:MonoBehaviour
{
 readonly List<Vector2> occupied=new List<Vector2>();
 static Rect ScreenBounds(RectTransform view)
 {var corners=new Vector3[4];view.GetWorldCorners(corners);var points=corners.Select(p=>RectTransformUtility.WorldToScreenPoint(null,p)).ToArray();return Rect.MinMaxRect(points.Min(p=>p.x),points.Min(p=>p.y),points.Max(p=>p.x),points.Max(p=>p.y));}
 void LateUpdate()
 {
  var hud=StatsOverlay.Instance;var player=PlayerController.Instance;var camera=Camera.main;
  if(hud==null||hud.Panel==null||player==null||camera==null)return;
  float scale=((RectTransform)hud.Panel.transform).lossyScale.x;var safe=Screen.safeArea;var center=safe.center;
  var edge=Rect.MinMaxRect(safe.xMin+56*scale,safe.yMin+84*scale,safe.xMax-56*scale,safe.yMax-84*scale);var half=edge.size*.5f;
  var game=ExplorationRun.Instance.GetComponent<flanne.Core.GameController>();
  var reserved=game.hud.GetComponentsInChildren<Graphic>().Where(g=>g.color.a>0).Select(g=>ScreenBounds(g.rectTransform)).ToList();
  if(hud.Body.gameObject.activeInHierarchy)reserved.Add(ScreenBounds(hud.Body.rectTransform));
  reserved.AddRange(hud.AttributeIcons.Concat(hud.SkillIcons).Where(i=>i.gameObject.activeInHierarchy).Select(i=>ScreenBounds(i.rectTransform)));
  occupied.Clear();
  foreach(var target in ChestDirectionIndicator.Active.Where(t=>t!=null&&t.gameObject.scene.IsValid()).OrderBy(t=>Vector3.Distance(t.transform.position,player.transform.position)))
  {
   var projected=camera.WorldToScreenPoint(target.transform.position);
   bool visible=projected.z>0&&projected.x>=safe.xMin&&projected.x<=safe.xMax&&projected.y>=safe.yMin&&projected.y<=safe.yMax;
   if(visible||target.Collecting){if(target.Marker!=null)target.Marker.gameObject.SetActive(false);continue;}
   if(target.Marker==null)target.Build(hud);target.Marker.gameObject.SetActive(true);
   var direction=(Vector2)projected-center;if(projected.z<0)direction=-direction;
   float factor=Mathf.Min(direction.x==0?float.PositiveInfinity:half.x/Mathf.Abs(direction.x),direction.y==0?float.PositiveInfinity:half.y/Mathf.Abs(direction.y));
   var desired=center+direction*factor;float spacing=96*scale;
   var points=new List<Vector2>{desired};int horizontal=Mathf.Max(1,Mathf.FloorToInt(edge.width/spacing)),vertical=Mathf.Max(1,Mathf.FloorToInt(edge.height/spacing));
   for(int i=0;i<=horizontal;i++){float x=Mathf.Lerp(edge.xMin,edge.xMax,(float)i/horizontal);points.Add(new Vector2(x,edge.yMin));points.Add(new Vector2(x,edge.yMax));}
   for(int i=1;i<vertical;i++){float y=Mathf.Lerp(edge.yMin,edge.yMax,(float)i/vertical);points.Add(new Vector2(edge.xMin,y));points.Add(new Vector2(edge.xMax,y));}
   var point=points.OrderBy(p=>reserved.Count(r=>r.Overlaps(new Rect(p.x-36*scale,p.y-52*scale,72*scale,88*scale)))*10000000+occupied.Count(other=>Vector2.Distance(other,p)<spacing)*1000000+Vector2.SqrMagnitude(p-desired)).First();occupied.Add(point);
   var parent=(RectTransform)hud.Panel.transform;RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,point,null,out var local);
   // Screen-to-local is relative to the parent's pivot. The marker anchors are
   // at its center; the HUD root itself uses a bottom-left pivot.
   target.Marker.anchoredPosition=local-parent.rect.center;
   target.Arrow.anchoredPosition=direction.normalized*21;target.Arrow.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
   target.DistanceText.text=Mathf.CeilToInt(Vector2.Distance(target.transform.position,player.transform.position)).ToString();
  }
 }
}
