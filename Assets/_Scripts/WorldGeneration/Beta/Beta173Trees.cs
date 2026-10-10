using System;
using System.Collections.Generic;

public enum BetaTreeKind { Oak, LargeOak, Birch, SpruceTall, SpruceFull }

// Coordinates stay in Beta space. The Unity adapter performs the X reflection.
public interface IBetaTreeWorld
{
    BlockType Get(int x, int y, int z);
    void Set(int x, int y, int z, BlockType block);
    bool IsOpaque(BlockType block);
}

public static class Beta173Trees
{
    public static bool IsLeaves(BlockType block)
    {
        return block == BlockType.Leaves || block == BlockType.SpruceLeaves || block == BlockType.BirchLeaves;
    }
    public static bool IsLog(BlockType block)
    {
        return block == BlockType.Log || block == BlockType.SpruceLog || block == BlockType.BirchLog;
    }
    private static bool Replaceable(BlockType block) { return block == BlockType.Air || IsLeaves(block); }

    public static BetaTreeKind Select(BetaBiomeType biome, JavaRandom random)
    {
        if (biome == BetaBiomeType.Taiga)
            return random.NextInt(3) == 0 ? BetaTreeKind.SpruceTall : BetaTreeKind.SpruceFull;
        if (biome == BetaBiomeType.Forest)
        {
            if (random.NextInt(5) == 0) return BetaTreeKind.Birch;
            return random.NextInt(3) == 0 ? BetaTreeKind.LargeOak : BetaTreeKind.Oak;
        }
        if (biome == BetaBiomeType.Rainforest)
            return random.NextInt(3) == 0 ? BetaTreeKind.LargeOak : BetaTreeKind.Oak;
        return random.NextInt(10) == 0 ? BetaTreeKind.LargeOak : BetaTreeKind.Oak;
    }

    // Supply the population RNG at the tree stage. Earlier population stages
    // must consume that same stream before this is used for exact world placement.
    public static int AttemptCount(BetaBiomeType biome, double noise, JavaRandom random)
    {
        int density = (int)((noise / 8.0 + random.NextDouble() * 4.0 + 4.0) / 3.0);
        int count = random.NextInt(10) == 0 ? 1 : 0;
        switch (biome)
        {
            case BetaBiomeType.Forest:
            case BetaBiomeType.Rainforest:
            case BetaBiomeType.Taiga: return count + density + 5;
            case BetaBiomeType.SeasonalForest: return count + density + 2;
            case BetaBiomeType.Desert:
            case BetaBiomeType.Tundra:
            case BetaBiomeType.Plains: return count - 20;
            default: return count;
        }
    }

    public static bool Generate(IBetaTreeWorld world, JavaRandom random, BetaTreeKind kind, int x, int y, int z)
    {
        switch (kind)
        {
            case BetaTreeKind.LargeOak: return new LargeOak(world, random, x, y, z).Generate();
            case BetaTreeKind.SpruceTall: return Spruce(world, random, x, y, z, true);
            case BetaTreeKind.SpruceFull: return Spruce(world, random, x, y, z, false);
            default: return Small(world, random, x, y, z, kind == BetaTreeKind.Birch);
        }
    }

    private static bool Ground(IBetaTreeWorld world, int x, int y, int z, int height)
    {
        var block = world.Get(x, y - 1, z);
        return (block == BlockType.Grass || block == BlockType.Dirt) && y < 127 - height;
    }

    private static bool Small(IBetaTreeWorld world, JavaRandom random, int x, int y, int z, bool birch)
    {
        int height = random.NextInt(3) + (birch ? 5 : 4);
        if (y < 1 || y + height + 1 > 128) return false;
        for (int yy = y; yy <= y + height + 1; yy++)
        {
            int radius = yy == y ? 0 : 1;
            if (yy >= y + height - 1) radius = 2;
            for (int xx = x - radius; xx <= x + radius; xx++)
            for (int zz = z - radius; zz <= z + radius; zz++)
                if (yy >= 128 || !Replaceable(world.Get(xx, yy, zz))) return false;
        }
        if (!Ground(world, x, y, z, height)) return false;
        world.Set(x, y - 1, z, BlockType.Dirt);
        var leaves = birch ? BlockType.BirchLeaves : BlockType.Leaves;
        var log = birch ? BlockType.BirchLog : BlockType.Log;
        for (int yy = y + height - 3; yy <= y + height; yy++)
        {
            int layer = yy - y - height;
            int radius = 1 - layer / 2;
            for (int xx = x - radius; xx <= x + radius; xx++)
            for (int zz = z - radius; zz <= z + radius; zz++)
            {
                // Keep short-circuit order: even excluded top corners consume RNG.
                if ((Math.Abs(xx - x) != radius || Math.Abs(zz - z) != radius ||
                     (random.NextInt(2) != 0 && layer != 0)) && !world.IsOpaque(world.Get(xx, yy, zz)))
                    world.Set(xx, yy, zz, leaves);
            }
        }
        for (int yy = 0; yy < height; yy++)
            if (Replaceable(world.Get(x, y + yy, z))) world.Set(x, y + yy, z, log);
        return true;
    }

