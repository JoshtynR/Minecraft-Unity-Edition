using UnityEngine;

/// <summary>
/// Beta 1.7.3 replaceBlocksForBiome surface pass. All noise/RNG work happens
/// in canonical Beta coordinates; block writes cross the coordinate boundary
/// only when addressing the Unity chunk.
/// </summary>
public static class BetaSurfaceDecorator
{
    public static void DecorateChunk(ChunkData data, Beta173Terrain terrain, int betaChunkX, int betaChunkZ)
    {
        long chunkSeed = unchecked((long)betaChunkX * 341873128712L + (long)betaChunkZ * 132897987541L);
        var random = new JavaRandom(chunkSeed);
        int betaWorldX = betaChunkX * 16;
        int betaWorldZ = betaChunkZ * 16;

        terrain.GenerateSurfaceNoise(betaWorldX, betaWorldZ,
            out double[] sandNoise, out double[] gravelNoise, out double[] stoneNoise);

        // Beta's replaceBlocksForBiome iterates X outer, Z inner, while the
        // generated 16x16 arrays are addressed as X + Z * 16.
        for (int betaX = 0; betaX < 16; betaX++)
        for (int betaZ = 0; betaZ < 16; betaZ++)
        {
            int noiseIndex = betaX + betaZ * 16;
            bool sandPatch = sandNoise[noiseIndex] + random.NextDouble() * 0.2 > 0.0;
            bool gravelPatch = gravelNoise[noiseIndex] + random.NextDouble() * 0.2 > 3.0;
            int thickness = (int)(stoneNoise[noiseIndex] / 3.0 + 3.0 + random.NextDouble() * 0.25);
            int remaining = -1;

            // The biome array used by Beta at this same index corresponds to
            // the actual X/Z column. Do not transpose the biome coordinates.
            BetaBiomeType biome = terrain.GetBiome(betaWorldX + betaX, betaWorldZ + betaZ);
            BlockType biomeTop = BetaBiome.TopBlock(biome);
            BlockType biomeFiller = BetaBiome.FillerBlock(biome);
            BlockType top = biomeTop;
            BlockType filler = biomeFiller;
            int unityX = BetaCoordinateSpace.BetaLocalToUnityLocalX(betaX);
            int unityZ = BetaCoordinateSpace.BetaLocalToUnityLocalZ(betaZ);

            for (int y = 127; y >= 0; y--)
            {
                var pos = new Vector3Int(unityX, y, unityZ);
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
                if (block != BlockType.Stone) continue;

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
                        if (gravelPatch)
                        {
                            top = BlockType.Air;
                            filler = BlockType.Gravel;
                        }
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
                    if (remaining == 0 && filler == BlockType.Sand)
                    {
                        remaining = random.NextInt(4);
                        filler = BlockType.Sandstone;
                    }
                }
            }
        }
    }

    private const int SeaBandMin = Beta173Terrain.SeaLevel - 4;
    private const int SeaBandMax = Beta173Terrain.SeaLevel + 1;
}
