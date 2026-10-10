using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;

// Compile alongside the actual generator files. No duplicated C# generator or Unity scene needed.
public static class BetaParityCheck
{
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Minecraft/Validate Beta 1.7.3 Parity")]
    public static void ValidateInEditor()
    {
        int failures = Run(Path.Combine(UnityEngine.Application.dataPath, "../Tools/BetaParity/fixtures.tsv"));
        if (failures == 0) UnityEngine.Debug.Log("Beta parity passed: 56 cases, 1064 stage checks. Population is not covered.");
        else UnityEngine.Debug.LogError("Beta parity failed: " + failures + " stage mismatches. See detailed test output.");
    }
#endif

    public static int Run(string manifestPath, string outputDirectory = null)
    {
        var groups = new Dictionary<string, List<string[]>>();
        foreach (string line in File.ReadAllLines(manifestPath))
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line)) continue;
            string[] parts = line.Split('\t');
            string key = parts[0] + "_" + parts[1] + "_" + parts[2];
            if (!groups.ContainsKey(key)) groups[key] = new List<string[]>();
            groups[key].Add(parts);
        }
        if (outputDirectory != null) Directory.CreateDirectory(outputDirectory);
        int failures = 0, checks = 0;
        foreach (var entry in groups)
        {
            string[] first = entry.Value[0];
            long seed = long.Parse(first[0], CultureInfo.InvariantCulture);
            int x = int.Parse(first[1]), z = int.Parse(first[2]);
            var terrain = new Beta173Terrain(seed);
            var stages = new Dictionary<string, byte[]>();
            var blocks = terrain.GenerateRawTerrain(x * 16, z * 16,
                (name, values) => stages[name] = DoubleBytes(values));
            var biomes = terrain.GenerateBiomeRegion(x * 16, z * 16, 16, 16);
            var biomeBytes = new byte[biomes.Length];
            for (int i = 0; i < biomes.Length; i++) biomeBytes[i] = (byte)biomes[i];
            stages["biomes"] = biomeBytes;
            terrain.GenerateSurfaceNoise(x * 16, z * 16, out double[] sand, out double[] gravel, out double[] stone);
            stages["sand"] = DoubleBytes(sand); stages["gravel"] = DoubleBytes(gravel); stages["stoneDepth"] = DoubleBytes(stone);
            var treeNoise = new double[25];
            for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++) treeNoise[i * 5 + j] = terrain.GetTreeCountNoise(x * 16 + i, z * 16 + j);
            stages["treeCount"] = DoubleBytes(treeNoise);
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                var random = new JavaRandom(seed);
                int[] bounds = { 1, 2, 3, 5, 16, 255, 256, 1073741825, 2147483647 };
                for (int i = 0; i < 64; i++)
                {
                    writer.Write(random.NextInt()); writer.Write(random.NextInt(bounds[i % bounds.Length]));
                    writer.Write(random.NextLong()); writer.Write(random.NextFloat()); writer.Write(random.NextDouble());
                }
                stages["random"] = stream.ToArray();
            }
            string[] seedTexts = { "1", "+42", "-1446162294", "9223372036854775807", "-9223372036854775808", "9223372036854775808", " 42 ", "1,000", "Glacier", "🧱", "-0", "+", "-" };
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                foreach (string text in seedTexts) writer.Write(MinecraftSeed.Parse(text));
                stages["seedParsing"] = stream.ToArray();
            }
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int unityChunkX = BetaCoordinateSpace.UnityChunkToBetaChunkX(x);
                int unityChunkZ = BetaCoordinateSpace.UnityChunkToBetaChunkZ(z);
                for (int i = 0; i < 16; i++)
                for (int j = 0; j < 16; j++)
                {
                    writer.Write(unityChunkX * 16 + BetaCoordinateSpace.BetaLocalToUnityLocalX(i));
                    writer.Write(unityChunkZ * 16 + BetaCoordinateSpace.BetaLocalToUnityLocalZ(j));
                }
                stages["coordinateMapping"] = stream.ToArray();
            }
            stages["raw"] = BlockBytes(blocks);
            BetaSurfaceDecorator.DecorateChunk(blocks, terrain, x, z);
            stages["surface"] = BlockBytes(blocks);
            new Beta173Caves(seed).Generate(blocks, x, z);
            stages["caves"] = BlockBytes(blocks);
            foreach (string[] expected in entry.Value)
            {
                checks++;
                byte[] bytes = stages[expected[3]];
                string actual;
                using (var sha = SHA256.Create()) actual = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
                if (outputDirectory != null) File.WriteAllBytes(Path.Combine(outputDirectory, entry.Key + "-" + expected[3] + ".bin"), bytes);
                if (actual != expected[4])
                {
                    failures++;
                    string message = "FAIL " + entry.Key + " " + expected[3] + " expected=" + expected[4] + " actual=" + actual;
#if UNITY_EDITOR
                    UnityEngine.Debug.LogError(message);
#else
                    Console.WriteLine(message);
#endif
                }
            }
        }
        Console.WriteLine(groups.Count + " cases, " + checks + " stage checks, " + failures + " failures.");
        return failures;
    }

    public static byte[] DoubleBytes(double[] values)
    {
        var bytes = new byte[values.Length * 8];
        for (int i = 0; i < values.Length; i++)
        {
            byte[] element = BitConverter.GetBytes(values[i]);
            if (!BitConverter.IsLittleEndian) Array.Reverse(element);
            Array.Copy(element, 0, bytes, i * 8, 8);
        }
        return bytes;
    }

    public static byte[] BlockBytes(BlockType[,,] blocks)
    {
        var bytes = new byte[32768];
        for (int x = 0; x < 16; x++)
        for (int z = 0; z < 16; z++)
        for (int y = 0; y < 128; y++)
            bytes[(x * 16 + z) * 128 + y] = Id(blocks[x, y, z]);
        return bytes;
    }

    private static byte Id(BlockType block)
    {
        switch (block)
        {
            case BlockType.Air: return 0;
            case BlockType.Stone: return 1;
            case BlockType.Grass: return 2;
            case BlockType.Dirt: return 3;
            case BlockType.Bedrock: return 7;
            case BlockType.Water: return 9;
            case BlockType.Lava: return 11;
            case BlockType.Sand: return 12;
            case BlockType.Gravel: return 13;
            case BlockType.Sandstone: return 24;
            case BlockType.Ice: return 79;
            default: throw new InvalidOperationException("Unexpected parity block " + block);
        }
    }

#if !UNITY_EDITOR
    public static int Main(string[] args)
    {
        return Run(args[0], args.Length > 1 ? args[1] : null) == 0 ? 0 : 1;
    }
#endif
}
