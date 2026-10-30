using System;
using System.Linq;
using System.IO;
using System.Collections;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using flanne;
using flanne.Core;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization.Validation;
public partial class RuntimeChecks
{
 float OpacityGpu(SpriteRenderer renderer)
 {
  int layer=renderer.gameObject.layer;renderer.gameObject.layer=30;
  var cameraObject=new GameObject("OpacityShaderProbe",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=Mathf.Max(renderer.bounds.extents.x,renderer.bounds.extents.y)*1.2f;camera.transform.position=renderer.bounds.center-Vector3.forward*5;camera.nearClipPlane=.1f;camera.farClipPlane=10;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
  var target=RenderTexture.GetTemporary(64,64,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.Linear);var previous=RenderTexture.active;var pixels=new Texture2D(64,64,TextureFormat.RGBA32,false,true);
  try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,64,64),0,0);pixels.Apply();return pixels.GetPixels().Sum(c=>c.r+c.g+c.b);}
  finally{renderer.gameObject.layer=layer;camera.targetTexture=null;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);Object.Destroy(pixels);Object.Destroy(cameraObject);}
 }
 IEnumerator CheckPlayerOpacity()
 {
  RunResume.Invalidate();var title=Progression.Title;var pad=InputSystem.AddDevice<Gamepad>("PlayerOpacityController");InputSystem.QueueStateEvent(fixtureMouse,new MouseState{position=new Vector2(-100,-100)});yield return null;
  while(title.mainMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  var options=Entries(title.mainMenu).Single(e=>e.button.GetComponentInChildren<TMPro.TMP_Text>().text=="Options").button;
  yield return PadClick(pad,title.mainMenu.transform,options);while(!Active(title.optionsMenu)||title.optionsMenu.GetComponent<CanvasGroup>().alpha<.999f)yield return null;
  var projectiles=title.optionsMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="ProjectileOpacitySetting");var effects=title.optionsMenu.GetComponentsInChildren<Button>().Single(b=>b.name=="EffectsOpacitySetting");
  Require((int)Plugin.Instance.ProjectileOpacity.DefaultValue==100&&(int)Plugin.Instance.EffectsOpacity.DefaultValue==100,"new opacity entries default to unchanged native100%");
  Plugin.Instance.ProjectileOpacity.Value=Plugin.Instance.EffectsOpacity.Value=100;Log("fixture-precondition","Reset only isolated QA opacity choices before the native four-level interaction sweep.");
  for(int i=0;i<4;i++)
  {
   int before=Plugin.Instance.ProjectileOpacity.Value,other=Plugin.Instance.EffectsOpacity.Value;yield return PadClick(pad,title.optionsMenu.transform,projectiles);
   Require(Plugin.Instance.ProjectileOpacity.Value==before%100+25&&Plugin.Instance.EffectsOpacity.Value==other,"controller cycles projectile opacity independently: "+Plugin.Instance.ProjectileOpacity.Value);
   before=Plugin.Instance.EffectsOpacity.Value;other=Plugin.Instance.ProjectileOpacity.Value;yield return PadClick(pad,title.optionsMenu.transform,effects);
   Require(Plugin.Instance.EffectsOpacity.Value==before%100+25&&Plugin.Instance.ProjectileOpacity.Value==other,"controller cycles effect opacity independently: "+Plugin.Instance.EffectsOpacity.Value);
  }
  yield return MenuPointer(projectiles);yield return MenuPointer(effects);yield return PadClick(pad,title.optionsMenu.transform,projectiles);
  Require(Plugin.Instance.ProjectileOpacity.Value==50&&Plugin.Instance.EffectsOpacity.Value==25,"native entries select projectile50% and effects25% separately");
  yield return HoneyArrow(pad,title.optionsMenu.transform,effects);yield return Capture("opacity-native-options.png");yield return SelectorPad(pad,GamepadButton.East);while(!Active(title.mainMenu))yield return null;
  yield return AchievementRun("Shana","RevolverData","20M_Forest",prepareRun:()=>Loadout.RuneSelection=new System.Collections.Generic.List<flanne.RuneSystem.RuneData>());
  var game=Object.FindObjectOfType<GameController>();StopFixtureSpawning();GameTimer.SharedInstance.Stop();var player=PlayerController.Instance;
  yield return AndroidPick(game,"FireStarter",game.playerXP.level);yield return AndroidPick(game,"MagicLens",game.playerXP.level);MoveLensesAway();
  var recipe=ProjectileRecipe(damage:40,speed:0,pool:"PF_Fireball");var shot=ProjectileFactory.SharedInstance.SpawnProjectile(recipe,Vector2.right,player.transform.position+Vector3.right*2);var sprite=shot.GetComponent<SpriteRenderer>();if(sprite==null)sprite=shot.GetComponentInChildren<SpriteRenderer>();
  foreach(var effect in shot.GetComponentsInChildren<Behaviour>().Where(b=>b.GetType().Name=="SpriteFX"))effect.enabled=false;
  yield return null;var opacity=sprite.GetComponent<PlayerVisualOpacity>();Require(opacity!=null&&opacity.Kind==PlayerVisualOpacity.Channel.Projectiles&&opacity.AppliedOpacity==.5f,"actual Fireball body uses projectile channel");
  var lens=player.GetComponentsInChildren<BulletEnhanceSummon>().Single();yield return null;var lensOpacity=lens.GetComponentInChildren<PlayerVisualOpacity>();Require(lensOpacity!=null&&lensOpacity.Kind==PlayerVisualOpacity.Channel.Effects&&lensOpacity.AppliedOpacity==.25f,"actual summon body uses the separate effects channel");
  float damage=shot.damage;var scale=shot.transform.localScale;float radius=shot.GetComponent<CircleCollider2D>().radius;var material=sprite.sharedMaterial;var color=sprite.color;
  Plugin.Instance.ProjectileOpacity.Value=100;AccessTools.Method(typeof(PlayerVisualOpacity),"Apply").Invoke(opacity,null);float full=OpacityGpu(sprite);
  Plugin.Instance.ProjectileOpacity.Value=25;AccessTools.Method(typeof(PlayerVisualOpacity),"Apply").Invoke(opacity,null);float faded=OpacityGpu(sprite);
  Log("opacity-gpu","full="+full+" quarter="+faded+" ratio="+(faded/full)+" shader="+material.shader.name);
  Require(full>1&&faded/full>.15f&&faded/full<.4f,"native Fireball shader visibly fades to quarter intensity rather than merely storing a setting");
  Require(shot.damage==damage&&shot.transform.localScale==scale&&shot.GetComponent<CircleCollider2D>().radius==radius&&sprite.color==color&&sprite.sharedMaterial==material,"opacity leaves Fireball damage, dimensions, collider, native color animation and shared material unchanged");
  Plugin.Instance.ProjectileOpacity.Value=50;AccessTools.Method(typeof(PlayerVisualOpacity),"Apply").Invoke(opacity,null);
  var fog=game.playerFogRevealer.GetComponentsInChildren<Renderer>(true);Require(player.playerSprite.GetComponent<PlayerVisualOpacity>()==null&&fog.All(r=>r.GetComponent<PlayerVisualOpacity>()==null),"character body and all native fog/reveal renderers remain outside opacity ownership");
  foreach(string tag in new[]{"EyeMonsterProjectile","CultistProjectile","ExplosionFX"})for(int i=0;i<2;i++)
  {var hostile=ObjectPooler.SharedInstance.GetPooledObject(tag);Require(hostile.GetComponentsInChildren<PlayerVisualOpacity>(true).Length==0,"hostile visual remains untouched across pool reuse: "+tag);ReturnFixtureActor(hostile);}
  var effectRoot=new GameObject("NativeAbilityOpacityFixture",typeof(SpriteRenderer));effectRoot.transform.SetParent(player.transform,false);var ability=effectRoot.GetComponent<SpriteRenderer>();ability.sprite=player.playerSprite.sprite;ability.sharedMaterial=player.playerSprite.sharedMaterial;effectRoot.transform.localPosition=Vector3.up*2;
  yield return null;yield return new WaitForEndOfFrame();Require(ability.GetComponent<PlayerVisualOpacity>()?.AppliedOpacity==.25f,"new attached character ability visuals join the effects channel without fading the character");
  var trail=shot.GetComponentsInChildren<TrailRenderer>(true).FirstOrDefault();var particles=shot.GetComponentsInChildren<ParticleSystemRenderer>(true).FirstOrDefault();
  if(trail!=null)Require(trail.GetComponent<PlayerVisualOpacity>()?.AppliedOpacity==.25f,"projectile trail uses effects opacity rather than multiplying both channels");
  if(particles!=null)Require(particles.GetComponent<PlayerVisualOpacity>()?.AppliedOpacity==.25f,"projectile particles use effects opacity independently of the body");
  yield return PoiCapture("opacity-fireball-and-ability.png");Object.Destroy(effectRoot);ReturnFixtureActor(shot.gameObject);
  var reused=ProjectileFactory.SharedInstance.SpawnProjectile(recipe,Vector2.right,player.transform.position+Vector3.right*2);yield return null;
  Require(reused.GetComponentsInChildren<PlayerVisualOpacity>().Where(o=>o.Kind==PlayerVisualOpacity.Channel.Projectiles).All(o=>o.AppliedOpacity==.5f),"pooled projectile reuse retains the selected factor without exponential fading");ReturnFixtureActor(reused.gameObject);
  Plugin.Instance.Config.Save();string saved=File.ReadAllText(Plugin.Instance.Config.ConfigFilePath);Require(saved.Contains("PlayerProjectileOpacity = 50")&&saved.Contains("PlayerEffectsOpacity = 25"),"both independent choices are written to the real QA configuration");
  bool autoSave=Plugin.Instance.Config.SaveOnConfigSet;Plugin.Instance.Config.SaveOnConfigSet=false;
  try{Plugin.Instance.ProjectileOpacity.Value=100;Plugin.Instance.EffectsOpacity.Value=100;Plugin.Instance.Config.Reload();Require(Plugin.Instance.ProjectileOpacity.Value==50&&Plugin.Instance.EffectsOpacity.Value==25,"the real configuration reload restores both opacity choices");}
  finally{Plugin.Instance.Config.SaveOnConfigSet=autoSave;}
  yield return AchievementExit(game,false);SetPhase("PASS");Log("complete","Two native opacity entries, independent channels, real GPU alpha, summon/ability rendering, hostile exclusion, pooling and persisted configuration verified.");Application.Quit(0);
 }
}
