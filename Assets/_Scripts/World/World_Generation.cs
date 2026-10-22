using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Mirror;
using Steamworks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Profiling;
using Debug = UnityEngine.Debug;

public partial class World
{
    public void GenerateWorld()
    {
        StartWorld();
        GenerateWorld(Vector3Int.zero);
    }

    public void StartWorld()
    {
        OnValidate();
        IsWorldCreated = false;
        WorldLoadingScreen.ResetProgress();

        disabled = false;
    }

    [System.NonSerialized] private System.Threading.SemaphoreSlim runtimeGenerationGate;
    private System.Threading.SemaphoreSlim generationGate => System.Threading.LazyInitializer.EnsureInitialized(ref runtimeGenerationGate, () => new System.Threading.SemaphoreSlim(1,1));
    private bool requestPlayerSpawn;
    public async void GenerateWorld(Vector3Int position, Action onGenerateNewChunks = null)
    {
        try { await EnsureAreaAsync(position); onGenerateNewChunks?.Invoke(); }
        catch (OperationCanceledException) { }
        catch (Exception error) { Debug.LogException(error); }
    }
    public async Task EnsureAreaAsync(Vector3Int position, bool fullArea = false)
    {
        await generationGate.WaitAsync(worldCancellationToken);
        try
        {
            while(chunkUpdateInProgress||waterMeshBuildInProgress)await Task.Yield();
            if(IsWorldCreated && HasReadyArea(position))return;
            PreparingDimensionArea=fullArea;
            await GenerateWorldCore(position);
        }
        finally { PreparingDimensionArea=false; generationGate.Release(); }
    }
    private async Task GenerateWorldCore(Vector3Int position)
    {
        var totalWatch=Stopwatch.StartNew();
        Profiler.BeginThreadProfiling("GenerateWorld", "GenerateWorld");
        if (!Application.isPlaying)
        {
            fullStopwatch.Start();
        }

        WorldGenerationData worldGenerationData = GetPositionsInRenderDistance(position);
        if(!IsWorldCreated)WorldLoadingScreen.ReportProgress("Building terrain",.05f);
        
        foreach (var pos in worldGenerationData.chunkPositionsToRemove)
        {
            WorldDataHelper.RemoveChunk(this, pos);
        }
        
        foreach (var pos in worldGenerationData.chunkDataToRemove)
        {
            WorldDataHelper.RemoveChunkData(this, pos);
        }

        // Generate data chunks
        ConcurrentDictionary<Vector3Int, ChunkData> dataDict = new ConcurrentDictionary<Vector3Int, ChunkData>();

        var dataStopWatch = new Stopwatch();
        dataStopWatch.Start();
        
        Profiler.BeginThreadProfiling("GenerateWorld", "GenerateData");

        if (worldGenerationData.chunkPositionsToCreate.Count > 0)
        {
            try
            {
                dataDict.AddRange(await CalculateWorldChunkData(worldGenerationData.chunkDataPositionsToCreate));
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Task cancelled");
                return;
            }
        }
        dataStopWatch.Stop();
        if(!IsWorldCreated)WorldLoadingScreen.ReportProgress("Reading world data",.3f);
        if (!Application.isPlaying)
        {
            Debug.Log($"Data generation took {dataStopWatch.ElapsedMilliseconds}ms");
        }

        var loadStopWatch = Stopwatch.StartNew();
        if (worldGenerationData.chunkDataPositionsToLoad.Count > 0)
        {
            try
            {
                dataDict.AddRange(await LoadChunksAsync(worldGenerationData.chunkDataPositionsToLoad, worldName));
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Task cancelled");
                return;
            }
        }
        
        loadStopWatch.Stop();
        if(!IsWorldCreated)WorldLoadingScreen.ReportProgress("Simulating world for a bit",.45f);
        if (!Application.isPlaying)
        {
            Debug.Log($"Loading took {loadStopWatch.ElapsedMilliseconds}ms");
        }

        foreach (var calculatedData in dataDict)
        {
            worldData.chunkDataDict.Add(calculatedData.Key, calculatedData.Value);
        }
        
        Profiler.EndThreadProfiling();

        var featureStopWatch = new Stopwatch();
        featureStopWatch.Start();
        
        // Populate lakes, dungeons, ores, trees, plants, springs, and snow.
        await CalculateFeatures(worldGenerationData.chunkPositionsToCreate);
        if(!IsWorldCreated)WorldLoadingScreen.ReportProgress("Calculating lighting",.65f);
        if (terrainGenerator.dumpBetaParity)
            foreach (var data in worldData.chunkDataDict.Values) terrainGenerator.DumpBetaPopulation(data);

        featureStopWatch.Stop();
        if (!Application.isPlaying)
        {
            Debug.Log($"Feature generation took {featureStopWatch.ElapsedMilliseconds}ms");
        }

        var visualStopWatch = new Stopwatch();
        visualStopWatch.Start();
        
        // Generate visual chunks
        ConcurrentDictionary<Vector3Int, MeshData> meshDataDict;
        
        List<ChunkData> dataToRender = worldData.chunkDataDict
            .Where(x => worldGenerationData.chunkPositionsToCreate.Contains(x.Key) || worldGenerationData.chunkPositionsToLoad.Contains(x.Key))
            .Select(x => x.Value)
            .ToList();

        await CastLightFirstTime(dataToRender);
        await RefreshTreeMeshes();
        if(!IsWorldCreated)WorldLoadingScreen.ReportProgress("Building terrain",.8f);

        try
        {
            meshDataDict = await CreateMeshDataAsync(dataToRender);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Task cancelled");
            return;
        }
        
        visualStopWatch.Stop();
        if (!Application.isPlaying)
        {
            Debug.Log($"Mesh generation took {visualStopWatch.ElapsedMilliseconds}ms");
        }

        var uploadWatch=Stopwatch.StartNew();
        await ApplyGeneratedChunks(meshDataDict, position);
        if(logGenerationTimings)Debug.Log($"[WorldGeneration:{dimension}] total={totalWatch.ElapsedMilliseconds}ms terrain={dataStopWatch.ElapsedMilliseconds}ms load={loadStopWatch.ElapsedMilliseconds}ms population={featureStopWatch.ElapsedMilliseconds}ms lighting/meshes={visualStopWatch.ElapsedMilliseconds}ms upload={uploadWatch.ElapsedMilliseconds}ms chunks={meshDataDict.Count}");
        
        Profiler.EndThreadProfiling();
    }
    
