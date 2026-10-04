/// <summary>
/// Beta 1.7.3 climate-to-biome lookup behavior from BiomeGenBase.
/// Only terrain-surface properties live here for now; population behavior is
/// added as the corresponding Beta generators are ported.
/// </summary>
public enum BetaBiomeType
{
    Rainforest,
    Swampland,
    SeasonalForest,
    Forest,
    Savanna,
    Shrubland,
    Taiga,
    Desert,
    Plains,
    IceDesert,
    Tundra
}

public static class BetaBiome
{
    public static BetaBiomeType FromClimate(double temperature, double humidity)
    {
        // Beta does not call getBiome() with the raw climate doubles here.
        // getBiomeFromLookup first quantizes both values onto a 64x64 table,
        // whose entries were built using float coordinates i / 63.0F.
        int temperatureIndex = (int)(temperature * 63.0);
        int humidityIndex = (int)(humidity * 63.0);
        if (temperatureIndex < 0) temperatureIndex = 0;
        else if (temperatureIndex > 63) temperatureIndex = 63;
        if (humidityIndex < 0) humidityIndex = 0;
        else if (humidityIndex > 63) humidityIndex = 63;

        float t = temperatureIndex / 63.0f;
        float h = humidityIndex / 63.0f;
        h *= t;

        if (t < 0.1f)
            return BetaBiomeType.Tundra;

        if (h < 0.2f)
        {
            if (t < 0.5f)
                return BetaBiomeType.Tundra;
            return t < 0.95f ? BetaBiomeType.Savanna : BetaBiomeType.Desert;
        }

        if (h > 0.5f && t < 0.7f)
            return BetaBiomeType.Swampland;

        if (t < 0.5f)
            return BetaBiomeType.Taiga;

        if (t < 0.97f)
            return h < 0.35f ? BetaBiomeType.Shrubland : BetaBiomeType.Forest;

        if (h < 0.45f)
            return BetaBiomeType.Plains;

        return h < 0.9f ? BetaBiomeType.SeasonalForest : BetaBiomeType.Rainforest;
    }

    public static BlockType TopBlock(BetaBiomeType biome)
    {
        return biome == BetaBiomeType.Desert || biome == BetaBiomeType.IceDesert
            ? BlockType.Sand
            : BlockType.Grass;
    }

    public static BlockType FillerBlock(BetaBiomeType biome)
    {
        return biome == BetaBiomeType.Desert || biome == BetaBiomeType.IceDesert
            ? BlockType.Sand
            : BlockType.Dirt;
    }

    public static bool HasSnow(BetaBiomeType biome)
    {
        return biome == BetaBiomeType.Taiga ||
               biome == BetaBiomeType.IceDesert ||
               biome == BetaBiomeType.Tundra;
    }
}
