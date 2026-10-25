// Compile actual camera/effect/player adapters without a Unity installation.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace Mirror
{
 public class SyncVarAttribute:Attribute {}public class CommandAttribute:Attribute {}public class TargetRpcAttribute:Attribute {}public class NetworkConnection {}
 public static class NetworkServer {public static bool active;}public static class NetworkClient {public static bool active;}
 public class NetworkManager {public static NetworkManager singleton;public int hostStops,clientStops;public void StopHost(){hostStops++;NetworkServer.active=false;NetworkClient.active=false;}public void StopClient(){clientStops++;NetworkClient.active=false;}}
}
namespace UnityEngine.SceneManagement {public static class SceneManager {public static string loaded;public static void LoadScene(string name){loaded=name;}}}
namespace UnityEngine.UI {public class Button:UnityEngine.Component {public class Click {public void AddListener(Action action){}}public Click onClick=new Click();}}
namespace TMPro {public class TMP_FontAsset:UnityEngine.Object {}}
namespace UnityEngine.Rendering {public enum ShadowCastingMode {Off}}
namespace UnityEngine
{
 public class Object {public string name;public bool destroyed;public static void Destroy(Object o){if(o!=null)o.destroyed=true;}}
 public class Component:Object {public GameObject gameObject;public Transform transform=>gameObject.transform;public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();}
 public class MonoBehaviour:Component {}
 public static class Debug {public static void LogException(Exception e){throw e;}}
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
 public class Transform:Component {public Vector3 position,eulerAngles;public Transform parent;public Transform Find(string path)=>this;public void SetParent(Transform t,bool world){parent=t;}}
 public struct Vector3
 {
  public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
  public float magnitude=>(float)Math.Sqrt(x*x+y*y+z*z);public Vector3 normalized=>magnitude<.000001f?new Vector3():new Vector3(x/magnitude,y/magnitude,z/magnitude);
  public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
  public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);
 }
 public struct Vector3Int
 {
  public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}
  public static Vector3Int up=>new Vector3Int(0,1,0);public static Vector3Int down=>new Vector3Int(0,-1,0);public static Vector3Int left=>new Vector3Int(-1,0,0);public static Vector3Int right=>new Vector3Int(1,0,0);public static Vector3Int back=>new Vector3Int(0,0,-1);public static Vector3Int forward=>new Vector3Int(0,0,1);public static Vector3Int FloorToInt(Vector3 v)=>new Vector3Int((int)Math.Floor(v.x),(int)Math.Floor(v.y),(int)Math.Floor(v.z));
  public static Vector3Int operator +(Vector3Int a,Vector3Int b)=>new Vector3Int(a.x+b.x,a.y+b.y,a.z+b.z);
  public static implicit operator Vector3(Vector3Int p)=>new Vector3(p.x,p.y,p.z);
 }
 public struct Vector4 {public float x,y,z,w;public Vector4(float x,float y,float z,float w){this.x=x;this.y=y;this.z=z;this.w=w;}public static Vector4 zero=>new Vector4();}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color Lerp(Color a,Color b,float t)=>a;public static Color white=>new Color(1,1,1,1);public static Color black=>new Color(0,0,0,1);}
 public struct Vector2 {public static Vector2 zero=>new Vector2();public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public static class Input {public static Vector3 mousePosition;}
 public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public bool Contains(Vector2 p)=>p.x>=x&&p.x<x+width&&p.y>=y&&p.y<y+height;}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);public static int Abs(int x)=>Math.Abs(x);public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Clamp01(float n)=>Math.Max(0,Math.Min(1,n));public static int FloorToInt(float n)=>(int)Math.Floor(n);}
 public enum CursorLockMode {None,Locked}public static class Cursor {public static CursorLockMode lockState=CursorLockMode.Locked;public static bool visible;}
 public class Texture:Object {}
 public enum TextureFormat {RGBA32}public enum FilterMode {Bilinear}public enum TextureWrapMode {Clamp}
 public class Font:Object {}
 public class Texture2D:Texture {public int width=16,height=16;public Color[] pixels;public FilterMode filterMode;public TextureWrapMode wrapMode;public Texture2D(){}public Texture2D(int w,int h,TextureFormat format,bool mip){width=w;height=h;}public void SetPixels(Color[] colors){pixels=colors;}public void Apply(){}public static Texture2D whiteTexture=new Texture2D();}
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
 public class Camera:Component {public float farClipPlane=1000;public Color backgroundColor;public CameraClearFlags clearFlags=CameraClearFlags.Skybox;}
 public static class RenderSettings {public static bool fog;public static Color fogColor;public static float fogDensity,fogStartDistance,fogEndDistance;public static FogMode fogMode;}
 public static class Shader {static Vector4 fog;public static Vector4 GetGlobalVector(string key)=>fog;public static void SetGlobalVector(string key,Vector4 value){fog=value;}}
 public static class Application {public static bool isBatchMode,isPlaying=true;}
 public static class Time {public static float time,unscaledTime,deltaTime=.05f;}
 public enum EventType {Repaint}public class Event {public static Event current=new Event();public EventType type;public Vector2 mousePosition;}
 public static class Screen {public static int width=800,height=600;}
 public struct Matrix4x4 {public static Matrix4x4 Scale(Vector3 v)=>new Matrix4x4();}
 public enum TextAnchor {MiddleCenter,UpperCenter}
 public class RectOffset {}
 public class GUIStyleState {public Color textColor;}
 public class GUIStyle {public Font font;public int fontSize;public bool richText;public RectOffset padding,margin;public Vector2 contentOffset;public TextAnchor alignment;public GUIStyleState normal=new GUIStyleState();public GUIStyle(){}public GUIStyle(GUIStyle other){font=other.font;fontSize=other.fontSize;richText=other.richText;alignment=other.alignment;}public static GUIStyle none=new GUIStyle();}
 public class GUISkin {public GUIStyle label=new GUIStyle();}
 public static class GUI {public static Color color;public static int depth;public static Matrix4x4 matrix;public static GUISkin skin=new GUISkin();public static List<string> labels=new List<string>();public static int clickButton=-1,buttons;public static void DrawTextureWithTexCoords(Rect r,Texture t,Rect uv){}public static void DrawTexture(Rect r,Texture t){}public static void Box(Rect r,string s){}public static void Label(Rect r,string s){}public static List<Tuple<Rect,string,Color,bool,int>> draws=new List<Tuple<Rect,string,Color,bool,int>>();public static void Label(Rect r,string s,GUIStyle style){labels.Add(s);draws.Add(Tuple.Create(r,s,style.normal.textColor,style.richText,style.fontSize));}public static bool Button(Rect r,string s,GUIStyle style)=>buttons++==clickButton;}
}
public class Block {public BlockType type;public byte waterLevel,blockShape;}
public static class SpecialBlockGeometry {public static bool NormalCube(BlockType t)=>t==BlockType.Stone;public static byte State(BlockType t,byte shape)=>shape;public static Vector3Int PartnerOffset(BlockType t,byte state)=>Vector3Int.right;}
public partial class World:MonoBehaviour
{
 public bool overworldFog=true;public double CurrentWorldTicks=>1000;public int renderDistance=6,chunkSize=16;
 public World worldRenderer=>this;public WorldDimension dimension;public static System.Threading.Tasks.Task SaveAllDimensionsAsync()=>System.Threading.Tasks.Task.CompletedTask;
 public static World Instance;public bool IsWorldCreated=true;public int worldHeight=12;
 public Dictionary<Vector3Int,Block> blocks=new Dictionary<Vector3Int,Block>();
 public Block GetBlock(Vector3Int p){Block b;return blocks.TryGetValue(p,out b)?b:new Block{type=BlockType.Air};}
 public static bool FluidHasSolidMaterial(BlockType t)=>t!=BlockType.Air&&t!=BlockType.Water&&t!=BlockType.Lava;
 public void AddChunkToUpdate(ChunkRenderer renderer,bool first=false){}
}
public class ChunkRenderer {}
public static class WorldDataHelper
{
 public static void SetBlock(World world,Vector3Int position,BlockType type){world.blocks[position]=new Block{type=type};}
 public static Vector3Int GetChunkPosition(World world,Vector3Int position)=>new Vector3Int((int)Math.Floor(position.x/16.0)*16,0,(int)Math.Floor(position.z/16.0)*16);
 public static ChunkRenderer GetChunk(World world,Vector3Int position)=>null;
}
public partial class Player:MonoBehaviour
{
 private class FallStub {public void Reset(){}}private readonly FallStub falling=new FallStub();
 private void ResetWalkMotion(){}
 public WorldDimension dimension;public World CurrentWorld=>world??World.Instance;public float PortalOverlay;private void BeginDimensionTravel(WorldDimension target,Vector3 position,bool portal){}
 public World world;public bool networkStarted,isLocalPlayer=true,isFlying,serverFlying,InWater,sleeping,hasBedSpawn;public Vector3Int bedSpawn;public string SleepMessage=>"";
 public bool isServer;public Mirror.NetworkConnection connectionToClient;
 public bool escapeMenuOpen;
 public int hunger;public float saturation,exhaustion;public int movementResets;
 private void ResetFluidMovement(){movementResets++;}
 private void ShowSleepMessage(string message){}public void RequestWakeFromBed(){}
 public int health=20;public float entityWidth=.3f,entityHeight=1.8f,eyeHeight=1.578f;public Transform cam;
}
public class GameManager {public static GameManager Instance;public Player localPlayer;}
public struct TestLobby {public void Leave(){LobbyManager.leaves++;}}
public static class LobbyManager {public static TestLobby? currentLobby;public static int leaves;}
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
  world.blocks[new Vector3Int(0,1,0)].type=BlockType.Air;
  RenderSettings.fogStartDistance=3;RenderSettings.fogEndDistance=7;
  Call(effect,"OnPreCull");Check(RenderSettings.fogMode==FogMode.Linear&&RenderSettings.fogEndDistance>16,"overworld air uses distance fog");
  Call(effect,"OnPostRender");Check(RenderSettings.fogStartDistance==3&&RenderSettings.fogEndDistance==7,"overworld fog range restored for other cameras");
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
  player.isFlying=false;player.transform.position=new Vector3(2.5f,1,.5f);world.blocks[new Vector3Int(2,0,0)]=new Block{type=BlockType.Stone};
  Call(player,"AdvanceFluidHazards",.05f);
  player.transform.position=new Vector3(.5f,0,.5f);player.health=0;Call(player,"UpdateFluidView");
  Check(Cursor.lockState==CursorLockMode.None&&Cursor.visible,"death releases the cursor for respawn");
  assets.guiFont=new Font();assets.guiWidgets=new Texture2D{width=256,height=256};
  GUI.labels.Clear();GUI.buttons=0;GUI.clickButton=0;Call(effect,"OnGUI");
  Check(player.health==0&&GUI.labels.Contains("You died!")&&GUI.labels.Contains("Score: <color=#ffff55>0</color>"),"Minecraft death screen draws title/score and blocks immediate respawn clicks");
  Check(GUI.labels.FindAll(s=>s=="You died!").Count==2&&GUI.labels.FindAll(s=>s=="Score: <color=#ffff55>0</color>").Count==1&&GUI.labels.FindAll(s=>s=="Score: 0").Count==1,"death screen draws one colored foreground and a plain shadow");
  var titleDraws=GUI.draws.FindAll(d=>d.Item2=="You died!");
  Check(titleDraws[0].Item3.r==0&&titleDraws[0].Item3.g==0&&titleDraws[0].Item3.b==0&&!titleDraws[0].Item4&&titleDraws[0].Item1.y==62&&titleDraws[1].Item1.y==60&&titleDraws[1].Item5==16,"title has a black two-pixel shadow under its correctly positioned foreground");
  var scoreShadow=GUI.draws.Find(d=>d.Item2=="Score: 0");
  Check(scoreShadow.Item3.r==0&&scoreShadow.Item3.g==0&&scoreShadow.Item3.b==0&&!scoreShadow.Item4&&scoreShadow.Item1.y==101,"colored score cannot override the black one-pixel shadow");
  var respawnDraws=GUI.draws.FindAll(d=>d.Item2=="Respawn");
  Check(Math.Abs(respawnDraws[1].Item3.r-160/255f)<.00001f&&respawnDraws[1].Item1.y==153,"disabled button text uses native gray and six-pixel top inset");
  var gradient=(Texture2D)typeof(FluidCameraEffects).GetField("deathGradient",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(effect);
  Check(gradient.pixels.Length==2&&Math.Abs(gradient.pixels[1].a-96/255f)<.00001f&&Math.Abs(gradient.pixels[0].a-160/255f)<.00001f,"death screen uses native red gradient opacity");
  var style=(GUIStyle)typeof(FluidCameraEffects).GetField("deathTextStyle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(effect);
  Check(style.font==assets.guiFont&&style.fontSize==8&&style.alignment==TextAnchor.UpperCenter,"death screen uses logical pixel sizing and native top coordinates");
  Time.unscaledTime=1.1f;GUI.buttons=0;GUI.clickButton=1;Call(effect,"OnGUI");
  Mirror.NetworkManager.singleton=new Mirror.NetworkManager();Mirror.NetworkServer.active=Mirror.NetworkClient.active=true;LobbyManager.currentLobby=new TestLobby();
  Time.unscaledTime=2.2f;GUI.buttons=0;GUI.clickButton=0;Call(effect,"OnGUI");
  Check(Mirror.NetworkManager.singleton.hostStops==1&&LobbyManager.leaves==1,"title confirmation leaves the lobby and stops the host once");
  EscapeMenuManager.ExitToTitle();Check(UnityEngine.SceneManagement.SceneManager.loaded=="MainMenu","offline title action returns to the main menu scene");
  GUI.clickButton=-1;
  player.RequestFluidRespawn();
  Check(player.health==20&&player.airSupply==300&&!player.burning&&player.transform.position.x==2.5f&&player.transform.position.y==1,"lava death respawns at a verified safe position and clears damage state");
  Check(player.movementResets==1&&Cursor.lockState==CursorLockMode.Locked&&!Cursor.visible,"respawn resets liquid motion and restores the cursor");
  world.blocks.Clear();world.blocks[new Vector3Int(15,3,0)]=new Block{type=BlockType.Stone};
  var edgeSpawn=WorldSpawnRules.Resolve(world);
  Check(edgeSpawn.x==15.5f&&edgeSpawn.y==4&&edgeSpawn.z==.5f,"world spawn searches the full 15-block radius around zero");
  world.blocks.Clear();world.blocks[new Vector3Int(16,3,0)]=new Block{type=BlockType.Stone};
  var fallbackSpawn=WorldSpawnRules.Resolve(world);
  Check(fallbackSpawn.x==.5f&&fallbackSpawn.y==10&&fallbackSpawn.z==.5f&&world.blocks[new Vector3Int(0,9,0)].type==BlockType.Stone,
      "world spawn creates a deterministic safe fallback when no natural candidate exists");
  player.transform.position=new Vector3(4,1,4);player.RequestFluidRespawn();Check(player.transform.position.x==4,"alive clients cannot use respawn as a teleport");
  Call(effect,"OnDestroy");Check(gradient.destroyed&&!assets.guiWidgets.destroyed,"death screen releases its gradient and preserves source artwork");
  Console.WriteLine(count+" production player/camera/particle ownership and integration checks passed (Unity rendering mocked).");
 }
}

public enum WorldDimension {Nether=-1,Overworld=0}
public class TextureData {public UnityEngine.Texture2D side,top;}
public class BlockTypeData {public TextureData textureData=new TextureData();}
public static class BlockDataManager {public static System.Collections.Generic.Dictionary<int,BlockTypeData> blockTypeDataDictionary=new System.Collections.Generic.Dictionary<int,BlockTypeData>();}
