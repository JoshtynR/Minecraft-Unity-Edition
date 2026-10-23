using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace UnityEngine {
 public class Object {public string name;public bool destroyed;public static void Destroy(Object o){if(o!=null)o.destroyed=true;}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();}
 public class MonoBehaviour:Component {public bool enabled=true;}
 public class GameObject:Object {
  public static List<GameObject> all=new List<GameObject>();public bool activeSelf=true;public int layer;public Transform transform;
  readonly List<Component> parts=new List<Component>();
  public GameObject(string name){this.name=name;transform=new Transform{gameObject=this};parts.Add(transform);all.Add(this);}
  public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};parts.Add(c);return c;}
  public T GetComponent<T>() where T:Component=>parts.OfType<T>().FirstOrDefault();public void SetActive(bool value){activeSelf=value;}
 }
 public class Transform:Component {public Vector3 position,localPosition,localScale=Vector3.one;public Quaternion rotation=Quaternion.identity,localRotation=Quaternion.identity;public Transform parent;public readonly List<Transform> children=new List<Transform>();public int childCount=>children.Count;public Transform GetChild(int i)=>children[i];public Vector3 right=>new Vector3(1,0,0);public void SetParent(Transform p,bool keep){if(parent!=null)parent.children.Remove(this);parent=p;if(p!=null)p.children.Add(this);}public Vector3 InverseTransformVector(Vector3 v)=>new Vector3(v.x/localScale.x,v.y/localScale.y,v.z/localScale.z);}
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>(float)Math.Sqrt(x*x+y*y);}
 public struct Vector3 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 down=>new Vector3(0,-1,0);public static Vector3 forward=>new Vector3(0,0,1);
  public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a)=>new Vector3(-a.x,-a.y,-a.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>a+-b;public static Vector3 operator*(Vector3 a,float s)=>new Vector3(a.x*s,a.y*s,a.z*s);
 }
 public struct Vector4 {public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}public static Vector4 zero=>new Vector4();}
 public struct Quaternion {
  public double x,y,z,w;public static Quaternion identity=>new Quaternion{w=1};
  public static Quaternion Euler(float x,float y,float z){double k=Math.PI/360;return new Quaternion{y=Math.Sin(y*k),w=Math.Cos(y*k)}*new Quaternion{x=Math.Sin(x*k),w=Math.Cos(x*k)}*new Quaternion{z=Math.Sin(z*k),w=Math.Cos(z*k)};}
  public static Quaternion Inverse(Quaternion q)=>new Quaternion{x=-q.x,y=-q.y,z=-q.z,w=q.w};
  public static Quaternion operator*(Quaternion a,Quaternion b)=>new Quaternion{x=a.w*b.x+a.x*b.w+a.y*b.z-a.z*b.y,y=a.w*b.y-a.x*b.z+a.y*b.w+a.z*b.x,z=a.w*b.z+a.x*b.y-a.y*b.x+a.z*b.w,w=a.w*b.w-a.x*b.x-a.y*b.y-a.z*b.z};
  public static Vector3 operator*(Quaternion q,Vector3 p){var v=q*new Quaternion{x=p.x,y=p.y,z=p.z}*Inverse(q);return new Vector3((float)v.x,(float)v.y,(float)v.z);}
  public static Quaternion LookRotation(Vector3 a,Vector3 b)=>identity;
 }
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1);public static Color clear=>new Color(0,0,0,0);public static Color Lerp(Color a,Color b,float t)=>new Color(a.r+(b.r-a.r)*t,a.g+(b.g-a.g)*t,a.b+(b.b-a.b)*t,a.a+(b.a-a.a)*t);}
 public static class Mathf {public const float PI=(float)Math.PI,Deg2Rad=PI/180;public static float Sin(float x)=>(float)Math.Sin(x);public static float Cos(float x)=>(float)Math.Cos(x);public static float Abs(float x)=>Math.Abs(x);public static float Max(float a,float b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static float Clamp01(float x)=>Math.Max(0,Math.Min(1,x));public static int RoundToInt(float x)=>(int)Math.Round(x);public static float MoveTowards(float x,float y,float d)=>Math.Abs(y-x)<d?y:x+Math.Sign(y-x)*d;}
 public static class Time {public static float time,fixedTime,deltaTime=.02f;}
 public class DefaultExecutionOrder:Attribute {public DefaultExecutionOrder(int order){}}
 public class Animator:Component {public float speed;public bool sneaking;public void SetFloat(string key,float value){speed=value;}public void SetBool(string key,bool value){sneaking=value;}}
 public static class Input {public static bool held=true;public static bool GetMouseButton(int n)=>held;public static bool GetMouseButtonDown(int n)=>false;}
 public static class Application {public static bool isPlaying=true;}
 public enum CameraClearFlags {SolidColor}
 public class Camera:Component {public bool enabled=true;public float farClipPlane=1000;public Color backgroundColor;public CameraClearFlags clearFlags;}
 public class Texture:Object {public int width,height;}
 public enum TextureFormat {RGBA32}public enum FilterMode {Point}public enum TextureWrapMode {Repeat,Clamp}
 public class Texture2D:Texture {Color[] pixels;public FilterMode filterMode;public TextureWrapMode wrapMode;public Texture2D(int w,int h,TextureFormat format,bool mip){width=w;height=h;pixels=new Color[w*h];}public void SetPixels(Color[] value){pixels=(Color[])value.Clone();}public Color[] GetPixels()=>pixels;public void Apply(bool mip,bool unreadable){}}
 public class Material:Object {public Texture mainTexture;public int renderQueue;public Vector2 mainTextureScale,mainTextureOffset;public Material(Material m){}public void SetFloat(string s,float f){}public void SetColor(string s,Color c){}public void SetVector(string s,Vector4 v){}}
 public class Mesh:Object {public Rendering.IndexFormat indexFormat;public Vector3[] vertices;public Vector2[] uv;public Color[] colors;public int[] triangles;public void SetVertices(List<Vector3> v){vertices=v.ToArray();}public void SetUVs(int channel,List<Vector2> v){uv=v.ToArray();}public void SetColors(List<Color> v){colors=v.ToArray();}public void SetTriangles(List<int> v,int sub){triangles=v.ToArray();}public void RecalculateBounds(){}}
 public class MeshFilter:Component {public Mesh sharedMesh;}
 public class MeshRenderer:Component {public Material sharedMaterial;public bool receiveShadows;public Rendering.ShadowCastingMode shadowCastingMode;}
 public static class Resources {public static Dictionary<string,Object> items=new Dictionary<string,Object>();public static T Load<T>(string name) where T:Object=>items.ContainsKey(name)?items[name] as T:null;}
 public static class Shader {public static Dictionary<string,Vector4> vectors=new Dictionary<string,Vector4>();public static Vector4 GetGlobalVector(string name)=>vectors.ContainsKey(name)?vectors[name]:Vector4.zero;public static void SetGlobalVector(string name,Vector4 value){vectors[name]=value;}}
 public class RequireComponent:Attribute {public RequireComponent(Type t){}}public class Header:Attribute {public Header(string s){}}
 public static class Debug {public static void LogError(string text){throw new Exception(text);}}
}
namespace UnityEngine.Rendering {public enum ShadowCastingMode {Off}public enum IndexFormat {UInt16,UInt32}}
namespace Mirror {public class Command:Attribute {}public class TargetRpc:Attribute {}public class SyncVar:Attribute {}public class NetworkConnection {}public static class NetworkTime {public static double time=>Time.time;}public static class NetworkClient {public static bool active;}public static class NetworkServer {public static bool active;}}
public enum WorldDimension {Overworld,Nether}
public enum BlockType {Air,Nothing,Water,Lava,Ladder,Stone,Cactus}
public struct InventoryStack {public ItemType type;public int count;public bool Empty=>count<=0||type==ItemType.Nothing;public void Normalize(){if(count<=0){type=ItemType.Nothing;count=0;}}}
public class PlayerInventory {public bool DropRequestPending,isOpen;public int selectedSlotIndex;public InventoryStack[] slots=new InventoryStack[36];public InventoryStack HeldItem()=>slots[selectedSlotIndex];public InventoryStack[] DropInventorySnapshot()=>(InventoryStack[])slots.Clone();public void ApplyDropInventory(InventoryStack[] s){slots=(InventoryStack[])s.Clone();}}
public static class ChestTransactions {public static bool Valid(InventoryStack[] s,InventoryStack cursor)=>s!=null&&s.Length==36&&s.All(x=>x.count>=0&&x.count<=64);}
public static class GameAudio {public static void Play(string s,Vector3 p,float volume,float pitch){}public static void Broadcast(string s,Vector3 p,float v,float pitch,World world){}public static float Variation(float a,float b)=>a;}
public static class FluidWorldQueries {public static bool submerged;public static bool Touches(World w,Vector3 p,float width,float height,BlockType type,out int ignored){ignored=0;return submerged;}public static BlockType AtEye(World w,Vector3 p)=>submerged?BlockType.Water:BlockType.Air;}
public static class BlockCollision {public static bool cactus;public static bool TouchesBlock(World w,Vector3 p,float width,float height,BlockType type)=>cactus&&type==BlockType.Cactus;}
public class Block {public BlockType type=BlockType.Stone;}
public partial class World {public static World Overworld;public bool IsWorldCreated=true;public int skyLightReduction,renderDistance=6,chunkSize=16;public WorldDimension dimension;public Block GetBlock(Vector3 p)=>new Block();public bool SpawnItemDrop(InventoryStack stack,Vector3 p,Vector3 velocity,int delay)=>true;}
public static class InventoryRules {public static int Insert(InventoryStack[] slots,ItemType type,int count,int start,int end){for(int n=start;n<end;n++)if(slots[n].Empty){slots[n]=new InventoryStack{type=type,count=count};return 0;}return count;}}
public partial class WorldServer {public static WorldServer instance;}
public class PlayerHudAssets:UnityEngine.Object {public Texture2D sunTexture,moonTexture,cloudTexture;}
public static class WorldLoadingScreen {public static bool IsLoading;}
public class PlayerObjects {public Transform leftArm,rightArm,leftLeg,rightLeg;}
public partial class Player:MonoBehaviour {
 public bool DimensionTransferInProgress=>dimensionTransfer;
 public bool isCrouching;
 public void MotionTick(float dt)=>AdvanceWalkMotion(dt);
 public World world,CurrentWorld;public Transform cam;public PlayerInventory inventory=new PlayerInventory();public int health=20,hunger=10;public float saturation,exhaustion,walkSpeed=4.317f,entityWidth=.3f,entityHeight=1.8f;private double hurtUntil;
 public bool dimensionTransfer,isGrounded=true,isFlying,InWater,InLava,escapeMenuOpen,networkStarted,isLocalPlayer=true,serverFlying,serverGrounded,isSprinting;public float horizontal,vertical;
 public int knownItemWearRevision;public Mirror.NetworkConnection connectionToClient;private bool AcceptsItemRevision(int revision)=>revision==knownItemWearRevision;
 public bool StartFood()=>TryStartEating();public void FoodTick()=>UpdateEating();public void EndFood(bool finish)=>FinishEatingRequest(finish);public void CameraTick()=>UpdateCameraBobbing();public void FallTick()=>AdvanceFallDamage();public void CactusTick()=>AdvanceCactusDamage();
}
