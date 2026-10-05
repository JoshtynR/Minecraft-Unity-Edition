using System;
using UnityEngine;

public sealed class Beta173Caves
{
    private const int Range = 8;
    private const float Pi = 3.1415927f;
    private const float HalfPi = 1.5707964f;
    private readonly long worldSeed;

    public Beta173Caves(long worldSeed) { this.worldSeed = worldSeed; }

    public void Generate(ChunkData chunk, int targetChunkX, int targetChunkZ, bool reflectLocalX = false)
    {
        var random = new JavaRandom(worldSeed);
        long seedX = MakeOdd(random.NextLong());
        long seedZ = MakeOdd(random.NextLong());
        for (int sourceChunkX = targetChunkX - Range; sourceChunkX <= targetChunkX + Range; sourceChunkX++)
        for (int sourceChunkZ = targetChunkZ - Range; sourceChunkZ <= targetChunkZ + Range; sourceChunkZ++)
        {
            random.SetSeed(unchecked((long)sourceChunkX * seedX + (long)sourceChunkZ * seedZ) ^ worldSeed);
            GenerateFromSource(chunk, random, sourceChunkX, sourceChunkZ, targetChunkX, targetChunkZ, reflectLocalX);
        }
    }

    private static long MakeOdd(long value) => unchecked(value / 2L * 2L + 1L);
    private static int UnityX(int betaX, bool reflect) => reflect ? 15 - betaX : betaX;

    private void GenerateFromSource(ChunkData chunk, JavaRandom random, int sourceChunkX, int sourceChunkZ,
        int targetChunkX, int targetChunkZ, bool reflectLocalX)
    {
        int caveCount = random.NextInt(random.NextInt(random.NextInt(40) + 1) + 1);
        if (random.NextInt(15) != 0) caveCount = 0;
        for (int i = 0; i < caveCount; i++)
        {
            double x = sourceChunkX * 16 + random.NextInt(16);
            double y = random.NextInt(random.NextInt(120) + 8);
            double z = sourceChunkZ * 16 + random.NextInt(16);
            int tunnels = 1;
            if (random.NextInt(4) == 0)
            {
                CarveTunnel(chunk, random, targetChunkX, targetChunkZ, x, y, z,
                    1.0f + random.NextFloat() * 6.0f, 0.0f, 0.0f, -1, -1, 0.5, reflectLocalX);
                tunnels += random.NextInt(4);
            }
            for (int tunnel = 0; tunnel < tunnels; tunnel++)
            {
                float yaw = random.NextFloat() * Pi * 2.0f;
                float pitch = (random.NextFloat() - 0.5f) * 2.0f / 8.0f;
                float width = random.NextFloat() * 2.0f + random.NextFloat();
                CarveTunnel(chunk, random, targetChunkX, targetChunkZ, x, y, z,
                    width, yaw, pitch, 0, 0, 1.0, reflectLocalX);
            }
        }
    }

