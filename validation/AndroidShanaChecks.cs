using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.UI;
using flanne.PowerupSystem;
using PlayerXP=flanne.Player.PlayerXP;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static bool ShanaMode=>Environment.GetEnvironmentVariable("MODERNIZATION_TEST").StartsWith("resume-shana-");
 internal static bool ShanaRearActive;
 internal static readonly List<Vector2> ShanaRearDirections=new List<Vector2>();
 internal static int ShanaActivations,ShanaSounds;
 internal static void ShanaActivation(ShanasHalo halo)
 {
  if(!ShanaMode)return;ShanaActivations++;var p=PlayerController.Instance;
  CombatClose(Bonus(p.stats[StatType.FireRate]),.5f,"Halo activation follows its source Fire Rate stats");
  CombatClose(Bonus(p.stats[StatType.ReloadRate]),.5f,"Halo activation follows its source Reload Rate stats");
  CombatClose(Bonus(p.stats[StatType.VisionRange]),.75f,"Halo activation follows its source vision stats");
 }
 static ShanasHalo ShanaHalo()=>PlayerController.Instance.GetComponentInChildren<ShanasHalo>();
 static bool ShanaSprite()=>((GameObject)WindField(ShanaHalo(),"haloSpriteObj")).activeSelf;
 static bool ShanaMeets(GameController game,Powerup power)=>(bool)AccessTools.Method(typeof(PowerupGenerator),"PrereqsMet").Invoke(game.powerupGenerator,new object[]{power});
 static int ShanaCount=>(int)AccessTools.Property(AccessTools.TypeByName("TwentyMinutes.Modernization.AndroidSpecialize"),"Count").GetValue(null);
 static bool ShanaSpent(TripleNextPowerup owner)=>(bool)WindField(owner,"used");
 static List<PowerupPoolItem> ShanaPool(GameController game)=>(List<PowerupPoolItem>)WindField(game.powerupGenerator,"powerupPool");
 static int ShanaApplications(string key)=>((List<RunResume.Acquisition>)WindField(AccessTools.Field(AccessTools.TypeByName("TwentyMinutes.Modernization.RunRecorder"),"Instance").GetValue(null),"Acquired")).Count(a=>a.key==key);
 static TripleNextPowerup[] ShanaSpecializes()=>PlayerController.Instance.GetComponentsInChildren<TripleNextPowerup>();
 static void ShanaUsed(params bool[] used)
 {
  var providers=ShanaSpecializes();Require(providers.Length==used.Length&&providers.Select(t=>ShanaSpent(t)).SequenceEqual(used),"source Specialize retained used flags: "+string.Join(",",used));
  Require(ShanaCount==used.Count(value=>!value),"Specialize exclusion counts only pending unused helpers after Start and consumption");
  Require(CombatValues("shana_perk_specialize_name").Select(v=>v.count).SequenceEqual(used.Select(b=>b?0:1)),"source attachment writer captures independent Specialize used values");
 }
 IEnumerator ShanaPick(GameController game,string key,bool reroll=false)
 {
  var field=AccessTools.Field(typeof(PowerupGenerator),"powerupPool");var pool=(List<PowerupPoolItem>)field.GetValue(game.powerupGenerator);var desired=pool.Single(i=>RunResume.Key(i.powerup)==key);
  Require(ShanaMeets(game,desired.powerup),"source prerequisites before native Shana choice: "+key);
  var choices=pool.Where(i=>i!=desired&&i.powerup.prereqs.Count==0&&ShanaMeets(game,i.powerup)).Take(game.numPowerupChoices-1).Concat(new[]{desired}).ToList();
  Require(choices.Count==game.numPowerupChoices,"controlled eligible native offer pool has the full requested number of distinct choices");field.SetValue(game.powerupGenerator,choices);
  game.playerXP.GainXP((float)AccessTools.Property(typeof(PlayerXP),"xpToLevel").GetValue(game.playerXP)+1);
  while(!(game.CurrentState is PowerupMenuState)||WindField(game.powerupMenu,"ConfirmEvent")==null)yield return null;
  var rerollButton=(Button)AccessTools.Property(typeof(GameState),"powerupRerollButton").GetValue(game.CurrentState);
  while(!rerollButton.gameObject.activeInHierarchy||!rerollButton.IsInteractable())yield return null;
  if(reroll){Click(rerollButton);while(rerollButton.gameObject.activeInHierarchy)yield return null;Require(PowerupGenerator.CanReroll,"one native reroll consumes this menu use without removing Shana's capability");}
  var toggles=(Toggle[])WindField(game.powerupMenu,"toggles");var target=toggles.Take(game.numPowerupChoices).Single(t=>t.GetComponent<DataUIBinding<Powerup>>().data==desired.powerup);target.isOn=true;
  Button confirm=null;while(confirm==null||!confirm.IsInteractable()){confirm=Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(b=>b.gameObject.activeInHierarchy&&Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentTarget(i)==game.powerupMenu&&b.onClick.GetPersistentMethodName(i)=="OnConfirmClicked"));yield return null;}
  Click(confirm);while(!(game.CurrentState is CombatState)&&!(game.CurrentState is ShanaHaloState))yield return null;field.SetValue(game.powerupGenerator,pool.Where(i=>i.numTimeRepeatable!=0).ToList());
 }
 IEnumerator ShanaRear(GameController game,int count,float spread)
 {
  var rear=PlayerController.Instance.GetComponentsInChildren<ShootBulletsBehind>().Single();CombatClose(Convert.ToSingle(WindField(rear,"numOfBullets")),count,"source rear-shot count");CombatClose((float)WindField(rear,"spread"),spread,"source rear-shot spread");
  ShanaRearDirections.Clear();yield return MothShots(game,1);Require(ShanaRearDirections.Count==count,"real native gun input produces exactly the source rear shots");
  for(int i=1;i<count;i++)CombatClose(Mathf.Abs(Vector2.SignedAngle(ShanaRearDirections[i-1],ShanaRearDirections[i])),spread/count,"source rear shots use i/count angular spacing");
 }
 IEnumerator CheckAndroidShana(string mode)
 {
  bool fresh=mode.EndsWith("save"),final=mode.Contains("final");GameController game;
  if(fresh){RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();}
  else{yield return CombatBuffContinue();game=Object.FindObjectOfType<GameController>();}
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();var p=PlayerController.Instance;p.SetPosition(new Vector3(100,100,0));Require(PowerupGenerator.CanReroll,"native Shana source passive enables reroll");
  Log("fixture-precondition","Actual native level-up/reroll/chest/halo buttons, gun input and Halo pickup collider. Chest offer notification and eligible offer pools are controlled; enemy scheduling/clock stopped, no runes, earned Endless access seeded. No synthetic gun/Halo pickup/application event or natural encounter-frequency claim.");
  if(fresh)
  {
   Require(SelectedMap.MapData.numPowerupsRepeat>=3,"source Endless character pool permits three Specialize acquisitions independent of isRepeatable");
   yield return AndroidWhirlwindChest(game,"shana_perk_quick_learner_name");CombatClose(game.playerXP.xpMultiplier.Modify(1),1.4f,"Quick Learner adds the source forty-percent XP");
   float before=(float)WindField(game.playerXP,"xp");game.playerXP.GainXP(1);CombatClose((float)WindField(game.playerXP,"xp")-before,1.4f,"actual XP consumer uses Quick Learner");
   yield return ShanaPick(game,"double_shot_name");var assassin=ShanaPool(game).Single(i=>i.powerup.name=="Assassin").powerup;
   foreach(var prerequisite in assassin.prereqs)if(!((List<Powerup>)WindField(game.powerupGenerator,"takenPowerups")).Contains(prerequisite))yield return AndroidPick(game,prerequisite.name,game.playerXP.level);
   Require(ShanaMeets(game,assassin),"Assassin's actual prerequisites are satisfied before Specialize");
   yield return AndroidWhirlwindChest(game,"shana_perk_specialize_name");ShanaUsed(false);yield return ShanaPick(game,"split_fire_name",true);ShanaUsed(true);Require(ShanaApplications("split_fire_name")==1,"Specialize modifies the already-created Split Fire instead of journaling two extra Equip calls");yield return ShanaRear(game,3,60);
   Require(ShanaMeets(game,assassin),"consumed Specialize releases Assassin exclusion");
   yield return AndroidWhirlwindChest(game,"shana_perk_specialize_name");ShanaUsed(true,false);int applications=ShanaApplications("vitality_name");yield return ShanaPick(game,"vitality_name",true);ShanaUsed(true,true);
   Require(ShanaApplications("vitality_name")==applications+3&&p.playerHealth.maxHP==7,"ordinary next upgrade applies three times through actual native Equip, reaching seven hearts");
   yield return AndroidWhirlwindChest(game,"shana_perk_ascension_name");var pool=ShanaPool(game);var pieces=pool.Where(i=>RunResume.Key(i.powerup).StartsWith("shana_halo_piece_")).ToArray();
   Require(pieces.Select(i=>RunResume.Key(i.powerup)).SequenceEqual(new[]{"shana_halo_piece_1","shana_halo_piece_2","shana_halo_piece_3"})&&pieces.All(i=>!((Dictionary<Powerup,int>)WindField(game.powerupGenerator.GetComponent<AndroidPowerupCounts>(),"Counts")).ContainsKey(i.powerup)),"source Ascension appends three ordered pieces without registering repeat dictionary entries");
   yield return ShanaPick(game,"shana_halo_piece_1");yield return ShanaPick(game,"shana_halo_piece_2");Require(Convert.ToInt32(WindField(ShanaHalo(),"counter"))==2&&!ShanaSprite(),"two actual pieces create one incomplete Halo with inactive sprite");
   yield return DragonSuspend(game,"Complete Shana native reroll/Quick Learner/Specialize normal and Split Fire branches/Ascension/partial Halo source checkpoint");yield break;
  }
  CombatClose(game.playerXP.xpMultiplier.Modify(1),1.4f,"source Continue rebuilds Quick Learner exactly once");Require(p.playerHealth.maxHP==7,"source Continue rebuilds triple Vitality exactly once");yield return ShanaRear(game,1,0);
  if(!final)
  {
   ShanaUsed(true,true);Require(Convert.ToInt32(WindField(ShanaHalo(),"counter"))==2&&!ShanaSprite(),"exact partial Halo checkpoint reconstructs two pieces with inactive sprite");
   ShanaActivations=ShanaSounds=0;yield return ShanaPick(game,"shana_halo_piece_3");
   if(!(game.CurrentState is ShanaHaloState)){var pickup=Object.FindObjectOfType<flanne.Pickups.HaloPickup>();Require(pickup!=null,"third real piece drops the native Halo pickup");CombatClose(Vector3.Distance(pickup.transform.position,p.transform.position+Vector3.down*1.5f),0,"source Halo drop offset");pickup.transform.position=p.transform.position;Physics2D.SyncTransforms();}
   while(!(game.CurrentState is ShanaHaloState))yield return null;Require(!ShanaSprite(),"actual Halo pickup/menu does not activate before taking it");while(!game.takeHaloButton.IsInteractable())yield return null;Click(game.takeHaloButton);while(!(game.CurrentState is CombatState))yield return null;
   Require(ShanaSprite()&&ShanaActivations==1&&ShanaSounds==2,"complete Halo source flow drops once, then activates sprite and sound once after stats");
   yield return AndroidWhirlwindChest(game,"shana_perk_specialize_name");ShanaUsed(true,true,false);yield return DragonSuspend(game,"Native third-piece physical pickup/Halo menu/source Apply and mixed spent/pending Specialize checkpoint");yield break;
  }
  ShanaUsed(true,true,false);Require(ShanaSprite(),"completed Halo source replay activates the actual sprite");CombatClose(Bonus(p.stats[StatType.FireRate]),.5f,"completed Halo source replay applies stats once");
  int old=ShanaApplications("vitality_name");yield return ShanaPick(game,"vitality_name",true);ShanaUsed(true,true,true);Require(p.playerHealth.maxHP==10&&ShanaApplications("vitality_name")==old+3,"restored unused Specialize consumes exactly one real upgrade with three applications");
  var blocked=Resources.FindObjectsOfTypeAll<Powerup>().First(i=>i.name=="Assassin");Require(ShanaMeets(game,blocked),"all three consumed helpers release Assassin exclusion after Continue");
  yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());game=Object.FindObjectOfType<GameController>();p=PlayerController.Instance;StopFixtureSpawning();
  Require(ShanaSpecializes().Length==0&&ShanaCount==0&&ShanaHalo()==null&&p.GetComponentInChildren<ShootBulletsBehind>()==null&&p.playerHealth.maxHP==4,"ordinary restart tears down all source Shana run state");CombatClose(game.playerXP.xpMultiplier.Modify(1),1,"restart clears Quick Learner");CombatClose(Bonus(p.stats[StatType.FireRate]),0,"restart clears Halo stats");yield return CheckRevolver(game,p,"android-shana-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Full Shana offer/reroll/Quick Learner/Specialize/Ascension/physical Halo/source Continue/pending provider/restart workflow");Application.Quit(0);
 }
}
[HarmonyPatch(typeof(ShootBulletsBehind),"OnShoot")]
class ObserveShanaRear
{static void Prefix(){RuntimeChecks.ShanaRearActive=RuntimeChecks.ShanaMode;}static void Postfix(){RuntimeChecks.ShanaRearActive=false;}}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class ObserveShanaRearProjectile
{static void Postfix(Vector2 __1){if(RuntimeChecks.ShanaRearActive)RuntimeChecks.ShanaRearDirections.Add(__1);}}
[HarmonyPatch(typeof(ShanasHalo),nameof(ShanasHalo.ActivateSprite))]
class ObserveShanaActivation
{static void Prefix(ShanasHalo __instance)=>RuntimeChecks.ShanaActivation(__instance);}
[HarmonyPatch(typeof(SoundEffectSO),nameof(SoundEffectSO.Play))]
class ObserveShanaSound
{static void Prefix(SoundEffectSO __instance){if(RuntimeChecks.ShanaMode&&PlayerController.Instance!=null){var halo=PlayerController.Instance.GetComponentInChildren<ShanasHalo>();if(halo!=null&&ReferenceEquals(AccessTools.Field(typeof(ShanasHalo),"soundFX").GetValue(halo),__instance))RuntimeChecks.ShanaSounds++;}}}
