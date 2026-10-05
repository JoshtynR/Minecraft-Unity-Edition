/// <summary>
/// Beta 1.7.3 replaceBlocksForBiome surface pass.
/// </summary>
public static class BetaSurfaceDecorator
{
    public static void DecorateChunk(BlockType[,,] blocks, Beta173Terrain terrain, int betaChunkX, int betaChunkZ,
        System.Action<string, double[]> observe = null)
    {
        long chunkSeed = unchecked((long)betaChunkX * 341873128712L + (long)betaChunkZ * 132897987541L);
        var random = new JavaRandom(chunkSeed);
        int betaWorldX = betaChunkX * 16;
        int betaWorldZ = betaChunkZ * 16;

        terrain.GenerateSurfaceNoise(betaWorldX, betaWorldZ,
            out double[] sandNoise, out double[] gravelNoise, out double[] stoneNoise);
        observe?.Invoke("sand", sandNoise);
        observe?.Invoke("gravel", gravelNoise);
        observe?.Invoke("stoneDepth", stoneNoise);
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
            // Beta's surface loop reads noise/biomes at k + l*16, but
            // writes the byte array at (l*16+k)*128+y: X=l, Z=k.
            // Coordinates stay canonical until TerrainGenerator copies the result.
            int x = betaZ, z = betaX;

            for (int y = 127; y >= 0; y--)
            {
                if (y <= random.NextInt(5))
                {
                    blocks[x, y, z] = BlockType.Bedrock;
                    continue;
                }

                BlockType block = blocks[x, y, z];
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
                        blocks[x, y, z] = y >= Beta173Terrain.SeaLevel - 1 ? top : filler;
                    }
                    else if (remaining > 0)
                    {
                        --remaining;
                        blocks[x, y, z] = filler;
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
