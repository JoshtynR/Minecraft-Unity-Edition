using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    public BiomeGenerator biomeGenerator;
    [Header("Beta 1.7.3")]
    [Tooltip("Run the full ordered Beta population pipeline for newly generated chunks.")]
    public bool useCompleteBetaPopulation = true;
    [Tooltip("Legacy lake-only toggle, used only when Complete Beta Population is disabled.")]
    public bool enableBetaPopulation = false;
    [HideInInspector] // Retain the old field so existing serialized data/local edits remain readable.
    public bool enableBetaTreePreview = false;
    [Tooltip("Legacy isolated tree preview, used only when Complete Beta Population is disabled.")]
    public bool enableBetaTrees = true;
    [Tooltip("Write terrain stages and the final population snapshot for Beta chunk (0,0).")]
    public bool dumpBetaParity = false;

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
        if (useCompleteBetaPopulation)
        {
            GenerateBetaTrees(data);
            return;
        }
        if (!enableBetaPopulation && !enableBetaTrees)
            return;

        long betaSeed = data.worldRef.betaWorldSeed;
        EnsureBetaGenerators(betaSeed);

        int unityChunkX = FloorDiv(data.worldPos.x, data.chunkSize);
        int unityChunkZ = FloorDiv(data.worldPos.z, data.chunkSize);
        int betaChunkX = BetaCoordinateSpace.UnityChunkToBetaChunkX(unityChunkX);
        int betaChunkZ = BetaCoordinateSpace.UnityChunkToBetaChunkZ(unityChunkZ);

        if (enableBetaPopulation)
            betaPopulation.Populate(data, betaChunkX, betaChunkZ);
        GenerateBetaTrees(data);
    }

    public void GenerateBetaTrees(ChunkData data)
    {
        if (useCompleteBetaPopulation)
        {
            if (data.betaPopulationDone) return;
            EnsureBetaGenerators(data.worldRef.betaWorldSeed);
            int bx = BetaCoordinateSpace.UnityChunkToBetaChunkX(FloorDiv(data.worldPos.x, data.chunkSize));
            int bz = BetaCoordinateSpace.UnityChunkToBetaChunkZ(FloorDiv(data.worldPos.z, data.chunkSize));
            if (!Beta173Population.HasCompleteNeighborhood(data, bx, bz)) return;
            betaPopulation.PopulateComplete(data, betaTerrain, bx, bz);
            data.betaPopulationDone = data.betaTreePopulationDone = true;
            data.modifiedAfterSave = true;
            return;
        }
        if (!enableBetaTrees || data.betaTreePopulationDone) return;
        EnsureBetaGenerators(data.worldRef.betaWorldSeed);
        int betaChunkX = BetaCoordinateSpace.UnityChunkToBetaChunkX(FloorDiv(data.worldPos.x, data.chunkSize));
        int betaChunkZ = BetaCoordinateSpace.UnityChunkToBetaChunkZ(FloorDiv(data.worldPos.z, data.chunkSize));
        if (!Beta173Population.HasTreeNeighborhood(data, betaChunkX, betaChunkZ)) return;
        betaPopulation.PopulateTreePreview(data, betaTerrain, betaChunkX, betaChunkZ);
        data.betaTreePopulationDone = true;
        data.modifiedAfterSave = true;
    }

    public void DumpBetaPopulation(ChunkData data)
    {
        if (!dumpBetaParity || !data.betaPopulationDone) return;
        int bx = BetaCoordinateSpace.UnityChunkToBetaChunkX(FloorDiv(data.worldPos.x, data.chunkSize));
        int bz = BetaCoordinateSpace.UnityChunkToBetaChunkZ(FloorDiv(data.worldPos.z, data.chunkSize));
        BetaParityDump.DumpDataChunk00(data, bx, bz, "population");
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
