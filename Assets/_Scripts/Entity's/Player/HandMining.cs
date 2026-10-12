using System;

public static class HandMining
{
    public static bool IsTarget(BlockType type) { return type != BlockType.Nothing && type != BlockType.Air && type != BlockType.Water && type != BlockType.Lava; }
    public static float Hardness(BlockType type)
    {
        switch(type)
        {
            case BlockType.Dirt: case BlockType.Sand: case BlockType.Ice: return 0.5f;
            case BlockType.Grass: case BlockType.Gravel: return 0.6f;
            case BlockType.Stone: return 1.5f;
            case BlockType.Sandstone: return 0.8f;
            case BlockType.Log: case BlockType.SpruceLog: case BlockType.BirchLog: case BlockType.WoodenSlab: return 2;
            case BlockType.Leaves: case BlockType.SpruceLeaves: case BlockType.BirchLeaves: return 0.2f;
            case BlockType.Glass: case BlockType.GlowStone: return 0.3f;
            default: return -1; // bedrock and fluids cannot be mined
        }
    }
    public static bool Harvestable(BlockType type)
    {
        // Beta glowstone and wooden slabs use rock material, unlike later Java.
        return IsTarget(type) && type != BlockType.Stone && type != BlockType.Sandstone && type != BlockType.WoodenSlab && type != BlockType.GlowStone && type != BlockType.Bedrock;
    }
    public static float DamagePerTick(BlockType type, bool grounded, bool underwater)
    {
        float hardness = Hardness(type);
        if (hardness < 0) return 0;
        if (!Harvestable(type)) return 1 / hardness / 100;
        float strength = 1;
        if (underwater) strength /= 5;
        if (!grounded) strength /= 5;
        return strength / hardness / 30;
    }
    public static BlockType ExistingDrop(BlockType type, int gravelRoll)
    {
        if (!Harvestable(type)) return BlockType.Nothing;
        switch(type)
        {
            case BlockType.Grass: return BlockType.Dirt;
            case BlockType.Glass: case BlockType.Ice:
            case BlockType.Leaves: case BlockType.BirchLeaves: case BlockType.SpruceLeaves: return BlockType.Nothing;
            // Flint/saplings aren't represented by the current block inventory.
            case BlockType.Gravel: return gravelRoll == 0 ? BlockType.Nothing : BlockType.Gravel;
            default: return type;
        }
    }
    public static int CrackStage(float progress) { return Math.Max(0, Math.Min(9, (int)(progress * 10))); }
}
