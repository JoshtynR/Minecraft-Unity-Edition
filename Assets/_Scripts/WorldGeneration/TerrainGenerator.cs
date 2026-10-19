using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    [Header("Beta 1.7.3")]
    [Tooltip("Write terrain stages and the final population snapshot for Beta chunk (0,0).")]
    public bool dumpBetaParity = false;

    private readonly object generatorLock = new object();
    private Generators generators;

    public ChunkData GenerateChunkData(ChunkData data)
    {
        if (data.chunkSize != 16 || data.worldRef.worldHeight < Beta173Terrain.TerrainHeight)
            throw new System.InvalidOperationException("Beta terrain requires 16-block chunks and a world height of at least 128.");
        long betaSeed = data.worldRef.betaWorldSeed;
        var generation = GetGenerators(betaSeed);

        int unityChunkX = FloorDiv(data.worldPos.x, data.chunkSize);
        int unityChunkZ = FloorDiv(data.worldPos.z, data.chunkSize);
        int betaChunkX = BetaCoordinateSpace.UnityChunkToBetaChunkX(unityChunkX);
        int betaChunkZ = BetaCoordinateSpace.UnityChunkToBetaChunkZ(unityChunkZ);
        int betaWorldX = BetaCoordinateSpace.BetaChunkToWorldX(betaChunkX);
        int betaWorldZ = BetaCoordinateSpace.BetaChunkToWorldZ(betaChunkZ);

        System.Action<string, double[]> observe = null;
        if (dumpBetaParity && betaChunkX == 0 && betaChunkZ == 0)
            observe = (stage, values) => BetaParityDump.DumpNoiseChunk00(stage, values, betaSeed);
        BlockType[,,] rawTerrain = generation.terrain.GenerateRawTerrain(betaWorldX, betaWorldZ, observe);
        if (dumpBetaParity) BetaParityDump.DumpRawChunk00(rawTerrain, betaSeed, betaChunkX, betaChunkZ);

        // Complete all chunk-local passes in canonical Beta coordinates, then
        // reflect X exactly once at the boundary to the Unity chunk storage.
        BetaSurfaceDecorator.DecorateChunk(rawTerrain, generation.terrain, betaChunkX, betaChunkZ, observe);
        if (dumpBetaParity) BetaParityDump.DumpRawChunk00(rawTerrain, betaSeed, betaChunkX, betaChunkZ, "surface");
        generation.caves.Generate(rawTerrain, betaChunkX, betaChunkZ);
        if (dumpBetaParity) BetaParityDump.DumpRawChunk00(rawTerrain, betaSeed, betaChunkX, betaChunkZ, "caves");

        for (int betaLocalX = 0; betaLocalX < data.chunkSize; betaLocalX++)
        for (int betaLocalZ = 0; betaLocalZ < data.chunkSize; betaLocalZ++)
        {
            int unityLocalX = BetaCoordinateSpace.BetaLocalToUnityLocalX(betaLocalX);
            int unityLocalZ = BetaCoordinateSpace.BetaLocalToUnityLocalZ(betaLocalZ);
            var localPos = new Vector3Int(unityLocalX, 0, unityLocalZ);
            for (int y = 0; y < data.worldRef.worldHeight; y++)
            {
                localPos.y = y;
                data.SetBlock(localPos, y < Beta173Terrain.TerrainHeight
                    ? rawTerrain[betaLocalX, y, betaLocalZ] : BlockType.Air);
            }
        }

        return data;
    }

    /// <summary>Populate once the full raw-terrain halo is available. Call in canonical chunk order.</summary>
    public void PopulateChunk(ChunkData data)
    {
        if (data.betaPopulationDone) return;
        int bx = BetaCoordinateSpace.UnityChunkToBetaChunkX(FloorDiv(data.worldPos.x, data.chunkSize));
        int bz = BetaCoordinateSpace.UnityChunkToBetaChunkZ(FloorDiv(data.worldPos.z, data.chunkSize));
        if (!Beta173Population.HasCompleteNeighborhood(data, bx, bz)) return;

        var generation = GetGenerators(data.worldRef.betaWorldSeed);
        generation.population.PopulateComplete(data, generation.terrain, bx, bz);
        data.betaPopulationDone = true;
        data.modifiedAfterSave = true;
    }

    public void DumpBetaPopulation(ChunkData data)
    {
        if (!dumpBetaParity || !data.betaPopulationDone) return;
        int bx = BetaCoordinateSpace.UnityChunkToBetaChunkX(FloorDiv(data.worldPos.x, data.chunkSize));
        int bz = BetaCoordinateSpace.UnityChunkToBetaChunkZ(FloorDiv(data.worldPos.z, data.chunkSize));
        BetaParityDump.DumpDataChunk00(data, bx, bz, "population");
    }

    private Generators GetGenerators(long seed)
    {
        // Chunk terrain runs in parallel. Publish the complete immutable set together,
        // and retain a local reference so a later world seed cannot mix generators.
        lock (generatorLock)
        {
            if (generators == null || generators.seed != seed)
                generators = new Generators(seed);
            return generators;
        }
    }

    private sealed class Generators
    {
        public readonly long seed;
        public readonly Beta173Terrain terrain;
        public readonly Beta173Caves caves;
        public readonly Beta173Population population;

        public Generators(long seed)
        {
            this.seed = seed;
            terrain = new Beta173Terrain(seed);
            caves = new Beta173Caves(seed);
            population = new Beta173Population(seed);
        }
    }

    private static int FloorDiv(int value, int divisor)
    {
        int result = value / divisor;
        int remainder = value % divisor;
        if (remainder != 0 && ((remainder < 0) != (divisor < 0))) result--;
        return result;
    }
}
