using System;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;
using Mirror.FizzySteam;
using Newtonsoft.Json;
using Steamworks;
using Steamworks.Data;
using UnityEngine;

// All entry points run on Unity's main thread. Steam callbacks are queued by MenuManager.
public static class LobbyManager
{
    // App 480 is shared with other projects. Never connect based on App ID alone.
    public const string Project = "joshtynr.minecraft-unity-edition";
    public const string Protocol = "1";
    public static Lobby? currentLobby;
    public static bool Hosting { get; private set; }
    public static bool Joining { get; private set; }
    public static string LastError { get; private set; }
    private static readonly HashSet<ulong> pendingJoins = new HashSet<ulong>();
    private static int generation;
    private static float connectDeadline;

    public static bool IsCompatible(Lobby lobby)
    {
        return lobby.GetData("project") == Project && lobby.GetData("protocol") == Protocol;
    }

    public static bool CanJoin(Lobby lobby)
    {
        return IsCompatible(lobby) && lobby.GetData("state") == "ready" &&
               lobby.Owner.IsFriend && !lobby.Owner.IsMe &&
               lobby.GetData("host") == lobby.Owner.Id.ToString() &&
               lobby.MemberCount < lobby.MaxMembers;
    }

    public static async Task HostAsync(string worldName, Vector3Int seedOffset)
    {
        if (Hosting || Joining || NetworkServer.active || NetworkClient.active) return;
        LastError = null;
        if (!PlayerIdentity.SteamAvailable) { Fail("Steam is unavailable. Start Steam and try again."); return; }
        Hosting = true; // Set before CreateLobbyAsync: Steam also raises OnLobbyEntered for the host.
        int attempt = ++generation;
        try
        {
            if (NetworkManager.singleton == null || NetworkManager.singleton.maxConnections < 2)
                throw new InvalidOperationException("The network manager needs room for at least two players.");
            var request = SteamMatchmaking.CreateLobbyAsync(Math.Min(8, NetworkManager.singleton.maxConnections));
            if (await Task.WhenAny(request, Task.Delay(15000)) != request)
            {
                LeaveLateLobby(request);
                throw new InvalidOperationException("Steam did not create the lobby in time.");
            }
            var result = await request;
            if (attempt != generation) { result?.Leave(); return; }
            if (!result.HasValue) throw new InvalidOperationException("Steam could not create the lobby.");
            var lobby = result.Value;
            currentLobby = lobby;
            if (!lobby.SetJoinable(false) || !lobby.SetFriendsOnly())
                throw new InvalidOperationException("Steam could not configure a friends lobby.");
            Set(lobby, "project", Project);
            Set(lobby, "protocol", Protocol);
            Set(lobby, "state", "starting");
            Set(lobby, "host", SteamClient.SteamId.ToString());
            Set(lobby, "name", string.IsNullOrWhiteSpace(LobbyCreationMenu.lobbyName) ? worldName : LobbyCreationMenu.lobbyName);
            Set(lobby, "world", worldName);
            Set(lobby, "seed", JsonConvert.SerializeObject(seedOffset));
            Set(lobby, "betaSeed", WorldSettingsManager.Instance.betaWorldSeed.ToString(CultureInfo.InvariantCulture));
            Set(lobby, "time", WorldSettingsManager.Instance.worldTimeTicks.ToString(CultureInfo.InvariantCulture));
            MinecraftNetworkManager.PrepareClientSession();
            if (!(NetworkManager.singleton.transport is FizzyFacepunch))
                throw new InvalidOperationException("The Steam transport is missing.");
            MinecraftNetworkManager.StartGameHost(false);
            // StartGameHost also cleans up failed sockets; its stop callbacks may
            // invalidate this attempt before returning, so report failure here.
            if (!NetworkServer.active) Fail("The world host could not start. Check the player log and try hosting again.");
        }
        catch (Exception error)
        {
            if (attempt == generation) Fail("Could not host a Steam game. " + error.Message);
        }
    }

