using System;
using System.Collections.Generic;
using UnityEngine;
namespace TwentyMinutes.Modernization;

// Integer lattice coordinates identify a corner shared by three hexagons.
// The six (x+y mod3, y mod3) classes occur exactly once in every cell.
public static class HoneycombLayout
{
 public const float Side=36;
 public struct Cell:IEquatable<Cell>
 {public int Q,R;public Cell(int q,int r){Q=q;R=r;}public bool Equals(Cell c)=>Q==c.Q&&R==c.R;}
 public struct Vertex
 {
  public int X,Y;public Vertex(int x,int y){X=x;Y=y;}
  public string Key=>X.ToString(System.Globalization.CultureInfo.InvariantCulture)+":"+Y.ToString(System.Globalization.CultureInfo.InvariantCulture);
  public int Slot=>Mod(X+Y,3)*2+(Mod(Y,3)==1?0:1);
 }
 static int Mod(int n,int divisor)=>(n%divisor+divisor)%divisor;
 public static IEnumerable<Vertex> Corners(Cell c)
 {foreach(var d in new[]{new Vector2Int(1,1),new Vector2Int(0,2),new Vector2Int(-1,1),new Vector2Int(-1,-1),new Vector2Int(0,-2),new Vector2Int(1,-1)})yield return new Vertex(2*c.Q+c.R+d.x,3*c.R+d.y);}
 public static Vector2 Position(Vertex v,Vector2 origin,float degrees)
 {var p=new Vector2(Mathf.Sqrt(3)*Side*.5f*v.X,Side*.5f*v.Y);float angle=degrees*Mathf.Deg2Rad;return origin+new Vector2(p.x*Mathf.Cos(angle)-p.y*Mathf.Sin(angle),p.x*Mathf.Sin(angle)+p.y*Mathf.Cos(angle));}
 public static Cell Locate(Vector2 world,Vector2 origin,float degrees)
 {
  var p=world-origin;float a=-degrees*Mathf.Deg2Rad,x=p.x*Mathf.Cos(a)-p.y*Mathf.Sin(a),y=p.x*Mathf.Sin(a)+p.y*Mathf.Cos(a);
  float q=(Mathf.Sqrt(3)/3*x-y/3)/Side,r=2*y/(3*Side),s=-q-r;int iq=Mathf.RoundToInt(q),ir=Mathf.RoundToInt(r),isv=Mathf.RoundToInt(s);
  if(Mathf.Abs(iq-q)>Mathf.Abs(ir-r)&&Mathf.Abs(iq-q)>Mathf.Abs(isv-s))iq=-ir-isv;else if(Mathf.Abs(ir-r)>Mathf.Abs(isv-s))ir=-iq-isv;
  return new Cell(iq,ir);
 }
 public static IEnumerable<Cell> Ring(Cell center,int radius)
 {for(int q=-radius;q<=radius;q++)for(int r=Math.Max(-radius,-q-radius);r<=Math.Min(radius,-q+radius);r++)if(Math.Max(Math.Abs(q),Math.Max(Math.Abs(r),Math.Abs(q+r)))==radius)yield return new Cell(center.Q+q,center.R+r);}
 public static uint Hash(uint seed,Vertex vertex)
 {unchecked{uint x=seed^(uint)vertex.X*0x9e3779b9u^(uint)vertex.Y*0x85ebca6bu;x^=x>>16;x*=0x7feb352du;x^=x>>15;x*=0x846ca68bu;return x^(x>>16);}}
 public static ExplorationReward Reward(uint seed,Vertex vertex)
 {
  var slots=new[]{ExplorationReward.Experience,ExplorationReward.Healing,ExplorationReward.Magnet,ExplorationReward.Freeze,ExplorationReward.Frenzy,ExplorationReward.Upgrade};
  uint random=seed;for(int i=slots.Length-1;i>0;i--){random=unchecked(random*1664525+1013904223);int j=(int)(random%(uint)(i+1));var old=slots[i];slots[i]=slots[j];slots[j]=old;}
  var reward=slots[vertex.Slot];return reward==ExplorationReward.Upgrade&&Hash(seed,vertex)%10==0?ExplorationReward.CharacterUpgrade:reward;
 }
}
