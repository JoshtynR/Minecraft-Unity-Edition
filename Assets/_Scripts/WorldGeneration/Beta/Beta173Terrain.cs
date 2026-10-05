/// <summary>Clean-room Beta 1.7.3 terrain generator.</summary>
public sealed class Beta173Terrain
{
    public const int TerrainHeight = 128;
    public const int SeaLevel = 64;
    private readonly BetaOctaveNoise minLimit, maxLimit, selector, sandGravel, stoneDepth, scale, depth, treeCount;
    private readonly BetaClimate climate;

    public Beta173Terrain(long seed)
    {
        var random = new JavaRandom(seed);
        minLimit = new BetaOctaveNoise(random, 16);
        maxLimit = new BetaOctaveNoise(random, 16);
        selector = new BetaOctaveNoise(random, 8);
        sandGravel = new BetaOctaveNoise(random, 4);
        stoneDepth = new BetaOctaveNoise(random, 4);
        scale = new BetaOctaveNoise(random, 10);
        depth = new BetaOctaveNoise(random, 16);
        treeCount = new BetaOctaveNoise(random, 8);
        climate = new BetaClimate(seed);
    }

    public double GetTreeCountNoise(int worldX, int worldZ)
    {
        return treeCount.Sample2D(worldX, worldZ, 0.5, 0.5);
    }

    public BetaBiomeType GetBiome(int worldX, int worldZ)
    {
        climate.Sample(worldX, worldZ, out double temperature, out double humidity);
        return BetaBiome.FromClimate(temperature, humidity);
    }

    public BetaBiomeType[] GenerateBiomeRegion(int worldX, int worldZ, int width, int depthSize)
    {
        climate.SampleRegion(worldX, worldZ, width, depthSize,
            out double[] temperatures, out double[] humidities);
        var biomes = new BetaBiomeType[width * depthSize];
        for (int i = 0; i < biomes.Length; i++)
            biomes[i] = BetaBiome.FromClimate(temperatures[i], humidities[i]);
        return biomes;
    }

    public void GenerateSurfaceNoise(int chunkWorldX, int chunkWorldZ,
        out double[] sandNoise, out double[] gravelNoise, out double[] stoneNoise)
    {
        const double s = 0.03125;
        sandNoise = sandGravel.GenerateNoiseOctaves(null,
            chunkWorldX, chunkWorldZ, 0.0,
            16, 16, 1, s, s, 1.0);
        gravelNoise = sandGravel.GenerateNoiseOctaves(null,
            chunkWorldX, 109.0134, chunkWorldZ,
            16, 1, 16, s, 1.0, s);
        stoneNoise = stoneDepth.GenerateNoiseOctaves(null,
            chunkWorldX, chunkWorldZ, 0.0,
            16, 16, 1, s * 2.0, s * 2.0, s * 2.0);
    }