    private void CarveTunnel(ChunkData chunk, JavaRandom parentRandom, int chunkX, int chunkZ,
        double x, double y, double z, float width, float yaw, float pitch,
        int step, int maxSteps, double verticalScale, bool reflectLocalX)
    {
        double centerX = chunkX * 16 + 8;
        double centerZ = chunkZ * 16 + 8;
        float yawVelocity = 0.0f, pitchVelocity = 0.0f;
        var localRandom = new JavaRandom(parentRandom.NextLong());
        if (maxSteps <= 0)
        {
            int distance = Range * 16 - 16;
            maxSteps = distance - localRandom.NextInt(distance / 4);
        }
        bool room = false;
        if (step == -1) { step = maxSteps / 2; room = true; }
        int branchStep = localRandom.NextInt(maxSteps / 2) + maxSteps / 4;
        bool gentlePitch = localRandom.NextInt(6) == 0;

        for (; step < maxSteps; step++)
        {
            float angle = (float)step * Pi / (float)maxSteps;
            double radiusXZ = 1.5 + BetaMathHelper.Sin(angle) * width;
            double radiusY = radiusXZ * verticalScale;
            float cosPitch = BetaMathHelper.Cos(pitch);
            float sinPitch = BetaMathHelper.Sin(pitch);
            x += BetaMathHelper.Cos(yaw) * cosPitch;
            y += sinPitch;
            z += BetaMathHelper.Sin(yaw) * cosPitch;
            pitch *= gentlePitch ? 0.92f : 0.7f;
            pitch += pitchVelocity * 0.1f;
            yaw += yawVelocity * 0.1f;
            pitchVelocity *= 0.9f;
            yawVelocity *= 0.75f;
            pitchVelocity += (localRandom.NextFloat() - localRandom.NextFloat()) * localRandom.NextFloat() * 2.0f;
            yawVelocity += (localRandom.NextFloat() - localRandom.NextFloat()) * localRandom.NextFloat() * 4.0f;

            if (!room && step == branchStep && width > 1.0f)
            {
                CarveTunnel(chunk, localRandom, chunkX, chunkZ, x, y, z,
                    localRandom.NextFloat() * 0.5f + 0.5f, yaw - HalfPi,
                    pitch / 3.0f, step, maxSteps, 1.0, reflectLocalX);
                CarveTunnel(chunk, localRandom, chunkX, chunkZ, x, y, z,
                    localRandom.NextFloat() * 0.5f + 0.5f, yaw + HalfPi,
                    pitch / 3.0f, step, maxSteps, 1.0, reflectLocalX);
                return;
            }
            if (!room && localRandom.NextInt(4) == 0) continue;

            double dx = x - centerX, dz = z - centerZ;
            double remaining = maxSteps - step;
            // Beta performs these additions as floats before widening to double.
            double maxReach = (double)(width + 2.0f + 16.0f);
            if (dx * dx + dz * dz - remaining * remaining > maxReach * maxReach) return;
            if (x < centerX - 16.0 - radiusXZ * 2.0 || z < centerZ - 16.0 - radiusXZ * 2.0 ||
                x > centerX + 16.0 + radiusXZ * 2.0 || z > centerZ + 16.0 + radiusXZ * 2.0) continue;

            int minX = Math.Max(BetaMathHelper.Floor(x - radiusXZ) - chunkX * 16 - 1, 0);
            int maxX = Math.Min(BetaMathHelper.Floor(x + radiusXZ) - chunkX * 16 + 1, 16);
            int minY = Math.Max(BetaMathHelper.Floor(y - radiusY) - 1, 1);
            int maxY = Math.Min(BetaMathHelper.Floor(y + radiusY) + 1, 120);
            int minZ = Math.Max(BetaMathHelper.Floor(z - radiusXZ) - chunkZ * 16 - 1, 0);
            int maxZ = Math.Min(BetaMathHelper.Floor(z + radiusXZ) - chunkZ * 16 + 1, 16);

            bool hitsWater = false;
            for (int bx = minX; !hitsWater && bx < maxX; bx++)
            for (int bz = minZ; !hitsWater && bz < maxZ; bz++)
            for (int by = maxY + 1; !hitsWater && by >= minY - 1; by--)
            {
                int ux = UnityX(bx, reflectLocalX);
                if (by >= 0 && by < 128 && chunk.GetBlock(new Vector3Int(ux, by, bz)).type == BlockType.Water)
                    hitsWater = true;
                if (by != minY - 1 && bx != minX && bx != maxX - 1 && bz != minZ && bz != maxZ - 1)
                    by = minY;
            }
            if (hitsWater) continue;

            for (int bx = minX; bx < maxX; bx++)
            {
                double nx = ((bx + chunkX * 16) + 0.5 - x) / radiusXZ;
                for (int bz = minZ; bz < maxZ; bz++)
                {
                    double nz = ((bz + chunkZ * 16) + 0.5 - z) / radiusXZ;
                    bool foundGrass = false;
                    if (nx * nx + nz * nz >= 1.0) continue;

                    // Beta's cave routine initializes its linear block-array cursor at maxY,
                    // while the geometric Y loop starts at maxY - 1. Preserve that one-block
                    // cursor offset instead of "correcting" it; it affects exact cave blocks.
                    for (int by = maxY - 1; by >= minY; by--)
                    {
                        double ny = (by + 0.5 - y) / radiusY;
                        if (ny <= -0.7 || nx * nx + ny * ny + nz * nz >= 1.0) continue;

                        int ux = UnityX(bx, reflectLocalX);
                        int blockY = by + 1;
                        var pos = new Vector3Int(ux, blockY, bz);
                        BlockType block = chunk.GetBlock(pos).type;
                        if (block == BlockType.Grass) foundGrass = true;
                        if (block == BlockType.Stone || block == BlockType.Dirt || block == BlockType.Grass)
                        {
                            if (by < 10)
                            {
                                chunk.SetBlock(pos, BlockType.Lava);
                            }
                            else
                            {
                                chunk.SetBlock(pos, BlockType.Air);
                                if (foundGrass)
                                {
                                    var below = new Vector3Int(ux, blockY - 1, bz);
                                    if (chunk.GetBlock(below).type == BlockType.Dirt)
                                        chunk.SetBlock(below, BlockType.Grass);
                                }
                            }
                        }
                    }
                }
            }
            if (room) break;
        }
    }
}
