using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Lighting
{
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
            World.Instance.AddChunkToUpdate(chunk, true);
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
        if (block == null) return;

        var pos = block.localChunkPosition;
        // Start BELOW this block
        pos.y--;

        while (pos.y >= 0)
        {
            var blockBelow = data.GetBlock(pos);
            if (blockBelow == null || blockBelow.type == BlockType.Nothing) return;

            // Only extend through non-opaque
            if (blockBelow.BlockData.opacity < 15)
            {
                if (blockBelow.GetSkyLight() != 15)
                {
                    data.skyLightUpdateQueue.Enqueue(new BlockLightNode(blockBelow, 15));
                }
            }
            else
            {
                return;
            }

            pos.y--;
        }
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
        if (block == null) return;

        var pos = block.localChunkPosition;
        pos.y--; // start just below

        while (pos.y >= 0)
        {
            var blockBelow = data.GetBlock(pos);
            if (blockBelow == null || blockBelow.type == BlockType.Nothing) return;

            if (blockBelow.GetSkyLight() == 15)
            {
                blockBelow.SetSkyLight(0);
                RemoveSkyLight(data, blockBelow, 15);
            }
            else
            {
                return;
            }

            pos.y--;
        }
    }

    public static void RemoveSkyLight(ChunkData data, Block block, int oldLightValue)
    {
        if (block == null) return;

        int nextLight = Mathf.Max(0, oldLightValue - 1);

        foreach (var neighbor in block.GetNeighbors())
        {
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

        // Raise this block's skylight if incoming is stronger
        if (lightLevel > block.GetSkyLight())
            block.SetSkyLight(lightLevel);
        else
            lightLevel = block.GetSkyLight();

        if (lightLevel <= 1) return;

        int next = lightLevel - 1;

        foreach (var neighbor in block.GetNeighbors())
        {
            if (neighbor == null || neighbor.type == BlockType.Nothing) continue;
            if (neighbor.BlockData.opacity == 15) continue;

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

        foreach (var neighbor in block.GetNeighbors())
        {
            if (neighbor == null || neighbor.type == BlockType.Nothing) continue;

            var nLight = neighbor.GetBlockLight();
            if (nLight == 0) continue;

            if (nLight < oldLightValue)
            {
                neighbor.SetBlockLight(0);
                CheckForEdgeUpdate(neighbor, data);
                data.blockLightRemoveQueue.Enqueue(new BlockLightNode(neighbor, (byte)nLight));
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

        // Ensure the source has at least lightValue
        if (lightValue > block.GetBlockLight())
            block.SetBlockLight(lightValue);

        int current = block.GetBlockLight();
        if (current <= 1) return;

        int next = current - 1;

        foreach (var neighbor in block.GetNeighbors())
        {
            if (neighbor == null || neighbor.type == BlockType.Nothing) continue;
            if (neighbor.BlockData.opacity == 15) continue;

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
        if (block == null) return;

        if (block.chunkData != data)
        {
            if (block.chunkData?.renderer != null)
                data.chunkToUpdateAfterLighting.Add(block.chunkData.renderer);
        }
        else if (data.IsOnEdge(block.globalWorldPosition))
        {
            var chunks = data.GetNeighbourChunk(block.globalWorldPosition);
            foreach (var chunk in chunks)
            {
                if (chunk?.renderer != null)
                    data.chunkToUpdateAfterLighting.Add(chunk.renderer);
            }
        }
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

                    int sky = block.GetSkyLight();
                    if (sky > 0)
                        chunkData.skyLightUpdateQueue.Enqueue(new BlockLightNode(block, (byte)sky));
                }
            }
        }
    }

    public static void RecastSunLight(ChunkData chunkData, Vector3Int startPos)
    {
        int worldH = chunkData.worldRef.worldHeight;
        int yStart = Mathf.Clamp(startPos.y, 0, worldH - 1);

        bool obstructed = false;

        // Loop from top to bottom of column.
        for (int y = yStart; y >= 0; y--)
        {
            var block = chunkData.GetBlock(new Vector3Int(startPos.x, y, startPos.z));
            if (block == null || block.type == BlockType.Nothing)
                continue;

            if (obstructed)
            {
                block.SetSkyLight(0);
            }
            else if (block.BlockData.opacity > 0)
            {
                block.SetSkyLight(0);
                obstructed = true;
            }
            else
            {
                block.SetSkyLight(15);
            }
        }
    }
}
