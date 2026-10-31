using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace UnityEngine
{
 public class MonoBehaviour
 {
  public string name;
  public static void Destroy(object o){}public static void DontDestroyOnLoad(object o){}
  public object gameObject=>this;
  public T GetComponent<T>()where T:new()=>new T();public T GetComponentInChildren<T>()where T:new()=>new T();public static T[] FindObjectsOfType<T>()=>new T[0];
 }
 public static class Helpers {public static void ExecuteAfterFrames(this MonoBehaviour b,int n,Action a){a();}}
 public class Transform {public Vector3 position;}
 public static class Time {public static float unscaledTime;}
 public static class Mathf {public static float Clamp(float f,float a,float b)=>Math.Max(a,Math.Min(b,f));public static int Clamp(int f,int a,int b)=>Math.Max(a,Math.Min(b,f));public static float Round(float f)=>(float)Math.Round(f);public static int RoundToInt(float f)=>(int)Math.Round(f);}
 public class Camera {public static Camera main;public float fieldOfView;}
 public static class PlayerPrefs {public static Dictionary<string,float> data=new Dictionary<string,float>();public static int saves;public static void SetFloat(string n,float f){data[n]=f;}public static void SetInt(string n,int i){data[n]=i;}public static float GetFloat(string n,float f)=>data.TryGetValue(n,out var v)?v:f;public static int GetInt(string n,int i)=>(int)GetFloat(n,i);public static void Save(){saves++;}}
}
namespace UnityEngine.SceneManagement
{
 public struct Scene{}public enum LoadSceneMode {Single}
 public static class SceneManager {public static event Action<Scene,LoadSceneMode> sceneLoaded;public static void Loaded(){sceneLoaded?.Invoke(new Scene(),LoadSceneMode.Single);}}
}
namespace UnityEngine.UI
{
 public class CanvasScaler {public float scaleFactor;}
 public class Slider
 {
  public class Event {public void AddListener(Action<float> action){}}
  public Event onValueChanged=new Event();public float value;public int notifications;
  public void SetValueWithoutNotify(float v){value=v;}
 }
}
namespace TMPro {public class TextMeshProUGUI {public string text;}}
public class UIToggleButton {public static List<UIToggleButton> toggleButtons=new List<UIToggleButton>();public string name;public void SetValue(int i){}}
public class MusicManager {public static MusicManager Instance;public float volume;public void SetVolume(float f){volume=f;}}
public class Player {public Transform transform=new Transform();public World CurrentWorld;public bool DimensionTransferInProgress;}
public class GameManager {public static GameManager Instance;public Player localPlayer;}
public partial class World
{
 public static World Instance;public static List<World> LoadedDimensions=new List<World>();public float gamma;public int renderDistance=4;
 public bool IsWorldCreated=true,disabled,isSaving,GenerateMoreChunks=true,streamingLoopRunning,streamingRequested;public Vector3Int streamingTarget;
 public int runs;public bool ready=true;private bool HasReadyArea(Vector3Int p)=>ready;private void RunChunkStreaming(){runs++;streamingLoopRunning=true;}
}
public static class Check
{
 static int checks;static void Assert(bool b,string name){checks++;if(!b)throw new Exception(name);}
 static void Call(object o,string name)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(o,null);
 static void Advance(SettingsManager s,float seconds){Time.unscaledTime+=seconds;Call(s,"Update");}
 public static void Main()
 {
  var s=new SettingsManager();s.Awake();s.LoadSettings();MusicManager.Instance=new MusicManager();
  var overworld=new World();var nether=new World();World.Instance=overworld;World.LoadedDimensions.Add(overworld);World.LoadedDimensions.Add(nether);
  var player=new Player{CurrentWorld=overworld};player.transform.position=new Vector3(3,64,5);GameManager.Instance=new GameManager{localPlayer=player};
  s.SetValue("brightness",7);Assert(overworld.gamma==.7f&&nether.gamma==.7f,"brightness reaches active and cached dimensions immediately");Assert(s.settings["brightness"]==7,"menu dictionary immediately current");Assert(PlayerPrefs.saves==0,"no synchronous save per slider event");
  s.SetValue("renderDistance",8);s.SetValue("renderDistance",10);s.SetValue("renderDistance",12);Assert(overworld.renderDistance==12&&nether.renderDistance==12,"distance updated for all worlds");Assert(overworld.runs==0,"distance requests debounced");
  Advance(s,.1f);Assert(overworld.runs==0,"no early streaming");Advance(s,.1f);Assert(overworld.runs==1&&overworld.streamingRequested&&overworld.streamingTarget.x==3,"stationary player forces streaming at latest position");Assert(PlayerPrefs.saves==0,"disk save remains deferred");Advance(s,.5f);Assert(PlayerPrefs.saves==1&&PlayerPrefs.data["renderDistance"]==12,"changes persist after debounce");
  overworld.streamingLoopRunning=false;overworld.streamingRequested=false;overworld.RequestChunkStreaming(Vector3Int.zero);Assert(!overworld.streamingRequested,"ordinary ready-area request stays cheap");
  s.SetValue("renderDistance",2);Advance(s,.2f);Assert(overworld.runs==2,"shrinking an already ready area forces eviction pass");
  s.SetValue("renderDistance",3);s.SetValue("renderDistance",4);Advance(s,.2f);Assert(overworld.runs==2&&overworld.streamingRequested,"active worker coalesces latest request rather than starting another loop");
  World.LoadedDimensions.Remove(nether);nether.gamma=0;nether.renderDistance=1;World.Instance=nether;player.CurrentWorld=nether;Advance(s,.01f);Assert(nether.gamma==.7f&&nether.renderDistance==4,"newly activated dimension inherits current values");
  nether.isSaving=true;Advance(s,.2f);Assert(nether.runs==0,"pending refresh waits for save");nether.isSaving=false;player.DimensionTransferInProgress=true;Advance(s,.2f);Assert(nether.runs==0,"refresh waits for player transfer");player.DimensionTransferInProgress=false;Advance(s,.2f);Assert(nether.runs==1,"refresh resumes after blockers clear");
  s.SetValue("brightness",float.NaN);Assert(s.brightness==7,"nonfinite input rejected");s.SetValue("renderDistance",1000);Assert(s.renderDistance==16,"distance bounded to menu maximum");s.SetValue("musicVolume",25);Assert(MusicManager.Instance.volume==.25f,"music remains live");
  int saves=PlayerPrefs.saves;s.SetValue("brightness",8);s.SetValue("brightness",8);Advance(s,.6f);Assert(PlayerPrefs.saves==saves+1,"same value does not cause repeated writes");
  var slider=new UISlider{name="brightness"};Call(slider,"Awake");slider.SetValue(8);Assert(s.brightness==8,"programmatic slider synchronization does not mutate settings");
  s.SetValue("brightness",9);s.ApplySettings();Assert(PlayerPrefs.data["brightness"]==9&&overworld.gamma==.9f,"Done still persists and applies");
  Call(s,"OnDestroy");Assert(SettingsManager.instance==null,"settings cleanup resets singleton");
  Console.WriteLine(checks+" live brightness/distance/debounce/dimension/streaming/persistence checks passed (Unity mocked).");
 }
}
