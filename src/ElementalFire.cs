using System.Collections.Generic;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

internal static class ElementalFire
{
 internal static float Radius(Projectile shot)
 {var r=shot.GetComponent<SpriteRenderer>();var size=r.sprite.bounds.extents;var scale=r.transform.lossyScale;return 2*Mathf.Max(size.x*Mathf.Abs(scale.x),size.y*Mathf.Abs(scale.y));}
 internal static void Explode(Projectile shot,GameObject primary,bool bulletEffect)
 {
  Vector2 center=shot.transform.position;float radius=Radius(shot);int full=(int)shot.damage;var ids=new List<int>();var seen=new HashSet<int>();var pool=ObjectPooler.SharedInstance.GetComponent<AndroidPools>();pool.GetMonsterInRange(ids,radius,center);ids.Insert(0,primary.GetInstanceID());
  ElementalRun.Instance.Pulse(center,radius,new Color(1,.5f,.25f));
  foreach(int id in ids)
  {
   var target=id==primary.GetInstanceID()?primary:pool.GetFromID(id);if(target==null||!target.activeInHierarchy||!seen.Add(id))continue;var health=target.GetComponent<Health>();if(health==null||health.isDead)continue;
   int damage=target==primary?full:Mathf.FloorToInt(full*AndroidLightning.Falloff(Vector2.Distance(center,target.transform.position),radius));if(AndroidAffixes.Has(target,3))damage/=2;
   if(damage>=health.HP)shot.PostNotification(Projectile.KillEvent,target);
   BurnSystem.SharedInstance.Burn(target,3);
   if(bulletEffect)PlayerController.Instance.gameObject.PostNotification(Projectile.ImpactEvent,target);
   AndroidDamageRounding.ChangeHealth(health,-damage,DamageType.Bullet);
  }
 }
}
