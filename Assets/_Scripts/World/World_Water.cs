using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using Mirror;
using UnityEngine;

public partial class World : IWaterWorld
{
    private WaterSimulation waterSimulation;
    private readonly ConcurrentQueue<Vector3Int> waterNotifications = new ConcurrentQueue<Vector3Int>();
    private readonly Queue<ChunkData> waterChunksToActivate = new Queue<ChunkData>();
    private readonly Dictionary<Vector3Int, byte> waterChanges = new Dictionary<Vector3Int, byte>();
    private readonly Dictionary<Vector3Int, Dictionary<Vector3Int, byte>> pendingWaterStates =
        new Dictionary<Vector3Int, Dictionary<Vector3Int, byte>>();
    private readonly HashSet<ChunkRenderer> waterDirtyChunks = new HashSet<ChunkRenderer>();
    private float waterTickTime;
    private bool waterMeshBuildInProgress;
    private int waterSession;

    // Block.SetType may run on generation workers. Only enqueue here; all water
    // simulation, network messages, and mesh requests run on Unity's main thread.
    public void NotifyWaterBlockChanged(Vector3Int position) { waterNotifications.Enqueue(position); }
    private void QueueWaterChunk(ChunkData chunk) { waterChunksToActivate.Enqueue(chunk); }

    private void ResetWater()
    {
        waterSimulation = null;
        waterSession++;
        waterMeshBuildInProgress = false;
        waterTickTime = 0;
        while (waterNotifications.TryDequeue(out _)) { }
        waterChunksToActivate.Clear();
        waterChanges.Clear();
        pendingWaterStates.Clear();
        waterDirtyChunks.Clear();
    }

    private static WaterPosition WaterPos(Vector3Int p) { return new WaterPosition(p.x, p.y, p.z); }
    private static Vector3Int BlockPos(WaterPosition p) { return new Vector3Int(p.x, p.y, p.z); }

    private bool TryGetWaterBlock(Vector3Int p, out Block block)
    {
        block = null;
        if (p.y < 0 || p.y >= worldHeight) return false;
        var origin = WorldDataHelper.GetChunkPosition(this, p);
        if (!worldData.chunkDataDict.TryGetValue(origin, out var chunk) || !chunk.isGenerated ||
            !worldData.chunkDict.ContainsKey(origin)) return false;
        block = chunk.GetBlock(p - chunk.worldPos);
        return block.type != BlockType.Nothing;
    }

    public int GetWaterCell(WaterPosition p)
    {
        if (p.y < 0 || p.y >= worldHeight) return WaterSimulation.Solid;
        if (!TryGetWaterBlock(BlockPos(p), out var block)) return WaterSimulation.Unloaded;
        if (block.type == BlockType.Water) return block.waterLevel;
        return block.type == BlockType.Air ? WaterSimulation.Air : WaterSimulation.Solid;
    }

    public void SetWaterCell(WaterPosition p, int level)
    {
        var position = BlockPos(p);
        if (ApplyWaterState(position, level))
            waterChanges[position] = level < 0 ? byte.MaxValue : (byte)level;
    }

    // Network clients apply server results without scheduling their own fluid ticks.
    public void ReceiveWaterState(Vector3Int position, byte level)
    {
        if (ApplyWaterState(position, level == byte.MaxValue ? WaterSimulation.Air : level & 15)) return;
        if (TryGetWaterBlock(position, out _)) return; // Already matches.
        var origin = WorldDataHelper.GetChunkPosition(this, position);
        if (!pendingWaterStates.TryGetValue(origin, out var states))
            pendingWaterStates[origin] = states = new Dictionary<Vector3Int, byte>();
        states[position] = level;
    }

    private bool ApplyWaterState(Vector3Int position, int level)
    {
        if (!TryGetWaterBlock(position, out var block)) return false;
        var type = level < 0 ? BlockType.Air : BlockType.Water;
        if (block.type == type && (type != BlockType.Water || block.waterLevel == level)) return false;
        var chunk = block.chunkData;
        if (block.type != type) chunk.SetBlock(position - chunk.worldPos, type);
        chunk.GetBlock(position - chunk.worldPos).waterLevel = level < 0 ? (byte)0 : (byte)level;
        chunk.modifiedAfterSave = true;
        // Corner heights sample diagonal cells too, including across chunk edges.
        for (int dx = -1; dx <= 1; dx++)
            for (int dz = -1; dz <= 1; dz++)
            {
                var origin = WorldDataHelper.GetChunkPosition(this, position + new Vector3Int(dx, 0, dz));
                if (worldData.chunkDict.TryGetValue(origin, out var renderer)) waterDirtyChunks.Add(renderer);
            }
        return true;
    }

