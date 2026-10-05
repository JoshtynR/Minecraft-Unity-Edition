using UnityEngine;

/// <summary>
/// Clean-room implementation of the Beta 1.7.3 chunk population pass.
/// Population uses canonical Beta world coordinates; conversion to Unity is
/// performed only when reading/writing blocks.
/// </summary>
public sealed class Beta173Population
{
    private readonly long worldSeed;

    public Beta173Population(long worldSeed)
    {
        this.worldSeed = worldSeed;
    }

    public void Populate(ChunkData owner, int betaChunkX, int betaChunkZ)
    {
        var random = new JavaRandom(worldSeed);
        long oddX = MakeOdd(random.NextLong());
        long oddZ = MakeOdd(random.NextLong());
        random.SetSeed(unchecked(((long)betaChunkX * oddX + (long)betaChunkZ * oddZ) ^ worldSeed));

        int originX = betaChunkX * 16;
        int originZ = betaChunkZ * 16;

        if (random.NextInt(4) == 0)
        {
            int x = originX + random.NextInt(16) + 8;
            int y = random.NextInt(128);
            int z = originZ + random.NextInt(16) + 8;
            GenerateLake(owner, random, x, y, z, BlockType.Water, false);
        }

        if (random.NextInt(8) == 0)
        {
            int x = originX + random.NextInt(16) + 8;
            int y = random.NextInt(random.NextInt(120) + 8);
            int z = originZ + random.NextInt(16) + 8;
            if (y < 64 || random.NextInt(10) == 0)
                GenerateLake(owner, random, x, y, z, BlockType.Lava, true);
        }
    }

    // Tree-only preview while the earlier Beta population stages (dungeons/ores/etc.)
    // are still being ported. It deliberately uses a separate deterministic stream so
    // it cannot disturb the exact population RNG sequence later.
    public void PopulateTreePreview(ChunkData owner, Beta173Terrain terrain, int betaChunkX, int betaChunkZ)
    {
        BetaBiomeType biome = terrain.GetBiome(betaChunkX * 16 + 16, betaChunkZ * 16 + 16);
        int attempts;
        switch (biome)
        {
            case BetaBiomeType.Forest:
            case BetaBiomeType.Rainforest: attempts = 7; break;
            case BetaBiomeType.SeasonalForest: attempts = 4; break;
            case BetaBiomeType.Taiga: attempts = 7; break;
            case BetaBiomeType.Shrubland:
            case BetaBiomeType.Swampland: attempts = 2; break;
            default: attempts = 0; break;
        }
        if (attempts == 0) return;

        long seed = unchecked(worldSeed ^ ((long)betaChunkX * 341873128712L) ^ ((long)betaChunkZ * 132897987541L) ^ 0x54A32D192ED03L);
        var random = new JavaRandom(seed);
        int originX = betaChunkX * 16;
        int originZ = betaChunkZ * 16;

        for (int i = 0; i < attempts; i++)
        {
            int x = originX + random.NextInt(16) + 8;
            int z = originZ + random.NextInt(16) + 8;
            int y = GetHeight(owner, x, z);
            GenerateClassicTree(owner, random, x, y, z);
        }
    }

    private static bool GenerateClassicTree(ChunkData owner, JavaRandom random, int x, int y, int z)
    {
        int height = random.NextInt(3) + 4;
        if (y < 1 || y + height + 1 > 128) return false;

        for (int yy = y; yy <= y + height + 1; yy++)
        {
            int radius = yy == y ? 0 : (yy >= y + height - 1 ? 2 : 1);
            for (int xx = x - radius; xx <= x + radius; xx++)
            for (int zz = z - radius; zz <= z + radius; zz++)
            {
                BlockType block = Get(owner, xx, yy, zz);
                if (block != BlockType.Air && block != BlockType.Leaves) return false;
            }
        }

        BlockType ground = Get(owner, x, y - 1, z);
        if ((ground != BlockType.Grass && ground != BlockType.Dirt) || y >= 127 - height) return false;
        Set(owner, x, y - 1, z, BlockType.Dirt);

        for (int yy = y - 3 + height; yy <= y + height; yy++)
        {
            int layer = yy - (y + height);
            int radius = 1 - layer / 2;
            for (int xx = x - radius; xx <= x + radius; xx++)
            for (int zz = z - radius; zz <= z + radius; zz++)
            {
                int dx = xx - x, dz = zz - z;
                if ((System.Math.Abs(dx) != radius || System.Math.Abs(dz) != radius || random.NextInt(2) != 0 || layer == 0) &&
                    !IsSolid(Get(owner, xx, yy, zz)))
                    Set(owner, xx, yy, zz, BlockType.Leaves);
            }
        }

        for (int yy = 0; yy < height; yy++)
        {
            BlockType block = Get(owner, x, y + yy, z);
            if (block == BlockType.Air || block == BlockType.Leaves)
                Set(owner, x, y + yy, z, BlockType.Log);
        }
        return true;
    }

