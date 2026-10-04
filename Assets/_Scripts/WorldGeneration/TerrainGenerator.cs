using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public BiomeGenerator biomeGenerator;
    private Beta173Terrain betaTerrain;
    private long betaTerrainSeed = long.MinValue;

    public ChunkData GenerateChunkData(ChunkData data, Vector3Int mapSeedOffset)
    {
        long betaSeed = data.worldRef.betaWorldSeed;
        if (betaTerrain == null || betaTerrainSeed != betaSeed)
        {
            betaTerrain = new Beta173Terrain(betaSeed);
            betaTerrainSeed = betaSeed;
        }

        bool[,,] betaSolidMask = betaTerrain.GenerateSolidMask(data.worldPos.x, data.worldPos.z);
        for (int x = 0; x < data.chunkSize; x++)
        for (int z = 0; z < data.chunkSize; z++)
            data = biomeGenerator.ProcessBetaDensityColumn(data, x, z, betaSolidMask);

        // Beta replaceBlocksForBiome is a whole-chunk pass. Keeping it here is
        // required for the historical JavaRandom consumption order.
        BetaSurfaceDecorator.DecorateChunk(data, betaTerrain);

        data.treeData = biomeGenerator.GenerateTreeData(data, mapSeedOffset);
        return data;
    }

    public void GenerateFeatures(ChunkData data, Vector3Int mapSeedOffset)
    {
        // Legacy feature generation remains temporarily connected while caves,
        // ores and population are replaced with their Beta 1.7.3 equivalents.
        for (int x = 0; x < data.chunkSize; x++)
        for (int z = 0; z < data.chunkSize; z++)
            biomeGenerator.ProcessFeatures(data, x, z, mapSeedOffset, null);
    }

    // Retained as a no-op because World currently calls this while streaming.
    // Beta climate/biome generation no longer needs the old Voronoi center cache.
    public void GenerateBiomePoints(Vector3 playerPos, int renderDistance, int chunkSize, Vector3Int mapSeedOffset)
    {
    }
}