    public BlockType[,,] GenerateRawTerrain(int chunkWorldX, int chunkWorldZ,
        System.Action<string, double[]> observe = null)
    {
        const int gridX = 5, gridZ = 5, gridY = 17;
        climate.SampleRegion(chunkWorldX, chunkWorldZ, 16, 16,
            out double[] temperatures, out double[] humidities);

        int coarseX = FloorDiv(chunkWorldX, 4), coarseZ = FloorDiv(chunkWorldZ, 4);
        double[] scaleNoise = scale.Generate2D(null, coarseX, coarseZ, gridX, gridZ, 1.121, 1.121);
        double[] depthNoise = depth.Generate2D(null, coarseX, coarseZ, gridX, gridZ, 200.0, 200.0);
        double[] selectorNoise = selector.GenerateNoiseOctaves(null, coarseX, 0.0, coarseZ, gridX, gridY, gridZ, 684.412 / 80.0, 684.412 / 160.0, 684.412 / 80.0);
        double[] minNoise = minLimit.GenerateNoiseOctaves(null, coarseX, 0.0, coarseZ, gridX, gridY, gridZ, 684.412, 684.412, 684.412);
        double[] maxNoise = maxLimit.GenerateNoiseOctaves(null, coarseX, 0.0, coarseZ, gridX, gridY, gridZ, 684.412, 684.412, 684.412);

        observe?.Invoke("temperature", temperatures);
        observe?.Invoke("humidity", humidities);
        observe?.Invoke("scale", scaleNoise);
        observe?.Invoke("depth", depthNoise);
        observe?.Invoke("selector", selectorNoise);
        observe?.Invoke("min", minNoise);
        observe?.Invoke("max", maxNoise);

        var density = new double[gridX, gridY, gridZ];
        // Beta intentionally uses 16/5 == 3, not the interpolation cell width.
        int densityIndex = 0, columnIndex = 0, sampleStride = 16 / gridX;
        for (int gx = 0; gx < gridX; gx++)
        {
            int climateX = gx * sampleStride + sampleStride / 2;
            for (int gz = 0; gz < gridZ; gz++)
            {
                int climateZ = gz * sampleStride + sampleStride / 2;
                double temperature = temperatures[climateX * 16 + climateZ];
                double humidity = humidities[climateX * 16 + climateZ];
                double humidTemp = humidity * temperature;
                double aridity = 1.0 - humidTemp; aridity *= aridity; aridity *= aridity;
                double climateFactor = 1.0 - aridity;
                double surface = (scaleNoise[columnIndex] + 256.0) / 512.0;
                surface *= climateFactor; if (surface > 1.0) surface = 1.0;
                double depthValue = depthNoise[columnIndex] / 8000.0;
                if (depthValue < 0.0) depthValue = -depthValue * 0.3;
                depthValue = depthValue * 3.0 - 2.0;
                if (depthValue < 0.0)
                {
                    depthValue /= 2.0; if (depthValue < -1.0) depthValue = -1.0;
                    depthValue /= 1.4; depthValue /= 2.0; surface = 0.0;
                }
                else { if (depthValue > 1.0) depthValue = 1.0; depthValue /= 8.0; }
                if (surface < 0.0) surface = 0.0;
                surface += 0.5;
                depthValue = depthValue * gridY / 16.0;
                double center = gridY / 2.0 + depthValue * 4.0;
                columnIndex++;
                for (int gy = 0; gy < gridY; gy++)
                {
                    double vertical = (gy - center) * 12.0 / surface;
                    if (vertical < 0.0) vertical *= 4.0;
                    double low = minNoise[densityIndex] / 512.0;
                    double high = maxNoise[densityIndex] / 512.0;
                    double blend = (selectorNoise[densityIndex] / 10.0 + 1.0) / 2.0;
                    double value = blend < 0.0 ? low : (blend > 1.0 ? high : low + (high - low) * blend);
                    value -= vertical;
                    if (gy > gridY - 4)
                    {
                        double fade = (double)((float)(gy - (gridY - 4)) / 3.0f);
                        value = value * (1.0 - fade) + -10.0 * fade;
                    }
                    density[gx, gy, gz] = value; densityIndex++;
                }
            }
        }

        if (observe != null)
        {
            var values = new double[gridX * gridZ * gridY];
            for (int gx = 0; gx < gridX; gx++)
            for (int gz = 0; gz < gridZ; gz++)
            for (int gy = 0; gy < gridY; gy++)
                values[(gx * gridZ + gz) * gridY + gy] = density[gx, gy, gz];
            observe("density", values);
        }

        var blocks = new BlockType[16, TerrainHeight, 16];
        for (int cellX = 0; cellX < 4; cellX++)
        for (int cellZ = 0; cellZ < 4; cellZ++)
        for (int cellY = 0; cellY < 16; cellY++)
        {
            double d000 = density[cellX, cellY, cellZ], d001 = density[cellX, cellY, cellZ + 1];
            double d100 = density[cellX + 1, cellY, cellZ], d101 = density[cellX + 1, cellY, cellZ + 1];
            double dy000 = (density[cellX, cellY + 1, cellZ] - d000) * 0.125;
            double dy001 = (density[cellX, cellY + 1, cellZ + 1] - d001) * 0.125;
            double dy100 = (density[cellX + 1, cellY + 1, cellZ] - d100) * 0.125;
            double dy101 = (density[cellX + 1, cellY + 1, cellZ + 1] - d101) * 0.125;
            for (int subY = 0; subY < 8; subY++)
            {
                double x0z0 = d000, x0z1 = d001;
                double dxz0 = (d100 - d000) * 0.25, dxz1 = (d101 - d001) * 0.25;
                for (int subX = 0; subX < 4; subX++)
                {
                    double current = x0z0, dz = (x0z1 - x0z0) * 0.25;
                    for (int subZ = 0; subZ < 4; subZ++)
                    {
                        int x = cellX * 4 + subX, y = cellY * 8 + subY, z = cellZ * 4 + subZ;
                        BlockType block = BlockType.Air;
                        if (y < SeaLevel)
                            block = temperatures[x * 16 + z] < 0.5 && y >= SeaLevel - 1 ? BlockType.Ice : BlockType.Water;
                        if (current > 0.0) block = BlockType.Stone;
                        blocks[x, y, z] = block; current += dz;
                    }
                    x0z0 += dxz0; x0z1 += dxz1;
                }
                d000 += dy000; d001 += dy001; d100 += dy100; d101 += dy101;
            }
        }
        return blocks;
    }

    private static int FloorDiv(int value, int divisor)
    {
        int result = value / divisor, remainder = value % divisor;
        if (remainder != 0 && ((remainder < 0) != (divisor < 0))) result--;
        return result;
    }
}
