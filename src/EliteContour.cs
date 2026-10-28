using System.Collections.Generic;
using UnityEngine;
namespace TwentyMinutes.Modernization;

public sealed partial class EliteRun
{
 readonly Dictionary<Texture2D,byte[]> contourAtlases=new Dictionary<Texture2D,byte[]>();
 readonly Dictionary<Sprite,Sprite> contours=new Dictionary<Sprite,Sprite>();
 internal Sprite Contour(Sprite source)
 {
  if(contours.TryGetValue(source,out var saved))return saved;
  if(!contourAtlases.TryGetValue(source.texture,out var alpha))
  {
   var sample=ColorfulStyle.Sample(source.texture);
   try{var pixels=sample.GetPixels32();alpha=new byte[pixels.Length];for(int i=0;i<pixels.Length;i++)alpha[i]=pixels[i].a;}
   finally{Destroy(sample);}
   contourAtlases.Add(source.texture,alpha);
  }
  const int padding=3;var rect=source.textureRect;int width=(int)rect.width,height=(int)rect.height,atlasWidth=source.texture.width;
  int outputWidth=width+padding*2,outputHeight=height+padding*2;var mask=new Color32[outputWidth*outputHeight];
  for(int y=-padding;y<height+padding;y++)for(int x=-padding;x<width+padding;x++)
  {
   bool filled=x>=0&&y>=0&&x<width&&y<height&&alpha[((int)rect.y+y)*atlasWidth+(int)rect.x+x]>8;
   if(filled)continue;
   int distance=padding+1;
   for(int dy=-padding;dy<=padding;dy++)for(int dx=-padding;dx<=padding;dx++)
   {
    int px=x+dx,py=y+dy;if(px<0||py<0||px>=width||py>=height)continue;
    int candidate=Mathf.Max(Mathf.Abs(dx),Mathf.Abs(dy));
    if(candidate<distance&&alpha[((int)rect.y+py)*atlasWidth+(int)rect.x+px]>8)distance=candidate;
   }
   byte opacity=distance==1?(byte)255:distance==2?(byte)75:distance==3?(byte)20:(byte)0;
   mask[(y+padding)*outputWidth+x+padding]=new Color32(255,255,255,opacity);
  }
  var texture=new Texture2D(outputWidth,outputHeight,TextureFormat.RGBA32,false){name="EliteContour_"+source.name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
  texture.SetPixels32(mask);texture.Apply(false,false);
  Vector2 pivot=source.pivot-source.textureRectOffset+Vector2.one*padding;pivot=new Vector2(pivot.x/outputWidth,pivot.y/outputHeight);
  saved=Sprite.Create(texture,new Rect(0,0,outputWidth,outputHeight),pivot,source.pixelsPerUnit,0,SpriteMeshType.FullRect);saved.name="EliteContour_"+source.name;contours.Add(source,saved);return saved;
 }
 void ReleaseContours()
 {foreach(var sprite in contours.Values){Destroy(sprite.texture);Destroy(sprite);}contours.Clear();contourAtlases.Clear();}
}

// Only the outside alpha edge is drawn. Animation, flipping and recolouring
// keep the original sprite intact; derived masks live with the battle owner.
public sealed class EliteContour:MonoBehaviour
{
 EnhancedEnemy owner;SpriteRenderer body,outline;
 internal void Initialize(EnhancedEnemy enemy,SpriteRenderer source,SpriteRenderer target){owner=enemy;body=source;outline=target;}
 void LateUpdate()
 {
  if(!owner.Enhanced||body.sprite==null){outline.enabled=false;return;}
  outline.sprite=EliteRun.Instance.Contour(body.sprite);outline.flipX=body.flipX;outline.flipY=body.flipY;
  outline.sortingLayerID=body.sortingLayerID;outline.sortingOrder=body.sortingOrder+1;
  float pulse=.9f+.1f*Mathf.Sin(Time.time*3);outline.color=new Color(pulse,.1f,.2f,body.color.a);outline.enabled=body.enabled;
 }
}