    public async void GenerateOnlyData(Vector3Int position, Action onDone = null)
    {
        
        WorldGenerationData worldGenerationData = GetPositionsInRenderDistance(position);

        // Generate data chunks
        ConcurrentDictionary<Vector3Int, ChunkData> dataDict = new ConcurrentDictionary<Vector3Int, ChunkData>();

        var dataStopWatch = new Stopwatch();
        dataStopWatch.Start();
        
        Profiler.BeginThreadProfiling("GenerateWorld", "GenerateData");

        if (worldGenerationData.chunkPositionsToCreate.Count > 0)
        {
            try
            {
                dataDict.AddRange(await CalculateWorldChunkData(worldGenerationData.chunkDataPositionsToCreate));
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Task cancelled");
                return;
            }
        }
        dataStopWatch.Stop();
        if (!Application.isPlaying)
        {
            Debug.Log($"Data generation took {dataStopWatch.ElapsedMilliseconds}ms");
        }

        var loadStopWatch = Stopwatch.StartNew();
        if (worldGenerationData.chunkDataPositionsToLoad.Count > 0)
        {
            try
            {
                dataDict.AddRange(await LoadChunksAsync(worldGenerationData.chunkDataPositionsToLoad, worldName));
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Task cancelled");
                return;
            }
        }
        
        loadStopWatch.Stop();
        if (!Application.isPlaying)
        {
            Debug.Log($"Loading took {loadStopWatch.ElapsedMilliseconds}ms");
        }

        foreach (var calculatedData in dataDict)
        {
            worldData.chunkDataDict.Add(calculatedData.Key, calculatedData.Value);
        }
        
        Profiler.EndThreadProfiling();

        var featureStopWatch = new Stopwatch();
        featureStopWatch.Start();
        
        // Populate lakes, dungeons, ores, trees, plants, springs, and snow.
        await CalculateFeatures(worldGenerationData.chunkPositionsToCreate);
        if (terrainGenerator.dumpBetaParity)
            foreach (var data in worldData.chunkDataDict.Values) terrainGenerator.DumpBetaPopulation(data);

        featureStopWatch.Stop();
        if (!Application.isPlaying)
        {
            Debug.Log($"Feature generation took {featureStopWatch.ElapsedMilliseconds}ms");
        }
        
        await CastLightFirstTime(worldData.chunkDataDict.Values.ToList());
        await RefreshTreeMeshes();
        onDone?.Invoke();
    }

    public UniTask CastLightFirstTime(List<ChunkData> dataToCast)
    {
        var loadedLightingChunks = worldData.chunkDataDict.Values.ToList();
        int workers=ChunkWorkerOptions().MaxDegreeOfParallelism;
        return UniTask.RunOnThreadPool(() =>
        {
            Lighting.InitializeLoadedChunks(loadedLightingChunks, worldCancellationToken, workers);
        }, true, worldCancellationToken);
    }
    
    public UniTask<ConcurrentDictionary<Vector3Int, MeshData>> CreateMeshDataAsync(List<ChunkData> dataToRender)
    {
        var options = ChunkWorkerOptions();
        return UniTask.RunOnThreadPool(() =>
        {
            // Each worker owns one result slot. Publishing into the concurrent
            // map afterward avoids lock/resize contention in the expensive mesh
            // phase while preserving the existing return type.
            var results = new MeshData[dataToRender.Count];
            Parallel.For(0, dataToRender.Count, options, index =>
            {
                worldCancellationToken.ThrowIfCancellationRequested();
                results[index] = dataToRender[index].GetMeshData();
            });

            var dict = new ConcurrentDictionary<Vector3Int, MeshData>();
            for (int i = 0; i < results.Length; i++)
                dict.TryAdd(dataToRender[i].worldPos, results[i]);
            return dict;
        }, true, worldCancellationToken);
    }

