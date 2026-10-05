using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Temporary parity instrumentation. Dumps the generated Beta chunk (0,0)
/// in the original 16x16x128 Blocks ordering so it can be compared byte-for-byte
/// with the user's real Beta 1.7.3 MCRegion save.
/// </summary>
public static class BetaParityDump
{
    private static bool dumped;

    public static void DumpChunk00(ChunkData data, int betaChunkX, int betaChunkZ)
    {
        if (dumped || betaChunkX != 0 || betaChunkZ != 0) return;
        dumped = true;

        byte[] blocks = new byte[16 * 16 * 128];
        int unknown = 0;

        for (int betaX = 0; betaX < 16; betaX++)
        for (int betaZ = 0; betaZ < 16; betaZ++)
        {
            int unityX = BetaCoordinateSpace.BetaLocalToUnityLocalX(betaX);
            int unityZ = BetaCoordinateSpace.BetaLocalToUnityLocalZ(betaZ);

            for (int y = 0; y < 128; y++)
            {
                BlockType type = data.GetBlock(new Vector3Int(unityX, y, unityZ)).type;
                byte id = ToBetaBlockId(type);
                if (id == 255) unknown++;
                blocks[(betaX * 16 + betaZ) * 128 + y] = id;
            }
        }

        string path = Path.Combine(Application.dataPath, "../beta-parity-chunk-0-0.txt");
        string payload = Convert.ToBase64String(blocks);
        File.WriteAllText(path,
            "seed=" + data.worldRef.betaWorldSeed + "\n" +
            "betaChunk=0,0\n" +
            "layout=(x*16+z)*128+y\n" +
            "unknownBlocks=" + unknown + "\n" +
            payload + "\n");

        Debug.Log($"[BetaParity] Wrote chunk (0,0) dump to {path}. Unknown blocks: {unknown}");
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
            default: return 255;
        }
    }
}
