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

        int unityChunkX = FloorDiv(data.worldPos.x, data.chunkSize);
        int unityChunkZ = FloorDiv(data.worldPos.z, data.chunkSize);
        int betaChunkX = BetaCoordinateSpace.UnityChunkToBetaChunkX(unityChunkX);
        int betaChunkZ = BetaCoordinateSpace.UnityChunkToBetaChunkZ(unityChunkZ);
        int betaWorldX = BetaCoordinateSpace.BetaChunkToWorldX(betaChunkX);
        int betaWorldZ = BetaCoordinateSpace.BetaChunkToWorldZ(betaChunkZ);

        // Every Beta subsystem receives the same canonical Beta coordinates.
        BlockType[,,] rawTerrain = betaTerrain.GenerateRawTerrain(betaWorldX, betaWorldZ);

        // Reflect X once at the Beta -> Unity boundary. Unlike the previous
        // per-chunk transpose attempt, the chunk index is reflected as well,
        // so neighboring blocks remain neighboring blocks across chunk seams.
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

        // Temporary legacy tree data until the Beta population pass replaces it.
        data.treeData = biomeGenerator.GenerateTreeData(data, mapSeedOffset);
        return data;
    }

    public void GenerateFeatures(ChunkData data, Vector3Int mapSeedOffset)
    {
        // Beta population/decorator pass will replace the old feature pipeline.
    }

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
