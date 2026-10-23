using UnityEngine;
using flanne;
namespace TwentyMinutes.Modernization;

// Source Generator observes every amount-change notification. Its paired
// BuffPlayerStats owns the actual source stat recipe and stack counter.
public sealed class AndroidGenerator:MonoBehaviour
{
 public bool Applied;
 public float AppliedAt;
 // Count actual recipe contributions separately from the source's monotonically
 // accumulated saved counter. Only the checkpoint bridge consumes this value.
 public int Contributions;
 BuffPlayerStats buff;
 void Start()
 {buff=GetComponent<BuffPlayerStats>();PlayerController.Instance.ammo.OnAmmoChanged.AddListener(Apply);}
 void Apply(int amount)
 {
  if(Applied)Remove();Applied=true;AppliedAt=GameTimer.SharedInstance.timer;buff.ApplyBuff();
 }
 void Remove(){Applied=false;buff.RemoveBuff();}
 void Update(){if(Applied&&GameTimer.SharedInstance.timer>AppliedAt+2)Remove();}
 // Source has no OnDestroy unsubscription or extra removal here.
}
