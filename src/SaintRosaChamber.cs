using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.Core;
namespace TwentyMinutes.Modernization;

// Source seven-frame chamber animation, placed in the compact native status
// corner rather than the mobile action-button canvas. HUD alpha is inherited.
[DefaultExecutionOrder(310)]
public sealed class SaintRosaChamber:MonoBehaviour
{
 SaintRosaSkill skill;RectTransform root;readonly List<Image> slots=new List<Image>();float blink;
 internal void Initialize(SaintRosaSkill value){skill=value;skill.ChamberChanged+=Changed;}
 void Changed(){if(skill.ShellCount<skill.BloomThreshold)blink=0;}
 void LateUpdate()
 {
  var game=RunRecorder.Instance?.GetComponent<GameController>();var corner=game?.hud.GetComponent<CornerStatusHud>();
  if(corner?.StatusRoot==null)return;
  if(root==null)root=StatsOverlay.Rect(new GameObject("RoseChamber",typeof(RectTransform)),corner.StatusRoot,new Vector2(0,1),new Vector2(0,1),new Vector2(104,-84),new Vector2(220,22));
  while(slots.Count<skill.Capacity)
  {
   var rect=StatsOverlay.Rect(new GameObject("RoseShell"+slots.Count,typeof(RectTransform),typeof(Image)),root,new Vector2(0,1),new Vector2(0,1),new Vector2(slots.Count*18,0),new Vector2(16,22));
   var image=rect.GetComponent<Image>();image.preserveAspect=true;image.raycastTarget=false;slots.Add(image);
  }
  if(skill.ShellCount>=skill.BloomThreshold)blink+=Time.deltaTime;
  for(int i=0;i<slots.Count;i++)
  {
   bool partial=i==skill.ShellCount&&skill.ShotProgress>0;
   var image=slots[i];image.enabled=PlayerController.Instance.playerHealth.hp>0&&(i<skill.ShellCount||partial);
   int frame=i<skill.ShellCount?(skill.ShellCount>=skill.BloomThreshold&&Mathf.FloorToInt(blink/.2f)%2!=0?5:6):Mathf.Clamp(Mathf.CeilToInt(skill.ShotProgress*6f/skill.ShotsPerShell)-1,0,5);
   image.sprite=SaintRosaAssets.Sprite(SaintRosaAssets.Data.chamber[frame]);
  }
 }
 void OnDestroy(){if(skill!=null)skill.ChamberChanged-=Changed;if(root!=null)Destroy(root.gameObject);}
}
