using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization;

// Reuse the actual native status widgets and their serialized variable/event
// bindings. Only hierarchy, dimensions and typography belong to this layout.
[DefaultExecutionOrder(300)]
public sealed class CornerStatusHud:MonoBehaviour
{
 GameController owner;TMP_Text score,level;RectTransform panel,root,portrait,health,xp,ammo,levelBadge;
 Image face;Sprite characterPortrait;
 internal void Initialize(GameController game,TMP_Text scoreText){owner=game;score=scoreText;panel=(RectTransform)game.hud.transform;}
 static RectTransform Child(Transform parent,string name)=>parent.Find(name).GetComponent<RectTransform>();
 static RectTransform Rect(GameObject value,Transform parent,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)=>StatsOverlay.Rect(value,parent,anchor,pivot,position,size);
 void Build()
 {
  root=Rect(new GameObject("CornerStatusHud",typeof(RectTransform)),panel,new Vector2(0,1),new Vector2(0,1),Vector2.zero,Vector2.zero);
  portrait=Rect(new GameObject("CharacterPortrait",typeof(RectTransform),typeof(CornerPortraitRing)),root,new Vector2(0,1),new Vector2(0,1),Vector2.zero,Vector2.zero);
  portrait.GetComponent<CornerPortraitRing>().color=StatsOverlay.Cream;
  var mask=Rect(new GameObject("PortraitClip",typeof(RectTransform),typeof(Image),typeof(Mask)),portrait,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,Vector2.zero);
  mask.GetComponent<Image>().sprite=Resources.FindObjectsOfTypeAll<Image>().First(i=>i.sprite!=null&&i.sprite.name=="T_Circle").sprite;
  mask.GetComponent<Mask>().showMaskGraphic=false;mask.GetComponent<Image>().raycastTarget=false;
  face=Rect(new GameObject("CharacterFace",typeof(RectTransform),typeof(Image)),mask,new Vector2(.5f,1),new Vector2(.5f,1),Vector2.zero,Vector2.zero).GetComponent<Image>();face.preserveAspect=true;face.raycastTarget=false;
  var badge=StatsOverlay.Box(mask,"LevelBadge",new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,2),new Vector2(46,17),StatsOverlay.Ink);badge.GetComponent<Image>().raycastTarget=false;levelBadge=badge.GetComponent<RectTransform>();
  var oldLevel=Child(panel,"LevelDisplay");level=oldLevel.Find("LvlValue").GetComponent<TMP_Text>();
  Rect(level.gameObject,levelBadge,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(44,17));level.fontSize=14;level.alignment=TextAlignmentOptions.Center;level.raycastTarget=false;oldLevel.gameObject.SetActive(false);
  health=Child(panel,"HealthUI");health.GetComponent<HorizontalLayoutGroup>().enabled=false;
  xp=Child(panel,"XPBar");ammo=Child(panel,"AmmoDisplay");
  foreach(var view in new[]{health,xp,ammo})Rect(view.gameObject,root,new Vector2(0,1),new Vector2(0,1),Vector2.zero,Vector2.zero);
  xp.GetComponent<Slider>().interactable=false;
  foreach(var graphic in root.GetComponentsInChildren<Graphic>(true))graphic.raycastTarget=false;
 }
 void LateUpdate()
 {
  var player=PlayerController.Instance;if(player==null||player.loadedCharacter==null)return;
  // All native Start bindings have run before this first LateUpdate.
  if(root==null)Build();
  if(characterPortrait!=player.loadedCharacter.portrait){characterPortrait=player.loadedCharacter.portrait;face.sprite=characterPortrait;}
  score.ForceMeshUpdate();
  float uiScale=Mathf.Sqrt((float)Screen.width/1280*Screen.height/720);float nativeScale=panel.lossyScale.x;
  var safe=Screen.safeArea;float topInset=Screen.height-safe.yMax;
  float panelTop=RectTransformUtility.WorldToScreenPoint(null,panel.TransformPoint(new Vector3(panel.rect.xMin,panel.rect.yMax))).y;
  float scoreBottom=RectTransformUtility.WorldToScreenPoint(null,score.transform.TransformPoint(score.textBounds.min)).y;
  float height=Mathf.Min(90,(panelTop-scoreBottom-topInset-12*uiScale)/uiScale);
  root.localScale=Vector3.one*(uiScale/nativeScale);root.sizeDelta=new Vector2(260,height);
  root.anchoredPosition=new Vector2((safe.xMin+12*uiScale)/nativeScale,-(topInset+12*uiScale)/nativeScale);
  portrait.sizeDelta=new Vector2(height,height);
  var mask=(RectTransform)face.transform.parent;mask.sizeDelta=new Vector2(height-4,height-4);
  face.rectTransform.sizeDelta=new Vector2(height*1.45f,height*1.45f*characterPortrait.rect.height/characterPortrait.rect.width);face.rectTransform.anchoredPosition=new Vector2(0,height*.12f);
  levelBadge.sizeDelta=new Vector2(Mathf.Min(46,height-8),17);
  float columnX=height+10,width=260-columnX;
  var hearts=health.Cast<Transform>().Where(t=>t.gameObject.activeSelf).Select(t=>(RectTransform)t).ToArray();
  int columns=Mathf.Min(8,Mathf.Max(1,hearts.Length)),rows=Mathf.Max(1,Mathf.CeilToInt((float)hearts.Length/columns));
  float cell=Mathf.Min(22,(width-(columns-1)*2)/columns,(height-38-(rows-1)*2)/rows);
  float heartHeight=rows*cell+(rows-1)*2,totalHeight=heartHeight+38,top=(height-totalHeight)*.5f;
  health.anchoredPosition=new Vector2(columnX,-top);health.sizeDelta=new Vector2(width,heartHeight);
  for(int i=0;i<hearts.Length;i++)
  {var item=hearts[i];item.anchorMin=item.anchorMax=new Vector2(0,1);item.pivot=new Vector2(0,1);item.localScale=Vector3.one;item.anchoredPosition=new Vector2(i%columns*(cell+2),-i/columns*(cell+2));item.sizeDelta=new Vector2(cell,cell);item.GetComponent<Image>().raycastTarget=false;}
  xp.anchoredPosition=new Vector2(columnX,-top-heartHeight-7);xp.sizeDelta=new Vector2(width,5);
  ammo.anchoredPosition=new Vector2(columnX,-top-heartHeight-19);ammo.sizeDelta=new Vector2(width,19);
  var icon=Child(ammo,"Icon");Rect(icon.gameObject,ammo,new Vector2(0,.5f),new Vector2(0,.5f),Vector2.zero,new Vector2(19,19));
  AmmoText("AmmoCount",23,36);AmmoText("Slash",59,12);AmmoText("MaxAmmo",73,46);
 }
 void AmmoText(string name,float x,float width)
 {
  var text=ammo.Find(name).GetComponent<TMP_Text>();Rect(text.gameObject,ammo,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(x,0),new Vector2(width,19));
  text.fontSize=18;text.alignment=TextAlignmentOptions.MidlineLeft;text.raycastTarget=false;
 }
}