    private static void Set(Lobby lobby, string key, string value)
    {
        if (!lobby.SetData(key, value)) throw new InvalidOperationException("Steam could not publish the lobby settings.");
    }

    // Called by the persistent network manager after Mirror's scene update.
    public static void Tick()
    {
        if (!Hosting && !Joining) return;
        if (!PlayerIdentity.SteamAvailable)
        {
            Fail("Steam disconnected. Reconnect Steam and try again.");
            StopNetwork();
            return;
        }
        if (Hosting && NetworkServer.active && currentLobby.HasValue &&
            World.Overworld != null && World.Overworld.IsWorldCreated)
        {
            var lobby = currentLobby.Value;
            // A migrated lobby must never advertise an owner who has no world server.
            if (!lobby.IsOwnedBy(SteamClient.SteamId))
            {
                Fail("The Steam lobby owner changed. Please host the world again.");
                StopNetwork();
                return;
            }
            if (lobby.GetData("state") != "ready")
            {
                try { Set(lobby, "state", "ready"); if (!lobby.SetJoinable(true)) throw new Exception("Steam could not open the lobby."); }
                catch (Exception error) { Fail(error.Message); StopNetwork(); }
            }
        }
        if (Joining && connectDeadline > 0 && Time.realtimeSinceStartup >= connectDeadline)
        {
            Fail("The host did not respond. Refresh the friends list and try again.");
            StopNetwork();
        }
    }

    public static async Task JoinAsync(Lobby lobby)
    {
        if (Hosting || Joining || NetworkServer.active || NetworkClient.active) return;
        LastError = null;
        if (!PlayerIdentity.SteamAvailable) { Fail("Steam is unavailable. Start Steam and try again."); return; }
        if (pendingJoins.Contains(lobby.Id)) { Fail("Steam is still finishing the previous join. Try again shortly."); return; }
        Joining = true;
        int attempt = ++generation;
        try
        {
            // Invites can arrive before metadata has been cached locally.
            if (!lobby.Refresh()) throw new InvalidOperationException("Steam could not refresh this game.");
            // RequestLobbyData has no Task in the bundled SDK. Poll the callback cache
            // briefly instead of assuming metadata arrived within a single frame.
            for (int i = 0; i < 50; i++)
            {
                await Task.Delay(100);
                if (attempt != generation) return;
                if (!string.IsNullOrEmpty(lobby.GetData("project")) &&
                    !string.IsNullOrEmpty(lobby.GetData("protocol")) &&
                    !string.IsNullOrEmpty(lobby.GetData("state")) &&
                    !string.IsNullOrEmpty(lobby.GetData("host"))) break;
            }
            if (attempt != generation) return;
            if (!CanJoin(lobby)) throw new InvalidOperationException("This friend's game is full, closed, or uses a different build.");
            var host = lobby.Owner.Id;
            OpenJoiningMenu();
            pendingJoins.Add(lobby.Id);
            Task<RoomEnter> request;
            try { request = lobby.Join(); }
            catch { pendingJoins.Remove(lobby.Id); throw; }
            if (await Task.WhenAny(request, Task.Delay(15000)) != request)
            {
                LeaveLateJoin(lobby, request);
                throw new InvalidOperationException("Steam did not join the lobby in time.");
            }
            RoomEnter result;
            try { result = await request; } finally { pendingJoins.Remove(lobby.Id); }
            if (attempt != generation) { if (result == RoomEnter.Success) lobby.Leave(); return; }
            if (result != RoomEnter.Success) throw new InvalidOperationException("Steam could not join this friend's game (" + result + ").");
            currentLobby = lobby;
            // Full is expected if this player just filled the last lobby slot.
            if (!IsCompatible(lobby) || lobby.GetData("state") != "ready" ||
                !lobby.Owner.IsFriend || (ulong)lobby.Owner.Id != (ulong)host ||
                lobby.GetData("host") != host.ToString())
                throw new InvalidOperationException("The host closed or changed the game. Refresh and try again.");
            var seed = JsonConvert.DeserializeObject<Vector3Int>(lobby.GetData("seed"));
            long betaSeed;
            double time;
            if (!long.TryParse(lobby.GetData("betaSeed"), NumberStyles.Integer, CultureInfo.InvariantCulture, out betaSeed) ||
                !double.TryParse(lobby.GetData("time"), NumberStyles.Float, CultureInfo.InvariantCulture, out time) ||
                double.IsNaN(time) || double.IsInfinity(time))
                throw new InvalidOperationException("The host's world settings are unavailable.");
            MinecraftNetworkManager.PrepareClientSession();
            if (!(NetworkManager.singleton.transport is FizzyFacepunch))
                throw new InvalidOperationException("The Steam transport is missing.");
            var settings = WorldSettingsManager.Instance;
            settings.seedOffset = seed;
            settings.betaWorldSeed = betaSeed;
            settings.worldTimeTicks = time;
            // Keep remote worlds distinct from local saves and their cached entity data.
            settings.worldName = "Steam-" + lobby.Id;
            NetworkManager.singleton.networkAddress = host.ToString();
            connectDeadline = Time.realtimeSinceStartup + 45;
            NetworkManager.singleton.StartClient();
        }
        catch (Exception error)
        {
            if (attempt == generation) { Fail("Could not join the game. " + error.Message); StopNetwork(); }
        }
    }

