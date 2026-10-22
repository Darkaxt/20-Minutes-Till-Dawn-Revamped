using System.Collections.Generic;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source GameObjectPool.Get/Release uses a stack. Attach this only to a
// reconstructed effect owner; native enemy/projectile pools remain untouched.
public sealed class AndroidEffectPool:MonoBehaviour
{
 readonly Stack<GameObject> returned=new Stack<GameObject>();
 internal GameObject Acquire(ObjectPooler native,string tag)=>returned.Count==0?native.GetPooledObject(tag):returned.Pop();
 internal void Release(GameObject visual)=>returned.Push(visual);
}
public sealed class AndroidEffectPoolMember:MonoBehaviour
{
 internal AndroidEffectPool Owner;
 void OnDisable(){if(Owner!=null)Owner.Release(gameObject);}
}
