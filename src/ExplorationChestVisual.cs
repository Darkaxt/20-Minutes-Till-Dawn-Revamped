using UnityEngine;
using flanne.Pickups;
namespace TwentyMinutes.Modernization;

// Fog reveal colors belong to the native mask cameras. Reward colors use
// separate world sprites, so revealing content never changes the fog channels.
public sealed class ExplorationChestVisual:MonoBehaviour
{
 public const float RevealDistance=8,FullDistance=2;
 public float Strength{get;private set;}
 ExplorationChest chest;SpriteRenderer body;readonly SpriteRenderer[] glow=new SpriteRenderer[3];
 public static Color RewardColor(ExplorationReward reward)
 {
  switch(reward)
  {
   case ExplorationReward.Experience:return new Color(.54f,.84f,.73f);
   case ExplorationReward.Credits:return RewardColor(ExplorationReward.Experience);
   case ExplorationReward.Healing:return new Color(.96f,.55f,.64f);
   case ExplorationReward.Magnet:return new Color(.65f,.57f,.91f);
   case ExplorationReward.Freeze:return new Color(.48f,.78f,.97f);
   case ExplorationReward.Frenzy:return new Color(1,.55f,.29f);
   case ExplorationReward.Upgrade:return new Color(.72f,.88f,.40f);
   case ExplorationReward.CharacterUpgrade:return new Color(.94f,.64f,.91f);
   default:throw new System.ArgumentOutOfRangeException(nameof(reward));
  }
 }
 internal static void Attach(ExplorationChest chest,ChestPickup template)
 {
  var visual=chest.gameObject.AddComponent<ExplorationChestVisual>();visual.chest=chest;visual.body=chest.GetComponent<SpriteRenderer>();
  foreach(string name in new[]{"FogCircleRed","FogCircleBlue"})
  {
   var original=template.transform.Find(name);var reveal=Instantiate(original.gameObject,chest.transform,false);reveal.name=name;
  }
  var circle=template.transform.Find("FogCircleRed").GetComponent<SpriteRenderer>();
  for(int i=0;i<visual.glow.Length;i++)
  {
   var halo=new GameObject("RewardGlow"+i,typeof(SpriteRenderer));halo.transform.SetParent(chest.transform,false);halo.layer=template.transform.Find("Arrow").gameObject.layer;
   halo.transform.localScale=Vector3.one*(1.05f-.22f*i);
   var renderer=halo.GetComponent<SpriteRenderer>();renderer.sprite=circle.sprite;renderer.sharedMaterial=circle.sharedMaterial;renderer.sortingLayerID=visual.body.sortingLayerID;renderer.sortingOrder=visual.body.sortingOrder-1;
   visual.glow[i]=renderer;
  }
  visual.Refresh();
 }
 void LateUpdate()=>Refresh();
 void Refresh()
 {
  var player=flanne.PlayerController.Instance;
  Strength=player==null?0:Mathf.Clamp01((RevealDistance-Vector2.Distance(transform.position,player.transform.position))/(RevealDistance-FullDistance));
  var color=RewardColor(chest.RefreshPresentation());body.color=Color.Lerp(ExplorationRun.Mint,color,Strength);
  for(int i=0;i<glow.Length;i++)
  {
   var renderer=glow[i];renderer.enabled=Strength>0;
   renderer.color=new Color(color.r,color.g,color.b,Strength*(.04f+.04f*i));
  }
 }
}
