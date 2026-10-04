using System;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public BiomeGenerator biomeGenerator;
    private Beta173Terrain betaTerrain;
    private Beta173Caves betaCaves;
    private long betaTerrainSeed = long.MinValue;

    public ChunkData GenerateChunkData(ChunkData data, Vector3Int mapSeedOffset)
    {
        long betaSeed = data.worldRef.betaWorldSeed;
        if (betaTerrain == null || betaTerrainSeed != betaSeed)
        {
            betaTerrain = new Beta173Terrain(betaSeed);
            betaCaves = new Beta173Caves(betaSeed);
            betaTerrainSeed = betaSeed;
        }

        bool[,,] betaSolidMask = betaTerrain.GenerateSolidMask(data.worldPos.x, data.worldPos.z);
        for (int x = 0; x < data.chunkSize; x++)
        for (int z = 0; z < data.chunkSize; z++)
            data = biomeGenerator.ProcessBetaDensityColumn(data, x, z, betaSolidMask);

        // Beta replaceBlocksForBiome runs before MapGenCaves.
        BetaSurfaceDecorator.DecorateChunk(data, betaTerrain);

        int chunkX = data.worldPos.x / data.chunkSize;
        int chunkZ = data.worldPos.z / data.chunkSize;
        betaCaves.Generate(data, chunkX, chunkZ);

        // Temporary legacy tree data until the Beta population pass replaces it.
        data.treeData = biomeGenerator.GenerateTreeData(data, mapSeedOffset);
        return data;
    }

    public void GenerateFeatures(ChunkData data, Vector3Int mapSeedOffset)
    {
        // Do not run the old lode/cave feature pass on top of Beta MapGenCaves.
        // This will become the Beta population/decorator pass next.
    }

    // Retained as a no-op because World currently calls this while streaming.
    // Beta climate/biome generation no longer needs the old Voronoi center cache.
    public void GenerateBiomePoints(Vector3 playerPos, int renderDistance, int chunkSize, Vector3Int mapSeedOffset)
    {
    }
}