    private static bool Spruce(IBetaTreeWorld world, JavaRandom random, int x, int y, int z, bool tall)
    {
        int height = tall ? random.NextInt(5) + 7 : random.NextInt(4) + 6;
        int bare = tall ? height - random.NextInt(2) - 3 : 1 + random.NextInt(2);
        int foliage = height - bare;
        int maxRadius = tall ? 1 + random.NextInt(foliage + 1) : 2 + random.NextInt(2);
        if (y < 1 || y + height + 1 > 128) return false;
        for (int yy = y; yy <= y + height + 1; yy++)
        {
            int radius = yy - y < bare ? 0 : maxRadius;
            for (int xx = x - radius; xx <= x + radius; xx++)
            for (int zz = z - radius; zz <= z + radius; zz++)
                if (yy >= 128 || !Replaceable(world.Get(xx, yy, zz))) return false;
        }
        if (!Ground(world, x, y, z, height)) return false;
        world.Set(x, y - 1, z, BlockType.Dirt);
        int r = tall ? 0 : random.NextInt(2);
        int limit = 1, reset = 0;
        for (int yy = y + height; yy >= y + bare; yy--)
        {
            for (int xx = x - r; xx <= x + r; xx++)
            for (int zz = z - r; zz <= z + r; zz++)
                if ((Math.Abs(xx - x) != r || Math.Abs(zz - z) != r || r == 0) && !world.IsOpaque(world.Get(xx, yy, zz)))
                    world.Set(xx, yy, zz, BlockType.SpruceLeaves);
            if (tall)
            {
                if (r >= 1 && yy == y + bare + 1) r--;
                else if (r < maxRadius) r++;
            }
            else if (r >= limit)
            {
                r = reset; reset = 1; limit++;
                if (limit > maxRadius) limit = maxRadius;
            }
            else r++;
        }
        int trunkHeight = tall ? height - 1 : height - random.NextInt(3);
        for (int yy = 0; yy < trunkHeight; yy++)
            if (Replaceable(world.Get(x, y + yy, z))) world.Set(x, y + yy, z, BlockType.SpruceLog);
        return true;
    }

