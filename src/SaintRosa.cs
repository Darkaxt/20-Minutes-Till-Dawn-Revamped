using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
using flanne.CharacterPassives;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// Reconstructed RoseChamberPassive / RosethornShellProjectile / RoseBurstVisual
// from Android8.7/1105. Native input, ProjectileFactory and source pools remain
// the platform boundaries; every shell in a volley shares one Bloom context.
public sealed class SaintRosaSkill:SkillPassive
{
 internal const string ShellTag="SaintRosa_RosethornShell",PetalTag="SaintRosa_PetalBurst",BloomTag="SaintRosa_BloomShock",SmallBloomTag="SaintRosa_BloomShockSmall";
 public Characters.Contract Contract;
 public int ShotProgress,ShellCount;
 public bool ThornBelt,PiercingPetal,FinalBloom;
 public float BuffRemaining;
 internal Vector2 LastAim=Vector2.up;
 PlayerController player;GameObject templates;SaintRosaChamber chamber;
 internal int Capacity=>Contract.skill.maxShells+(ThornBelt?3:0);
 internal int ShotsPerShell=>Mathf.Max(1,Contract.skill.shotsPerShell-(ThornBelt?1:0));
 internal int BloomThreshold=>FinalBloom?6:8;
 internal float SplashMultiplier=>Contract.skill.splashMultiplier*(PiercingPetal?1.3f:1);
 internal float SplashRadius=>Contract.skill.splashRadius*(PiercingPetal?1.5f:1);
 internal float BloomRadius=>PiercingPetal?4.5f:Contract.skill.bloomRadius;
 internal event Action ChamberChanged;
 protected override void Init()
 {
  player=PlayerController.Instance;PrepareActors();
  this.AddObserver(OnEffectiveShot,AndroidGunRuntime.EffectiveShotEvent);
  chamber=gameObject.AddComponent<SaintRosaChamber>();chamber.Initialize(this);
 }
 internal bool CanPerformSkill()=>player!=null&&player.playerHealth.hp>0&&!player.disableAction.value&&templates!=null&&ProjectileFactory.SharedInstance!=null;
 void OnEffectiveShot(object sender,object args)
 {
  if(sender!=player.gun||player.playerHealth.hp<=0||ShellCount>=Capacity)return;
  ShotProgress++;
  if(ShotProgress>=ShotsPerShell){ShotProgress-=ShotsPerShell;ShellCount++;}
  ChamberChanged?.Invoke();
 }
 internal void Upgrade(string key)
 {
  switch(key)
  {
   case "ThornBelt":
    if(ThornBelt)return;ThornBelt=true;
    while(ShotProgress>=ShotsPerShell&&ShellCount<Capacity){ShotProgress-=ShotsPerShell;ShellCount++;}
    break;
   case "PiercingPetal":if(PiercingPetal)return;PiercingPetal=true;break;
   case "FinalBloom":if(FinalBloom)return;FinalBloom=true;Plugin.SetField(this,"cooldown",6f);break;
   default:throw new InvalidOperationException("Unknown Saint Rosa upgrade: "+key);
  }
  ChamberChanged?.Invoke();
 }
 protected override void PerformSkill()
 {
  int count=ShellCount;Vector2 direction=AndroidGunRuntime.Direction(player.gun);
  if(direction.sqrMagnitude>.0001f)LastAim=direction.normalized;
  var basis=player.gun.GetProjectileRecipe();
  float damage=player.gun.damage*Contract.skill.damageMultiplier*(1+Contract.skill.damagePerExtraShell*(count-1));
  var volley=new RosaVolley{bloomAvailable=count>=BloomThreshold,largeBloom=PiercingPetal,
   bloomDamage=player.gun.damage*Contract.skill.bloomMultiplier,bloomRadius=BloomRadius,
   splashDamage=damage*SplashMultiplier,splashRadius=SplashRadius};
  ShellCount=0;
  for(int i=0;i<count;i++)
  {
   var recipe=new AndroidProjectileRecipe{objectPoolTag=ShellTag,damage=damage,knockback=basis.knockback,size=basis.size,
    projectileSpeed=basis.projectileSpeed,piercing=0,bounce=Mathf.Max(0,basis.bounce)+Contract.skill.shellBounceCount+(ThornBelt?1:0),
    owner=basis.owner,BulletEffect=true,BurnDamage=(basis as AndroidProjectileRecipe)?.BurnDamage??0};
   player.gun.PostNotification(Gun.ShootEvent,recipe);
   recipe.objectPoolTag=ShellTag;recipe.piercing=0;
   var shell=ProjectileFactory.SharedInstance.SpawnProjectile(recipe,LastAim.Rotate((i-(count-1)*.5f)*Contract.skill.shellAngleSpacing),player.gun.transform.position,1,true);
   shell.GetComponent<RosaShell>().Configure(this,volley);
  }
  RefillAmmo();
  if(count>0&&FinalBloom){if(BuffRemaining<=0)player.stats[StatType.FireRate].AddMultiplierBonus(.25f);BuffRemaining=3;}
  ChamberChanged?.Invoke();
 }
 void RefillAmmo()
 {
  if(player.CurrentState is flanne.Player.ReloadState reload){AndroidReloadRuntime.Get(reload).CompleteImmediately(reload,player);return;}
  var shooting=player.CurrentState as flanne.Player.ShootingState;
  if(shooting!=null)AndroidShootingRuntime.Get(shooting,player).CancelPendingReload();
  player.ammo.Reload();player.reloadBar.value=0;AndroidReloadRuntime.Hide(player);
  if(shooting!=null&&player.playerInput.actions["Fire"].ReadValue<float>()==0)player.ChangeState<flanne.Player.IdleState>();
 }
 void LateUpdate()
 {
  if(BuffRemaining<=0||PauseController.isPaused)return;
  BuffRemaining-=Time.deltaTime;
  if(BuffRemaining<=0){BuffRemaining=0;player.stats[StatType.FireRate].AddMultiplierBonus(-.25f);}
 }
 internal void DestroySource()
 {
  this.RemoveObserver(OnEffectiveShot,AndroidGunRuntime.EffectiveShotEvent);
  if(BuffRemaining>0&&player!=null)player.stats[StatType.FireRate].AddMultiplierBonus(-.25f);
  BuffRemaining=0;
 }
 internal void RestoreCounters(RosaRunState saved)
 {
  ShotProgress=saved.progress;ShellCount=saved.shells;LastAim=new Vector2(saved.aimX,saved.aimY);
  PortedCharacterSkill.Timer(this)=saved.cooldown;
  if(BuffRemaining>0)player.stats[StatType.FireRate].AddMultiplierBonus(-.25f);
  BuffRemaining=saved.buffRemaining;
  if(BuffRemaining>0)player.stats[StatType.FireRate].AddMultiplierBonus(.25f);
  ChamberChanged?.Invoke();
 }
 internal void Explosion(Vector3 center,RosaVolley volley,float hitDamage)
 {
  if(player==null||player.playerHealth.hp<=0)return;
  if(volley.bloomAvailable&&!volley.bloomTriggered)
  {
   volley.bloomTriggered=true;Burst(volley.largeBloom?BloomTag:SmallBloomTag,center,volley.bloomRadius);
   Area(center,volley.bloomRadius,volley.bloomDamage,true);
  }
  Burst(PetalTag,center,volley.splashRadius);Area(center,volley.splashRadius,hitDamage*SplashMultiplier,false);
 }
 void Area(Vector3 center,float radius,float damage,bool bloom)
 {
  var pools=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();var ids=new List<int>();var seen=new HashSet<int>();
  pools.GetMonsterInRange(ids,radius,center);
  foreach(int id in ids)
  {
   var actor=pools.GetFromID(id);if(actor==null||!actor.activeInHierarchy)continue;
   var health=actor.GetComponent<Health>();if(health==null||health.isDead||!seen.Add(health.GetInstanceID()))continue;
   if(bloom)
   {
    var move=actor.GetComponent<MoveComponent2D>();
    if(move!=null&&!move.knockbackImmune)
    {
     Vector2 away=actor.transform.position-center;
     if(away.sqrMagnitude<.0001f)away=actor.transform.position-player.transform.position;
     if(away.sqrMagnitude<.0001f)away=Vector2.up;
     move.vector=away.normalized*Contract.skill.bloomKnockback;
    }
   }
   AndroidDamageRounding.ChangeHealth(health,-Mathf.Max(1,Mathf.FloorToInt(damage)),DamageType.Bullet);
  }
 }
 internal static void Burst(string tag,Vector3 center,float radius)
 {
  var actor=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().Get(tag,center);
  actor.GetComponent<RosaBurst>().Play(radius);
 }
 void PrepareActors()
 {
  templates=new GameObject("SaintRosaTemplates");templates.SetActive(false);templates.transform.SetParent(transform,false);
  var pools=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();
  foreach(var contract in SaintRosaAssets.Data.actors.Where(a=>a.field!="pulsePrefab"))
  {
   string tag=contract.field=="shellPrefab"?ShellTag:contract.field=="petalBurstPrefab"?PetalTag:contract.field=="bloomShockPrefab"?BloomTag:SmallBloomTag;
   var actor=new GameObject(tag);actor.transform.SetParent(templates.transform,false);
   foreach(var visual in contract.visuals)
   {
    var target=Characters.PoseChain(actor.transform,visual.poses);
    var sprite=target.gameObject.AddComponent<SpriteRenderer>();sprite.sprite=SaintRosaAssets.Sprite(visual.sprite);sprite.color=visual.color;sprite.sortingOrder=visual.order;
    sprite.sharedMaterial=player.playerSprite.sharedMaterial;
   }
   if(tag==ShellTag)
   {
    actor.layer=LayerMask.NameToLayer("PlayerProjectile");
    var body=actor.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.useFullKinematicContacts=true;body.gravityScale=0;
    var move=actor.AddComponent<MoveComponent2D>();move.drag=1;move.Rb=body;
    var knockback=actor.AddComponent<Knockback>();knockback.knockbackForce=20;
    var circle=actor.AddComponent<CircleCollider2D>();circle.radius=contract.radius;circle.isTrigger=false;
    var shot=actor.AddComponent<Projectile>();Plugin.SetField(shot,"rb",body);Plugin.SetField(shot,"kb",knockback);Plugin.SetField(shot,"move",move);
    actor.AddComponent<RosaShell>();Plugin.SetField(actor.AddComponent<TimeToLive>(),"lifetime",contract.lifetime);
   }
   else{var burst=actor.AddComponent<RosaBurst>();burst.Field=contract.field;}
   pools.Register(tag,actor);
  }
 }
}

