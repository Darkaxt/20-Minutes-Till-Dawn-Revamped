// Generated from original Android/Windows Forest hierarchy records.
// Input SHA256: 6a0d93390a0060e57c28ff2c38aca8333a4ba45eb6b4e80a945b542f0e8d1368
using System.Collections.Generic;
using UnityEngine;
namespace TwentyMinutes.Modernization;
internal static class AndroidForestTables
{
 internal sealed class Pose
 {
  internal string Name;internal Vector3 Position,Scale;internal Quaternion Rotation;
  internal Pose(string name,Vector3 position,Vector3 scale,Quaternion rotation){Name=name;Position=position;Scale=scale;Rotation=rotation;}
 }
 internal static readonly Dictionary<string,Pose> Poses=new Dictionary<string,Pose>{
  {"PF_TreeMonster",new Pose("PF_TreeMonster (1)",new Vector3(-11.38624f,-7.47265625f,0f),new Vector3(1f,1f,0f),new Quaternion(-0f,-0f,-0f,1f))},
  {"PF_TreeMonster (1)",new Pose("PF_TreeMonster (15)",new Vector3(-0.819999993f,-10.1599998f,0f),new Vector3(1f,1f,0f),new Quaternion(0f,0f,0f,1f))},
  {"PF_TreeMonster (12)",new Pose("PF_TreeMonster (12)",new Vector3(12.0642376f,-5.70280075f,0f),new Vector3(1f,1f,0f),new Quaternion(-0f,-0f,-0f,1f))},
  {"PF_TreeMonster (14)",new Pose("PF_TreeMonster (8)",new Vector3(13.6000004f,7.63000011f,0f),new Vector3(1f,1f,0f),new Quaternion(-0f,-0f,-0f,1f))},
  {"PF_TreeMonster (15)",new Pose("PF_TreeMonster (14)",new Vector3(8.39999962f,13.1999998f,0f),new Vector3(1f,1f,0f),new Quaternion(0f,0f,0f,1f))},
  {"PF_TreeMonster (17)",new Pose("PF_TreeMonster",new Vector3(-8.21000004f,10.71f,0f),new Vector3(1f,1f,0f),new Quaternion(-0f,-0f,-0f,1f))},
  {"PF_TreeMonster (4)",new Pose("PF_TreeMonster (4)",new Vector3(-2.91000009f,0.689999998f,0f),new Vector3(1f,1f,0f),new Quaternion(-0f,-0f,-0f,1f))},
  {"PF_TreeMonster (8)",new Pose("PF_TreeMonster",new Vector3(8.09000015f,0.810000002f,0f),new Vector3(1f,1f,0f),new Quaternion(-0f,-0f,-0f,1f))},
  {"PF_TreeMonster (9)",new Pose("PF_TreeMonster (9)",new Vector3(4.61610413f,-13.0034218f,0f),new Vector3(1f,1f,0f),new Quaternion(-0f,-0f,-0f,1f))},
 };
}