    private sealed class LargeOak
    {
        private readonly IBetaTreeWorld world;
        private readonly JavaRandom random;
        private readonly int[] origin;
        private int height, trunkHeight;
        // ChunkProviderGenerate calls scale(1,1,1): five-leaf layers, height bound 12.
        private const int LeafLayers = 5;
        private readonly List<int[]> nodes = new List<int[]>();
        private static readonly int[] Axis = { 2, 0, 0, 1, 2, 1 };
        public LargeOak(IBetaTreeWorld world, JavaRandom populationRandom, int x, int y, int z)
        {
            this.world = world;
            random = new JavaRandom(populationRandom.NextLong());
            origin = new[] { x, y, z };
            height = 5 + random.NextInt(12);
        }
        public bool Generate()
        {
            var ground = world.Get(origin[0], origin[1] - 1, origin[2]);
            if (ground != BlockType.Grass && ground != BlockType.Dirt) return false;
            int obstruction = Line(origin, new[] { origin[0], origin[1] + height - 1, origin[2] }, false);
            if (obstruction != -1)
            {
                if (obstruction < 6) return false;
                height = obstruction;
            }
            BuildNodes();
            foreach (var node in nodes)
                for (int layer = 0; layer < LeafLayers; layer++)
                    LeafDisc(node[0], node[1] + layer, node[2], layer == 0 || layer == LeafLayers - 1 ? 2 : 3);
            Line(origin, new[] { origin[0], origin[1] + trunkHeight, origin[2] }, true);
            foreach (var node in nodes)
                if ((double)(node[3] - origin[1]) >= height * 0.2)
                    Line(new[] { origin[0], node[3], origin[2] }, new[] { node[0], node[1], node[2] }, true);
            return true;
        }
        private void BuildNodes()
        {
            trunkHeight = (int)(height * 0.618);
            if (trunkHeight >= height) trunkHeight = height - 1;
            int perLayer = Math.Max(1, (int)(1.382 + Math.Pow(height / 13.0, 2.0)));
            int yy = origin[1] + height - LeafLayers;
            int branchTop = origin[1] + trunkHeight;
            int relative = yy - origin[1];
            nodes.Add(new[] { origin[0], yy, origin[2], branchTop });
            yy--;
            // Beta computes the first radius with the previous layer's relative Y.
            while (relative >= 0)
            {
                float radius = LayerRadius(relative);
                if (radius >= 0)
                    for (int i = 0; i < perLayer; i++)
                    {
                        double distance = radius * ((double)random.NextFloat() + 0.328);
                        double angle = (double)random.NextFloat() * 2.0 * 3.14159;
                        int xx = BetaMathHelper.Floor(distance * Math.Sin(angle) + origin[0] + 0.5);
                        int zz = BetaMathHelper.Floor(distance * Math.Cos(angle) + origin[2] + 0.5);
                        int[] leaf = { xx, yy, zz };
                        if (Line(leaf, new[] { xx, yy + LeafLayers, zz }, false) != -1) continue;
                        double offset = Math.Sqrt(Math.Pow(Math.Abs(origin[0] - xx), 2.0) + Math.Pow(Math.Abs(origin[2] - zz), 2.0)) * 0.381;
                        int branchY = yy - offset > branchTop ? branchTop : (int)(yy - offset);
                        if (Line(new[] { origin[0], branchY, origin[2] }, leaf, false) == -1)
                            nodes.Add(new[] { xx, yy, zz, branchY });
                    }
                yy--; relative--;
            }
        }
        private float LayerRadius(int layer)
        {
            if (layer < (double)(float)height * 0.3) return -1.618f;
            float half = (float)height / 2f, offset = half - layer;
            float radius = offset == 0 ? half : Math.Abs(offset) >= half ? 0 :
                (float)Math.Sqrt(Math.Pow(Math.Abs(half), 2.0) - Math.Pow(Math.Abs(offset), 2.0));
            return radius * 0.5f;
        }
        private void LeafDisc(int x, int y, int z, float radius)
        {
            int extent = (int)((double)radius + 0.618);
            // For the horizontal disc, Beta's secondary axes are X then Z.
            for (int dx = -extent; dx <= extent; dx++)
            for (int dz = -extent; dz <= extent; dz++)
                if (Math.Sqrt(Math.Pow(Math.Abs(dx) + 0.5, 2.0) + Math.Pow(Math.Abs(dz) + 0.5, 2.0)) <= radius &&
                    Replaceable(world.Get(x + dx, y, z + dz)))
                    world.Set(x + dx, y, z + dz, BlockType.Leaves);
        }
        // Obstruction checks truncate with floor; log placement rounds by adding
        // 0.5. These deliberately differ, including at negative coordinates.
        private int Line(int[] start, int[] end, bool place)
        {
            int[] delta = { end[0] - start[0], end[1] - start[1], end[2] - start[2] };
            int major = 0;
            for (int axis = 0; axis < 3; axis++)
                if (Math.Abs(delta[axis]) > Math.Abs(delta[major])) major = axis;
            if (delta[major] == 0) return -1;
            int a = Axis[major], b = Axis[major + 3];
            int step = delta[major] > 0 ? 1 : -1;
            double slopeA = (double)delta[a] / delta[major], slopeB = (double)delta[b] / delta[major];
            int[] p = new int[3];
            for (int i = 0; i != delta[major] + step; i += step)
            {
                p[major] = start[major] + i;
                p[a] = BetaMathHelper.Floor(start[a] + i * slopeA + (place ? 0.5 : 0));
                p[b] = BetaMathHelper.Floor(start[b] + i * slopeB + (place ? 0.5 : 0));
                if (place) world.Set(p[0], p[1], p[2], BlockType.Log);
                else if (!Replaceable(world.Get(p[0], p[1], p[2]))) return Math.Abs(i);
            }
            return -1;
        }
    }
}