    private void ActivateWaterChunk(ChunkData chunk)
    {
        if (!worldData.chunkDataDict.TryGetValue(chunk.worldPos, out var current) || current != chunk) return;
        if (pendingWaterStates.TryGetValue(chunk.worldPos, out var states))
        {
            foreach (var state in states) ApplyWaterState(state.Key, state.Value == byte.MaxValue ? -1 : state.Value & 15);
            pendingWaterStates.Remove(chunk.worldPos);
        }
        if (!NetworkServer.active) return;
        foreach (var section in chunk.sections)
            foreach (var block in section.blocks)
            {
                var p = block.globalWorldPosition;
                if (block.type == BlockType.Water)
                {
                    bool active = block.waterLevel != 0;
                    if (!active)
                    {
                        foreach (var direction in WaterBlockDirections)
                            if (GetWaterCell(WaterPos(p + direction)) == WaterSimulation.Air) { active = true; break; }
                    }
                    if (active) waterSimulation.Schedule(WaterPos(p));
                }
                // Resume water waiting on the other side of a previously absent chunk.
                if (block.position.x == 0) waterSimulation.Schedule(WaterPos(p + Vector3Int.left));
                if (block.position.x == chunk.chunkSize - 1) waterSimulation.Schedule(WaterPos(p + Vector3Int.right));
                if (block.position.z == 0) waterSimulation.Schedule(WaterPos(p + Vector3Int.back));
                if (block.position.z == chunk.chunkSize - 1) waterSimulation.Schedule(WaterPos(p + Vector3Int.forward));
            }
    }

    private static readonly Vector3Int[] WaterBlockDirections =
        { Vector3Int.left, Vector3Int.right, Vector3Int.back, Vector3Int.forward, Vector3Int.down };

    private void UpdateWater()
    {
        if (!Application.isPlaying || !IsWorldCreated || disabled) return;
        if (waterSimulation == null) waterSimulation = new WaterSimulation(this);
        for (int i = 0; i < 2 && waterChunksToActivate.Count > 0; i++)
            ActivateWaterChunk(waterChunksToActivate.Dequeue());
        while (waterNotifications.TryDequeue(out var p))
            if (NetworkServer.active) waterSimulation.NotifyChanged(WaterPos(p));

        if (NetworkServer.active)
        {
            waterTickTime += Time.deltaTime;
            // 20 game ticks/sec; water schedules its next update five ticks later.
            int ticks = 0;
            while (waterTickTime >= 0.05f && ticks++ < 5)
            {
                waterTickTime -= 0.05f;
                waterSimulation.AdvanceTick();
            }
            // Bound catch-up after a pause while preserving normal tick spacing.
            waterTickTime = Mathf.Min(waterTickTime, 0.25f);
            if (waterChanges.Count > 0 && WorldServer.instance != null)
            {
                var positions = new List<Vector3Int>(256);
                var levels = new List<byte>(256);
                foreach (var change in waterChanges)
                {
                    positions.Add(change.Key);
                    levels.Add(change.Value);
                    if (positions.Count < 256) continue;
                    WorldServer.instance.BroadcastWaterUpdates(positions.ToArray(), levels.ToArray());
                    positions.Clear(); levels.Clear();
                }
                if (positions.Count > 0) WorldServer.instance.BroadcastWaterUpdates(positions.ToArray(), levels.ToArray());
                waterChanges.Clear();
            }
        }
        if (waterDirtyChunks.Count > 0 && !waterMeshBuildInProgress) RebuildWaterMeshes();
    }

    // Called on Unity's main thread: Task awaits retain its synchronization
    // context, so RenderMesh never runs on the background chunk-update thread.
    private async void RebuildWaterMeshes()
    {
        int session = waterSession;
        var chunks = new List<ChunkRenderer>();
        foreach (var chunk in waterDirtyChunks) if (chunk != null) chunks.Add(chunk);
        waterDirtyChunks.Clear();
        if (chunks.Count == 0) return;
        waterMeshBuildInProgress = true;
        try
        {
            var meshes = await UpdateMeshDataAsync(chunks, false);
            if (session != waterSession) return;
            foreach (var chunk in chunks)
                if (chunk != null && worldData.chunkDict.TryGetValue(chunk.ChunkData.worldPos, out var current) && current == chunk)
                    chunk.UpdateChunk(meshes[chunk]);
        }
        catch (OperationCanceledException) { }
        catch (Exception exception) { Debug.LogException(exception); }
        finally { if (session == waterSession) waterMeshBuildInProgress = false; }
    }
}
