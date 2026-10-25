using System;
using System.Collections;
using System.IO;
using kcp2k;
using Mirror;
using Steamworks;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MinecraftNetworkManager : NetworkManager
{
    [Space] 
    public GameObject WorldServerPrefab;
    private Transport steamTransport;
    private Coroutine worldRequestRoutine;
    private bool worldRequested;
    private static bool startingHost;
    public static bool OfflineSessionRequested { get; private set; }

    // Normal local worlds do not need a Mirror server or a bound socket. LAN is
    // an explicit opt-in because it deliberately makes this machine the host.
    public static void StartLocalWorld()
    {
        if (SettingsManager.LanEnabled) StartLanHost();
        else StartOfflineWorld();
    }

    public static void StartOfflineWorld()
    {
        if (startingHost || NetworkServer.active || NetworkClient.active)
        {
            Debug.LogWarning("World startup requested while a network session is already active. Stop the current session before starting another world.");
            return;
        }
        startingHost = true;
        try
        {
            if (singleton == null) throw new InvalidOperationException("The scene has no active NetworkManager.");
            PlayerIdentity.BeginSession();
            OfflineSessionRequested = true;
            NetworkServer.dontListen = true;
            WorldLoadingScreen.Begin();
            string worldScene = string.IsNullOrEmpty(singleton.onlineScene) ? "World" : singleton.onlineScene;
            SceneManager.LoadScene(worldScene);
        }
        catch (Exception error)
        {
            OfflineSessionRequested = false;
            PlayerIdentity.EndSession();
            WorldLoadingScreen.Cancel();
            Debug.LogError("Cannot start offline world: " + error);
        }
        finally { startingHost = false; }
    }

    // Used when the World scene is entered directly in the editor. The scene is
    // already loaded, so only establish the session identity and offline mode.
    public static void StartOfflineWorldInLoadedScene()
    {
        if (NetworkServer.active || NetworkClient.active) return;
        PlayerIdentity.BeginSession();
        OfflineSessionRequested = true;
        NetworkServer.dontListen = true;
    }

    public static void StartLanHost()
    {
        StartNetworkHost(false, true);
    }

    public static void EndOfflineSession()
    {
        if (!OfflineSessionRequested) return;
        OfflineSessionRequested = false;
        PlayerIdentity.EndSession();
    }

    // All local host entry points go through this before Mirror starts listening.
    public static void StartGameHost(bool localOnly = true)
    {
        StartNetworkHost(localOnly, false);
    }

    private static void StartNetworkHost(bool localOnly, bool forceLocalTransport)
    {
        if (startingHost || NetworkServer.active || NetworkClient.active)
        {
            Debug.LogWarning("World startup requested while a network session is already active. Stop the current session before starting another world.");
            return;
        }
        startingHost=true;
        try
        {
            if(singleton==null)throw new InvalidOperationException("The scene has no active NetworkManager.");
            OfflineSessionRequested = false;
            PrepareClientSession(forceLocalTransport);
            // A local-only compatibility host must not reserve a public endpoint.
            // LAN and Steam hosts deliberately listen for remote connections.
            NetworkServer.dontListen=localOnly;
            singleton.StartHost();
        }
        catch(Exception error)
        {
            Debug.LogError("Cannot start world host: "+error);
            try
            {
                if(singleton!=null)
                {
                    if(NetworkServer.active)singleton.StopHost();
                    else
                    {
                        // Listen can throw after Initialize but before active=true.
                        // Shutdown still releases the partially created socket.
                        NetworkServer.Shutdown();
                        singleton.StopClient();
                    }
                }
            }
            catch(Exception cleanupError){Debug.LogWarning("Host startup cleanup: "+cleanupError.Message);}
            WorldLoadingScreen.Cancel();
            PlayerIdentity.EndSession();
            bool inWorld=SceneManager.GetActiveScene().name.Contains("World");
            if(inWorld&&(singleton==null||string.IsNullOrEmpty(singleton.offlineScene)))SceneManager.LoadScene("MainMenu");
            else if(!inWorld&&Menu.currentMenu!=null&&Menu.lastMenu!=null)Menu.CloseCurrentMenu();
        }
        finally{startingHost=false;}
    }

    public static void PrepareClientSession(bool forceLocalTransport = false)
    {
        PlayerIdentity.BeginSession();
        var manager = singleton as MinecraftNetworkManager;
        if (manager == null) return;
        manager.ConfigureSessionTransport(forceLocalTransport);
    }

    private void ConfigureSessionTransport(bool forceLocalTransport)
    {
        if (transport is Mirror.FizzySteam.FizzyFacepunch) steamTransport = transport;
        if (PlayerIdentity.UsingSteam && !forceLocalTransport)
        {
            if (steamTransport != null) transport = steamTransport;
        }
        else
        {
            // LAN and offline-profile hosts use KCP even when Steam is running.
            // The normal prefab already contains it; also support older scenes.
            var localTransport = transport as KcpTransport;
            if (localTransport == null) localTransport = GetComponent<KcpTransport>();
            if (localTransport == null) localTransport = gameObject.AddComponent<KcpTransport>();
            localTransport.enabled = true;
            transport = localTransport;
            networkAddress = "localhost";
        }
        Transport.activeTransport = transport;
    }

    public override void OnStopClient()
    {
        if(worldRequestRoutine!=null)StopCoroutine(worldRequestRoutine);
        worldRequestRoutine=null;
        worldRequested=false;
        base.OnStopClient();
        PlayerIdentity.EndSession();
    }


    public override void OnClientConnect()
    {
        WorldLoadingScreen.Begin();
        base.OnClientConnect();
        QueueWorldRequest();
    }

    public override void OnClientSceneChanged()
    {
        base.OnClientSceneChanged();
        QueueWorldRequest();
    }

    private void QueueWorldRequest()
    {
        if(!worldRequested&&worldRequestRoutine==null)
            worldRequestRoutine=StartCoroutine(RequestWorldWhenReady());
    }

    private IEnumerator RequestWorldWhenReady()
    {
        // Mirror can connect before the online scene's Start methods have run.
        // Let World register its reply handler, and the host register its server
        // handlers, before sending a request which otherwise has no receiver.
        yield return null;
        float warningAt=Time.realtimeSinceStartup+15;
        while(NetworkClient.active)
        {
            var world=World.Overworld;
            bool sceneReady=world!=null&&world.IsInitialized&&!NetworkClient.isLoadingScene;
            bool serverReady=!NetworkServer.active||WorldServer.instance!=null;
            if(NetworkClient.isConnected&&sceneReady&&serverReady)
            {
                worldRequested=true;
                WorldLoadingScreen.ReportProgress("Building terrain",.01f);
                Debug.Log("[WorldStartup] World initialized; requesting terrain from server.");
                NetworkClient.Send(new WorldServer.StartPlayerMessage(PlayerIdentity.Id));
                break;
            }
            WorldLoadingScreen.ReportProgress(sceneReady?"Waiting for world server":"Loading world scene",0);
            if(Time.realtimeSinceStartup>=warningAt)
            {
                Debug.LogWarning($"[WorldStartup] Waiting for startup: connected={NetworkClient.isConnected}, worldInitialized={sceneReady}, serverReady={serverReady}.");
                warningAt=Time.realtimeSinceStartup+15;
            }
            yield return null;
        }
        worldRequestRoutine=null;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (SceneManager.GetActiveScene().name == "World")
        {
            OnServerSceneChanged("World");
        }
    }

    public override void OnServerSceneChanged(string sceneName)
    {
        if (sceneName.Contains("World"))
        {
            var worldServer = Instantiate(WorldServerPrefab);
            NetworkServer.Spawn(worldServer);
        }
    }

    public override void OnServerChangeScene(string sceneName)
    {
        if (sceneName.Contains("World")) WorldLoadingScreen.Begin();
        base.OnServerChangeScene(sceneName);
    }

    public override void OnClientChangeScene(string sceneName, SceneOperation operation, bool customHandling)
    {
        if (sceneName.Contains("World")) WorldLoadingScreen.Begin();
        base.OnClientChangeScene(sceneName, operation, customHandling);
    }

    public override void OnClientDisconnect()
    {
        WorldLoadingScreen.Cancel();
        if (LobbyManager.currentLobby.HasValue)
        {
            LobbyManager.currentLobby.Value.Leave();
        }
        base.OnClientDisconnect();
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        var server = WorldServer.instance;
        if (server != null && conn != null)
        {
            server.CloseChestForDisconnect(conn);
            server.CloseFurnaceForDisconnect(conn);
            var player = conn.identity != null ? conn.identity.GetComponent<Player>() : null;
            // Cancelling during loading can disconnect before any player exists or Start has run.
            if (player != null && player.inventory != null && !player.DimensionTransferInProgress) server.SavePlayerMessageHandler(conn, player.SavePlayer());
        }
        
        base.OnServerDisconnect(conn);
    }
}
