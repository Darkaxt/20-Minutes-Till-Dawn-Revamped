using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 internal static int RosaBloomBursts;
 static T RosaField<T>(object owner,string field)=>(T)AccessTools.Field(owner.GetType(),field).GetValue(owner);
 static void RosaCall(object owner,string name,params object[] args)=>AccessTools.Method(owner.GetType(),name).Invoke(owner,args);
 static RosaRunState RosaCapture(PlayerController player)=>(RosaRunState)AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.SaintRosaResume"),"Capture").Invoke(null,new object[]{player});
 static RosaShell[] RosaShells()=>Object.FindObjectsOfType<RosaShell>().Where(s=>RosaField<SaintRosaSkill>(s,"Owner")!=null).ToArray();
 IEnumerator RosaFire(SaintRosaSkill skill,int count)
 {
  var p=PlayerController.Instance;
  p.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);p.playerInput.neverAutoSwitchControlSchemes=true;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*5)}.WithButton(MouseButton.Left));
  while(skill.ShellCount<count)yield return null;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=Camera.main.WorldToScreenPoint(p.transform.position+Vector3.right*5)});yield return null;
  while(!(p.CurrentState is flanne.Player.IdleState))yield return null;
 }
 IEnumerator CheckSaintRosa(string mode)
 {
  if(mode=="saint-rosa-integrations")
  {
   RunResume.Invalidate();yield return AchievementRun("Revamped_SaintRosa","RevolverData","20M_Forest");
   var active=Object.FindObjectOfType<GameController>();var actor=PlayerController.Instance;var passive=actor.GetComponentInChildren<SaintRosaSkill>();
   while(passive==null){yield return null;passive=actor.GetComponentInChildren<SaintRosaSkill>();}
   yield return RosaIntegration(active,actor,passive);SetPhase("PASS");Log("complete","Rosa actual chamber rendering, rune/familiarity/wing integration, native Retry and Custom verified");Application.Quit(0);yield break;
  }
  bool continuation=mode.EndsWith("continue");var title=Progression.Title;
  if(!continuation)
  {
   SetPhase("rosa-unlock-controller");RunResume.Invalidate();
   Require(Characters.Items(title.characterMenu).Count==16&&Characters.Data.ContainsKey("SaintRosa"),"Saint Rosa is the sixteenth native character");
   var gate=Gate("Revamped_SaintRosa");Require(!Progression.Allowed(gate.Data),"Saint Rosa starts locked without a currency purchase");
   var pad=InputSystem.AddDevice<Gamepad>("SaintRosaController");
   InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
   yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);
   while(!Active(title.characterMenu)||!title.characterMenu.interactable)yield return null;
   var rosaButton=Entries(title.characterMenu)[Characters.Slots["SaintRosa"]].button;
   yield return MenuMove(pad,title.characterMenu.transform,rosaButton);
   Require(SelectorText().Contains("Saint Rosa")&&SelectorText().Contains("Abby")&&SelectorText().Contains("8"),"controller selection refreshes Rosa's portrait and achievement description");
   yield return Capture("saint-rosa-locked.png");Click(title.loadoutBackButton);while(!Active(title.mainMenu))yield return null;
   Progression.State.earned.Add("character:Abby");Progression.State.earned.Add("scenario:20M_Temple");Progression.Refresh();
   Log("fixture-precondition","Only Abby and Temple ownership plus Darkness3 and fire-rate values are seeded; Rosa must be earned by actual native miniboss deaths.");
   yield return AchievementRun("Abby","RevolverData","20M_Temple",3);
   var first=Object.FindObjectOfType<GameController>();StopFixtureSpawning();Attribute(StatType.FireRate,7.5f);yield return Mini(first);Earned("Revamped_SaintRosa",false);
   Attribute(StatType.FireRate,8);yield return Mini(first);Earned("Revamped_SaintRosa");
   while(UnlockNotifications.Instance.Showing!=gate.Id)yield return null;
   yield return Capture("saint-rosa-earned.png");SaveSystem.Save();yield return AchievementExit(first,false);
   title=Progression.Title;yield return PadClick(pad,title.mainMenu.transform,Entries(title.mainMenu)[0].button);
   while(!Active(title.characterMenu)||!title.characterMenu.interactable)yield return null;
   yield return MenuMove(pad,title.characterMenu.transform,Entries(title.characterMenu)[Characters.Slots["SaintRosa"]].button);
   yield return Capture("saint-rosa-selection.png");Click(title.loadoutBackButton);while(!Active(title.mainMenu))yield return null;
   yield return AchievementRun("Revamped_SaintRosa","RevolverData","20M_Forest");
  }
  else
  {
   Click(Entries(title.mainMenu)[0].button);yield return null;
   Click(Resources.FindObjectsOfTypeAll<Button>().Single(b=>b.name=="ResumeContinue"&&b.gameObject.activeInHierarchy));
  }
  GameController game;while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState)||RunResume.Restoring)yield return null;
  var p=PlayerController.Instance;var skill=p.GetComponentInChildren<SaintRosaSkill>();while(skill==null||!RosaField<GameObject>(skill,"templates")){yield return null;skill=p.GetComponentInChildren<SaintRosaSkill>();}
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  Require(p.loadedCharacter==Characters.Data["SaintRosa"]&&p.GetComponentInChildren<PortedCharacterSkill>()==null,"Rosa uses her own complete passive, not the previous character's ability");
  if(continuation)
  {
   SetPhase("rosa-continue");game.ChangeState<PauseState>();yield return null;
   RosaRunState expected;using(var stream=File.OpenRead(Path.Combine(Environment.GetEnvironmentVariable("MODERNIZATION_EXPECTED_DIR"),"saved-run.json")))
    expected=((RunResume.Snapshot)new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(RunResume.Snapshot)).ReadObject(stream)).saintRosa;
   var actual=RosaCapture(p);
   Require(actual.shells==expected.shells&&actual.progress==expected.progress&&skill.ThornBelt&&skill.PiercingPetal&&skill.FinalBloom,"Continue restores charging and all three exclusive flags through acquisition replay");
   Require(actual.projectiles.Count==expected.projectiles.Count&&actual.volleys.Count==expected.volleys.Count,"Continue restores live shells and shared volley identities");
   Require(actual.buffRemaining>0&&actual.buffRemaining<=expected.buffRemaining&&actual.cooldown>0,"Continue restores remaining buff/cooldown without restarting them");
   foreach(var state in actual.projectiles)Require(Mathf.Abs(state.damage-expected.projectiles[0].damage)<.001f,"Continue retains the shell damage rather than multiplying the recipe again");
   yield return Capture("saint-rosa-continue.png");Click(game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;
   while(skill.BuffRemaining>0||RosaShells().Length>0)yield return null;
   CombatClose(p.stats[StatType.FireRate].Modify(1),1,"restored temporary buff expires exactly once");
   yield return AchievementExit(game,false);yield return AchievementRun("Revamped_SaintRosa","RevolverData","20M_Forest");
   p=PlayerController.Instance;skill=p.GetComponentInChildren<SaintRosaSkill>();while(skill==null){yield return null;skill=p.GetComponentInChildren<SaintRosaSkill>();}
   Require(skill.ShellCount==0&&skill.ShotProgress==0&&!skill.FinalBloom,"a fresh run after Continue has no leaked chamber or upgrade state");
   yield return RosaIntegration(Object.FindObjectOfType<GameController>(),p,skill);
   SetPhase("PASS");Log("complete","Rosa fresh-process Continue, source cooldown/buff/actor expiry and new-run reset verified");Application.Quit(0);yield break;
  }
  SetPhase("rosa-ability");p.SetPosition(new Vector3(100,100,0));p.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);p.playerInput.neverAutoSwitchControlSchemes=true;
  Require(p.playerHealth.maxHP==4&&Mathf.Abs(p.movementSpeed-3.2f)<.001f,"Rosa base HP and movement match8.7");
  p.ammo.UseAmmo(2);yield return SkillInput(null);Require(p.ammo.fullOnAmmo&&skill.ShellCount==0&&PortedCharacterSkill.Timer(skill)>7,"an empty-chamber native activation still refills ammunition and starts cooldown");
  yield return RosaFire(skill,8);Require(skill.ShellCount==8,"actual native effective shots charge eight stored shells");
  RosaBloomBursts=0;PortedCharacterSkill.Timer(skill)=0;yield return SkillInput(null);
  var shells=RosaShells();Require(shells.Length==8&&skill.ShellCount==0,"native activation releases eight real pooled shell actors");
  var volley=RosaField<RosaVolley>(shells[0],"Volley");Require(shells.All(s=>ReferenceEquals(RosaField<RosaVolley>(s,"Volley"),volley)),"all shells share one Bloom context");
  float damage=Mathf.Floor(p.gun.damage*2.6f*(1+.2f*7));foreach(var shell in shells)CombatClose(shell.GetComponent<Projectile>().damage,damage,"stored-shell count participates once in source damage");
  var hit=HitEffectTarget(maximum:100000);hit.transform.position=shells[0].transform.position+(Vector3)shells[0].GetComponent<Projectile>().vector.normalized*.4f;Physics2D.SyncTransforms();
  while(hit.HP==100000&&RosaShells().Length>0)yield return null;
  Require(hit.HP<100000&&volley.bloomTriggered,"real sampled shell contact damages the enemy and triggers Bloom");
  ReturnFixtureActor(hit.gameObject);while(RosaShells().Length>0)yield return null;
  Require(RosaBloomBursts==1,"remaining source shell contacts/lifetime expirations cannot duplicate the shared Bloom");
  yield return ExclusiveChest(game,null);Require(skill.ThornBelt&&skill.PiercingPetal&&skill.FinalBloom,"all three ordinary native exclusive chest rewards reach Rosa");
  Require((int)AccessTools.Property(typeof(SaintRosaSkill),"Capacity").GetValue(skill)==11,"Thorn Belt expands chamber to eleven");
  yield return RosaFire(skill,11);PortedCharacterSkill.Timer(skill)=0;
  if(!ColorfulStyle.Instance.Enabled)ColorfulStyle.Instance.Toggle();yield return null;yield return Capture("saint-rosa-chamber-colored.png");
  ColorfulStyle.Instance.Toggle();yield return null;yield return Capture("saint-rosa-chamber-original.png");ColorfulStyle.Instance.Toggle();
  yield return SkillInput(null);shells=RosaShells();Require(shells.Length==11&&shells.All(s=>s.GetComponent<Projectile>().bounce==4&&s.GetComponent<Projectile>().piercing==0),"Thorn Belt changes bounce, not pierce, matching the executable source");
  CombatClose(p.stats[StatType.FireRate].Modify(1),1.25f,"Final Bloom adds one25-percent fire-rate contribution");
  Require(skill.BuffRemaining>0&&PortedCharacterSkill.Timer(skill)>5,"Final Bloom uses the six-second native callback cooldown");
  RunResume.Save();var saved=RunResume.Read();Require(saved.saintRosa!=null&&saved.saintRosa.projectiles.Count==11,"real Continue checkpoint includes every active shell");
  File.Copy(RunResume.PathName,Path.Combine(ResultDir,"saved-run.json"));
  yield return Capture("saint-rosa-volley-colored.png");
  SetPhase("PASS");Log("complete","Controller lock/earned selection, real miniboss award, native ability, source shell formulas, authored artwork, exclusive chests and persistent checkpoint verified");Application.Quit(0);
 }
 IEnumerator RosaIntegration(GameController game,PlayerController player,SaintRosaSkill skill)
 {
  SetPhase("rosa-retry-custom-integration");StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  var profile=RuneProgression.Profile(player.loadedCharacter.name);
  Require(profile.character=="Revamped_SaintRosa"&&profile.ranks.All(r=>r.rank>=RuneProgression.SharedMinimum(r.name)),"Rosa has an independent rune profile with the current shared head start");
  Require(player.GetComponent<WeaponFamiliarity>()!=null&&player.GetComponent<WeaponFamiliarity>().Rank==0,"new Rosa weapon familiarity does not inherit another character's crown");
  var wing=Cosmetics.Items.Values.First(i=>i.kind=="wing");if(!Cosmetics.Owned(wing.id))Cosmetics.State.owned.Add(wing.id);Require(Cosmetics.Equip("Revamped_SaintRosa","wing",wing.id),"Rosa can select an earned shared wing");
  yield return RosaFire(skill,3);yield return null;
  var chamber=GameObject.Find("RoseChamber");Require(chamber!=null&&chamber.GetComponentsInChildren<Image>().Count(i=>i.enabled)==3,"three charged shells appear in the actual corner chamber");
  Require(chamber.GetComponentsInChildren<Image>().Where(i=>i.enabled).All(i=>i.sprite!=null&&i.canvasRenderer.GetInheritedAlpha()>.7f),"authored chamber icons render with the gameplay HUD opacity");
  yield return Capture("saint-rosa-chamber-visible.png");PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.giveupButton);
  while(!(game.CurrentState is PlayerDeadState)||!game.retryRunButton.IsInteractable())yield return null;
  int prior=game.GetInstanceID();Click(game.retryRunButton);
  while((game=Object.FindObjectOfType<GameController>())==null||game.GetInstanceID()==prior||!(game.CurrentState is CombatState))yield return null;
  player=PlayerController.Instance;skill=player.GetComponentInChildren<SaintRosaSkill>();while(skill==null){yield return null;skill=player.GetComponentInChildren<SaintRosaSkill>();}
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();Require(skill.ShellCount==0&&skill.ShotProgress==0&&!skill.FinalBloom,"native Retry retains Rosa but resets her previous chamber and exclusive state");
  Require(AppearanceRun.Instance.Character.wing==wing.id&&player.GetComponentsInChildren<SpriteRenderer>().Any(s=>s.transform.parent!=null&&s.transform.parent.name=="AppearanceWing"||s.name=="AppearanceWing"),"Retry binds Rosa's selected shared wing to the new run");
  yield return AchievementExit(game,false);yield return OpenCustomConfiguration("RevolverData","Revamped_SaintRosa");
  Click(CustomButton("CustomTab1"));CustomValue(0,"40");Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>();Click(CustomButton("CustomStart"));
  while((game=Object.FindObjectOfType<GameController>())==null||!(game.CurrentState is CombatState))yield return null;
  player=PlayerController.Instance;skill=player.GetComponentInChildren<SaintRosaSkill>();while(skill==null){yield return null;skill=player.GetComponentInChildren<SaintRosaSkill>();}
  StopFixtureSpawning();GameTimer.SharedInstance.Stop();Require(CustomMode.Active&&player.loadedCharacter.name=="Revamped_SaintRosa"&&player.gun.damage==40,"obtained Rosa starts in the actual Custom mode with its configured weapon");
  yield return RosaFire(skill,1);PortedCharacterSkill.Timer(skill)=0;yield return SkillInput(null);
  CombatClose(RosaShells()[0].GetComponent<Projectile>().damage,104,"Rosa's shell formula consumes the real Custom weapon damage once");
  yield return Capture("saint-rosa-custom.png");yield return AchievementExit(game,false);
 }
}
[HarmonyPatch(typeof(RosaBurst),"Play")]
class RosaBurstObservation
{static void Postfix(RosaBurst __instance){if(__instance.name=="SaintRosa_BloomShock"||__instance.name=="SaintRosa_BloomShockSmall")RuntimeChecks.RosaBloomBursts++;}}
