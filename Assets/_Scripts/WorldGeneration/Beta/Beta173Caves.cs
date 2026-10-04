using System;
using UnityEngine;

/// <summary>
/// Clean C# behavioral port of Beta 1.7.3 MapGenBase + MapGenCaves.
/// Carves the already-generated 16x128x16 chunk after surface replacement.
/// </summary>
public sealed class Beta173Caves
{
    private const int Range = 8;
    private readonly long worldSeed;
    private readonly JavaRandom random = new JavaRandom(0L);

    public Beta173Caves(long worldSeed)
    {
        this.worldSeed = worldSeed;
    }

    public void Generate(ChunkData chunk, int targetChunkX, int targetChunkZ)
    {
        random.SetSeed(worldSeed);
        long seedX = MakeOdd(random.NextLong());
        long seedZ = MakeOdd(random.NextLong());

        for (int sourceChunkX = targetChunkX - Range; sourceChunkX <= targetChunkX + Range; sourceChunkX++)
        for (int sourceChunkZ = targetChunkZ - Range; sourceChunkZ <= targetChunkZ + Range; sourceChunkZ++)
        {
            long seed = unchecked((long)sourceChunkX * seedX + (long)sourceChunkZ * seedZ) ^ worldSeed;
            random.SetSeed(seed);
            GenerateFromSource(chunk, sourceChunkX, sourceChunkZ, targetChunkX, targetChunkZ);
        }
    }

    private static long MakeOdd(long value)
    {
        // Java integer division truncates toward zero, as does C#.
        return unchecked(value / 2L * 2L + 1L);
    }

    private void GenerateFromSource(ChunkData chunk, int sourceChunkX, int sourceChunkZ, int targetChunkX, int targetChunkZ)
    {
        int caveCount = random.NextInt(random.NextInt(random.NextInt(40) + 1) + 1);
        if (random.NextInt(15) != 0)
            caveCount = 0;

        for (int i = 0; i < caveCount; i++)
        {
            double x = sourceChunkX * 16 + random.NextInt(16);
            double y = random.NextInt(random.NextInt(120) + 8);
            double z = sourceChunkZ * 16 + random.NextInt(16);
            int tunnels = 1;

            if (random.NextInt(4) == 0)
            {
                CarveRoom(chunk, targetChunkX, targetChunkZ, x, y, z);
                tunnels += random.NextInt(4);
            }

            for (int tunnel = 0; tunnel < tunnels; tunnel++)
            {
                float yaw = random.NextFloat() * Mathf.PI * 2.0f;
                float pitch = (random.NextFloat() - 0.5f) * 2.0f / 8.0f;
                float width = random.NextFloat() * 2.0f + random.NextFloat();
                CarveTunnel(chunk, targetChunkX, targetChunkZ, x, y, z, width, yaw, pitch, 0, 0, 1.0);
            }
        }
    }

    private void CarveRoom(ChunkData chunk, int chunkX, int chunkZ, double x, double y, double z)
    {
        CarveTunnel(chunk, chunkX, chunkZ, x, y, z,
            1.0f + random.NextFloat() * 6.0f, 0.0f, 0.0f, -1, -1, 0.5);
    }

