using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public BiomeGenerator biomeGenerator;
    [Header("Beta 1.7.3")]
    [Tooltip("Keep full population disabled while exact population parity is being implemented.")]
    public bool enableBetaPopulation = false;
    [Tooltip("Non-parity visual preview only. Keep disabled for exact Beta 1.7.3 generation.")]
    public bool enableBetaTreePreview = false;
    [Tooltip("Write raw/surface/cave blocks and intermediate noise arrays for Beta chunk (0,0).")]
    public bool dumpBetaParity = true;

    private Beta173Terrain betaTerrain;
    private Beta173Caves betaCaves;
    private Beta173Population betaPopulation;
    private long betaTerrainSeed = long.MinValue;

    public ChunkData GenerateChunkData(ChunkData data, Vector3Int mapSeedOffset)
    {
        if (data.chunkSize != 16 || data.worldRef.worldHeight < Beta173Terrain.TerrainHeight)
            throw new System.InvalidOperationException("Beta terrain requires 16-block chunks and a world height of at least 128.");
        long betaSeed = data.worldRef.betaWorldSeed;
        EnsureBetaGenerators(betaSeed);

        int unityChunkX = FloorDiv(data.worldPos.x, data.chunkSize);
        int unityChunkZ = FloorDiv(data.worldPos.z, data.chunkSize);
        int betaChunkX = BetaCoordinateSpace.UnityChunkToBetaChunkX(unityChunkX);
        int betaChunkZ = BetaCoordinateSpace.UnityChunkToBetaChunkZ(unityChunkZ);
        int betaWorldX = BetaCoordinateSpace.BetaChunkToWorldX(betaChunkX);
        int betaWorldZ = BetaCoordinateSpace.BetaChunkToWorldZ(betaChunkZ);

        System.Action<string, double[]> observe = null;
        if (dumpBetaParity && betaChunkX == 0 && betaChunkZ == 0)
            observe = (stage, values) => BetaParityDump.DumpNoiseChunk00(stage, values, betaSeed);
        BlockType[,,] rawTerrain = betaTerrain.GenerateRawTerrain(betaWorldX, betaWorldZ, observe);
        if (dumpBetaParity) BetaParityDump.DumpRawChunk00(rawTerrain, betaSeed, betaChunkX, betaChunkZ);

        // Complete all chunk-local passes in canonical Beta coordinates, then
        // reflect X exactly once at the boundary to the Unity chunk storage.
        BetaSurfaceDecorator.DecorateChunk(rawTerrain, betaTerrain, betaChunkX, betaChunkZ, observe);
        if (dumpBetaParity) BetaParityDump.DumpRawChunk00(rawTerrain, betaSeed, betaChunkX, betaChunkZ, "surface");
        betaCaves.Generate(rawTerrain, betaChunkX, betaChunkZ);
        if (dumpBetaParity) BetaParityDump.DumpRawChunk00(rawTerrain, betaSeed, betaChunkX, betaChunkZ, "caves");

        for (int betaLocalX = 0; betaLocalX < data.chunkSize; betaLocalX++)
        for (int betaLocalZ = 0; betaLocalZ < data.chunkSize; betaLocalZ++)
        {
            int unityLocalX = BetaCoordinateSpace.BetaLocalToUnityLocalX(betaLocalX);
            int unityLocalZ = BetaCoordinateSpace.BetaLocalToUnityLocalZ(betaLocalZ);
            data = biomeGenerator.ProcessBetaTerrainColumn(
                data, unityLocalX, unityLocalZ, rawTerrain, betaLocalX, betaLocalZ);
        }

        data.treeData = new TreeData();
        return data;
    }

    public void GenerateFeatures(ChunkData data, Vector3Int mapSeedOffset)
    {
        if (!enableBetaPopulation && !enableBetaTreePreview)
            return;

        long betaSeed = data.worldRef.betaWorldSeed;
        EnsureBetaGenerators(betaSeed);

        int unityChunkX = FloorDiv(data.worldPos.x, data.chunkSize);
        int unityChunkZ = FloorDiv(data.worldPos.z, data.chunkSize);
        int betaChunkX = BetaCoordinateSpace.UnityChunkToBetaChunkX(unityChunkX);
        int betaChunkZ = BetaCoordinateSpace.UnityChunkToBetaChunkZ(unityChunkZ);

        if (enableBetaPopulation)
            betaPopulation.Populate(data, betaChunkX, betaChunkZ);
        if (enableBetaTreePreview)
            betaPopulation.PopulateTreePreview(data, betaTerrain, betaChunkX, betaChunkZ);
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
