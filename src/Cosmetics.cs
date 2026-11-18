using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// The collection is a companion to existing progression. Source cosmetic IDs
// are stable across versions; purchases and seasonal state are not imported.
public static class Cosmetics
{
 [Serializable]public sealed class Selection{public string owner;public int character,weapon,wing,bullet;}
 [Serializable]public sealed class Collection
 {public List<int> owned=new List<int>(),pending=new List<int>();public List<string> victories=new List<string>();public List<Selection> selections=new List<Selection>();}
 public sealed class Frame{public float time;public uint path;public string sprite;}
 public sealed class Clip{public string source,name;public float duration;public bool loop;public Frame[] frames;}
 public sealed class Item
 {public int id,colorIndex;public string kind,name,owner,icon,portrait,summonSprite,groundSprite,projectileName;public Clip[] clips,previewClips,summonClips,projectileClips;public string[] wingFrames,summonFrames,projectileFrames;public Color color;public Visual[] projectileVisuals;}
 public sealed class Visual{public string path,sprite;public Characters.Pose[] poses;public string[] frames;public float interval;public Clip[] clips;public Color color;public int order;}
 public sealed class ColorBinding{public string template,path;public int channel;public Color[] palette;}
 public sealed class SpriteRecord{public string key,name,file;public float pivotX,pivotY,ppu;}
 public sealed class Manifest{public int version;public Item[] items;public SpriteRecord[] sprites;public ColorBinding[] colorBindings;public Characters.Pose[] wingPoses;public float wingInterval;}
 public static Characters.Pose[] WingPoses;public static float WingInterval;
 public static ColorBinding[] ColorBindings=new ColorBinding[0];
 public static readonly Dictionary<int,Item> Items=new Dictionary<int,Item>();
 public static readonly Dictionary<string,Sprite> Sprites=new Dictionary<string,Sprite>();
 static readonly Dictionary<string,SpriteRecord> records=new Dictionary<string,SpriteRecord>();
 static readonly Dictionary<CharacterData,Tuple<Sprite,Sprite>> originalCharacters=new Dictionary<CharacterData,Tuple<Sprite,Sprite>>();
 static readonly Dictionary<GunData,Sprite> originalWeapons=new Dictionary<GunData,Sprite>();
 public static bool Available=>Items.Count!=0;
 public static Collection State=>Progression.State.cosmetics;
 internal static void Initialize()
 {
  if(Progression.State.cosmetics==null)Progression.State.cosmetics=new Collection();
  if(Available){RegisterData();RefreshData();return;}
  string path=Path.Combine(Plugin.AssetRoot,"cosmetics/manifest.json");
  if(!File.Exists(path))return; // Existing local preparations retain their original appearance.
  Manifest manifest;using(var stream=File.OpenRead(path))manifest=(Manifest)new DataContractJsonSerializer(typeof(Manifest)).ReadObject(stream);
  if(manifest.version!=1)throw new InvalidDataException("Unsupported appearance catalog version");
  foreach(var record in manifest.sprites)records.Add(record.key,record);
  foreach(var item in manifest.items)Items.Add(item.id,item);
  ColorBindings=manifest.colorBindings;WingPoses=manifest.wingPoses;WingInterval=manifest.wingInterval;RegisterData();
  RefreshData();
 }
 static void RegisterData()
 {
  foreach(var item in Characters.Items(Progression.Title.characterMenu))if(!originalCharacters.ContainsKey(item.data))originalCharacters[item.data]=Tuple.Create(item.data.portrait,item.data.icon);
  foreach(var item in NativeSelection.GunItems(Progression.Title.gunMenu))if(!originalWeapons.ContainsKey(item.data))originalWeapons[item.data]=item.data.icon;
 }
 internal static Sprite Original(string owner,string kind)=>kind=="character"?originalCharacters.First(p=>p.Key!=null&&p.Key.name==owner).Value.Item1:originalWeapons.First(p=>p.Key!=null&&p.Key.name==owner).Value;
 public static Sprite Sprite(string key)
 {
  if(key==null)return null;if(Sprites.TryGetValue(key,out var sprite))return sprite;
  var record=records[key];var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);texture.LoadImage(File.ReadAllBytes(Path.Combine(Plugin.AssetRoot,"cosmetics",record.file)));texture.filterMode=FilterMode.Point;texture.name="Appearance_"+record.name;
  sprite=UnityEngine.Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(record.pivotX,record.pivotY),record.ppu);sprite.name=record.name;
  Object.DontDestroyOnLoad(texture);Object.DontDestroyOnLoad(sprite);Sprites.Add(key,sprite);return sprite;
 }
 public static Selection For(string owner)
 {var selection=State.selections.FirstOrDefault(s=>s.owner==owner);if(selection==null){selection=new Selection{owner=owner};State.selections.Add(selection);}return selection;}
 public static bool Owned(int id)=>id==0||State.owned.Contains(id)||Items[id].kind=="bullet";
 public static Item Selected(string owner,string kind)
 {
  var selection=For(owner);int id=kind=="character"?selection.character:kind=="weapon"?selection.weapon:kind=="wing"?selection.wing:selection.bullet;
  return id!=0&&Items.TryGetValue(id,out var item)&&Owned(id)&&item.kind==kind&&(item.owner==""||item.owner==owner)?item:null;
 }
 public static bool Equip(string owner,string kind,int id)
 {
  if(id!=0&&(!Items.TryGetValue(id,out var item)||!Owned(id)||item.kind!=kind||item.owner!=""&&item.owner!=owner))return false;
  var selection=For(owner);
  if(kind=="character")selection.character=id;else if(kind=="weapon")selection.weapon=id;else if(kind=="wing")selection.wing=id;else if(kind=="bullet")selection.bullet=id;else throw new ArgumentException("Unknown appearance kind");
  RefreshData();Progression.Save();return true;
 }
 internal static void RefreshData()
 {
  foreach(var entry in originalCharacters.Where(p=>p.Key!=null))
  {
   var skin=Selected(entry.Key.name,"character");entry.Key.portrait=skin==null?entry.Value.Item1:Sprite(skin.portrait);
   var idle=skin?.clips.FirstOrDefault(c=>c.source.EndsWith("Idle"));entry.Key.icon=idle==null?entry.Value.Item2:Sprite(idle.frames.First().sprite);
  }
  foreach(var entry in originalWeapons.Where(p=>p.Key!=null)){var skin=Selected(entry.Key.name,"weapon");entry.Key.icon=skin==null?entry.Value:Sprite(skin.icon);}
  // Assets and collection outlive the title scene; its slot views do not.
  var title=Progression.Title;if(title==null)return;
  foreach(var slot in Characters.Items(title.characterMenu))CosmeticSlotImage.Bind(slot.gameObject,slot.data.name,"character");
  foreach(var slot in NativeSelection.GunItems(title.gunMenu))CosmeticSlotImage.Bind(slot.gameObject,slot.data.name,"weapon");
 }
 internal static int[] Award(string run,string character,string weapon,System.Random random)
 {
  if(!Available||State.victories.Contains(run))return new int[0];
  var awarded=new List<int>();
  var pools=new[]{Items.Values.Where(i=>(i.kind=="character"&&i.owner==character||i.kind=="wing")&&!Owned(i.id)).ToArray(),Items.Values.Where(i=>i.kind=="weapon"&&i.owner==weapon&&!Owned(i.id)).ToArray()};
  foreach(var pool in pools)if(pool.Length!=0){var chosen=pool[random.Next(pool.Length)];State.owned.Add(chosen.id);State.pending.Add(chosen.id);awarded.Add(chosen.id);}
  State.victories.Add(run);Progression.Save();return awarded.ToArray();
 }
 internal static void RecordVictory()
 {
  if(!Available||CustomMode.Active)return;
  var rewards=RuneRewardRun.Instance;rewards.Ensure();if(rewards.State==null)return;
  var ids=Award(rewards.State.id,Loadout.CharacterSelection.name,Loadout.GunSelection.name,new System.Random());Plugin.Trace("appearance-victory",new{run=rewards.State.id,awarded=ids});
 }
 internal static bool TakeNotice(out Sprite icon,out string text)
 {
  icon=null;text=null;if(!Available||State.pending.Count==0)return false;
  int id=State.pending[0];var item=Items[id];State.pending.RemoveAt(0);Progression.Save();icon=Sprite(item.icon);text="<color=#FC525F>Unlocked: "+item.name+"</color><br>Available in Appearance";return true;
 }
}

[HarmonyPatch(typeof(PlayerSurvivedState),"CheckDifficultyUnlock")]
class CosmeticVictory{static void Postfix()=>Cosmetics.RecordVictory();}