    private UniTask<ConcurrentDictionary<Vector3Int, ChunkData>> CalculateWorldChunkData(HashSet<Vector3Int> chunkDataPositionsToCreate)
    {
        var options = ChunkWorkerOptions();
        return UniTask.RunOnThreadPool(() =>
        {
            Profiler.BeginThreadProfiling("MyThreads","CalculateWorldChunkData");
            var positions = chunkDataPositionsToCreate.ToArray();
            var generated = new ChunkData[positions.Length];
            Parallel.For(0, positions.Length, options, index =>
            {
                if(taskTokenSource.IsCancellationRequested)
                {
                    worldCancellationToken.ThrowIfCancellationRequested();
                }

                var pos = positions[index];
                var data = new ChunkData(chunkSize, chunkHeight, this, pos);
                generated[index] = terrainGenerator.GenerateChunkData(data);

            });

            var dataDict = new ConcurrentDictionary<Vector3Int, ChunkData>();
            for (int i = 0; i < generated.Length; i++)
                dataDict.TryAdd(positions[i], generated[i]);
            Profiler.EndThreadProfiling();
            return dataDict;
        }, true, worldCancellationToken);
    }

    private async UniTask CalculateFeatures(HashSet<Vector3Int> chunkDataPositionsToCreate)
    {
        waitForBetaPopulation = true;
        var populated = new List<Vector3Int>();
        Interlocked.Increment(ref betaPopulationJobs);
        try
        {
            await UniTask.RunOnThreadPool(() =>
            {
                // A single canonical order includes ready data-only halo owners.
                // Their +8 features can reach visible chunks. Never run render owners
                // first and halo owners afterward: that changes cross-chunk results.
                foreach (var data in worldData.chunkDataDict.Values.Where(d => !d.betaPopulationDone)
                    .OrderBy(d => -d.worldPos.x).ThenBy(d => d.worldPos.z))
                {
                    worldCancellationToken.ThrowIfCancellationRequested();
                    terrainGenerator.PopulateChunk(data);
                    if (data.betaPopulationDone) populated.Add(data.worldPos);
                }
                foreach (var pos in chunkDataPositionsToCreate)
                    worldData.chunkDataDict[pos].isGenerated = true;
            },true, worldCancellationToken);
        }
        finally { Interlocked.Decrement(ref betaPopulationJobs); }
        var wake = new HashSet<Vector3Int>();
        foreach (var owner in populated)
            for (int dx=0;dx<=1;dx++) for (int dz=0;dz<=1;dz++)
                wake.Add(owner+new Vector3Int(-dx*chunkSize,0,dz*chunkSize));
        foreach (var origin in wake)
            if (worldData.chunkDict.ContainsKey(origin) && worldData.chunkDataDict.TryGetValue(origin,out var chunk)) QueueWaterChunk(chunk);
    }

    private async Task ApplyGeneratedChunks(ConcurrentDictionary<Vector3Int, MeshData> meshDataConDict, Vector3Int playerPos)
    {
        var meshDataDict = meshDataConDict.OrderBy(pair => Vector3Int.Distance(playerPos, pair.Key)).ToArray();
        for (var i = 0; i < meshDataDict.Length; i++)
        {
            var item = meshDataDict[i];
            CreateChunk(worldData, item.Key, item.Value);
            if(!IsWorldCreated)WorldLoadingScreen.ReportProgress("Building terrain",.8f+.15f*(i+1)/meshDataDict.Length);
            if (Application.isPlaying && (i + 1) % Math.Max(1, (int)chunksPerFrame) == 0)
            {
                await Task.Yield();
                worldCancellationToken.ThrowIfCancellationRequested();
            }
        }

        if (!IsWorldCreated && Application.isPlaying)
        {
            IsWorldCreated = true;
            WorldLoadingScreen.ReportProgress("Loading player",.95f);
            OnWorldCreated?.Invoke();
            if (requestPlayerSpawn)
            {
                requestPlayerSpawn = false;
                NetworkClient.Send(new WorldServer.SpawnPlayerMessage(PlayerIdentity.Id));
            }
        }

        if (!Application.isPlaying)
        {
            fullStopwatch.Stop();
            Debug.Log($"World created in {fullStopwatch.ElapsedMilliseconds}ms");
            fullStopwatch.Reset();
        }
    }

    private void CreateChunk(WorldData worldData, Vector3Int pos, MeshData meshData)
    {
        var chunkRenderer = worldRenderer.RenderChunk(worldData, pos, meshData);
        worldData.chunkDict.TryAdd(pos, chunkRenderer);
        QueueWaterChunk(worldData.chunkDataDict[pos]);
    }
}