    private void CarveTunnel(ChunkData chunk, int chunkX, int chunkZ,
        double x, double y, double z, float width, float yaw, float pitch,
        int step, int maxSteps, double verticalScale)
    {
        double centerX = chunkX * 16 + 8;
        double centerZ = chunkZ * 16 + 8;
        float yawVelocity = 0.0f;
        float pitchVelocity = 0.0f;
        var localRandom = new JavaRandom(random.NextLong());

        if (maxSteps <= 0)
        {
            int distance = Range * 16 - 16;
            maxSteps = distance - localRandom.NextInt(distance / 4);
        }

        bool room = false;
        if (step == -1)
        {
            step = maxSteps / 2;
            room = true;
        }

        int branchStep = localRandom.NextInt(maxSteps / 2) + maxSteps / 4;
        bool gentlePitch = localRandom.NextInt(6) == 0;

        for (; step < maxSteps; step++)
        {
            double radiusXZ = 1.5 + Math.Sin(step * Math.PI / maxSteps) * width;
            double radiusY = radiusXZ * verticalScale;
            float cosPitch = Mathf.Cos(pitch);
            float sinPitch = Mathf.Sin(pitch);
            x += Mathf.Cos(yaw) * cosPitch;
            y += sinPitch;
            z += Mathf.Sin(yaw) * cosPitch;
            pitch *= gentlePitch ? 0.92f : 0.7f;
            pitch += pitchVelocity * 0.1f;
            yaw += yawVelocity * 0.1f;
            pitchVelocity *= 0.9f;
            yawVelocity *= 0.75f;
            pitchVelocity += (localRandom.NextFloat() - localRandom.NextFloat()) * localRandom.NextFloat() * 2.0f;
            yawVelocity += (localRandom.NextFloat() - localRandom.NextFloat()) * localRandom.NextFloat() * 4.0f;

            if (!room && step == branchStep && width > 1.0f)
            {
                CarveTunnel(chunk, chunkX, chunkZ, x, y, z,
                    localRandom.NextFloat() * 0.5f + 0.5f,
                    yaw - Mathf.PI / 2.0f, pitch / 3.0f, step, maxSteps, 1.0);
                CarveTunnel(chunk, chunkX, chunkZ, x, y, z,
                    localRandom.NextFloat() * 0.5f + 0.5f,
                    yaw + Mathf.PI / 2.0f, pitch / 3.0f, step, maxSteps, 1.0);
                return;
            }

            if (!room && localRandom.NextInt(4) == 0)
                continue;

            double dx = x - centerX;
            double dz = z - centerZ;
            double remaining = maxSteps - step;
            double maxReach = width + 2.0f + 16.0f;
            if (dx * dx + dz * dz - remaining * remaining > maxReach * maxReach)
                return;

            if (x < centerX - 16.0 - radiusXZ * 2.0 || z < centerZ - 16.0 - radiusXZ * 2.0 ||
                x > centerX + 16.0 + radiusXZ * 2.0 || z > centerZ + 16.0 + radiusXZ * 2.0)
                continue;

            int minX = Mathf.FloorToInt((float)(x - radiusXZ)) - chunkX * 16 - 1;
            int maxX = Mathf.FloorToInt((float)(x + radiusXZ)) - chunkX * 16 + 1;
            int minY = Mathf.FloorToInt((float)(y - radiusY)) - 1;
            int maxY = Mathf.FloorToInt((float)(y + radiusY)) + 1;
            int minZ = Mathf.FloorToInt((float)(z - radiusXZ)) - chunkZ * 16 - 1;
            int maxZ = Mathf.FloorToInt((float)(z + radiusXZ)) - chunkZ * 16 + 1;

            minX = Mathf.Max(minX, 0); maxX = Mathf.Min(maxX, 16);
            minY = Mathf.Max(minY, 1); maxY = Mathf.Min(maxY, 120);
            minZ = Mathf.Max(minZ, 0); maxZ = Mathf.Min(maxZ, 16);

            bool hitsWater = false;
            for (int lx = minX; !hitsWater && lx < maxX; lx++)
            for (int lz = minZ; !hitsWater && lz < maxZ; lz++)
            for (int ly = maxY + 1; !hitsWater && ly >= minY - 1; ly--)
            {
                if (ly >= 0 && ly < Beta173Terrain.TerrainHeight &&
                    chunk.GetBlock(new Vector3Int(lx, ly, lz)).type == BlockType.Water)
                    hitsWater = true;

                if (ly != minY - 1 && lx != minX && lx != maxX - 1 && lz != minZ && lz != maxZ - 1)
                    ly = minY;
            }

            if (hitsWater)
                continue;

            for (int lx = minX; lx < maxX; lx++)
            {
                double nx = ((lx + chunkX * 16) + 0.5 - x) / radiusXZ;
                for (int lz = minZ; lz < maxZ; lz++)
                {
                    double nz = ((lz + chunkZ * 16) + 0.5 - z) / radiusXZ;
                    bool foundGrass = false;
                    if (nx * nx + nz * nz >= 1.0)
                        continue;

                    for (int ly = maxY - 1; ly >= minY; ly--)
                    {
                        double ny = (ly + 0.5 - y) / radiusY;
                        if (ny <= -0.7 || nx * nx + ny * ny + nz * nz >= 1.0)
                            continue;

                        var pos = new Vector3Int(lx, ly, lz);
                        BlockType block = chunk.GetBlock(pos).type;
                        if (block == BlockType.Grass)
                            foundGrass = true;

                        if (block == BlockType.Stone || block == BlockType.Dirt || block == BlockType.Grass)
                        {
                            chunk.SetBlock(pos, ly < 10 ? BlockType.Lava : BlockType.Air);
                            if (foundGrass && ly > 0)
                            {
                                var below = new Vector3Int(lx, ly - 1, lz);
                                if (chunk.GetBlock(below).type == BlockType.Dirt)
                                    chunk.SetBlock(below, BlockType.Grass);
                            }
                        }
                    }
                }
            }

            if (room)
                break;
        }
    }
}
