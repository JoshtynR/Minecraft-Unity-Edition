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
        // BiomeGenBase.getBiome multiplies humidity by temperature first.
        double wetness = humidity * temperature;

        if (temperature < 0.1)
            return BetaBiomeType.Tundra;

        if (wetness < 0.2)
        {
            if (temperature < 0.5)
                return BetaBiomeType.Tundra;
            return temperature < 0.95 ? BetaBiomeType.Savanna : BetaBiomeType.Desert;
        }

        if (wetness > 0.5 && temperature < 0.7)
            return BetaBiomeType.Swampland;

        if (temperature < 0.5)
            return BetaBiomeType.Taiga;

        if (temperature < 0.97)
            return wetness < 0.35 ? BetaBiomeType.Shrubland : BetaBiomeType.Forest;

        if (wetness < 0.45)
            return BetaBiomeType.Plains;

        return wetness < 0.9 ? BetaBiomeType.SeasonalForest : BetaBiomeType.Rainforest;
    }

    public static BlockType TopBlock(BetaBiomeType biome)
    {
        // Beta's generateBiomeLookup changes Desert and Ice Desert to sand.
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
