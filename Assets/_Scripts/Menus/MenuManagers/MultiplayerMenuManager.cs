using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Steamworks;
using Steamworks.Data;
using TMPro;
using UnityEngine;

public class MultiplayerMenuManager : MonoBehaviour
{
    public static MultiplayerMenuManager Instance;
    public GameObject lobbyPrefab;
    public GameObject textPrefab;
    public Transform lobbyContainer;
    public LobbyButton selectedLobby;
    private int refreshGeneration;
    private bool refreshing;
    private float nextRefresh;

    private void OnEnable() { Instance = this; }
    private void OnDisable()
    {
        ++refreshGeneration;
        refreshing = false;
        if (Instance == this) Instance = null;
    }

    public void Start() { Cursor.lockState = CursorLockMode.None; }

    private void Update()
    {
        var menu = GetComponent<Menu>();
        if (menu != null && menu.opened && !refreshing && Time.realtimeSinceStartup >= nextRefresh)
            GetLobbys();
    }

    public void PressSelectedLobby() { if (selectedLobby != null) selectedLobby.onClick?.Invoke(); }

    // Keep this name: scene button events already reference it.
    public async void GetLobbys()
    {
        string sessionError = LobbyManager.TakeError();
        if (!string.IsNullOrEmpty(sessionError)) { DisplayMessage(sessionError); return; }
        if (refreshing) return;
        if (!PlayerIdentity.SteamAvailable)
        {
            DisplayMessage("Steam is unavailable. Start Steam to see friends' games. Single-player works offline.");
            return;
        }
        int request = ++refreshGeneration;
        refreshing = true;
        try
        {
            var lobbies = new Dictionary<ulong, Lobby>();
            foreach (var friend in SteamFriends.GetFriends())
            {
                if (!friend.IsFriend || friend.IsMe || !friend.IsPlayingThisGame) continue;
                var game = friend.GameInfo;
                if (!game.HasValue || !game.Value.Lobby.HasValue) continue;
                var lobby = game.Value.Lobby.Value;
                if ((ulong)lobby.Id == 0 || lobbies.ContainsKey(lobby.Id)) continue;
                lobbies.Add(lobby.Id, lobby);
                lobby.Refresh(); // Friends-only lobbies are absent from the public lobby query.
            }
            if (lobbyContainer.childCount == 0) DisplayMessage("Looking for friends' games…");
            // Refresh delivers metadata asynchronously through Steam's callback cache.
            await Task.Delay(900);
            if (this == null || request != refreshGeneration) return;
            var menu = GetComponent<Menu>();
            if (menu != null && !menu.opened) return;
            ulong selected = selectedLobby != null ? selectedLobby.steamLobbyId : 0;
            ClearRows();
            foreach (var lobby in lobbies.Values.OrderBy(l => l.Owner.Name))
            {
                if (!LobbyManager.CanJoin(lobby)) continue;
                var obj = Instantiate(lobbyPrefab, lobbyContainer);
                var labels = obj.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var label in labels) label.richText = false;
                labels[0].text = lobby.GetData("name") + "\n" + lobby.Owner.Name + " · " + lobby.GetData("world");
                labels[1].text = lobby.MemberCount + "/" + lobby.MaxMembers;
                obj.SetActive(true);
                var button = obj.GetComponent<LobbyButton>();
                button.steamLobbyId = lobby.Id;
                button.onClick = () => Join(lobby);
                if (button.steamLobbyId == selected) button.SetBorder(true);
            }
            if (lobbyContainer.childCount == 0)
                DisplayMessage("No friends are hosting a joinable game. Ask a Steam friend to host through Multiplayer using the same build.");
        }
        catch (Exception error)
        {
            if (this != null && request == refreshGeneration)
            {
                Debug.LogWarning("Steam friends list: " + error.Message);
                DisplayMessage("Could not refresh friends' games. Check Steam and try again.");
            }
        }
        finally
        {
            if (request == refreshGeneration) { refreshing = false; nextRefresh = Time.realtimeSinceStartup + 5; }
        }
    }

    private static async void Join(Lobby lobby) { await LobbyManager.JoinAsync(lobby); }

    private void ClearRows()
    {
        selectedLobby = null;
        // Destroy is deferred; detach immediately so childCount and selection stay accurate.
        for (int i = lobbyContainer.childCount - 1; i >= 0; i--)
        {
            var child = lobbyContainer.GetChild(i);
            child.gameObject.SetActive(false);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
    }

    public void ShowMessage(string message)
    {
        ++refreshGeneration;
        refreshing = false;
        DisplayMessage(message);
    }

    private void DisplayMessage(string message)
    {
        ClearRows();
        var obj = Instantiate(textPrefab, lobbyContainer);
        var label = obj.GetComponentInChildren<TextMeshProUGUI>();
        label.richText = false;
        label.text = message;
        obj.GetComponent<RectTransform>().sizeDelta = new Vector2(305, 130);
        nextRefresh = Time.realtimeSinceStartup + 8;
    }
}
