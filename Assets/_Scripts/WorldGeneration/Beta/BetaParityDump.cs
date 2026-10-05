using System;
using System.IO;
using UnityEngine;

/// <summary>Temporary Beta 1.7.3 parity instrumentation for chunk (0,0).</summary>
public static class BetaParityDump
{
    public static void DumpRawChunk00(BlockType[,,] raw, long seed, int betaChunkX, int betaChunkZ)
    {
        if (betaChunkX != 0 || betaChunkZ != 0) return;
        byte[] blocks = new byte[16 * 16 * 128];
        int unknown = 0;
        for (int x = 0; x < 16; x++)
        for (int z = 0; z < 16; z++)
        for (int y = 0; y < 128; y++)
        {
            byte id = ToBetaBlockId(raw[x, y, z]);
            if (id == 255) unknown++;
            blocks[(x * 16 + z) * 128 + y] = id;
        }
        Write("raw", blocks, seed, unknown);
    }

    public static void DumpDataChunk00(ChunkData data, int betaChunkX, int betaChunkZ, string stage)
    {
        if (betaChunkX != 0 || betaChunkZ != 0) return;
        byte[] blocks = new byte[16 * 16 * 128];
        int unknown = 0;
        for (int betaX = 0; betaX < 16; betaX++)
        for (int betaZ = 0; betaZ < 16; betaZ++)
        {
            int unityX = BetaCoordinateSpace.BetaLocalToUnityLocalX(betaX);
            int unityZ = BetaCoordinateSpace.BetaLocalToUnityLocalZ(betaZ);
            for (int y = 0; y < 128; y++)
            {
                byte id = ToBetaBlockId(data.GetBlock(new Vector3Int(unityX, y, unityZ)).type);
                if (id == 255) unknown++;
                blocks[(betaX * 16 + betaZ) * 128 + y] = id;
            }
        }
        Write(stage, blocks, data.worldRef.betaWorldSeed, unknown);
    }

    private static void Write(string stage, byte[] blocks, long seed, int unknown)
    {
        string path = Path.Combine(Application.dataPath, "../beta-parity-" + stage + "-0-0.txt");
        File.WriteAllText(path,
            "seed=" + seed + "\n" +
            "betaChunk=0,0\n" +
            "stage=" + stage + "\n" +
            "layout=(x*16+z)*128+y\n" +
            "unknownBlocks=" + unknown + "\n" +
            Convert.ToBase64String(blocks) + "\n");
        Debug.Log("[BetaParity] Wrote " + stage + " chunk (0,0): " + path + ". Unknown blocks: " + unknown);
    }

    private static byte ToBetaBlockId(BlockType type)
    {
        switch (type)
        {
            case BlockType.Nothing:
            case BlockType.Air: return 0;
            case BlockType.Stone: return 1;
            case BlockType.Grass: return 2;
            case BlockType.Dirt: return 3;
            case BlockType.Bedrock: return 7;
            case BlockType.Water: return 9;
            case BlockType.Lava: return 11;
            case BlockType.Sand: return 12;
            case BlockType.Gravel: return 13;
            case BlockType.Log: return 17;
            case BlockType.Leaves: return 18;
            case BlockType.Sandstone: return 24;
            case BlockType.Ice: return 79;
            default: return 255;
        }
    }
}