// Rasterize only the frame on a 40-pixel grid; keep the approved native portrait
// and its clipping mask untouched. Every edge is horizontal or vertical.
public sealed class CornerPortraitRing:MaskableGraphic
{
 const int Pixels=40;
 static bool Inside(int x,int y){float dx=x+.5f-Pixels*.5f,dy=y+.5f-Pixels*.5f;return dx*dx+dy*dy<=Pixels*Pixels*.25f;}
 protected override void OnPopulateMesh(VertexHelper mesh)
 {
  mesh.Clear();var bounds=rectTransform.rect;float pixel=bounds.width/Pixels;
  for(int y=0;y<Pixels;y++)for(int x=0;x<Pixels;x++)
  {
   if(!Inside(x,y)||(Inside(x-1,y)&&Inside(x+1,y)&&Inside(x,y-1)&&Inside(x,y+1)))continue;
   float left=bounds.xMin+x*pixel,bottom=bounds.yMin+y*pixel;int start=mesh.currentVertCount;
   mesh.AddVert(new Vector2(left,bottom),color,Vector2.zero);mesh.AddVert(new Vector2(left,bottom+pixel),color,Vector2.zero);
   mesh.AddVert(new Vector2(left+pixel,bottom+pixel),color,Vector2.zero);mesh.AddVert(new Vector2(left+pixel,bottom),color,Vector2.zero);
   mesh.AddTriangle(start,start+1,start+2);mesh.AddTriangle(start,start+2,start+3);
  }
 }
}
