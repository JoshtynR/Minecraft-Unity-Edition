using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

public partial class World
{
    public WorldDimension dimension;
    [NonSerialized] public bool secondaryDimension;
    public static World Overworld { get; private set; }
    private static readonly Dictionary<WorldDimension,World> dimensions = new Dictionary<WorldDimension,World>();
    public string StorageName => DimensionRules.StorageName(worldName,dimension);
    public static IEnumerable<World> LoadedDimensions => dimensions.Values.Where(w=>w!=null).ToArray();
    private float dimensionStreamAt;

    private void RegisterDimension()
    {
        dimension=DimensionRules.Valid(dimension);
        if(!secondaryDimension) { dimensions.Clear(); Overworld=this; }
        dimensions[dimension]=this;
    }
    private void UnregisterDimension()
    {
        if(dimensions.TryGetValue(dimension,out var current)&&current==this)dimensions.Remove(dimension);
        if(Instance==this)Instance=null;
        if(Overworld==this)Overworld=null;
    }
    public static World FindDimension(WorldDimension id)
    { dimensions.TryGetValue(DimensionRules.Valid(id),out var world);return world; }

    public static World GetDimension(WorldDimension id)
    {
        id=DimensionRules.Valid(id);
        var found=FindDimension(id);if(found!=null)return found;
        var source=Overworld;
        if(source==null)return Instance;
        var root=new GameObject("Nether dimension");root.SetActive(false);
        root.transform.SetParent(source.transform,false);
        var world=root.AddComponent<World>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source),world);
        world.dimension=id;world.secondaryDimension=true;
        world.spawnPassiveMobs=false;
        world.isSaving=false;world.disabled=false;
        world.chunksToUpdate=new List<ChunkRenderer>();world.blockToUpdate=new Queue<Block>();
        world.OnWorldCreated=new UnityEvent();world.OnNewChunksGenerated=new UnityEvent();
        world.OnNewChunksGenerated.AddListener(()=>
        {
            if(Instance==world&&GameManager.Instance!=null&&GameManager.Instance.localPlayer!=null)
                GameManager.Instance.StartCheckingForChunks();
        });
        var rendererRoot=new GameObject("Nether chunks");rendererRoot.transform.SetParent(root.transform,false);
        world.worldRenderer=rendererRoot.AddComponent<WorldRenderer>();world.worldRenderer.chunkPrefab=source.worldRenderer.chunkPrefab;
        world.terrainGenerator=root.AddComponent<TerrainGenerator>();
        world.terrainGenerator.dumpBetaParity=false;
        world.worldData=new WorldData{worldName=world.worldName,owner=world,
            chunkDataDict=new Dictionary<Vector3Int,ChunkData>(),chunkDict=new Dictionary<Vector3Int,ChunkRenderer>()};
        root.SetActive(true);
        world.RestoreDimensionState();
        rendererRoot.SetActive(false);
        return world;
    }
    private void RestoreDimensionState()
    {
        if(!DropAuthority)return;
        string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),".minecraftUnity/saves",StorageName,"world.json");
        if(!File.Exists(path))return;
        string json=File.ReadAllText(path);
        if(!json.TrimStart().StartsWith("{"))json=Decompress(json);
        var saved=JsonUtility.FromJson<WorldSaveData>(json);
        RestoreFluidTicks(saved.fluidTicks);RestoreDroppedItems(saved.droppedItems);RestoreMobs(saved.SavedMobs);
    }
    public static void ActivateDimension(World world)
    {
        if(world==null)return;
        Instance=world;
        foreach(var loaded in LoadedDimensions)
        {
            // Renderer roots contain chunks, chest models, drops and animals, never World itself.
            loaded.worldRenderer.gameObject.SetActive(loaded==world);
            loaded.lastBetaSkyDarkness=-1;loaded.lastBetaBrightnessBoost=-1;
        }
        if(GameManager.Instance!=null)GameManager.Instance.world=world;
        world.UpadteLightTexture();
    }
    public static async void SaveAllDimensions()
    {
        try { await SaveAllDimensionsAsync(); }
        catch(Exception error){Debug.LogException(error);}
    }
    public static async Task SaveAllDimensionsAsync()
    {
        foreach(var world in LoadedDimensions)
        {
            while(world!=null&&world.isSaving)await Task.Yield();
            if(world!=null)await world.SaveDimensionAsync();
        }
    }
    private void StreamDimensionPlayers()
    {
        if(this!=Overworld || !NetworkServer.active || GameManager.Instance==null || Time.unscaledTime<dimensionStreamAt)return;
        dimensionStreamAt=Time.unscaledTime+1;
        foreach(var player in GameManager.Instance.players.ToArray())
        {
            if(player==null||player.DimensionTransferInProgress)continue;
            var realm=player.CurrentWorld;
            if(realm!=null&&realm.GenerateMoreChunks&&!realm.GenerationBusy)realm.GenerateWorld(Vector3Int.FloorToInt(player.transform.position));
        }
    }
}

public partial class World
{
    public bool GenerationBusy => generationGate.CurrentCount == 0;
    public bool PreparingDimensionArea { get; private set; }
    public bool HasReadyArea(Vector3Int position)
    {
        if(!IsWorldCreated||worldData.chunkDict==null||worldData.chunkDataDict==null)return false;
        foreach(var origin in WorldDataHelper.GetChunkPositionsInRenderDistance(this,position))
        {
            if(!worldData.chunkDict.TryGetValue(origin,out var renderer)||renderer==null||
               !worldData.chunkDataDict.TryGetValue(origin,out var data)||
               !ReferenceEquals(renderer.ChunkData,data)||!data.betaPopulationDone)return false;
        }
        foreach(var origin in WorldDataHelper.GetDataPositionsInRenderDistance(this,position))
            if(!worldData.chunkDataDict.ContainsKey(origin))return false;
        return true;
    }
    public async Task ApplyDimensionSnapshotsAsync(Dictionary<Vector3Int,string> incoming)
    {
        await generationGate.WaitAsync(worldCancellationToken);
        bool wasDisabled=disabled;disabled=true;
        try
        {
            while(chunkUpdateInProgress||waterMeshBuildInProgress)await Task.Yield();
            ResetChunkUpdates();
            foreach(var pair in incoming)
            {
                var save=JsonUtility.FromJson<ChunkSaveData>(Decompress(pair.Value));
                if(save.position!=pair.Key)throw new InvalidDataException("Dimension chunk position mismatch");
                var chunk=ChunkData.Deserialize(save,this);
                worldData.chunkDataDict[pair.Key]=chunk;
                if(worldData.chunkDict.TryGetValue(pair.Key,out var renderer))renderer.Initialize(chunk);
            }
            await RebuildDimensionMeshesCoreAsync();
        }
        finally{disabled=wasDisabled;generationGate.Release();}
    }
}

public partial class WorldServer
{
    public void CloseContainersForDimensionTravel(NetworkConnectionToClient connection)
    {
        foreach(var world in World.LoadedDimensions)
        {
            var chest=world.chests.Close(connection.connectionId);
            if(chest.accepted)
            {
                connection.identity.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(chest.inventory,chest.cursor);
                connection.Send(chest);
            }
            var furnace=world.furnaces.Close(connection.connectionId);
            if(furnace.accepted)
            {
                connection.identity.GetComponent<PlayerInventory>().ApplyChestPlayerSnapshot(furnace.inventory,furnace.cursor);
                connection.Send(furnace);
            }
        }
    }
}
