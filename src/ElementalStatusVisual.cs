using System.Collections.Generic;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// The shipped distance-field GPU program provides an alpha mask and scrolling
// RGBA face textures. With binary sprite alpha its face is exactly the sprite
// interior: authored pixel patterns replace the old fog disks without changing
// any original enemy material, atlas, animation or artwork. No runtime compiler
// or external shader binary is required by the Windows/DroidDeck player.
public sealed partial class ElementalRun
{
 readonly Dictionary<Sprite,Mesh> statusMeshes=new Dictionary<Sprite,Mesh>();
 Material[] statusMaterials;Texture2D[] statusPatterns;
 internal Material StatusMaterial(int kind)
 {
  if(statusMaterials==null)
  {
   var shader=Shader.Find("TextMeshPro/Distance Field");statusMaterials=new Material[3];statusPatterns=new Texture2D[3];
   for(int i=0;i<3;i++)
   {
    statusPatterns[i]=StatusPattern(i);var m=new Material(shader){name="Revamped_"+new[]{"Burning","Electrified","Disoriented"}[i]};
    m.SetTexture("_FaceTex",statusPatterns[i]);m.SetFloat("_FaceUVSpeedX",i==1?.37f:i==2?.22f:0);m.SetFloat("_FaceUVSpeedY",i==0?-.48f:i==1?.61f:-.12f);
    m.SetColor("_FaceColor",Color.white);m.SetFloat("_GradientScale",5);m.SetFloat("_ScaleX",1);m.SetFloat("_ScaleY",1);m.SetFloat("_PerspectiveFilter",0);
    m.SetFloat("_FaceDilate",0);m.SetFloat("_WeightNormal",0);m.SetFloat("_OutlineWidth",0);m.SetFloat("_OutlineSoftness",0);m.SetFloat("_ScaleRatioA",1);m.SetFloat("_CullMode",0);
    statusMaterials[i]=m;
   }
  }
  return statusMaterials[kind];
 }
 internal Mesh StatusMesh(Sprite sprite)
 {
  if(statusMeshes.TryGetValue(sprite,out var mesh))return mesh;
  var source=sprite.vertices;var vertices=new Vector3[source.Length];var normals=new Vector3[source.Length];var colors=new Color32[source.Length];var faceUV=new Vector2[source.Length];
  for(int i=0;i<source.Length;i++)
  {
   vertices[i]=source[i];normals[i]=Vector3.back;colors[i]=new Color32(255,255,255,255);
   var point=source[i]*sprite.pixelsPerUnit+sprite.pivot;
   // The native TMP shader packs two face UV components into UV1.x.
   int u=Mathf.RoundToInt(point.x/sprite.rect.width*512),v=Mathf.RoundToInt(point.y/sprite.rect.height*512);faceUV[i]=new Vector2(u*4096+v,1);
  }
  var original=sprite.triangles;var triangles=new int[original.Length];for(int i=0;i<original.Length;i++)triangles[i]=original[i];
  mesh=new Mesh{name="StatusMask_"+sprite.name,vertices=vertices,normals=normals,colors32=colors,uv=sprite.uv,uv2=faceUV,triangles=triangles};mesh.RecalculateBounds();statusMeshes.Add(sprite,mesh);return mesh;
 }
 static Texture2D StatusPattern(int kind)
 {
  const int width=32,height=64;var pixels=new Color32[width*height];
  for(int y=0;y<height;y++)for(int x=0;x<width;x++)
  {
   Color c=Color.clear;
   if(kind==0)
   {
    // Uneven rising tongues with pale ember tips and a transparent background.
    for(int tongue=0;tongue<4;tongue++)
    {
     int py=(y+tongue*17)%height;float center=tongue*8+3+Mathf.Sin(py*.22f+tongue)*2;float distance=Mathf.Abs(Mathf.DeltaAngle(x/32f*360,center/32f*360))/360*32;
     float breadth=py<27?2.2f*(1-py/31f):0;
     if(distance<breadth)c=Color.Lerp(new Color(1,.24f,.035f,.55f),new Color(1,.82f,.3f,.9f),py/27f);
     if(py>=33&&py<36&&distance<.8f)c=new Color(1,.78f,.28f,.8f);
    }
   }
   else if(kind==1)
   {
    // Narrow, forked pixel arcs; most of the original sprite stays visible.
    int center=8+(y/4%2==0?y%4:3-y%4)*2;
    int branch=(center+((y/8)%2==0?y%8:7-y%8)*2+9)%32;
    if(x==center||y%24<10&&x==branch)c=new Color(.6f,.89f,1,.95f);
    else if(Mathf.Abs(x-center)==1)c=new Color(.18f,.53f,1,.2f);
   }
   else
   {
    int line=(x+y/3+(int)(Mathf.Sin(y*.2f)*2))%16;
    if(line==0&&y%17<10)c=new Color(.66f,.96f,.81f,.72f);
    else if(line==1&&y%17<10)c=new Color(.4f,.77f,.64f,.22f);
   }
   pixels[y*width+x]=c;
  }
  var texture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="Revamped_StatusPattern_"+kind,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
 }
 void ReleaseStatusArt()
 {
  foreach(var mesh in statusMeshes.Values)Destroy(mesh);statusMeshes.Clear();
  if(statusMaterials!=null)foreach(var material in statusMaterials)Destroy(material);
  if(statusPatterns!=null)foreach(var texture in statusPatterns)Destroy(texture);
 }
}

