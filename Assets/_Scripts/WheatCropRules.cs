using System;

public static class WheatCropRules
{
    // ID 65 is the single live crop. IDs 126..132 remain reserved for old saves.
    public const BlockType Crop = BlockType.WheatCrop0;
    public static bool IsLegacy(BlockType type) => (int)type >= 126 && (int)type <= 132;
    public static bool IsCrop(BlockType type) => type == Crop || IsLegacy(type);
    public static byte LegacyAge(BlockType type) => IsLegacy(type) ? (byte)((int)type - 125) : (byte)0;
    public static BlockType Canonical(BlockType type) => IsCrop(type) ? Crop : type;
    public static BlockType LegacyTextureType(int age) => age == 0 ? Crop : (BlockType)(125 + Math.Max(1, Math.Min(7, age)));
    public static bool IsSoil(BlockType type) => type == BlockType.Farmland || type == BlockType.MoistFarmland;

    public static int GrowthBound(Func<int,int,BlockType> crops, Func<int,int,int> soil)
    {
        float rate = 1;
        for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
        {
            int moisture = soil(x,z);
            float contribution = moisture < 0 ? 0 : moisture > 0 ? 3 : 1;
            if (x != 0 || z != 0) contribution /= 4;
            rate += contribution;
        }
        bool eastWest = IsCrop(crops(-1,0)) || IsCrop(crops(1,0));
        bool northSouth = IsCrop(crops(0,-1)) || IsCrop(crops(0,1));
        bool diagonal = IsCrop(crops(-1,-1)) || IsCrop(crops(-1,1)) || IsCrop(crops(1,-1)) || IsCrop(crops(1,1));
        if (diagonal || (eastWest && northSouth)) rate /= 2;
        // Beta 1.7.3 uses this bound directly, without the later +1.
        return (int)(100f / rate);
    }

    public static int SeedDrops(int age, Func<int,int> next)
    {
        int count = 0; age = Math.Max(0, Math.Min(7, age));
        for (int i = 0; i < 3; i++) if (next(15) <= age) count++;
        return count;
    }
}