    private static async void LeaveLateLobby(Task<Lobby?> request)
    {
        try { var lobby = await request; lobby?.Leave(); } catch (Exception error) { Debug.LogWarning(error.Message); }
    }

    private static async void LeaveLateJoin(Lobby lobby, Task<RoomEnter> request)
    {
        try { if (await request == RoomEnter.Success) lobby.Leave(); } catch (Exception error) { Debug.LogWarning(error.Message); }
        finally { pendingJoins.Remove(lobby.Id); }
    }

    public static string TakeError()
    {
        string error = LastError;
        LastError = null;
        return error;
    }

    public static void Connected() { connectDeadline = 0; }

    public static void Leave()
    {
        ++generation;
        var lobby = currentLobby;
        bool owned = Hosting;
        currentLobby = null;
        Hosting = Joining = false;
        connectDeadline = 0;
        if (!lobby.HasValue) return;
        try
        {
            if (owned) { lobby.Value.SetJoinable(false); lobby.Value.SetData("state", "closed"); }
            lobby.Value.Leave();
        }
        catch (Exception error) { Debug.LogWarning("Steam lobby cleanup: " + error.Message); }
    }

    public static void Cancel()
    {
        LastError = null;
        Leave();
        StopNetwork();
        WorldLoadingScreen.Cancel();
    }

    public static void Disconnected()
    {
        if (Joining) Fail("Disconnected from your friend's game. The host may have left.");
        else Leave();
    }

    private static void StopNetwork()
    {
        if (NetworkManager.singleton == null) return;
        if (NetworkServer.active) NetworkManager.singleton.StopHost();
        else if (NetworkClient.active) NetworkManager.singleton.StopClient();
    }

    private static void Fail(string message)
    {
        LastError = message;
        Debug.LogWarning(message);
        Leave();
        WorldLoadingScreen.Cancel();
        if (Menu.currentMenu != null && Menu.currentMenu.gameObject.name == "JoiningMenu" && Menu.lastMenu != null)
            Menu.CloseCurrentMenu();
        if (MultiplayerMenuManager.Instance != null) MultiplayerMenuManager.Instance.ShowMessage(message);
    }

    private static void OpenJoiningMenu()
    {
        var obj = GameObject.Find("Canvas/JoiningMenu");
        if (obj != null) obj.GetComponent<Menu>().Open();
        else WorldLoadingScreen.Begin();
    }
}