// Each active effect has its own masked layer, so a stun does not erase a burn.
// Meshes/materials are shared by the battle; only per-actor state uses blocks.
public sealed class ElementalStatusVisual:MonoBehaviour
{
 ElementalControl control;Health health;SpriteRenderer body;readonly MeshRenderer[] overlays=new MeshRenderer[3];readonly MeshFilter[] filters=new MeshFilter[3];readonly MaterialPropertyBlock properties=new MaterialPropertyBlock();
 void Awake(){control=GetComponent<ElementalControl>();health=GetComponent<Health>();body=GetComponent<SpriteRenderer>();}
 internal void Hide(){foreach(var overlay in overlays)if(overlay!=null)overlay.enabled=false;}
 void OnDisable()=>Hide();
 void LateUpdate()
 {
  if(body==null||body.sprite==null||health.isDead){Hide();return;}
  var burn=BurnSystem.SharedInstance==null?null:AndroidBurnState.Get(BurnSystem.SharedInstance)?.Find(gameObject);
  for(int i=0;i<overlays.Length;i++)
  {
   bool active=i==0?burn!=null:i==1?control.Stun>0:control.Dizzy>0;
   if(active&&overlays[i]==null)
   {
    var child=new GameObject("ElementalSprite_"+i,typeof(MeshFilter),typeof(MeshRenderer));child.layer=body.gameObject.layer;child.transform.SetParent(body.transform,false);
    filters[i]=child.GetComponent<MeshFilter>();overlays[i]=child.GetComponent<MeshRenderer>();overlays[i].sharedMaterial=ElementalRun.Instance.StatusMaterial(i);overlays[i].receiveShadows=false;overlays[i].shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
   }
   var renderer=overlays[i];if(renderer==null)continue;renderer.enabled=active&&body.enabled&&body.color.a>0;if(!renderer.enabled)continue;
   filters[i].sharedMesh=ElementalRun.Instance.StatusMesh(body.sprite);
   renderer.transform.localScale=new Vector3(body.flipX?-1:1,body.flipY?-1:1,1);renderer.sortingLayerID=body.sortingLayerID;renderer.sortingOrder=body.sortingOrder+1+i;
   properties.Clear();properties.SetTexture("_MainTex",body.sprite.texture);float pulse=i==1?.7f+.3f*Mathf.Sin(Time.time*27):1;properties.SetColor("_FaceColor",new Color(1,1,1,body.color.a*pulse*PlayerVisualOpacity.Factor(PlayerVisualOpacity.Channel.Effects)));renderer.SetPropertyBlock(properties);
  }
 }
}
