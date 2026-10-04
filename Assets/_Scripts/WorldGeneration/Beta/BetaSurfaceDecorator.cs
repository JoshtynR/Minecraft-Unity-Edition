using UnityEngine;

/// <summary>
/// Beta 1.7.3 replaceBlocksForBiome-style surface pass. Runs once per chunk so
/// JavaRandom consumption follows the historical x/z/y traversal order.
/// Gravel and sandstone substitutions are deferred until those BlockTypes exist.
/// </summary>
public static class BetaSurfaceDecorator
{
    public static void DecorateChunk(ChunkData data, Beta173Terrain terrain)
    {
        int chunkX = FloorDiv(data.worldPos.x, 16);
        int chunkZ = FloorDiv(data.worldPos.z, 16);
        long chunkSeed = unchecked((long)chunkX * 341873128712L + (long)chunkZ * 132897987541L);
        var random = new JavaRandom(chunkSeed);

        terrain.GenerateSurfaceNoise(data.worldPos.x, data.worldPos.z,
            out double[] sandNoise, out double[] gravelNoise, out double[] stoneNoise);

        // Reference order is local Z outer, local X inner. Noise index is X + Z*16.
        for (int z = 0; z < 16; z++)
        for (int x = 0; x < 16; x++)
        {
            int noiseIndex = x + z * 16;
            bool sandPatch = sandNoise[noiseIndex] + random.NextDouble() * 0.2 > 0.0;
            bool gravelPatch = gravelNoise[noiseIndex] + random.NextDouble() * 0.2 > 3.0;
            int thickness = (int)(stoneNoise[noiseIndex] / 3.0 + 3.0 + random.NextDouble() * 0.25);
            int remaining = -1;

            BetaBiomeType biome = terrain.GetBiome(data.worldPos.x + x, data.worldPos.z + z);
            BlockType biomeTop = BetaBiome.TopBlock(biome);
            BlockType biomeFiller = BetaBiome.FillerBlock(biome);
            BlockType top = biomeTop;
            BlockType filler = biomeFiller;

            for (int y = 127; y >= 0; y--)
            {
                var pos = new Vector3Int(x, y, z);

                if (y <= random.NextInt(5))
                {
                    data.SetBlock(pos, BlockType.Bedrock);
                    continue;
                }

                BlockType block = data.GetBlock(pos).type;
                if (block == BlockType.Air)
                {
                    remaining = -1;
                    continue;
                }
                if (block != BlockType.Stone)
                    continue;

                if (remaining == -1)
                {
                    if (thickness <= 0)
                    {
                        top = BlockType.Air;
                        filler = BlockType.Stone;
                    }
                    else if (y >= SeaBandMin && y <= SeaBandMax)
                    {
                        top = biomeTop;
                        filler = biomeFiller;

                        // Beta gives gravel priority first, then sand. Gravel is
                        // not represented by this project's BlockType yet.
                        if (gravelPatch)
                            top = BlockType.Air;

                        if (sandPatch)
                        {
                            top = BlockType.Sand;
                            filler = BlockType.Sand;
                        }
                    }

                    if (y < Beta173Terrain.SeaLevel && top == BlockType.Air)
                        top = BlockType.Water;

                    remaining = thickness;
                    data.SetBlock(pos, y >= Beta173Terrain.SeaLevel - 1 ? top : filler);
                }
                else if (remaining > 0)
                {
                    remaining--;
                    data.SetBlock(pos, filler);
                    // Exact Beta changes exhausted sand filler to sandstone here.
                    // Preserve its random call until Sandstone exists in BlockType.
                    if (remaining == 0 && filler == BlockType.Sand)
                        _ = random.NextInt(4);
                }
            }
        }
    }

    private const int SeaBandMin = Beta173Terrain.SeaLevel - 4;
    private const int SeaBandMax = Beta173Terrain.SeaLevel + 1;

    private static int FloorDiv(int value, int divisor)
    {
        int result = value / divisor;
        int remainder = value % divisor;
        if (remainder != 0 && ((remainder < 0) != (divisor < 0))) result--;
        return result;
    }
}
