using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using flanne;
using flanne.Core;
using flanne.Player;
using flanne.UI;
using flanne.UIExtensions;
using flanne.TitleScreen;
using flanne.CharacterPassives;
using flanne.PerkSystem;
using flanne.PerkSystem.Triggers;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// Only the two recovered playable forms. The source manifest is a local asset
// extraction product; no Android assembly or Unity 2022 asset bundle is loaded.
public static class Characters
{
 public class Manifest{public Contract[] characters;public SpriteRecord[] sprites;}
 public class SpriteRecord{public string key,name,file;public float pivotX,pivotY,ppu;}
 public class Frame{public float time;public string sprite;}
 public class Clip{public string name;public float duration;public bool loop;public Frame[] frames;}
 public class Pose{public string name;public Vector3 position,scale;public Quaternion rotation;}
 public class Effect{public string field;public float secondsPerFrame;public bool loop;public string[] frames,alternateFrames;public Pose[] transforms;}
 public class Fog{public string sprite;public Pose[] transforms;}
 public class Stat{public string type;public int flatValue,isFlatMod;public float value;}
 public class Upgrade{public string key,icon,name,description;public Stat[] stats;}
 public class SkillData{public float cooldown,cd,reduceCd,duration,EffectDuration,EffectRange,secondsPerTick,chargeSpeedMulti,chargeDuration,chargeDamageMultiplier,baseKnockback,nextShotDamageMultiplier;public int damageAmount;public bool triggerOnHit;}
 public class Contract{public string key,name,description,portrait,icon,skillIcon;public int startHP;public float moveSpeed,shootMoveSpeedMultiplier,colliderRadius;public Clip[] clips;public SkillData skill;public Effect[] effects;public Upgrade[] powers;public Fog fog;}
 public static readonly Dictionary<string,Sprite> Sprites=new Dictionary<string,Sprite>();
 public static readonly Dictionary<string,Contract> Contracts=new Dictionary<string,Contract>();
 public static readonly Dictionary<string,CharacterData> Data=new Dictionary<string,CharacterData>();
 public static readonly Dictionary<string,int> Slots=new Dictionary<string,int>(),UnlockSlots=new Dictionary<string,int>();
 public static readonly Dictionary<string,Powerup> Powers=new Dictionary<string,Powerup>();
 public static InputActionAsset Inputs;
 public static SpriteTrail TrailTemplate;
 public static int Count;
 public static List<DataUI<CharacterData>> Items(CharacterMenu menu)=>(List<DataUI<CharacterData>>)AccessTools.Field(typeof(DataListMenu<CharacterData>),"dataUIs").GetValue(menu);
 public static bool TryContract(CharacterData data,out Contract c)
 {c=null;return data!=null && Contracts.TryGetValue(data.nameStringID.key.Replace("revamped_character_",""),out c);}
 static void Prepare(CharacterData shana,CharacterData hina)
 {
  if(Data.Count>0)return;
  Manifest manifest;
  using(var stream=File.OpenRead(Path.Combine(Plugin.AssetRoot,"characters/characters.json")))manifest=(Manifest)new DataContractJsonSerializer(typeof(Manifest)).ReadObject(stream);
  Plugin.Trace("character-assets",new{characters=manifest.characters?.Length,sprites=manifest.sprites?.Length});
  foreach(var record in manifest.sprites)
  {
   var t=new Texture2D(2,2,TextureFormat.RGBA32,false);t.LoadImage(File.ReadAllBytes(Path.Combine(Plugin.AssetRoot,"characters",record.file)));t.filterMode=FilterMode.Point;t.name="Revamped_"+record.name;
   var sprite=Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(record.pivotX,record.pivotY),record.ppu);sprite.name=record.name;Object.DontDestroyOnLoad(t);Object.DontDestroyOnLoad(sprite);Sprites.Add(record.key,sprite);
  }
  var nativeSkill=hina.passivePrefab.GetComponentInChildren<SkillPassive>(true);
  Plugin.Trace("character-native-skill",new{passive=hina.passivePrefab.name,skill=nativeSkill?.GetType().FullName,components=string.Join(",",hina.passivePrefab.GetComponentsInChildren<MonoBehaviour>(true).Select(x=>x?.GetType().FullName))});
  Inputs=(InputActionAsset)AccessTools.Field(typeof(SkillPassive),"inputs").GetValue(nativeSkill);
  TrailTemplate=hina.passivePrefab.GetComponentInChildren<SpriteTrail>(true);
  foreach(var c in manifest.characters)
  {
   Contracts.Add(c.key,c);var data=Object.Instantiate(shana);data.name="Revamped_"+c.key;data.nameStringID="revamped_character_"+c.key;data.descriptionStringID="revamped_description_"+c.key;
   Plugin.Text[data.nameStringID.key]=c.name;
   Plugin.Text[data.descriptionStringID.key]=(c.key=="Haruka"?c.description.Replace(">10s<",">8s<"):c.description)+"<br>Skill: RMB / LB / LT.";
   data.startHP=c.startHP;data.portrait=Sprites[c.portrait];
   // Android leaves CharacterData.icon null for these forms. Windows requires
   // this HUD sprite. Stock Windows characters use their idle body frame,
   // not the large selection portrait (also used by native intro silhouettes).
   data.icon=Sprites[c.icon??c.clips.First(x=>x.name.EndsWith("Idle")).frames[0].sprite];data.passivePrefab=null;
   var pool=ScriptableObject.CreateInstance<PowerupPoolProfile>();pool.name="Revamped_"+c.key+"Powerups";pool.powerupPool=new List<Powerup>();
   foreach(var u in c.powers)
   {
    var power=ScriptableObject.CreateInstance<Powerup>();power.name="Revamped_"+u.key;power.icon=Sprites[u.icon];power.prereqs=new List<Powerup>();
    string nk="revamped_power_"+u.key,dk=nk+"_description";Plugin.Text[nk]=u.name;Plugin.Text[dk]=u.description;
    Plugin.SetField(power,"nameStrID",new LocalizedString(nk));Plugin.SetField(power,"desStrID",new LocalizedString(dk));
    Plugin.SetField(power,"statChanges",u.stats.Select(s=>new StatChange{type=(StatType)Enum.Parse(typeof(StatType),s.type),flatValue=s.flatValue,isFlatMod=s.isFlatMod!=0,value=s.value}).ToArray());
    var effect=new PerkEffect();Plugin.SetField(effect,"trigger",new InstantTrigger());Plugin.SetField(effect,"action",new CharacterUpgradeAction{Key=u.key});
    Plugin.SetField(power,"effects",new[]{effect});Plugin.SetField(power,"stackedEffects",new PerkEffect[0]);Object.DontDestroyOnLoad(power);pool.powerupPool.Add(power);Powers.Add(u.key,power);
   }
   data.exclusivePowerups=pool;Object.DontDestroyOnLoad(pool);Object.DontDestroyOnLoad(data);Data.Add(c.key,data);
  }
 }
 public static void Append(TitleScreenController title)
 {
  var items=Items(title.characterMenu);if(items.Any(x=>TryContract(x.data,out _)))return;
  var shana=items.Single(x=>x.data.name=="Shana").data;var hina=items.Single(x=>x.data.name=="Hina").data;Prepare(shana,hina);
  var template=items.First(x=>x.GetComponent<Unlockable>()!=null);var entries=NativeSelection.GunEntriesFor(title.characterMenu);
  int templateIndex=items.IndexOf(template);var badges=NativeSelection.VictoryObjects(title.characterVictories);
  if(badges.Length!=items.Count)throw new InvalidOperationException("Unsupported native character victory layout");
  foreach(string key in new[]{"Haruka","Dimond_Portrait"})
  {
   var c=Contracts[key];
   int slot=items.Count;var copy=Object.Instantiate(template.gameObject,template.transform.parent);copy.name="RevampedCharacter_"+c.key;
   var ui=copy.GetComponent<DataUI<CharacterData>>();Plugin.SetField(ui,"defaultData",Data[c.key]);ui.Set(Data[c.key]);
   var image=copy.GetComponentsInChildren<Image>(true).Single(x=>x.name=="LoadoutItem");image.GetComponent<Animator>().enabled=false;image.sprite=Sprites[c.clips.First(x=>x.name.EndsWith("Idle")).frames[0].sprite];image.preserveAspect=true;
   var last=items[slot-1].GetComponent<RectTransform>();var prior=items[slot-2].GetComponent<RectTransform>();if(copy.transform.parent.GetComponent<LayoutGroup>()==null)copy.GetComponent<RectTransform>().anchoredPosition=last.anchoredPosition+(last.anchoredPosition-prior.anchoredPosition);
   items.Add(ui);entries.Add(copy.GetComponent<ButtonExtension>());copy.transform.SetAsLastSibling();Slots[c.key]=slot;
   var unlocks=NativeSelection.Unlocks(title.characterUnlocker);UnlockSlots[c.key]=unlocks.Length;NativeSelection.Unlocks(title.characterUnlocker)=unlocks.Concat(new[]{copy.GetComponent<Unlockable>()}).ToArray();
   var badge=copy.transform.Find(NativeSelection.Relative(template.transform,badges[templateIndex].transform)).gameObject;
   NativeSelection.VictoryObjects(title.characterVictories)=NativeSelection.VictoryObjects(title.characterVictories).Concat(new[]{badge}).ToArray();
   var labels=NativeSelection.VictoryLabels(title.characterVictories);if(labels!=null)NativeSelection.VictoryLabels(title.characterVictories)=labels.Concat(new[]{badge.GetComponentInChildren<TMPro.TMP_Text>(true)}).ToArray();
  }
  Count=items.Count;Plugin.Trace("characters-appended",new{count=Count,added=string.Join(",",Contracts.Keys)});
 }
 public static void Extend(SaveData data)
 {
  if(Count==0)return;int unlockCount=UnlockSlots.Values.Max()+1;
  if(data.characterUnlocks==null)data.characterUnlocks=new UnlockData(unlockCount);
  if(data.characterUnlocks.unlocks==null)data.characterUnlocks.unlocks=new bool[unlockCount];
  if(data.characterUnlocks.unlocks.Length<unlockCount)Array.Resize(ref data.characterUnlocks.unlocks,unlockCount);
  int previous=data.characterHighestClear?.Length??0;if(previous<Count){Array.Resize(ref data.characterHighestClear,Count);for(int i=previous;i<Count;i++)data.characterHighestClear[i]=-1;}
 }
 public static Transform PoseChain(Transform parent,Pose[] poses)
 {foreach(var pose in poses){var t=new GameObject(pose.name).transform;t.SetParent(parent,false);t.localPosition=pose.position;t.localScale=pose.scale;t.localRotation=pose.rotation;parent=t;}return parent;}
}
[Serializable]public class CharacterUpgradeAction:flanne.PerkSystem.Action
{
 public string Key;
 public override void Activate(GameObject target)=>target.GetComponentInChildren<PortedCharacterSkill>().Upgrade(Key);
}
[HarmonyPatch(typeof(LoadoutInitializer),"Start")]
class InitializePortedCharacter
{
 static void Postfix()
 {
  var player=PlayerController.Instance;if(!Characters.TryContract(player.loadedCharacter,out var c))return;
  player.movementSpeed=c.moveSpeed;
  var obj=new GameObject("RevampedSkill_"+c.key);obj.SetActive(false);obj.transform.SetParent(player.transform,false);
  var skill=obj.AddComponent<PortedCharacterSkill>();skill.Contract=c;Plugin.SetField(skill,"inputs",Characters.Inputs);Plugin.SetField(skill,"cooldown",c.key=="Haruka"?c.skill.cd:c.skill.cooldown);
  var animation=player.gameObject.AddComponent<CharacterFrames>();animation.Player=player;animation.Skill=skill;animation.Contract=c;
  obj.SetActive(true);
 }
}
[HarmonyPatch(typeof(PowerupGenerator),nameof(PowerupGenerator.SetCharacterPowerupPool))]
class RecoveredExclusiveRepeatPolicy
{
 static void Postfix(PowerupGenerator __instance,PowerupPoolProfile characterExclusiveProfile)
 {
  if(!Characters.Data.Values.Any(x=>x.exclusivePowerups==characterExclusiveProfile))return;
  var pool=(List<PowerupPoolItem>)AccessTools.Field(typeof(PowerupGenerator),"characterPool").GetValue(__instance);
  foreach(var entry in pool)entry.numTimeRepeatable=1; // source isRepeatable=0
 }
}
[HarmonyPatch(typeof(flanne.Player.ShootingState),"Enter")]
class CharacterWalkSpeed
{
 static void Postfix(flanne.Player.ShootingState __instance)
 {var p=(PlayerController)AccessTools.Field(typeof(PlayerState),"owner").GetValue(__instance);float factor=Characters.TryContract(p.loadedCharacter,out var c)?c.shootMoveSpeedMultiplier:p.loadedCharacter.name=="Shana"?.45f:.35f;p.moveSpeedMultiplier=Mathf.Min(1,p.stats[StatType.WalkSpeed].Modify(factor));}
}
// Native animator still owns state, facing and tint. Its sprite track is bound
// to recovered frames after animation evaluation, at the source clip times.
public class CharacterFrames:MonoBehaviour
{
 public PlayerController Player;public PortedCharacterSkill Skill;public Characters.Contract Contract;
 string state;float elapsed;
 void LateUpdate()
 {
  if(Player==null || Player.playerHealth.hp==0 || PauseController.isPaused)return;
  string next=Skill.ChargeMoving?"IdleSpecial":Player.moveVec==Vector3.zero||Player.disableMove.value?"Idle":Player.moveSpeedMultiplier>=1?"Run":"Walk";
  if(next!=state){state=next;elapsed=0;}else elapsed+=Time.deltaTime;
  var clip=Contract.clips.Single(x=>x.name.EndsWith(state));float time=clip.loop?elapsed%clip.duration:Mathf.Min(elapsed,clip.duration);
  Player.playerSprite.sprite=Characters.Sprites[clip.frames.Last(x=>x.time<=time).sprite];
 }
}
public class PortedCharacterSkill:SkillPassive
{
 public Characters.Contract Contract;
 public bool Ready,Charging,ChargeMoving,NextShotBuffed;
 public float EffectRemaining,ReducedCooldown;
 public int ChargeDamage;
 public bool Hurricane,Neural,Ultimate,Perpetual,Steel;
 public float BaseRange=1.960000038f;
 public static readonly AccessTools.FieldRef<SkillPassive,float> Timer=AccessTools.FieldRefAccess<SkillPassive,float>("_timer");
 static readonly AccessTools.FieldRef<SkillPassive,float> Cooldown=AccessTools.FieldRefAccess<SkillPassive,float>("cooldown");
 static readonly AccessTools.FieldRef<PlayerController,Vector3> Move=AccessTools.FieldRefAccess<PlayerController,Vector3>("<moveVec>k__BackingField");
 PlayerController player;CircleCollider2D circle;SpriteTrail trail;
 readonly List<Health> contacts=new List<Health>();readonly HashSet<int> charged=new HashSet<int>();
 readonly List<GameObject> effects=new List<GameObject>();SpriteFX galeFX;GameObject fog;
 float tick,oldMoveSpeed;Vector3 oldMove;bool ownsAction,ownsAnimation,ownsInvincible,ownsTrail;
 static float Multiplier(StatMod stat)=>stat.Modify(1-stat.FlatBonus);
 public float RealCooldown=>Contract.skill.cd-(Neural?Contract.skill.reduceCd*Mathf.Min(Multiplier(player.stats[StatType.ReloadRate])-1,1):0);
 public float RealDuration=>Contract.skill.duration+(Hurricane?Contract.skill.EffectDuration*Mathf.Min(Multiplier(player.stats[StatType.MoveSpeed])-1,1):0);
 public float RealRange=>BaseRange+(Ultimate?Contract.skill.EffectRange*Mathf.Min(Multiplier(player.stats[StatType.FireRate])-1,1):0);
 protected override void Init()
 {
  player=GetComponentInParent<PlayerController>();gameObject.layer=TagLayerUtil.PlayerProjectile;
  var body=gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.useFullKinematicContacts=true;body.gravityScale=0;
  circle=gameObject.AddComponent<CircleCollider2D>();circle.radius=Contract.colliderRadius;circle.isTrigger=false;circle.enabled=Contract.key=="Haruka";
  foreach(var effect in Contract.effects)
  {
   var root=new GameObject(effect.field);root.SetActive(false);root.transform.SetParent(transform,false);var t=Characters.PoseChain(root.transform,effect.transforms);
   var renderer=t.gameObject.AddComponent<SpriteRenderer>();renderer.sharedMaterial=player.playerSprite.sharedMaterial;renderer.sortingLayerID=player.playerSprite.sortingLayerID;renderer.sortingOrder=player.playerSprite.sortingOrder+1;renderer.color=Color.white;
   var fx=t.gameObject.AddComponent<SpriteFX>();Plugin.SetField(fx,"spriteRenderer",renderer);Plugin.SetField(fx,"secPerFrame",effect.secondsPerFrame);Plugin.SetField(fx,"loop",effect.loop);Plugin.SetField(fx,"sprites",effect.frames.Select(x=>Characters.Sprites[x]).ToArray());effects.Add(root);if(effect.field=="spriteEffect")galeFX=fx;
  }
  if(Contract.key=="Haruka")
  {
   var game=Object.FindObjectOfType<GameController>();var native=game.playerFogRevealer.GetComponentInChildren<SpriteRenderer>(true);
   fog=new GameObject("TempestSlashFog");fog.SetActive(false);fog.transform.SetParent(transform,false);var t=Characters.PoseChain(fog.transform,Contract.fog.transforms);t.gameObject.layer=native.gameObject.layer;
   var renderer=t.gameObject.AddComponent<SpriteRenderer>();renderer.sharedMaterial=native.sharedMaterial;renderer.sortingLayerID=native.sortingLayerID;renderer.sortingOrder=native.sortingOrder;renderer.sprite=Characters.Sprites[Contract.fog.sprite];renderer.color=Color.red;
  }
  else
  {
   var go=Object.Instantiate(Characters.TrailTemplate.gameObject,transform);trail=go.GetComponent<SpriteTrail>();trail.mLeadingSprite=player.playerSprite;
  }
  Ready=true;Plugin.Trace("character-ready",new{character=Contract.key,hp=player.playerHealth.maxHP,speed=player.movementSpeed});
 }
 public void Upgrade(string key)
 {
  switch(key)
  {
   case "HurricaneDancePowerup":Hurricane=true;break;
   case "NeuralResponsePowerup":Neural=true;break;
   case "UltimatePowerup":Ultimate=true;BaseRange=3;Plugin.SetField(galeFX,"sprites",Contract.effects.Single().alternateFrames.Select(x=>Characters.Sprites[x]).ToArray());break;
   case "BarrelReinforcementPowerup":break;
   case "PerpetualFighterPowerup":Perpetual=true;break;
   case "SteelAccelerationPowerup":Steel=true;break;
   default:throw new InvalidOperationException("Unknown recovered exclusive: "+key);
  }
  Plugin.Trace("character-upgrade",key);
 }
 protected override void PerformSkill()
 {
  if(player.playerHealth.hp==0)return;
  if(Contract.key=="Haruka")
  {
   // Windows increments the timer before calling this method; Android does it
   // afterward. Adjust the same native timer so Reload Rate affects this use.
   float cd=RealCooldown;Timer(this)+=cd-Cooldown(this);Cooldown(this)=cd;
   EffectRemaining=RealDuration;circle.radius=RealRange;fog.transform.localScale=new Vector3(RealRange,RealRange,1);
   effects[0].transform.localScale=Ultimate?new Vector3(RealRange/BaseRange,RealRange/BaseRange,1):Vector3.one;
   foreach(var effect in effects)effect.SetActive(true);fog.SetActive(true);Plugin.Trace("tempest-slash",new{cooldown=cd,duration=EffectRemaining,range=circle.radius});
  }
  else if(!Charging)StartCoroutine(Charge());
 }
 IEnumerator Charge()
 {
  Charging=true;ChargeMoving=true;ChargeDamage=Mathf.FloorToInt(player.gun.damage*(Contract.skill.chargeDamageMultiplier+(Steel?1:0)));charged.Clear();ReducedCooldown=0;circle.enabled=true;
  oldMove=player.moveVec;oldMoveSpeed=player.moveSpeedMultiplier;
  ownsInvincible=true;player.playerHealth.isInvincible.Flip();ownsAction=true;player.disableAction.Flip();ownsAnimation=true;player.disableAnimation.Flip();
  foreach(string trigger in new[]{"Idle","Run","Walk"})player.playerAnimator.ResetTrigger(trigger);player.playerAnimator.SetTrigger("Special");
  trail.SetEnabled(true);ownsTrail=true;
  effects[0].transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(player.moveVec.y,player.moveVec.x)*Mathf.Rad2Deg);foreach(var effect in effects)effect.SetActive(true);
  if(player.moveVec==Vector3.zero)Move(player)=player.playerSprite.flipX?Vector3.left:Vector3.right;
  player.moveSpeedMultiplier=oldMoveSpeed*(Contract.skill.chargeSpeedMulti+(Steel?1:0));
  Plugin.Trace("bulwark-charge",new{damage=ChargeDamage,speedMultiplier=player.moveSpeedMultiplier,duration=Contract.skill.chargeDuration});
  yield return new WaitForSeconds(Contract.skill.chargeDuration);
  RestoreMovement();
  // ARM64 literal at 0xa159ac is 0.8f; it is not stock Hina's 0.3f.
  yield return new WaitForSeconds(.8f);
  if(ownsInvincible){player.playerHealth.isInvincible.UnFlip();ownsInvincible=false;}
  Charging=false;
  if(!NextShotBuffed){NextShotBuffed=true;this.AddObserver(OnNextShot,Gun.ShootEvent,player.gun);}
 }
 void RestoreMovement()
 {
  if(!ChargeMoving)return;ChargeMoving=false;Move(player)=oldMove;player.moveSpeedMultiplier=oldMoveSpeed;
  if(ownsTrail){trail.SetEnabled(false);ownsTrail=false;}foreach(var effect in effects)effect.SetActive(false);circle.enabled=false;
  if(ownsAnimation){player.disableAnimation.UnFlip();ownsAnimation=false;}if(ownsAction){player.disableAction.UnFlip();ownsAction=false;}
 }
 void OnNextShot(object sender,object args)
 {
  if(!(args is ProjectileRecipe recipe))return;recipe.damage*=1+Contract.skill.nextShotDamageMultiplier;
  NextShotBuffed=false;this.RemoveObserver(OnNextShot,Gun.ShootEvent,player.gun);Plugin.Trace("bulwark-shot",new{damage=recipe.damage});
 }
 void OnCollisionEnter2D(Collision2D collision)
 {
  var obj=collision.gameObject;if(obj.layer!=(int)TagLayerUtil.Enemy)return;var health=obj.GetComponent<Health>();if(health==null)return;
  if(Contract.key=="Haruka")contacts.Add(health);
  else if(Charging && ChargeMoving && charged.Add(obj.GetInstanceID()))
  {
   health.HPChange(-ChargeDamage);var movement=obj.GetComponent<MoveComponent2D>();if(movement!=null&&!movement.knockbackImmune)movement.vector=(obj.transform.position-player.transform.position).normalized*Contract.skill.baseKnockback;
   if(Perpetual){float reduce=Mathf.Min(1,3-ReducedCooldown);Timer(this)-=reduce;ReducedCooldown+=reduce;}
   Plugin.Trace("bulwark-hit",new{target=obj.GetInstanceID(),damage=ChargeDamage,reduced=ReducedCooldown});
  }
 }
 void OnCollisionExit2D(Collision2D collision){if(Contract.key=="Haruka")contacts.Remove(collision.gameObject.GetComponent<Health>());}
 void FixedUpdate()
 {
  if(!Ready||Contract.key!="Haruka"||EffectRemaining<=0)return;
  EffectRemaining-=Time.fixedDeltaTime;
  if(EffectRemaining<0){foreach(var effect in effects)effect.SetActive(false);fog.SetActive(false);EffectRemaining=0;return;}
  tick+=Time.deltaTime;if(tick<Contract.skill.secondsPerTick)return;tick-=Contract.skill.secondsPerTick;
  int damage=Mathf.CeilToInt(player.stats[StatType.BulletDamage].Modify(Contract.skill.damageAmount));
  for(int i=contacts.Count-1;i>=0;i--)
  {
   var target=contacts[i];if(target==null){contacts.RemoveAt(i);continue;}target.HPChange(-damage);
   if(Contract.skill.triggerOnHit)player.gameObject.PostNotification(Projectile.ImpactEvent,target.gameObject);
   Plugin.Trace("tempest-hit",new{target=target.gameObject.GetInstanceID(),damage});
  }
 }
 void OnDisable()
 {
  if(!Ready)return;StopAllCoroutines();RestoreMovement();
  if(ownsInvincible){player.playerHealth.isInvincible.UnFlip();ownsInvincible=false;}
  if(NextShotBuffed){this.RemoveObserver(OnNextShot,Gun.ShootEvent,player.gun);NextShotBuffed=false;}
  if(fog!=null)fog.SetActive(false);foreach(var effect in effects)effect.SetActive(false);EffectRemaining=0;Charging=false;contacts.Clear();
 }
}
