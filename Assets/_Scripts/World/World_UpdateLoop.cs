using System;
using System.Collections.Generic;
using UnityEngine;

public partial class World
{
    [NonSerialized] private Queue<Block> runtimeBlockToUpdate;
    public Queue<Block> blockToUpdate
    {get=>System.Threading.LazyInitializer.EnsureInitialized(ref runtimeBlockToUpdate, () => new Queue<Block>());set=>runtimeBlockToUpdate=value;}
    [HideInInspector] public List<ChunkRenderer> chunksToUpdate = new List<ChunkRenderer>();
    [NonSerialized] private HashSet<ChunkRenderer> runtimeQueuedChunks;
    private HashSet<ChunkRenderer> queuedChunks => System.Threading.LazyInitializer.EnsureInitialized(
        ref runtimeQueuedChunks, () => chunksToUpdate == null
            ? new HashSet<ChunkRenderer>() : new HashSet<ChunkRenderer>(chunksToUpdate));
    [NonSerialized] private object runtimeChunkUpdateLock;
    public object chunkUpdateThreadLock=>System.Threading.LazyInitializer.EnsureInitialized(ref runtimeChunkUpdateLock, () => new object());
    private bool chunkUpdateInProgress;
    private int chunkUpdateSession;
    private Camera backgroundCamera;

    private void ResetChunkUpdates()
    {
        lock (chunkUpdateThreadLock)
        {
            if(chunksToUpdate==null)chunksToUpdate=new List<ChunkRenderer>();else chunksToUpdate.Clear();
            queuedChunks.Clear();
        }
        blockToUpdate.Clear();
        chunkUpdateSession++;
        chunkUpdateInProgress = false;
    }

    // Called by the main-thread Update. Chunk jobs still run on the thread pool,
    // but there is no idle polling thread and no concurrent upload from it.
    public async void UpdateChunks()
    {
        if (chunkUpdateInProgress || disabled || !IsWorldCreated || GenerationBusy) return;
        lock (chunkUpdateThreadLock)
        {
            if(chunksToUpdate==null)chunksToUpdate=new List<ChunkRenderer>();
            if (chunksToUpdate.Count == 0) return;
        }
        var chunks = new List<ChunkRenderer>();
        lock (chunkUpdateThreadLock)
        {
            int count = Math.Min(chunksToUpdate.Count, Math.Max(1, (int)chunksPerFrame));
            for (int i = 0; i < count; i++)
            {
                var chunk = chunksToUpdate[0];
                chunksToUpdate.RemoveAt(0);
                queuedChunks.Remove(chunk);
                if (chunk != null && worldData.chunkDict.TryGetValue(chunk.ChunkData.worldPos, out var current) && current == chunk)
                    chunks.Add(chunk);
            }
        }
        if (chunks.Count == 0) return;
        int session = chunkUpdateSession;
        chunkUpdateInProgress = true;
        try
        {
            var meshes = await UpdateMeshDataAsync(chunks, false);
            if (session != chunkUpdateSession || disabled) return;
            foreach (var chunk in chunks)
                if (chunk != null && worldData.chunkDict.TryGetValue(chunk.ChunkData.worldPos, out var current) && current == chunk)
                    chunk.UpdateChunk(meshes[chunk]);
        }
        catch (OperationCanceledException) { }
        catch (Exception error) { Debug.LogException(error); }
        finally { if (session == chunkUpdateSession) chunkUpdateInProgress = false; }
    }

    public void AddChunkToUpdate(ChunkRenderer chunk, bool first = false)
    {
        lock (chunkUpdateThreadLock)
        {
            if (disabled) return;
            if(chunksToUpdate==null)chunksToUpdate=new List<ChunkRenderer>();
            if (chunk != null && queuedChunks.Add(chunk))
            {
                if (first) chunksToUpdate.Insert(0, chunk);
                else chunksToUpdate.Add(chunk);
            }
        }
    }

    private void Update()
    {
        AdvanceAtmosphereClock(Time.deltaTime);
        UpdateWater();
        UpdateDroppedItems();
        UpdateFurnaces();
        UpdateMobs();
        UpdatePortals();
        StreamDimensionPlayers();
        if (Application.isPlaying) UpdateChunks();
        if (Instance != this) return;
        UpdateLighting();
        if (backgroundCamera == null) backgroundCamera = Camera.main;
        if (backgroundCamera != null)
            backgroundCamera.backgroundColor = dimension == WorldDimension.Nether ? new Color(.2f,.03f,.03f,1) :
                Color.Lerp(new Color(.015f,.02f,.045f),new Color(.50f,.70f,1f),SkyClock.Daylight(CurrentWorldTicks));
    }

    public void UpdateBlocks()
    {
        while (blockToUpdate.Count > 0) blockToUpdate.Dequeue().OnBlockUpdate();
    }
}
