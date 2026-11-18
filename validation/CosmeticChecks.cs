using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.EventSystems;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 int[] AppearanceAward(string run,string character,string weapon)=> (int[])AccessTools.Method(typeof(Cosmetics),"Award").Invoke(null,new object[]{run,character,weapon,new System.Random(19)});
 Transform AppearanceRoot()=>GameObject.Find("AppearanceCollection").transform;
 Button AppearanceButton(string name)=>AppearanceRoot().GetComponentsInChildren<Button>().Single(b=>b.name==name);
 IEnumerator AppearanceMenuChecks()
 {
  var title=Progression.Title;var pad=InputSystem.AddDevice<Gamepad>("AppearanceController");InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
  yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);
  while(!Active(title.characterMenu)||!title.characterMenu.interactable)
  {
   var fresh=Resources.FindObjectsOfTypeAll<Button>().SingleOrDefault(b=>b.name=="ResumeNewRun"&&b.gameObject.activeInHierarchy);
   if(fresh!=null&&fresh.IsInteractable())yield return PadClick(pad,fresh.GetComponentInParent<Canvas>().transform,fresh);
   yield return null;
  }
  var tab=title.selectPanel.GetComponent<AppearanceTab>();Require(tab!=null&&tab.Button.gameObject.activeInHierarchy,"Appearance is a real selection-screen tab beside Runes");
  title.Save();string ownership=Ownership();int credits=PointsTracker.pts;
  yield return PadClick(pad,title.selectPanel.transform,tab.Button);while(!(title.CurrentState is AppearanceState)||AppearanceMenu.Current==null)yield return null;
  yield return PadClick(pad,AppearanceRoot(),AppearanceButton("AppearanceWings"));
  Require(AppearanceMenu.Current.Kind=="wing","controller switches appearance sections");
  foreach(var id in AppearanceMenu.Current.Visible)
  {var button=AppearanceButton("AppearanceItem_"+id);yield return MenuMove(pad,AppearanceRoot(),button);Require(AppearanceMenu.Current.Selected==id,"controller previews each visible locked or original appearance");}
  int locked=AppearanceMenu.Current.Visible.First(id=>id!=0);yield return PadClick(pad,AppearanceRoot(),AppearanceButton("AppearanceItem_"+locked));
  Require(!Cosmetics.Owned(locked)&&Cosmetics.Selected("Shana","wing")==null,"browsing/confirming a locked cosmetic neither unlocks nor equips it");
  yield return Capture("appearance-locked-1280.png");
  yield return PadClick(pad,AppearanceRoot(),AppearanceButton("AppearanceNext"));Require(AppearanceMenu.Current.Visible.First()==2020,"controller reaches the second wing page without leaving the modal");
  foreach(var size in new[]{new Vector2Int(900,900),new Vector2Int(1920,1080),new Vector2Int(1280,720)})
  {
   Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);yield return null;
   var root=(RectTransform)AppearanceRoot();var panel=(RectTransform)root.Find("AppearanceOpaquePanel");var corners=new Vector3[4];panel.GetWorldCorners(corners);
   Require(corners.All(c=>root.rect.Contains(root.InverseTransformPoint(c))),"opaque appearance panel fits "+size.x+"x"+size.y);
   var preview=(RectTransform)panel.Find("AppearancePreview");preview.GetWorldCorners(corners);Require(corners.All(c=>panel.rect.Contains(panel.InverseTransformPoint(c))),"complete appearance preview stays inside the panel at "+size.x+"x"+size.y);
   foreach(var label in panel.GetComponentsInChildren<TMP_Text>())
   {label.ForceMeshUpdate();Require(label.textInfo.characterInfo.Take(label.textInfo.characterCount).Where(c=>c.isVisible).All(c=>panel.rect.Contains(panel.InverseTransformPoint(label.transform.TransformPoint(c.bottomLeft)))&&panel.rect.Contains(panel.InverseTransformPoint(label.transform.TransformPoint(c.topRight)))),"appearance text fits its panel: "+label.name);}
   yield return Capture("appearance-locked-"+size.x+".png");
  }
  yield return MenuPointer(AppearanceButton("AppearanceWeapon"));Require(AppearanceMenu.Current.Kind=="weapon","pointer switches to the actual weapon appearance section");
  yield return PadClick(pad,AppearanceRoot(),AppearanceButton("AppearanceBack"));while(!(title.CurrentState is CharacterSelectState)||!Active(title.characterMenu))yield return null;
  Require(title.selectPanel.interactable&&tab.Button.gameObject.activeInHierarchy,"Back restores the originating selector and action tab");
  yield return PadClick(pad,title.characterMenu.GetComponentInParent<Canvas>().rootCanvas.transform,Entries(title.characterMenu)[0].button);while(!(title.CurrentState is GunSelectState)||!Active(title.gunMenu))yield return null;
  yield return PadClick(pad,title.selectPanel.transform,tab.Button);while(AppearanceMenu.Current==null)yield return null;Require(AppearanceMenu.Current.Kind=="weapon","weapon selector opens its own appearance tab");
  yield return SelectorPad(pad,GamepadButton.East);while(!(title.CurrentState is GunSelectState)||!Active(title.gunMenu))yield return null;
  Require(Ownership()==ownership&&PointsTracker.pts==credits,"appearance browsing and Cancel preserve gameplay ownership and currency");
  Click(title.loadoutBackButton);while(!(title.CurrentState is CharacterSelectState)||!Active(title.characterMenu))yield return null;Click(title.loadoutBackButton);while(!Active(title.mainMenu))yield return null;
 }
 void AppearanceCollectionChecks()
 {
  // Deliberate isolated collection fixture. Direct award calls exercise the
  // finite reward contract; the separate native win below proves its wiring.
  var all=Cosmetics.Items.Values.ToArray();var chars=new HashSet<string>(Characters.Items(Progression.Title.characterMenu).Select(i=>i.data.name));var guns=new HashSet<string>(NativeSelection.GunItems(Progression.Title.gunMenu).Select(i=>i.data.name));
  Require(all.Length==147&&all.Where(i=>i.kind=="character").All(i=>chars.Contains(i.owner))&&all.Where(i=>i.kind=="weapon").All(i=>guns.Contains(i.owner)),"all 147 catalog entries have supported native owner associations");
  foreach(var group in all.Where(i=>i.kind=="character").GroupBy(i=>i.owner))
   for(int attempt=0;attempt<51;attempt++)
   {string run="qa-character-"+group.Key+"-"+attempt;var ids=AppearanceAward(run,group.Key,"RevolverData");Require(ids.Length<=2&&ids.All(id=>Cosmetics.Items[id].kind=="wing"||Cosmetics.Items[id].kind=="character"&&Cosmetics.Items[id].owner==group.Key||Cosmetics.Items[id].kind=="weapon"&&Cosmetics.Items[id].owner=="RevolverData"),"victory grants only a related character/wing and weapon skin");Require(AppearanceAward(run,group.Key,"RevolverData").Length==0,"replaying the same run cannot grant duplicate cosmetics");if(ids.Length==0)break;}
  foreach(var owner in all.Where(i=>i.kind=="weapon").Select(i=>i.owner).Distinct())
   for(int attempt=0;attempt<15;attempt++){var ids=AppearanceAward("qa-weapon-"+owner+"-"+attempt,"Shana",owner);if(ids.Length==0)break;}
  Require(all.All(i=>Cosmetics.Owned(i.id))&&Cosmetics.State.owned.Distinct().Count()==Cosmetics.State.owned.Count,"finite victory pools unlock all skins/wings without duplicates; bullet colours remain freely selectable");
  foreach(var item in all)
  {string owner=item.owner.Length==0?(item.kind=="bullet"?"RevolverData":"Shana"):item.owner;Require(Cosmetics.Equip(owner,item.kind,item.id),"each unlocked appearance can be selected: "+item.id);Require(Cosmetics.Equip(owner,item.kind,0),"each appearance can return to its original look");}
  Cosmetics.State.pending.Clear();Progression.Save();Progression.Load(true);Require(all.All(i=>Cosmetics.Owned(i.id)),"real journal reload preserves the full earned collection");
 }
 void AppearanceVisualContract(string character,int skinId,string weapon,int weaponSkin)
 {
  var player=PlayerController.Instance;var skin=Cosmetics.Items[skinId];var gunSkin=Cosmetics.Items[weaponSkin];
  Require(AppearanceRun.Instance!=null&&AppearanceRun.Instance.Character.character==skinId&&AppearanceRun.Instance.Weapon.weapon==weaponSkin,"fresh/Continue battle uses its captured character and weapon looks");
  Require(skin.clips.SelectMany(c=>c.frames).Any(f=>Cosmetics.Sprite(f.sprite)==player.playerSprite.sprite),"actual animated player renderer displays the selected skin");
  Require(player.gun.gunObj.GetComponentsInChildren<SpriteRenderer>().Any(r=>gunSkin.clips.SelectMany(c=>c.frames).Any(f=>Cosmetics.Sprite(f.sprite)==r.sprite)),"actual gun renderer displays the selected source skin");
  Require(player.GetComponentInChildren<CosmeticFrames>()!=null,"selected animated wings are attached to the player");
  var recipe=player.gun.GetProjectileRecipe();Require(recipe.damage==player.gun.gunData.damage&&player.gun.maxAmmo==player.gun.gunData.maxAmmo&&player.gun.numOfProjectiles==player.gun.gunData.numOfProjectiles,"appearance retains native damage, magazine and projectile count with an empty build");
 }
 IEnumerator AppearanceHeroActors()
 {
  foreach(var sample in new[]{new{hero="Lilith",skin=610401,actor="SpiritSummon"},new{hero="Raven",skin=610111,actor="PF_Crow"},new{hero="Luna",skin=610801,actor="PF_LunaBlackHole"}})
  {
   Progression.State.earned.Add("character:"+sample.hero);Progression.Refresh();Cosmetics.Equip(sample.hero,"character",sample.skin);Cosmetics.Equip(sample.hero,"wing",2049);Cosmetics.Equip("DualSMGsData","weapon",4101);
   yield return AchievementRun(sample.hero,"DualSMGsData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return null;
   AppearanceVisualContract(sample.hero,sample.skin,"DualSMGsData",4101);var pool=ObjectPooler.SharedInstance;
   if(!pool.itemDictionary.ContainsKey(sample.actor)){var template=Resources.FindObjectsOfTypeAll<GameObject>().First(g=>g.name==sample.actor&&!g.scene.IsValid());AccessTools.Method(typeof(AndroidPools),"Register").Invoke(pool.GetComponent<AndroidPools>(),new object[]{sample.actor,template,null});}
   var actor=pool.GetPooledObject(sample.actor);actor.transform.position=PlayerController.Instance.transform.position+Vector3.right*2;yield return null;yield return null;
   var skin=Cosmetics.Items[sample.skin];Require(actor.GetComponentsInChildren<SpriteRenderer>().Any(r=>skin.summonSprite!=null&&r.sprite==Cosmetics.Sprite(skin.summonSprite)||skin.summonClips.SelectMany(c=>c.frames).Any(f=>Cosmetics.Sprite(f.sprite)==r.sprite)),"actual pooled hero actor displays its source skin: "+sample.actor);
   ReturnFixtureActor(actor);var reused=pool.GetPooledObject(sample.actor);yield return null;yield return null;Require(reused==actor,"actual pool returns the same hero actor instance");
   Require(reused.GetComponentsInChildren<SpriteRenderer>().Any(r=>skin.summonSprite!=null&&r.sprite==Cosmetics.Sprite(skin.summonSprite)||skin.summonClips.SelectMany(c=>c.frames).Any(f=>Cosmetics.Sprite(f.sprite)==r.sprite)),"pool reuse retains hero appearance: "+sample.actor);
   if(sample.hero=="Luna")
   {
    const string effect="PF_LunaBlackHoleDamage";if(!pool.itemDictionary.ContainsKey(effect)){var template=Resources.FindObjectsOfTypeAll<GameObject>().First(g=>g.name==effect&&!g.scene.IsValid());AccessTools.Method(typeof(AndroidPools),"Register").Invoke(pool.GetComponent<AndroidPools>(),new object[]{effect,template,null});}
    var burst=pool.GetPooledObject(effect);burst.transform.position=PlayerController.Instance.transform.position+Vector3.left*2;yield return null;
    Require(burst.GetComponentsInChildren<SpriteFX>().Any(fx=>((Sprite[])WindField(fx,"sprites")).SequenceEqual(skin.summonFrames.Select(Cosmetics.Sprite))),"Luna's damage animation uses her complete source skin frame sequence");
   }
   yield return Capture("appearance-"+sample.hero.ToLowerInvariant()+"-actor.png");yield return AchievementExit(Object.FindObjectOfType<GameController>(),false);
  }
 }
 IEnumerator AppearanceBroom()
 {
  Progression.State.earned.Add("weapon:SwordData");Progression.Refresh();Cosmetics.Equip("Abby","character",610101);Cosmetics.Equip("Abby","wing",2048);Cosmetics.Equip("SwordData","weapon",4809);Cosmetics.Equip("SwordData","bullet",202);
  yield return AchievementRun("Abby","SwordData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return null;
  var player=PlayerController.Instance;AppearanceVisualContract("Abby",610101,"SwordData",4809);
  var actor=ObjectPooler.SharedInstance.GetPooledObject(Loadout.GunSelection.bullet.name);actor.transform.position=player.transform.position+Vector3.right*2;yield return null;yield return null;
  Require(actor.GetComponentsInChildren<Transform>().Any(t=>t.name=="CosmeticProjectileArtwork"),"Wondrous Broom carries its source projectile artwork into the real native pool");
  Require(actor.GetComponentsInChildren<SpriteRenderer>().Any(r=>Cosmetics.Items[4809].projectileVisuals.Any(p=>Cosmetics.Sprite(p.sprite)==r.sprite)),"custom source projectile texture is actually rendered");
  yield return Capture("appearance-broom.png");ReturnFixtureActor(actor);
  var game=Object.FindObjectOfType<GameController>();PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.giveupButton);while(!(game.CurrentState is PlayerDeadState)||!game.retryRunButton.IsInteractable())yield return null;
  int previous=game.GetInstanceID();Click(game.retryRunButton);while((game=Object.FindObjectOfType<GameController>())==null||game.GetInstanceID()==previous||!(game.CurrentState is CombatState))yield return null;
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return null;AppearanceVisualContract("Abby",610101,"SwordData",4809);yield return AchievementExit(game,false);
 }
 IEnumerator AppearanceBowGround()
 {
  Progression.State.earned.Add("weapon:MagicBowData");Progression.Refresh();Cosmetics.Equip("MagicBowData","weapon",4704);
  yield return AchievementRun("Abby","MagicBowData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return null;
  var source=Loadout.GunSelection.bullet.GetComponent<SpawnOnDisableProjectile>();var prefab=(GameObject)AccessTools.Field(typeof(SpawnOnDisableProjectile),"prefab").GetValue(source);
  var pool=ObjectPooler.SharedInstance;if(!pool.itemDictionary.ContainsKey(prefab.name))AccessTools.Method(typeof(AndroidPools),"Register").Invoke(pool.GetComponent<AndroidPools>(),new object[]{prefab.name,prefab,null});
  var arrow=pool.GetPooledObject(prefab.name);arrow.transform.position=PlayerController.Instance.transform.position+Vector3.right*2;yield return null;yield return null;
  Require(arrow.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.sprite==Cosmetics.Sprite(Cosmetics.Items[4704].groundSprite)),"Magic Bow ground arrows carry the selected source skin through the actual drop pool");ReturnFixtureActor(arrow);
  yield return AchievementExit(Object.FindObjectOfType<GameController>(),false);
 }
 IEnumerator CheckCosmetics(string mode)
 {
  Require(Cosmetics.Available,"locally prepared appearance catalog is loaded");
  if(mode=="cosmetics-continue")
  {
   yield return CombatBuffContinue();StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return null;
   AppearanceVisualContract("Abby",610101,"DualSMGsData",4211);Require(Cosmetics.Selected("Abby","character")==null,"Continue keeps saved run art even after the title profile selects Original");
   yield return Capture("appearance-continue.png");
   var game=Object.FindObjectOfType<GameController>();yield return ArrowEvolution(game,PlayerController.Instance.gun.gunData.gunEvolutions[0].name);yield return null;
   Require(PlayerController.Instance.gun.gunObj.GetComponentsInChildren<SpriteRenderer>().Any(r=>Cosmetics.Items[4211].clips.SelectMany(c=>c.frames).Any(f=>Cosmetics.Sprite(f.sprite)==r.sprite)),"native weapon evolution retains its selected skin artwork");
   yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Actual Continue retains captured appearances independently of later menu selections; native evolution preserves selected gun artwork");Application.Quit(0);yield break;
  }
  bool saveOnly=mode=="cosmetics-save",renderOnly=mode=="cosmetics-render";
  if(saveOnly){Cosmetics.State.owned=Cosmetics.Items.Values.Where(i=>i.kind!="bullet").Select(i=>i.id).ToList();Progression.Save();}
  else if(renderOnly)
  {
   // Separate render fixture: finite collection and real victory acquisition
   // already passed in run-3. Do not replay those awards to prepare artwork.
   var earned=Cosmetics.Items.Values.Where(i=>i.kind!="bullet").Select(i=>i.id).ToList();
   Cosmetics.State.owned.Clear();AccessTools.Method(typeof(Cosmetics),"RefreshData").Invoke(null,null);
   try{yield return AppearanceMenuChecks();}
   finally{Cosmetics.State.owned=earned;AccessTools.Method(typeof(Cosmetics),"RefreshData").Invoke(null,null);Progression.Save();}
  }
  else
  {Progression.State.cosmetics=new Cosmetics.Collection();Progression.Save();yield return AppearanceMenuChecks();AppearanceCollectionChecks();Cosmetics.State.owned.Remove(610101);Cosmetics.State.owned.Remove(4211);Progression.Save();}
  Progression.State.earned.Add("character:Abby");Progression.State.earned.Add("weapon:DualSMGsData");Progression.Refresh();
  if(!renderOnly&&!saveOnly)
  {yield return AchievementRun("Abby","DualSMGsData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());StopFixtureSpawning();GameTimer.SharedInstance.Stop();var game=Object.FindObjectOfType<GameController>();yield return AchievementWin(game);Require(Cosmetics.Owned(610101)&&Cosmetics.Owned(4211),"actual native successful run awards the remaining related character and weapon skins");}
  if(!saveOnly){yield return AppearanceHeroActors();yield return AppearanceBroom();yield return AppearanceBowGround();}
  Cosmetics.State.pending.Clear();Require(Cosmetics.Equip("Abby","character",610101)&&Cosmetics.Equip("Abby","wing",2048)&&Cosmetics.Equip("DualSMGsData","weapon",4211)&&Cosmetics.Equip("DualSMGsData","bullet",202),"earned character, wing, weapon and bullet choices save");
  yield return AchievementRun("Abby","DualSMGsData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());StopFixtureSpawning();GameTimer.SharedInstance.Stop();yield return null;
  AppearanceVisualContract("Abby",610101,"DualSMGsData",4211);yield return Capture("appearance-abby-wondrous.png");
  if(!ColorfulStyle.Instance.Enabled)ColorfulStyle.Instance.Toggle();yield return null;AppearanceVisualContract("Abby",610101,"DualSMGsData",4211);
  RunResume.Save();Require(RunResume.Read().appearance.character.character==610101,"actual Continue checkpoint captures run appearance");Cosmetics.Equip("Abby","character",0);Cosmetics.Equip("DualSMGsData","weapon",0);
  yield return DragonSuspend(Object.FindObjectOfType<GameController>(),"Appearance collection/controller/pointer/responsive UI, finite related reward pools, real victory grants, real journal reload, source animation, Reborn compatibility and run checkpoint.");
 }
}
