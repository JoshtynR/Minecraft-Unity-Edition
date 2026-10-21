using System;
using System.Globalization;
using Steamworks;
using UnityEngine;

// Capture once per connection so losing Steam mid-game cannot switch save files.
public static class PlayerIdentity
{
    private const string OfflineIdKey = "Minecraft.OfflinePlayerId";
    private static bool resolved;
    private static SteamId id;
    private static string displayName;
    private static bool usingSteam;

    public static SteamId Id { get { Resolve(); return id; } }
    public static string DisplayName { get { Resolve(); return displayName; } }
    public static bool UsingSteam { get { Resolve(); return usingSteam; } }
    public static bool SteamAvailable
    {
        get
        {
            try { return SteamClient.IsValid && (ulong)SteamClient.SteamId != 0; }
            catch (Exception) { return false; }
        }
    }

    public static void BeginSession() { resolved = false; Resolve(); }
    public static void EndSession() { resolved = false; }

    public static void TryInitializeSteam(uint appId)
    {
        if (SteamAvailable) return;
        try { SteamClient.Init(appId); }
        catch (Exception error)
        { Debug.LogWarning("Steam is unavailable; single-player will use the local Windows profile. " + error.Message); }
    }

    private static void Resolve()
    {
        if (resolved) return;
        if (SteamAvailable)
        {
            try
            {
                id = SteamClient.SteamId;
                displayName = SteamClient.Name;
                if (string.IsNullOrWhiteSpace(displayName)) displayName = "Player";
                usingSteam = true;
                resolved = true;
                return;
            }
            catch (Exception) { /* Steam can disappear between the validity check and getters. */ }
        }

        // Preferences and the existing save root are scoped to the OS user.
        // ID 1 also retains saves made with the earlier offline DLL test patch.
        ulong localId;
        if (!ulong.TryParse(PlayerPrefs.GetString(OfflineIdKey, "1"), NumberStyles.None,
                CultureInfo.InvariantCulture, out localId) || localId == 0)
            localId = 1;
        PlayerPrefs.SetString(OfflineIdKey, localId.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.Save();
        id = localId;
        try { displayName = Environment.UserName; }
        catch (Exception) { displayName = "Player"; }
        if (string.IsNullOrWhiteSpace(displayName)) displayName = "Player";
        usingSteam = false;
        resolved = true;
    }
}
