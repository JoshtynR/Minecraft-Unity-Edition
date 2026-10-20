using System;
using UnityEngine;

// PNG pixels are read once on the main thread. Mesh workers only read these
// immutable CPU tables; they never access a Texture2D or the GPU.
public static class BiomeColorPalette
{
    public const int Pine = 0x619961;
    public const int Birch = 0x80A755;
    public const int BasicFoliage = 0x48B518;
    private static volatile Palette current = new Palette(null, null);
    public static Palette Current => current;

    public static void Initialize(Color32[] grass, Color32[] foliage)
    {
        current = new Palette(TopDown(grass), TopDown(foliage));
    }

    private static int[] TopDown(Color32[] pixels)
    {
        if (pixels == null || pixels.Length != 256 * 256) return null;
        var colors = new int[pixels.Length];
        for (int y = 0; y < 256; y++)
        for (int x = 0; x < 256; x++)
        {
            var p = pixels[(255 - y) * 256 + x];
            colors[y * 256 + x] = p.r << 16 | p.g << 8 | p.b;
        }
        return colors;
    }

    public static int LookupIndex(double temperature, double humidity)
    {
        temperature = Clamp(temperature);
        humidity = Clamp(humidity) * temperature;
        int x = (int)((1.0 - temperature) * 255.0);
        int y = (int)((1.0 - humidity) * 255.0);
        return y << 8 | x;
    }

    private static double Clamp(double value) => double.IsNaN(value) ? 0 : Math.Max(0, Math.Min(1, value));

    // Beta tall grass/ferns perturb the climate sample using the block's
    // canonical position. This is separate from the plant's geometry offset.
    public static void TallGrassSample(int x, int y, int z, out int sampleX, out int sampleZ)
    {
        unchecked
        {
            long hash = (long)(x * 3129871 + z * 6129781 + y);
            hash = hash * hash * 42317861L + hash * 11L;
            sampleX = (int)((long)x + ((hash >> 14) & 31L));
            sampleZ = (int)((long)z + ((hash >> 24) & 31L));
        }
    }

    public sealed class Palette
    {
        private readonly int[] grass, foliage;
        internal Palette(int[] grass, int[] foliage) { this.grass = grass; this.foliage = foliage; }
        public int Grass(double temperature, double humidity) => grass == null ? 0x7CBD6B : grass[LookupIndex(temperature, humidity)];
        public int Foliage(double temperature, double humidity) => foliage == null ? BasicFoliage : foliage[LookupIndex(temperature, humidity)];
    }
}
