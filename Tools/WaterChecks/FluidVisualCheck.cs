// Compile actual camera/effect/player adapters without a Unity installation.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace Mirror {public class SyncVarAttribute:Attribute {}}
namespace TMPro {public class TMP_FontAsset:UnityEngine.Object {}}
namespace UnityEngine.Rendering {public enum ShadowCastingMode {Off}}
namespace UnityEngine
{
 public class Object {public string name;public bool destroyed;public static void Destroy(Object o){if(o!=null)o.destroyed=true;}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();}
 public class MonoBehaviour:Component {}
 public class ScriptableObject:Object {}
 public class RequireComponent:Attribute {public RequireComponent(Type t){}}
 public class Tooltip:Attribute {public Tooltip(string t){}}
 public class GameObject:Object
 {
  readonly Dictionary<Type,Component> components=new Dictionary<Type,Component>();public Transform transform;
  public GameObject(string name=""){this.name=name;transform=new Transform{gameObject=this};components[typeof(Transform)]=transform;}
  public T AddComponent<T>() where T:Component,new(){var c=new T{gameObject=this};components[typeof(T)]=c;typeof(T).GetMethod("Awake",BindingFlags.Instance|BindingFlags.NonPublic)?.Invoke(c,null);return c;}
  public T GetComponent<T>() where T:Component {Component c;return components.TryGetValue(typeof(T),out c)?(T)c:null;}
 }
 public class Transform:Component {public Vector3 position,eulerAngles;public Transform parent;public void SetParent(Transform t,bool world){parent=t;}}
 public struct Vector3
 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
 }
 public struct Vector3Int
 {
  public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}
  public static Vector3Int up=>new Vector3Int(0,1,0);public static Vector3Int FloorToInt(Vector3 v)=>new Vector3Int((int)Math.Floor(v.x),(int)Math.Floor(v.y),(int)Math.Floor(v.z));
  public static Vector3Int operator +(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
  public static implicit operator Vector3(Vector3Int p)=>new Vector3(p.x,p.y,p.z);
 }
 public struct Vector4 {public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}public static Vector4 zero=>new Vector4();}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public struct Rect {public Rect(float x,float y,float w,float h){}}
 public static class Mathf {public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int FloorToInt(float n)=>(int)Math.Floor(n);}
 public class Texture:Object {}
 public class Texture2D:Texture {public int width=16,height=16;}
 public class Material:Object {public Texture mainTexture;public Material(Material m){}}
 public static class Resources {public static Dictionary<string,Object> items=new Dictionary<string,Object>();public static T Load<T>(string key) where T:Object {Object o;return items.TryGetValue(key,out o)?(T)o:null;}}
 public class AudioClip:Object {public static AudioClip Create(string name,int a,int b,int c,bool d)=>new AudioClip{name=name};public void SetData(float[] v,int start){}}
 public enum AudioRolloffMode {Linear}
 public class AudioSource:Component {public bool playOnAwake;public float spatialBlend,maxDistance;public AudioRolloffMode rolloffMode;public void PlayOneShot(AudioClip c,float v){}}
 public enum ParticleSystemStopBehavior {StopEmittingAndClear}public enum ParticleSystemSimulationSpace {World}
 public class ParticleSystem:Component
 {
  public class MainModule {public bool loop,playOnAwake;public int maxParticles;public float gravityModifier;public ParticleSystemSimulationSpace simulationSpace;}
  public class Toggle {public bool enabled;}
  public struct EmitParams {public Vector3 position,velocity;public float startLifetime,startSize;public Color startColor;}
  public MainModule main=new MainModule();public Toggle emission=new Toggle(),shape=new Toggle();public int emitted;
  void Awake(){gameObject.AddComponent<ParticleSystemRenderer>();}
  public void Stop(bool b,ParticleSystemStopBehavior s){}public void Play(){}public void Emit(EmitParams p,int n){emitted+=n;}
 }
 public class ParticleSystemRenderer:Component {public Material sharedMaterial;public Rendering.ShadowCastingMode shadowCastingMode;public bool receiveShadows;}
 public enum FogMode {Exponential,Linear}public enum CameraClearFlags {SolidColor,Skybox}
 public class Camera:Component {public Color backgroundColor;public CameraClearFlags clearFlags=CameraClearFlags.Skybox;}
 public static class RenderSettings {public static bool fog;public static Color fogColor;public static float fogDensity;public static FogMode fogMode;}
 public static class Shader {static Vector4 fog;public static Vector4 GetGlobalVector(string key)=>fog;public static void SetGlobalVector(string key,Vector4 value){fog=value;}}
 public static class Application {public static bool isBatchMode,isPlaying=true;}
 public static class Time {public static float time,deltaTime=.05f;}
 public enum EventType {Repaint}public class Event {public static Event current=new Event();public EventType type;}
 public static class Screen {public static int width=800,height=600;}
 public static class GUI {public static Color color;public static int depth;public static void DrawTextureWithTexCoords(Rect r,Texture t,Rect uv){}}
}
public class Block {public BlockType type;public byte waterLevel;}
public static class SpecialBlockGeometry {public static bool NormalCube(BlockType t)=>t==BlockType.Stone;}
public partial class World:MonoBehaviour
{
 public static World Instance;public bool IsWorldCreated=true;
 public Dictionary<Vector3Int,Block> blocks=new Dictionary<Vector3Int,Block>();
 public Block GetBlock(Vector3Int p){Block b;return blocks.TryGetValue(p,out b)?b:new Block{type=BlockType.Air};}
 public static bool FluidHasSolidMaterial(BlockType t)=>t!=BlockType.Air&&t!=BlockType.Water&&t!=BlockType.Lava;
}
public partial class Player:MonoBehaviour
{
 public World world;public bool networkStarted,isLocalPlayer=true,isFlying,serverFlying,InWater;
 public int health=20;public float entityWidth=.3f,entityHeight=1.8f,eyeHeight=1.578f;public Transform cam;
}
public class GameManager {public static GameManager Instance;public Player localPlayer;}
public static class FluidVisualCheck
{
 static int count;
 static void Check(bool condition,string why){count++;if(!condition)throw new Exception(why);}
 static void Call(object o,string method,params object[] args)=>o.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
 public static void Main()
 {
  var template=new Material(null);var assets=new PlayerHudAssets{fluidFizz=new AudioClip{name="userFizz"},fluidSplash=new AudioClip{name="userSplash"}};
  Resources.items["FluidParticleMaterial"]=template;Resources.items["PlayerHudAssets"]=assets;
  var world=World.Instance=new GameObject("World").AddComponent<World>();
  var player=new GameObject("Player").AddComponent<Player>();player.world=world;
  player.transform.position=new Vector3(.5f,0,.5f);player.cam=new GameObject("Camera").transform;
  var camera=player.cam.gameObject.AddComponent<Camera>();player.cam.position=new Vector3(.5f,1.5f,.5f);
  world.blocks[new Vector3Int(0,1,0)]=new Block{type=BlockType.Water};
  Call(player,"UpdateFluidView");var effect=player.cam.GetComponent<FluidCameraEffects>();
  Shader.SetGlobalVector("_FluidFog",new Vector4(1,2,3,4));RenderSettings.fog=false;
  Call(effect,"OnPreCull");Check(Math.Abs(Shader.GetGlobalVector("_FluidFog").w-.1f)<1e-7&&RenderSettings.fog&&camera.clearFlags==CameraClearFlags.SolidColor,"underwater camera installs Beta fog and hides skybox");
  Call(effect,"OnPostRender");Check(Shader.GetGlobalVector("_FluidFog").w==4&&!RenderSettings.fog&&camera.clearFlags==CameraClearFlags.Skybox,"camera restores global state after rendering");
  world.blocks[new Vector3Int(0,1,0)].type=BlockType.Lava;Call(effect,"OnPreCull");Check(Shader.GetGlobalVector("_FluidFog").w==2,"lava camera uses dense fog");
  Call(effect,"OnDisable");Check(Shader.GetGlobalVector("_FluidFog").w==4,"disabled camera also restores globals");
  var fluidEffects=world.GetComponent<FluidEffects>();fluidEffects.Emit(new Vector3(8,9,10),0);
  Check(world.transform.position.x==0&&world.transform.position.y==0,"positional sound never moves the world transform");
  var emitters=(ParticleSystem[])typeof(FluidEffects).GetField("emitters",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(fluidEffects);
  Check(emitters[0].emitted==8&&emitters[0].main.loop&&emitters[0].main.maxParticles==256,"smoke emitter reuses a bounded looping system");
  Call(fluidEffects,"OnDestroy");Check(emitters[0].gameObject.destroyed&&!template.destroyed&&!assets.fluidFizz.destroyed&&!assets.fluidSplash.destroyed,"cleanup destroys owned emitters and preserves borrowed assets");
  world.blocks[new Vector3Int(0,0,0)]=new Block{type=BlockType.Lava};world.blocks[new Vector3Int(0,1,0)].type=BlockType.Lava;
  Call(player,"AdvanceFluidHazards",.05f);Check(player.health==16&&player.burning,"actual player adapter applies authoritative lava damage");
  world.blocks[new Vector3Int(0,0,0)].type=BlockType.Water;world.blocks[new Vector3Int(0,1,0)].type=BlockType.Water;
  Call(player,"AdvanceFluidHazards",.05f);Check(!player.burning&&player.airSupply==299,"actual player adapter extinguishes and consumes air");
  player.isFlying=true;Call(player,"AdvanceFluidHazards",.05f);Check(player.airSupply==300&&!player.burning,"flight clears hazards");
  Console.WriteLine(count+" production player/camera/particle ownership and integration checks passed (Unity rendering mocked).");
 }
}
