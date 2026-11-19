using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using UnityEngine;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// Locally decoded source frames; Unity2022 bundles never enter the Windows
// player. The existing palette owner supplies both artwork modes.
public static class SaintRosaAssets
{
 public sealed class Actor
 {
  public string field,name;public CosmeticVisual[] visuals;public string[] frames;
  public float duration,lifetime,radius,interval;public bool loop;
 }
 public sealed class CosmeticVisual
 {
  public string path,sprite;public Characters.Pose[] poses;public string[] frames;
  public float interval;public Color color;public int order;
 }
 public sealed class Manifest{public Actor[] actors;public string[] chamber;public Characters.SpriteRecord[] sprites;}
 internal static Manifest Data;
 static readonly Dictionary<string,Sprite> sprites=new Dictionary<string,Sprite>();
 internal static void Load()
 {
  if(Data!=null)return;
  string root=Path.Combine(Plugin.AssetRoot,"saint-rosa");
  using(var stream=File.OpenRead(Path.Combine(root,"manifest.json")))Data=(Manifest)new DataContractJsonSerializer(typeof(Manifest)).ReadObject(stream);
  foreach(var record in Data.sprites)
  {
   string original=Path.Combine(root,record.file);
   var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
   texture.LoadImage(File.ReadAllBytes(original));texture.filterMode=FilterMode.Point;texture.name="SaintRosaPalette_"+record.key;
   ColorfulStyle.Instance.RegisterLocalPalette(texture,original,Path.Combine(root,record.colorful));
   var sprite=UnityEngine.Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(record.pivotX,record.pivotY),record.ppu);
   sprite.name=record.name;Object.DontDestroyOnLoad(texture);Object.DontDestroyOnLoad(sprite);sprites.Add(record.key,sprite);
  }
  ColorfulStyle.Instance.ApplyLoaded();
 }
 internal static Sprite Sprite(string key)=>key==null?null:sprites[key];
 internal static Actor Contract(string field)=>Data.actors.Single(a=>a.field==field);
}
