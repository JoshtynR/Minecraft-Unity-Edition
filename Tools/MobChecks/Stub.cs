using System;
using System.Collections.Generic;
using UnityEngine;
namespace Mirror {
 public interface NetworkMessage{}
 public class NetworkConnection{}
 public static class NetworkServer {public static bool active;public static void SendToAll<T>(T value){}}
 public static class NetworkTime {public static double time;}
}
namespace UnityEngine {
 public class HeaderAttribute:Attribute {public HeaderAttribute(string x){}}
 public class RangeAttribute:Attribute {public RangeAttribute(int x,int y){}}
 public class Object {public static void Destroy(Object o){}}
 public class MonoBehaviour:Object {public Transform transform=new Transform();}
 public class Material:Object{}
 public class Collider:Object{}
 public class MeshRenderer {public Material sharedMaterial;}
 public enum PrimitiveType{Cube}
 public class GameObject:Object {public string name;public Transform transform=new Transform();public static GameObject CreatePrimitive(PrimitiveType p)=>new GameObject();public T GetComponent<T>()where T:new()=>new T();}
 public class Transform {public Vector3 position,localScale;public Quaternion rotation;public void SetParent(Transform p,bool keep){}}
 public struct Quaternion {public static Quaternion LookRotation(Vector3 v)=>new Quaternion();}
 public static class Resources {public static T Load<T>(string path)where T:new()=>new T();}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);
  public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude<.000001f?zero:this/magnitude;
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 operator/(Vector3 a,float b)=>new Vector3(a.x/b,a.y/b,a.z/b);
 }
 public struct Vector3Int {
  public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}
  public static Vector3Int FloorToInt(Vector3 v)=>new Vector3Int((int)Math.Floor(v.x),(int)Math.Floor(v.y),(int)Math.Floor(v.z));
  public static implicit operator Vector3(Vector3Int p)=>new Vector3(p.x,p.y,p.z);
  public static Vector3Int operator+(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
 }
 public struct Ray {public Vector3 origin,direction;public Ray(Vector3 o,Vector3 d){origin=o;direction=d;}}
 public struct Bounds {
  public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}
  public bool Intersects(Bounds b)=>Math.Abs(center.x-b.center.x)<(size.x+b.size.x)/2&&Math.Abs(center.y-b.center.y)<(size.y+b.size.y)/2&&Math.Abs(center.z-b.center.z)<(size.z+b.size.z)/2;
  public bool IntersectRay(Ray ray,out float distance){float lo=0,hi=float.MaxValue;float[] p={ray.origin.x,ray.origin.y,ray.origin.z},d={ray.direction.x,ray.direction.y,ray.direction.z},c={center.x,center.y,center.z},s={size.x,size.y,size.z};for(int i=0;i<3;i++){if(Math.Abs(d[i])<.000001){if(p[i]<c[i]-s[i]/2||p[i]>c[i]+s[i]/2){distance=0;return false;}continue;}float a=(c[i]-s[i]/2-p[i])/d[i],b=(c[i]+s[i]/2-p[i])/d[i];lo=Math.Max(lo,Math.Min(a,b));hi=Math.Min(hi,Math.Max(a,b));}distance=lo;return hi>=lo;}
 }
 public static class Mathf {
  public const float Rad2Deg=57.29578f;public static float Atan2(float a,float b)=>(float)Math.Atan2(a,b);public static float Sqrt(float a)=>(float)Math.Sqrt(a);
  public static int FloorToInt(float v)=>(int)Math.Floor(v);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static int Max(int a,int b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);
 }
}
public enum WorldDimension {Overworld,Nether}
public enum SurvivalDifficulty {Easy,Normal,Hard}
public enum BlockType {Nothing,Air,Grass,Dirt,Stone,Bedrock}
public class Block {public BlockType type;public int sky,light;public int GetSkyLight()=>sky;public int GetBlockLight()=>light;}
public struct InventoryStack {public ItemType type;public int count,damage;public bool Empty=>count<=0;public InventoryStack(ItemType t,int c,int d=0){type=t;count=c;damage=d;}public void Normalize(){if(count<=0)type=ItemType.Nothing;}}
public static class InventoryRules {public static int Capacity(InventoryStack[] a,ItemType t,int x,int y)=>1;public static void Insert(InventoryStack[] a,ItemType t,int n,int x,int y){}}
public static class BetaItemCatalog {public struct Entry {public int SourceId,Metadata;}public static bool TryGet(ItemType t,out Entry e){e=new Entry();return false;}}
public class JavaRandom {private Random random=new Random(173);public int NextInt(int n)=>random.Next(n);public float NextFloat()=>(float)random.NextDouble();}
public static class GameAudio {public static int sounds;public static void Broadcast(string s,Vector3 p,float v,float pitch,World w){sounds++;}}
public static class BlockCollision {public static bool Supported(World w,Vector3 p,float width,float height)=>true;}
public static class BlockTargeting {public static Block Raycast(World w,Vector3 p,Vector3 d,float distance,out Vector3Int cell,out Vector3 point){cell=new Vector3Int();point=p+d*distance;return w.blocked?new Block{type=BlockType.Stone}:null;}}
public partial class Player:MonoBehaviour {
 public int health=20;public bool networkStarted,isServer,isLocalPlayer,isFlying,dimensionTransfer,serverFlying;public double hurtUntil;public float exhaustion;public bool DimensionTransferInProgress=>dimensionTransfer;public World CurrentWorld;public Mirror.NetworkConnection connectionToClient=new Mirror.NetworkConnection();public int pushes;
 public void RpcAddForce(Mirror.NetworkConnection c,Vector3 a,float b){pushes++;}public void AddForce(Vector3 a,float b){pushes++;}
}
public partial class World:MonoBehaviour {
 public WorldDimension dimension;public SurvivalDifficulty survivalDifficulty=SurvivalDifficulty.Normal;
 private List<Player> cowPlayers=new List<Player>();private List<CowState> activeCows=new List<CowState>();private List<Vector3Int> cowSpawnCandidates=new List<Vector3Int>();public CowStore cows=new CowStore();private JavaRandom cowRandom=new JavaRandom();public int betaSkylightSubtracted=11;public bool blocked;public int edits,drops;public RendererRoot worldRenderer=new RendererRoot();
 private sealed class Data {public Dictionary<Vector3Int,int> chunkDict=new Dictionary<Vector3Int,int>();}private Data worldData=new Data();
 public Block GetBlock(Vector3 p)=>new Block{type=BlockType.Air,sky=15};public Block GetBlock(Vector3Int p)=>GetBlock((Vector3)p);
 private bool CowAreaLoaded(Vector3 p)=>true;private float CowPlayerDistance(Vector3 p){float d=float.MaxValue;foreach(var player in cowPlayers)d=Math.Min(d,(player.transform.position-p).sqrMagnitude);return d;}
 private void BroadcastCow(CowState c,bool removed=false){}private void SetFarmBlock(Vector3Int p,BlockType b){edits++;}private static float BedBlastResistance(BlockType b)=>3;
 private bool SpawnItemDrop(InventoryStack stack,Vector3 p){drops+=stack.count;return true;}
 private bool DamageCow(CowState cow,int amount,Vector3 away)=>CowRules.Hurt(cow,amount,away);
 private bool CanReplaceChest(Vector3Int p,BlockType type)=>true;private InventoryStack MiningItemDrop(BlockType type,ItemType tool)=>new InventoryStack();
 public void Add(Player p){p.CurrentWorld=this;cowPlayers.Add(p);}public bool Step(CowState c)=>TickHostile(c);public void Arrows()=>TickMobArrows();public int ArrowCount=>mobArrows.Count;public int VisualCount=>mobArrowVisuals.Count;public void Reset()=>ResetMobArrows();
}
public class RendererRoot {public Transform transform=new Transform();}
