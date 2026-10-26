using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
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
  if(key=="tome_of_stillness_name")__result="<color=#A8D5BA>Bullet Damage +100%<br>Bullet Size +100% · Projectiles +1</color><br><color=#E8A3A8>Move Speed −100%</color>";
 }
}
public sealed class SkillPanelDetails:MonoBehaviour
{
 TMP_Text interactions;
 internal void Refresh(PowerupDescription owner)
 {
  var power=owner.data;if(power==null)return;
  var root=(RectTransform)owner.transform;root.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,560);
  var name=(TMP_Text)AccessTools.Field(typeof(PowerupDescription),"nameTMP").GetValue(owner);
  var description=(TMP_Text)AccessTools.Field(typeof(PowerupDescription),"descriptionTMP").GetValue(owner);
  StatsOverlay.Rect(name.gameObject,root,new Vector2(0,1),new Vector2(0,1),new Vector2(16,-10),new Vector2(360,24));
  StatsOverlay.Rect(description.gameObject,root,new Vector2(0,1),new Vector2(0,1),new Vector2(16,-39),new Vector2(360,60));
  description.enableAutoSizing=true;description.fontSizeMin=8;description.fontSizeMax=11;description.overflowMode=TextOverflowModes.Overflow;
  var tree=(PowerupTreeUI)AccessTools.Field(typeof(PowerupDescription),"powerupTreeUI").GetValue(owner);
  if(tree!=null)StatsOverlay.Rect(tree.gameObject,root,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(214,-7),new Vector2(100,100));
  if(interactions==null)interactions=StatsOverlay.Text(root,"BuildInteractions",description.font,"",8,new Vector2(16,-101),new Vector2(360,28));
  interactions.color=new Color(.66f,.84f,.73f);interactions.enableAutoSizing=true;interactions.fontSizeMin=8;interactions.fontSizeMax=9;interactions.lineSpacing=2;
  interactions.text=Explain(power);
 }
 static StatChange[] Stats(Powerup p)=>(StatChange[])AccessTools.Field(typeof(Powerup),"statChanges").GetValue(p);
 static Summon[] SummonActors(Powerup p)
 {
  var effects=(PerkEffect[])AccessTools.Field(typeof(Powerup),"effects").GetValue(p);
  return effects.Select(e=>AccessTools.Field(typeof(PerkEffect),"action").GetValue(e)).OfType<AttachPrefabToPlayerAction>()
   .Select(a=>(GameObject)AccessTools.Field(typeof(AttachPrefabToPlayerAction),"prefab").GetValue(a))
   .Where(pf=>pf!=null).SelectMany(pf=>pf.GetComponentsInChildren<Summon>(true)).ToArray();
 }
 internal static string Explain(Powerup power)
 {
  var player=PlayerController.Instance;var hud=StatsOverlay.Instance;
  // Native DataUIBinding.Start also refreshes this panel during InitState,
  // before the loadout exists. Selection refresh supplies the real build later.
  if(player==null||hud==null||player.loadedCharacter==null||player.gun==null||player.gun.gunData==null)return "";
  var owned=hud.Collected.Values.ToArray();var stats=Stats(power);var messages=new List<string>();var actors=SummonActors(power);
  bool damage=stats.Any(s=>s.type==StatType.SummonDamage),speed=stats.Any(s=>s.type==StatType.SummonAttackSpeed);
  if(damage||speed)
  {
   var consumers=owned.Where(e=>e.Power!=null&&SummonActors(e.Power).Length>0).Select(e=>e.Name).ToList();
   if(player.loadedCharacter.name=="Lilith")consumers.Add("Lilith’s spirits");
   if(player.loadedCharacter.name=="Hastur")consumers.Add("Hastur’s tentacles");
   if(player.loadedCharacter.name=="Hina")consumers.Add("Hina’s shadow clone");
   if(damage&&consumers.Count>0)messages.Add("Summon damage → "+string.Join(", ",consumers.Distinct()));
   var attackers=owned.Where(e=>e.Power!=null&&SummonActors(e.Power).Any(s=>s is AttackingSummon)).Select(e=>e.Name).ToList();
   if(player.loadedCharacter.name=="Hastur")attackers.Add("Hastur’s tentacles");
   if(speed&&attackers.Count>0)messages.Add("Summon attack speed → "+string.Join(", ",attackers.Distinct()));
  }
  if(actors.Length>0)
  {
   messages.Add("Summon · uses your summon damage"+(actors.Any(s=>s is AttackingSummon)?" and attack speed":""));
   bool attacks=actors.Any(s=>s is AttackingSummon);
   var buffs=owned.Where(e=>e.Power!=null&&Stats(e.Power).Any(s=>s.type==StatType.SummonDamage||attacks&&s.type==StatType.SummonAttackSpeed)).Select(e=>e.Name).Distinct().ToArray();
   if(buffs.Length>0)messages.Add("Benefits from: "+string.Join(", ",buffs));
  }
  if(stats.Any(s=>new[]{StatType.BulletDamage,StatType.FireRate,StatType.ReloadRate,StatType.Projectiles,StatType.ProjectileSize,StatType.ProjectileSpeed,StatType.Piercing,StatType.Bounce}.Contains(s.type)))
   messages.Add("Weapon: "+player.gun.gunData.nameString);
  // Named multi-prerequisite recipes are distinct from ordinary stat effects.
  var previous=new HashSet<string>(hud.Collected.Keys);var acquired=new HashSet<string>(previous);acquired.Add(RunResume.Key(power));
  var recipes=Resources.FindObjectsOfTypeAll<Powerup>().Where(p=>p.powerupTreeUIData==null&&p.prereqs!=null&&p.prereqs.Count>1&&p.prereqs.Any(r=>RunResume.Key(r)==RunResume.Key(power)))
   .Where(p=>p.anyPrereqFulfill?p.prereqs.Any(r=>acquired.Contains(RunResume.Key(r))):p.prereqs.All(r=>acquired.Contains(RunResume.Key(r))))
   .Where(p=>!(p.anyPrereqFulfill?p.prereqs.Any(r=>previous.Contains(RunResume.Key(r))):p.prereqs.All(r=>previous.Contains(RunResume.Key(r)))))
   .GroupBy(RunResume.Key).Select(g=>g.First().nameString).ToArray();
  if(recipes.Length>0)messages.Add("Synergy unlocked by this pick: "+string.Join(", ",recipes));
  if(RunResume.Key(power)=="ghost_wizard_name")messages.Add("Ghost Friend’s shots gain your bullet effects and 6 base burn damage.");
  return string.Join("\n",messages);
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
