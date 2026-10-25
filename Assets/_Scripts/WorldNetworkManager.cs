using System;
using System.IO;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldNetworkManager : MonoBehaviour
{
    private async void Start()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
        if (NetworkClient.active || NetworkServer.active) return;

        PlayerIdentity.TryInitializeSteam(480);
        if (!MinecraftNetworkManager.OfflineSessionRequested)
        {
            // Directly playing the World scene in the editor follows the same
            // setting as menu-launched worlds instead of silently hosting.
            if (SettingsManager.LanEnabled)
            {
                MinecraftNetworkManager.StartLanHost();
                return;
            }
            MinecraftNetworkManager.StartOfflineWorldInLoadedScene();
        }

        await StartOfflinePlayer();
    }

    private async Task StartOfflinePlayer()
    {
        try
        {
            WorldLoadingScreen.Begin();
            while (this != null && (World.Overworld == null || !World.Overworld.IsInitialized || GameManager.Instance == null))
                await Task.Yield();
            if (this == null) return;

            var overworld = World.Overworld;
            WorldServer.SavePlayerMessage savedPlayer;
            bool hasSave = TryLoadPlayer(overworld.worldName, out savedPlayer);
            var destination = World.GetDimension(hasSave ? savedPlayer.dimension : WorldDimension.Overworld);
            var generationPosition = hasSave ? Vector3Int.RoundToInt(savedPlayer.position) : Vector3Int.zero;

            World.ActivateDimension(destination);
            destination.StartWorld();
            await destination.EnsureAreaAsync(generationPosition, true);
            await destination.EnsureWorldMetadataAsync();
            if (this == null) return;

            Vector3 spawn = hasSave ? savedPlayer.position : WorldSpawnRules.Resolve(destination);
            var prefab = GameManager.Instance.playerPrefab;
            if (prefab == null && NetworkManager.singleton != null) prefab = NetworkManager.singleton.playerPrefab;
            if (prefab == null) throw new InvalidOperationException("The World scene has no player prefab assigned.");

            var playerObject = Instantiate(prefab, spawn, Quaternion.identity);
            var player = playerObject.GetComponent<Player>();
            if (player == null) throw new InvalidOperationException("The configured player prefab has no Player component.");
            player.dimension = destination.dimension;
            player.SteamId = PlayerIdentity.Id;
            player.PlayerName = PlayerIdentity.DisplayName;
            player.expectsSavedPlayerData = hasSave;

            while (player != null && player.inventory == null) await Task.Yield();
            if (player == null) return;
            if (hasSave) player.LoadPlayer(savedPlayer);
            Debug.Log("[WorldStartup] Offline world ready without starting a Mirror server.");
        }
        catch (OperationCanceledException) { }
        catch (Exception error)
        {
            Debug.LogError("Cannot start offline player: " + error);
            WorldLoadingScreen.Cancel();
            MinecraftNetworkManager.EndOfflineSession();
            SceneManager.LoadScene("MainMenu");
        }
    }

    private static bool TryLoadPlayer(string worldName, out WorldServer.SavePlayerMessage player)
    {
        player = default(WorldServer.SavePlayerMessage);
        if (string.IsNullOrEmpty(worldName)) return false;
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ".minecraftUnity", "saves", worldName, "playerdata", ((ulong)PlayerIdentity.Id) + ".json");
        if (!File.Exists(path)) return false;
        try
        {
            player = JsonUtility.FromJson<WorldServer.SavePlayerMessage>(File.ReadAllText(path));
            player.dimension = DimensionRules.Valid(player.dimension);
            return true;
        }
        catch (Exception error)
        {
            Debug.LogWarning("Could not load saved player; using world spawn instead. " + error.Message);
            return false;
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name.Contains("World")) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        MinecraftNetworkManager.EndOfflineSession();
    }
}
