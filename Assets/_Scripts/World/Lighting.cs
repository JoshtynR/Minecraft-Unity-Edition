using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class Lighting
{
    internal static readonly object SyncRoot = new object();

    // Seed the entire loaded lighting halo before propagating or sampling. The
    // outer data ring has no renderer yet, but AO samples its light values.
    public static void InitializeLoadedChunks(IList<ChunkData> chunks, CancellationToken token = default(CancellationToken), int workerCount = 1)
    {
        lock (SyncRoot)
        {
            var options=new ParallelOptions{CancellationToken=token,MaxDegreeOfParallelism=System.Math.Max(1,workerCount)};
            var seeded=chunks.Where(chunk=>!chunk.lightingInitialized).ToArray();
            Parallel.ForEach(seeded,options,chunk =>
            {
                token.ThrowIfCancellationRequested();
                RecastSunLightFirstTime(chunk);
                chunk.lightingInitialized = true;
            });
            // Beta updates shared borders when a chunk becomes available. A
            // clean old neighbor has no pending queue of its own; explicitly
            // import its boundary sources after every new chunk has been seeded.
            foreach(var chunk in seeded)SeedLoadedBoundaries(chunk);
            // One sequential transport phase prevents adjacent meshing workers
            // from racing writes to packed sky/block light or observing half-
            // initialized neighbors during the first mesh build.
            foreach (var chunk in chunks)
            {
                token.ThrowIfCancellationRequested();
                if (chunk.HasPendingLighting) chunk.UpdateLight();
            }
        }
    }

    private static void SeedLoadedBoundaries(ChunkData chunk)
    {
        int size=chunk.chunkSize,height=chunk.worldRef.worldHeight;
        for(int y=0;y<height;y++)for(int edge=0;edge<size;edge++)
        {
            SeedBoundary(chunk,new Vector3Int(-1,y,edge),new Vector3Int(0,y,edge));SeedBoundary(chunk,new Vector3Int(size,y,edge),new Vector3Int(size-1,y,edge));
            SeedBoundary(chunk,new Vector3Int(edge,y,-1),new Vector3Int(edge,y,0));SeedBoundary(chunk,new Vector3Int(edge,y,size),new Vector3Int(edge,y,size-1));
        }
    }
    private static void SeedBoundary(ChunkData owner,Vector3Int local,Vector3Int inside)
    {
        var source=owner.GetBlock(local);
        if(source==null||source.type==BlockType.Nothing)return;
        var target=owner.GetBlock(inside);if(target==null||target.type==BlockType.Nothing)return;
        int sky=source.GetSkyLight(),light=System.Math.Max(source.GetBlockLight(),source.BlockData.lightEmission),opacity=target.BlockData.opacity;
        // Open-air borders already at 15 need no queue entry. Import only light
        // which can actually improve the newly available destination cell.
        if(VoxelLightingMath.Attenuate(sky,opacity)>target.GetSkyLight())owner.skyLightUpdateQueue.Enqueue(new BlockLightNode(source,(byte)sky));
        if(VoxelLightingMath.Attenuate(light,opacity)>target.GetBlockLight())owner.blockLightUpdateQueue.Enqueue(new BlockLightNode(source,(byte)light));
    }

    public static void CalculateLight(ChunkData data)
    {
        // --- Sky light removal pass ---
        while (data.skyLightRemoveQueue.Count > 0)
        {
            var node = data.skyLightRemoveQueue.Dequeue();
            RemoveSkyLight(data, node.block, node.lightLevel);
        }

        // --- Sky light placement pass ---
        while (data.skyLightUpdateQueue.Count > 0)
        {
            var node = data.skyLightUpdateQueue.Dequeue();
            PlaceSkyLight(data, node.block, node.lightLevel);
        }

        // --- Block light removal pass ---
        while (data.blockLightRemoveQueue.Count > 0)
        {
            var node = data.blockLightRemoveQueue.Dequeue();
            RemoveBlockLight(data, node.block, node.lightLevel);
        }

        // --- Block light placement pass ---
        while (data.blockLightUpdateQueue.Count > 0)
        {
            var node = data.blockLightUpdateQueue.Dequeue();
            PlaceBlockLight(data, node.block, node.lightLevel);
        }

        // Distinct + null-safe push to world update queue
        data.chunkToUpdateAfterLighting = data.chunkToUpdateAfterLighting
            .Where(c => c != null)
            .Distinct()
            .ToList();

        foreach (var chunk in data.chunkToUpdateAfterLighting)
        {
            data.worldRef.AddChunkToUpdate(chunk, true);
        }
        data.chunkToUpdateAfterLighting.Clear();
    }

    public static void CalculateSkyLightExtend(ChunkData data)
    {
        if (data.skyExtendList.Count == 0) return;

        foreach (var block in data.skyExtendList)
        {
            if (block == null || block.type == BlockType.Nothing) continue;
            if (block.GetSkyLight() == 15)
            {
                ExtendSunRay(data, block);
            }
        }

        data.skyExtendList.Clear();
    }

    public static void ExtendSunRay(ChunkData data, Block block)
    {
        if (block != null) RecastSunLight(data, block.localChunkPosition, true);
    }

    public static void CalculateSkyLightRemove(ChunkData data)
    {
        if (data.skyRemoveList.Count == 0) return;

        foreach (var block in data.skyRemoveList)
        {
            if (block == null || block.type == BlockType.Nothing) continue;
            if (block.GetSkyLight() == 0)
            {
                BlockSunRay(data, block);
            }
        }

        data.skyRemoveList.Clear();
    }

    public static void BlockSunRay(ChunkData data, Block block)
    {
        if (block != null) RecastSunLight(data, block.localChunkPosition, true);
    }

    public static void RemoveSkyLight(ChunkData data, Block block, int oldLightValue)
    {
        if (block == null) return;

        int nextLight = Mathf.Max(0, oldLightValue - 1);

        for (int i = 0; i < 6; i++)
        {
            var neighbor = block.GetNeighbor(i);
            if (neighbor == null || neighbor.type == BlockType.Nothing) continue;

            int neighborLight = neighbor.GetSkyLight();
            if (neighborLight <= 0) continue;

            if (neighborLight <= nextLight)
            {
                neighbor.SetSkyLight(0);
                CheckForEdgeUpdate(neighbor, data);
                data.skyLightRemoveQueue.Enqueue(new BlockLightNode(neighbor, (byte)neighborLight));
            }
            else
            {
                // neighbor has stronger light → needs a re-propagation
                data.skyLightUpdateQueue.Enqueue(new BlockLightNode(neighbor, (byte)neighborLight));
            }
        }
    }

    public static void PlaceSkyLight(ChunkData data, Block block, int lightLevel)
    {
        if (block == null || block.type == BlockType.Nothing) return;

        // Queued values may predate a removal; never resurrect stale light.
        lightLevel = block.GetSkyLight();
        if (lightLevel <= 1) return;


        for (int i = 0; i < 6; i++)
        {
            var neighbor = block.GetNeighbor(i);
            if (neighbor == null || neighbor.type == BlockType.Nothing) continue;
            if (neighbor.BlockData.opacity == 15) continue;

            int next = VoxelLightingMath.Attenuate(lightLevel, neighbor.BlockData.opacity);
            if (neighbor.GetSkyLight() < next)
            {
                neighbor.SetSkyLight(next);
                CheckForEdgeUpdate(neighbor, data);
                data.skyLightUpdateQueue.Enqueue(new BlockLightNode(neighbor, (byte)next));
            }
        }
    }

    public static void RemoveBlockLight(ChunkData data, Block block, byte oldLightValue)
    {
        if (block == null) return;

        for (int i = 0; i < 6; i++)
        {
            var neighbor = block.GetNeighbor(i);
            if (neighbor == null || neighbor.type == BlockType.Nothing) continue;

            var nLight = neighbor.GetBlockLight();
            if (nLight == 0) continue;

            if (nLight < oldLightValue)
            {
                neighbor.SetBlockLight(0);
                CheckForEdgeUpdate(neighbor, data);
                data.blockLightRemoveQueue.Enqueue(new BlockLightNode(neighbor, (byte)nLight));
                if (neighbor.BlockData.lightEmission > 0)
                    data.blockLightUpdateQueue.Enqueue(new BlockLightNode(neighbor, neighbor.BlockData.lightEmission));
            }
            else // nLight >= oldLightValue
            {
                // Re-propagate from this neighbor since it might be a source or connect to a stronger path.
                data.blockLightUpdateQueue.Enqueue(new BlockLightNode(neighbor, (byte)nLight));
            }
        }
    }

    public static void PlaceBlockLight(ChunkData data, Block block, byte lightValue)
    {
        if (block == null || block.type == BlockType.Nothing) return;

        int current = Mathf.Max(block.GetBlockLight(), block.BlockData.lightEmission);
        if (current > block.GetBlockLight()) block.SetBlockLight(current);
        if (current <= 1) return;


        for (int i = 0; i < 6; i++)
        {
            var neighbor = block.GetNeighbor(i);
            if (neighbor == null || neighbor.type == BlockType.Nothing) continue;
            if (neighbor.BlockData.opacity == 15) continue;

            int next = VoxelLightingMath.Attenuate(current, neighbor.BlockData.opacity);
            if (neighbor.GetBlockLight() < next)
            {
                neighbor.SetBlockLight(next);
                CheckForEdgeUpdate(neighbor, data);
                data.blockLightUpdateQueue.Enqueue(new BlockLightNode(neighbor, (byte)next));
            }
        }
    }

    public static void CheckForEdgeUpdate(Block block, ChunkData data)
    {
        if (block == null || block.type == BlockType.Nothing) return;
        block.chunkData.MarkRenderSectionsDirty(block.globalWorldPosition.y);

        // Interior cells owned by this build cannot invalidate another column.
        // Keep the shared halo path for edges, including diagonal AO neighbors.
        if (ReferenceEquals(block.chunkData, data) && !data.IsOnEdge(block.globalWorldPosition)) return;

        data.worldRef.CollectRenderUpdatesAround(block.globalWorldPosition, data.chunkToUpdateAfterLighting, data);
    }

    public static void RecastSunLightFirstTime(ChunkData chunkData)
    {
        int size = chunkData.chunkSize;
        int worldH = chunkData.worldRef.worldHeight; // use chunk's worldRef consistently

        // Cast rays from top for each column
        for (int x = 0; x < size; x++)
        {
            for (int z = 0; z < size; z++)
            {
                RecastSunLight(chunkData, new Vector3Int(x, worldH - 1, z));
            }
        }

        // Re-enqueue existing lit blocks to propagate horizontally
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < worldH; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    var block = chunkData.GetBlock(new Vector3Int(x, y, z));
                    if (block == null || block.type == BlockType.Nothing) continue;

                    int emitted = block.BlockData.lightEmission;
                    int previous = block.GetBlockLight();
                    // Reinitialization is also used after population touches a
                    // previously lit halo. Drop old transport/source values.
                    block.SetBlockLight(emitted);
                    if(previous!=emitted)CheckForEdgeUpdate(block,chunkData);
                    if(previous>emitted)chunkData.blockLightRemoveQueue.Enqueue(new BlockLightNode(block,(byte)previous));
                    if (emitted > 0)
                    {
                        chunkData.blockLightUpdateQueue.Enqueue(new BlockLightNode(block, (byte)emitted));
                    }
                }
            }
        }
    }

    public static void RecastSunLight(ChunkData chunkData, Vector3Int startPos, bool changedColumn = false)
    {
        int worldH = chunkData.worldRef.worldHeight;

        int level = chunkData.worldRef.dimension == WorldDimension.Nether ? 0 : 15;
        int minChanged=worldH,maxChanged=-1;
        for (int y = worldH - 1; y >= 0; y--)
        {
            var block = chunkData.GetBlock(new Vector3Int(startPos.x, y, startPos.z));
            if (block == null || block.type == BlockType.Nothing) continue;
            int previous = block.GetSkyLight();
            int opacity = block.BlockData.opacity;
            if (changedColumn && level < 15) opacity = Mathf.Max(1, opacity);
            level = Mathf.Max(0, level - opacity);
            block.SetSkyLight(level);
            if(previous!=level){minChanged=System.Math.Min(minChanged,y);maxChanged=System.Math.Max(maxChanged,y);}
            if (previous > level)
                chunkData.skyLightRemoveQueue.Enqueue(new BlockLightNode(block, (byte)previous));
            if (level > 0 && (!changedColumn || previous != level))
                chunkData.skyLightUpdateQueue.Enqueue(new BlockLightNode(block, (byte)level));
        }
        // Direct column changes can alter a neighbor's corner samples even
        // when propagation does not change the neighbor's stored light. Batch
        // notifications by vertical section rather than once for every cell.
        for(int y=minChanged;y<=maxChanged;y+=16)CheckForEdgeUpdate(chunkData.GetBlock(new Vector3Int(startPos.x,y,startPos.z)),chunkData);
        if(maxChanged>=0)CheckForEdgeUpdate(chunkData.GetBlock(new Vector3Int(startPos.x,maxChanged,startPos.z)),chunkData);
    }
}
