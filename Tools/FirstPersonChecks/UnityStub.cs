using System;
using System.Collections.Generic;
using System.Linq;
namespace UnityEngine {
 public class Object { public string name; public bool destroyed; public static void Destroy(Object o){if(o!=null)o.destroyed=true;} }
 public class Component:Object { public GameObject gameObject; public Transform transform=>gameObject.transform; public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>(); }
 public class MonoBehaviour:Component { public bool enabled=true; }
 public class GameObject:Object {
  public static Dictionary<string,GameObject> objects=new Dictionary<string,GameObject>();
  public readonly List<Component> components=new List<Component>(); public Transform transform; public int layer;
  public GameObject(string name){this.name=name;objects[name]=this;transform=new Transform{gameObject=this};components.Add(transform);}
  public T AddComponent<T>() where T:Component,new(){var t=new T{gameObject=this};components.Add(t);return t;}
  public T GetComponent<T>() where T:Component=>components.OfType<T>().FirstOrDefault();
  public static GameObject Find(string name)=>objects.ContainsKey(name)?objects[name]:null;
 }
 public class Transform:Component { public Vector3 position,localPosition,localScale=Vector3.one; public Quaternion localRotation,rotation; public Transform parent;public void SetParent(Transform p,bool world){parent=p;}public void SetPositionAndRotation(Vector3 p,Quaternion q){position=p;rotation=q;}public Vector3 lossyScale {get{var p=parent==null?Vector3.one:parent.lossyScale;return new Vector3(localScale.x*p.x,localScale.y*p.y,localScale.z*p.z);}} }
 public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public struct Vector3 {public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float s)=>new Vector3(a.x*s,a.y*s,a.z*s);}
 public struct Quaternion {public Vector3 euler;public static Quaternion Euler(Vector3 p)=>new Quaternion{euler=p};}
 public struct Rect { public float x,y,width,height; }
 public enum CameraClearFlags {Depth}
 public class Camera:Component {public int cullingMask,targetDisplay;public bool enabled=true,useOcclusionCulling,allowHDR,allowMSAA;public float depth,nearClipPlane,farClipPlane,fieldOfView;public Rect rect;public object targetTexture;public CameraClearFlags clearFlags;}
 public class Canvas:Component {public Camera worldCamera;}
 public class Texture:Object {public int width=64,height=64;}
 public class Texture2D:Texture {public static Texture2D whiteTexture=new Texture2D();}
 public class Material:Object {public Texture mainTexture;public Dictionary<string,float> values=new Dictionary<string,float>();public Material(){}public Material(Material m){mainTexture=m.mainTexture;}public void SetFloat(string key,float value){values[key]=value;}}
 public class Mesh:Object {public List<Vector3> vertices;public List<Vector2> uv;public List<int> triangles;public int vertexCount=>vertices==null?0:vertices.Count; public void SetVertices(List<Vector3> v){vertices=v;}public void SetUVs(int ch,List<Vector2> v){uv=v;}public void SetTriangles(List<int> t,int sub){triangles=t;}public void RecalculateNormals(){}public void RecalculateBounds(){} }
 public class MeshFilter:Component {public Mesh sharedMesh;}
 public class MeshRenderer:Component {public Material sharedMaterial;public Rendering.ShadowCastingMode shadowCastingMode;public bool receiveShadows;}
 public static class Resources {public static Dictionary<string,Object> assets=new Dictionary<string,Object>();public static T Load<T>(string path) where T:Object=>assets.ContainsKey(path)?(T)assets[path]:null;}
 public static class Mathf {public const float PI=(float)Math.PI;public static int Clamp(int x,int a,int b)=>Math.Max(a,Math.Min(b,x));public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Sqrt(float f)=>(float)Math.Sqrt(f);public static float Sin(float f)=>(float)Math.Sin(f);public static float Cos(float f)=>(float)Math.Cos(f);public static float Abs(float f)=>Math.Abs(f);public static float MoveTowards(float a,float b,float step)=>Math.Abs(a-b)<=step?b:a+Math.Sign(b-a)*step;}
 public static class Time {public static float deltaTime=.02f;}
 public static class Input {public static bool[] buttons=new bool[2];public static bool GetMouseButton(int i)=>buttons[i];}
 public static class Debug {public static void LogError(string s){throw new Exception(s);}}
}
namespace UnityEngine.Rendering {public enum ShadowCastingMode {Off}}
public enum ItemType {Nothing,Stone,Pickaxe,Missing}
public enum BlockType {Nothing,Stone}
public class InventoryStack {public ItemType type;public bool Empty=>type==ItemType.Nothing;}
public class PlayerInventory {public bool isOpen;public InventoryStack stack=new InventoryStack();public InventoryStack HeldItem()=>stack;}
public class Player:UnityEngine.MonoBehaviour {public World CurrentWorld=>World.Instance;public UnityEngine.Transform cam;public bool IsReadyForPlay=true,escapeMenuOpen;public int health=20;public PlayerInventory inventory=new PlayerInventory();}
public class PlayerHudAssets:UnityEngine.Object {public UnityEngine.Texture inventoryPlayerSkin;}
public static class WorldLoadingScreen {public static bool IsLoading;}
public class World {public WorldDimension dimension;public static World Instance;public int betaSkylightSubtracted;public Block GetBlock(UnityEngine.Vector3 position)=>new Block();}
public class Block {public int GetSkyLight()=>10;public int GetBlockLight()=>0;}
public static class Beta173Lighting {public static float Brightness(int sky,int block,int sub,int gamma,bool nether=false)=>.5f;}
public class ItemDefinition {public UnityEngine.Texture2D icon;}
public static class ItemCatalog {public static ItemDefinition Get(ItemType type)=>type==ItemType.Missing?null:new ItemDefinition{icon=new UnityEngine.Texture2D{width=16,height=16}};public static bool TryPlacedBlock(ItemType t,out BlockType b){b=BlockType.Stone;return t==ItemType.Stone;}}
public static class BlockShapesExtensions {public static bool IsPlant(BlockType b)=>false;}
public static class SpecialBlockGeometry {public static bool FlatShading(BlockType b)=>false;}
public static class ItemVisualMesh {public static UnityEngine.Mesh Build(BlockType b)=>FirstPersonMesh.Sprite(1,1);}

public enum WorldDimension { Nether=-1,Overworld=0 }
