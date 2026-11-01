using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Steamworks.Data;

namespace UnityEngine
{
    public class GameObject
    {
        public string name;
        public bool activeSelf = true;
        public Transform transform;
        private readonly Dictionary<Type,object> components = new Dictionary<Type,object>();
        public GameObject(string name="object") { this.name=name;transform=new Transform(this); }
        public static GameObject Find(string name) { return null; }
        public void SetActive(bool active) { activeSelf=active; }
        public T Add<T>(T component) where T:class { components[typeof(T)]=component;var m=component as MonoBehaviour;if(m!=null)m.gameObject=this;return component; }
        public T GetComponent<T>() where T:class {return components.TryGetValue(typeof(T),out var c)?c as T:null;}
        public T[] GetComponentsInChildren<T>(bool inactive=false) where T:class {return components.Values.OfType<T>().Concat(transform.children.SelectMany(c=>c.gameObject.GetComponentsInChildren<T>(inactive))).ToArray();}
        public T GetComponentInChildren<T>() where T:class {return GetComponentsInChildren<T>().FirstOrDefault();}
        public GameObject Clone() {
            var obj=new GameObject(name);
            foreach(var c in components.Values) {
                if(c is TMPro.TextMeshProUGUI)obj.Add(new TMPro.TextMeshProUGUI());
                if(c is RectTransform)obj.Add(new RectTransform());
                if(c is LobbyButton)obj.Add(new LobbyButton());
            }
            foreach(var c in transform.children)c.gameObject.Clone().transform.SetParent(obj.transform);
            return obj;
        }
    }
    public class Transform {public GameObject gameObject;public List<Transform> children=new List<Transform>();private Transform parent;public Transform(GameObject obj){gameObject=obj;}public int childCount=>children.Count;public Transform GetChild(int i)=>children[i];public void SetParent(Transform next){parent?.children.Remove(this);parent=next;parent?.children.Add(this);}}
    public class MonoBehaviour {
        public GameObject gameObject=new GameObject();public Transform transform=>gameObject.transform;
        public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();
        public static GameObject Instantiate(GameObject prefab,Transform parent){var obj=prefab.Clone();obj.transform.SetParent(parent);return obj;}
        public static void Destroy(GameObject obj){}
    }
    public struct Vector3Int {public int x,y,z;}
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
    public class RectTransform {public Vector2 sizeDelta;}
    public enum CursorLockMode {None,Locked}
    public static class Cursor {public static CursorLockMode lockState;}
    public enum KeyCode {Escape}
    public static class Input {public static bool GetKeyDown(KeyCode key)=>false;}
    public static class Time {public static float realtimeSinceStartup;}
    public static class Debug {public static void LogWarning(object message){}}
}
namespace UnityEngine.EventSystems {
    public class PointerEventData {public int clickCount;}
    public interface IPointerDownHandler {void OnPointerDown(PointerEventData e);}
    public interface IPointerClickHandler {void OnPointerClick(PointerEventData e);}
}
namespace TMPro {public class TextMeshProUGUI {public string text;public bool richText=true;}}
namespace Newtonsoft.Json {public static class JsonConvert {public static string SerializeObject(object value)=>"seed-json";public static T DeserializeObject<T>(string value){if(value!="seed-json")throw new Exception("Invalid seed");return default(T);}}}
namespace Mirror {
    public class Transport {}
    public static class NetworkServer {public static bool active,dontListen;}
    public static class NetworkClient {public static bool active;}
    public class NetworkManager {
        public static NetworkManager singleton=new NetworkManager();public int maxConnections=8,hosts,clients;public string networkAddress;public Transport transport=new Mirror.FizzySteam.FizzyFacepunch();public Action beforeHost;
        public void StartHost(){hosts++;beforeHost?.Invoke();NetworkServer.active=NetworkClient.active=true;}
        public void StartClient(){clients++;NetworkClient.active=true;}
        public void StopHost(){NetworkServer.active=NetworkClient.active=false;LobbyManager.Leave();}
        public void StopClient(){NetworkClient.active=false;LobbyManager.Leave();}
    }
}
namespace Mirror.FizzySteam {public class FizzyFacepunch:Mirror.Transport {}}
public static class MinecraftNetworkManager {public static bool failHost;public static void PrepareClientSession(){}public static void StartGameHost(bool localOnly=true){if(failHost){LobbyManager.Leave();return;}Mirror.NetworkServer.dontListen=localOnly;Mirror.NetworkManager.singleton.StartHost();}}
public static class PlayerIdentity {public static bool SteamAvailable=true;}
public class World {public static World Overworld;public bool IsWorldCreated;}
public class WorldSettingsManager {public static WorldSettingsManager Instance=new WorldSettingsManager();public Vector3Int seedOffset;public long betaWorldSeed=123;public double worldTimeTicks=1000;public string worldName;}
public class LobbyCreationMenu {public static string lobbyName="Friends world";}
public class Menu {public static Menu currentMenu,lastMenu;public bool opened=true;public GameObject gameObject=new GameObject("MultiplayerMenu");public void Open(){opened=true;}public static void CloseCurrentMenu(){currentMenu=lastMenu;}}
public static class WorldLoadingScreen {public static void Begin(){}public static void Cancel(){}}

#if !SDK
namespace Steamworks {
    public enum RoomEnter {Success,Full,Error}
    public struct SteamId {
        public ulong Value;public static implicit operator SteamId(ulong v)=>new SteamId{Value=v};public static implicit operator ulong(SteamId v)=>v.Value;public override string ToString()=>Value.ToString();
    }
    public struct Friend {
        public SteamId Id;public bool IsFriend,IsMe,IsPlayingThisGame;public string Name;public FriendGameInfo? GameInfo;
        public struct FriendGameInfo {public Lobby? Lobby;}
    }
    public static class SteamClient {public static SteamId SteamId=10;}
    public static class SteamFriends {
        public static List<Friend> friends=new List<Friend>();public static IEnumerable<Friend> GetFriends()=>friends;
        public static event Action<Lobby,SteamId> OnGameLobbyJoinRequested;
    }
    public static class SteamMatchmaking {
        public static Task<Lobby?> create;public static int capacity;public static Action created;
        public static Task<Lobby?> CreateLobbyAsync(int size){capacity=size;created?.Invoke();return create;}
    }
}
namespace Steamworks.Data {
    public class LobbyState {public Dictionary<string,string> data=new Dictionary<string,string>();public Friend owner;public int members=1,max=8,leaves,joins,refreshes;public bool friendsOnly,joinable=true;public Task<RoomEnter> join=Task.FromResult(RoomEnter.Success);}
    public struct Lobby {
        public SteamId Id;public static Dictionary<ulong,LobbyState> states=new Dictionary<ulong,LobbyState>();public Lobby(ulong id){Id=id;}
        public LobbyState State=>states[Id];public Friend Owner=>State.owner;public int MemberCount=>State.members;public int MaxMembers=>State.max;
        public bool IsOwnedBy(SteamId id)=>(ulong)Owner.Id==(ulong)id;
        public bool SetFriendsOnly(){State.friendsOnly=true;return true;}public bool SetJoinable(bool value){State.joinable=value;return true;}
        public bool SetData(string key,string value){State.data[key]=value;return true;}public string GetData(string key)=>State.data.TryGetValue(key,out var v)?v:"";
        public bool Refresh(){State.refreshes++;return true;}public Task<RoomEnter> Join(){State.joins++;return State.join;}public void Leave(){State.leaves++;}
    }
}
#endif
