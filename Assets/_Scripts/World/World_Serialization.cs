using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public partial class World
{
    [HideInInspector]
    public bool isSaving = false;

    // A brand-new world needs a seed/list entry, not a full chunk save before
    // the player can spawn. Existing metadata must never be overwritten here.
    public async Task EnsureWorldMetadataAsync()
    {
        if(string.IsNullOrEmpty(worldName))return;
        await generationGate.WaitAsync(worldCancellationToken);
        try
        {
            string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),".minecraftUnity/saves/"+StorageName+"/world.json");
            if(File.Exists(path))return;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var metadata=new WorldSaveData{chunks=new ChunkSaveData[0],worldName=worldName,dimension=dimension,
                seedOffset=mapSeedOffset,betaWorldSeed=betaWorldSeed};
            // Plain JSON also allows the menu to list the world before its first autosave.
            await File.WriteAllTextAsync(path,JsonUtility.ToJson(metadata));
        }
        finally{generationGate.Release();}
    }
    
    public async void SaveWorld()
    {
        try { await SaveDimensionAsync(); } catch (Exception error) { Debug.LogException(error); }
    }

    public async Task SaveDimensionAsync()
    {
        if(worldName == "")
        {
            Debug.Log("World name is empty - not saving");
            return;
        }
        
        if (isSaving)
        {
            Debug.Log("Already saving");
            return;
        }

        await generationGate.WaitAsync(worldCancellationToken);
        isSaving = true;
        ChunkData[] capturedChunks=null;
        bool saveCompleted=false;
        try
        {
            chests.SaveWorld(StorageName);
            furnaces.SaveWorld(StorageName);
            // Capture player stacks before yielding, at the same point as chest contents.
            // A transfer during chunk IO must not be saved on just one side.
            var chestPlayerSnapshots=GameManager.Instance==null?new WorldServer.SavePlayerMessage[0]:
                GameManager.Instance.players.Where(player=>player!=null&&player.inventory!=null&&!player.DimensionTransferInProgress).Select(player=>player.SavePlayer()).ToArray();
            var stopwatch = Stopwatch.StartNew();
            var saveWatch = Stopwatch.StartNew();
            capturedChunks=worldData.chunkDataDict.Values.Where(data=>data.modifiedAfterSave).ToArray();
            // Create object with data to save
            var worldSaveData = new WorldSaveData
            {
                chunks = capturedChunks.Select(ChunkSaveData.Serialize).ToArray(),
                worldName = worldName,
                dimension = dimension,
                seedOffset = mapSeedOffset,
                betaWorldSeed = betaWorldSeed,
                fluidTicks = CaptureFluidTicks(),
                droppedItems = CaptureDroppedItems(),
                cows = CaptureCows()
            };
            // Capture/clear synchronously. Changes arriving during asynchronous
            // IO set dirty again and must remain eligible for the next save.
            foreach(var chunk in capturedChunks)chunk.modifiedAfterSave=false;
            saveWatch.Stop();
            Debug.Log($"Creating local save object took {saveWatch.ElapsedMilliseconds}ms");

            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraftUnity/saves/"+StorageName+"/chunks"));

            // Save the chunks
            var saveOptions=ChunkWorkerOptions();
            await Task.Run(() =>
            {
                Parallel.ForEach(worldSaveData.chunks,saveOptions, chunk =>
                {
                    var path = GetChunkPath(worldName, chunk.position, dimension);
                    string json;
                    if (binaryCompressSaves)
                    {
                        json = Compress(JsonUtility.ToJson(chunk));
                    }
                    else
                    {
                        json = JsonUtility.ToJson(chunk);
                    }
                    File.WriteAllText(path, json);
                });
            });

            // Save the world data
            var savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraftUnity/saves/"+StorageName+"/world.json");

            string worldJson;

            if (binaryCompressSaves)
            {
                worldJson = Compress(JsonUtility.ToJson(worldSaveData));
            }
            else
            {
                worldJson = JsonUtility.ToJson(worldSaveData);
            }

            await File.WriteAllTextAsync(savePath, worldJson);

            // Save all the player data

            Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraftUnity/saves/"+worldSaveData.worldName+"/playerdata"));

            foreach(var playerSnapshot in chestPlayerSnapshots)SavePlayer(playerSnapshot);
            saveCompleted=true;
            Debug.Log("Saved world in " + stopwatch.ElapsedMilliseconds + "ms");
        }
        finally
        {
            if(!saveCompleted&&capturedChunks!=null)
                foreach(var chunk in capturedChunks)chunk.modifiedAfterSave=true;
            isSaving = false;
            generationGate.Release();
        }
    }

    public void SavePlayer(WorldServer.SavePlayerMessage message)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraftUnity/saves/" + worldName + $"/playerdata/{message.steamId}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(message));
    }

    public UniTask<ConcurrentDictionary<Vector3Int, ChunkData>> LoadChunksAsync(IEnumerable<Vector3Int> chunks, string worldName)
    {
        return UniTask.RunOnThreadPool(() =>
        {
            // TODO: for some reason when you use more than 1 thread it crashes because of type mismatch in chunkSection deserialize
            // this seems to be a bug in unity
            var dict = new ConcurrentDictionary<Vector3Int, ChunkData>();
            // Parallel.ForEach(chunks, chunkPos =>
            // {
            foreach (var chunkPos in chunks)
            {
                if(taskTokenSource.IsCancellationRequested)
                {
                    worldCancellationToken.ThrowIfCancellationRequested();
                }
                
                var path = GetChunkPath(worldName, chunkPos, dimension);
        
                if (!File.Exists(path))
                {
                    Debug.LogWarning("Chunk file not found at: " + chunkPos);
                    continue;
                }
                string json;

                if (binaryCompressSaves)
                {
                    json = Decompress(File.ReadAllText(path));
                }
                else
                {
                    json = File.ReadAllText(path);
                }
                
                var chunkSaveData = JsonUtility.FromJson<ChunkSaveData>(json);
                dict.TryAdd(chunkPos, ChunkData.Deserialize(chunkSaveData, this));
            }

            // });
            return dict;
        }, cancellationToken: worldCancellationToken);
    }

    public static string GetChunkPath(string worldName, Vector3Int chunkPos, WorldDimension dimension = WorldDimension.Overworld)
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),".minecraftUnity/saves/"+DimensionRules.StorageName(worldName,dimension)+"/chunks/"+chunkPos.x+"_"+chunkPos.y+"_"+chunkPos.z+".json");
    }

    public static string Compress(string s)
    {
        var bytes = Encoding.Unicode.GetBytes(s);
        using var msi = new MemoryStream(bytes);
        using var mso = new MemoryStream();
        using (var gs = new GZipStream(mso, CompressionMode.Compress))
        {
            msi.CopyTo(gs);
        }
        return Convert.ToBase64String(mso.ToArray());
    }
 
    public static string Decompress(string s)
    {
        var bytes = Convert.FromBase64String(s);
        using (var msi = new MemoryStream(bytes))
        using (var mso = new MemoryStream())
        {
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }
            return Encoding.Unicode.GetString(mso.ToArray());
        }
    }
}
