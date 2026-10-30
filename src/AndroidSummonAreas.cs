using System.Collections.Generic;
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
  SummonPulseVisual.Show(this);
  var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();targets.Clear();pool.GetMonsterInRange(targets,Range,transform.position);
  for(int i=targets.Count-1;i>=0;i--)
  {
   var actor=pool.GetFromID(targets[i]);if(actor==null)continue;
   var health=actor.GetComponent<Health>();if(health!=null)AndroidDamageRounding.ChangeHealth(health,-Mathf.FloorToInt(Damage),DamageType.Summon);
  }
 }
}
// One reusable wave per summon; its world-space center stays at the actual
// impact point even when a scythe or lance moves away during the animation.
public sealed class SummonPulseVisual:MonoBehaviour
{
 LineRenderer wave;Material material;float elapsed,radius;Vector3 center;
 internal static void Show(AndroidSummonPulse pulse)
 {
  var visual=pulse.GetComponent<SummonPulseVisual>()??pulse.gameObject.AddComponent<SummonPulseVisual>();
  visual.center=pulse.transform.position;visual.radius=pulse.Range;visual.elapsed=0;visual.enabled=true;visual.Paint();
 }
 void Awake()
 {
  var child=new GameObject("SummonPulseWave",typeof(LineRenderer));child.layer=9;child.transform.SetParent(transform,false);
  wave=child.GetComponent<LineRenderer>();material=new Material(Shader.Find("Sprites/Default"));wave.sharedMaterial=material;
  wave.useWorldSpace=true;wave.loop=true;wave.positionCount=48;wave.startWidth=wave.endWidth=.035f;wave.sortingOrder=15;
  PlayerVisualOpacity.Attach(child);
 }
 void Paint()
 {
  float progress=Mathf.Clamp01(elapsed/.4f);float size=radius*Mathf.Lerp(.15f,1,progress);
  var color=new Color(.82f,.76f,.95f,(1-progress)*.65f);wave.startColor=wave.endColor=color;
  for(int i=0;i<wave.positionCount;i++){float angle=i*Mathf.PI*2/wave.positionCount;wave.SetPosition(i,center+new Vector3(Mathf.Cos(angle),Mathf.Sin(angle))*size);}
  wave.enabled=true;
 }
 void Update(){elapsed+=Time.deltaTime;if(elapsed>=.4f){wave.enabled=false;enabled=false;}else Paint();}
 void OnDisable(){if(wave!=null)wave.enabled=false;}
 void OnDestroy(){if(material!=null)Destroy(material);}
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
