using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class BiomeGenerator : MonoBehaviour
{
    public NoiseSettings settings;
    public TerrainNoiseSettings terrainSettings;

    public DomainWarping domainWarping;

    public bool useWarping = true;

    public bool enableLodes = true;

    public BlockLayerHandler startLayerHandler;
    
    public TreeNoiseGenerator treeNoiseGenerator;

    public List<BlockLayerHandler> featureLayerHandlers;
    
    public List<Lode> lodes;
    
    public int extraTerrainHeightPercentage = 0;

    public ChunkData ProcessBetaDensityColumn(ChunkData data, int x, int z, Vector3Int mapSeedOffset)
    {
        int topSolidY = -1;
        var localPos = new Vector3Int(x, 0, z);
        int worldX = data.worldPos.x + x;
        int worldZ = data.worldPos.z + z;

        // First pass: true 3D density. This establishes stone/air without
        // allowing the legacy heightmap handlers to fill overhangs back in.
        for (int y = 0; y < data.worldRef.worldHeight; y++)
        {
            localPos.y = y;
            float density = BetaTerrainDensity.Sample(
                worldX, y, worldZ, data.worldRef.worldHeight, mapSeedOffset);

            if (density > 0f)
            {
                data.SetBlock(localPos, BlockType.Stone);
                topSolidY = y;
            }
            else
            {
                data.SetBlock(localPos, BlockType.Air);
            }
        }

        // Surface decoration is deliberately conservative for the first pass:
        // only the highest exposed solid block gets the biome's existing
        // surface/subsurface treatment. Internal overhang geometry remains intact.
        if (topSolidY >= 0)
        {
            var worldPos = new Vector3Int(worldX, topSolidY, worldZ);
            localPos.y = topSolidY;
            startLayerHandler.Handle(data, worldPos, localPos, topSolidY, mapSeedOffset);
        }

        return data;
    }

    public ChunkData ProcessChunkColumn(ChunkData data, int x, int z, Vector3Int mapSeedOffset, int? terrainHeightNoise)
    {
        settings.worldSeedOffset = mapSeedOffset;
        
        var groundPos = terrainHeightNoise ?? GetSurfaceHeightNoise(data.worldPos.x + x, data.worldPos.z + z,data.worldRef.worldHeight);

        var worldPos = new Vector3Int(data.worldPos.x + x, 0, data.worldPos.z + z);
        var localPos = new Vector3Int(x, 0, z);

        for (var y = 0; y < data.worldRef.worldHeight; y++)
        {
            worldPos.y = y;
            localPos.y = y;

            // if (MyNoise.OctavePerlin3D(worldPos, settings))
            // {
            //     data.SetBlock(localPos, BlockType.Stone);
            // }
            // else
            // {
            //     data.SetBlock(localPos, BlockType.Air);
            // }
            //
            startLayerHandler.Handle(data, worldPos, localPos, groundPos, mapSeedOffset);
        }


        return data;
    }


    public void ProcessFeatures(ChunkData data, int x, int z, Vector3Int mapSeedOffset, int? terrainHeightNoise)
    {
        settings.worldSeedOffset = mapSeedOffset;
        var groundPos = terrainHeightNoise ?? GetSurfaceHeightNoise(data.worldPos.x + x, data.worldPos.z + z,data.worldRef.worldHeight);

        var worldPos = new Vector3Int(data.worldPos.x + x, 0, data.worldPos.z + z);
        var localPos = new Vector3Int(x, 0, z);

        if (enableLodes)
        {
            // Process lodes
            foreach (var lode in lodes)
            {
                for (var y = lode.maxHeight - 1; y >= lode.minHeight; y--)
                {
                    worldPos.y = y;
                    localPos.y = y;
                    
                    if (data.GetBlock(localPos).type is BlockType.Air or BlockType.Water)
                    {
                        continue;
                    }
                    
                    lode.noiseSettings.worldSeedOffset = mapSeedOffset;

                    if (MyNoise.OctavePerlin3D(worldPos, lode.noiseSettings, lode.threshold))
                    {
                        if (data.GetBlock(localPos).type == BlockType.Grass && data.GetBlock(localPos+Vector3Int.down).type == BlockType.Dirt)
                        {
                            data.SetBlock(localPos+Vector3Int.down, BlockType.Grass);
                        }
                        data.SetBlock(localPos, lode.blockType);
                    }
                }
            }
        }
        
        foreach (var layer in featureLayerHandlers)
        {
            for (var y = 0; y < data.worldRef.worldHeight; y++)
            {
                worldPos.y = y;
                localPos.y = y;
                
                layer.Handle(data, worldPos,localPos, groundPos, mapSeedOffset);
            }
        }
    }

    public int GetSurfaceHeightNoise(int x, int z, int worldHeight)
    {
        if (terrainSettings == null || !terrainSettings.useLayeredTerrain)
        {
            float terrainHeight;
            if (useWarping)
            {
                terrainHeight = domainWarping.GenerateDomainNoise(x, z, settings);
            }
            else
            {
                terrainHeight = MyNoise.OctavePerlin(x, z, settings);
            }

            terrainHeight = MyNoise.Redistribution(terrainHeight, settings);
            return (int)Mathf.Lerp(
                worldHeight * (extraTerrainHeightPercentage / 100f),
                worldHeight - 1,
                terrainHeight);
        }

        float seedX = settings.worldSeedOffset.x + settings.offset.x;
        float seedZ = settings.worldSeedOffset.z + settings.offset.z;

        float continental = Mathf.PerlinNoise(
            (x + seedX) * terrainSettings.continentalScale,
            (z + seedZ) * terrainSettings.continentalScale);
        continental = (continental - 0.5f) * 2f;

        float hills = FractalPerlin(
            x + seedX,
            z + seedZ,
            terrainSettings.hillScale,
            terrainSettings.hillOctaves,
            terrainSettings.hillPersistence);
        hills = (hills - 0.5f) * 2f;

        float mountainMask = Mathf.PerlinNoise(
            (x + seedX + 1731f) * terrainSettings.mountainMaskScale,
            (z + seedZ - 947f) * terrainSettings.mountainMaskScale);
        mountainMask = Mathf.InverseLerp(
            terrainSettings.mountainStart,
            1f,
            mountainMask);
        mountainMask = mountainMask * mountainMask;

        float mountainShape = FractalPerlin(
            x + seedX + 3917f,
            z + seedZ + 2179f,
            terrainSettings.mountainScale,
            4,
            0.5f);
        mountainShape = Mathf.Abs(mountainShape - 0.5f) * 2f;
        mountainShape = 1f - mountainShape;
        mountainShape *= mountainShape;

        float detail = Mathf.PerlinNoise(
            (x + seedX - 811f) * terrainSettings.detailScale,
            (z + seedZ + 1297f) * terrainSettings.detailScale);
        detail = (detail - 0.5f) * 2f;

        float height =
            worldHeight * terrainSettings.baseHeightPercent +
            continental * terrainSettings.continentalHeight +
            hills * terrainSettings.hillHeight +
            mountainMask * mountainShape * terrainSettings.mountainHeight +
            detail * terrainSettings.detailHeight;

        return Mathf.Clamp(Mathf.RoundToInt(height), 1, worldHeight - 1);
    }

    private static float FractalPerlin(
        float x,
        float z,
        float scale,
        int octaves,
        float persistence)
    {
        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxAmplitude = 0f;

        for (int i = 0; i < Mathf.Max(1, octaves); i++)
        {
            total += Mathf.PerlinNoise(
                x * scale * frequency,
                z * scale * frequency) * amplitude;

            maxAmplitude += amplitude;
            amplitude *= persistence;
            frequency *= 2f;
        }

        return maxAmplitude > 0f ? total / maxAmplitude : 0.5f;
    }

    public TreeData GenerateTreeData(ChunkData data, Vector3Int mapSeedOffset)
    {
        if(treeNoiseGenerator == null)
        {
            return new TreeData();
        }
        return treeNoiseGenerator.GenerateTreeData(data, mapSeedOffset);
    }
}

[System.Serializable]
public class Lode
{
    public BlockType blockType;
    public int minHeight;
    public int maxHeight;
    public float threshold;
    public NoiseSettings noiseSettings;
}

#if UNITY_EDITOR

[CustomEditor(typeof(BiomeGenerator))]
public class BiomeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var biomeGenerator = (BiomeGenerator)this.target;
        DrawDefaultInspector();
        EditorGUILayout.Space(20);
        var customEditor = Editor.CreateEditor(biomeGenerator.settings);
        customEditor.OnInspectorGUI();
    }
}
    
#endif
