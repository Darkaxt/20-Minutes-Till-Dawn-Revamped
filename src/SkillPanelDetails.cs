using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using flanne;
using flanne.UI;
using flanne.PerkSystem;
using flanne.PerkSystem.Actions;
namespace TwentyMinutes.Modernization;

[HarmonyPatch(typeof(PowerupTreeUI),nameof(PowerupTreeUI.Refresh))]
class VisibleUpgradePath
{
 static void Postfix(PowerupTreeUI __instance)
 {
  // Finite offer binding previously hid the serialized chart slots along with
  // unused choices. Native sprite binding does not re-enable their objects.
  foreach(string field in new[]{"startingPowerup","leftPowerup","rightPowerup","finalPowerup"})
  {
   var icon=(PowerupIcon)AccessTools.Field(typeof(PowerupTreeUI),field).GetValue(__instance);icon.gameObject.SetActive(icon.data!=null);
   // These are previews, not offers. Preserve native tooltip/sprite binding,
   // without allowing an unoffered chart skill to become the chosen toggle.
   var toggle=icon.GetComponent<UnityEngine.UI.Toggle>();
   if(toggle!=null){var colors=toggle.colors;colors.disabledColor=colors.normalColor;toggle.colors=colors;toggle.interactable=false;}
  }
 }
}
[HarmonyPatch(typeof(Powerup),"get_description")]
class ReadableRiskTomes
{
 static void Postfix(Powerup __instance,ref string __result)
 {
  string key=RunResume.Key(__instance);
  if(key=="tome_of_glass_name")__result="<color=#A8D5BA>All damage ×2</color> on the first pick (×4, ×6 on repeats).<br><color=#E8A3A8>No dodge. Any unprotected damaging hit is fatal.</color>";
  if(key=="tome_of_stillness_name")__result="<color=#A8D5BA>Bullet Damage +100%<br>Bullet Size +100% · Projectiles +1</color><br><color=#E8A3A8>Cannot move while shooting.</color>";
 }
}
public sealed class SkillPanelDetails:MonoBehaviour
{
 RectTransform interactions;
 public class BenefitIcon{public string Key;public Sprite Sprite;}
 public class Relation{public string Label;public bool Penalty,Mixed;public List<BenefitIcon> Icons;}
 internal void Refresh(PowerupDescription owner)
 {
  var power=owner.data;if(power==null)return;
  var root=(RectTransform)owner.transform;root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,560);
  var name=(TMP_Text)AccessTools.Field(typeof(PowerupDescription),"nameTMP").GetValue(owner);
  var description=(TMP_Text)AccessTools.Field(typeof(PowerupDescription),"descriptionTMP").GetValue(owner);
  StatsOverlay.Rect(name.gameObject,root,new Vector2(0,1),new Vector2(0,1),new Vector2(16,-10),new Vector2(314,24));
  bool hasPath=power.powerupTreeUIData!=null;
  StatsOverlay.Rect(description.gameObject,root,new Vector2(0,1),new Vector2(0,1),new Vector2(hasPath?122:16,-39),new Vector2(hasPath?208:314,80));
  description.enableAutoSizing=true;description.fontSizeMin=8;description.fontSizeMax=11;description.overflowMode=TextOverflowModes.Overflow;
  var tree=(PowerupTreeUI)AccessTools.Field(typeof(PowerupDescription),"powerupTreeUI").GetValue(owner);
  if(tree!=null){StatsOverlay.Rect(tree.gameObject,root,new Vector2(0,.5f),new Vector2(.5f,.5f),new Vector2(65,-16),new Vector2(100,100));tree.transform.localScale=Vector3.one*.8f;}
  // The root Image is PowerupFrame; BG is only its dark interior fill.
  var nativeFrame=tree.GetComponentsInChildren<PowerupIcon>(true).First().GetComponent<Image>();
  if(interactions==null)interactions=StatsOverlay.Rect(new GameObject("BuildInteractions",typeof(RectTransform)),root,new Vector2(0,1),new Vector2(0,1),new Vector2(350,-20),new Vector2(194,104));
  foreach(Transform child in interactions){child.gameObject.SetActive(false);Destroy(child.gameObject);}
  var relations=Describe(power);
  var bands=new[]{relations.Where(r=>!r.Penalty&&!r.Mixed).ToArray(),relations.Where(r=>r.Mixed).ToArray(),relations.Where(r=>r.Penalty).ToArray()}.Where(r=>r.Length>0).ToArray();
  for(int band=0;band<bands.Length;band++)
  {
   var row=bands[band];bool mixed=row[0].Mixed,penalty=row[0].Penalty;var labels=new List<TMP_Text>();float top=-(bands.Length==3?34:52)*band,labelHeight=bands.Length==3?10:12,iconOffset=bands.Length==3?14:18;
   foreach(var relation in row){var label=StatsOverlay.Text(interactions,"Relation",description.font,relation.Label,9,Vector2.zero,new Vector2(194,labelHeight));label.color=mixed?new Color(.91f,.81f,.65f):penalty?new Color(.91f,.64f,.66f):new Color(.66f,.84f,.73f);labels.Add(label);}
   // Labels sit above their icons. Both rows begin at the same x; the native
   // hexagons have a full ten-unit gap even at their largest size.
   float desiredWidth=row.Select((r,i)=>Mathf.Max(labels[i].preferredWidth,r.Icons.Count*28-4)).Sum()+Mathf.Max(0,row.Length-1)*8;
   float scale=desiredWidth>194?194/desiredWidth:1,size=(bands.Length==3?16:24)*scale,gap=4*scale,x=0;
   for(int group=0;group<row.Length;group++)
   {
    var label=labels[group];float width=Mathf.Max(label.preferredWidth*scale,row[group].Icons.Count*(size+gap)-gap);label.fontSize=9*scale;label.rectTransform.anchoredPosition=new Vector2(x,top);label.rectTransform.sizeDelta=new Vector2(width,labelHeight);
    foreach(var item in row[group].Icons)
    {
     var frame=new GameObject("InteractionHexagon",typeof(RectTransform),typeof(Image));StatsOverlay.Rect(frame,interactions,new Vector2(0,1),new Vector2(0,1),new Vector2(x,top-iconOffset),Vector2.one*size);
     var border=frame.GetComponent<Image>();border.sprite=nativeFrame.sprite;border.color=nativeFrame.color;border.material=nativeFrame.material;border.type=nativeFrame.type;border.preserveAspect=true;border.raycastTarget=false;
     var icon=new GameObject((mixed?"Mixed_":penalty?"Penalty_":"Benefit_")+item.Key,typeof(RectTransform),typeof(Image));StatsOverlay.Rect(icon,frame.transform,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,Vector2.one*(size*.8f));
     var image=icon.GetComponent<Image>();image.sprite=item.Sprite;image.preserveAspect=true;image.raycastTarget=false;x+=size+gap;
    }
    x=label.rectTransform.anchoredPosition.x+width+8*scale;
   }
  }
 }
 static StatChange[] Stats(Powerup p)=>(StatChange[])AccessTools.Field(typeof(Powerup),"statChanges").GetValue(p);
 static Summon[] SummonActors(Powerup p)
 {
  var effects=(PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(p);
  return effects.Select(e=>AccessTools.Field(typeof(PerkEffect),"action").GetValue(e)).OfType<AttachPrefabToPlayerAction>()
   .Select(a=>(GameObject)AccessTools.Field(typeof(AttachPrefabToPlayerAction),"prefab").GetValue(a))
   .Where(pf=>pf!=null).SelectMany(pf=>pf.GetComponentsInChildren<Summon>(true)).ToArray();
 }
 static BenefitIcon Icon(Powerup power)=>new BenefitIcon{Key=RunResume.Key(power),Sprite=power.icon};
 static bool Gain(StatChange change)=>change.isFlatMod?change.flatValue>0:change.value>0;
 static bool Loss(StatChange change)=>change.isFlatMod?change.flatValue<0:change.value<0;
 static void Add(List<Relation> rows,string label,IEnumerable<BenefitIcon> icons,bool penalty=false)
 {var distinct=icons.GroupBy(i=>i.Key).Select(g=>g.First()).ToList();if(distinct.Count>0)rows.Add(new Relation{Label=label,Penalty=penalty,Icons=distinct});}
 public static List<Relation> Describe(Powerup power)
 {
  var player=PlayerController.Instance;var hud=StatsOverlay.Instance;
  // Native DataUIBinding.Start also refreshes this panel during InitState,
  // before the loadout exists. Selection refresh supplies the real build later.
  var messages=new List<Relation>();
  if(player==null||hud==null||player.loadedCharacter==null||player.gun==null||player.gun.gunData==null)return messages;
  var owned=hud.Collected.Values.ToArray();var stats=Stats(power);var actors=SummonActors(power);var benefited=new List<BenefitIcon>();var penalized=new List<BenefitIcon>();
  foreach(bool penalty in new[]{false,true})
  {
   var changed=stats.Where(s=>penalty?Loss(s):Gain(s)).ToArray();var affected=penalty?penalized:benefited;
   bool damage=changed.Any(s=>s.type==StatType.SummonDamage),speed=changed.Any(s=>s.type==StatType.SummonAttackSpeed);
   if(damage||speed)
   {
    affected.AddRange(owned.Where(e=>e.Power!=null&&SummonActors(e.Power).Any(s=>damage||speed&&s is AttackingSummon)).Select(e=>Icon(e.Power)));
    string character=player.loadedCharacter.name;
    if(character=="Hastur"||damage&&(character=="Lilith"||character=="Hina"))affected.Add(new BenefitIcon{Key="character:"+character,Sprite=player.loadedCharacter.portrait});
   }
   if(changed.Any(s=>new[]{StatType.BulletDamage,StatType.FireRate,StatType.ReloadRate,StatType.Projectiles,StatType.ProjectileSize,StatType.ProjectileSpeed,StatType.Piercing,StatType.Bounce}.Contains(s.type)))
    affected.Add(new BenefitIcon{Key="weapon:"+player.gun.gunData.name,Sprite=player.gun.gunData.icon});
  }
  if(actors.Length>0)
  {
   bool attacks=actors.Any(s=>s is AttackingSummon);
   Add(messages,"Benefits from",owned.Where(e=>e.Power!=null&&Stats(e.Power).Any(s=>Gain(s)&&(s.type==StatType.SummonDamage||attacks&&s.type==StatType.SummonAttackSpeed))).Select(e=>Icon(e.Power)));
   Add(messages,"Penalties from",owned.Where(e=>e.Power!=null&&Stats(e.Power).Any(s=>Loss(s)&&(s.type==StatType.SummonDamage||attacks&&s.type==StatType.SummonAttackSpeed))).Select(e=>Icon(e.Power)),true);
  }
  // Named multi-prerequisite recipes are distinct from ordinary stat effects.
  var previous=new HashSet<string>(hud.Collected.Keys);var acquired=new HashSet<string>(previous);acquired.Add(RunResume.Key(power));
  var recipes=Resources.FindObjectsOfTypeAll<Powerup>().Where(p=>p.powerupTreeUIData==null&&p.prereqs!=null&&p.prereqs.Count>1&&p.prereqs.Any(r=>RunResume.Key(r)==RunResume.Key(power)))
   .Where(p=>p.anyPrereqFulfill?p.prereqs.Any(r=>acquired.Contains(RunResume.Key(r))):p.prereqs.All(r=>acquired.Contains(RunResume.Key(r))))
   .Where(p=>!(p.anyPrereqFulfill?p.prereqs.Any(r=>previous.Contains(RunResume.Key(r))):p.prereqs.All(r=>previous.Contains(RunResume.Key(r)))))
   .GroupBy(RunResume.Key).Select(g=>Icon(g.First())).ToArray();
  if(RunResume.Key(power)=="ghost_wizard_name")benefited.AddRange(owned.Where(e=>e.Power!=null&&RunResume.Key(e.Power)=="ghost_friend_name").Select(e=>Icon(e.Power)));
  Add(messages,"Benefits",benefited);Add(messages,"Penalties",penalized,true);Add(messages,"Synergy",recipes);
  // A weapon/summon can gain one attribute and lose another. Report that
  // real trade-off once instead of making contradictory binary claims.
  var mixed=messages.Where(r=>r.Label!="Synergy").SelectMany(r=>r.Icons.Select(i=>new{r.Penalty,Icon=i})).GroupBy(i=>i.Icon.Key).Where(g=>g.Any(i=>i.Penalty)&&g.Any(i=>!i.Penalty)).Select(g=>g.First().Icon).ToList();
  var keys=new HashSet<string>(mixed.Select(i=>i.Key));
  foreach(var relation in messages.Where(r=>r.Label!="Synergy"))relation.Icons.RemoveAll(i=>keys.Contains(i.Key));
  messages.RemoveAll(r=>r.Icons.Count==0);
  if(mixed.Count>0)messages.Add(new Relation{Label="Mixed (+ / −)",Mixed=true,Icons=mixed});
  return messages;
 }
}
[HarmonyPatch(typeof(PowerupDescription),nameof(PowerupDescription.Refresh))]
class CurrentBuildSkillDetails
{
 static void Postfix(PowerupDescription __instance)
 {
  // The native chart exists only on the level-up description, not every small
  // choice icon/tooltip. Reuse that panel and its existing selection events.
  if(AccessTools.Field(typeof(PowerupDescription),"powerupTreeUI").GetValue(__instance)==null)return;
  var details=__instance.GetComponent<SkillPanelDetails>();if(details==null)details=__instance.gameObject.AddComponent<SkillPanelDetails>();details.Refresh(__instance);
 }
}
