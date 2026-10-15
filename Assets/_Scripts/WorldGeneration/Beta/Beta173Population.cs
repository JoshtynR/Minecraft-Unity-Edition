using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Clean-room implementation of the Beta 1.7.3 chunk population pass.
/// Population uses canonical Beta world coordinates; conversion to Unity is
/// performed only when reading/writing blocks.
/// </summary>
public sealed partial class Beta173Population
{
    private readonly long worldSeed;

    public Beta173Population(long worldSeed)
    {
        this.worldSeed = worldSeed;
    }

    public void Populate(ChunkData owner, int betaChunkX, int betaChunkZ)
    {
        var world = new TreeWorld(owner);
        PopulateLakes(world, PopulationRandom(betaChunkX, betaChunkZ), betaChunkX*16, betaChunkZ*16);
        world.MarkDirtyChunks();
    }
    private JavaRandom PopulationRandom(int x, int z)
    {
        var random = new JavaRandom(worldSeed);
        long oddX = MakeOdd(random.NextLong()), oddZ = MakeOdd(random.NextLong());
        random.SetSeed(unchecked(((long)x*oddX+(long)z*oddZ)^worldSeed));
        return random;
    }
    private static void PopulateLakes(IBetaPopulationWorld world, JavaRandom random, int originX, int originZ)
    {
        if (random.NextInt(4) == 0)
        {
            int x = originX + random.NextInt(16) + 8;
            int y = random.NextInt(128);
            int z = originZ + random.NextInt(16) + 8;
            GenerateLake(world, random, x, y, z, BlockType.Water, false);
        }

        if (random.NextInt(8) == 0)
        {
            int x = originX + random.NextInt(16) + 8;
            int y = random.NextInt(random.NextInt(120) + 8);
            int z = originZ + random.NextInt(16) + 8;
            if (y < 64 || random.NextInt(10) == 0)
                GenerateLake(world, random, x, y, z, BlockType.Lava, true);
        }
    }

    // Tree-only preview while the earlier Beta population stages (dungeons/ores/etc.)
    // are still being ported. It deliberately uses a separate deterministic stream so
    // it cannot disturb the exact population RNG sequence later.
    public static bool HasTreeNeighborhood(ChunkData owner, int betaChunkX, int betaChunkZ)
    {
        // The +8 population window and large-oak canopy fit within this 2x2 area.
        for (int dx = 0; dx <= 1; dx++)
        for (int dz = 0; dz <= 1; dz++)
            if (Get(owner, betaChunkX * 16 + dx * 31, 0, betaChunkZ * 16 + dz * 31) == BlockType.Nothing)
                return false;
        return true;
    }

    public static bool HasCompleteNeighborhood(ChunkData owner, int chunkX, int chunkZ)
    {
        // Feature writes occupy 2x2; plant/lake light queries can reach a
        // further 15 cells. Require the surrounding raw-data halo as well.
        for (int dx=-1;dx<=2;dx++) for (int dz=-1;dz<=2;dz++)
            if (Get(owner,(chunkX+dx)*16,0,(chunkZ+dz)*16)==BlockType.Nothing) return false;
        return true;
    }

    public void PopulateTreePreview(ChunkData owner, Beta173Terrain terrain, int betaChunkX, int betaChunkZ)
    {
        BetaBiomeType biome = terrain.GetBiome(betaChunkX * 16 + 16, betaChunkZ * 16 + 16);
        // This is explicitly a tree-only stage. Its RNG is isolated until all
        // preceding population generators are ported; shapes/selection use Beta rules.
        long seed = unchecked(worldSeed ^ ((long)betaChunkX * 341873128712L) ^ ((long)betaChunkZ * 132897987541L) ^ 0x54A32D192ED03L);
        var random = new JavaRandom(seed);
        int originX = betaChunkX * 16, originZ = betaChunkZ * 16;
        int attempts = Beta173Trees.AttemptCount(biome, terrain.GetTreeCountNoise(originX, originZ), random);
        var world = new TreeWorld(owner);
        for (int i = 0; i < attempts; i++)
        {
            int x = originX + random.NextInt(16) + 8;
            int z = originZ + random.NextInt(16) + 8;
            var kind = Beta173Trees.Select(biome, random);
            int y = world.Height(x, z);
            Beta173Trees.Generate(world, random, kind, x, y, z);
        }
        world.MarkDirtyChunks();
    }

