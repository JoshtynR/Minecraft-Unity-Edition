// Shared math doubles for mob and inventory regression checks; not a Unity runtime substitute.
using System;
namespace UnityEngine {
 [Serializable] public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 down=>new Vector3(0,-1,0);
  public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude<.000001f?zero:this/magnitude;
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 operator/(Vector3 a,float b)=>new Vector3(a.x/b,a.y/b,a.z/b);
  public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;public void Normalize(){this=normalized;}
 }
 [Serializable] public struct Vector3Int {
  public static Vector3Int zero=>new Vector3Int();public static Vector3Int up=>new Vector3Int(0,1,0);public static Vector3Int down=>new Vector3Int(0,-1,0);
  public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}
  public static Vector3Int FloorToInt(Vector3 v)=>new Vector3Int((int)Math.Floor(v.x),(int)Math.Floor(v.y),(int)Math.Floor(v.z));
  public static implicit operator Vector3(Vector3Int p)=>new Vector3(p.x,p.y,p.z);
  public static Vector3Int operator+(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3Int operator-(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x-b.x,a.y-b.y,a.z-b.z);public int sqrMagnitude=>x*x+y*y+z*z;
  public static bool operator==(Vector3Int a,Vector3Int b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;public static bool operator!=(Vector3Int a,Vector3Int b)=>!(a==b);public override bool Equals(object value)=>value is Vector3Int&&(Vector3Int)value==this;public override int GetHashCode()=>x*397^y*31^z;
 }
}
