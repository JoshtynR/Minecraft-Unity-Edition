using System.Collections.Concurrent;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;
    private readonly ConcurrentQueue<Lobby> requestedLobbies = new ConcurrentQueue<Lobby>();
    
    private void Start()
    {
        Instance = this;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnEnable()
    {
        Instance = this;
        SteamFriends.OnGameLobbyJoinRequested += OnGameLobbyJoinRequested;
    }

    private void OnDisable()
    {
        SteamFriends.OnGameLobbyJoinRequested -= OnGameLobbyJoinRequested;
        if (Instance == this) Instance = null;
    }

    public void OnGameLobbyJoinRequested(Lobby lobby, SteamId steamId)
    {
        // Facepunch uses async callbacks. Touch Unity objects only from Update.
        requestedLobbies.Enqueue(lobby);
    }

    public void CancelLobby()
    {
        LobbyManager.Cancel();
    }

    private static async void JoinRequestedLobby(Lobby lobby) { await LobbyManager.JoinAsync(lobby); }

    private void Update()
    {
        Lobby lobby;
        while (requestedLobbies.TryDequeue(out lobby)) JoinRequestedLobby(lobby);
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (LobbyManager.Hosting || LobbyManager.Joining) LobbyManager.Cancel();
            if (Menu.currentMenu != null && Menu.lastMenu != null) Menu.CloseCurrentMenu();
        }
    }
}
