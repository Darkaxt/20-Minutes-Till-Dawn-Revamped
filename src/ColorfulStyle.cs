using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;
public class ColorfulStyle:MonoBehaviour
{
 public static ColorfulStyle Instance;
 public class Sheet{public string Name,Hash;public int Width,Height;public byte[] Original,Colorful,OriginalDxt,ColorfulDxt;}
 readonly Dictionary<string,Sheet> sheets=new Dictionary<string,Sheet>();
 public class AppliedSheet{public Sheet Sheet;public string OriginalPixelHash;public byte[] RestorePng;}
 readonly Dictionary<Texture2D,AppliedSheet> applied=new Dictionary<Texture2D,AppliedSheet>();
 readonly Dictionary<Texture2D,bool> neutralTextures=new Dictionary<Texture2D,bool>();
 public int AppliedCount=>applied.Count;
 public int LastRestored,LastRestoreFailures;
 public bool Enabled=>Plugin.Instance.ColorfulEnabled.Value;
 public Button SettingButton;
 void Awake()
 {
  Instance=this;string root=Path.Combine(Plugin.AssetRoot,"reborn");
  foreach(var line in File.ReadAllLines(Path.Combine(root,"textures.tsv")))
  {
   var f=line.Split('\t');if(f.Length!=4)throw new InvalidDataException("Invalid texture manifest row");
   sheets.Add(f[0],new Sheet{Name=f[0],Width=int.Parse(f[1]),Height=int.Parse(f[2]),Hash=f[3],Original=File.ReadAllBytes(Path.Combine(root,f[0]+".original.png")),Colorful=File.ReadAllBytes(Path.Combine(root,f[0]+".colorful.png"))});
   var sheet=sheets[f[0]];string compressed=Path.Combine(root,f[0]+".original.dxt5");
   if(File.Exists(compressed)){sheet.OriginalDxt=File.ReadAllBytes(compressed);sheet.ColorfulDxt=File.ReadAllBytes(Path.Combine(root,f[0]+".colorful.dxt5"));}
  }
  SceneManager.sceneLoaded+=Loaded;
 }
 void OnDestroy(){SceneManager.sceneLoaded-=Loaded;}
 void Loaded(Scene scene,LoadSceneMode mode){ApplyLoaded();}
 public void Toggle(){Plugin.Instance.ColorfulEnabled.Value=!Enabled;ApplyLoaded();Labels();}
 public void ApplyLoaded()
 {
  foreach(var dead in applied.Keys.Where(x=>x==null).ToArray())applied.Remove(dead);
  LastRestored=0;LastRestoreFailures=0;
  foreach(var texture in Resources.FindObjectsOfTypeAll<Texture2D>())
  {
   if(!sheets.TryGetValue(texture.name,out var sheet) || texture.width!=sheet.Width || texture.height!=sheet.Height)continue;
   if(Enabled && !applied.ContainsKey(texture))
   {
    if((texture.format!=TextureFormat.RGBA32 && texture.format!=TextureFormat.RGB24 && texture.format!=TextureFormat.ARGB32 && texture.format!=TextureFormat.DXT5) || texture.mipmapCount!=1)throw new InvalidDataException("Current texture no longer matches manifest: "+texture.name);
    string originalPixels=PixelHash(texture);
    byte[] restore=sheet.Original;
    if(sheet.ColorfulDxt!=null)CopyDxt(texture,sheet.ColorfulDxt);
    else if(!texture.LoadImage(sheet.Colorful,false))throw new InvalidOperationException("Colorful texture decode failed: "+texture.name);
    if(texture.width!=sheet.Width || texture.height!=sheet.Height)throw new InvalidOperationException("Texture bounds changed: "+texture.name);
    applied[texture]=new AppliedSheet{Sheet=sheet,OriginalPixelHash=originalPixels,RestorePng=restore};
    if(texture.name=="T_Death_Sword_Spin")Plugin.Trace("compressed-sheet",texture.name+" loaded as "+texture.format+" readable="+texture.isReadable);
   }
   else if(!Enabled && applied.ContainsKey(texture))
   {
    if(LastRestored==0)Plugin.Trace("restore-native-format",texture.name+" PNG-loaded format="+texture.format+" original=RGBA32");
    if(sheet.OriginalDxt!=null)CopyDxt(texture,sheet.OriginalDxt);
    else if(!texture.LoadImage(applied[texture].RestorePng,false))throw new InvalidOperationException("Original texture decode failed: "+texture.name);
    if(PixelHash(texture)!=applied[texture].OriginalPixelHash){LastRestoreFailures++;Plugin.Trace("restore-mismatch",texture.name+" format="+texture.format+" linear="+QualitySettings.activeColorSpace);}
    LastRestored++;applied.Remove(texture);
   }
  }
  Plugin.Trace("visual-style",new{style=Enabled?"Colorful":"Original",applied=applied.Count,restored=LastRestored,restoreHashFailures=LastRestoreFailures});
  if(LastRestoreFailures!=0)throw new InvalidOperationException("Original texture pixel restoration failed");
  foreach(var visual in Resources.FindObjectsOfTypeAll<ColorfulProjectileVisuals>())if(visual.gameObject.scene.IsValid())visual.Apply();
 }
 public static string Hash(byte[] bytes){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
 internal bool NeedsProjectileTint(Sprite sprite)
 {
  if(sprite==null||sheets.ContainsKey(sprite.texture.name))return false;
  var texture=sprite.texture;if(neutralTextures.TryGetValue(texture,out bool neutral))return neutral;
  var copy=Sample(texture);
  try
  {
   neutral=copy.GetPixels32().Where(p=>p.a>8).All(p=>Mathf.Max(p.r,Mathf.Max(p.g,p.b))-Mathf.Min(p.r,Mathf.Min(p.g,p.b))<=78);
   neutralTextures.Add(texture,neutral);return neutral;
  }
  finally{Object.Destroy(copy);}
 }
 static void CopyDxt(Texture2D target,byte[] bytes)
 {
  if(target.format!=TextureFormat.DXT5 || target.mipmapCount!=1)throw new InvalidDataException("Compressed target format changed: "+target.name);
  var upload=new Texture2D(target.width,target.height,TextureFormat.DXT5,false);
  try{upload.LoadRawTextureData(bytes);upload.Apply(false,false);Graphics.CopyTexture(upload,target);}
  finally{Object.Destroy(upload);}
 }
 public static string PixelHash(Texture2D source)
 {
  var copy=Sample(source);try{return Hash(copy.GetRawTextureData());}finally{Object.Destroy(copy);}
 }
 public static Texture2D Sample(Texture2D source)
 {
  // Imported sheets are non-readable. LoadImage changes RGBA32 to ARGB32 but
  // preserves that flag, so compare canonical sampled pixels at the GPU boundary.
  var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
  Texture2D copy=null;
  try
  {
   Graphics.Blit(source,rt);RenderTexture.active=rt;copy=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
   copy.ReadPixels(new Rect(0,0,source.width,source.height),0,0);copy.Apply(false,false);return copy;
  }
  catch{if(copy!=null)Object.Destroy(copy);throw;}
  finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
 }
 internal void Labels(){foreach(var t in Resources.FindObjectsOfTypeAll<TMP_Text>().Where(x=>x.name=="ColorfulSettingLabel"))t.text="Visual style: "+(Enabled?"Sprites Reborn":"Original");}
}
