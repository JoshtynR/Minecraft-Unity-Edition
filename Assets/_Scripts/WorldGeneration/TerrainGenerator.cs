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

        BlockType[,,] rawTerrain = betaTerrain.GenerateRawTerrain(data.worldPos.x, data.worldPos.z);
        for (int x = 0; x < data.chunkSize; x++)
        for (int z = 0; z < data.chunkSize; z++)
            data = biomeGenerator.ProcessBetaTerrainColumn(data, x, z, rawTerrain);

        // Historical order: raw terrain -> biome surface replacement -> caves.
        BetaSurfaceDecorator.DecorateChunk(data, betaTerrain);

        int chunkX = FloorDiv(data.worldPos.x, data.chunkSize);
        int chunkZ = FloorDiv(data.worldPos.z, data.chunkSize);
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

    // World currently calls this while streaming. Beta climate generation does
    // not need the old Voronoi biome-center cache.
    public void GenerateBiomePoints(Vector3 playerPos, int renderDistance, int chunkSize, Vector3Int mapSeedOffset)
    {
    }

    private static int FloorDiv(int value, int divisor)
    {
        int result = value / divisor;
        int remainder = value % divisor;
        if (remainder != 0 && ((remainder < 0) != (divisor < 0))) result--;
        return result;
    }
}
