using UnityEditor;
using UnityEngine;

public static class MobTools
{
    [MenuItem("Minecraft/Entities/Spawn Zombie Near Player")]public static void SpawnZombie(){Spawn(MobType.Zombie);}
    [MenuItem("Minecraft/Entities/Spawn Skeleton Near Player")]public static void SpawnSkeleton(){Spawn(MobType.Skeleton);}
    [MenuItem("Minecraft/Entities/Spawn Creeper Near Player")]public static void SpawnCreeper(){Spawn(MobType.Creeper);}
    [MenuItem("Minecraft/Entities/Spawn Spider Near Player")]public static void SpawnSpider(){Spawn(MobType.Spider);}
    [MenuItem("Minecraft/Entities/Spawn Pig Near Player")]public static void SpawnPig(){Spawn(MobType.Pig);}
    [MenuItem("Minecraft/Entities/Spawn Sheep Near Player")]public static void SpawnSheep(){Spawn(MobType.Sheep);}
    [MenuItem("Minecraft/Entities/Spawn Chicken Near Player")]public static void SpawnChicken(){Spawn(MobType.Chicken);}
    private static void Spawn(MobType species)
    {
        var player=GameManager.Instance!=null?GameManager.Instance.localPlayer:null;
        if(!Application.isPlaying||player==null||player.CurrentWorld==null)return;
        if(!player.CurrentWorld.SpawnMobNear(player.transform.position+player.transform.forward*4,species))Debug.LogWarning("No loaded clear ground available for "+species);
    }
    [MenuItem("Minecraft/Entities/Spawn Cow Near Player")]
    public static void SpawnNearPlayer()
    {
        var world=World.Instance;var player=GameManager.Instance!=null?GameManager.Instance.localPlayer:null;
        if(!Application.isPlaying || world==null || player==null)
        {Debug.LogWarning("Enter Play Mode and load a world before spawning a mob.");return;}
        var origin=player.transform.position+(player.cam!=null?player.cam.forward:player.transform.forward)*4;
        if(!world.SpawnMobNear(origin))Debug.LogWarning("No loaded, clear ground nearby, or this client does not own the world.");
    }
}
