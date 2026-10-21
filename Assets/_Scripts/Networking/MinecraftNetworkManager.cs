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


    public override void OnClientConnect()
    {
        WorldLoadingScreen.Begin();
        base.OnClientConnect();
        NetworkClient.Send(new WorldServer.StartPlayerMessage(SteamClient.SteamId));
        
        // NetworkClient.Send(new WorldServer.SpawnPlayerMessage(SteamClient.SteamId));
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
