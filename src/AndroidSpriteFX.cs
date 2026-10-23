using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

internal static class AndroidSpriteFX
{
 static readonly FieldInfo Sprites=AccessTools.Field(typeof(SpriteFX),"sprites"),Coroutine=AccessTools.Field(typeof(SpriteFX),"_coroutine");
 internal static readonly MethodInfo Play=AccessTools.Method(typeof(SpriteFX),"Play");
 internal static void SetSprites(SpriteFX owner,Sprite[] sprites)
 {
  Sprites.SetValue(owner,sprites);
  var routine=(IEnumerator)Coroutine.GetValue(owner);
  if(routine!=null)owner.StopCoroutine(routine);
 }
}

// Source starts only a nonempty sequence, stopping the previous iterator.
// The native frame/wait/loop coroutine has the same reached sequence body.
[HarmonyPatch(typeof(SpriteFX),"OnEnable")]
class AndroidSpriteFXEnable
{
 static bool Prefix(SpriteFX __instance,Sprite[] ___sprites,ref IEnumerator ____coroutine)
 {
  if(___sprites==null||___sprites.Length==0)return false;
  if(____coroutine!=null)__instance.StopCoroutine(____coroutine);
  ____coroutine=(IEnumerator)AndroidSpriteFX.Play.Invoke(__instance,null);
  __instance.StartCoroutine(____coroutine);return false;
 }
}
[HarmonyPatch(typeof(SpriteFX),"OnDisable")]
class AndroidSpriteFXDisable
{
 static bool Prefix(SpriteFX __instance,IEnumerator ____coroutine)
 {if(____coroutine!=null)__instance.StopCoroutine(____coroutine);return false;}
}