    private static int GetHeight(ChunkData owner, int betaX, int betaZ)
    {
        for (int y = 127; y >= 0; y--)
        {
            BlockType b = Get(owner, betaX, y, betaZ);
            if (b != BlockType.Air && b != BlockType.Nothing && b != BlockType.Leaves && b != BlockType.Log)
                return y + 1;
        }
        return 0;
    }

    private static long MakeOdd(long value) => unchecked(value / 2L * 2L + 1L);

    private static bool GenerateLake(ChunkData owner, JavaRandom random, int betaX, int y, int betaZ, BlockType liquid, bool lava)
    {
        betaX -= 8;
        betaZ -= 8;
        while (y > 0 && IsAir(owner, betaX, y, betaZ)) y--;
        y -= 4;

        var carve = new bool[2048];
        int ellipsoids = random.NextInt(4) + 4;
        for (int e = 0; e < ellipsoids; e++)
        {
            double sx = random.NextDouble() * 6.0 + 3.0;
            double sy = random.NextDouble() * 4.0 + 2.0;
            double sz = random.NextDouble() * 6.0 + 3.0;
            double cx = random.NextDouble() * (16.0 - sx - 2.0) + 1.0 + sx / 2.0;
            double cy = random.NextDouble() * (8.0 - sy - 4.0) + 2.0 + sy / 2.0;
            double cz = random.NextDouble() * (16.0 - sz - 2.0) + 1.0 + sz / 2.0;

            for (int x = 1; x < 15; x++)
            for (int z = 1; z < 15; z++)
            for (int yy = 1; yy < 7; yy++)
            {
                double dx = (x - cx) / (sx / 2.0);
                double dy = (yy - cy) / (sy / 2.0);
                double dz = (z - cz) / (sz / 2.0);
                if (dx * dx + dy * dy + dz * dz < 1.0)
                    carve[(x * 16 + z) * 8 + yy] = true;
            }
        }

        for (int x = 0; x < 16; x++)
        for (int z = 0; z < 16; z++)
        for (int yy = 0; yy < 8; yy++)
        {
            bool edge = IsBoundary(carve, x, z, yy);
            if (!edge) continue;
            BlockType block = Get(owner, betaX + x, y + yy, betaZ + z);
            if (yy >= 4 && IsLiquid(block)) return false;
            if (yy < 4 && !IsSolid(block) && block != liquid) return false;
        }

        for (int x = 0; x < 16; x++)
        for (int z = 0; z < 16; z++)
        for (int yy = 0; yy < 8; yy++)
            if (carve[(x * 16 + z) * 8 + yy])
                Set(owner, betaX + x, y + yy, betaZ + z, yy >= 4 ? BlockType.Air : liquid);

        for (int x = 0; x < 16; x++)
        for (int z = 0; z < 16; z++)
        for (int yy = 4; yy < 8; yy++)
            if (carve[(x * 16 + z) * 8 + yy] && Get(owner, betaX + x, y + yy - 1, betaZ + z) == BlockType.Dirt)
                Set(owner, betaX + x, y + yy - 1, betaZ + z, BlockType.Grass);

        if (lava)
        {
            for (int x = 0; x < 16; x++)
            for (int z = 0; z < 16; z++)
            for (int yy = 0; yy < 8; yy++)
                if (IsBoundary(carve, x, z, yy) && (yy < 4 || random.NextInt(2) != 0))
                {
                    int wx = betaX + x, wy = y + yy, wz = betaZ + z;
                    if (IsSolid(Get(owner, wx, wy, wz))) Set(owner, wx, wy, wz, BlockType.Stone);
                }
        }
        return true;
    }

    private static bool IsBoundary(bool[] c, int x, int z, int y)
    {
        int i = (x * 16 + z) * 8 + y;
        if (c[i]) return false;
        return (x < 15 && c[((x + 1) * 16 + z) * 8 + y]) ||
               (x > 0 && c[((x - 1) * 16 + z) * 8 + y]) ||
               (z < 15 && c[(x * 16 + z + 1) * 8 + y]) ||
               (z > 0 && c[(x * 16 + z - 1) * 8 + y]) ||
               (y < 7 && c[(x * 16 + z) * 8 + y + 1]) ||
               (y > 0 && c[(x * 16 + z) * 8 + y - 1]);
    }

    private static bool IsLiquid(BlockType b) => b == BlockType.Water || b == BlockType.Lava;
    private static bool IsSolid(BlockType b) => b != BlockType.Air && b != BlockType.Water && b != BlockType.Lava && b != BlockType.Nothing;
    private static bool IsAir(ChunkData owner, int x, int y, int z) => Get(owner, x, y, z) == BlockType.Air;

    private static BlockType Get(ChunkData owner, int betaWorldX, int y, int betaWorldZ)
    {
        int unityWorldX = -betaWorldX - 1;
        return owner.worldRef.GetBlock(new Vector3Int(unityWorldX, y, betaWorldZ)).type;
    }

    private static void Set(ChunkData owner, int betaWorldX, int y, int betaWorldZ, BlockType block)
    {
        int unityWorldX = -betaWorldX - 1;
        WorldDataHelper.SetBlock(owner.worldRef, new Vector3Int(unityWorldX, y, betaWorldZ), block);
    }
}
