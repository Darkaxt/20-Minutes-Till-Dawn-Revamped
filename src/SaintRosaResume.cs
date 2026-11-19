using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

[Serializable]public sealed class RosaRunState
{
 public int progress,shells;public float cooldown,buffRemaining,aimX,aimY;
 public List<RosaVolley> volleys=new List<RosaVolley>();
 public List<RosaShellState> projectiles=new List<RosaShellState>();
 public List<RosaBurstState> bursts=new List<RosaBurstState>();
}
[Serializable]public sealed class RosaShellState
{
 public int volley,bounce,piercing,burn;public bool lensChanged,bulletEffect;
 public float x,y,z,vx,vy,angle,damage,size,knockback,remaining;
}
[Serializable]public sealed class RosaBurstState
{public string field;public float x,y,z,radius,elapsed;}
internal static class SaintRosaResume
{
 internal static RosaRunState Capture(PlayerController player)
 {
  var skill=player.GetComponentInChildren<SaintRosaSkill>();if(skill==null)return null;
  var saved=new RosaRunState{progress=skill.ShotProgress,shells=skill.ShellCount,cooldown=PortedCharacterSkill.Timer(skill),buffRemaining=skill.BuffRemaining,aimX=skill.LastAim.x,aimY=skill.LastAim.y};
  var contexts=new Dictionary<RosaVolley,int>();
  foreach(var shell in UnityEngine.Object.FindObjectsOfType<RosaShell>().Where(s=>s.Owner==skill&&s.Volley!=null))
  {
   if(!contexts.TryGetValue(shell.Volley,out int index)){index=saved.volleys.Count;contexts.Add(shell.Volley,index);saved.volleys.Add(shell.Volley);}
   var shot=shell.GetComponent<Projectile>();var actor=shell.GetComponent<AndroidProjectile>();var point=shell.transform.position;
   saved.projectiles.Add(new RosaShellState{volley=index,bounce=shot.bounce,piercing=shot.piercing,burn=actor.BurnDamage,lensChanged=actor.LensChanged,bulletEffect=actor.BulletEffect,
    x=point.x,y=point.y,z=point.z,vx=shot.vector.x,vy=shot.vector.y,angle=shell.transform.eulerAngles.z,damage=shot.damage,size=actor.Size,
    knockback=shell.GetComponent<Knockback>().knockbackForce,remaining=Mathf.Max(0,shell.Remaining)});
  }
  foreach(var burst in UnityEngine.Object.FindObjectsOfType<RosaBurst>())
  {
   var point=burst.transform.position;float radius=burst.transform.localScale.x*SaintRosaAssets.Sprite(burst.Contract.frames[0]).bounds.size.x*.5f;
   saved.bursts.Add(new RosaBurstState{field=burst.Field,x=point.x,y=point.y,z=point.z,radius=radius,elapsed=burst.Elapsed});
  }
  return saved;
 }
 static bool Finite(params float[] values)=>values.All(v=>!float.IsNaN(v)&&!float.IsInfinity(v));
 internal static void Validate(RosaRunState saved)
 {
  if(saved==null)return;
  if(saved.progress<0||saved.progress>=6||saved.shells<0||saved.shells>11||saved.buffRemaining<0||saved.buffRemaining>3||!Finite(saved.cooldown,saved.buffRemaining,saved.aimX,saved.aimY)||saved.volleys==null||saved.projectiles==null||saved.bursts==null)
   throw new InvalidOperationException("Invalid Saint Rosa checkpoint; the save has been preserved.");
  if(saved.volleys.Any(v=>v==null||!Finite(v.bloomDamage,v.bloomRadius,v.splashDamage,v.splashRadius)||v.bloomRadius<0||v.splashRadius<0)||
   saved.projectiles.Any(s=>s==null||s.volley<0||s.volley>=saved.volleys.Count||s.remaining<0||!Finite(s.x,s.y,s.z,s.vx,s.vy,s.angle,s.damage,s.size,s.knockback,s.remaining))||
   saved.bursts.Any(b=>b==null||!(new[]{"petalBurstPrefab","bloomShockPrefab","smallBloomShockPrefab"}).Contains(b.field)||b.elapsed<0||b.elapsed>=.350001f||b.radius<0||!Finite(b.x,b.y,b.z,b.radius,b.elapsed)))
   throw new InvalidOperationException("Invalid Saint Rosa actor checkpoint; the save has been preserved.");
 }
 internal static void Restore(PlayerController player,RosaRunState saved)
 {
  var skill=player.GetComponentInChildren<SaintRosaSkill>();if(skill==null){if(saved!=null)throw new InvalidOperationException("Saint Rosa state has no active character owner.");return;}
  if(saved==null)return;
  Validate(saved);skill.RestoreCounters(saved);
  foreach(var state in saved.projectiles)
  {
   var recipe=new AndroidProjectileRecipe{objectPoolTag=SaintRosaSkill.ShellTag,damage=state.damage,size=state.size,knockback=state.knockback,bounce=state.bounce,piercing=state.piercing,projectileSpeed=new Vector2(state.vx,state.vy).magnitude,owner=player.gameObject,BulletEffect=state.bulletEffect,BurnDamage=state.burn};
   var shot=ProjectileFactory.SharedInstance.SpawnProjectile(recipe,new Vector2(state.vx,state.vy),new Vector3(state.x,state.y,state.z),1,true);
   shot.angle=state.angle;shot.GetComponent<AndroidProjectile>().LensChanged=state.lensChanged;
   var shell=shot.GetComponent<RosaShell>();shell.Configure(skill,saved.volleys[state.volley]);shell.Remaining=state.remaining;
   var lifetime=shot.GetComponent<TimeToLive>();lifetime.CancelInvoke();lifetime.Invoke("Deactivate",state.remaining);
  }
  foreach(var state in saved.bursts)
  {
   string tag=state.field=="petalBurstPrefab"?SaintRosaSkill.PetalTag:state.field=="bloomShockPrefab"?SaintRosaSkill.BloomTag:SaintRosaSkill.SmallBloomTag;
   var actor=ObjectPooler.SharedInstance.GetComponent<AndroidPools>().Get(tag,new Vector3(state.x,state.y,state.z));
   var burst=actor.GetComponent<RosaBurst>();burst.Play(state.radius);burst.Elapsed=state.elapsed;burst.Draw();
  }
 }
}
