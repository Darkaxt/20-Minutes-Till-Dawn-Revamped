using System;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using flanne;
using flanne.RuneSystem;
namespace TwentyMinutes.Modernization;

// Unscaled animation runs inside the paused reward popup. Visual cycling has
// no random gameplay side effects and stops on the journaled result.
public sealed class RuneConversionVisual:MonoBehaviour
{
 sealed class Slot{internal Image Icon,Edge;internal Button Button;internal Outline Glow;internal TMP_Text Rank;internal Vector2 Origin;internal RuneRewardDraw Draw;}
 sealed class Gem{internal Image Icon;internal Vector2 Origin,Scatter;internal float Delay;}
 sealed class Ember{internal Image Icon;internal Vector2 Origin;internal float Delay,Side;}
 readonly List<Slot> slots=new List<Slot>();readonly List<Gem> gems=new List<Gem>();readonly List<Ember> embers=new List<Ember>();
 public const float RollSeconds=4f,SettleSeconds=.9f;
 RuneBossReward reward;Action checkpoint,completed;Action<RuneRewardDraw> show;float spin,settle,elapsed,duration;bool burning,holding,rollingStarted;
 SoundEffectSO leadup,revealSound;
 public IReadOnlyList<Button> Buttons=>slots.Select(s=>s.Button).ToArray();
 public bool Ready{get;private set;}
 public float Progress=>burning&&duration>0?Mathf.Clamp01(elapsed/duration):0;
 internal void Initialize(RuneBossReward reward,Action checkpoint,Action completed,Action<RuneRewardDraw> show)
 {
  this.reward=reward;this.checkpoint=checkpoint;this.completed=completed;this.show=show;
  var sounds=Resources.FindObjectsOfTypeAll<SoundEffectSO>();leadup=sounds.First(s=>s.name=="ChestOpenLeadUp_SFX");revealSound=sounds.First(s=>s.name=="ChestOpen_SFX");
  int columns=Math.Min(5,reward.draws.Count),rows=(reward.draws.Count+columns-1)/columns;
  for(int i=0;i<reward.draws.Count;i++)
  {
   var origin=new Vector2((i%columns-(columns-1)*.5f)*108,(rows>1?132:96)-i/columns*86);
   var icon=Icon("RuneSlot"+i,null,origin,new Vector2(52,52));icon.color=new Color(.3f,.37f,.38f,1);
   var button=icon.gameObject.AddComponent<Button>();button.targetGraphic=icon;button.interactable=false;
   var glow=icon.gameObject.AddComponent<Outline>();glow.effectColor=new Color(1,.8f,.4f,0);glow.effectDistance=new Vector2(2,-2);
   var edge=Icon("RuneBurnEdge"+i,null,origin,new Vector2(52,4));edge.enabled=false;
   var rank=StatsOverlay.Text(transform,"RuneSlotRank"+i,StatsOverlay.Instance.Font,"",17,Vector2.zero,new Vector2(100,24));
   var rect=rank.rectTransform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);rect.anchoredPosition=origin+Vector2.down*43;rank.alignment=TextAlignmentOptions.Center;
   var slot=new Slot{Icon=icon,Button=button,Glow=glow,Edge=edge,Rank=rank,Origin=origin,Draw=reward.draws[i]};slots.Add(slot);
   // Native selection and pointer hover both reveal the selected result's
   // description, after the animation/activation decisions have completed.
   var trigger=icon.gameObject.AddComponent<EventTrigger>();
   foreach(var kind in new[]{EventTriggerType.Select,EventTriggerType.PointerEnter})
   {var entry=new EventTrigger.Entry{eventID=kind};entry.callback.AddListener(_=>{if(button.interactable)this.show(slot.Draw);});trigger.triggers.Add(entry);}
   button.onClick.AddListener(()=>this.show(slot.Draw));
   if(i<reward.revealed)Reveal(slot,false);
  }
  if(reward.phase==1)BeginBurn();
 }
 Sprite Sprite(string name)=>Resources.FindObjectsOfTypeAll<RuneData>().First(r=>r.name==name).icon;
 void Reveal(Slot slot,bool celebrate)
 {
  slot.Icon.sprite=Sprite(slot.Draw.rune);slot.Icon.color=Color.white;slot.Rank.text=slot.Draw.capped?"MAX":slot.Draw.rank+"/5";slot.Rank.color=slot.Draw.capped?StatsOverlay.Coral:StatsOverlay.Cream;
  show(slot.Draw);if(celebrate){revealSound.Play();Plugin.Trace("rune-reward-revealed",new{slot.Draw.rune,slot.Draw.rank,slot.Draw.capped,seconds=spin});}
 }
 Image Icon(string name,Sprite sprite,Vector2 position,Vector2 size)
 {var icon=StatsOverlay.Box(transform,name,new Vector2(.5f,.5f),new Vector2(.5f,.5f),position,size,Color.white).GetComponent<Image>();icon.sprite=sprite;icon.preserveAspect=true;icon.raycastTarget=false;return icon;}
 void BeginBurn()
 {
  burning=true;elapsed=0;var capped=slots.Where(s=>s.Draw.capped).ToArray();
  if(capped.Length==0){Complete();return;}
  var xp=ObjectPooler.SharedInstance.pooledObjectsDictionary["SmallXP"][0].GetComponentInChildren<SpriteRenderer>(true);
  foreach(var slot in capped)
  {
   slot.Icon.type=Image.Type.Filled;slot.Icon.fillMethod=Image.FillMethod.Vertical;slot.Icon.fillOrigin=0;
   for(int j=0;j<8;j++){var ember=Icon("RuneEmber",null,slot.Origin,new Vector2(4,4));ember.enabled=false;embers.Add(new Ember{Icon=ember,Origin=slot.Origin+new Vector2(j*7-24,16-j*4),Delay=.3f+j*.06f,Side=j%2==0?-1:1});}
   for(int j=0;j<6*slot.Draw.tier;j++)
   {
    float angle=j*2.399963f;var scatter=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(30+j%5*7);
    var gem=Icon("ConvertedXP",xp.sprite,slot.Origin,new Vector2(14,14));gem.color=xp.color;gem.enabled=false;
    float launch=.55f+j*.025f;gems.Add(new Gem{Icon=gem,Origin=slot.Origin,Scatter=scatter,Delay=launch});duration=Mathf.Max(duration,launch+.85f);
   }
  }
 }
 void Complete()
 {
  var xp=ObjectPooler.SharedInstance.pooledObjectsDictionary["SmallXP"][0].transform.Find("Bounce").GetComponent<SpriteRenderer>();
  foreach(var slot in slots)
  {
   if(slot.Draw.capped){slot.Icon.type=Image.Type.Simple;slot.Icon.sprite=xp.sprite;slot.Icon.color=xp.color;slot.Rank.text="XP";slot.Rank.alpha=1;slot.Rank.color=StatsOverlay.Green;}
   slot.Icon.rectTransform.localScale=Vector3.one;slot.Glow.effectColor=new Color(1,.8f,.4f,0);slot.Icon.raycastTarget=true;
  }
  Ready=true;var finish=completed;completed=null;finish?.Invoke();
 }
 void Update()
 {
  if(Ready)return;
  if(!burning)
  {
   if(holding)
   {
    settle+=Time.unscaledDeltaTime;var winner=slots[reward.revealed-1];float pulse=Mathf.Clamp01(settle/SettleSeconds);
    winner.Icon.rectTransform.localScale=Vector3.one*(1+.22f*Mathf.Sin(pulse*Mathf.PI));winner.Glow.effectColor=new Color(1,.8f,.4f,.9f*(1-pulse));
    if(pulse<1)return;holding=false;winner.Icon.rectTransform.localScale=Vector3.one;
    if(reward.phase==1)BeginBurn();return;
   }
   var slot=slots[reward.revealed];spin+=Time.unscaledDeltaTime;
   if(!rollingStarted){leadup.Play();rollingStarted=true;}
   float progress=Mathf.Clamp01(spin/RollSeconds);int cycle=Mathf.FloorToInt(48*(1-Mathf.Pow(1-progress,2.4f)));
   slot.Icon.sprite=Sprite(RuneRewardRun.Candidates[slot.Draw.tier-1][(cycle+reward.revealed*3)%6]);slot.Icon.color=Color.white;
   if(spin<RollSeconds)return;
   Reveal(slot,true);reward.revealed++;spin=0;rollingStarted=false;holding=true;settle=0;
   if(reward.revealed==slots.Count)reward.phase=1;
   checkpoint();return;
  }
  elapsed+=Time.unscaledDeltaTime;
  foreach(var slot in slots.Where(s=>s.Draw.capped))
  {
   float burn=Mathf.Clamp01((elapsed-.3f)/.65f);slot.Icon.fillAmount=1-burn;slot.Icon.color=Color.Lerp(Color.white,new Color(1,.45f,.25f),burn);slot.Rank.alpha=1-burn;
   slot.Edge.enabled=burn>0&&burn<1;slot.Edge.color=new Color(1,.75f,.4f,1);slot.Edge.rectTransform.anchoredPosition=slot.Origin+Vector2.up*(26-52*burn);
  }
  foreach(var ember in embers){float age=elapsed-ember.Delay;ember.Icon.enabled=age>=0&&age<.4f;if(ember.Icon.enabled){ember.Icon.rectTransform.anchoredPosition=ember.Origin+new Vector2(ember.Side*age*18,age*45);ember.Icon.color=new Color(1,.6f,.3f,1-age/.4f);}}
  foreach(var gem in gems)
  {
   float age=elapsed-gem.Delay;gem.Icon.enabled=age>=0&&age<.85f;if(!gem.Icon.enabled)continue;
   float scatter=Mathf.Clamp01(age/.2f),fly=Mathf.Clamp01((age-.2f)/.65f);fly=fly*fly*(3-2*fly);
   var point=Vector2.Lerp(gem.Origin+gem.Scatter*scatter,new Vector2(0,-84),fly);gem.Icon.rectTransform.anchoredPosition=new Vector2(Mathf.Round(point.x/2)*2,Mathf.Round(point.y/2)*2);
  }
  if(elapsed>=duration)Complete();
 }
}
