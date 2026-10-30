using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;
using flanne;
using flanne.Core;
using flanne.UI;
using flanne.UIExtensions;
using flanne.PerkSystem;
using Object=UnityEngine.Object;

namespace TwentyMinutes.Modernization;
[BepInPlugin(Id,"20MTD Modernization","1.3.17")]
public class Plugin:BaseUnityPlugin
{
    public const string Id="local.20mtd.modernization";
    public static Plugin Instance;
    public static event System.Action<string,object> Diagnostic;
    public static void Trace(string kind,object value){Diagnostic?.Invoke(kind,value);}
    public ConfigEntry<MetricsView> HudMode;
    public ConfigEntry<bool> ColorfulEnabled;
    public ConfigEntry<int> ZoomOut;
    public ConfigEntry<bool> DamageNumbers;
    public ConfigEntry<int> ProjectileOpacity,EffectsOpacity,HudOpacity;
    public static readonly Dictionary<string,string> Text=new Dictionary<string,string>();
    public static readonly Dictionary<GunData,Evolution> Guns=new Dictionary<GunData,Evolution>();
    public static bool TryMode(GunData data,out Evolution mode)
    {
        // PlayerPerks.Equip deep-copies GunEvolution and its GunData. Use the
        // registered serialized content key, rather than Unity object identity.
        foreach(var pair in Guns)
            if(data!=null && data.nameStringID.key==pair.Key.nameStringID.key){mode=pair.Value;return true;}
        mode=default;return false;
    }
    public static GunData BaseGun;
    public static Sprite[] Sprites;
    public static readonly Dictionary<Evolution,Sprite[]> ModeSprites=new Dictionary<Evolution,Sprite[]>();
    public static string AssetRoot=>Path.Combine(Path.GetDirectoryName(Instance.Info.Location),"Assets");
    public void Awake()
    {
        Instance=this;
        // Original Android TimeManager. FixedUpdate owners and projectile
        // sampling must run at its shipped cadence, not Windows'0.02.
        Time.fixedDeltaTime=.03f;
        Text["rune_menu_instructions"]="Select to toggle · Earn ranks from arena bosses";
        Text["currency_earned_label"]="Total Score";
        Text["thunderspawns_description"]="Your summons have a <color=#f5d6c1>15%</color> chance on hit to call down <color=#f5d6c1>Lightning</color> for <color=#f5d6c1>22</color> damage.";
        Text["soul_knight_description"]="<color=#f5d6c1>Magic Spear</color> gains <color=#f5d6c1>+15</color> damage per <color=#f5d6c1>Soul Heart</color> you currently hold.";
        Text["rune_description_cadence"]="Every 3rd shot gains +99 Piercing, +20% Bullet Size per rank and +20% Bullet Damage per rank.";
        Text["rune_description_tenacity"]="When your HP is at or below half of your Max HP, gain +3% Fire Rate and +5% Knockback per rank.";
        var legacyMetrics=Config.Bind("Enhancements","StatsOverlay",true,"Legacy enabled value used only when migrating to MetricsMode.");
        HudMode=Config.Bind("Enhancements","MetricsMode",legacyMetrics.Value?MetricsView.Extended:MetricsView.Off,"F2 cycles Off, Little and Extended metrics. Selected skill paths remain visible. Little shows final values; Extended adds base and bonus. Pastel values show benefit relative to the starting value; shorter reload time is green.");
        ColorfulEnabled=Config.Bind("Enhancements","ColorfulStyle",false,"Optional compatible Sprites Reborn art (Law / Daramos / Darkly77). Original is default. Selected over older Colorful Overhaul by measured current-build coverage.");
        ZoomOut=Config.Bind("Enhancements","CameraZoomOut",0,new ConfigDescription("Extra world view in percent. 0 retains the original camera; menus and HUD keep their normal size.",new AcceptableValueList<int>(0,25,50,75,100)));
        DamageNumbers=Config.Bind("Enhancements","DamageNumbers",true,"Show damage numbers over enemies. Controlled through native Options; frame-rate dips never change this choice.");
        new Harmony(Id).PatchAll(typeof(Plugin).Assembly);
        Logger.LogInfo("Native gameplay modernization loaded");
        gameObject.AddComponent<StatsOverlay>();
        gameObject.AddComponent<ColorfulStyle>();
        gameObject.AddComponent<TitleArtwork>();
        gameObject.AddComponent<UnlockNotifications>();
        gameObject.AddComponent<MenuFocusArrow>();
        ProjectileOpacity=Config.Bind("Enhancements","PlayerProjectileOpacity",100,new ConfigDescription("Player bullet/fireball/summon-projectile opacity percent; visual only. Hostile shots are unchanged.",new AcceptableValueList<int>(25,50,75,100)));
        EffectsOpacity=Config.Bind("Enhancements","PlayerEffectsOpacity",100,new ConfigDescription("Player ability/summon/explosion/trail/particle opacity percent; character, enemies, HUD and vision are unchanged.",new AcceptableValueList<int>(25,50,75,100)));
        HudOpacity=Config.Bind("Enhancements","GameplayHudOpacity",85,new ConfigDescription("Gameplay HUD opacity percent. Menus, mobs, attacks and vision retain their own appearance.",new AcceptableValueList<int>(80,85,90)));
    }
    public void LoggerForArtwork(string message)=>Logger.LogInfo(message);
    public static void SetField(object target,string field,object value)=>AccessTools.Field(target.GetType(),field).SetValue(target,value);
    static Sprite LoadSprite(string file,float ppu)
    {
        var t=new Texture2D(2,2,TextureFormat.RGBA32,false);t.LoadImage(File.ReadAllBytes(Path.Combine(AssetRoot,file)));t.filterMode=FilterMode.Point;
        return Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),ppu);
    }
    public static void PrepareGun(GunData original)
    {
        if(BaseGun!=null)return;
        Sprites=Enumerable.Range(0,7).Select(i=>LoadSprite("T_ChargeCannon_SS_"+i+".png",original.icon.pixelsPerUnit)).ToArray();
        ModeSprites[Evolution.None]=Sprites.Take(3).ToArray();
        foreach(var pair in new[]{new{mode=Evolution.OverloadShell,prefix="weapon-a_"},new{mode=Evolution.ArmorPierce,prefix="weapon-b_"},new{mode=Evolution.DualBarrage,prefix="weapon-c_"}})
            ModeSprites[pair.mode]=Enumerable.Range(1,3).Select(i=>LoadSprite(pair.prefix+i+".png",original.icon.pixelsPerUnit)).ToArray();
        var cannonProjectile=AndroidProjectileTemplates.Cannon(original.bullet);
        foreach(Evolution mode in Enum.GetValues(typeof(Evolution)))
        {
            var data=Object.Instantiate(original);data.name="ModernizationChargeCannon_"+mode;
            data.nameStringID="modernization_charge_name_"+mode;data.descriptionStringID="modernization_charge_desc";
            Text[data.nameStringID.key]="Charge Cannon";
            data.damage=20f;data.shotCooldown=.4f;data.maxAmmo=5;data.reloadDuration=2.5f;
            data.numOfProjectiles=mode==Evolution.DualBarrage?2:1;data.spread=mode==Evolution.DualBarrage?30f:0f;
            data.knockback=5f;data.projectileSpeed=28f;data.bounce=0;data.piercing=0;data.inaccuracy=5f;
            data.icon=Sprites[0];data.disableManualReload=false;data.isSummonGun=false;
            data.bullet=cannonProjectile;data.model=original.model;data.gunEvolutions=new List<GunEvolution>();
            Guns[data]=mode;Object.DontDestroyOnLoad(data);
            if(mode==Evolution.None)BaseGun=data;
        }
        Text["modernization_charge_name"]="Charge Cannon ";
        Text["modernization_charge_desc"]="Hold to charge, release to fire. 0.8s: 2.2x; 1.5s: 4x and piercing.";
        foreach(Evolution mode in new[]{Evolution.OverloadShell,Evolution.ArmorPierce,Evolution.DualBarrage})
        {
            var evo=ScriptableObject.CreateInstance<GunEvolution>();evo.name="Modernization_"+mode;evo.icon=ModeSprites[mode][0];
            string nk="modernization_"+mode+"_name",dk="modernization_"+mode+"_desc";
            Text[nk]=mode==Evolution.OverloadShell?"Overload Shell":mode==Evolution.ArmorPierce?"Armor-Piercing Cannon":"Dual Barrage";
            Text[dk]=mode==Evolution.OverloadShell?"Full-charge bullet size +200%.":mode==Evolution.ArmorPierce?"All shells pierce. Full charge deals +15% damage.":"Two shells per charged shot.";
            SetField(evo,"nameStrID",new LocalizedString(nk));SetField(evo,"desStrID",new LocalizedString(dk));
            SetField(evo,"gunData",Guns.Single(x=>x.Value==mode).Key);
            SetField(evo,"statChanges",mode==Evolution.DualBarrage?new[]{new StatChange{type=StatType.BulletDamage,isFlatMod=false,value=-.2f}}:new StatChange[0]);
            SetField(evo,"effects",new PerkEffect[0]);Object.DontDestroyOnLoad(evo);BaseGun.gunEvolutions.Add(evo);
        }
        Instance.Logger.LogInfo("Recovered-data Charge Cannon and three evolutions registered");
    }
}

