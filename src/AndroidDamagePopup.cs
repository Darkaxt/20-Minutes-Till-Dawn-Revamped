using HarmonyLib;
using TMPro;
using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source PopupText.Pop initializes after placement. The native numeric popup's
// lifetime, MoveTowards/scale Update and pooled release already match source.
public sealed class AndroidDamagePopup:MonoBehaviour
{
 static readonly AccessTools.FieldRef<WorldTextPopup,float> Timer=AccessTools.FieldRefAccess<WorldTextPopup,float>("timer");
 static readonly AccessTools.FieldRef<WorldTextPopup,Vector3> Next=AccessTools.FieldRefAccess<WorldTextPopup,Vector3>("newPos");
 internal static void ConfigurePrefab(GameObject prefab)
 {
  if(prefab.name=="PF_DamagePoppup"&&prefab.GetComponent<AndroidDamagePopup>()==null)prefab.AddComponent<AndroidDamagePopup>();
 }
 internal void Init()
 {
  var native=GetComponent<WorldTextPopup>();
  Timer(native)=0;
  var origin=transform.position;
  origin.x+=Random.Range(-.5f,.5f);
  origin.y+=Random.Range(-.5f,.5f);
  transform.position=origin;
  Next(native)=new Vector3(origin.x+Random.Range(-.5f,.5f),origin.y+.5f,0);
  transform.localScale=Vector3.one;
 }
 internal static void PopDamage(int amount,Transform target)
 {
  if(PlayerPrefs.GetInt("ShowDamagePopup",1)==1)Pop(amount.ToString(),target);
 }
 internal static void Pop(string text,Transform target)
 {
  var value=ObjectPooler.SharedInstance.GetPooledObject("DamagePopup");
  if(value==null)return;
  value.transform.position=target.position;
  value.GetComponent<AndroidDamagePopup>().Init();
  value.GetComponent<TextMeshPro>().text=text;
 }
}
[HarmonyPatch(typeof(WorldTextPopup),"OnEnable")]
class SourceDamagePopupExplicitInit
{static bool Prefix(WorldTextPopup __instance)=>__instance.GetComponent<AndroidDamagePopup>()==null;}
