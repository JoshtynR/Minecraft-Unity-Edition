using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityEngine {
 public class ContextMenu:Attribute { public ContextMenu(string label){} }
 public class Component {
  public GameObject gameObject;
  public Transform transform { get { return gameObject.transform; } }
  public T GetComponent<T>() where T:class { return gameObject.GetComponent<T>(); }
  public T GetComponentInChildren<T>(bool inactive=false) where T:class { return gameObject.GetComponentsInChildren<T>().FirstOrDefault(); }
  public T[] GetComponentsInChildren<T>() where T:class { return gameObject.GetComponentsInChildren<T>(); }
 }
 public class MonoBehaviour:Component {
  protected static void Destroy(GameObject go){ go.transform.parent.children.Remove(go.transform); }
  protected static GameObject Instantiate(GameObject prefab,Transform parent){
   var go=new GameObject("row",parent);go.Add(new WorldButton());
   new GameObject("name",go.transform).Add(new TMPro.TextMeshProUGUI());
   new GameObject("date",go.transform).Add(new TMPro.TextMeshProUGUI());return go;
  }
 }
 public class GameObject {
  public string name;public Transform transform;readonly List<Component> components=new List<Component>();
  public GameObject(string name,Transform parent=null){this.name=name;transform=new Transform{gameObject=this,parent=parent};if(parent!=null)parent.children.Add(transform);}
  public T Add<T>(T value) where T:Component {value.gameObject=this;components.Add(value);return value;}
  public T GetComponent<T>() where T:class {return components.OfType<T>().FirstOrDefault();}
  public T[] GetComponentsInChildren<T>() where T:class {return components.OfType<T>().Concat(transform.children.SelectMany(c=>c.gameObject.GetComponentsInChildren<T>())).ToArray();}
 }
 public class Transform:Component {
  public Transform parent;public List<Transform> children=new List<Transform>();
  public int childCount {get{return children.Count;}}public Transform GetChild(int i){return children[i];}
  public Transform Find(string path){Transform node=this;foreach(var part in path.Split('/')){node=node.children.FirstOrDefault(c=>c.gameObject.name==part);if(node==null)return null;}return node;}
 }
 public static class Debug {public static int errors,warnings;public static void LogError(string s,object context){errors++;}public static void LogWarning(string s,object context){Interlocked.Increment(ref warnings);}}
 public static class Resources {public static PlayerHudAssets assets;public static T Load<T>(string path) where T:class{return assets as T;}}
 public static class JsonUtility {public static T FromJson<T>(string s) where T:class {if(s!="{}")throw new FormatException();return new WorldSaveData() as T;}}
}
namespace UnityEngine.UI {public class ScrollRect:Component {public Transform content;}}
namespace TMPro {public class TextMeshProUGUI:Component {public string text;}}
namespace Mirror {}
public class PlayerHudAssets {public GameObject worldListEntryPrefab;}
public class Menu:Component {public bool opened;public void Open(){opened=true;}}
public class WorldButton:Component {public static WorldButton currentWorldButton;public Action onClick;}
public class UIButton:Component {public void Enable(){}}
public class WorldCreationMenuManager:Component {public bool multiplayerMode;public static void CreateLobby(string n,int s){}}
public class WorldSettingsManager {public static WorldSettingsManager Instance=new WorldSettingsManager();public Task LoadWorldSettings(WorldSaveData d){return Task.CompletedTask;}}
public static class MinecraftNetworkManager {public static void StartLocalWorld(){}public static void StartGameHost(){}}
public class WorldSaveData {public string worldName;public int seedOffset;}
public static class World {public static string Decompress(string s){throw new FormatException("bad save");}}
public static class MenuEnvironment {
 public enum SpecialFolder {ApplicationData}public static string root;
 public static string GetFolderPath(SpecialFolder folder){return root;}
}
public static class Check {
 static int checks;
 static void Assert(bool ok,string label){if(!ok)throw new Exception(label);checks++;}
 public static void Main(string[] args){
  MenuEnvironment.root=args[0];
  var canvas=new GameObject("Canvas");var list=new GameObject("WorldListMenu",canvas.transform);
  var manager=list.Add(new WorldListMenuManager());manager.multiplayerMode=true;
  var scroll=new GameObject("Scroll View",list.transform);
  var viewport=new GameObject("Viewport",scroll.transform);
  var content=new GameObject("Content",viewport.transform).transform;
  var sr=scroll.Add(new UnityEngine.UI.ScrollRect{content=content});
  var joining=new GameObject("JoiningMenu",canvas.transform).Add(new Menu());
  var creation=new GameObject("WorldCreationMenu",canvas.transform);var menu=creation.Add(new Menu());var creator=creation.Add(new WorldCreationMenuManager());
  var prefab=new GameObject("prefab");Resources.assets=new PlayerHudAssets{worldListEntryPrefab=prefab};
  manager.OpenCreationMenu();
  Assert(menu.opened&&creator.multiplayerMode,"Create opens and receives multiplayer mode");
  Assert(manager.worldContainer==content&&manager.joiningMenu==joining&&manager.worldPrefab==prefab,"Missing scene links recovered");
  var custom=new GameObject("Custom").transform;manager.worldContainer=custom;manager.OpenCreationMenu();Assert(manager.worldContainer==custom,"Custom references preserved");
  manager.worldContainer=null;sr.content=null;manager.OpenCreationMenu();Assert(manager.worldContainer==content,"Hierarchy fallback without ScrollRect content");
  var saves=Path.Combine(args[0],".minecraftUnity/saves");Directory.CreateDirectory(Path.Combine(saves,"good"));Directory.CreateDirectory(Path.Combine(saves,"bad"));Directory.CreateDirectory(Path.Combine(saves,"incomplete"));
  File.WriteAllText(Path.Combine(saves,"good/world.json"),"{}");File.WriteAllText(Path.Combine(saves,"bad/world.json"),"corrupt");
  manager.GetWorlds();var until=DateTime.UtcNow.AddSeconds(5);while((content.childCount!=1||Debug.warnings!=1)&&DateTime.UtcNow<until)Thread.Sleep(5);
  Assert(content.childCount==1&&Debug.warnings==1,"Unreadable and incomplete saves do not hide valid saves");
  Assert(content.GetChild(0).GetComponentInChildren<TMPro.TextMeshProUGUI>().text=="good","Missing name falls back to save folder");
  var broken=new GameObject("Broken").Add(new WorldListMenuManager());broken.OpenCreationMenu();broken.GetWorlds();Assert(Debug.errors==2,"Missing UI reports actionable errors without throwing");
  Console.WriteLine(checks+" world-menu checks passed.");
 }
}
