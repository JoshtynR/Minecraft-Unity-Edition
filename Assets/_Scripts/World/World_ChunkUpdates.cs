using System;
using System.Collections.Generic;
using UnityEngine;

public partial class World
{
    [HideInInspector] public List<ChunkRenderer> chunksToUpdate = new List<ChunkRenderer>();
    [NonSerialized] private HashSet<ChunkRenderer> runtimeQueuedChunks;
    private HashSet<ChunkRenderer> queuedChunks => System.Threading.LazyInitializer.EnsureInitialized(
        ref runtimeQueuedChunks, () => chunksToUpdate == null
            ? new HashSet<ChunkRenderer>() : new HashSet<ChunkRenderer>(chunksToUpdate));
    [NonSerialized] private object runtimeChunkUpdateLock;
    public object chunkUpdateThreadLock=>System.Threading.LazyInitializer.EnsureInitialized(ref runtimeChunkUpdateLock, () => new object());
    private bool chunkUpdateInProgress;
    private int chunkUpdateSession => workLifetime.Version;

    private bool IsLiveChunk(ChunkRenderer chunk)
    {
        if (chunk == null || chunk.ChunkData == null || worldData.chunkDict == null) return false;
        return worldData.chunkDict.TryGetValue(chunk.ChunkData.worldPos, out var current) && ReferenceEquals(current, chunk);
    }

    private void ResetChunkUpdates()
    {
        lock (chunkUpdateThreadLock)
        {
            if(chunksToUpdate==null)chunksToUpdate=new List<ChunkRenderer>();else chunksToUpdate.Clear();
            queuedChunks.Clear();
        }
        ResetBlockSurvival();
        workLifetime.Invalidate();
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
        PrepareRebuildPriority();
        lock (chunkUpdateThreadLock)
        {
            int count = Math.Min(chunksToUpdate.Count, Math.Max(1, (int)chunksPerFrame));
            for (int i = 0; i < count; i++)
            {
                int selected = SelectNextRebuild();
                var chunk = chunksToUpdate[selected];
                chunksToUpdate.RemoveAt(selected);
                queuedChunks.Remove(chunk);
                if (IsLiveChunk(chunk))
                    chunks.Add(chunk);
            }
        }
        if (chunks.Count == 0) return;
        int session = chunkUpdateSession;
        chunkUpdateInProgress = true;
        var buildWatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var meshes = await UpdateMeshDataAsync(chunks, false);
            buildWatch.Stop();
            var uploadWatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (!IsWorkSessionCurrent(session)) return;
                for (int i=0; i<chunks.Count; i++)
                {
                    if (!IsWorkSessionCurrent(session)) return;
                    TryApplyChunkMesh(chunks[i], meshes[chunks[i]], session);
                    await YieldBetweenMeshUploadsAsync(i+1, chunks.Count);
                }
            }
            finally
            {
                // Also release results rejected after pooling, reset or unload.
                ChunkMeshPipeline.Release(meshes.Values);
                uploadWatch.Stop();
                if (logGenerationTimings)
                    Debug.Log("[ChunkRendering] columns=" + chunks.Count + " buildWaitMs=" + buildWatch.ElapsedMilliseconds + " applyAndYieldMs=" + uploadWatch.ElapsedMilliseconds + " queued=" + chunksToUpdate.Count);
            }
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
}
