using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using flanne;
using Object=UnityEngine.Object;
namespace TwentyMinutes.Modernization;

// Android's registered arena is acquired before its boss owner and position
// are assigned. LifeBinding polls that owner; it does not subscribe to death.
[HarmonyPatch]
class BossArenaOwnership
{
 static MethodBase TargetMethod()=>AccessTools.EnumeratorMoveNext(AccessTools.Method(typeof(BossSpawner),"WaitToSpawnCR"));
 static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions,MethodBase __originalMethod)
 {
  var locals=__originalMethod.GetMethodBody().LocalVariables;
  if(locals.Count<3||locals[2].LocalType!=typeof(Health))throw new InvalidOperationException("Unsupported boss spawn coroutine: expected native boss Health local2.");
  var instantiate=typeof(Object).GetMethods().Single(m=>m.Name=="Instantiate"&&m.IsGenericMethodDefinition&&m.GetParameters().Length==1).MakeGenericMethod(typeof(GameObject));
  var prefab=AccessTools.Field(typeof(BossSpawner),"arenaMonsterPrefab");
  var acquire=AccessTools.Method(typeof(BossArenaLifetime),nameof(BossArenaLifetime.Acquire));
  var player=AccessTools.Field(typeof(BossSpawner),"playerTransform");
  var code=instructions.ToList();int bound=0;
  for(int i=0;i<code.Count;i++)
  {
   if(i>0&&code[i-1].LoadsField(prefab)&&code[i].Calls(instantiate))
   {
    if(i+8>=code.Count||code[i+1].opcode!=OpCodes.Dup||!code[i+2].Calls(AccessTools.PropertyGetter(typeof(GameObject),nameof(GameObject.transform)))||code[i+3].opcode!=OpCodes.Ldloc_1||!code[i+4].LoadsField(player)||!code[i+5].Calls(AccessTools.PropertyGetter(typeof(Transform),nameof(Transform.position)))||!code[i+6].Calls(AccessTools.PropertySetter(typeof(Transform),nameof(Transform.position)))||code[i+7].opcode!=OpCodes.Ldc_I4_1||!code[i+8].Calls(AccessTools.Method(typeof(GameObject),nameof(GameObject.SetActive))))
     throw new InvalidOperationException("Unsupported boss spawn coroutine: expected native arena position/activation sequence.");
    var owner=new CodeInstruction(code[i]);owner.opcode=OpCodes.Ldloc_2;owner.operand=null;yield return owner;
    yield return new CodeInstruction(OpCodes.Call,acquire);
    // Acquisition already activates. Keep the native position assignment,
    // without its extra retained object and second SetActive invocation.
    foreach(int index in new[]{i+1,i+7,i+8}){code[index].opcode=OpCodes.Nop;code[index].operand=null;}
    bound++;
   }
   else yield return code[i];
  }
  if(bound!=1)throw new InvalidOperationException("Unsupported boss spawn coroutine: expected one native arena creation.");
 }
}
public sealed class BossArenaLifetime:MonoBehaviour
{
 public Health owner;
 internal static void ConfigurePrefab(GameObject prefab)
 {
  if(prefab.name=="ArenaMonster"&&prefab.GetComponent<BossArenaLifetime>()==null)prefab.AddComponent<BossArenaLifetime>();
 }
 internal static GameObject Acquire(GameObject prefab,Health boss)
 {
  var arena=ObjectPooler.SharedInstance.GetPooledObject(prefab.name);
  arena.GetComponent<BossArenaLifetime>().owner=boss;
  return arena;
 }
 void Update(){if(owner!=null&&owner.isDead)AndroidPools.Return(gameObject);}
}