[HarmonyPatch(typeof(LocalizationSystem),"GetLocalizedValue")]
class TextPatch
{
    static bool Prefix(string key,ref string __result)
    {if(Plugin.Text.TryGetValue(key,out var text)){__result=text;return false;}return true;}
}
[HarmonyPatch(typeof(Gun),nameof(Gun.LoadGun))]
class LoadPatch
{
    static void Postfix(Gun __instance)
    {
        Plugin.Trace("gun-load",new{name=__instance.gunData.name,recognized=Plugin.TryMode(__instance.gunData,out var identified)});
        StatsOverlay.Instance?.Dirty();
        if(!Plugin.TryMode(__instance.gunData,out var mode))return;
        foreach(var old in __instance.shooters) {old.enabled=false;Object.Destroy(old);}
        var shooter=__instance.gunObj.AddComponent<PortedShooter>();shooter.Mode=mode;shooter.onShoot=new UnityEvent();
        shooter.onShoot.AddListener(()=>__instance.OnShoot.Invoke());Plugin.SetField(__instance,"<shooters>k__BackingField",new List<Shooter>{shooter});
        foreach(var animator in __instance.gunAnimators)animator.enabled=false;
        shooter.Renderer=__instance.gunObj.GetComponentInChildren<SpriteRenderer>();shooter.Renderer.sprite=Plugin.ModeSprites[mode][0];
    }
}
public class PortedShooter:Shooter
{
    public Evolution Mode;public SpriteRenderer Renderer;
    public float ChargeTime;public bool Charging;public bool Ready;
    public static ShotPlan LastPlan;public static float LastDamage;public static int Shots;
    public override bool fireOnStop=>true;
    public override void Init(){Ready=true;}
    public override void Shoot(ProjectileRecipe r,Vector2 direction,int n,float spread,float inaccuracy)
    {if(!Charging){Charging=true;ChargeTime=0;}}
    void Update()
    {
        if(!Charging || PauseController.isPaused)return;
        ChargeTime+=Time.deltaTime;
        int tier=ChargeRules.Plan(ChargeTime,Mode).Tier;
        Renderer.sprite=Plugin.ModeSprites[Mode][tier];
    }
    public override void OnStopShoot(ProjectileRecipe r,Vector2 direction,int n,float spread,float inaccuracy)
    {
        if(!Charging)return;
        // Source release checks current ammo before changing the recipe or
        // invoking base Shoot; an empty release still resets its charge.
        if(PlayerController.Instance.ammo.amount==0){ResetCharge();return;}
        LastPlan=ChargeRules.Plan(ChargeTime,Mode);
        r.damage=Mathf.RoundToInt(r.damage*LastPlan.DamageMultiplier);r.size*=LastPlan.SizeMultiplier;
        if(LastPlan.PierceAll)r.piercing+=999;
        LastDamage=r.damage;Shots++;
        Plugin.Trace("shot",new{mode=Mode.ToString(),seconds=ChargeTime,tier=LastPlan.Tier,damage=r.damage,size=r.size,piercing=r.piercing,projectiles=n});
        base.Shoot(r,direction,n,spread,inaccuracy);
        ResetCharge();
    }
    void ResetCharge(){Charging=false;ChargeTime=0;Renderer.sprite=Plugin.ModeSprites[Mode][0];}
}
