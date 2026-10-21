using System;
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

    // All local host entry points go through this before Mirror starts listening.
    public static void StartGameHost()
    {
        if (NetworkServer.active || NetworkClient.active) return;
        PrepareClientSession();
        if (singleton != null) singleton.StartHost();
    }

    public static void PrepareClientSession()
    {
        PlayerIdentity.BeginSession();
        var manager = singleton as MinecraftNetworkManager;
        if (manager == null) return;
        manager.ConfigureSessionTransport();
    }

    private void ConfigureSessionTransport()
    {
        if (transport is Mirror.FizzySteam.FizzyFacepunch) steamTransport = transport;
        if (PlayerIdentity.UsingSteam)
        {
            if (steamTransport != null) transport = steamTransport;
        }
        else if (transport is Mirror.FizzySteam.FizzyFacepunch)
        {
            // The normal prefab already contains KCP. Also support older scenes.
            var localTransport = GetComponent<KcpTransport>();
            if (localTransport == null) localTransport = gameObject.AddComponent<KcpTransport>();
            localTransport.enabled = true;
            transport = localTransport;
            networkAddress = "localhost";
        }
        Transport.activeTransport = transport;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        PlayerIdentity.EndSession();
    }


    public override void OnClientConnect()
    {
        WorldLoadingScreen.Begin();
        base.OnClientConnect();
        NetworkClient.Send(new WorldServer.StartPlayerMessage(PlayerIdentity.Id));
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
