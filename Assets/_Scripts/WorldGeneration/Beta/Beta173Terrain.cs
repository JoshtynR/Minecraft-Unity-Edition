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
    public const int SeaLevel = 63;

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
        _ = new BetaOctaveNoise(random, 4);
        scale = new BetaOctaveNoise(random, 10);
        depth = new BetaOctaveNoise(random, 16);
        _ = new BetaOctaveNoise(random, 8);
        climate = new BetaClimate(seed);
    }

    public bool[,,] GenerateSolidMask(int chunkWorldX, int chunkWorldZ)
    {
        const int gridX = 5;
        const int gridZ = 5;
        const int gridY = 17; // 128 / 8 + 1

        var density = new double[gridX, gridY, gridZ];

        for (int gx = 0; gx < gridX; gx++)
        {
            for (int gz = 0; gz < gridZ; gz++)
            {
                double worldX = chunkWorldX + gx * 4;
                double worldZ = chunkWorldZ + gz * 4;

                double surfaceValue = scale.Sample(worldX, 10, worldZ, 1.121, 1.0, 1.121);
                climate.Sample(worldX, worldZ, out double temperature, out double humidity);
                double aridity = 1.0 - humidity * temperature;
                aridity *= aridity;
                aridity *= aridity;
                double climateFactor = 1.0 - aridity;
                double surface = (surfaceValue / 512.0 + 0.5) * climateFactor;
                if (surface > 1.0) surface = 1.0;

                double depthValue = depth.Sample(worldX, 10, worldZ, 200.0, 1.0, 200.0);
                depthValue /= 8000.0;
                if (depthValue < 0) depthValue = -depthValue * 0.3;
                depthValue = depthValue * 3.0 - 2.0;
                if (depthValue < 0)
                {
                    depthValue *= 0.5;
                    depthValue = System.Math.Max(depthValue, -1.0);
                    depthValue /= 2.8;
                    surface = 0.0;
                }
                else
                {
                    depthValue = System.Math.Min(depthValue, 1.0) / 8.0;
                }

                if (surface < 0.0) surface = 0.0;
                surface += 0.5;
                depthValue = depthValue * 17.0 / 16.0;
                double center = 17.0 / 2.0 + depthValue * 4.0;

                for (int gy = 0; gy < gridY; gy++)
                {
                    // The classic algorithm used 33 samples over 256-height-era
                    // storage; for Beta's 128 terrain band we sample every 8 blocks.
                    double vertical = (gy - center) * 12.0 / surface;
                    if (vertical < 0) vertical *= 4.0;

                    double y = gy * 8.0;
                    double low = minLimit.Sample(worldX, y, worldZ,
                        684.412, 684.412, 684.412) / 512.0;
                    double high = maxLimit.Sample(worldX, y, worldZ,
                        684.412, 684.412, 684.412) / 512.0;
                    double blend = (selector.Sample(worldX, y, worldZ,
                        8.55515, 4.277575, 8.55515) / 10.0 + 1.0) * 0.5;
                    blend = System.Math.Max(0.0, System.Math.Min(1.0, blend));

                    double value = low + (high - low) * blend - vertical;

                    if (gy > 14)
                    {
                        double fade = (gy - 14) / 2.0;
                        fade = System.Math.Min(1.0, fade);
                        value = value * (1.0 - fade) + -10.0 * fade;
                    }

                    density[gx, gy, gz] = value;
                }
            }
        }

        var solid = new bool[16, TerrainHeight, 16];

        for (int cellX = 0; cellX < 4; cellX++)
        for (int cellZ = 0; cellZ < 4; cellZ++)
        for (int cellY = 0; cellY < 16; cellY++)
        {
            for (int subY = 0; subY < 8; subY++)
            {
                double ty = subY / 8.0;
                double d00 = Lerp(density[cellX, cellY, cellZ], density[cellX, cellY + 1, cellZ], ty);
                double d01 = Lerp(density[cellX, cellY, cellZ + 1], density[cellX, cellY + 1, cellZ + 1], ty);
                double d10 = Lerp(density[cellX + 1, cellY, cellZ], density[cellX + 1, cellY + 1, cellZ], ty);
                double d11 = Lerp(density[cellX + 1, cellY, cellZ + 1], density[cellX + 1, cellY + 1, cellZ + 1], ty);

                for (int subX = 0; subX < 4; subX++)
                {
                    double tx = subX / 4.0;
                    double a = Lerp(d00, d10, tx);
                    double b = Lerp(d01, d11, tx);

                    for (int subZ = 0; subZ < 4; subZ++)
                    {
                        double tz = subZ / 4.0;
                        int x = cellX * 4 + subX;
                        int y = cellY * 8 + subY;
                        int z = cellZ * 4 + subZ;
                        solid[x, y, z] = Lerp(a, b, tz) > 0.0;
                    }
                }
            }
        }

        return solid;
    }

    private static double Lerp(double a, double b, double t) => a + (b - a) * t;
}
