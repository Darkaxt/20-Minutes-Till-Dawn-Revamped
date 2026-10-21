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

// Android BossSpawner.CreateArena passes the exact spawned boss Health to
// LifeBinding. Keep native Windows arena creation/position/60-second expiry,
// adding only that missing ownership relationship at its creation point.
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
  var attach=AccessTools.Method(typeof(BossArenaOwnership),nameof(Attach));
  var code=instructions.ToList();int bound=0;
  for(int i=0;i<code.Count;i++)
  {
   yield return code[i];
   if(i>0&&code[i-1].LoadsField(prefab)&&code[i].Calls(instantiate))
   {
    yield return new CodeInstruction(OpCodes.Ldloc_2);
    yield return new CodeInstruction(OpCodes.Call,attach);bound++;
   }
  }
  if(bound!=1)throw new InvalidOperationException("Unsupported boss spawn coroutine: expected one native arena creation.");
 }
 static GameObject Attach(GameObject arena,Health boss)
 {
  if(boss!=null)arena.AddComponent<BossArenaLifetime>().Bind(boss);
  return arena;
 }
}
public sealed class BossArenaLifetime:MonoBehaviour
{
 public Health owner;
 public void Bind(Health boss)
 {
  owner=boss;owner.onDeath.AddListener(ClearArena);
  if(owner.isDead)ClearArena();
 }
 void ClearArena()=>Object.Destroy(gameObject);
 void OnDestroy(){if(owner!=null)owner.onDeath.RemoveListener(ClearArena);}
}
