using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne.TitleScreen;
using flanne;
using flanne.Core;
using flanne.UI;
using flanne.PowerupSystem;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 IEnumerator CheckPlaytestColors()
 {
  SetPhase("actual-shotgun-firestarter-colors");RunResume.Invalidate();Progression.State.earned.Add("weapon:ShotgunData");Progression.Refresh();
  var style=ColorfulStyle.Instance;if(!style.Enabled)style.Toggle();
  yield return AchievementRun("Shana","ShotgunData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var player=PlayerController.Instance;
  player.playerPerks.Equip(FallbackPool(game.powerupGenerator).Select(i=>i.powerup).First(p=>p.name=="FireStarter"));
  var generator=player.GetComponentsInChildren<ProjectileOnShoot>().Single();while(WindField(generator,"OP")==null)yield return null;
  CameraZoom.Value=0;FeedbackCameras();
  game.playerInput.SwitchCurrentControlScheme("Keyboard&Mouse",fixtureKeyboard,fixtureMouse);game.playerInput.neverAutoSwitchControlSchemes=true;
  int shots=0;UnityEngine.Events.UnityAction observed=()=>shots++;player.gun.OnShoot.AddListener(observed);
  var aim=(Vector2)Camera.main.WorldToScreenPoint(player.transform.position+Vector3.right*5);
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim}.WithButton(MouseButton.Left));while(shots<4)yield return null;
  InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=aim});yield return new WaitForFixedUpdate();Time.timeScale=0;player.gun.OnShoot.RemoveListener(observed);
  Require(shots==4,"real native input fires four Shotgun rounds without returning its live projectiles");
  var fireball=Object.FindObjectsOfType<Projectile>().Single(p=>p.name=="PF_Fireball").gameObject;
  Require(fireball.activeInHierarchy,"actual Shotgun fourth shot creates the source Fire Starter projectile");
  var ps=fireball.GetComponentInChildren<ParticleSystem>();var tint=FeedbackParticleColor(ps);
  Require(tint.r>tint.g&&tint.g>tint.b,"actual fireball renderer is orange");
  // Native particle simulation is explicit because batchmode has no automatic
  // render pass. Preserve its original modules/seed/shape; advance only0.05s.
  ps.Simulate(.05f,true,false);ps.Play(true);
  var pellets=ObjectPooler.SharedInstance.pooledObjectsDictionary["PF_ShotgunProjectile"].Where(p=>p.activeSelf).ToArray();Require(pellets.Length>0,"actual Shotgun pellets remain active for rendered verification");
  foreach(var pellet in pellets){var trail=pellet.GetComponentInChildren<TrailRenderer>();Require(trail.startColor.r>trail.startColor.b,"actual Shotgun trails use the warm recolor");}
  Log("fixture-precondition","Real native gun input/fourth-shot producer and particle renderer. Explicit0.05s native particle advancement supports batch rendering; no fabricated sprite or geometry.");
  Log("cursor-render-role",ShootingCursor.Instance.name+" layer="+ShootingCursor.Instance.gameObject.layer);
  yield return Capture("actual-shotgun-fireball-recolored.png");
  foreach(var renderer in fireball.GetComponentsInChildren<Renderer>())Log("fireball-renderer",renderer.GetType().Name+" shader="+renderer.sharedMaterial?.shader?.name+" material="+renderer.sharedMaterial?.name+" sprite="+(renderer as SpriteRenderer)?.sprite?.name+" texture="+(renderer as SpriteRenderer)?.sprite?.texture?.name+" color="+(renderer as SpriteRenderer)?.color);
  var coverage=Resources.FindObjectsOfTypeAll<GameObject>().Where(p=>!p.scene.IsValid()&&p.layer==(int)TagLayerUtil.PlayerProjectile).Where(p=>p.GetComponentsInChildren<TrailRenderer>(true).Length>0||p.GetComponentsInChildren<ParticleSystem>(true).Length>0).GroupBy(p=>p.name).Select(g=>g.First()).ToArray();
  foreach(var template in coverage)Log("procedural-projectile-audit",template.name+" trails="+template.GetComponentsInChildren<TrailRenderer>(true).Length+" particles="+template.GetComponentsInChildren<ParticleSystem>(true).Length);
  Time.timeScale=1;while(!(player.CurrentState is flanne.Player.IdleState))yield return null;
  SetPhase("PASS");Log("complete","Actual Shotgun/Fire Starter producer, warm pellet trails, orange live fireball shader and bounded procedural-template inventory");Application.Quit(0);
 }
 static Color FeedbackParticleColor(ParticleSystem ps)
 {
  var renderer=ps.GetComponent<ParticleSystemRenderer>();var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);var material=renderer.sharedMaterial;
  string key=material.HasProperty("_Color")?"_Color":"_TintColor";var color=block.GetColor(key);return color==Color.clear?material.GetColor(key):color;
 }
 IEnumerator CheckPlaytestContracts()
 {
  SetPhase("feedback-native-contracts");RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var player=PlayerController.Instance;var hud=StatsOverlay.Instance;
  while(hud.Panel==null)yield return null;
  StatsOverlay.Mode=MetricsView.Extended;yield return CycleMetrics("Off");Require(hud.Panel.activeSelf&&hud.SkillContent.gameObject.activeInHierarchy&&!hud.Body.transform.parent.gameObject.activeSelf,"Off keeps skills independent of metrics");yield return CycleMetrics("Little");yield return CycleMetrics("Extended");
  player.stats[StatType.ReloadRate].AddMultiplierBonus(-.5f);hud.Refresh();Require(hud.LastStatsText.Split('\n').Single(s=>s.StartsWith("Reload")).Contains("#E8A3A8"),"longer reload is detrimental pastel red");player.stats[StatType.ReloadRate].AddMultiplierBonus(.5f);
  yield return BugOffers(game);
  game.ChangeState<PowerupMenuState>();while(WindField(game.powerupMenu,"ConfirmEvent")==null)yield return null;
  var description=game.powerupMenuPanel.GetComponentsInChildren<PowerupDescription>(true).Single();
  var trees=Resources.FindObjectsOfTypeAll<PowerupTreeUIData>().GroupBy(t=>t.name).Select(g=>g.First()).ToArray();Require(trees.Length==25,"complete loaded inventory has25 regular upgrade paths");
  foreach(var tree in trees)
  {
   description.data=tree.startingPowerup;var view=(PowerupTreeUI)WindField(description,"powerupTreeUI");
   var expected=new[]{tree.startingPowerup,tree.leftPowerup,tree.rightPowerup,tree.finalPowerup}.Select(RunResume.Key).OrderBy(k=>k).ToArray();
   Require(view.GetComponentsInChildren<PowerupIcon>().Select(i=>RunResume.Key(i.data)).OrderBy(k=>k).SequenceEqual(expected),"all four exact native nodes bind for "+tree.name);
  }
  game.powerupMenu.OnConfirmClicked();while(!(game.CurrentState is CombatState))yield return null;
  // Complete a real collectible boundary, rather than injecting its event.
  var chest=ObjectPooler.SharedInstance.GetPooledObject("Chest");chest.transform.position=player.transform.position+Vector3.right*40;yield return null;
  player.SetPosition(chest.transform.position);Physics2D.SyncTransforms();while(!(game.CurrentState is ChestState))yield return null;
  Require(chest.GetComponent<AndroidCollectIgnore>()!=null,"physical player/chest contact marks the actual reward as collected");
  Require(!hud.Panel.GetComponentsInChildren<Image>(true).Any(i=>i.name=="BossChestDirection"&&i.gameObject.activeSelf),"actual collection hides the edge marker");
  var take=(Button)WindField(game.chestUIController,"takeButton");while(!take.IsInteractable())yield return null;yield return MenuPointer(take);while(!(game.CurrentState is CombatState))yield return null;
  yield return AchievementExit(game,false);Require(!hud.Panel.GetComponentsInChildren<Image>(true).Any(i=>i.name=="BossChestDirection"),"scene exit destroys every old chest marker");
  SetPhase("PASS");Log("complete","F2 modes/reload red, finite/empty native offers, all25 exact tree bindings, physical chest collection and scene teardown");Application.Quit(0);
 }
 static void FeedbackCameras()
 {foreach(var pp in Object.FindObjectsOfType<UnityEngine.U2D.PixelPerfectCamera>().Where(p=>p.enabled)){AccessTools.Method(pp.GetType(),"LateUpdate").Invoke(pp,null);AccessTools.Method(pp.GetType(),"OnPreCull").Invoke(pp,null);}Canvas.ForceUpdateCanvases();}
 IEnumerator FeedbackOffer(GameController game,Gamepad pad)
 {
  game.ChangeState<PowerupMenuState>();while(WindField(game.powerupMenu,"ConfirmEvent")==null)yield return null;
  while(game.powerupMenu.GetComponentsInParent<CanvasGroup>().Any(g=>g.alpha<.99f))yield return null;
  var description=game.powerupMenuPanel.GetComponentsInChildren<PowerupDescription>(true).Single();
  var toggles=(Toggle[])WindField(game.powerupMenu,"toggles");
  foreach(var toggle in toggles.Where(t=>t.gameObject.activeInHierarchy&&t.GetComponentInParent<PowerupTreeUI>()==null))
  {
   yield return MenuMove(pad,game.powerupMenu.GetComponentInParent<Canvas>().rootCanvas.transform,toggle);
   var data=toggle.GetComponent<DataUIBinding<Powerup>>().data;
   Require(description.data==data,"actual controller focus refreshes selected skill "+data.name);
   var tree=(PowerupTreeUI)WindField(description,"powerupTreeUI");
   if(data.powerupTreeUIData!=null)
   {
    var icons=tree.GetComponentsInChildren<PowerupIcon>();Require(icons.Length==4,"four native upgrade-path icons are active for "+data.name);
    FeedbackCameras();foreach(var icon in icons){var at=HudPoint((RectTransform)icon.transform);Require(at.x>0&&at.x<Screen.width&&at.y>0&&at.y<Screen.height,"upgrade icon is inside actual viewport: "+icon.data.name);}
   }
  }
  yield return Capture("upgrade-paths-and-build.png");
  var confirm=Resources.FindObjectsOfTypeAll<Button>().First(b=>b.gameObject.activeInHierarchy&&Enumerable.Range(0,b.onClick.GetPersistentEventCount()).Any(i=>b.onClick.GetPersistentTarget(i)==game.powerupMenu&&b.onClick.GetPersistentMethodName(i)=="OnConfirmClicked"));
  yield return MenuPointer(confirm);while(!(game.CurrentState is CombatState))yield return null;
 }
 IEnumerator CheckPlaytestFeedback(TitleScreenController title,bool continued)
 {
  SetPhase("feedback-integrated");var pad=InputSystem.AddDevice<Gamepad>("RevampedPlaytestPad");
  if(continued)yield return CombatBuffContinue();
  else{RunResume.Invalidate();Progression.State.earned.Add("character:Lilith");Progression.Refresh();yield return AchievementRun("Lilith","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());}
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var player=PlayerController.Instance;var hud=StatsOverlay.Instance;
  while(hud.Panel==null)yield return null;
  var powers=FallbackPool(game.powerupGenerator).Select(i=>i.powerup).ToArray();
  var big=powers.First(p=>p.name=="BigShot");
  if(!continued)
  {
   Log("fixture-precondition","Native Lilith with controlled real PlayerPerks acquisitions, stopped encounter scheduler/clock and synthetic input devices. No natural unlock, drop-rate or full-run balance claim.");
   foreach(var name in new[]{"PowerShot","BigShot","BigShot","BigShot","Haste","GhostFriend","SummonMastery","Trainer","RapidFire","QuickHands"})player.playerPerks.Equip(powers.First(p=>p.name==name));
  }
  hud.Dirty();yield return null;
  Require(hud.Collected.Values.Single(e=>e.Name==big.nameString).Count==3,"tripled skill survives acquisition/Continue");
  Require(hud.SkillIcons.Count==hud.Collected.Count&&hud.SkillContent.GetComponentsInChildren<TMP_Text>().Length==0,"all collected skills are icon-only, with no count/tier labels");
  Require(hud.Collected.Values.Single(e=>e.Name==big.nameString).View.GetComponent<Outline>()!=null,"tripled skill has subtle outline glow");
  foreach(var group in hud.Collected.Values.GroupBy(e=>e.Path))Require(group.Select(e=>e.View.rectTransform.anchoredPosition.y).Distinct().Count()==1,"same upgrade path shares one HUD row: "+group.Key);
  foreach(var size in new[]{new Vector2Int(900,900),new Vector2Int(1024,768),new Vector2Int(1280,720)})
  {
   Screen.SetResolution(size.x,size.y,false);yield return WaitDisplay(size.x,size.y);FeedbackCameras();yield return null;
   yield return Capture("grouped-hud-"+size.x+".png");CheckHudBounds(hud,game);
   Require(Mathf.Abs(HudPoint((RectTransform)hud.Body.transform.parent).y-Screen.safeArea.center.y)<2,"left metrics are vertically centered in safe viewport");
   Require(Mathf.Abs(HudPoint((RectTransform)hud.SkillContent.parent).y-Screen.safeArea.center.y)<2,"right skill paths are vertically centered in safe viewport");
   hud.Refresh();
  }
  StatsOverlay.Mode=MetricsView.Little;hud.Refresh();Require(!hud.Body.text.Contains("("),"Little retains final-only values");
  float old=player.gun.reloadDuration;player.stats[StatType.ReloadRate].AddMultiplierBonus(.2f);hud.Refresh();Require(player.gun.reloadDuration<old&&hud.LastStatsText.Split('\n').Single(s=>s.StartsWith("Reload")).Contains("#A8D5BA"),"shorter reload is beneficial pastel green");player.stats[StatType.ReloadRate].AddMultiplierBonus(-.2f);
  var explain=(string)AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.SkillPanelDetails"),"Explain").Invoke(null,new object[]{powers.First(p=>p.name=="SummonMastery")});
  Require(explain.Contains("Ghost")&&explain.Contains("Lilith"),"summon damage preview explicitly covers Ghost Friend and Lilith spirits");
  var lens=powers.First(p=>p.name=="MagicLens");
  var lensDetails=(string)AccessTools.Method(AccessTools.TypeByName("TwentyMinutes.Modernization.SkillPanelDetails"),"Explain").Invoke(null,new object[]{lens});
  Log("lens-build-details",lensDetails);
  var trainerStats=(StatChange[])AccessTools.Field(typeof(Powerup),"statChanges").GetValue(powers.First(p=>p.name=="Trainer"));
  Require(trainerStats.Any(s=>s.type==StatType.SummonDamage),"Trainer also supplies summon damage, so Magic Lens legitimately benefits from it");
  Require(lensDetails.Contains(powers.First(p=>p.name=="SummonMastery").nameString)&&!lensDetails.Contains("attack speed"),"non-attacking Magic Lens lists summon damage buffs without claiming attack-speed benefits");
  foreach(int zoom in new[]{0,25,50,75,100})
  {
   CameraZoom.Value=zoom;FeedbackCameras();var target=HitEffectTarget(maximum:100000);target.transform.position=player.transform.position+Vector3.right*2;
   int before=target.HP;target.TakeDamage(DamageType.Bullet,12);yield return null;
   var popup=Object.FindObjectsOfType<AndroidDamagePopup>().Last();var text=popup.GetComponent<TextMeshPro>();
   CombatClose(text.fontSize,2*(1+zoom/100f),"damage glyph size compensates zoom"+zoom);Require(before>target.HP&&text.text.Length>0,"actual enemy damage produces visible numeric text");
   yield return Capture("damage-zoom-"+zoom+".png");ReturnFixtureActor(target.gameObject);
  }
  CameraZoom.Value=0;FeedbackCameras();
  if(!continued)
  {
   var treeCount=Resources.FindObjectsOfTypeAll<PowerupTreeUIData>().Select(t=>t.name).Distinct().Count();Log("upgrade-path-inventory",treeCount);
   yield return FeedbackOffer(game,pad);
   var style=ColorfulStyle.Instance;if(style.Enabled)style.Toggle();
   foreach(var tag in new[]{"PF_ShotgunProjectile","PF_Fireball","PF_HellfireProjectile"})
   {
    var prefab=Resources.FindObjectsOfTypeAll<GameObject>().First(o=>!o.scene.IsValid()&&o.name==tag);
    Log("color-template",tag+" layer="+prefab.layer+" visual="+(prefab.GetComponent<ColorfulProjectileVisuals>()!=null));
    // These weapon templates are normally registered by the actual producer.
    AccessTools.Method(typeof(AndroidPools),"Register").Invoke(ObjectPooler.SharedInstance.GetComponent<AndroidPools>(),new object[]{tag,prefab,null});
    var actor=ObjectPooler.SharedInstance.GetPooledObject(tag);actor.transform.position=player.transform.position+Vector3.right*3;
    var trail=actor.GetComponentInChildren<TrailRenderer>();var ps=actor.GetComponentInChildren<ParticleSystem>();
    Color original=ps!=null?FeedbackParticleColor(ps):trail.startColor;
    style.Toggle();Color colored=ps!=null?FeedbackParticleColor(ps):trail.startColor;Require(colored!=original&&Mathf.Abs(colored.a-original.a)<.001f,"recolored "+tag+" preserves alpha and colors its procedural renderer");
    yield return Capture("color-"+tag+".png");style.Toggle();Color restored=ps!=null?FeedbackParticleColor(ps):trail.startColor;Require(restored==original,"Original mode restores exact renderer color: "+tag);
    ReturnFixtureActor(actor);
   }
   yield return NativeTomePick(game,false,"TomeOfGlass");var victim=HitEffectTarget(maximum:100000);int hp=victim.HP;victim.TakeDamage(DamageType.Bullet,20);Require(hp-victim.HP==40,"Glass actual damage multiplier is2, not penalty-only");ReturnFixtureActor(victim.gameObject);
   float bullet=Bonus(player.stats[StatType.BulletDamage]),sizeBonus=Bonus(player.stats[StatType.ProjectileSize]);int count=player.gun.numOfProjectiles;
   yield return NativeTomePick(game,false,"TomeOfStillness");CombatClose(Bonus(player.stats[StatType.BulletDamage])-bullet,1,"Stillness adds100-percent bullet damage");CombatClose(Bonus(player.stats[StatType.ProjectileSize])-sizeBonus,1,"Stillness adds100-percent bullet size");CombatClose(player.finalMoveSpeed,0,"Stillness retains zero movement through native multiplicative reduction");Require(player.gun.numOfProjectiles==count+1,"Stillness adds one projectile");
  }
  var chest=Object.FindObjectsOfType<flanne.Pickups.ChestPickup>().FirstOrDefault(c=>c.GetComponent<AndroidCollectIgnore>()==null);
  if(chest==null)chest=ObjectPooler.SharedInstance.GetPooledObject("Chest").GetComponent<flanne.Pickups.ChestPickup>();
  chest.transform.position=player.transform.position+Vector3.right*40;yield return null;FeedbackCameras();yield return null;
  var marker=hud.Panel.GetComponentsInChildren<Image>().Single(i=>i.name=="BossChestDirection");Require(HudPoint(marker.rectTransform).x>Screen.width-70,"off-screen boss chest has right-edge marker");yield return Capture("offscreen-chest.png");
  chest.transform.position=player.transform.position+Vector3.up*2;yield return null;Require(!marker.gameObject.activeSelf,"visible chest hides its directional marker");chest.transform.position=player.transform.position+Vector3.right*40;yield return null;
  CameraZoom.Value=50;StatsOverlay.Mode=MetricsView.Little;Plugin.Instance.Config.Save();
  if(!continued){yield return DragonSuspend(game,"Integrated playtest HUD/menu/Tome/zoom/color/chest fixes and exact source Continue checkpoint");yield break;}
  yield return AchievementExit(game,false);Require(hud.Collected.Count==0,"scene exit clears acquired HUD paths");SetPhase("PASS");Log("complete","Current Continue restores grouped skills, exact Tome effects and chest indicators; no stale scene markers");Application.Quit(0);
 }
 IEnumerator InspectPlaytestFeedback()
 {
  RunResume.Invalidate();yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();
  var powers=Resources.FindObjectsOfTypeAll<Powerup>();
  foreach(var power in powers.Where(p=>p.name=="BigShot"||p.name=="Haste"||p.name.IndexOf("Tome",StringComparison.OrdinalIgnoreCase)>=0))
   Log("power-data",power.name+" key="+RunResume.Key(power)+" tree="+power.powerupTreeUIData?.name+" description="+power.description);
  foreach(var camera in Object.FindObjectsOfType<Camera>())Log("camera",camera.name+" mask="+camera.cullingMask+" ortho="+camera.orthographicSize+" depth="+camera.depth+" rect="+camera.rect+" target="+camera.targetTexture?.name);
  var popup=ObjectPooler.SharedInstance.GetPooledObject("DamagePopup");popup.transform.position=PlayerController.Instance.transform.position+Vector3.up;
  var text=popup.GetComponent<TextMeshPro>();text.text="123";
  Log("damage-popup",popup.name+" layer="+popup.layer+" scale="+popup.transform.lossyScale+" font="+text.fontSize+" renderer="+text.renderer.enabled+" sorting="+text.renderer.sortingLayerName+"/"+text.renderer.sortingOrder);
  foreach(var camera in Object.FindObjectsOfType<Camera>())Log("popup-camera",camera.name+" projects="+camera.WorldToViewportPoint(popup.transform.position)+" draws="+((camera.cullingMask&(1<<popup.layer))!=0));
  game.ChangeState<PowerupMenuState>();
  while(AccessTools.Field(typeof(ToggleGroupMenu<Powerup>),"ConfirmEvent").GetValue(game.powerupMenu)==null)yield return null;
  while(game.powerupMenu.GetComponentsInParent<CanvasGroup>().Any(g=>g.alpha<.99f))yield return null;
  foreach(var d in game.powerupMenuPanel.GetComponentsInChildren<PowerupDescription>(true))
  {
   var tree=(PowerupTreeUI)AccessTools.Field(typeof(PowerupDescription),"powerupTreeUI").GetValue(d);
   Log("offer-description",d.name+" data="+d.data?.name+" treeData="+d.data?.powerupTreeUIData?.name+" tree="+tree?.name+" active="+tree?.gameObject.activeInHierarchy+" scale="+tree?.transform.lossyScale);
  }
  foreach(var prefab in Resources.FindObjectsOfTypeAll<GameObject>().Where(o=>!o.scene.IsValid()&&(o.name=="PF_ShotgunProjectile"||o.name=="PF_Fireball"||o.name=="PF_HellfireProjectile")))
  {foreach(var tr in prefab.GetComponentsInChildren<TrailRenderer>(true))Log("template-trail",prefab.name+"/"+tr.name+" color="+tr.startColor+" material="+tr.sharedMaterial?.name);foreach(var ps in prefab.GetComponentsInChildren<ParticleSystem>(true))Log("template-particles",prefab.name+"/"+ps.name+" color="+ps.main.startColor.color+" mode="+ps.main.startColor.mode+" material="+ps.GetComponent<ParticleSystemRenderer>()?.sharedMaterial?.name);}
  foreach(var renderer in Resources.FindObjectsOfTypeAll<TrailRenderer>().Where(r=>r.gameObject.scene.IsValid()).GroupBy(r=>r.name).Select(g=>g.First()))Log("trail",renderer.transform.root.name+"/"+renderer.name+" material="+renderer.sharedMaterial?.name+" color="+renderer.startColor+"/"+renderer.endColor+" texture="+renderer.sharedMaterial?.mainTexture?.name);
  foreach(var ps in Resources.FindObjectsOfTypeAll<ParticleSystem>().Where(r=>r.gameObject.scene.IsValid()).GroupBy(r=>(r.transform.parent?.name??"")+"/"+r.name).Select(g=>g.First()))
   Log("particles",ps.name+" parent="+ps.transform.parent?.name+" color="+ps.main.startColor.color+" material="+ps.GetComponent<ParticleSystemRenderer>()?.sharedMaterial?.name+" texture="+ps.GetComponent<ParticleSystemRenderer>()?.sharedMaterial?.mainTexture?.name);
  foreach(var d in game.powerupMenuPanel.GetComponentsInChildren<PowerupDescription>(true))foreach(var tr in d.GetComponentsInChildren<RectTransform>(true))Log("upgrade-layout",tr.name+" active="+tr.gameObject.activeInHierarchy+" position="+tr.position+" size="+tr.rect+" scale="+tr.lossyScale);
  yield return Capture("upgrade-before.png");SetPhase("PASS");Log("complete","Read-only native presentation diagnosis; no acceptance claim");Application.Quit(0);
 }
}
