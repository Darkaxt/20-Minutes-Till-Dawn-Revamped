using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 void ContourShape(Sprite body,Sprite edge)
 {
  Require(!body.packed,"native enemy frame uses the verified unrotated source texture contract");
  var sampled=ColorfulStyle.Sample(body.texture);var pixels=sampled.GetPixels32();Object.Destroy(sampled);
  var rect=body.textureRect;var mask=edge.texture.GetPixels32();int width=(int)rect.width,height=(int)rect.height,mw=edge.texture.width;
  bool clean=true;for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(pixels[((int)rect.y+y)*body.texture.width+(int)rect.x+x].a>8&&mask[(y+3)*mw+x+3].a!=0)clean=false;
  Require(clean,"contour leaves every opaque source pixel untouched: "+body.name);
  Require(mask.Any(p=>p.a==255)&&mask.Any(p=>p.a==75)&&mask.Any(p=>p.a==20),"contour has a crisp outside edge and two softer pixel-glow bands");
  Require(edge.texture.filterMode==FilterMode.Point&&edge.texture.width==width+6&&edge.texture.height==height+6,"contour padding/filtering retains native pixel geometry");
  CombatClose(edge.pixelsPerUnit,body.pixelsPerUnit,"contour shares the source pixel density");
 }
 IEnumerator EliteContourPresentation(GameController game)
 {
  var masks=new List<Sprite>();var player=PlayerController.Instance;Vector3 origin=player.transform.position;
  foreach(string tag in new[]{"Boomer","BigBoomer","BrainMonster","EyeMonster","Lamprey","PF_Cultist","EyeBat","GhostMonster","PumpkinMonster","PF_SquidMonster","WitchMonster"})
  {
   Require(ObjectPooler.SharedInstance.itemDictionary.ContainsKey(tag),"loaded source pool for contour coverage: "+tag);
   var ordinary=EliteSpawn(tag);var body=ordinary.GetComponent<SpriteRenderer>();while(body.sprite==null)yield return null;
   var original=ordinary.transform.localScale;var collider=ordinary.GetComponent<Collider2D>();Physics2D.SyncTransforms();Vector3 size=collider.bounds.size;
   ReturnFixtureActor(ordinary.gameObject);Plugin.SetField(ScoreCalculator.SharedInstance,"_enemiesKilled",(EliteClaims+1)*200);
   var elite=EliteSpawn(tag);Require(Enhanced(elite),"actual regular-enemy promotion: "+tag);
   var ai=elite.GetComponent<AIComponent>();elite.transform.position=origin+Vector3.right*(Mathf.Sqrt(ai.specialRangeSqr)+6);
   foreach(var direction in new[]{Vector2.left,Vector2.right,Vector2.left})
   {
    AccessTools.Method(typeof(AIController),"AILookTowards").Invoke(AIController.SharedInstance,new object[]{ai,direction});
    CombatClose(Mathf.Abs(elite.transform.localScale.x),Mathf.Abs(original.x)*1.25f,"native facing preserves 125 percent X for "+tag);
    CombatClose(Mathf.Abs(elite.transform.localScale.y),Mathf.Abs(original.y)*1.25f,"native facing preserves 125 percent Y for "+tag);
   }
   Vector3 before=elite.transform.position;while(elite.transform.position==before)yield return null;
   CombatClose(Mathf.Abs(elite.transform.localScale.x),Mathf.Abs(original.x)*1.25f,"actual native movement retains elite width for "+tag);
   ai.enabled=false;elite.GetComponent<MoveComponent2D>().enabled=false;elite.transform.position=origin+new Vector3(2,1);
   yield return null;var contour=elite.transform.Find("EliteContour").GetComponent<SpriteRenderer>();
   Require(contour.enabled&&contour.sprite.name=="EliteContour_"+body.sprite.name,"contour follows the actual animated frame: "+tag);ContourShape(body.sprite,contour.sprite);masks.Add(contour.sprite);
   Physics2D.SyncTransforms();CombatClose(collider.bounds.size.x,size.x*1.25f,"visible/collision width remains proportional for "+tag);CombatClose(collider.bounds.size.y,size.y*1.25f,"visible/collision height remains proportional for "+tag);
   var animator=elite.GetComponent<Animator>();float cycle=animator.GetCurrentAnimatorStateInfo(0).normalizedTime+1;
   while(animator.GetCurrentAnimatorStateInfo(0).normalizedTime<cycle)yield return null;yield return null;
   Require(contour.sprite.name=="EliteContour_"+body.sprite.name,"animated silhouette refreshes without a circular status sprite: "+tag);
   if(tag=="Boomer")
   {
    var normal=EliteSpawn(tag);normal.GetComponent<AIComponent>().enabled=false;normal.GetComponent<MoveComponent2D>().enabled=false;normal.transform.position=origin+new Vector3(-2,1);while(normal.GetComponent<SpriteRenderer>().sprite==null)yield return null;
    foreach(bool reborn in new[]{false,true})
    {
     if(ColorfulStyle.Instance.Enabled!=reborn)ColorfulStyle.Instance.Toggle();yield return null;
     foreach(var viewport in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})
     {Screen.SetResolution(viewport.x,viewport.y,false);yield return WaitDisplay(viewport.x,viewport.y);yield return PoiCapture("elite-contour-"+(reborn?"reborn":"original")+"-"+viewport.x+".png");}
    }
    game.pauseController.Pause();game.ChangeState<PauseState>();yield return null;var color=contour.color;yield return null;Require(contour.color==color,"native pause freezes the contour glow");yield return MenuPointer(game.pauseResumeButton);while(!(game.CurrentState is CombatState))yield return null;
    normal.GetComponent<AIComponent>().enabled=true;normal.GetComponent<MoveComponent2D>().enabled=true;ReturnFixtureActor(normal.gameObject);
    ai.enabled=true;elite.GetComponent<MoveComponent2D>().enabled=true;elite.transform.position=origin+Vector3.right*.3f;
    while(elite.gameObject.activeSelf)
    {CombatClose(Mathf.Abs(elite.transform.localScale.x),Mathf.Abs(original.x)*1.25f,"Boomer explosion windup keeps elite geometry");yield return null;}
    Require(!contour.enabled,"native exploder death removes its contour before pool reuse");
   }
   else{ai.enabled=true;elite.GetComponent<MoveComponent2D>().enabled=true;ReturnFixtureActor(elite.gameObject);}
   Require(elite.transform.localScale==original&&!contour.enabled,"pool return restores exact scale and hides contour for "+tag);
   var reused=EliteSpawn(tag);Require(reused==elite&&!Enhanced(reused)&&!contour.enabled,"ordinary pool reuse never retains a contour for "+tag);ReturnFixtureActor(reused.gameObject);
  }
  yield return AchievementExit(game,false);Require(masks.All(s=>s==null),"battle teardown releases all derived contour sprites");
  SetPhase("PASS");Log("complete","All11 regular wave actor types, native Boomer facing/movement/windup/death, outside-alpha glow, animated silhouettes, both styles/three viewports, pause and exact pool/resource teardown");Application.Quit(0);
 }
}
