using UnityEngine;

public static class BiomeBlockColors
{
    private static readonly object samplerLock = new object();
    private static Sampler sampler;

    public static Color ForFace(BlockType type, Direction face, ChunkData chunk, Vector3Int local, Color configured, bool grassOverlay = false)
    {
        int rgb;
        if (type == BlockType.SpruceLeaves) rgb = BiomeColorPalette.Pine;
        else if (type == BlockType.BirchLeaves) rgb = BiomeColorPalette.Birch;
        else if (type == BlockType.DryTallGrass) rgb = 0xFFFFFF;
        else if (type == BlockType.Grass && face != Direction.up && !grassOverlay) rgb = 0xFFFFFF;
        else if (type == BlockType.Grass || type == BlockType.Leaves || type == BlockType.TallGrass || type == BlockType.Fern)
        {
            var palette = BiomeColorPalette.Current;
            if (chunk == null)
                rgb = type == BlockType.Leaves ? BiomeColorPalette.BasicFoliage : palette.Grass(0.5, 1.0);
            else
            {
                var colors = chunk.biomeColors;
                long seed = chunk.worldRef.betaWorldSeed;
                if (colors == null || colors.source.seed != seed || colors.source.palette != palette)
                {
                    colors = new ChunkColors(GetSampler(seed, palette), chunk.worldPos);
                    chunk.biomeColors = colors;
                }
                if (type == BlockType.TallGrass || type == BlockType.Fern)
                {
                    var world = chunk.GetGlobalBlockCoords(local);
                    BiomeColorPalette.TallGrassSample(-world.x - 1, world.y, world.z, out int x, out int z);
                    rgb = colors.source.GrassAt(x, z);
                }
                else
                {
                    int index = local.x * 16 + local.z;
                    rgb = type == BlockType.Leaves ? colors.foliage[index] : colors.grass[index];
                }
            }
        }
        else return configured;
        return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, configured.a);
    }

    private static Sampler GetSampler(long seed, BiomeColorPalette.Palette palette)
    {
        lock (samplerLock)
        {
            if (sampler == null || sampler.seed != seed || sampler.palette != palette)
                sampler = new Sampler(seed, palette);
            return sampler;
        }
    }

    internal sealed class Sampler
    {
        public readonly long seed;
        public readonly BiomeColorPalette.Palette palette;
        public readonly BetaClimate climate;
        public Sampler(long seed, BiomeColorPalette.Palette palette)
        {
            this.seed = seed; this.palette = palette; climate = new BetaClimate(seed);
        }
        public int GrassAt(int x, int z)
        {
            climate.Sample(x, z, out double temperature, out double humidity);
            return palette.Grass(temperature, humidity);
        }
    }

    // Owned by ChunkData and reclaimed with it. No growing global chunk cache,
    // no save changes, and no climate resampling on each leaf face or lighting rebuild.
    public sealed class ChunkColors
    {
        internal readonly Sampler source;
        internal readonly int[] grass = new int[256], foliage = new int[256];
        internal ChunkColors(Sampler source, Vector3Int origin)
        {
            this.source = source;
            source.climate.SampleRegion(-origin.x - 16, origin.z, 16, 16, out double[] temperatures, out double[] humidities);
            for (int x = 0; x < 16; x++)
            for (int z = 0; z < 16; z++)
            {
                int beta = (15 - x) * 16 + z, unity = x * 16 + z;
                grass[unity] = source.palette.Grass(temperatures[beta], humidities[beta]);
                foliage[unity] = source.palette.Foliage(temperatures[beta], humidities[beta]);
            }
        }
    }
}
