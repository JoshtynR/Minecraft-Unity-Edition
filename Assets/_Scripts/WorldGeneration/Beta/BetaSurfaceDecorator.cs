using UnityEngine;

/// <summary>
/// Replaces exposed stone with classic-style top/filler blocks without flattening
/// 3D density terrain. Gravel is intentionally omitted until BlockType supports it.
/// </summary>
public static class BetaSurfaceDecorator
{
    public static void DecorateColumn(ChunkData data, int x, int z, Vector3Int seedOffset)
    {
        int worldX = data.worldPos.x + x;
        int worldZ = data.worldPos.z + z;

        // Deterministic column random for the irregular Beta bedrock floor.
        long columnSeed = unchecked(
            ((long)worldX * 341873128712L) ^
            ((long)worldZ * 132897987541L) ^
            ((long)seedOffset.x << 32) ^
            (uint)seedOffset.z);
        var random = new JavaRandom(columnSeed);

        // Beta's bottom bedrock is uneven rather than a perfectly flat layer.
        for (int y = 0; y < 5 && y < data.worldRef.worldHeight; y++)
        {
            if (y <= random.NextInt(5))
                data.SetBlock(new Vector3Int(x, y, z), BlockType.Bedrock);
        }

        int depthRemaining = -1;
        BlockType filler = BlockType.Dirt;

        // Walk downward so every stone surface exposed to air/water gets a proper
        // top and filler layer while overhangs and internal voids remain intact.
        for (int y = Beta173Terrain.TerrainHeight - 1; y >= 0; y--)
        {
            var pos = new Vector3Int(x, y, z);
            BlockType current = data.GetBlock(pos).type;

            if (current == BlockType.Air || current == BlockType.Water)
            {
                depthRemaining = -1;
                continue;
            }

            if (current != BlockType.Stone)
                continue;

            if (depthRemaining == -1)
            {
                bool underwater = y < Beta173Terrain.SeaLevel;
                bool beachBand = y >= Beta173Terrain.SeaLevel - 2 &&
                                 y <= Beta173Terrain.SeaLevel + 1;

                BlockType top;
                int fillerDepth;

                if (underwater || beachBand)
                {
                    top = BlockType.Sand;
                    filler = BlockType.Sand;
                    fillerDepth = 3;
                }
                else
                {
                    top = BlockType.Grass;
                    filler = BlockType.Dirt;
                    // Classic surface thickness wanders around a few blocks.
                    fillerDepth = 3 + random.NextInt(2);
                }

                data.SetBlock(pos, top);
                depthRemaining = fillerDepth;
            }
            else if (depthRemaining > 0)
            {
                data.SetBlock(pos, filler);
                depthRemaining--;
            }
        }
    }
}
