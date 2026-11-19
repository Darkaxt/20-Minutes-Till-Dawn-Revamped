using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source Summon.Init creates a DurationDamageBuff, then configures the first
// component and registers its owner. Derived Init paths which omit base.Init
// have no pulse/listener. Tick phase survives pooled reuse and repeated picks.
public sealed class AndroidSummonPulse:MonoBehaviour
{
 internal const string UpdateEvent="Summon_DurationDamage_Update";
 internal static bool DurationDamage;
 internal static string DurationData="";
 public float Timer,Damage,Interval,Range;
 readonly List<int> targets=new List<int>();
 internal static void Clear(){DurationDamage=false;DurationData="";}
 internal static void Initialize(Summon summon)
 {
  summon.gameObject.AddComponent<AndroidSummonPulse>();
  var owner=summon.GetComponent<AndroidSummonPulse>();owner.UpdateData(null,DurationData);
  summon.AddObserver(owner.UpdateData,UpdateEvent);
 }
 internal static void Activate(object sender)
 {
  DurationDamage=true;
  DurationData="50,2,3";
  sender.PostNotification(UpdateEvent,DurationData);
 }
 void UpdateData(object sender,object args)
 {
  DurationData=(string)args;var pulse=GetComponent<AndroidSummonPulse>();pulse.enabled=DurationDamage;
  if(string.IsNullOrEmpty(DurationData))return;
  var values=DurationData.Split(',');pulse.Damage=int.Parse(values[0]);pulse.Interval=float.Parse(values[1]);pulse.Range=float.Parse(values[2]);
 }
 void Update()
 {
  Timer+=Time.deltaTime;if(Timer<Interval)return;Timer-=Interval;
  var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();targets.Clear();pool.GetMonsterInRange(targets,Range,transform.position);
  for(int i=targets.Count-1;i>=0;i--)
  {
   var actor=pool.GetFromID(targets[i]);if(actor==null)continue;
   var health=actor.GetComponent<Health>();if(health!=null)AndroidDamageRounding.ChangeHealth(health,-Mathf.FloorToInt(Damage),DamageType.Summon);
  }
  SummonPulseVisual.Show(this);
 }
}
// Android8.7's authored pooled SpriteFX stays at the impact point. It copies
// the source prefab's rotation/scale independently of the moving summon.
public sealed class SummonPulseVisual:MonoBehaviour
{
 GameObject prefab;
 internal static void Show(AndroidSummonPulse pulse)
 {
  var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();
  var visual=pool.GetComponent<SummonPulseVisual>()??pool.gameObject.AddComponent<SummonPulseVisual>();
  if(visual.prefab==null)visual.Build(pool);
  var actor=pool.Get(visual.prefab.name,pulse.transform.position);if(actor==null)return;
  actor.transform.rotation=visual.prefab.transform.rotation;actor.transform.localScale=visual.prefab.transform.localScale;
  PlayerVisualOpacity.Attach(actor);
 }
 void Build(AndroidPools pool)
 {
  var contract=SaintRosaAssets.Contract("pulsePrefab");
  var hidden=new GameObject("SourcePulseTemplate");hidden.SetActive(false);hidden.transform.SetParent(transform,false);
  prefab=new GameObject(contract.name);prefab.transform.SetParent(hidden.transform,false);
  foreach(var source in contract.visuals)
  {
   var target=Characters.PoseChain(prefab.transform,source.poses);var view=target.gameObject.AddComponent<SpriteRenderer>();
   view.sprite=SaintRosaAssets.Sprite(source.sprite);view.color=source.color;view.sortingOrder=source.order;view.sharedMaterial=PlayerController.Instance.playerSprite.sharedMaterial;
   if(source.path=="FogReveal")
   {
    var native=Object.FindObjectOfType<flanne.Core.GameController>().playerFogRevealer.GetComponentInChildren<SpriteRenderer>(true);
    target.gameObject.layer=native.gameObject.layer;view.sharedMaterial=native.sharedMaterial;view.sortingLayerID=native.sortingLayerID;view.sortingOrder=native.sortingOrder;
    continue;
   }
   var fx=target.gameObject.AddComponent<SpriteFX>();Plugin.SetField(fx,"spriteRenderer",view);Plugin.SetField(fx,"secPerFrame",contract.interval);Plugin.SetField(fx,"loop",contract.loop);Plugin.SetField(fx,"sprites",contract.frames.Select(SaintRosaAssets.Sprite).ToArray());
  }
  Plugin.SetField(prefab.AddComponent<TimeToLive>(),"lifetime",contract.lifetime);pool.Register(prefab.name,prefab);
 }
}
// This callback belongs to the original powerup asset. Repeated applications
// grant another heart but AddObserver retains the same single callback owner.
public sealed class AndroidSoulLink:ScriptableObject
{
 readonly List<int> targets=new List<int>();
 internal void Apply(){PlayerController.Instance.playerHealth.shp++;this.AddObserver(Lost,AndroidSoulHealth.ChangeEvent);}
 void Lost(object sender,object args)
 {
  if((int)args>=0)return;
  var center=PlayerController.Instance.transform.position;
  targets.Clear();ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetMonsterInRange(targets,3,center);
  for(int i=0;i<targets.Count;i++){var actor=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().GetFromID(targets[i]);var victim=actor!=null?actor.GetComponent<Health>():null;if(victim!=null)AndroidDamageRounding.ChangeHealth(victim,-Mathf.FloorToInt(victim.maxHP*.8f),DamageType.Summon);}
 }
}
