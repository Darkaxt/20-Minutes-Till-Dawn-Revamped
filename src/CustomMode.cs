using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// Custom owns run-local values. Native content, stat calculations, pools,
// acquisition limits, controllers and score/credit accounting remain native.
public static class CustomMode
{
 [Serializable]public class Value{public string key;public float value;}
 [Serializable]public class Rules{public int version=1;public string map="20M_Forest";public List<Value> values=new List<Value>();}
 public static MapData MenuMap,RunMap;
 public static Rules Current;
 internal static Rules NextRun;
 static readonly string[] MapNames={"20M_Forest","20M_Temple","20M_PumpkinPatch","10M_Forest","Endless_Forest","Challenge_Forest"};
 static readonly Dictionary<string,MapData> Sources=new Dictionary<string,MapData>();
 public static bool Active=>Current!=null&&RunMap!=null&&SelectedMap.MapData==RunMap;
 public static bool Selected=>Progression.Title!=null&&Progression.Title.modeSelectMenu.toggledData==MenuMap&&MenuMap!=null;
 public static readonly string[] WeaponFields={"damage","shotCooldown","maxAmmo","reloadDuration","numOfProjectiles","spread","knockback","projectileSpeed","bounce","piercing","inaccuracy"};
 public static readonly string[] SpawnFields={"spawnRateMulitplier","healthMultiplier","eliteHealthMultiplier","speedMultiplier","eliteSpeedMultiplier","enemyDamage","eliteDamage","bossHealth","bossDamage","bossCooldown"};
 static readonly Dictionary<GunData,GunData> Guns=new Dictionary<GunData,GunData>();
 static readonly HashSet<GunData> Copies=new HashSet<GunData>();
 public static float Get(string key,float original=1)=>Current?.values.FirstOrDefault(v=>v.key==key)?.value??original;
 public static void Reset(string prefix)
 {
  foreach(var entry in Current.values.Where(v=>v.key.StartsWith(prefix)).ToArray())
  {if(Active&&entry.key.StartsWith("perk:"))CustomPerkStats.ChangeScale(entry.key,1);Current.values.Remove(entry);}
  if(Active){RefreshWeapons();foreach(StatType type in Enum.GetValues(typeof(StatType)))if(type<StatType.Count)PlayerController.Instance.stats[type].AddFlatBonus(0);StatsOverlay.Instance?.Dirty();}
 }
 public static void Set(string key,float value)
 {
  if(Current==null||float.IsNaN(value)||float.IsInfinity(value))throw new InvalidOperationException("Invalid Custom value");
  if(Active&&key.StartsWith("perk:"))CustomPerkStats.ChangeScale(key,value);
  var item=Current.values.FirstOrDefault(v=>v.key==key);if(item==null){item=new Value{key=key};Current.values.Add(item);}item.value=value;
  if(Active){RefreshWeapons();StatsOverlay.Instance?.Dirty();var p=PlayerController.Instance;if(p!=null)foreach(StatType type in Enum.GetValues(typeof(StatType)))if(type<StatType.Count)p.stats[type].AddFlatBonus(0);}
 }
 public static void Validate(Rules rules)
 {
  if(rules==null)return;
  if(rules.version!=1||string.IsNullOrEmpty(rules.map)||rules.values==null||rules.values.Any(v=>v==null||string.IsNullOrEmpty(v.key)||float.IsNaN(v.value)||float.IsInfinity(v.value))||rules.values.Select(v=>v.key).Distinct().Count()!=rules.values.Count)
   throw new InvalidOperationException("Unsupported Custom settings; checkpoint preserved.");
  foreach(var v in rules.values)
  {
   bool stat=v.key.StartsWith("stat:")&&Enum.TryParse(v.key.Substring(5),out StatType type)&&type<StatType.Count;
   bool weapon=v.key.StartsWith("weapon:")&&WeaponFields.Contains(v.key.Substring(7));
   bool spawns=v.key.StartsWith("spawn:")&&SpawnFields.Contains(v.key.Substring(6));
   bool summon=v.key.StartsWith("summon:")&&(v.key.EndsWith(":damage")||v.key.EndsWith(":speed"));
   bool perk=v.key.StartsWith("perk:")&&v.key.EndsWith(":scale");
   if(!(stat||weapon||spawns||summon||perk||v.key=="xp")||v.value<0||v.value>10000||(weapon&&(v.key.EndsWith("shotCooldown")||v.key.EndsWith("reloadDuration"))&&v.value<.05f))
    throw new InvalidOperationException("Invalid Custom setting: "+v.key);
  }
 }
 public static void Append(flanne.UI.GameModeMenu menu)
 {
  foreach(string name in MapNames){var source=Resources.FindObjectsOfTypeAll<MapData>().FirstOrDefault(m=>m.name==name&&m!=RunMap);if(source!=null)Sources[name]=source;}
  var toggles=ChallengeMode.Toggles(menu);var found=toggles.FirstOrDefault(t=>t.GetComponent<flanne.UI.MapEntry>().data.name=="RevampedCustomMode");
  if(found!=null){MenuMap=found.GetComponent<flanne.UI.MapEntry>().data;return;}
  if(MenuMap==null){MenuMap=Object.Instantiate(ChallengeMode.Map);MenuMap.name="RevampedCustomMode";Object.DontDestroyOnLoad(MenuMap);MenuMap.nameStringID="revamped_custom_name";MenuMap.descriptionStringID="revamped_custom_description";}
  Plugin.Text["revamped_custom_name"]="Custom";Plugin.Text["revamped_custom_description"]="Choose your rules. Earn score; unlocks and achievements are disabled. Only obtained content is available.";
  var copy=Object.Instantiate(toggles.Last().gameObject,toggles.Last().transform.parent);copy.name="RevampedCustomMode";var toggle=copy.GetComponent<Toggle>();toggle.isOn=false;copy.GetComponent<flanne.UI.MapEntry>().data=MenuMap;
  Plugin.SetField(menu,"toggles",toggles.Concat(new[]{toggle}).ToArray());
  var element=copy.GetComponent<LayoutElement>()??copy.AddComponent<LayoutElement>();element.minWidth=0;element.preferredWidth=1;element.flexibleWidth=1;
  toggle.onValueChanged.AddListener(_=>ChallengeMode.RefreshDifficulty());
 }
 public static MapData[] Maps()=>MapNames.Where(Sources.ContainsKey).Select(n=>Sources[n]).Where(m=>m!=null&&Progression.Allowed(m)).ToArray();
 public static string ModeName(MapData map)=>map.name=="20M_Forest"?"Standard — Forest":map.name=="20M_Temple"?"Standard — Temple":map.name=="20M_PumpkinPatch"?"Standard — Pumpkin Patch":map.name=="10M_Forest"?"Quickplay":map.name=="Endless_Forest"?"Endless":"Challenge";
 public static void LoadDefaults(MapData source)
 {
  if(source==null||!Maps().Contains(source))throw new InvalidOperationException("This mode has not been obtained.");
  if(Active)
  {if(Current.map!=source.name)throw new InvalidOperationException("Changing modes starts a new Custom run.");Reset("");}
  else{Current=new Rules{map=source.name};Loadout.difficultyLevel=0;}
 }
 public static void RestartWithDefaults(MapData source)
 {
  if(!Active||!Maps().Contains(source))throw new InvalidOperationException("This mode has not been obtained.");
  NextRun=new Rules{map=source.name};RunRecorder.Instance.Finished=true;RunResume.Invalidate();
  Object.FindObjectOfType<GameController>().ChangeState<TransitionToRetryState>();
 }
 internal static void PrepareRetry()
 {
  if(!Active)return;
  bool preset=NextRun!=null;
  var rules=NextRun??new Rules{map=Current.map,values=Current.values.Where(v=>!v.key.StartsWith("perk:")&&!v.key.StartsWith("summon:")).Select(v=>new Value{key=v.key,value=v.value}).ToList()};
  NextRun=null;if(preset)Loadout.difficultyLevel=0;Prepare(rules,Maps().Single(m=>m.name==rules.map));
 }
 public static void Prepare(Rules rules,MapData source)
 {
  Validate(rules);
  if(source==null||!Progression.Allowed(source)||!Progression.Allowed(Loadout.CharacterSelection)||!Progression.Allowed(Loadout.GunSelection))throw new InvalidOperationException("This save has not obtained the selected content.");
  Loadout.difficultyLevel=Mathf.Clamp(Loadout.difficultyLevel,0,source.name=="Challenge_Forest"?Progression.State.challengeDifficultyUnlocked:SaveSystem.data.difficultyUnlocked);
  Current=rules;RunMap=Object.Instantiate(source);RunMap.name=source.name;RunMap.achievementsDisabled=true;SelectedMap.MapData=RunMap;
 }
 public static void End(MapData ended)
 {foreach(var copy in Copies)Object.Destroy(copy);Copies.Clear();Guns.Clear();Object.Destroy(ended);if(RunMap==ended){RunMap=null;Current=null;NextRun=null;}}
 internal static GunData Weapon(GunData data)
 {
  if(!Active||data==null||Copies.Contains(data))return data;
  if(!Guns.TryGetValue(data,out var copy)){copy=Object.Instantiate(data);copy.name=data.name;Guns.Add(data,copy);Copies.Add(copy);}
  RefreshWeapon(data,copy);return copy;
 }
 static void RefreshWeapons(){foreach(var pair in Guns)RefreshWeapon(pair.Key,pair.Value);}
 static void RefreshWeapon(GunData data,GunData copy)
 {
  foreach(string field in WeaponFields)
  {
   var f=AccessTools.Field(typeof(GunData),field);var setting=Current.values.FirstOrDefault(v=>v.key=="weapon:"+field);
   if(setting!=null)f.SetValue(copy,Convert.ChangeType(setting.value,f.FieldType));else f.SetValue(copy,f.GetValue(data));
  }
 }
 internal static float StatFactor(StatMod mod)
 {
  if(!Active)return 1;var p=PlayerController.Instance;if(p==null)return 1;
  for(int i=0;i<(int)StatType.Count;i++)if(ReferenceEquals(p.stats[(StatType)i],mod))return Get("stat:"+(StatType)i);
  var xp=p.GetComponentInChildren<flanne.Player.PlayerXP>();if(xp!=null&&ReferenceEquals(xp.xpMultiplier,mod))return Get("xp");return 1;
 }
 public static List<Tuple<string,Powerup>> AvailablePerks()
 {
  var result=new List<Tuple<string,Powerup>>();var g=PowerupGenerator.Instance;if(g==null)return result;
  foreach(string field in new[]{"powerupPool","characterPool","devilPool"})
  foreach(var item in (List<PowerupPoolItem>)AccessTools.Field(typeof(PowerupGenerator),field).GetValue(g)??new List<PowerupPoolItem>())
   if(item.numTimeRepeatable!=0&&(bool)AccessTools.Method(typeof(PowerupGenerator),"PrereqsMet").Invoke(g,new object[]{item.powerup}))result.Add(Tuple.Create(field,item.powerup));
  return result;
 }
 public static void Grant(string pool,string key)
 {
  if(!Active)throw new InvalidOperationException("Custom run required");
  var item=AvailablePerks().FirstOrDefault(x=>x.Item1==pool&&RunResume.Key(x.Item2)==key);if(item==null)throw new InvalidOperationException("This upgrade is unavailable or its prerequisites are not met.");
  PlayerController.Instance.playerPerks.Equip(item.Item2);
  string method=pool=="powerupPool"?"RemoveFromPool":pool=="characterPool"?"RemoveFromCharacterPool":"RemoveFromDevilPool";
  AccessTools.Method(typeof(PowerupGenerator),method).Invoke(PowerupGenerator.Instance,new object[]{item.Item2});StatsOverlay.Instance?.Dirty();
 }
 public static void Spawn(string name)
 {
  if(!Active)throw new InvalidOperationException("Custom run required");
  var p=PlayerController.Instance;var map=SelectedMap.MapData;
  var source=Object.FindObjectOfType<AndroidEndlessRun>();
  var enemy=map.spawnSessions.FirstOrDefault(w=>w.monsterPrefab.name==name);
  if(enemy!=null)
  {source.SpawnActor(name,enemy.HP,enemy.isElite);return;}
  var boss=map.bossSpawns.FirstOrDefault(b=>b.bossPrefab.name==name);
  if(boss!=null)
  {source.SpawnScheduledBoss(map.bossSpawns.IndexOf(boss),false);return;}
  var pickup=Resources.FindObjectsOfTypeAll<flanne.Pickups.HPPickup>().Cast<Component>().Concat(Resources.FindObjectsOfTypeAll<flanne.Pickups.SHPPickup>().Cast<Component>()).FirstOrDefault(c=>!c.gameObject.scene.IsValid()&&c.name==name);
  if(pickup==null)throw new InvalidOperationException("Pickup unavailable");var pool=ObjectPooler.SharedInstance;pool.AddObject(name,pickup.gameObject,1);var obj=pool.GetPooledObject(name);obj.transform.position=p.transform.position+Vector3.right*3;obj.SetActive(true);
 }
}
[HarmonyPatch(typeof(ModeSelectState),nameof(ModeSelectState.OnClickPlay))]
class CustomEntry{static bool Prefix(){if(!CustomMode.Selected)return true;Progression.Title.ChangeState<CustomSetupState>();return false;}}
class CustomSetupState:TitleScreenState
{
 public override void Enter(){CustomMode.Current=new CustomMode.Rules();CustomMenu.Show(Progression.Title.transform,Progression.Title.input,()=>owner.ChangeState<ModeSelectState>(),()=>{var map=CustomMode.Maps().Single(m=>m.name==CustomMode.Current.map);CustomMode.Prepare(CustomMode.Current,map);owner.ChangeState<WaitToLoadIntoBattleState>();Progression.Title.selectPanel.Hide();Progression.Title.gunMenu.Hide();});}
 public override void Exit(){CustomMenu.Close();}
}
class CustomSettingsState:GameState
{
 // PauseState already owns the pause hold; its Options/Synergy submenus use
 // that same hold until the native Resume action releases it.
 public override void Enter(){CustomMenu.Show(owner.transform,owner.playerInput.actions,()=>owner.ChangeState<PauseState>(),null);}
 public override void Exit(){CustomMenu.Close();}
}
[HarmonyPatch(typeof(Gun),nameof(Gun.LoadGun))]
class CustomWeapon{static void Prefix(ref GunData gunToLoad){gunToLoad=CustomMode.Weapon(gunToLoad);}}
[HarmonyPatch(typeof(StatMod),nameof(StatMod.Modify))]
class CustomStat{static void Postfix(StatMod __instance,ref float __result){__result*=CustomMode.StatFactor(__instance);}}
[HarmonyPatch(typeof(StatMod),nameof(StatMod.ModifyInverse))]
class CustomStatInverse{static void Postfix(StatMod __instance,ref float __result){__result/=Mathf.Max(.01f,CustomMode.StatFactor(__instance));}}
[HarmonyPatch(typeof(SteamIntegration),nameof(SteamIntegration.UnlockAchievement))]
class CustomNoSteamAchievement{static bool Prefix()=>!CustomMode.Active;}
[HarmonyPatch(typeof(flanne.UI.LeaderboardUI),"SubmitAndShowsync")]
class CustomNoLeaderboard{static bool Prefix()=>!CustomMode.Active;}
[HarmonyPatch(typeof(TransitionToRetryState),nameof(TransitionToRetryState.Enter))]
class CustomRetry{static void Prefix(){CustomMode.PrepareRetry();}}
[HarmonyPatch(typeof(GameController),"Awake")]
class CustomLabelLifetime{static void Postfix(GameController __instance){if(CustomMode.Active)__instance.gameObject.AddComponent<CustomRunLabel>();}}
class CustomRunLabel:MonoBehaviour
{
 GameObject root;
 void Start()
 {
  root=new GameObject("CustomRunBadge",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));root.transform.SetParent(transform,false);
  root.AddComponent<GameplayHudOpacity>();
  root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;root.GetComponent<Canvas>().sortingOrder=19;
  var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
  var font=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().First(t=>t.font!=null&&t.gameObject.scene.IsValid()).font;
  var label=StatsOverlay.Text(root.transform,"CustomCreditsOnly",font,"CUSTOM · Score only",17,Vector2.zero,new Vector2(300,26));label.alignment=TMPro.TextAlignmentOptions.Center;label.color=StatsOverlay.Green;
  StatsOverlay.Rect(label.gameObject,root.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-4),new Vector2(300,26));
 }
 void Update(){if(root!=null)root.SetActive(GetComponent<GameController>().CurrentState is CombatState&&!PauseController.isPaused);}
}
