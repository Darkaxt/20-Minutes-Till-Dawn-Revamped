using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

public sealed class AndroidAffixes:MonoBehaviour
{
 List<int> values;
 internal Transform IconParent;internal int SortingLayer,SortingOrder;
 void Awake(){values=new List<int>();}
 internal bool Has(int value)=>values.Contains(value);
 internal static bool Has(GameObject actor,int value)=>actor.GetComponent<AndroidAffixes>()?.Has(value)==true;
 internal void Add(int value)
 {
  values.Add(value);
  var icon=new GameObject("affixIcon",typeof(SpriteRenderer));
  var renderer=icon.GetComponent<SpriteRenderer>();renderer.sprite=AndroidAffixArt.Sprite(value);renderer.sortingLayerID=SortingLayer;renderer.sortingOrder=SortingOrder;
  icon.transform.SetParent(IconParent);icon.transform.localScale=new Vector3(1,1,0);icon.SetActive(true);
  int count=IconParent.childCount;float width=count+.5f*(count-1);
  for(int i=0;i<count;i++)IconParent.GetChild(i).localPosition=new Vector3(i*.5f+i+.5f-width*.5f,0,0);
 }
 internal void Clear()
 {
  values.Clear();
  for(int i=IconParent.childCount-1;i>=0;i--)
  {var child=IconParent.GetChild(i);child.SetParent(null);Destroy(child.gameObject);}
 }
 void OnDisable()=>Clear();
 // Harmful mutates its stored integer on contact with this affix. Do not
 // replace it with a generic damage multiplier: later pierced contacts and
 // other targets must see the already-halved value too.
 internal static void Modify(Harmful harm,GameObject target){if(Has(target,3))harm.damageAmount/=2;}
}
static class AndroidAffixArt
{
 [Serializable]class Manifest{public Icon[] icons;public Actor[] actors;}
 [Serializable]class Icon{public string name;public float ppu,pivotX,pivotY;}
 [Serializable]class Actor{public string name;public Pose[] poses;public int sortingLayer,sortingOrder;}
 [Serializable]class Pose{public string name;public Vector3 position,scale;public Quaternion rotation;}
 static Manifest Data;
 static readonly string[] Names={"Affix_Fire","Affix_Thunder","Affix_Ice","Affix_BulletDamageReduce"};
 static readonly Dictionary<int,Sprite> Sprites=new Dictionary<int,Sprite>();
 static void Load()
 {
  if(Data==null)
   using(var stream=File.OpenRead(Path.Combine(Plugin.AssetRoot,"affixes.json")))
    Data=(Manifest)new DataContractJsonSerializer(typeof(Manifest)).ReadObject(stream);
 }
 internal static AndroidAffixes Attach(GameObject actor,string prefabName)
 {
  Load();var source=Data.actors.SingleOrDefault(a=>a.name==prefabName);if(source==null)return null;
  var state=actor.GetComponent<AndroidAffixes>();if(state!=null)return state;state=actor.AddComponent<AndroidAffixes>();
  Transform parent=actor.transform;
  foreach(var pose in source.poses){var child=new GameObject(pose.name);child.transform.SetParent(parent,false);parent=child.transform;parent.localPosition=pose.position;parent.localScale=pose.scale;parent.localRotation=pose.rotation;}
  state.IconParent=parent;state.SortingLayer=source.sortingLayer;state.SortingOrder=source.sortingOrder;return state;
 }
 internal static Sprite Sprite(int value)
 {
  // All96 original configurations map0..3 to these sprites. Source GetConfig
  // returns a default struct/null icon when no enum entry matches.
  if((uint)value>=(uint)Names.Length)return null;
  if(Sprites.TryGetValue(value,out var sprite))return sprite;
  Load();var source=Data.icons.Single(i=>i.name==Names[value]);
  var image=new Texture2D(2,2,TextureFormat.RGBA32,false);image.LoadImage(File.ReadAllBytes(Path.Combine(Plugin.AssetRoot,Names[value]+".png")));image.filterMode=FilterMode.Point;
  sprite=UnityEngine.Sprite.Create(image,new Rect(0,0,image.width,image.height),new Vector2(source.pivotX,source.pivotY),source.ppu);Sprites.Add(value,sprite);return sprite;
 }
}
