using UnityEngine;

/// <summary>
/// Classic Beta-style terrain density generator.
/// Recreates the old min-limit / max-limit / selector octave structure on a
/// 5 x 17 x 5 coarse grid, then trilinearly interpolates to a 16 x 128 x 16 chunk.
/// The rest of this project's 240-block vertical space remains available above it.
/// </summary>
public sealed class Beta173Terrain
{
    public const int TerrainHeight = 128;
    public const int SeaLevel = 64;

    private readonly BetaOctaveNoise minLimit;
    private readonly BetaOctaveNoise maxLimit;
    private readonly BetaOctaveNoise selector;
    private readonly BetaOctaveNoise scale;
    private readonly BetaOctaveNoise depth;
    private readonly BetaClimate climate;

    public Beta173Terrain(long seed)
    {
        var random = new JavaRandom(seed);
        minLimit = new BetaOctaveNoise(random, 16);
        maxLimit = new BetaOctaveNoise(random, 16);
        selector = new BetaOctaveNoise(random, 8);

        // Advance/create the classic auxiliary generators in their historical order.
        _ = new BetaOctaveNoise(random, 4); // field_702_n: sand/gravel surface noise
        _ = new BetaOctaveNoise(random, 4); // field_701_o: stone-depth surface noise
        scale = new BetaOctaveNoise(random, 10);
        depth = new BetaOctaveNoise(random, 16);
        _ = new BetaOctaveNoise(random, 8);
        climate = new BetaClimate(seed);
    }

    public bool[,,] GenerateSolidMask(int chunkWorldX, int chunkWorldZ)
    {
        const int gridX = 5;
        const int gridZ = 5;
        const int gridY = 17;

        int coarseX = chunkWorldX / 4;
        int coarseZ = chunkWorldZ / 4;

        double[] scaleNoise = scale.Generate2D(null, coarseX, coarseZ, gridX, gridZ, 1.121, 1.121);
        double[] depthNoise = depth.Generate2D(null, coarseX, coarseZ, gridX, gridZ, 200.0, 200.0);
        double[] selectorNoise = selector.GenerateNoiseOctaves(
            null, coarseX, 0.0, coarseZ,
            gridX, gridY, gridZ,
            684.412 / 80.0, 684.412 / 160.0, 684.412 / 80.0);
        double[] minNoise = minLimit.GenerateNoiseOctaves(
            null, coarseX, 0.0, coarseZ,
            gridX, gridY, gridZ,
            684.412, 684.412, 684.412);
        double[] maxNoise = maxLimit.GenerateNoiseOctaves(
            null, coarseX, 0.0, coarseZ,
            gridX, gridY, gridZ,
            684.412, 684.412, 684.412);

        var density = new double[gridX, gridY, gridZ];
        int densityIndex = 0;
        int columnIndex = 0;
        int sampleStride = 16 / gridX; // Beta: 16 / 5 = 3

        for (int gx = 0; gx < gridX; gx++)
        {
            int climateX = gx * sampleStride + sampleStride / 2;

            for (int gz = 0; gz < gridZ; gz++)
            {
                int climateZ = gz * sampleStride + sampleStride / 2;
                double climateWorldX = chunkWorldX + climateX;
                double climateWorldZ = chunkWorldZ + climateZ;

                climate.Sample(climateWorldX, climateWorldZ, out double temperature, out double humidity);

                double humidTemp = humidity * temperature;
                double aridity = 1.0 - humidTemp;
                aridity *= aridity;
                aridity *= aridity;
                double climateFactor = 1.0 - aridity;

                double surface = (scaleNoise[columnIndex] + 256.0) / 512.0;
                surface *= climateFactor;
                if (surface > 1.0) surface = 1.0;

                double depthValue = depthNoise[columnIndex] / 8000.0;
                if (depthValue < 0.0) depthValue = -depthValue * 0.3;
                depthValue = depthValue * 3.0 - 2.0;

                if (depthValue < 0.0)
                {
                    depthValue /= 2.0;
                    if (depthValue < -1.0) depthValue = -1.0;
                    depthValue /= 1.4;
                    depthValue /= 2.0;
                    surface = 0.0;
                }
                else
                {
                    if (depthValue > 1.0) depthValue = 1.0;
                    depthValue /= 8.0;
                }

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

                    double value;
                    if (blend < 0.0) value = low;
                    else if (blend > 1.0) value = high;
                    else value = low + (high - low) * blend;

                    value -= vertical;

                    if (gy > gridY - 4)
                    {
                        double fade = (gy - (gridY - 4)) / 3.0;
                        value = value * (1.0 - fade) + -10.0 * fade;
                    }

                    density[gx, gy, gz] = value;
                    densityIndex++;
                }
            }
        }

        var solid = new bool[16, TerrainHeight, 16];

        for (int cellX = 0; cellX < 4; cellX++)
        for (int cellZ = 0; cellZ < 4; cellZ++)
        for (int cellY = 0; cellY < 16; cellY++)
        {
            double d000 = density[cellX, cellY, cellZ];
            double d001 = density[cellX, cellY, cellZ + 1];
            double d100 = density[cellX + 1, cellY, cellZ];
            double d101 = density[cellX + 1, cellY, cellZ + 1];

            double dy000 = (density[cellX, cellY + 1, cellZ] - d000) * 0.125;
            double dy001 = (density[cellX, cellY + 1, cellZ + 1] - d001) * 0.125;
            double dy100 = (density[cellX + 1, cellY + 1, cellZ] - d100) * 0.125;
            double dy101 = (density[cellX + 1, cellY + 1, cellZ + 1] - d101) * 0.125;

            for (int subY = 0; subY < 8; subY++)
            {
                double x0z0 = d000;
                double x0z1 = d001;
                double dxz0 = (d100 - d000) * 0.25;
                double dxz1 = (d101 - d001) * 0.25;

                for (int subX = 0; subX < 4; subX++)
                {
                    double current = x0z0;
                    double dz = (x0z1 - x0z0) * 0.25;

                    for (int subZ = 0; subZ < 4; subZ++)
                    {
                        int x = cellX * 4 + subX;
                        int y = cellY * 8 + subY;
                        int z = cellZ * 4 + subZ;
                        solid[x, y, z] = current > 0.0;
                        current += dz;
                    }

                    x0z0 += dxz0;
                    x0z1 += dxz1;
                }

                d000 += dy000;
                d001 += dy001;
                d100 += dy100;
                d101 += dy101;
            }
        }

        return solid;
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
