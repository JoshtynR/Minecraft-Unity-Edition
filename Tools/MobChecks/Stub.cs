using System;
using System.Collections.Generic;
using UnityEngine;
namespace Mirror {
 public interface NetworkMessage{}
 public class NetworkConnection{}
 public class CommandAttribute:Attribute{}
 public class NetworkConnectionToClient:NetworkConnection {public GameObject identity;public List<object> messages=new List<object>();public void Send<T>(T message){messages.Add(message);}}
 public static class NetworkServer {public static bool active;public static Dictionary<int,NetworkConnectionToClient> connections=new Dictionary<int,NetworkConnectionToClient>();public static void SendToAll<T>(T value){if(NetworkClient.active)NetworkClient.Deliver(value);}}
 public static class NetworkClient {public static bool active,ready=true;public static GameObject localPlayer;private static Dictionary<Type,Delegate> handlers=new Dictionary<Type,Delegate>();public static void RegisterHandler<T>(Action<T> action){handlers[typeof(T)]=action;}public static void Send<T>(T message){}public static void Deliver<T>(T message){if(handlers.TryGetValue(typeof(T),out var callback))((Action<T>)callback)(message);}}
 public static class NetworkTime {public static double time;}
}
namespace UnityEngine {
 public class HeaderAttribute:Attribute {public HeaderAttribute(string x){}}
 public class TooltipAttribute:Attribute {public TooltipAttribute(string x){}}
 public class RangeAttribute:Attribute {public RangeAttribute(int x,int y){}}
 public class Object {public string name;public bool destroyed;public static void Destroy(Object o){if(o!=null)o.destroyed=true;}}
 public class MonoBehaviour:Object {public Transform transform=new Transform();}
 public class Texture2D:Object {public int width=64,height=32;}
 public class Material:Object{public Texture2D mainTexture=new Texture2D();public Material(){}public Material(Material source){mainTexture=source.mainTexture;}}
 public class Collider:Object{}
 public class MeshRenderer {public static float lastHurt;public Material sharedMaterial;public Rendering.ShadowCastingMode shadowCastingMode;public bool receiveShadows;public void SetPropertyBlock(MaterialPropertyBlock p){lastHurt=p.hurt;}}
 public struct Vector4 {public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}}
 public class MaterialPropertyBlock {public float hurt;public void SetVector(string name,Vector4 value){}public void SetFloat(string name,float value){if(name=="_Hurt")hurt=value;}public void SetColor(string name,Color value){}}
 public class MeshFilter {public Mesh sharedMesh;}
 public class Mesh:Object {public static List<Mesh> all=new List<Mesh>();public Vector3[] vertices=new Vector3[0];public Vector2[] uv;public int[] triangles;public int vertexCount=>vertices.Length;public Mesh(){all.Add(this);}public void SetVertices(List<Vector3> v){vertices=v.ToArray();}public void SetTriangles(List<int> t,int sub){triangles=t.ToArray();}public void SetUVs(int channel,List<Vector2> uv){}public void SetUVs(int channel,Vector2[] uv){}public void RecalculateNormals(){}public void RecalculateBounds(){}}
 public enum PrimitiveType{Cube}
 public class GameObject:Object {public bool activeSelf=true;public Transform transform;private Dictionary<Type,object> components=new Dictionary<Type,object>();public GameObject(string name="",params Type[] types){this.name=name;transform=new Transform{gameObject=this};foreach(var t in types)components[t]=Activator.CreateInstance(t);}public static GameObject CreatePrimitive(PrimitiveType p)=>new GameObject();public T GetComponent<T>()where T:new(){if(!components.TryGetValue(typeof(T),out var result))components[typeof(T)]=result=new T();return (T)result;}public T AddComponent<T>()where T:new(){var value=new T();components[typeof(T)]=value;return value;}public void SetActive(bool value){activeSelf=value;}}
 public class Transform {public GameObject gameObject;public Vector3 position,localPosition,localScale=Vector3.one,eulerAngles;public Vector3 forward=>rotation*new Vector3(0,0,1);public Quaternion rotation=Quaternion.identity,localRotation=Quaternion.identity;public Transform parent;public void SetParent(Transform p,bool keep){parent=p;}}
 public struct Quaternion {
  public double x,y,z,w;public static Quaternion identity=>new Quaternion{w=1};public static Quaternion LookRotation(Vector3 v)=>identity;
  public static Quaternion Euler(float x,float y,float z){double k=Math.PI/360;return new Quaternion{y=Math.Sin(y*k),w=Math.Cos(y*k)}*new Quaternion{x=Math.Sin(x*k),w=Math.Cos(x*k)}*new Quaternion{z=Math.Sin(z*k),w=Math.Cos(z*k)};}
  public static Quaternion operator*(Quaternion a,Quaternion b)=>new Quaternion{x=a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y,y=a.w*b.y-a.x*b.z+a.y*b.w+a.z*b.x,z=a.w*b.z+a.x*b.y-a.y*b.x+a.z*b.w,w=a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z};
  public static Vector3 operator*(Quaternion q,Vector3 v){var r=q*new Quaternion{x=v.x,y=v.y,z=v.z}*new Quaternion{x=-q.x,y=-q.y,z=-q.z,w=q.w};return new Vector3((float)r.x,(float)r.y,(float)r.z);}
 }
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>(float)Math.Sqrt(x*x+y*y);}
 public static class Resources {public static T Load<T>(string path)where T:new()=>new T();}
 public struct Color {public float r,g,b,a;public static Color white=>new Color(1,1,1,1);public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 down=>new Vector3(0,-1,0);
  public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>(float)Math.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude<.000001f?zero:this/magnitude;
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
  public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);public static Vector3 operator/(Vector3 a,float b)=>new Vector3(a.x/b,a.y/b,a.z/b);
  public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*t;public void Normalize(){this=normalized;}
 }
 public struct Vector3Int {
  public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}
  public static Vector3Int FloorToInt(Vector3 v)=>new Vector3Int((int)Math.Floor(v.x),(int)Math.Floor(v.y),(int)Math.Floor(v.z));
  public static implicit operator Vector3(Vector3Int p)=>new Vector3(p.x,p.y,p.z);
  public static Vector3Int operator+(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3Int operator-(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x-b.x,a.y-b.y,a.z-b.z);public int sqrMagnitude=>x*x+y*y+z*z;
  public static bool operator==(Vector3Int a,Vector3Int b)=>a.x==b.x&&a.y==b.y&&a.z==b.z;public static bool operator!=(Vector3Int a,Vector3Int b)=>!(a==b);public override bool Equals(object value)=>value is Vector3Int&&(Vector3Int)value==this;public override int GetHashCode()=>x*397^y*31^z;
 }
 public struct Ray {public Vector3 origin,direction;public Ray(Vector3 o,Vector3 d){origin=o;direction=d;}}
 public struct Bounds {
  public Vector3 center,size;public Bounds(Vector3 c,Vector3 s){center=c;size=s;}
  public bool Intersects(Bounds b)=>Math.Abs(center.x-b.center.x)<(size.x+b.size.x)/2&&Math.Abs(center.y-b.center.y)<(size.y+b.size.y)/2&&Math.Abs(center.z-b.center.z)<(size.z+b.size.z)/2;
  public Vector3 ClosestPoint(Vector3 p)=>new Vector3(Mathf.Clamp(p.x,center.x-size.x*.5f,center.x+size.x*.5f),Mathf.Clamp(p.y,center.y-size.y*.5f,center.y+size.y*.5f),Mathf.Clamp(p.z,center.z-size.z*.5f,center.z+size.z*.5f));
  public bool IntersectRay(Ray ray,out float distance){float lo=0,hi=float.MaxValue;float[] p={ray.origin.x,ray.origin.y,ray.origin.z},d={ray.direction.x,ray.direction.y,ray.direction.z},c={center.x,center.y,center.z},s={size.x,size.y,size.z};for(int i=0;i<3;i++){if(Math.Abs(d[i])<.000001){if(p[i]<c[i]-s[i]/2||p[i]>c[i]+s[i]/2){distance=0;return false;}continue;}float a=(c[i]-s[i]/2-p[i])/d[i],b=(c[i]+s[i]/2-p[i])/d[i];lo=Math.Max(lo,Math.Min(a,b));hi=Math.Min(hi,Math.Max(a,b));}distance=lo;return hi>=lo;}
 }
 public static class Mathf {
  public const float Rad2Deg=57.29578f,PI=(float)Math.PI;public static float Atan2(float a,float b)=>(float)Math.Atan2(a,b);public static float Sqrt(float a)=>(float)Math.Sqrt(a);
  public static int FloorToInt(float v)=>(int)Math.Floor(v);public static int Clamp(int v,int a,int b)=>Math.Max(a,Math.Min(b,v));public static int Max(int a,int b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);
  public static int Min(int a,int b)=>Math.Min(a,b);
  public static float Clamp(float v,float a,float b)=>Math.Max(a,Math.Min(b,v));public static float DeltaAngle(float a,float b){float value=(b-a)%360;return value>180?value-360:value<-180?value+360:value;}
  public const float Deg2Rad=(float)Math.PI/180;public static float Sin(float a)=>(float)Math.Sin(a);public static float Cos(float a)=>(float)Math.Cos(a);public static float Abs(float a)=>Math.Abs(a);public static float Max(float a,float b)=>Math.Max(a,b);public static float Lerp(float a,float b,float t)=>a+(b-a)*t;public static float Clamp01(float a)=>Clamp(a,0,1);public static float Exp(float a)=>(float)Math.Exp(a);public static float MoveTowardsAngle(float a,float b,float t)=>a+Clamp(DeltaAngle(a,b),-t,t);public static float LerpAngle(float a,float b,float t)=>a+DeltaAngle(a,b)*t;public static int CeilToInt(float a)=>(int)Math.Ceiling(a);
 }
 public static class Time {public static float time,deltaTime=.05f;public static float unscaledTime=>time;}
 public static class Application {public static bool isPlaying=true;}
 public class Camera {public static Camera main=new Camera();public Transform transform=new Transform();}
}
namespace UnityEngine.Rendering {public enum ShadowCastingMode{Off}}
namespace UnityEngine.Serialization {public class FormerlySerializedAsAttribute:Attribute {public FormerlySerializedAsAttribute(string name){}}}
public enum WorldDimension {Overworld,Nether}
public enum SurvivalDifficulty {Easy,Normal,Hard}
public enum BlockType {Nothing,Air,Grass,Dirt,Stone,Bedrock,Water,Lava,Fire,SnowLayer}
public class BlockData {public bool generateCollider=true;}
public class Block {public BlockType type;public int sky,light,blockShape;public BlockData BlockData=new BlockData();public int GetSkyLight()=>sky;public int GetBlockLight()=>light;}
public static class SpecialBlockGeometry {public static bool NoCollision(BlockType t)=>t==BlockType.Air;public static int CollisionCount(BlockType t)=>1;public static Bounds CollisionPart(BlockType t,int s,int p)=>new Bounds(Vector3.one*.5f,Vector3.one);}
public struct InventoryStack {public ItemType type;public int count,damage;public bool Empty=>count<=0;public InventoryStack(ItemType t,int c,int d=0){type=t;count=c;damage=d;}public void Normalize(){if(count<=0)type=ItemType.Nothing;}}
public static class InventoryRules {public static int Capacity(InventoryStack[] a,ItemType t,int x,int y)=>1;public static void Insert(InventoryStack[] a,ItemType t,int n,int x,int y){}}
public static class BetaItemCatalog {public struct Entry {public int SourceId,Metadata;}public static bool TryGet(ItemType t,out Entry e){e=new Entry();return false;}}
public static class GameAudio {public static int sounds;public static float Variation(float a,float b)=>(a+b)*.5f;public static void Broadcast(string s,Vector3 p,float v,float pitch,World w){sounds++;}public static void Play(string s,Vector3 p,float v,float pitch=1){sounds++;}}
public static class BlockCollision {public static bool Supported(World w,Vector3 p,float width,float height)=>p.y<=64.001f;public static Vector3 Resolve(World w,Vector3 p,float r,float h,Vector3 move,bool step){if(p.y+move.y<64)move.y=64-p.y;return move;}public static bool OverlapsBody(Vector3 p,float r,float h,Vector3Int cell,Bounds box)=>new Bounds(p+Vector3.up*(h*.5f),new Vector3(r*2,h,r*2)).Intersects(new Bounds((Vector3)cell+box.center,box.size));}
public static class BlockTargeting {public static Block Raycast(World w,Vector3 p,Vector3 d,float distance,out Vector3Int cell,out Vector3 point){cell=new Vector3Int();point=p+d*distance;return w.blocked?new Block{type=BlockType.Stone}:null;}}
public static class FluidWorldQueries {public static bool Touches(World w,Vector3 p,float width,float height,BlockType type,out Vector3 flow){flow=Vector3.zero;return false;}public static BlockType AtEye(World w,Vector3 p)=>BlockType.Air;}
public partial class Player:MonoBehaviour {
 public PlayerInventory inventory=new PlayerInventory();
 public WorldDimension dimension;
 public int health=20;public bool networkStarted,isServer,isLocalPlayer,isFlying,dimensionTransfer,serverFlying;public double hurtUntil;public float exhaustion;public bool DimensionTransferInProgress=>dimensionTransfer;public World CurrentWorld;public Mirror.NetworkConnection connectionToClient=new Mirror.NetworkConnection();public int pushes;
 public void RpcAddForce(Mirror.NetworkConnection c,Vector3 a,float b){pushes++;}public void AddForce(Vector3 a,float b){pushes++;}
#if FULL_MOB_SYSTEM
 private int dropRequestId,lastServerDropRequest,knownItemWearRevision;private bool awaitingNetworkDrop,lastServerDropAccepted,serverMining;private InventoryStack[] lastServerDropReply;public World world;public Transform cam=new Transform();public float reach=5,eyeHeight=1.62f;
 private bool AcceptsItemRevision(int revision)=>revision==0;private void CancelMining(){}private void CompleteDropAction(int request,InventoryStack[] items,bool accepted,bool network){if(accepted)inventory.ApplyDropInventory(items);inventory.DropRequestPending=false;}
#endif
}
public partial class World:MonoBehaviour {
 public WorldDimension dimension;public SurvivalDifficulty survivalDifficulty=SurvivalDifficulty.Normal;
#if !FULL_MOB_SYSTEM
 private List<Player> mobPlayers=new List<Player>();private List<MobState> activeMobs=new List<MobState>();private List<Vector3Int> mobSpawnCandidates=new List<Vector3Int>();private MobEntitySystem mobEntities=new MobEntitySystem();public MobEntitySystem MobEntities=>mobEntities;public MobStore mobs=>mobEntities.Store;private JavaRandom mobRandom=new JavaRandom(173);private int mobTick;
#endif
 public int skyLightReduction=11,worldHeight=128;
#if !FULL_MOB_SYSTEM
 public int chunkSize=16;
#endif
 public float gamma;public bool blocked;public int edits,drops,deathSmoke;public RendererRoot worldRenderer=new RendererRoot();
 private sealed class Data {public Dictionary<Vector3Int,int> chunkDict=new Dictionary<Vector3Int,int>();}private Data worldData=new Data();
 public Dictionary<Vector3Int,Block> cells=new Dictionary<Vector3Int,Block>();public Block GetBlock(Vector3 p)=>GetBlock(Vector3Int.FloorToInt(p));public Block GetBlock(Vector3Int p)=>cells.TryGetValue(p,out var block)?block:new Block{type=p.y<64?(p.y==63?BlockType.Grass:BlockType.Stone):BlockType.Air,sky=p.y<64?0:15};
#if !FULL_MOB_SYSTEM
 private bool MobAreaLoaded(Vector3 p)=>true;private float MobPlayerDistance(Vector3 p){float d=float.MaxValue;foreach(var player in mobPlayers)d=Math.Min(d,(player.transform.position-p).sqrMagnitude);return d;}
 private bool FindMobGround(int x,int z,bool grass,MobType species,out Vector3 feet){feet=new Vector3(x+.5f,64,z+.5f);return true;}
 private void BroadcastMob(MobState c,bool removed=false){}
 private bool DamageMob(MobState mob,int amount,Vector3 away)=>MobRules.Hurt(mob,amount,away);
#endif
 private void SetFarmBlock(Vector3Int p,BlockType b){edits++;cells[p]=new Block{type=b};}private static float BedBlastResistance(BlockType b)=>3;
 private bool SpawnItemDrop(InventoryStack stack,Vector3 p){drops+=stack.count;return true;}
 public void PlayMobDeathSmoke(Vector3 p,float radius,float height){deathSmoke++;}
 private bool CanReplaceChest(Vector3Int p,BlockType type)=>true;private InventoryStack MiningItemDrop(BlockType type,ItemType tool)=>new InventoryStack();
 public void Add(Player p){p.CurrentWorld=this;mobPlayers.Add(p);
#if FULL_MOB_SYSTEM
 p.world=this;GameManager.Instance=new GameManager{localPlayer=p};
#endif
 }public bool Step(MobState c){mobTick++;mobNavigationBudget=2;activeMobs.Clear();activeMobs.AddRange(mobs.Entities);return TickMobBrain(c);}public void Arrows()=>TickMobArrows();public int ArrowCount=>mobArrows.Count;public int VisualCount=>mobArrowVisuals.Count;public void Reset()=>ResetMobArrows();
}
public class RendererRoot {public Transform transform=new Transform();}
public class PlayerInventory {public InventoryStack held;public InventoryStack HeldItem()=>held;
#if FULL_MOB_SYSTEM
 public bool DropRequestPending;public int selectedSlotIndex;public InventoryStack[] DropInventorySnapshot(){var items=new InventoryStack[36];items[0]=held;return items;}public void ApplyDropInventory(InventoryStack[] items){held=items[0];}
#endif
}
public class MeshData {
 public List<Vector3> vertices=new List<Vector3>(),uv=new List<Vector3>();public List<int> triangles=new List<int>();
 public MeshData(bool main){}public void AddVertex(Vector3 point){vertices.Add(point);}public void AddQuadTriangles(){int start=vertices.Count-4;triangles.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});}
}
