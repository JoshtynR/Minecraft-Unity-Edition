using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Steamworks;
using UnityEngine;

namespace UnityEngine
{
    public class GameObject
    {
        private readonly Dictionary<Type,object> components = new Dictionary<Type,object>();
        public T GetComponent<T>() where T:class { object found;return components.TryGetValue(typeof(T),out found)?found as T:null; }
        public T AddComponent<T>() where T:class,new() {var item=new T();components[typeof(T)]=item;return item;}
    }
    public class MonoBehaviour
    {
        public static void Destroy(GameObject value){}
        public GameObject gameObject=new GameObject();
        public T GetComponent<T>() where T:class=>gameObject.GetComponent<T>();
        public static GameObject Instantiate(GameObject prefab)=>new GameObject();
        public Coroutine StartCoroutine(IEnumerator iterator){var c=new Coroutine{iterator=iterator};if(iterator.MoveNext())Scheduler.running.Add(c);return c;}
        public void StopCoroutine(Coroutine c){Scheduler.running.Remove(c);}
    }
    public class Coroutine {public IEnumerator iterator;}
    public static class Scheduler {public static List<Coroutine> running=new List<Coroutine>();public static void Step(){foreach(var c in running.ToArray())if(!c.iterator.MoveNext())running.Remove(c);}}
    public static class Time {public static float realtimeSinceStartup;}
    public class SpaceAttribute:Attribute {}
    public static class Debug {public static readonly List<string> warnings=new List<string>();public static void LogWarning(object message){warnings.Add(message.ToString());}public static void Log(object m){}public static void LogError(object m){warnings.Add(m.ToString());}}
    public static class PlayerPrefs
    {
        public static readonly Dictionary<string,string> values=new Dictionary<string,string>();
        public static int saves;
        public static string GetString(string key,string fallback)=>values.ContainsKey(key)?values[key]:fallback;
        public static void SetString(string key,string value){values[key]=value;}
        public static void Save(){saves++;}
    }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene {public string name;}
    public static class SceneManager {public static Scene GetActiveScene()=>new Scene{name="MainMenu"};public static void LoadScene(string name){}}
}
namespace Unity.Mathematics {}
namespace Unity.VisualScripting {}
namespace Steamworks
{
    public struct SteamId
    {
        public ulong Value;
        public static implicit operator SteamId(ulong value)=>new SteamId{Value=value};
        public static implicit operator ulong(SteamId value)=>value.Value;
    }
    public static class SteamClient
    {
        public static bool valid,throwValidity,throwName,throwInit;
        public static ulong steamId=76561198000000123;
        public static string name="Steam Josh";
        public static int nameReads,initializations;
        public static bool IsValid {get{if(throwValidity)throw new DllNotFoundException();return valid;}}
        public static SteamId SteamId {get{if(!valid)throw new NullReferenceException("Steam unavailable");return steamId;}}
        public static string Name {get{nameReads++;if(throwName||!valid)throw new NullReferenceException();return name;}}
        public static void Init(uint appId){initializations++;if(throwInit)throw new DllNotFoundException();valid=true;}
    }
}
namespace Mirror
{
    public enum SceneOperation {Normal}
    public class Transport {public static Transport activeTransport;public bool enabled;}
    public class NetworkIdentity:MonoBehaviour {}
    public class NetworkConnectionToClient {public NetworkIdentity identity;}
    public static class NetworkServer {public static bool active,dontListen;public static int shutdowns;public static void Spawn(GameObject obj){}public static void Shutdown(){shutdowns++;active=false;dontListen=false;}}
    public static class NetworkClient
    {
        public static bool active,isConnected=true,isLoadingScene;
        public static readonly List<object> messages=new List<object>();
        public static void Send<T>(T message){messages.Add(message);}
    }
    public class NetworkManager:MonoBehaviour
    {
        public virtual void Awake(){}
        public static NetworkManager singleton;
        public Transport transport;public string networkAddress="localhost",offlineScene="MainMenu";public int starts,bindAttempts;public bool portOccupied;public Action duringStart;
        public void StartHost(){starts++;duringStart?.Invoke();if(!NetworkServer.dontListen){bindAttempts++;if(portOccupied)throw new System.Net.Sockets.SocketException((int)System.Net.Sockets.SocketError.AddressAlreadyInUse);}NetworkServer.active=NetworkClient.active=true;}
        public void StopHost(){NetworkServer.Shutdown();StopClient();}public void StopClient(){NetworkClient.active=false;OnStopClient();}
        public virtual void OnClientConnect(){}
        public virtual void OnClientSceneChanged(){}
        public virtual void OnStartServer(){}
        public virtual void OnServerSceneChanged(string name){}
        public virtual void OnServerChangeScene(string name){}
        public virtual void OnClientChangeScene(string name,SceneOperation operation,bool custom){}
        public virtual void OnClientDisconnect(){}
        public virtual void OnServerDisconnect(NetworkConnectionToClient conn){}
        public virtual void OnStopClient(){}
    }
}
namespace Mirror.FizzySteam {public class FizzyFacepunch:Transport {}}
namespace kcp2k {public class KcpTransport:Transport {}}
public class Menu{public static Menu currentMenu,lastMenu;public static int returns;public static void CloseCurrentMenu(){returns++;}}
public static class WorldLoadingScreen {public static int cancels;public static void Begin(){}public static void Cancel(){cancels++;}public static void ReportProgress(string s,float value){}}
public class World {public static World Overworld;public bool IsInitialized;}
public static class LobbyManager {public static Lobby? currentLobby;public struct Lobby {public void Leave(){}}}
public class PlayerInventory {}
public class Player {public PlayerInventory inventory;public bool DimensionTransferInProgress;public object SavePlayer()=>null;}
public class WorldServer
{
    public static WorldServer instance;
    public struct StartPlayerMessage {public SteamId steamId;public StartPlayerMessage(SteamId id){steamId=id;}}
    public void CloseChestForDisconnect(NetworkConnectionToClient c){}
    public void CloseFurnaceForDisconnect(NetworkConnectionToClient c){}
    public void SavePlayerMessageHandler(NetworkConnectionToClient c,object state){}
}