[Serializable]public sealed class RosaVolley
{public bool bloomAvailable,bloomTriggered,largeBloom;public float bloomDamage,bloomRadius,splashDamage,splashRadius;}
public sealed class RosaShell:MonoBehaviour
{
 internal SaintRosaSkill Owner;internal RosaVolley Volley;bool processing;
 internal float Remaining;
 internal void Configure(SaintRosaSkill owner,RosaVolley volley){Owner=owner;Volley=volley;processing=false;Remaining=SaintRosaAssets.Contract("shellPrefab").lifetime;}
 void Update(){Remaining-=Time.deltaTime;}
 internal void Trigger(AndroidProjectile contact,GameObject target,Health health)
 {
  var owner=Owner;var volley=Volley;var center=target.transform.position;float damage=contact.Shot.damage;
  contact.Shot.piercing=0;processing=true;
  try{contact.BaseTrigger(target,health);}finally{processing=false;}
  if(owner!=null&&volley!=null)owner.Explosion(center,volley,damage);
 }
 void OnDisable()
 {
  var owner=Owner;var volley=Volley;Owner=null;Volley=null;
  if(processing)return;
  if(owner!=null&&volley!=null)owner.Explosion(transform.position,volley,GetComponent<Projectile>().damage);
 }
}
public sealed class RosaBurst:MonoBehaviour
{
 public string Field;internal SaintRosaAssets.Actor Contract=>SaintRosaAssets.Contract(Field);internal float Elapsed;SpriteRenderer view;
 internal void Play(float radius)
 {
  view=GetComponentInChildren<SpriteRenderer>();Elapsed=0;
  float scale=2*radius/Mathf.Max(.01f,SaintRosaAssets.Sprite(Contract.frames[0]).bounds.size.x);
  transform.localScale=Vector3.one*scale;Draw();PlayerVisualOpacity.Attach(gameObject);
 }
 internal void Draw()
 {
  if(view==null)view=GetComponentInChildren<SpriteRenderer>();
  view.sprite=SaintRosaAssets.Sprite(Contract.frames[Mathf.Min(Contract.frames.Length-1,Mathf.FloorToInt(Elapsed/Contract.duration*Contract.frames.Length))]);
  view.color=new Color(1,1,1,1-Elapsed/Contract.duration);
 }
 void Update(){Elapsed+=Time.deltaTime;if(Elapsed>=Contract.duration)AndroidPools.Return(gameObject);else Draw();}
}

[HarmonyPatch(typeof(SkillPassive),"OnDestroy")]
class SaintRosaDestruction{static void Postfix(SkillPassive __instance){if(__instance is SaintRosaSkill rosa)rosa.DestroySource();}}
