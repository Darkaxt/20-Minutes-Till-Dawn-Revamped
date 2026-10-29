using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using HarmonyLib;
using TMPro;
using UnityEngine;
using flanne;
using flanne.Core;
using flanne.Pickups;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// Credits keeps its serialized ordinal for old checkpoints; restored drops
// convert to Experience before their world object and marker are created.
public enum ExplorationReward{Experience,Credits,Healing,Magnet,Freeze,Frenzy,Upgrade,CharacterUpgrade}
[Serializable]public sealed class ExplorationDrop
{public int id;public ExplorationReward reward;public float x,y;public string power;}
[Serializable]public sealed class ExplorationState
{public uint random;public int nextId,credits;public float remaining,frenzy;public List<ExplorationDrop> drops=new List<ExplorationDrop>();}

// One battle-scene owner for the requested exploration rules. Permanent perks,
// XP, status effects and credit payout keep their existing native owners.
public sealed class ExplorationRun:MonoBehaviour
{
 public const int Maximum=3;
 public static readonly Color Mint=new Color(.64f,.88f,.76f);
 public static ExplorationRun Instance;
 public ExplorationState State;
 public readonly List<ExplorationChest> Chests=new List<ExplorationChest>();
 public ExplorationDrop PendingReward;
 GameController game;bool frenzyApplied;TMP_Text notice;float noticeRemaining;
 Sprite xpMarker;Texture2D xpMarkerTexture;
 static readonly StatType[] FrenzyStats={StatType.BulletDamage,StatType.SummonDamage,StatType.FireRate};
 public static ExplorationReward RewardForRoll(int roll)
 {
  if(roll<0||roll>=100)throw new ArgumentOutOfRangeException(nameof(roll));
  return roll<45?ExplorationReward.Experience:roll<60?ExplorationReward.Healing:roll<72?ExplorationReward.Magnet:roll<82?ExplorationReward.Freeze:roll<90?ExplorationReward.Frenzy:roll<98?ExplorationReward.Upgrade:ExplorationReward.CharacterUpgrade;
 }
 void Awake(){Instance=this;game=GetComponent<GameController>();}
 internal static void Validate(ExplorationState state)
 {
  if(state==null)return;
  if(state.random==0||state.nextId<0||state.credits<0||state.drops==null||state.drops.Count>Maximum||state.remaining<0||state.remaining>90||state.frenzy<0||state.frenzy>15||
   new[]{state.remaining,state.frenzy}.Any(v=>float.IsNaN(v)||float.IsInfinity(v))||state.drops.Any(d=>d==null||d.id<0||d.id>=state.nextId||!Enum.IsDefined(typeof(ExplorationReward),d.reward)||new[]{d.x,d.y}.Any(v=>float.IsNaN(v)||float.IsInfinity(v))||((d.reward==ExplorationReward.Upgrade||d.reward==ExplorationReward.CharacterUpgrade)&&string.IsNullOrEmpty(d.power)))||state.drops.Select(d=>d.id).Distinct().Count()!=state.drops.Count)
   throw new InvalidOperationException("The checkpoint's exploration rewards are invalid; it has been preserved.");
 }
 uint Next(){uint x=State.random;x^=x<<13;x^=x>>17;x^=x<<5;State.random=x;return x;}
 int Next(int limit)=>(int)(Next()%(uint)limit);
 float Interval()=>60+Next(30001)/1000f;
 internal void Restore(ExplorationState saved)
 {
  Validate(saved);
  State=saved??new ExplorationState{random=((uint)DateTime.UtcNow.Ticks)|1};
  if(saved==null)State.remaining=Interval();
  foreach(var drop in State.drops)Create(drop);
  SetFrenzy(State.frenzy>0);
 }
 internal ExplorationState Capture()=>new ExplorationState{random=State.random,nextId=State.nextId,credits=State.credits,remaining=State.remaining,frenzy=State.frenzy,drops=State.drops.Select(d=>new ExplorationDrop{id=d.id,reward=d.reward,x=d.x,y=d.y,power=d.power}).ToList()};
 void Update()
 {
  if(State==null){if(RunResume.Restoring||PlayerController.Instance?.loadedCharacter==null||ObjectPooler.SharedInstance==null||!(game.CurrentState is CombatState))return;Restore(null);}
  if(notice!=null)notice.gameObject.SetActive(noticeRemaining>0&&game.CurrentState is CombatState);
  if(noticeRemaining>0&&game.CurrentState is CombatState)noticeRemaining=Mathf.Max(0,noticeRemaining-Time.deltaTime);
  if(!(game.CurrentState is CombatState)||PauseController.isPaused)return;
  if(State.frenzy>0){State.frenzy=Mathf.Max(0,State.frenzy-Time.deltaTime);if(State.frenzy==0)SetFrenzy(false);}
  State.remaining=Mathf.Max(0,State.remaining-Time.deltaTime);
  if(State.remaining==0){SpawnWave();State.remaining=Interval();RunResume.Save();}
 }
 internal List<Powerup> Eligible(bool character)
 {
  var generator=game.powerupGenerator;var pool=AndroidPowerupCounts.Pool(generator,character?"characterPool":"powerupPool");
  var method=AccessTools.Method(typeof(PowerupGenerator),"PrereqsMet");
  var choices=pool.Where(p=>p.numTimeRepeatable!=0&&(bool)method.Invoke(generator,new object[]{p.powerup})).Select(p=>p.powerup).Distinct().OrderBy(RunResume.Key).ToList();
  if(!character&&choices.Any(p=>!FallbackPowerups.IsFallback(p)))choices.RemoveAll(FallbackPowerups.IsFallback);
  return choices;
 }
 internal void SpawnWave()
 {
  int available=Maximum-State.drops.Count;if(available==0)return;
  var first=RewardForRoll(Next(100));bool common=first<=ExplorationReward.Magnet;int count=common?1+Next(available):1;
  float start=Next(36000)*.01f;bool empty=State.drops.Count==0;
  for(int i=0;i<count;i++)
  {
   float angle=empty?start+360f*i/count:OpenDirection();
   if(!TravelPosition(angle,5+Next(25001)/1000f,out var position))continue;
   var reward=i==0?first:RewardForRoll(Next(72));
   var drop=new ExplorationDrop{id=State.nextId++,reward=reward,x=position.x,y=position.y};
   if(reward==ExplorationReward.Upgrade||reward==ExplorationReward.CharacterUpgrade)
   {var choices=Eligible(reward==ExplorationReward.CharacterUpgrade);if(choices.Count==0)drop.reward=ExplorationReward.Experience;else drop.power=RunResume.Key(choices[Next(choices.Count)]);}
   State.drops.Add(drop);Create(drop);Plugin.Trace("exploration-spawn",new{drop.id,drop.reward,drop.x,drop.y,drop.power});
  }
 }
 float OpenDirection()
 {
  var player=(Vector2)PlayerController.Instance.transform.position;
  var angles=State.drops.Select(d=>{var v=new Vector2(d.x,d.y)-player;return Mathf.Repeat(Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg,360);}).OrderBy(a=>a).ToArray();
  float best=-1,direction=0;for(int i=0;i<angles.Length;i++){float gap=(i+1==angles.Length?angles[0]+360:angles[i+1])-angles[i];if(gap>best){best=gap;direction=angles[i]+gap*.5f;}}
  return direction;
 }
 static bool Blocks(Collider2D c)=>c.CompareTag("MapBounds")||!c.isTrigger&&c.GetComponentInParent<AIComponent>()==null&&c.GetComponentInParent<PlayerController>()==null;
 internal bool FindPosition(float angle,out Vector2 position)
  =>TravelPosition(angle,25,out position);
 bool TravelPosition(float angle,float seconds,out Vector2 position)
 {
  var p=PlayerController.Instance;var camera=Camera.main;var origin=(Vector2)p.transform.position;
  // Budget travel at the player's current walking speed. Enclosed arenas cap
  // the journey at their nearest wall, rather than spawning through it.
  float farthest=0;position=default;
  foreach(float turn in new[]{0f,-5f,5f,-10f,10f})
  {
   float radians=(angle+turn)*Mathf.Deg2Rad;var direction=new Vector2(Mathf.Cos(radians),Mathf.Sin(radians));
   float edge=Mathf.Min(direction.x==0?float.PositiveInfinity:camera.orthographicSize*camera.aspect/Mathf.Abs(direction.x),direction.y==0?float.PositiveInfinity:camera.orthographicSize/Mathf.Abs(direction.y));
   float distance=Mathf.Max(edge*1.15f,p.finalMoveSpeed*seconds);
   foreach(var hit in Physics2D.RaycastAll(origin,direction,distance+.75f).Where(h=>Blocks(h.collider)))distance=Mathf.Min(distance,hit.distance-.75f);
   if(distance<=farthest)continue;
   var point=origin+direction*distance;
   var screen=camera.WorldToViewportPoint(point);if(screen.x>=0&&screen.x<=1&&screen.y>=0&&screen.y<=1)continue;
   if(State.drops.Any(d=>Vector2.Angle(point-origin,new Vector2(d.x,d.y)-origin)<89.99f))continue;
   if(Physics2D.LinecastAll(origin,point).Any(h=>Blocks(h.collider))||Physics2D.OverlapCircleAll(point,.5f).Any(Blocks)||State.drops.Any(d=>Vector2.Distance(point,new Vector2(d.x,d.y))<1))continue;
   position=point;farthest=distance;
  }
  return farthest>0;
 }
 internal ExplorationChest Create(ExplorationDrop drop)
 {
  if(drop.reward==ExplorationReward.Credits){drop.reward=ExplorationReward.Experience;drop.power=null;}
  var template=Resources.FindObjectsOfTypeAll<ChestPickup>().First(c=>!c.gameObject.scene.IsValid());
  var go=new GameObject("ExplorationChest_"+drop.id,typeof(SpriteRenderer),typeof(BoxCollider2D));go.transform.SetParent(transform,false);go.transform.position=new Vector3(drop.x,drop.y,0);go.layer=template.gameObject.layer;
  var renderer=go.GetComponent<SpriteRenderer>();var original=template.GetComponentInChildren<SpriteRenderer>();renderer.sprite=(Sprite)AccessTools.Field(typeof(ChestPickup),"chestClosed").GetValue(template);renderer.color=Mint;renderer.sortingLayerID=original.sortingLayerID;renderer.sortingOrder=original.sortingOrder;
  var collider=go.GetComponent<BoxCollider2D>();collider.isTrigger=true;collider.size=(Vector2)renderer.sprite.bounds.size*.8f;
  var chest=go.AddComponent<ExplorationChest>();chest.Owner=this;chest.Drop=drop;Chests.Add(chest);
  var marker=go.AddComponent<ChestDirectionIndicator>();marker.Exploration=true;
  ExplorationChestVisual.Attach(chest,template);
  return chest;
 }
 public Sprite ContentIcon(ExplorationDrop drop)
  =>ContentIcon(drop,ShownReward(drop));
 internal ExplorationReward ShownReward(ExplorationDrop drop)
 {
  if(drop.reward==ExplorationReward.Credits)return ExplorationReward.Experience;
  var health=PlayerController.Instance.playerHealth;
  if(drop.reward==ExplorationReward.Healing&&health.hp>=health.maxHP)return ExplorationReward.Experience;
  if((drop.reward==ExplorationReward.Upgrade||drop.reward==ExplorationReward.CharacterUpgrade)&&!Eligible(drop.reward==ExplorationReward.CharacterUpgrade).Any(p=>RunResume.Key(p)==drop.power))return ExplorationReward.Experience;
  return drop.reward;
 }
 static SpriteRenderer PickupArt(ExplorationReward reward)
 =>(reward==ExplorationReward.Healing?Resources.FindObjectsOfTypeAll<HPPickup>().Single(p=>!p.gameObject.scene.IsValid()&&p.name=="PF_HeartPickup").transform:ObjectPooler.SharedInstance.pooledObjectsDictionary["SmallXP"][0].transform).Find("Bounce").GetComponent<SpriteRenderer>();
 internal static Color ContentTint(ExplorationReward shown)
 =>shown==ExplorationReward.Experience||shown==ExplorationReward.Healing?PickupArt(shown).color:Color.white;
 internal Sprite ContentIcon(ExplorationDrop drop,ExplorationReward shown)
 {
  if(shown==ExplorationReward.Experience)return ExperienceMarker();
  if(shown==ExplorationReward.Healing)return PickupArt(shown).sprite;
  var powers=Resources.FindObjectsOfTypeAll<Powerup>();
  if(shown==ExplorationReward.Upgrade||shown==ExplorationReward.CharacterUpgrade)
  return powers.First(p=>RunResume.Key(p)==drop.power).icon;
  string key=shown==ExplorationReward.Magnet?"Magnetism":shown==ExplorationReward.Freeze?"FrostMage":"RapidFire";
  return powers.First(p=>p.name==key).icon;
 }
 Sprite ExperienceMarker()
 {
  if(xpMarker!=null)return xpMarker;
  // Three pixel-rounded XP orbs, tinted by the native pickup palette. This
  // owned HUD symbol never edits the actual pickup sprite/atlas or its value.
  const int width=20,height=18;var pixels=new Color[width*height];
  foreach(var center in new[]{new Vector2(9.5f,12.5f),new Vector2(5,5),new Vector2(14,5)})
   for(int y=0;y<height;y++)for(int x=0;x<width;x++)
   {
    var offset=new Vector2(x,y)-center;float distance=offset.sqrMagnitude;if(distance>18)continue;
    float shade=distance>10?.42f:offset.x<0&&offset.y>0?1f:.78f;pixels[y*width+x]=new Color(shade,shade,shade,1);
   }
  xpMarkerTexture=new Texture2D(width,height,TextureFormat.RGBA32,false){name="ExplorationXPOrbs",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
  xpMarkerTexture.SetPixels(pixels);xpMarkerTexture.Apply(false,true);
  xpMarker=Sprite.Create(xpMarkerTexture,new Rect(0,0,width,height),new Vector2(.5f,.5f),16);xpMarker.name="ExplorationXPOrbs";return xpMarker;
 }
 void Magnet()
 {
  var player=PlayerController.Instance;float previous=-1f/60;
  var pickups=Object.FindObjectsOfType<XPPickup>().Select(x=>new{Pickup=x.GetComponent<AndroidPickup>(),Distance=Vector2.Distance(x.transform.position,player.transform.position)}).OrderBy(x=>x.Distance);
  foreach(var orb in pickups)
  {
   float delay=Mathf.Max(orb.Distance/12,previous+1f/60);
   if(orb.Pickup.MagnetPick(player.gameObject,delay))previous=delay;
  }
 }
 internal void Collect(ExplorationChest chest)
 {
  if(chest.Claimed||!(game.CurrentState is CombatState)||RunResume.Restoring)return;
  chest.Claimed=true;var drop=chest.Drop;State.drops.Remove(drop);Chests.Remove(chest);chest.gameObject.SetActive(false);Object.Destroy(chest.gameObject);
  var player=PlayerController.Instance;
  switch(drop.reward)
  {
   case ExplorationReward.Experience:
   case ExplorationReward.Credits:Experience();break;
   case ExplorationReward.Healing:if(player.playerHealth.hp<player.playerHealth.maxHP){player.playerHealth.Heal(1);Notify("Healed 1 heart");}else Experience();break;
   case ExplorationReward.Magnet:Magnet();Notify("XP magnet");break;
   case ExplorationReward.Freeze:foreach(var ai in Object.FindObjectsOfType<AIComponent>().Where(a=>a.gameObject.layer==(int)TagLayerUtil.Enemy))AndroidFreezeApplication.Apply(FreezeSystem.SharedInstance,ai.gameObject,3);Notify("Freeze");break;
   case ExplorationReward.Frenzy:State.frenzy=15;SetFrenzy(true);Notify("Frenzy +25% damage / fire rate · 15s");break;
   default:
    if(Eligible(drop.reward==ExplorationReward.CharacterUpgrade).Any(p=>RunResume.Key(p)==drop.power)){PendingReward=drop;game.ChangeState<ExplorationRewardState>();}
    else Experience();
    break;
  }
  StatsOverlay.Instance?.Dirty();RunResume.Save();Plugin.Trace("exploration-collected",new{drop.id,drop.reward,State.credits,State.frenzy});
 }
 void Experience()
 {
  var xp=game.playerXP;float amount=.3f*(float)AccessTools.Property(xp.GetType(),"xpToLevel").GetValue(xp);
  Notify("+"+xp.xpMultiplier.Modify(amount).ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+" XP");xp.GainXP(amount);
 }
 void SetFrenzy(bool enabled)
 {
  if(frenzyApplied==enabled)return;frenzyApplied=enabled;
  foreach(var stat in FrenzyStats)PlayerController.Instance.stats[stat].AddMultiplierBonus(enabled?.25f:-.25f);
  StatsOverlay.Instance?.Dirty();
 }
 internal void Notify(string text)
 {
  if(notice==null){var hud=StatsOverlay.Instance;notice=StatsOverlay.Text(hud.Panel.transform,"ExplorationRewardNotice",hud.Font,"",16,Vector2.zero,new Vector2(600,28));StatsOverlay.Rect(notice.gameObject,hud.Panel.transform,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,75),new Vector2(600,28));notice.alignment=TextAlignmentOptions.Center;notice.color=Mint;}
  notice.text=text;noticeRemaining=3;
 }
 void OnDestroy(){if(notice!=null)Object.Destroy(notice.gameObject);if(xpMarker!=null)Destroy(xpMarker);if(xpMarkerTexture!=null)Destroy(xpMarkerTexture);if(Instance==this)Instance=null;}
}
public sealed class ExplorationChest:MonoBehaviour
{
 public ExplorationRun Owner;public ExplorationDrop Drop;public bool Claimed;
 internal ExplorationReward ShownReward{get;private set;}bool presentationSet;
 internal ExplorationReward RefreshPresentation()
 {
  var shown=Owner.ShownReward(Drop);
  if(presentationSet&&shown==ShownReward)return shown;
  ShownReward=shown;presentationSet=true;
  var marker=GetComponent<ChestDirectionIndicator>();marker.Icon=Owner.ContentIcon(Drop,shown);marker.IconTint=ExplorationRun.ContentTint(shown);
  if(marker.Marker!=null)
  {
   var image=marker.Marker.GetComponent<UnityEngine.UI.Image>();image.sprite=marker.Icon;image.color=marker.IconTint;
   marker.Arrow.GetComponent<TMP_Text>().color=ExplorationChestVisual.RewardColor(shown);
  }
  return shown;
 }
 void OnTriggerEnter2D(Collider2D other){if(other.CompareTag("Player"))Owner.Collect(this);}
}
public sealed class ExplorationRewardState:GameState
{
 Powerup power;ExplorationDrop drop;
 public override void Enter()
 {
  drop=ExplorationRun.Instance.PendingReward;power=ExplorationRun.Instance.Eligible(drop.reward==ExplorationReward.CharacterUpgrade).Single(p=>RunResume.Key(p)==drop.power);
  owner.pauseController.Pause();AudioManager.Instance.SetLowPassFilter(true);owner.chestUIController.SetToPowerup(power);owner.chestUIController.Show();owner.chestUIController.TakeClickEvent+=Take;owner.chestUIController.LeaveClickEvent+=Leave;
  // This reward state owns its initial focus. Native chest presentation can
  // otherwise inherit no selection after entering Custom through the pointer.
  ((UnityEngine.UI.Button)AccessTools.Field(owner.chestUIController.GetType(),"takeButton").GetValue(owner.chestUIController)).Select();
 }
 void Take(object sender,EventArgs args)
 {
  PlayerController.Instance.playerPerks.Equip(power);
  if(drop.reward==ExplorationReward.CharacterUpgrade)owner.powerupGenerator.RemoveFromCharacterPool(power);else owner.powerupGenerator.RemoveFromPool(power);
  ExplorationRun.Instance.Notify(power.nameString);owner.ChangeState<CombatState>();
 }
 void Leave(object sender,EventArgs args)=>owner.ChangeState<CombatState>();
 public override void Exit()
 {
  owner.chestUIController.TakeClickEvent-=Take;owner.chestUIController.LeaveClickEvent-=Leave;owner.chestUIController.Hide();ExplorationRun.Instance.PendingReward=null;AudioManager.Instance.SetLowPassFilter(false);owner.pauseController.UnPause();
 }
}
