using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace UnityEngine
{
 public class Object
 {
  public string name;
  public bool destroyed;
  public static void Destroy(Object obj){if(obj==null)return;obj.destroyed=true;if(obj is GameObject go){go.activeInHierarchy=false;foreach(var c in go.components.ToArray())Lifecycle.Call(c,"OnDestroy");}}
  public static void DontDestroyOnLoad(Object obj){}
  public static GameObject Instantiate(GameObject source)=>new GameObject(source.name);
 }
 public static class Lifecycle
 {
  public static void Call(object obj,string method){var m=obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);if(m!=null)m.Invoke(obj,null);}
 }
 public class Component:Object
 {
  public GameObject gameObject;public Transform transform=>gameObject.transform;
  public T GetComponent<T>() where T:Component=>gameObject.GetComponent<T>();
  public T GetComponentInChildren<T>(bool includeInactive=false) where T:Component=>gameObject.GetComponentInChildren<T>();
 }
 public class Behaviour:Component{public bool enabled=true;}
 public class MonoBehaviour:Behaviour
 {
  public Coroutine StartCoroutine(IEnumerator routine){var c=new Coroutine{routine=routine};if(routine.MoveNext())Scheduler.running.Add(c);return c;}
  public void StopCoroutine(Coroutine c){Scheduler.running.Remove(c);}
 }
 public class Coroutine {public IEnumerator routine;}
 public static class Scheduler
 {
  public static readonly List<Coroutine> running=new List<Coroutine>();
  public static void Step(){foreach(var c in running.ToArray())if(running.Contains(c)&&!c.routine.MoveNext())running.Remove(c);}
 }
 public class GameObject:Object
 {
  public static Dictionary<string,GameObject> named=new Dictionary<string,GameObject>();public static GameObject Find(string name)=>named.ContainsKey(name)?named[name]:null;
  public bool activeInHierarchy=true;public readonly List<Component> components=new List<Component>();public Transform transform;
  public GameObject(string name,params Type[] types){this.name=name;transform=new RectTransform{gameObject=this};components.Add(transform);foreach(var t in types)if(t!=typeof(RectTransform))Add(t);}
  Component Add(Type t){var c=(Component)Activator.CreateInstance(t);c.gameObject=this;components.Add(c);Lifecycle.Call(c,"Awake");return c;}
  public T AddComponent<T>() where T:Component,new()=> (T)Add(typeof(T));
  public T GetComponent<T>() where T:Component=>components.OfType<T>().FirstOrDefault();
  public T GetComponentInChildren<T>() where T:Component {var self=GetComponent<T>();if(self!=null)return self;foreach(var child in transform.children){var found=child.gameObject.GetComponentInChildren<T>();if(found!=null)return found;}return null;}
 }
 public class Transform:Component
 {
  public Vector3 position;public readonly List<Transform> children=new List<Transform>();
  public void SetParent(Transform parent,bool stays){parent.children.Add(this);}
 }
 public class RectTransform:Transform {public Vector2 anchorMin,anchorMax,pivot,anchoredPosition,sizeDelta;}
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>(float)Math.Sqrt(x*x+y*y);public static Vector2 zero=>new Vector2();public static Vector2 one=>new Vector2(1,1);}
 public struct Vector3{public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 down=>new Vector3(0,-1,0);public static Vector3 zero=>new Vector3();public float sqrMagnitude=>x*x+y*y+z*z;public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);public static Vector3 operator-(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);public static Vector3 operator*(Vector3 a,float b)=>new Vector3(a.x*b,a.y*b,a.z*b);}
 public struct Vector3Int{public int x,y,z;public Vector3Int(int x,int y,int z){this.x=x;this.y=y;this.z=z;}public static implicit operator Vector3(Vector3Int p)=>new Vector3(p.x,p.y,p.z);public static Vector3Int FloorToInt(Vector3 p)=>new Vector3Int((int)Math.Floor(p.x),(int)Math.Floor(p.y),(int)Math.Floor(p.z));}
 public struct Color{public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color black=>new Color(0,0,0,1);public static Color white=>new Color(1,1,1,1);}
 public struct Rect{public float x,y,width,height;public Rect(float x,float y,float width,float height){this.x=x;this.y=y;this.width=width;this.height=height;}}
 public class Texture:Object{}
 public class Texture2D:Texture{}
 public class Material:Object{}
 public class Sprite:Object{}
 public class AudioClip:Object {public float length=10;}
 public class AudioSource:Behaviour
 {
  public AudioClip clip;public float volume,time,spatialBlend,pitch,minDistance,maxDistance,dopplerLevel;public AudioRolloffMode rolloffMode;public bool loop,playOnAwake,ignoreListenerPause;
  public Audio.AudioMixerGroup outputAudioMixerGroup;private bool playing,paused;
  public bool isPlaying=>playing&&(!AudioListener.pause||ignoreListenerPause);
  public void Play(){playing=clip!=null;paused=false;}
  public void Stop(){playing=false;paused=false;time=0;}
  public void Pause(){if(playing)paused=true;playing=false;}
  public void UnPause(){if(paused){playing=true;paused=false;}}
  public void Step(float seconds){if(!isPlaying)return;time+=seconds;if(time>=clip.length){if(loop)time%=clip.length;else{time=clip.length;playing=false;}}}
 }
 public static class AudioListener{public static bool pause;}
 public enum AudioRolloffMode{Linear}
 public class Camera:Behaviour{public static Camera main;}
 public static class Time{public static float unscaledDeltaTime=.1f,deltaTime=.1f;public static int frameCount;}
 public static class Application{public static bool isBatchMode;}
 public static class Screen{public static int width=1920,height=1080;}
 public static class Cursor{public static bool visible;public static CursorLockMode lockState;}
 public enum CursorLockMode{None,Locked}
 public static class Resources{public static object hud;public static Dictionary<string,AudioClip[]> audio=new Dictionary<string,AudioClip[]>();public static int reads;public static T Load<T>(string path) where T:class=>path=="PlayerHudAssets"?hud as T:null;public static T[] LoadAll<T>(string path) where T:class {reads++;return audio.ContainsKey(path)?audio[path].Cast<T>().ToArray():new T[0];}}
 public static class Mathf
 {
  public static int Clamp(int x,int min,int max)=>Math.Max(min,Math.Min(max,x));public static float Clamp01(float x)=>Math.Max(0,Math.Min(1,x));
  public static float Clamp(float x,float min,float max)=>Math.Max(min,Math.Min(max,x));public static float Abs(float x)=>Math.Abs(x);
  public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static int FloorToInt(float x)=>(int)Math.Floor(x);
 }
 public class Canvas:Behaviour {public RenderMode renderMode;public int sortingOrder;public bool pixelPerfect;}
 public enum RenderMode{ScreenSpaceOverlay}
 public class DisallowMultipleComponent:Attribute{}
 public class SpaceAttribute:Attribute{}
 public class HeaderAttribute:Attribute{public HeaderAttribute(string s){}}
 public class TooltipAttribute:Attribute{public TooltipAttribute(string s){}}
 public class RangeAttribute:Attribute{public RangeAttribute(float a,float b){}}
 public class MinAttribute:Attribute{public MinAttribute(float a){}}
 namespace Audio {public class AudioMixerGroup:Object{}}
 namespace UI
 {
  public class GraphicRaycaster:Behaviour{}
  public class CanvasScaler:Behaviour{public ScaleMode uiScaleMode;public float scaleFactor;public enum ScaleMode{ConstantPixelSize}}
  public class Image:Behaviour{public Sprite sprite;public Material material;public Color color;public int type;}
  public class RawImage:Behaviour{public Texture texture;public Color color;public Rect uvRect;public bool raycastTarget;}
  public class Selectable:Behaviour{public enum Transition{None}public Transition transition;}
  public class Button:Selectable{public Click onClick=new Click();public class Click{Action action;public void AddListener(Action value){action+=value;}public void Invoke(){action?.Invoke();}}}
 }
 namespace SceneManagement
 {
  public struct Scene{public string name;}
  public enum LoadSceneMode{Single,Additive}
  public static class SceneManager
  {
   public static Scene active=new Scene{name="MainMenu"};public static event Action<Scene,LoadSceneMode> sceneLoaded;
   public static Scene GetActiveScene()=>active;
   public static void LoadScene(string name){active=new Scene{name=name};sceneLoaded?.Invoke(active,LoadSceneMode.Single);}
  }
 }
}
namespace UnityEngine.EventSystems{public class PointerEventData{}public interface IPointerEnterHandler{void OnPointerEnter(PointerEventData data);}public interface IPointerExitHandler{void OnPointerExit(PointerEventData data);}}
namespace TMPro
{
 public class TMP_FontAsset:UnityEngine.Object{}
 public static class TMP_Settings {public static TMP_FontAsset defaultFontAsset=new TMP_FontAsset();}
 public enum TextAlignmentOptions{Center}
 public class TextMeshProUGUI:UnityEngine.Component{public TMP_FontAsset font;public float fontSize;public UnityEngine.Color color;public TextAlignmentOptions alignment;public bool enableWordWrapping,extraPadding,raycastTarget;public UnityEngine.Material fontSharedMaterial;public string text;}
}
namespace Mirror
{
 public class ClientRpcAttribute:Attribute{}
 public enum SceneOperation{Normal,LoadAdditive,UnloadAdditive}
 public class NetworkConnectionToClient{public UnityEngine.GameObject identity;}
 public static class NetworkServer{public static bool active,isLoadingScene;public static void Spawn(UnityEngine.GameObject go){}}
 public static class NetworkClient{public static bool isLoadingScene;public static void Send<T>(T value){}}
 public class NetworkManager:UnityEngine.MonoBehaviour
 {
  public static NetworkManager singleton;public string offlineScene="MainMenu";public bool stopped;public int disconnects;
  public void StopHost(){stopped=true;}public void StopClient(){stopped=true;}
  public virtual void OnClientConnect(){}public virtual void OnStartServer(){}public virtual void OnServerSceneChanged(string name){}
  public virtual void OnServerChangeScene(string name){}public virtual void OnClientChangeScene(string name,SceneOperation operation,bool customHandling){}
  public virtual void OnClientDisconnect(){stopped=true;}public virtual void OnServerDisconnect(NetworkConnectionToClient conn){disconnects++;}
 }
}
public class PlayerHudAssets{public TMPro.TMP_FontAsset levelFont;public UnityEngine.Texture2D loadingBackground,guiWidgets;}
public static class MinecraftUIText{public static UnityEngine.Material CreateMaterial(TMPro.TMP_FontAsset font,float size,bool shadow)=>new UnityEngine.Material();}
public class Menu {public static Menu currentMenu,lastMenu;public static void CloseCurrentMenu(){}}
public class Block {public BlockType type=BlockType.Stone;public int sky=15,light;public int GetSkyLight()=>sky;public int GetBlockLight()=>light;}
public class World {public WorldDimension dimension;public static World Instance;public bool IsWorldCreated,disabled;public WorldData worldData=new WorldData();public Func<UnityEngine.Vector3,Block> blockAt;public Block GetBlock(UnityEngine.Vector3 p)=>blockAt==null?new Block():blockAt(p);}
public class WorldData{public Dictionary<UnityEngine.Vector3Int,ChunkRenderer> chunkDict=new Dictionary<UnityEngine.Vector3Int,ChunkRenderer>();}
public class ChunkRenderer:UnityEngine.Component{public object ChunkData;}
public class GameManager {public static GameManager Instance;public bool playerSpawned;public Player localPlayer;}
public class Player:UnityEngine.Component {public WorldDimension dimension;public bool DimensionTransferInProgress;public bool IsReadyForPlay,isFlying,isCrouching,isLocalPlayer,InWater;public int health=20,experienceLevel;public float eyeHeight=1.6f;public object inventory;public object SavePlayer()=>new object();}
public static class WorldDataHelper
{
 public static UnityEngine.Vector3Int GetChunkPosition(World w,UnityEngine.Vector3Int p)=>new UnityEngine.Vector3Int((int)Math.Floor(p.x/16f)*16,0,(int)Math.Floor(p.z/16f)*16);
}
namespace kcp2k{}
namespace Unity.Mathematics{}
namespace Unity.VisualScripting{}
namespace Steamworks{public static class SteamClient{public static ulong SteamId;}}
public class LobbyManager{public static Lobby? currentLobby;public struct Lobby{public void Leave(){}}}
public partial class WorldServer
{
 public static WorldServer instance;public int chestCloses,furnaceCloses,saves;
 public void CloseChestForDisconnect(Mirror.NetworkConnectionToClient conn){chestCloses++;}
 public void CloseFurnaceForDisconnect(Mirror.NetworkConnectionToClient conn){furnaceCloses++;}
 public void SavePlayerMessageHandler(Mirror.NetworkConnectionToClient conn,object data){saves++;}
 public struct StartPlayerMessage{public StartPlayerMessage(ulong steamId){}}
}

public enum WorldDimension {Nether=-1,Overworld=0}
