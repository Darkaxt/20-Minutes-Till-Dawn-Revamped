using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.Text;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 int justiceSmiteNotices;
 [DataContract]sealed class CheckpointVersion{[DataMember]public int version{get;set;}}
 static bool JusticeSubscribed(GainMaxHPOnSmiteKill owner)
 {
  var table=(Dictionary<string,Dictionary<object,List<Action<object,object>>>>)AccessTools.Field(typeof(NotificationCenter),"_table").GetValue(NotificationCenter.instance);
  return table.TryGetValue(SmitePassive.SmiteKillNotification,out var senders)&&senders.Values.SelectMany(handlers=>handlers).Any(h=>ReferenceEquals(h.Target,owner));
 }
 void JusticeSmiteSeen(object sender,object args)=>justiceSmiteNotices++;
 IEnumerator JusticeHit(PlayerController player,Health target,bool protectedHit=false)
 {
  while(JusticeSmiteFlowProbe.Running)yield return null;
  var actor=ObjectPooler.SharedInstance.GetPooledObject("Validation_Justice_Enemy");Require(actor==target.gameObject,"Justice reuses the actual returned source pool actor");DurableEnemy(actor,player.transform.position+Vector3.right,false);actor.tag="Enemy";
  if(protectedHit)target.isInvincible.Flip();Physics2D.SyncTransforms();int previous=justiceSmiteNotices;
  player.ammo.Reload();player.ammo.UseAmmo(player.ammo.amount);while(justiceSmiteNotices==previous)yield return null;while(JusticeSmiteFlowProbe.Running)yield return null;
  Log("justice-smite-result",new{previous,notices=justiceSmiteNotices,hp=target.HP,protectedHit});
  Require(justiceSmiteNotices==previous+1&&target.HP==(protectedHit?10000:9980),"actual last-ammo Smite emits one source notification after a "+(protectedHit?"protected":"surviving")+" target hit");
  if(protectedHit)target.isInvincible.UnFlip();ReturnFixtureActor(target.gameObject);
 }
 IEnumerator CheckAndroidJustice(bool continuation)
 {
  bool legacy=Environment.GetEnvironmentVariable("MODERNIZATION_TEST").Contains("-legacy-");
  if(legacy)
  {
   int contracts=0;var serializer=new DataContractJsonSerializer(typeof(RunResume.Snapshot));var marker=new DataContractJsonSerializer(typeof(CheckpointVersion));
   var corpus=Directory.GetParent(Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR")).FullName;
   foreach(string path in Directory.EnumerateFiles(corpus,"saved-run.json",SearchOption.AllDirectories).OrderBy(p=>p))
   {
    int version;using(var stream=File.OpenRead(path))version=((CheckpointVersion)marker.ReadObject(stream)).version;
    if(version<3||version>6)continue;
    RunResume.Snapshot decoded;using(var stream=File.OpenRead(path))decoded=(RunResume.Snapshot)serializer.ReadObject(stream);
    contracts++;Log("legacy-contract-decoded",path+" schema="+decoded.version);
   }
   Require(contracts>0,"native serializer decodes the actual supported historical checkpoint corpus, including missing later envelope/runtime/summon/shield fields");
   bool rejected=false;try{using(var partial=new MemoryStream(Encoding.UTF8.GetBytes("{\"version\":3}")))serializer.ReadObject(partial);}catch(SerializationException){rejected=true;}
   Require(rejected,"original baseline checkpoint members remain required; optional later members do not accept an incomplete envelope");
   byte[] original=File.ReadAllBytes(RunCheckpoint);var read=RunResume.Read();
   Require(read.version==3&&read.runtime.sourcePowerups!=null&&read.runtime.stats.Count==0,"actual schema3 checkpoint is rebuilt in memory through the source runtime reader without aggregate stats");
   Require(read.runtime.sourcePowerups.Single(p=>p.key=="justice_name").values.Select(v=>v.count).SequenceEqual(new[]{3,2}),"legacy rebuild extracts the recorded earned Justice counts3/2");
   Require(original.SequenceEqual(File.ReadAllBytes(RunCheckpoint)),"legacy source read leaves the owned checkpoint bytes unchanged");
  }
  GameController game;PlayerController player;
  if(!continuation)
  {
   RunResume.Invalidate();Progression.State.earned.Add("scenario:Endless_Forest");Progression.Refresh();
   yield return AchievementRun("Shana","RevolverData","Endless_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;StopFixtureSpawning();
   yield return AndroidPick(game,"Justice",game.playerXP.level);
  }
  else
  {
   var title=Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>();Click(Entries(title.mainMenu)[0].button);yield return null;
   Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
   while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))
   {
    var error=Resources.FindObjectsOfTypeAll<TMPro.TMP_Text>().FirstOrDefault(t=>t.name=="CheckpointDescription"&&t.gameObject.activeInHierarchy&&t.text.StartsWith("Could not restore"));
    if(error!=null)throw new InvalidOperationException(error.text);yield return null;
   }
   player=PlayerController.Instance;StopFixtureSpawning();
  }
  var helpers=player.GetComponentsInChildren<GainMaxHPOnSmiteKill>();
  Require(helpers.Length==(continuation?2:1),"each actual Justice application attaches an independent source growth helper");
  while(helpers.Any(h=>!JusticeSubscribed(h)))yield return null;
  Require(helpers.All(h=>(int)WindField(h,"killsToGainMHP")==500&&(int)WindField(h,"mhpGainCap")==3),"source Justice keeps the shipped500-event threshold and three-growth limit per owner");
  player.SetPosition(new Vector3(100,100,0));GameTimer.SharedInstance.Stop();
  if(legacy)Require(GameTimer.SharedInstance.timer>=0,"legacy Endless's negative Windows startup display clock maps to its recorded elapsed encounter time");
  // Source RestoreAmmo also invokes OnAmmoChanged. An empty restored clip can
  // already have a real Smite coroutine pending; do not overlap a new test hit.
  while(JusticeSmiteFlowProbe.Running)yield return null;
  var pool=ObjectPooler.SharedInstance;var template=pool.itemDictionary["BrainMonster"].objectToPool;
  pool.AddObject("Validation_Justice_Enemy",template,1);var target=pool.GetPooledObject("Validation_Justice_Enemy").GetComponent<Health>();ReturnFixtureActor(target.gameObject);
  this.AddObserver(JusticeSmiteSeen,SmitePassive.SmiteKillNotification);
  if(continuation)
  {
   Require(player.playerHealth.maxHP==20&&player.playerHealth.hp==2&&player.stats[StatType.MaxHP].Modify(player.loadedCharacter.startHP)==4,"Continue restores saved actual20-heart maximum/injury without importing unrecorded stat seeds or adding earned Justice stats again");
   Require((int)WindField(helpers[0],"_mhpGainCounter")==3&&(int)WindField(helpers[1],"_mhpGainCounter")==2&&(int)WindField(helpers[1],"_killCounter")==0,"Continue preserves independent earned Justice counts and resets source-unsaved partial kill progress");
   yield return JusticeHit(player,target);yield return JusticeHit(player,target);
   Require((int)WindField(helpers[1],"_killCounter")==2&&(int)WindField(helpers[1],"_mhpGainCounter")==2,"actual continued Smite hits start a new source counter without granting health early");
   Plugin.SetField(helpers[1],"_killCounter",499);yield return JusticeHit(player,target);
   Require(player.playerHealth.maxHP==5&&player.playerHealth.hp==2&&(int)WindField(helpers[1],"_killCounter")==0&&(int)WindField(helpers[1],"_mhpGainCounter")==3&&player.stats[StatType.MaxHP].Modify(player.loadedCharacter.startHP)==5,"final continued Justice growth uses the actual replayed stat total, preserves injury and completes that source owner");
   yield return JusticeHit(player,target);Require((int)WindField(helpers[1],"_killCounter")==0,"an exhausted source helper stops counting after its third growth");
   this.RemoveObserver(JusticeSmiteSeen,SmitePassive.SmiteKillNotification);
   yield return AndroidPick(game,"Vitality",game.playerXP.level);Require(player.playerHealth.maxHP==6,"ordinary Vitality uses the current replayed MaxHP stat after Justice's explicit assignment");
   if(legacy)
   {
    RunResume.Save();var written=RunResume.Read();Require(written.version==7&&written.runtime.sourcePowerups!=null,"normal save after legacy continuation writes current source schema7");
    File.Copy(RunCheckpoint,Path.Combine(ResultDir,"migrated-run.json"));
   }
   yield return AchievementExit(game,false);yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
   game=Object.FindObjectOfType<GameController>();player=PlayerController.Instance;
   Require(player.GetComponentsInChildren<GainMaxHPOnSmiteKill>().Length==0&&player.GetComponentsInChildren<SmitePassive>().Length==0&&player.playerHealth.maxHP==4,"original restart removes source Justice/Smite owners and their health growth");
   yield return CheckRevolver(game,player,"android-justice-original-restart.png");yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Source Justice earned counts, omitted partial progress, saved actual health, live-stat final growth and original restart survive Continue");Application.Quit(0);yield break;
  }
  var first=helpers[0];Plugin.SetField(first,"_killCounter",498);player.playerHealth.hp=2;
  Log("fixture-precondition","Justice counters seeded immediately before source thresholds; actual last-ammo Smite notifications perform every tested growth, not500 full-play kills");
  yield return JusticeHit(player,target,true);Require((int)WindField(first,"_killCounter")==499&&player.playerHealth.maxHP==4,"source Justice counts the protected-target notification without an early growth");
  yield return JusticeHit(player,target);Require((int)WindField(first,"_killCounter")==0&&(int)WindField(first,"_mhpGainCounter")==1&&player.playerHealth.maxHP==5&&player.playerHealth.hp==3,"the actual500th source notification grants one maximum/current heart from injured health");
  for(int growth=2;growth<=3;growth++){Plugin.SetField(first,"_killCounter",499);yield return JusticeHit(player,target);Require((int)WindField(first,"_mhpGainCounter")==growth&&player.playerHealth.maxHP==4+growth,"source Justice completes growth "+growth);}
  yield return JusticeHit(player,target);Require((int)WindField(first,"_killCounter")==0&&player.playerHealth.maxHP==7,"first Justice owner stops at three increments");
  yield return AndroidPick(game,"Justice",game.playerXP.level);helpers=player.GetComponentsInChildren<GainMaxHPOnSmiteKill>();while(helpers.Any(h=>!JusticeSubscribed(h)))yield return null;
  Require(helpers.Length==2&&(int)WindField(helpers[1],"_mhpGainCounter")==0,"repeat acquisition creates a fresh capped source Justice owner");
  Log("fixture-precondition","Explicit unrecorded MaxHP+16 seed exercises source Justice cap/injury; source Continue replays acquisitions and does not retain this raw seed or regrant earned Justice stats (A151/A156).");player.stats[StatType.MaxHP].AddFlatBonus(16);Require(player.playerHealth.maxHP==23,"ordinary HP stat remains uncapped before the explicit source Justice assignment");player.playerHealth.hp=2;
  for(int growth=1;growth<=2;growth++){Plugin.SetField(helpers[1],"_killCounter",499);yield return JusticeHit(player,target);Require(player.playerHealth.maxHP==20&&player.playerHealth.hp==2&&(int)WindField(helpers[1],"_mhpGainCounter")==growth,"source Justice assigns its20-heart cap once without an intermediate uncapped heal, growth "+growth);}
  Plugin.SetField(helpers[1],"_killCounter",497);this.RemoveObserver(JusticeSmiteSeen,SmitePassive.SmiteKillNotification);
  PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();yield return Capture("android-justice-source-counters-and-injury.png");
  Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeSaveAndReturn"&&b.gameObject.activeInHierarchy));
  while(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>()==null||!Active(Object.FindObjectOfType<flanne.TitleScreen.TitleScreenController>().mainMenu))yield return null;
  File.Copy(RunCheckpoint,Path.Combine(ResultDir,"saved-run.json"));SetPhase("PASS");Log("complete","Android Justice actual protected/surviving Smite notifications, separate capped owners and injured explicit maximum checkpoint");Application.Quit(0);
 }
[HarmonyPatch(typeof(SmitePassive),"SmiteCR")]
class JusticeSmiteFlowProbe
{
 static readonly Dictionary<IEnumerator,SmitePassive> Active=new Dictionary<IEnumerator,SmitePassive>();
 internal static bool Running=>Active.Values.Any(owner=>owner!=null&&owner.gameObject.activeInHierarchy);
 static void Postfix(SmitePassive __instance,ref IEnumerator __result)=>__result=Observe(__instance,__result);
 static IEnumerator Observe(SmitePassive owner,IEnumerator native)
 {Active.Add(native,owner);try{while(native.MoveNext())yield return native.Current;}finally{Active.Remove(native);}}
}
}
