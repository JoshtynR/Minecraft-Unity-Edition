using UnityEditor;
using UnityEngine;

public static class CowTools
{
    [MenuItem("Minecraft/Entities/Spawn Cow Near Player")]
    public static void SpawnNearPlayer()
    {
        var world=World.Instance;var player=GameManager.Instance!=null?GameManager.Instance.localPlayer:null;
        if(!Application.isPlaying || world==null || player==null)
        {Debug.LogWarning("Enter Play Mode and load a world before spawning a cow.");return;}
        var origin=player.transform.position+(player.cam!=null?player.cam.forward:player.transform.forward)*4;
        if(!world.SpawnCowNear(origin))Debug.LogWarning("No loaded, clear ground nearby, or this client does not own the world.");
    }
}