    private sealed partial class TreeWorld : IBetaPopulationWorld
    {
        private readonly ChunkData owner;
        private readonly HashSet<Vector3Int> changedChunks = new HashSet<Vector3Int>();
        public TreeWorld(ChunkData owner) { this.owner = owner; }
        public BlockType Get(int x, int y, int z)
        {
            return y < 0 || y >= 128 ? BlockType.Air : Beta173Population.Get(owner, x, y, z);
        }
        public void Set(int x, int y, int z, BlockType block)
        {
            if (y < 0 || y >= 128) return;
            if (Get(x, y, z) == block) return;
            skyColumns.Remove(new Vector3Int(x,0,z));
            Beta173Population.Set(owner, x, y, z, block);
            int ux = -x - 1;
            changedChunks.Add(new Vector3Int(BetaMathHelper.Floor(ux / 16.0) * 16, 0, BetaMathHelper.Floor(z / 16.0) * 16));
        }
        public void MarkDirtyChunks()
        {
            foreach (var chunk in changedChunks) owner.worldRef.NotifyTreeChunkChanged(chunk);
        }
        public bool IsOpaque(BlockType block)
        {
            return BetaPopulationFeatures.Opaque(block);
        }

        public int Height(int x, int z)
        {
            for (int y = 127; y >= 0; y--)
            {
                var block = Get(x, y, z);
                if (block == BlockType.Nothing) return 0;
                // Beta's height map includes leaves, logs, and liquids.
                if (BetaPopulationFeatures.Opacity(block) > 0) return y + 1;
            }
            return 0;
        }
    }

    private static long MakeOdd(long value) => unchecked(value / 2L * 2L + 1L);

    public static bool GenerateLake(IBetaPopulationWorld world, JavaRandom random, int betaX, int y, int betaZ, BlockType liquid, bool lava)
    {
        betaX -= 8;
        betaZ -= 8;
        while (y > 0 && world.Get(betaX, y, betaZ) == BlockType.Air) y--;
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
            BlockType block = world.Get(betaX + x, y + yy, betaZ + z);
            if (yy >= 4 && IsLiquid(block)) return false;
            if (yy < 4 && !IsSolid(block) && block != liquid) return false;
        }

        for (int x = 0; x < 16; x++)
        for (int z = 0; z < 16; z++)
        for (int yy = 0; yy < 8; yy++)
            if (carve[(x * 16 + z) * 8 + yy])
                world.Set(betaX + x, y + yy, betaZ + z, yy >= 4 ? BlockType.Air : liquid);

        for (int x = 0; x < 16; x++)
        for (int z = 0; z < 16; z++)
        for (int yy = 4; yy < 8; yy++)
            if (carve[(x * 16 + z) * 8 + yy] && world.Get(betaX + x, y + yy - 1, betaZ + z) == BlockType.Dirt && world.SkyLight(betaX + x, y + yy, betaZ + z) > 0)
                world.Set(betaX + x, y + yy - 1, betaZ + z, BlockType.Grass);

        if (lava)
        {
            for (int x = 0; x < 16; x++)
            for (int z = 0; z < 16; z++)
            for (int yy = 0; yy < 8; yy++)
                if (IsBoundary(carve, x, z, yy) && (yy < 4 || random.NextInt(2) != 0))
                {
                    int wx = betaX + x, wy = y + yy, wz = betaZ + z;
                    if (IsSolid(world.Get(wx, wy, wz))) world.Set(wx, wy, wz, BlockType.Stone);
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
    private static bool IsSolid(BlockType b) => BetaPopulationFeatures.Solid(b);
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
