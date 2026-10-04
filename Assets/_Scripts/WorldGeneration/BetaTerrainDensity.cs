using UnityEngine;

/// <summary>
/// Experimental Beta 1.7.3-style density terrain adapted to this project's taller worlds.
/// Generates a true 3D density field so terrain can contain cliffs and overhangs.
/// This is intentionally isolated from caves, ores, trees and biome selection.
/// </summary>
public static class BetaTerrainDensity
{
    public static float Sample(int x, int y, int z, int worldHeight, Vector3Int seedOffset)
    {
        // Keep the old Beta terrain concentrated around its classic vertical scale
        // instead of stretching it over the project's full 240-block world.
        const float seaLevel = 64f;
        const float horizontalScale = 1f / 96f;
        const float detailScale = 1f / 32f;

        float sx = x + seedOffset.x;
        float sz = z + seedOffset.z;

        // Large-scale height and roughness controls.
        float baseNoise = Fbm2D(sx, sz, horizontalScale, 4, 0.5f, 0f, 0f);
        float roughness = Fbm2D(sx, sz, 1f / 180f, 3, 0.5f, 1731f, -947f);

        float targetHeight = seaLevel
                           + (baseNoise - 0.5f) * 34f
                           + (roughness - 0.5f) * 22f;

        // 3D noise perturbs the density itself. Unlike a heightmap this allows
        // solid terrain to return above an air pocket/ledge.
        float low3D = Fbm3D(sx, y, sz, 1f / 72f, 4, 0.5f, 3917f);
        float high3D = Fbm3D(sx, y, sz, detailScale, 3, 0.5f, -2179f);

        float verticalDensity = (targetHeight - y) / 18f;
        float terrainNoise = (low3D - 0.5f) * 2.2f + (high3D - 0.5f) * 0.55f;

        // Force the upper world toward air and the bottom toward solid terrain.
        float topFadeStart = Mathf.Min(worldHeight - 1f, 112f);
        float topFade = y > topFadeStart ? (y - topFadeStart) / 8f : 0f;
        float bottomBoost = y < 8 ? (8 - y) / 4f : 0f;

        return verticalDensity + terrainNoise + bottomBoost - topFade;
    }

    private static float Fbm2D(float x, float z, float scale, int octaves, float persistence, float ox, float oz)
    {
        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float max = 0f;

        for (int i = 0; i < octaves; i++)
        {
            total += Mathf.PerlinNoise(
                (x + ox) * scale * frequency,
                (z + oz) * scale * frequency) * amplitude;
            max += amplitude;
            amplitude *= persistence;
            frequency *= 2f;
        }

        return total / max;
    }

    private static float Fbm3D(float x, float y, float z, float scale, int octaves, float persistence, float offset)
    {
        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float max = 0f;

        for (int i = 0; i < octaves; i++)
        {
            float px = (x + offset) * scale * frequency;
            float py = (y - offset * 0.37f) * scale * frequency;
            float pz = (z + offset * 0.61f) * scale * frequency;

            // Unity has no native 3D Perlin. Blend axis-pair samples into a
            // continuous deterministic 3D field.
            float n =
                Mathf.PerlinNoise(px, py) +
                Mathf.PerlinNoise(py, pz) +
                Mathf.PerlinNoise(px, pz) +
                Mathf.PerlinNoise(py, px) +
                Mathf.PerlinNoise(pz, py) +
                Mathf.PerlinNoise(pz, px);

            total += (n / 6f) * amplitude;
            max += amplitude;
            amplitude *= persistence;
            frequency *= 2f;
        }

        return total / max;
    }
}
