using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public BiomeGenerator biomeGenerator;
    [Header("Beta 1.7.3")]
    [Tooltip("Keep population disabled while raw terrain/surface parity is being verified. Population changes visible terrain after replaceBlocksForBiome.")]
    public bool enableBetaPopulation = false;

    private Beta173Terrain betaTerrain;
    private Beta173Caves betaCaves;
    private Beta173Population betaPopulation;
    private long betaTerrainSeed = long.MinValue;

    public ChunkData GenerateChunkData(ChunkData data, Vector3Int mapSeedOffset)
    {
        long betaSeed = data.worldRef.betaWorldSeed;
        EnsureBetaGenerators(betaSeed);

        int unityChunkX = FloorDiv(data.worldPos.x, data.chunkSize);
        int unityChunkZ = FloorDiv(data.worldPos.z, data.chunkSize);
        int betaChunkX = BetaCoordinateSpace.UnityChunkToBetaChunkX(unityChunkX);
        int betaChunkZ = BetaCoordinateSpace.UnityChunkToBetaChunkZ(unityChunkZ);
        int betaWorldX = BetaCoordinateSpace.BetaChunkToWorldX(betaChunkX);
        int betaWorldZ = BetaCoordinateSpace.BetaChunkToWorldZ(betaChunkZ);

        BlockType[,,] rawTerrain = betaTerrain.GenerateRawTerrain(betaWorldX, betaWorldZ);

        for (int betaLocalX = 0; betaLocalX < data.chunkSize; betaLocalX++)
        for (int betaLocalZ = 0; betaLocalZ < data.chunkSize; betaLocalZ++)
        {
            int unityLocalX = BetaCoordinateSpace.BetaLocalToUnityLocalX(betaLocalX);
            int unityLocalZ = BetaCoordinateSpace.BetaLocalToUnityLocalZ(betaLocalZ);
            data = biomeGenerator.ProcessBetaTerrainColumn(
                data, unityLocalX, unityLocalZ, rawTerrain, betaLocalX, betaLocalZ);
        }

        BetaSurfaceDecorator.DecorateChunk(data, betaTerrain, betaChunkX, betaChunkZ);
        betaCaves.Generate(data, betaChunkX, betaChunkZ, reflectLocalX: true);

        data.treeData = new TreeData();
        return data;
    }

    public void GenerateFeatures(ChunkData data, Vector3Int mapSeedOffset)
    {
        if (!enableBetaPopulation)
            return;

        long betaSeed = data.worldRef.betaWorldSeed;
        EnsureBetaGenerators(betaSeed);

        int unityChunkX = FloorDiv(data.worldPos.x, data.chunkSize);
        int unityChunkZ = FloorDiv(data.worldPos.z, data.chunkSize);
        int betaChunkX = BetaCoordinateSpace.UnityChunkToBetaChunkX(unityChunkX);
        int betaChunkZ = BetaCoordinateSpace.UnityChunkToBetaChunkZ(unityChunkZ);
        betaPopulation.Populate(data, betaChunkX, betaChunkZ);
    }

    public void GenerateBiomePoints(Vector3 playerPos, int renderDistance, int chunkSize, Vector3Int mapSeedOffset)
    {
    }

    private void EnsureBetaGenerators(long seed)
    {
        if (betaTerrain != null && betaTerrainSeed == seed) return;
        betaTerrain = new Beta173Terrain(seed);
        betaCaves = new Beta173Caves(seed);
        betaPopulation = new Beta173Population(seed);
        betaTerrainSeed = seed;
    }

    private static int FloorDiv(int value, int divisor)
    {
        int result = value / divisor;
        int remainder = value % divisor;
        if (remainder != 0 && ((remainder < 0) != (divisor < 0))) result--;
        return result;
    }
}
