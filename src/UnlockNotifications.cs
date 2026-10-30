using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using flanne;
using flanne.Core;
using flanne.TitleScreen;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// Product UI only. Native combat/title states control visibility; the clock
// below controls presentation duration, never completion of game operations.
public class UnlockNotifications:MonoBehaviour
{
 public static UnlockNotifications Instance;
 public GameObject Root;public Image Icon;public TMP_Text Label;
 public string Showing{get;private set;}
 float visibleSeconds;
 void Awake(){Instance=this;}
 void OnApplicationQuit(){Progression.Save();}
 bool CanShow()
 {
  var game=Object.FindObjectOfType<GameController>();
  if(game!=null)return game.CurrentState is CombatState&&!PauseController.isPaused;
  return Progression.Title!=null&&Progression.Title.CurrentState is TitleMainMenuState;
 }
 void Build()
 {
  var font=Resources.FindObjectsOfTypeAll<TMP_Text>().FirstOrDefault(t=>t.font!=null&&t.gameObject.scene.IsValid())?.font;if(font==null)return;
  var canvas=new GameObject("AchievementNotifications",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));canvas.transform.SetParent(transform,false);
  canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=22;
  var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
  Root=new GameObject("UnlockNotice",typeof(RectTransform));StatsOverlay.Rect(Root,canvas.transform,new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-46),new Vector2(560,80));
  Root.AddComponent<GameplayHudOpacity>();
  var iconObject=new GameObject("ContentIcon",typeof(RectTransform),typeof(Image));StatsOverlay.Rect(iconObject,Root.transform,new Vector2(0,1),new Vector2(0,1),new Vector2(4,-8),new Vector2(44,44));
  Icon=iconObject.GetComponent<Image>();Icon.preserveAspect=true;Icon.raycastTarget=false;
  Label=StatsOverlay.Text(Root.transform,"UnlockNameAndGoal",font,"",18,new Vector2(60,-4),new Vector2(496,76));
  Root.SetActive(false);
 }
 void Update()
 {
  if(Progression.State==null)return;
  bool eligible=CanShow();if(Root!=null)Root.SetActive(eligible&&Showing!=null);
  if(!eligible)return;
  if(Showing!=null)
  {
   visibleSeconds+=Time.unscaledDeltaTime;
   if(visibleSeconds<4f)return;
   Showing=null;Root.SetActive(false);
  }
  if(Progression.State.pending.Count==0)return;
  if(Root==null)Build();if(Root==null)return;
  string id=Progression.State.pending[0];var gate=Progression.Gates.Single(g=>g.Id==id);
  var notice=Progression.State.notices.Single(n=>n.id==id);string completedGoal=notice.goal;
  // Commit delivery before making the notice visible. Other pending notices
  // remain queued across a close, including a close during this one.
  Progression.State.pending.RemoveAt(0);Progression.State.notices.Remove(notice);if(!Progression.State.notified.Contains(id))Progression.State.notified.Add(id);Progression.Save();
  Showing=id;visibleSeconds=0;Icon.sprite=gate.Icon;Icon.enabled=gate.Icon!=null;
  Label.text="<color=#FC525F>Unlocked: "+gate.Name+"</color><br>"+completedGoal;
  Root.SetActive(true);Plugin.Trace("achievement-notified",id);
 }
}

[HarmonyPatch(typeof(ForestMapRhogogSpawner),"OnRhogogDefeated")]
class SecretBossAchievement
{
 static bool Prefix()
 {
  if(Progression.State==null)return true;
  Progression.SecretBoss();GameTimer.SharedInstance.Start();return false;
 }
}
