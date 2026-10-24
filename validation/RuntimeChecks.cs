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
using UnityEngine.SceneManagement;
using TMPro;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using flanne.UI;
using PlayerXP=flanne.Player.PlayerXP;
using Object=UnityEngine.Object;
using XMenu=flanne.UIExtensions.Menu;

namespace TwentyMinutes.Modernization.Validation;
using TwentyMinutes.Modernization;
public partial class RuntimeChecks:MonoBehaviour
{
    public static int GunSlot;
    public static int Projectiles;
    public static int TargetId,DamageEvents;
    public static int TargetDamage;
    public static readonly HashSet<int> StartedSummons=new HashSet<int>();
    public static readonly Dictionary<int,float> StartedSummonTimers=new Dictionary<int,float>();
    static bool SummonStarted(Summon summon)=>StartedSummons.Contains(summon.GetInstanceID());
    static readonly Dictionary<Health,List<UnityEngine.Events.UnityAction<int>>> damageObservers=new Dictionary<Health,List<UnityEngine.Events.UnityAction<int>>>();
    static void ObserveDamage(Health target,UnityEngine.Events.UnityAction<int> callback)
    {
        if(!damageObservers.TryGetValue(target,out var callbacks))damageObservers.Add(target,callbacks=new List<UnityEngine.Events.UnityAction<int>>());
        callbacks.Add(callback);
    }
    static void UnobserveDamage(Health target,UnityEngine.Events.UnityAction<int> callback)
    {if(damageObservers.TryGetValue(target,out var callbacks))callbacks.Remove(callback);}
    public static void ObservedSourceDamage(Health target,int amount)
    {if(damageObservers.TryGetValue(target,out var callbacks))foreach(var callback in callbacks.ToArray())callback(amount);}
    static void StopFixtureSpawning()
    {
        Object.FindObjectOfType<HordeSpawner>().enabled=false;
        Object.FindObjectOfType<AndroidEndlessRun>().enabled=false;
        foreach(var actor in ObjectPooler.SharedInstance.pooledObjectsDictionary.Values.SelectMany(list=>list).Where(actor=>actor.activeSelf&&actor.layer==LayerMask.NameToLayer("Enemy")&&actor.GetComponent<Health>()!=null).Distinct().ToArray())ReturnFixtureActor(actor);
        Log("fixture-precondition","Disable the actual source scheduler and return pre-existing monsters for controlled combat checks; no natural encounter/scheduling claim.");
    }
    static string ResultDir=>Environment.GetEnvironmentVariable("MODERNIZATION_RESULT_DIR") ?? throw new InvalidOperationException("Missing validation result directory");
    Mouse fixtureMouse;Keyboard fixtureKeyboard;
    static string phase;
    static StreamWriter events;
    string previousNativeState;
    void Update()
    {
        if(File.Exists(Path.Combine(ResultDir,"cancel.request"))){SetPhase("CANCELLED");Application.Quit(3);return;}
        var p=PlayerController.Instance;if(p==null)p=null;var g=Object.FindObjectOfType<GameController>();
        string native=(g?.CurrentState?.GetType().Name ?? "title")+"|"+(p?.CurrentState?.GetType().Name ?? "no-player")+"|"+(p?.gun?.gunData?.name ?? "no-gun")+"|"+(p?.gun?.shooters?.FirstOrDefault()?.GetType().Name ?? "no-shooter")+"|"+((p?.gun?.shooters?.FirstOrDefault() as PortedShooter)?.Ready.ToString() ?? "not-ported");
        if(native!=previousNativeState){previousNativeState=native;Log("native-state",native);}
    }
    public static void Log(string kind,object data)
    {
        if(events==null)events=new StreamWriter(Path.Combine(ResultDir,"runtime-events.log"),true){AutoFlush=true};
        events.WriteLine(DateTime.UtcNow.ToString("O")+" | "+kind+" | "+data);
        if(kind=="phase" || kind=="shot" || kind=="complete" || kind=="exception" || kind=="FAIL")events.Flush();
    }
    void Awake()
    {
        Application.logMessageReceived+=Error;
    }
    void OnDestroy(){Application.logMessageReceived-=Error;events?.Flush();events?.Dispose();events=null;}
    void Error(string message,string stack,LogType type)
    {if(type==LogType.Exception){Log("exception",message+"\n"+stack);Application.Quit(2);}}
    void SetPhase(string next)
    {
        phase=next;Log("phase",next);
        File.WriteAllText(Path.Combine(ResultDir,"runtime-phase.json"),JsonUtility.ToJson(new Phase{phase=next,scene=SceneManager.GetActiveScene().name,utc=DateTime.UtcNow.ToString("O")},true));
    }
    [Serializable]class Phase{public string phase,scene,utc;}
    static bool Active(XMenu menu)=>menu!=null && (bool)AccessTools.Field(typeof(XMenu),"isActive").GetValue(menu);
    static List<flanne.UIExtensions.ButtonExtension> Entries(XMenu m)=>(List<flanne.UIExtensions.ButtonExtension>)AccessTools.Field(typeof(XMenu),"entries").GetValue(m);
    static void Click(Button b)
    {Require(b.gameObject.activeInHierarchy && b.IsInteractable(),"UI control active and interactable: "+b.name);Log("ui-click",b.name);b.onClick.Invoke();}
    static void Require(bool condition,string criterion)
    {if(!condition)throw new InvalidOperationException("FAIL: "+criterion);Log("pass",criterion);}
    IEnumerator Start()
    {
        // Chainloader may be invoked from InputSystem's static constructor. Start is the
        // lifecycle boundary after that constructor completes and before scene Updates.
        fixtureMouse=InputSystem.AddDevice<Mouse>("ChargeSpikeMouse");
        fixtureKeyboard=InputSystem.AddDevice<Keyboard>("ChargeSpikeKeyboard");
        AudioListener.volume=0f;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        Log("input-fixture","Synthetic devices ignore desktop focus; no OS input injection");
        var routine=Run();
        // Supervise coroutine exceptions without a cancellation deadline.
        while(true)
        {
            bool next=false;object item=null;
            try {next=routine.MoveNext();if(next)item=routine.Current;}
            catch(Exception ex){Log("FAIL",ex.ToString());SetPhase("FAILED");Application.Quit(2);yield break;}
            if(!next)yield break;
            yield return item;
        }
    }
    IEnumerator Run()
    {
        SetPhase("waiting-title");
        TitleScreenController title;
        while((title=Object.FindObjectOfType<TitleScreenController>())==null || !Active(title.mainMenu))yield return null;
        Require(Application.persistentDataPath.Contains("CodexModernization"),"isolated persistent identity");
        GunSlot=NativeSelection.GunIndex;
        string testMode=Environment.GetEnvironmentVariable("MODERNIZATION_TEST");
        if(testMode=="powerups-android-numeric"){yield return CheckAndroidNumeric();yield break;}
        if(testMode=="powerups-android-shield"){yield return CheckAndroidShield();yield break;}
        if(testMode=="powerups-android-refresh"){yield return CheckAndroidRefresh();yield break;}
        if(testMode=="powerups-android-burn"){yield return CheckAndroidBurn();yield break;}
        if(testMode=="powerups-android-health"){yield return CheckAndroidHealth();yield break;}
        if(testMode=="powerups-android-base"){yield return CheckAndroidBase();yield break;}
        if(testMode.StartsWith("resume-heart-perks-")){yield return CheckAndroidHealthPerks(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-dodge-")){yield return CheckAndroidDodge(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-rune-consumers-")){yield return CheckAndroidRuneConsumers(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-hit-effects-")){yield return CheckAndroidHitEffects(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-lightning-")){yield return CheckAndroidLightning(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-wind-")){yield return CheckAndroidWind(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-glare-")){yield return CheckAndroidGlare(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-shield-")){yield return CheckAndroidShieldOwners(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-justice-")){yield return CheckAndroidJustice(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-generator-")){yield return CheckAndroidGenerator(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-spear-")){yield return CheckAndroidSpears(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-weapon-contact-")){yield return CheckAndroidWeaponContacts(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-dragon-")){yield return CheckAndroidDragon(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-combat-buffs-")){yield return CheckAndroidCombatBuffs(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-counted-buffs-")){yield return CheckAndroidCountedBuff(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-character-buffs-")){yield return CheckAndroidCharacterBuffs(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-growth-rune-")){yield return CheckAndroidGrowthRune(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-combat-runes-")){yield return CheckAndroidCombatRunes(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-status-runes-")){yield return CheckAndroidStatusRunes(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-character-actors-")){yield return CheckAndroidCharacterActors(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-named-weapons-")){yield return CheckAndroidNamedWeapons(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-spirits-")){yield return CheckAndroidSpirits(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-moths-")){yield return CheckAndroidMoths(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-emitters-")){yield return CheckAndroidEmitters(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-shana-")){yield return CheckAndroidShana(testMode);yield break;}
        if(testMode.StartsWith("resume-transform-")){yield return CheckAndroidTransforms(testMode);yield break;}
        if(testMode.StartsWith("resume-status-tree-")){yield return CheckAndroidStatusTree(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-recovery-")){yield return CheckAndroidRecovery(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-mixed-tomes-")){yield return CheckAndroidMixedTomes(testMode);yield break;}
        if(testMode.StartsWith("resume-electro-bug-")){yield return CheckAndroidElectroBug(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-splinter-")){yield return CheckAndroidSplinter(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-spores-")){yield return CheckAndroidSpores(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-world-")){yield return CheckAndroidWorld(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-enemy-attacks-")){yield return CheckAndroidEnemyAttacks(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-boundaries-")){yield return CheckAndroidBoundaries(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-arrows-")){yield return CheckAndroidArrows(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-cannon-")){yield return CheckAndroidCannon(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-shooting-exit-")){yield return CheckShootingExit(testMode.EndsWith("baseline"));yield break;}
        if(testMode.StartsWith("resume-shooting-")){yield return CheckAndroidShooting(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-ghost-upgrades-")){yield return CheckAndroidGhostUpgrades(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-ghost-wizard-")){yield return CheckAndroidGhostWizard(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-cold-")){yield return CheckAndroidCold(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("resume-android-")){yield return CheckAndroidResume(testMode.EndsWith("continue"));yield break;}
        if(testMode.StartsWith("artwork-")){yield return CheckTitleArtwork(title);yield break;}
        if(testMode.StartsWith("delivery-")||testMode=="gale"||testMode=="bulwark")RunResume.Invalidate();
        if(testMode.StartsWith("integrated-hud")){yield return CheckIntegratedHud(title);yield break;}
        if(testMode.StartsWith("custom-native")){yield return CheckCustom();yield break;}
        if(testMode=="resume-custom-source-continue"){yield return CheckCustomSourceContinue();yield break;}
        if(testMode=="custom-fresh"){yield return CheckFreshCustom();yield break;}
        if(testMode=="custom-challenge"){yield return CheckCustomChallenge();yield break;}
        if(testMode=="custom-presets"){yield return CheckCustomPresets();yield break;}
        if(testMode=="resume-early-save"){yield return SaveEarlyCustom();yield break;}
        if(testMode=="resume-preset-continue"||testMode=="resume-early-continue"){yield return CheckPresetContinue(title);yield break;}
        if(testMode=="resume-invalid"){yield return CheckInvalidResume(title);yield break;}
        if(testMode.StartsWith("resume-")&&(testMode.EndsWith("save")||testMode.EndsWith("continue")))
        {
            yield return CheckResume(title,testMode.EndsWith("continue"));yield break;
        }
        if(testMode.StartsWith("challenge-"))
        {
            yield return CheckChallenge(title,testMode.EndsWith("relaunch"));yield break;
        }
        if(testMode.StartsWith("powerups-boss-arena-"))
        {
            yield return CheckBossArenas(testMode.EndsWith("baseline"));yield break;
        }
        if(testMode=="powerups-rune-progression-ui")
        {
            yield return CheckRuneProgressionUI(title);yield break;
        }
        if(testMode=="powerups-rune-progression-baseline")
        {
            yield return CheckRuneProgressionBaseline(title);yield break;
        }
        if(testMode=="powerups-rune-progression-fixed"||testMode=="powerups-rune-progression-relaunch")
        {
            yield return CheckRuneProgression(title,testMode.EndsWith("relaunch"));yield break;
        }
        if(testMode.StartsWith("powerups-runes-"))
        {
            yield return CheckRuneContracts(testMode.EndsWith("baseline"));yield break;
        }
        if(testMode.StartsWith("powerups-tome-might-"))
        {
            yield return CheckTomeMight(testMode.EndsWith("baseline"));yield break;
        }
        if(testMode=="powerups-aero")
        {
            yield return CheckAeroDescription();yield break;
        }
        if(testMode.StartsWith("powerups-generator-"))
        {
            yield return CheckGenerator(testMode=="powerups-generator-baseline");yield break;
        }
        if(testMode.StartsWith("holy-spear-"))
        {
            yield return CheckHolySpear(testMode=="holy-spear-baseline");yield break;
        }
        if(testMode=="powerups-fallback")
        {
            yield return CheckFallbackPowerups();yield break;
        }
        if(testMode.StartsWith("achievements-v2-"))
        {
            yield return CheckEncounterAchievements(title,testMode);yield break;
        }
        if(testMode.StartsWith("burn-field-"))
        {
            yield return CheckBurnField(testMode=="burn-field-baseline");yield break;
        }
        if(testMode=="achievements-legacy-stock")
        {
            Require(Progression.Gates.Where(g=>g.Character&&g.Index<12||!g.Character&&g.Index>=0&&g.Index<10).All(g=>Progression.Allowed(g.Data)),"every originally owned character and weapon remains owned from stock-shaped arrays");
            Require(Progression.Gates.Where(g=>g.Id.StartsWith("scenario:")&&g.Data.name!="Challenge_Forest").All(g=>Progression.Allowed(g.Data)),"legacy mode and Darkness-based map access survives the new curve");
            Require(!Progression.Allowed(Progression.Gates.Single(g=>g.Data.name=="Challenge_Forest").Data),"old Windows clear arrays do not invent the new Challenge accomplishment");
            Require(Progression.State.bestLevel==1&&Progression.State.bestSeconds==0&&!Progression.State.facts.Any(f=>f.StartsWith("boss:")||f.StartsWith("perk:")),"migration does not invent historical survival, levels, perks or boss defeats");
            Require(!Progression.Allowed(Plugin.BaseGun)&&!Progression.Allowed(Characters.Data["Haruka"])&&!Progression.Allowed(Characters.Data["Dimond_Portrait"]),"old clears do not invent revised miniboss, build or second-boss conditions for new content");
            Require(Progression.State.version==2&&Progression.State.attempts.Count==0&&Progression.State.pending.Count==0&&Progression.State.notified.Count==26,"legacy ownership produces no retrospective notices or fabricated encounter attempts");
            title.Save();SetPhase("PASS");Log("complete","Stock-shaped legacy ownership/currency/runes/victories preserved; unsupported new encounter awards remain locked");Application.Quit(0);yield break;
        }
        if(testMode=="achievements-complete-relaunch")
        {
            Require(Progression.State.earned.Count==30&&Progression.Gates.All(g=>Progression.Allowed(g.Data)),"every earned character, weapon and scenario survives fresh-process journal/native save loading");
            Require(Progression.Gates.Where(g=>g.Unlock!=null).All(g=>!g.Unlock.IsLocked),"all native unlock controls agree with persisted earned ownership");
            Require(Progression.State.pending.Count==0&&Progression.State.notified.Count==30&&UnlockNotifications.Instance.Showing==null,"all delivered notifications stay deduplicated on complete-profile relaunch");
            title.Save();SetPhase("PASS");Log("complete","Complete native progression and notice delivery retain exact values through fresh-process load/save");Application.Quit(0);yield break;
        }
        if(testMode=="achievements-fresh"||testMode=="achievements-relaunch")
        {
            yield return CheckAchievements(title,testMode=="achievements-relaunch");yield break;
        }
        if(testMode=="achievements-events")
        {
            yield return CheckAchievementEvents(title);yield break;
        }
        if(testMode=="achievements-lifecycle")
        {
            yield return AchievementRun("Katana","MagicBowData","20M_Forest");var lifecycleGame=Object.FindObjectOfType<GameController>();
            var sword=ObjectPooler.SharedInstance.GetPooledObject("PF_MagicSword");sword.transform.position=new Vector3(15,15,0);sword.SetActive(true);
            while(!SummonStarted(sword.GetComponent<SeekerWeapon>()))yield return null;
            Require(Resources.FindObjectsOfTypeAll<SeekerWeapon>().Any(x=>x.gameObject.scene.IsValid()&&!x.gameObject.activeSelf&&object.ReferenceEquals(AccessTools.Field(typeof(Summon),"stats").GetValue(x),null)),"native pool contains both an initialized sword and unused uninitialized members before scene exit");
            yield return AchievementWin(lifecycleGame);SetPhase("PASS");Log("complete","Native Katana/Magic Bow scene lifecycle");Application.Quit(0);yield break;
        }
        if(testMode=="achievements-legacy")
        {
            Require(Progression.Gates.Count==29,"every native character, weapon and gated scenario is registered");
            Require(Progression.State.pending.Count==0,"legacy owned content produces no retrospective notification flood");
            Require(SaveSystem.data.characterHighestClear[Characters.Slots["Haruka"]]==2&&SaveSystem.data.characterHighestClear[Characters.Slots["Dimond_Portrait"]]==3,"existing form victory records survive gate migration");
            Require(SaveSystem.data.points>=321&&SaveSystem.data.runeUnlocks.unlocks[0]==2,"currency and rune purchases survive gate migration");
            title.Save();yield return Capture("legacy-achievements.png");SetPhase("PASS");Log("complete","Legacy ownership, scenario availability and notification suppression");Application.Quit(0);yield break;
        }
        bool characterTest=testMode=="gale"||testMode=="bulwark";
        if(characterTest)
        {
            // The S2 fixture predates achievement-gated ownership. Seed only
            // its required content in the isolated profile; use native locks.
            Progression.State.earned.Add("weapon:ModernizationChargeCannon_None");
            Progression.State.earned.Add("character:"+(testMode=="gale"?"Revamped_Haruka":"Revamped_Dimond_Portrait"));
            Progression.Refresh();Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>();
        }
        bool repeatTest=testMode.StartsWith("repeat-");
        if(testMode=="characters-relaunch")
        {
            Require(Characters.Items(title.characterMenu).Count==15,"native roster retains thirteen originals and exactly two recovered forms on relaunch");
            foreach(var pair in Characters.Slots)Require(SaveSystem.data.characterHighestClear[pair.Value]==(pair.Key=="Haruka"?2:3),"new form victory index persists: "+pair.Key);
            Require(SaveSystem.data.characterHighestClear[2]==4 && SaveSystem.data.runeUnlocks.unlocks[0]==2 && SaveSystem.data.points>=321,"old character victory/rune records remain and currency has not been reset");
            SetPhase("PASS");Log("complete","Native save/relaunch preserves appended character indices and old progression");Application.Quit(0);yield break;
        }
        var guns=NativeSelection.GunItems(title.gunMenu);
        Require(guns.Count==12 && guns.Any(x=>x.data.name=="RevolverData") && guns[GunSlot].data==Plugin.BaseGun,"all eleven original guns remain and Charge Cannon has its own twelfth selection");
        Require(!NativeSelection.Unlocks(title.gunUnlocker)[NativeSelection.UnlockIndex].IsLocked,"new gun is available without mobile purchases");
        Require(SaveSystem.data.gunHighestClear.Length==12,"native save victory data accommodates the new index");
        var gunIcon=guns[GunSlot].GetComponentsInChildren<Image>(true).Single(x=>x.name=="LoadoutItem");
        Require(gunIcon.sprite==Plugin.BaseGun.icon && !gunIcon.GetComponent<Animator>().enabled,"new slot displays recovered Charge Cannon artwork without inherited Revolver animation");
        Require(guns[0].GetComponentsInChildren<Animator>(true).Single().enabled,"original Revolver menu animation remains intact");
        if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="visual-inspect")
        {
            foreach(var item in new[]{guns[0],guns[GunSlot]})
            {
                Log("gun-icon-data",item.name+" dataIcon="+item.data.icon.name);
                foreach(var image in item.GetComponentsInChildren<Image>(true))Log("gun-icon-image",image.name+" sprite="+image.sprite?.name+" matchesData="+(image.sprite==item.data.icon)+" animators="+image.GetComponents<Animator>().Length);
                foreach(var anim in item.GetComponentsInChildren<Animator>(true))Log("gun-icon-animator",anim.name+" controller="+anim.runtimeAnimatorController?.name);
            }
            Log("diagnostic-complete","Native gun icon hierarchy inspected; no gameplay acceptance assertion");Application.Quit(0);yield break;
        }
        if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="persist")
        {
            Require(StatsOverlay.Enabled && ColorfulStyle.Instance.Enabled,"enabled overlay and visual settings persist across process relaunch");
            StatsOverlay.Enabled=false;Plugin.Instance.ColorfulEnabled.Value=false;Plugin.Instance.Config.Save();
            SetPhase("PASS");Log("complete","Both enabled enhancement settings survive a fresh native player process; restored Original afterward");Application.Quit(0);yield break;
        }
        if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="relaunch" || Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="legacy")
        {
            Require(!StatsOverlay.Enabled && !ColorfulStyle.Instance.Enabled,"mod settings persist across process relaunch");
            Require(SaveSystem.data.points==321 && SaveSystem.data.gunHighestClear[0]==3,"existing native progression survives save and relaunch");
            Require(SaveSystem.data.gunUnlocks.unlocks[NativeSelection.UnlockIndex],"new gun unlock survives native save and relaunch");
            if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="legacy")
            {
                Require(SaveSystem.data.gunHighestClear[10]==6 && SaveSystem.data.gunHighestClear[11]==-1 && SaveSystem.data.gunUnlocks.unlocks[2] && SaveSystem.data.gunUnlocks.unlocks[9],"stock-shaped legacy gun arrays retain original unlocks and victory indices");
                Require(SaveSystem.data.characterUnlocks.unlocks[2] && SaveSystem.data.characterHighestClear[2]==4 && SaveSystem.data.runeUnlocks.unlocks[0]==2,"legacy character and rune progression survives migration");
                title.Save();
            }
            SetPhase("PASS");Log("complete","Independent production plugin retains native progression and mod settings across relaunch");Application.Quit(0);yield break;
        }

        if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="colorful")
        {
            Log("diagnostic-action","Enter native title Options state to test visual setting");title.ChangeState<OptionsMenuState>();
            while(!Active(title.optionsMenu) || title.optionsMenu.GetComponent<CanvasGroup>().alpha<.999f || title.mainMenu.GetComponent<CanvasGroup>().alpha>.001f)yield return null;
            Require(!ColorfulStyle.Instance.Enabled,"Original is default for new visual setting");
            yield return Capture("options-original.png");Click(ColorfulStyle.Instance.SettingButton);
            Require(ColorfulStyle.Instance.Enabled && ColorfulStyle.Instance.AppliedCount>0,"native setting applies compatible Colorful textures");
            yield return Capture("options-colorful.png");
            title.ChangeState<TitleMainMenuState>();while(!Active(title.mainMenu))yield return null;
        }
        if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="overlay")
        {
            Log("diagnostic-action","Enter native title Options state directly to exercise added setting button");
            title.ChangeState<OptionsMenuState>();
            while(!Active(title.optionsMenu))yield return null;
            while(title.optionsMenu.GetComponent<CanvasGroup>().alpha<.999f || title.mainMenu.GetComponent<CanvasGroup>().alpha>.001f)yield return null;
            var setting=StatsOverlay.Instance.SettingButton;
            if(!StatsOverlay.Enabled)Click(setting);
            Click(setting);Require(!StatsOverlay.Enabled,"Options setting disables stats");
            Click(setting);Require(StatsOverlay.Enabled,"Options setting enables stats");
            yield return Capture("options-stats.png");
            title.ChangeState<TitleMainMenuState>();while(!Active(title.mainMenu))yield return null;
        }
        Click(Entries(title.mainMenu)[0].button);
        while(!Active(title.characterMenu))yield return null;
        SetPhase("selecting-character");
        if(characterTest)
        {
            while(title.characterMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
            Require(Characters.Items(title.characterMenu).Count==15,"all thirteen original characters retained with two appended native entries");
            int slot=Characters.Slots[testMode=="gale"?"Haruka":"Dimond_Portrait"];Entries(title.characterMenu)[slot].button.Select();
            Snapshot("character-selection-ui.json");yield return Capture("character-selection.png");Click(Entries(title.characterMenu)[slot].button);
            GunSlot=0;
        }
        else Click(Entries(title.characterMenu)[0].button);
        while(!Active(title.gunMenu) || title.gunMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
        if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="revolver" || repeatTest)GunSlot=guns.FindIndex(x=>x.data.name=="RevolverData");
        SetPhase("selecting-weapon");
        Entries(title.gunMenu)[GunSlot].button.Select();
        Snapshot("selection-ui.json");yield return Capture("selection.png");
        Click(Entries(title.gunMenu)[GunSlot].button);
        while(!(title.CurrentState is ModeSelectState))yield return null;
        Require(Loadout.GunSelection==guns[GunSlot].data,"native selection passes selected independent weapon to Loadout");
        if(repeatTest)
        {
            var modes=(Toggle[])AccessTools.Field(typeof(ToggleGroupMenu<MapData>),"toggles").GetValue(title.modeSelectMenu);
            var endless=modes.Single(x=>x.GetComponent<DataUIBinding<MapData>>().data.endless);Require(endless.gameObject.activeInHierarchy&&endless.IsInteractable(),"native Endless option is selectable");endless.isOn=true;
        }
        Click(title.modeSelectStartButton);
        if(!repeatTest){while(!(title.CurrentState is MapSelectState))yield return null;Click(title.mapSelectStartButton);}
        GameController game;
        while((game=Object.FindObjectOfType<GameController>())==null || !(game.CurrentState is CombatState))yield return null;
        var player=PlayerController.Instance;
        if(repeatTest){yield return CheckRepeatPerk(game,player,testMode=="repeat-baseline");SetPhase("PASS");Application.Quit(0);yield break;}
        if(characterTest)
        {
            yield return CheckCharacter(game,player,testMode);
            SetPhase("PASS");Log("complete","Recovered native character selection, ability, exclusives, visuals, cleanup and original restart: "+testMode);Application.Quit(0);yield break;
        }
        if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="revolver")
        {
            yield return CheckRevolver(game,player);
            SetPhase("PASS");Log("complete","Original Revolver remains selectable and playable alongside added Charge Cannon");Application.Quit(0);yield break;
        }
        while(player.gun.shooters.Count!=1 || !(player.gun.shooters[0] is PortedShooter) || !((PortedShooter)player.gun.shooters[0]).Ready)yield return null;
        SetPhase("combat-base");
        OptionsSetter.AutoReloadEnabled=false;
        var mouse=fixtureMouse;var keyboard=fixtureKeyboard;
        game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",keyboard,mouse);
        game.playerInput.neverAutoSwitchControlSchemes=true;
        Require(player.gun.gunData==Plugin.BaseGun && player.ammo.amount==5,"base weapon integrated with 5-round ammo");
        // Keep this fixture alive without changing combat damage calculations.
        player.playerHealth.isInvincible.Flip();
        if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="colorful")
        {
            yield return CheckColorful(game,player,mouse,keyboard);
            StatsOverlay.Enabled=true;Plugin.Instance.ColorfulEnabled.Value=true;Plugin.Instance.Config.Save();
            SetPhase("PASS");Log("complete","Native visual setting, scene transition and exact original pixel restoration");
            InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);Application.Quit(0);yield break;
        }
        if(Environment.GetEnvironmentVariable("MODERNIZATION_TEST")=="overlay")
        {
            yield return CheckOverlay(game,player,mouse,keyboard);
            SetPhase("PASS");Log("complete","Native overlay, live modifiers, skill counts, toggles and gameplay input");
            InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);Application.Quit(0);yield break;
        }
        GameObject enemy;
        while((enemy=EnemyFinder.GetClosestEnemy(player.transform.position))==null)yield return null;
        foreach(var ai in enemy.GetComponents<AIComponent>())ai.enabled=false;
        var move=enemy.GetComponent<MoveComponent2D>();if(move!=null)move.enabled=false;
        var body=enemy.GetComponent<Rigidbody2D>();if(body!=null)body.velocity=Vector2.zero;
        enemy.transform.position=player.transform.position+Vector3.right*2f;
        var health=enemy.GetComponent<Health>();health.maxHP=10000;TargetId=enemy.GetInstanceID();
        Log("fixture",new{target=enemy.name,hp=health.HP,position=enemy.transform.position});
        // Real input state events flow through the existing Fire action and player states.
        yield return Shot(mouse,.1f,Evolution.None,20f,1,enemy);
        while(DamageEvents==0)yield return null;
        Require(TargetDamage==20,"real projectile collision changes enemy HP by 20");
        yield return Shot(mouse,.9f,Evolution.None,44f,1,null);
        yield return Shot(mouse,1.6f,Evolution.None,80f,1,null);
        // Existing damage and projectile modifiers must compose with the new weapon.
        player.stats[StatType.BulletDamage].AddMultiplierBonus(.5f);
        player.stats[StatType.Projectiles].AddFlatBonus(1);
        yield return Shot(mouse,.9f,Evolution.None,66f,2,null);
        player.stats[StatType.Projectiles].AddFlatBonus(-1);
        player.stats[StatType.BulletDamage].AddMultiplierBonus(-.5f);
        yield return Shot(mouse,.1f,Evolution.None,20f,1,null);
        while(!player.ammo.fullOnAmmo || !(player.CurrentState is flanne.Player.IdleState))yield return null;
        Require(player.ammo.amount==5,"empty magazine follows native reload path");
        Log("base-reload",new{duration=player.gun.reloadDuration});
        foreach(var mode in new[]{Evolution.OverloadShell,Evolution.ArmorPierce,Evolution.DualBarrage})
        {
            // Isolate each evolution without stacking previous evolution stat changes.
            player.gun.LoadGun(Plugin.BaseGun);player.ammo.Reload();
            SetPhase("evolution-"+mode);
            Log("diagnostic-action","Seeded level 19 and zero XP, then native GainXP triggers the actual level-20 evolution transition; did not simulate the preceding 19 levels");
            var xp=game.playerXP;
            AccessTools.Field(typeof(PlayerXP),"<level>k__BackingField").SetValue(xp,19);
            AccessTools.Field(typeof(PlayerXP),"xp").SetValue(xp,0f);
            xp.GainXP((float)AccessTools.Property(typeof(PlayerXP),"xpToLevel").GetValue(xp)+1f);
            while(!(game.CurrentState is GunEvoMenuState))yield return null;
            while(!(AccessTools.Field(typeof(flanne.UI.Menu),"ClickEvent").GetValue(game.gunEvoMenu) is Delegate))yield return null;
            Snapshot("evolution-"+mode+"-ui.json");yield return Capture("evolution-"+mode+".png");
            var choices=Enumerable.Range(0,3).Select(i=>game.gunEvoMenu.GetEntry(i)).ToArray();
            var choice=choices.Single(b=>b.GetComponent<GunEvoUI>().data.name=="Modernization_"+mode);
            Click(choice);
            while(!(game.CurrentState is CombatState) || !Plugin.TryMode(player.gun.gunData,out var equipped) || equipped!=mode || !((PortedShooter)player.gun.shooters[0]).Ready)yield return null;
            Require(Plugin.TryMode(player.gun.gunData,out var verifiedMode) && verifiedMode==mode,"native evolution menu equips "+mode);
            if(mode==Evolution.ArmorPierce)yield return Shot(mouse,.1f,mode,20f,1,null);
            yield return Shot(mouse,1.6f,mode,mode==Evolution.ArmorPierce?92f:mode==Evolution.DualBarrage?64f:80f,mode==Evolution.DualBarrage?2:1,null);
        }
        Snapshot("combat-ui.json");yield return Capture("combat.png");
        SetPhase("native-scene-lifecycle");
        var previousShooter=player.gun.shooters[0];var previousPlayer=player;
        PauseController.SharedInstance.Pause();game.ChangeState<PauseState>();Click(game.giveupButton);
        while(!(game.CurrentState is PlayerDeadState) || !game.quitToTitleButton.gameObject.activeInHierarchy || !game.quitToTitleButton.IsInteractable())yield return null;
        Click(game.quitToTitleButton);
        while(SceneManager.GetActiveScene().name!="TitleScreen" || (title=Object.FindObjectOfType<TitleScreenController>())==null || !Active(title.mainMenu))yield return null;
        Require(previousPlayer==null && previousShooter==null,"returning through native Give Up and title navigation destroys the previous player and charge shooter");
        Require(NativeSelection.GunItems(title.gunMenu).Count==12,"returning to title retains all original guns and exactly one added gun");
        Click(Entries(title.mainMenu)[0].button);while(!Active(title.characterMenu))yield return null;
        Click(Entries(title.characterMenu)[0].button);while(!Active(title.gunMenu))yield return null;
        Click(Entries(title.gunMenu)[0].button);while(!(title.CurrentState is ModeSelectState))yield return null;
        Click(title.modeSelectStartButton);while(!(title.CurrentState is MapSelectState))yield return null;Click(title.mapSelectStartButton);
        while((game=Object.FindObjectOfType<GameController>())==null || !(game.CurrentState is CombatState))yield return null;
        yield return CheckRevolver(game,PlayerController.Instance);
        InputSystem.RemoveDevice(mouse);InputSystem.RemoveDevice(keyboard);
        SaveSystem.data.points=321;SaveSystem.data.gunHighestClear[0]=3;
        SaveSystem.Save();
        Require(File.Exists(Path.Combine(Application.persistentDataPath,"gamedata.json")),"native save written in isolated profile");
        StatsOverlay.Enabled=false;Plugin.Instance.ColorfulEnabled.Value=false;Plugin.Instance.Config.Save();
        SetPhase("PASS");Log("complete",new{shots=PortedShooter.Shots,projectiles=Projectiles,enemyDamageEvents=DamageEvents,nativeLevel20Evolution=true,nativeSceneReturnAndRevolverRestart=true});
        Application.Quit(0);
    }
    IEnumerator CheckRevolver(GameController game,PlayerController player,string capture="original-revolver-combat.png",string expectedRecipe="RevolverData")
    {
        Require(player.gun.gunData.name==expectedRecipe && !(player.gun.shooters[0] is PortedShooter),"native weapon retains the expected recipe and native shooter: "+expectedRecipe);
        // Fire is ignored while the native pause owner holds input. Observe
        // that actual state before delivering the synthetic input event.
        while(PauseController.isPaused)yield return null;
        game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
        bool fired=false;UnityEngine.Events.UnityAction onShot=()=>fired=true;player.gun.OnShoot.AddListener(onShot);
        var aim=(Vector2)Camera.main.WorldToScreenPoint(player.transform.position+Vector3.up*5);
        InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));
        while(!fired||(!player.ammo.infiniteAmmo.value&&player.ammo.amount==player.gun.maxAmmo))yield return null;
        player.gun.OnShoot.RemoveListener(onShot);
        if(Characters.TryContract(player.loadedCharacter,out var contract))Require(Mathf.Abs(player.moveSpeedMultiplier-Mathf.Min(1,player.stats[StatType.WalkSpeed].Modify(contract.shootMoveSpeedMultiplier)))<.001f,"recovered character shooting movement multiplier composes with native Walk Speed");
        else if(player.loadedCharacter.name=="Shana")Require(Mathf.Abs(player.moveSpeedMultiplier-Mathf.Min(1,player.stats[StatType.WalkSpeed].Modify(.45f)))<.001f,"Android Shana shooting movement composes with native Walk Speed");
        InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});
        Require(fired&&(player.ammo.infiniteAmmo.value||player.ammo.amount<player.gun.maxAmmo),"original Revolver fires with native ammunition rules");
        yield return Capture(capture);
        while(!(player.CurrentState is flanne.Player.IdleState))yield return null;
    }
    IEnumerator Shot(Mouse mouse,float hold,Evolution mode,float damage,int expectedProjectiles,GameObject target)
    {
        var player=PlayerController.Instance;
        while(!(player.CurrentState is flanne.Player.IdleState))yield return null;
        if(player.ammo.outOfAmmo)throw new Exception("Shot fixture has no ammo");
        var shooter=(PortedShooter)player.gun.shooters[0];
        int shots=PortedShooter.Shots,projectiles=Projectiles,ammo=player.ammo.amount;
        var point=target!=null?target.transform.position:player.transform.position+Vector3.up*10f;
        var screenPoint=(Vector2)Camera.main.WorldToScreenPoint(point);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=screenPoint}.WithButton(MouseButton.Left));
        while(!shooter.Charging || shooter.ChargeTime<hold)yield return null;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=screenPoint});
        while(PortedShooter.Shots==shots)yield return null;
        Require(Mathf.Abs(PortedShooter.LastDamage-damage)<.01f,"native input "+mode+" hold="+hold+" damage="+damage);
        Require(Projectiles-projectiles==expectedProjectiles,"projectile count="+expectedProjectiles);
        Require(player.ammo.amount==ammo-1,"one ammo consumed per release");
        var plan=PortedShooter.LastPlan;
        Require(plan.PierceAll==(hold>=1.5f || mode==Evolution.ArmorPierce),"piercing matches charge/evolution");
        Require(plan.SizeMultiplier==(mode==Evolution.OverloadShell && hold>=1.5f?3f:1f),"size matches evolution");
    }
    [Serializable]class UiNode{public string id,text,role;public bool active,interactable;public float[] bounds;}
    IEnumerator Capture(string file)
    {
        // Batch players have no usable backbuffer. Render the native cameras and
        // overlay canvases explicitly into a GPU texture, without a desktop window.
        yield return null;Canvas.ForceUpdateCanvases();
        var cameraObject=new GameObject("OffscreenUiCamera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=360;camera.transform.position=new Vector3(0,0,-1000);camera.nearClipPlane=.1f;camera.farClipPlane=10;camera.clearFlags=CameraClearFlags.Depth;camera.cullingMask=-1;
        var overlays=Resources.FindObjectsOfTypeAll<Canvas>().Where(x=>x.isRootCanvas && x.gameObject.activeInHierarchy && x.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var previousCameras=overlays.Select(x=>x.worldCamera).ToArray();
        var previousDistances=overlays.Select(x=>x.planeDistance).ToArray();
        var cameras=Camera.allCameras.Where(x=>x.enabled && x.targetTexture==null).OrderBy(x=>x.depth).ToArray();
        var previousTargets=cameras.Select(x=>x.targetTexture).ToArray();
        var previousRects=cameras.Select(x=>x.rect).ToArray();var previousOrtho=cameras.Select(x=>x.orthographicSize).ToArray();
        var pixelPerfect=cameras.SelectMany(x=>x.GetComponents<Behaviour>()).Where(x=>x.GetType().Name=="PixelPerfectCamera" && x.enabled).ToArray();
        int width=Screen.width,height=Screen.height;var rt=RenderTexture.GetTemporary(width,height,24);var active=RenderTexture.active;
        try
        {
            foreach(var pp in pixelPerfect)pp.enabled=false;
            for(int i=0;i<cameras.Length;i++){cameras[i].targetTexture=rt;cameras[i].rect=new Rect(0,0,1,1);cameras[i].orthographicSize=previousOrtho[i];Log("offscreen-camera",cameras[i].name+" layers="+cameras[i].cullingMask);}
            camera.targetTexture=rt;
            for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceCamera;overlays[i].worldCamera=camera;overlays[i].planeDistance=camera.nearClipPlane+.1f;}
            Canvas.ForceUpdateCanvases();
            foreach(var c in cameras)c.Render();
            camera.Render();
            RenderTexture.active=rt;var t=new Texture2D(width,height,TextureFormat.RGB24,false);t.ReadPixels(new Rect(0,0,width,height),0,0);t.Apply();
            File.WriteAllBytes(Path.Combine(ResultDir,file),t.EncodeToPNG());Object.Destroy(t);
            Log("offscreen-capture",file);
        }
        finally
        {
            for(int i=0;i<cameras.Length;i++){cameras[i].targetTexture=previousTargets[i];cameras[i].rect=previousRects[i];cameras[i].orthographicSize=previousOrtho[i];}
            foreach(var pp in pixelPerfect)pp.enabled=true;
            for(int i=0;i<overlays.Length;i++){overlays[i].renderMode=RenderMode.ScreenSpaceOverlay;overlays[i].worldCamera=previousCameras[i];overlays[i].planeDistance=previousDistances[i];}
            camera.targetTexture=null;Object.Destroy(cameraObject);RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);Canvas.ForceUpdateCanvases();
        }
    }
    [Serializable]class UiSnapshot{public string scene,phase;public int width,height;}
    static void Snapshot(string file)
    {
        Canvas.ForceUpdateCanvases();var nodes=new List<UiNode>();
        foreach(var b in Resources.FindObjectsOfTypeAll<Button>().Where(x=>x.gameObject.scene.IsValid() && x.gameObject.activeInHierarchy))
        {
            var rt=b.transform as RectTransform;var cs=new Vector3[4];rt.GetWorldCorners(cs);
            nodes.Add(new UiNode{id=b.name,text=b.GetComponentInChildren<TMP_Text>()?.text,role="button",active=b.gameObject.activeInHierarchy,interactable=b.IsInteractable(),bounds=new[]{cs[0].x,cs[0].y,cs[2].x,cs[2].y}});
        }
        var header=JsonUtility.ToJson(new UiSnapshot{scene=SceneManager.GetActiveScene().name,phase=phase,width=Screen.width,height=Screen.height});
        File.WriteAllText(Path.Combine(ResultDir,file),header.TrimEnd('}')+",\"nodes\":["+string.Join(",",nodes.Select(n=>JsonUtility.ToJson(n)))+"]}");
    }
}
[HarmonyPatch(typeof(ProjectileFactory),nameof(ProjectileFactory.SpawnProjectile))]
class ProjectileProbe{static void Postfix(){RuntimeChecks.Projectiles++;}}
[HarmonyPatch(typeof(Health),nameof(Health.TakeDamage))]
class DamageProbe
{
    static void Prefix(Health __instance,out int __state){__state=__instance.HP;}
    static void Postfix(Health __instance,int __state)
    {
        if(__instance.gameObject.GetInstanceID()!=RuntimeChecks.TargetId || __state==__instance.HP)return;
        RuntimeChecks.DamageEvents++;RuntimeChecks.TargetDamage+=__state-__instance.HP;
        RuntimeChecks.Log("enemy-hp",new{before=__state,after=__instance.HP});
    }
}
