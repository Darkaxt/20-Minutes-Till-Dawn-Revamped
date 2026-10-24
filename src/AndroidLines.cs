using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// The source's separate collider prefab uses its electron tag. Windows has no
// such registered tag; the marker carries that identity for collision-consumer
// integration without modifying the installed game's tag database.
public sealed class AndroidElectroEdge:MonoBehaviour{}
public sealed class AndroidLinePositions:MonoBehaviour
{
 static readonly FieldInfo Targets=AccessTools.Field(typeof(LineController),"targets");
 static readonly Vector3[] Initial={new Vector3(15,-10,0),new Vector3(-15,-10,0),new Vector3(-15,10,0),new Vector3(15,10,0)};
 internal Vector3[] Positions;
 void OnEnable()
 {
  var targets=(GameObject[])Targets.GetValue(GetComponent<LineController>());
  for(int i=0;i<Initial.Length;i++)targets[i].transform.localPosition=Initial[i];
 }
}
public sealed class AndroidLineSegments:MonoBehaviour
{
 readonly List<PolygonCollider2D> segments=new List<PolygonCollider2D>();
 readonly Dictionary<int,Vector2[]> paths=new Dictionary<int,Vector2[]>();
 internal void Set(int index,Vector2 first,Vector2 second,float width)
 {
  PolygonCollider2D shape;
  if(segments.Count>index)shape=segments[index];
  else
  {
   // The native template is the entire ElectroLine GameObject, including its
   // controller. Recreate only the three source-template components instead
   // of recursively cloning the native controller and renderer per segment.
   var edge=new GameObject("Collider");edge.SetActive(false);edge.layer=(int)TagLayerUtil.EnemyProjectile;edge.tag="EProjectile";
   shape=edge.AddComponent<PolygonCollider2D>();var body=edge.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.useFullKinematicContacts=true;
   edge.AddComponent<AndroidElectroEdge>();edge.transform.SetParent(transform);edge.SetActive(true);
   segments.Add(shape);paths.Add(index,new Vector2[4]);
  }
  float angle=Mathf.Atan2(second.y-first.y,second.x-first.x);
  Vector2 offset=new Vector2(-width*Mathf.Sin(angle),width*Mathf.Cos(angle));
  var path=paths[index];path[0]=first+offset;path[1]=first-offset;path[2]=second-offset;path[3]=second+offset;
  shape.gameObject.SetActive(true);shape.enabled=true;shape.transform.position=Vector3.zero;shape.SetPath(0,path);
  // Source keeps previously allocated segments when the point count shrinks
  // or loop is switched off. Do not prune or disable those cached colliders.
 }
}
[HarmonyPatch(typeof(LineController),"Start")]
class AndroidLineStart
{
 static bool Prefix(LineController __instance,LineRenderer ___lineRenderer,GameObject[] ___targets)
 {if(__instance.GetComponent<AndroidLinePositions>()==null)__instance.gameObject.AddComponent<AndroidLinePositions>();___lineRenderer.positionCount=___targets.Length;return false;}
}
[HarmonyPatch(typeof(LineController),"Update")]
class AndroidLineUpdate{static bool Prefix(LineController __instance){__instance.GetPositions();return false;}}
[HarmonyPatch(typeof(LineController),nameof(LineController.GetPositions))]
class AndroidLineRead
{
 static bool Prefix(LineController __instance,LineRenderer ___lineRenderer,GameObject[] ___targets,ref Vector3[] __result)
 {
  var state=__instance.GetComponent<AndroidLinePositions>();if(state==null)state=__instance.gameObject.AddComponent<AndroidLinePositions>();
  if(state.Positions==null)state.Positions=new Vector3[___targets.Length];
  for(int i=0;i<___targets.Length;i++)state.Positions[i]=___targets[i].transform.position;
  ___lineRenderer.SetPositions(state.Positions);__result=state.Positions;return false;
 }
}
[HarmonyPatch(typeof(LineCollider),"LateUpdate")]
class AndroidLineColliderStep
{
 static bool Prefix(LineCollider __instance,LineController ___lineController,PolygonCollider2D ___polygonCollider2D,bool ___loop)
 {
  var state=__instance.GetComponent<AndroidLineSegments>();if(state==null)state=__instance.gameObject.AddComponent<AndroidLineSegments>();
  // Android's source template is an inactive separate GameObject. Windows'
  // original polygon lives on the live renderer/controller root.
  ___polygonCollider2D.enabled=false;
  var points=___lineController.GetPositions();
  if(points.Length<2){___polygonCollider2D.pathCount=0;return false;}
  float width=___lineController.GetWidth();int last=points.Length-1;
  for(int i=0;i<last;i++)state.Set(i,points[i],points[i+1],width);
  if(___loop)state.Set(last,points[last],points[0],width);return false;
 }
}