public static class OfflineCheck
{
    static int checks;
    static void Check(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
    static void Reset()
    {
        PlayerIdentity.EndSession();SteamClient.valid=false;SteamClient.throwValidity=false;
        SteamClient.throwName=false;SteamClient.throwInit=false;SteamClient.name="Steam Josh";
        SteamClient.steamId=76561198000000123;SteamClient.nameReads=0;
        NetworkServer.active=NetworkClient.active=false;
        NetworkClient.isConnected=true;NetworkClient.isLoadingScene=false;
        Scheduler.running.Clear();World.Overworld=null;WorldServer.instance=null;
    }
    public static void Main()
    {
        Reset();PlayerPrefs.values.Clear();PlayerIdentity.BeginSession();
        Check(!PlayerIdentity.UsingSteam&&(ulong)PlayerIdentity.Id==1,"offline patch save ID retained");
        Check(PlayerIdentity.DisplayName==Environment.UserName,"Windows username is used offline");
        Check(SteamClient.nameReads==0,"invalid Steam name getter never called");
        Check(PlayerPrefs.values["Minecraft.OfflinePlayerId"]=="1","offline identity persisted in OS-user preferences");
        SteamClient.valid=true;
        Check(!PlayerIdentity.UsingSteam&&(ulong)PlayerIdentity.Id==1,"Steam becoming available does not switch an active save");
        PlayerIdentity.BeginSession();
        Check(PlayerIdentity.UsingSteam&&(ulong)PlayerIdentity.Id==SteamClient.steamId&&PlayerIdentity.DisplayName=="Steam Josh","next session uses real Steam identity");
        SteamClient.valid=false;
        Check(PlayerIdentity.DisplayName=="Steam Josh"&&(ulong)PlayerIdentity.Id==SteamClient.steamId,"Steam loss keeps the connection identity stable");
        Reset();SteamClient.throwValidity=true;PlayerIdentity.BeginSession();
        Check(!PlayerIdentity.UsingSteam&&(ulong)PlayerIdentity.Id==1,"missing native Steam DLL falls back safely");
        Reset();SteamClient.valid=true;SteamClient.throwName=true;PlayerIdentity.BeginSession();
        Check(!PlayerIdentity.UsingSteam,"failed Steam getters fall back safely");
        Reset();SteamClient.valid=true;SteamClient.steamId=0;PlayerIdentity.BeginSession();
        Check(!PlayerIdentity.UsingSteam,"zero Steam identity falls back");
        Reset();PlayerPrefs.values["Minecraft.OfflinePlayerId"]="0";PlayerIdentity.BeginSession();
        Check((ulong)PlayerIdentity.Id==1,"invalid offline preference repaired");
        Reset();PlayerPrefs.values["Minecraft.OfflinePlayerId"]="123";PlayerIdentity.BeginSession();
        Check((ulong)PlayerIdentity.Id==123,"persisted local identity survives sessions");
        PlayerPrefs.values.Clear();Reset();SteamClient.throwInit=true;PlayerIdentity.TryInitializeSteam(480);
        Check(!PlayerIdentity.SteamAvailable,"native initialization failure leaves offline startup available");
        Reset();SteamClient.valid=true;int init=SteamClient.initializations;PlayerIdentity.TryInitializeSteam(480);
        Check(SteamClient.initializations==init,"already initialized Steam is not initialized twice");

        Reset();var manager=new MinecraftNetworkManager();NetworkManager.singleton=manager;
        var steam=new Mirror.FizzySteam.FizzyFacepunch();manager.transport=steam;
        var kcp=manager.gameObject.AddComponent<kcp2k.KcpTransport>();
        MinecraftNetworkManager.StartGameHost();
        Check(manager.starts==1&&manager.transport==kcp&&Transport.activeTransport==kcp,"offline host selects KCP before starting Mirror");
        Check(kcp.enabled&&manager.networkAddress=="localhost","local transport enabled");
        manager.OnClientConnect();
        Scheduler.Step();Check(NetworkClient.messages.Count==0,"connect before world scene does not send an unhandled request");
        World.Overworld=new World();Scheduler.Step();Check(NetworkClient.messages.Count==0,"world Awake alone is insufficient");
        World.Overworld.IsInitialized=true;Scheduler.Step();Check(NetworkClient.messages.Count==0,"host waits for server handlers");
        WorldServer.instance=new WorldServer();NetworkClient.isLoadingScene=true;Scheduler.Step();Check(NetworkClient.messages.Count==0,"request waits for scene activation");
        NetworkClient.isLoadingScene=false;Scheduler.Step();
        Check((ulong)((WorldServer.StartPlayerMessage)NetworkClient.messages[0]).steamId==1,"world startup sends offline ID without Steam");
        manager.OnClientSceneChanged();Scheduler.Step();Check(NetworkClient.messages.Count==1,"connect and scene callbacks send only one request");
        MinecraftNetworkManager.StartGameHost();Check(manager.starts==1,"active host is not restarted");
        manager.OnStopClient();NetworkServer.active=NetworkClient.active=false;SteamClient.valid=true;
        MinecraftNetworkManager.StartGameHost();
        Check(manager.transport==steam&&Transport.activeTransport==steam&&PlayerIdentity.UsingSteam,"next Steam session restores configured Steam transport");
        manager.OnClientConnect();
        Scheduler.Step();
        Check((ulong)((WorldServer.StartPlayerMessage)NetworkClient.messages[1]).steamId==SteamClient.steamId,"Steam startup retains the real account ID");
        manager.OnStopClient();NetworkServer.active=NetworkClient.active=false;SteamClient.valid=false;
        var oldScene=new MinecraftNetworkManager{transport=new Mirror.FizzySteam.FizzyFacepunch()};
        NetworkManager.singleton=oldScene;MinecraftNetworkManager.StartGameHost();
        Check(oldScene.transport is kcp2k.KcpTransport,"older scene without KCP receives a local transport");
        NetworkClient.messages.Clear();World.Overworld=new World{IsInitialized=true};WorldServer.instance=new WorldServer();
        oldScene.OnClientSceneChanged();Scheduler.Step();Check(NetworkClient.messages.Count==1,"scene callback can initiate startup without connect callback");
        oldScene.OnStopClient();World.Overworld=null;oldScene.OnClientConnect();oldScene.OnStopClient();
        World.Overworld=new World{IsInitialized=true};Scheduler.Step();Check(NetworkClient.messages.Count==1,"cancel while waiting never sends a stale request");
        NetworkServer.active=false;NetworkClient.active=true;NetworkClient.messages.Clear();WorldServer.instance=null;
        oldScene.OnClientConnect();Scheduler.Step();Check(NetworkClient.messages.Count==1,"remote client does not require a local WorldServer");
        Reset();var occupied=new MinecraftNetworkManager{transport=new kcp2k.KcpTransport(),portOccupied=true};
        NetworkManager.singleton=occupied;
        occupied.duringStart=()=>MinecraftNetworkManager.StartGameHost();
        MinecraftNetworkManager.StartGameHost();
        Check(occupied.starts==1&&occupied.bindAttempts==0&&NetworkServer.active&&NetworkServer.dontListen,"single-player bypasses occupied ports and rejects reentrant startup");
        occupied.StopHost();occupied.duringStart=null;
        Menu.currentMenu=new Menu();Menu.lastMenu=new Menu();int cancels=WorldLoadingScreen.cancels,returns=Menu.returns,shutdowns=NetworkServer.shutdowns;
        MinecraftNetworkManager.StartGameHost(false);
        Check(occupied.bindAttempts==1&&!NetworkServer.active&&!NetworkClient.active,"multiplayer bind failure leaves no active network session");
        Check(NetworkServer.shutdowns==shutdowns+1&&WorldLoadingScreen.cancels==cancels+1&&Menu.returns==returns+1,"partial listen failure cleans up and restores the menu");
        occupied.portOccupied=false;MinecraftNetworkManager.StartGameHost(false);
        Check(NetworkServer.active&&!NetworkServer.dontListen&&occupied.bindAttempts==2,"multiplayer can retry after failure and still listens externally");
        Console.WriteLine(checks+" production offline identity / Steam / host transport checks passed (native Steam and Unity mocked).");
    }
}
