using UnityEngine;

/// <summary>
/// Beta 1.7.3 replaceBlocksForBiome surface pass.
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
        BetaBiomeType[] biomes = terrain.GenerateBiomeRegion(betaWorldX, betaWorldZ, 16, 16);

        for (int betaX = 0; betaX < 16; betaX++)
        for (int betaZ = 0; betaZ < 16; betaZ++)
        {
            int index = betaX + betaZ * 16;
            BetaBiomeType biome = biomes[index];
            bool sandPatch = sandNoise[index] + random.NextDouble() * 0.2 > 0.0;
            bool gravelPatch = gravelNoise[index] + random.NextDouble() * 0.2 > 3.0;
            int thickness = (int)(stoneNoise[index] / 3.0 + 3.0 + random.NextDouble() * 0.25);
            int remaining = -1;
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
                }
                else if (block == BlockType.Stone)
                {
                    if (remaining == -1)
                    {
                        if (thickness <= 0)
                        {
                            top = BlockType.Air;
                            filler = BlockType.Stone;
                        }
                        else if (y >= Beta173Terrain.SeaLevel - 4 && y <= Beta173Terrain.SeaLevel + 1)
                        {
                            top = biomeTop;
                            filler = biomeFiller;
                            if (gravelPatch) top = BlockType.Air;
                            if (gravelPatch) filler = BlockType.Gravel;
                            if (sandPatch) top = BlockType.Sand;
                            if (sandPatch) filler = BlockType.Sand;
                        }

                        if (y < Beta173Terrain.SeaLevel && top == BlockType.Air)
                            top = BlockType.Water;

                        remaining = thickness;
                        data.SetBlock(pos, y >= Beta173Terrain.SeaLevel - 1 ? top : filler);
                    }
                    else if (remaining > 0)
                    {
                        --remaining;
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
    }
}
