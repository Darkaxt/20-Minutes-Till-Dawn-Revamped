using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// The owned logo is a tightly shaped sprite, not an ordinary alpha-masked PNG.
// Extend its pixel artwork in the unused lower margin and supply an untrimmed
// sprite: the stock UI crops the tight sprite's lower margin out of its UVs.
public class TitleArtwork:MonoBehaviour
{
 public const string Credit="MAINTAINED BY DARKAXT";
 public const string LogoName="T_20Logo";
 public static TitleArtwork Instance;
 public sealed class AppliedLogo
 {
  public Sprite Sprite;
  public Sprite Artwork;
  public Vector2[] OriginalVertices;
  public ushort[] OriginalTriangles;
  public string OriginalTextureHash;
  public Color32[] OriginalPixels;
  public int CreditPixels;
 }
 public readonly List<AppliedLogo> Logos=new List<AppliedLogo>();
 static readonly Dictionary<char,string> Glyphs=new Dictionary<char,string>
 {
  ['A']="0011100/0100010/1000001/1000001/1111111/1000001/1000001/1000001/1000001",
  ['B']="1111100/1000010/1000001/1000010/1111100/1000010/1000001/1000010/1111100",
  ['D']="1111100/1000010/1000001/1000001/1000001/1000001/1000001/1000010/1111100",
  ['E']="1111111/1000000/1000000/1000000/1111100/1000000/1000000/1000000/1111111",
  ['F']="1111111/1000000/1000000/1000000/1111100/1000000/1000000/1000000/1000000",
  ['I']="0111110/0001000/0001000/0001000/0001000/0001000/0001000/0001000/0111110",
  ['K']="1000001/1000010/1000100/1001000/1110000/1001000/1000100/1000010/1000001",
  ['M']="1000001/1100011/1010101/1001001/1000001/1000001/1000001/1000001/1000001",
  ['N']="1000001/1100001/1010001/1001001/1000101/1000011/1000001/1000001/1000001",
  ['O']="0011100/0100010/1000001/1000001/1000001/1000001/1000001/0100010/0011100",
  ['R']="1111100/1000010/1000001/1000010/1111100/1001000/1000100/1000010/1000001",
  ['T']="1111111/0001000/0001000/0001000/0001000/0001000/0001000/0001000/0001000",
  ['X']="1000001/0100010/0010100/0001000/0001000/0001000/0010100/0100010/1000001",
  ['Y']="1000001/0100010/0010100/0001000/0001000/0001000/0001000/0001000/0001000"
 };
 void Awake(){Instance=this;SceneManager.sceneLoaded+=Loaded;ApplyLoaded();}
 void Loaded(Scene scene,LoadSceneMode mode)=>ApplyLoaded();
 void OnDestroy()
 {
  SceneManager.sceneLoaded-=Loaded;
  foreach(var logo in Logos)
  {
   if(logo.Artwork==null)continue;
   foreach(var image in Resources.FindObjectsOfTypeAll<Image>().Where(i=>i.sprite==logo.Artwork))image.sprite=logo.Sprite;
   foreach(var renderer in Resources.FindObjectsOfTypeAll<SpriteRenderer>().Where(r=>r.sprite==logo.Artwork))renderer.sprite=logo.Sprite;
   if(logo.Artwork!=null){Object.Destroy(logo.Artwork.texture);Object.Destroy(logo.Artwork);}
  }
  if(Instance==this)Instance=null;
 }
 public void ApplyLoaded()
 {
  foreach(var sprite in Resources.FindObjectsOfTypeAll<Sprite>().Where(s=>s.name==LogoName))
  {
   if(Logos.Any(l=>l.Sprite==sprite))continue;
   if(sprite.rect!=new Rect(0,0,320,176)||sprite.texture.width!=320||sprite.texture.height!=176)
    throw new InvalidOperationException("Unsupported native title artwork layout");
   var record=new AppliedLogo{Sprite=sprite,OriginalVertices=sprite.vertices,OriginalTriangles=sprite.triangles,OriginalTextureHash=ColorfulStyle.PixelHash(sprite.texture)};
   var pixels=ColorfulStyle.Sample(sprite.texture);
   try
   {
    record.OriginalPixels=pixels.GetPixels32();
    var ink=(Color32)pixels.GetPixel(160,43);int left=(320-(Credit.Length*8-1))/2;
    for(int c=0;c<Credit.Length;c++)
    {
     if(Credit[c]==' ')continue;
     var rows=Glyphs[Credit[c]].Split('/');
     if(rows.Length!=9||rows.Any(r=>r.Length!=7))throw new InvalidOperationException("Invalid title pixel glyph");
     for(int row=0;row<9;row++)for(int column=0;column<7;column++)
     {
      if(rows[row][column]!='1')continue;
      int x=left+c*8+column,y=14+8-row;
      var blank=(Color32)pixels.GetPixel(x,y);
      if(blank.a!=0||ink.a!=255)throw new InvalidOperationException("Title credit overlaps original artwork");
      pixels.SetPixel(x,y,ink);
      record.CreditPixels++;
     }
    }
    pixels.Apply(false,false);pixels.name="Revamped_"+LogoName;pixels.filterMode=FilterMode.Point;
    record.Artwork=Sprite.Create(pixels,sprite.rect,new Vector2(sprite.pivot.x/sprite.rect.width,sprite.pivot.y/sprite.rect.height),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect,sprite.border);
    record.Artwork.name="Revamped_"+LogoName;
   }
   catch{Object.Destroy(pixels);throw;}
   Logos.Add(record);
   Plugin.Instance.LoggerForArtwork("Native pixel artwork credit applied: "+Credit);
   Plugin.Trace("title-artwork",new{sprite=sprite.name,pixels=record.CreditPixels,originalVertices=record.OriginalVertices.Length});
  }
  foreach(var logo in Logos)
  {
   foreach(var image in Resources.FindObjectsOfTypeAll<Image>().Where(i=>i.sprite==logo.Sprite))image.sprite=logo.Artwork;
   foreach(var renderer in Resources.FindObjectsOfTypeAll<SpriteRenderer>().Where(r=>r.sprite==logo.Sprite))renderer.sprite=logo.Artwork;
  }
 }
}
[HarmonyPatch(typeof(TitleScreenController),"Start")]
class ApplyTitleArtwork
{
 static void Postfix(TitleScreenController __instance)
 {
  TitleArtwork.Instance.ApplyLoaded();
  var titleCanvas=GameObject.Find("Canvas/TitleScreen");
  var promotion=titleCanvas==null?null:titleCanvas.transform.Find("EmberpathBanner");
  if(promotion!=null)promotion.gameObject.SetActive(false);
 }
}
