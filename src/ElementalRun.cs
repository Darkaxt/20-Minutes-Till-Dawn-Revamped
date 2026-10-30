using System.Linq;
using UnityEngine;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization;

// Native art and a small reusable pulse pool owned by the actual battle.
public sealed partial class ElementalRun:MonoBehaviour
{
 internal static ElementalRun Instance;internal float BodyWidth;internal bool Chain;
 GameObject pulse;Sprite circle;Material material;
 void Awake(){Instance=this;}
 void Start()
 {
  var reference=ObjectPooler.SharedInstance.itemDictionary["BrainMonster"].objectToPool;
  var shape=reference.GetComponent<CircleCollider2D>();
  BodyWidth=(shape!=null?shape.radius*2:reference.GetComponent<BoxCollider2D>().size.x)*Mathf.Abs(reference.transform.localScale.x);
  var renderer=GetComponent<GameController>().playerFogRevealer.GetComponentInChildren<SpriteRenderer>(true);circle=renderer.sprite;material=renderer.sharedMaterial;
 }
 internal void Pulse(Vector2 position,float radius,Color color)
 {
  if(pulse==null)
  {
   pulse=new GameObject("Revamped_ElementPulse");pulse.SetActive(false);pulse.transform.SetParent(transform,false);pulse.AddComponent<ElementalPulse>();
   for(int i=0;i<3;i++){var child=new GameObject("Ring"+i,typeof(SpriteRenderer));child.layer=9;child.transform.SetParent(pulse.transform,false);child.transform.localScale=Vector3.one*(1-i*.16f);var r=child.GetComponent<SpriteRenderer>();r.sprite=circle;r.sharedMaterial=material;r.sortingOrder=10;}
   ObjectPooler.SharedInstance.AddObject(pulse.name,pulse,-1);
  }
  var actor=ObjectPooler.SharedInstance.GetPooledObject(pulse.name);actor.transform.position=position;actor.transform.localScale=Vector3.one*(radius/circle.bounds.extents.x);
  actor.GetComponent<ElementalPulse>().Show(color);
 }
 internal void Arc(Vector2 from,Vector2 to)
 {
  var root=new GameObject("LightningChain",typeof(LineRenderer),typeof(ElementalArc));root.layer=9;root.transform.SetParent(transform,false);
  var line=root.GetComponent<LineRenderer>();line.sharedMaterial=material;line.startColor=line.endColor=new Color(.65f,.85f,1,.85f);line.startWidth=line.endWidth=.035f;line.positionCount=2;line.SetPosition(0,from);line.SetPosition(1,to);line.sortingOrder=15;
  PlayerVisualOpacity.Attach(root);
 }
 internal SpriteRenderer Status(Transform target)
 {
  var child=new GameObject("ElementalStatus",typeof(SpriteRenderer));child.layer=9;child.transform.SetParent(target,false);child.transform.localScale=Vector3.one*(BodyWidth*.6f/circle.bounds.extents.x);
  var renderer=child.GetComponent<SpriteRenderer>();renderer.sprite=circle;renderer.sharedMaterial=material;renderer.sortingOrder=5;return renderer;
 }
 void OnDestroy(){ReleaseStatusArt();if(Instance==this)Instance=null;}
}
public sealed class ElementalPulse:MonoBehaviour
{
 float remaining;Color color;SpriteRenderer[] rings;
 internal void Show(Color value){remaining=.25f;color=value;if(rings==null)rings=GetComponentsInChildren<SpriteRenderer>();Paint();}
 void Paint(){for(int i=0;i<rings.Length;i++)rings[i].color=new Color(color.r,color.g,color.b,(.05f+.04f*i)*remaining/.25f);}
 void Update(){remaining-=Time.deltaTime;if(remaining<=0)AndroidPools.Return(gameObject);else Paint();}
}
public sealed class ElementalArc:MonoBehaviour
{float remaining=.15f;void Update(){remaining-=Time.deltaTime;if(remaining<=0)Destroy(gameObject);}}
