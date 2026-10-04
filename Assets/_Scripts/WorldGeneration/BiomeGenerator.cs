using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class BiomeGenerator : MonoBehaviour
{
    public NoiseSettings settings;
    public DomainWarping domainWarping;
    public bool useWarping = true;
    public bool enableLodes = true;
    public BlockLayerHandler startLayerHandler;
    public TreeNoiseGenerator treeNoiseGenerator;
    public List<BlockLayerHandler> featureLayerHandlers;
    public List<Lode> lodes;
    public int extraTerrainHeightPercentage = 0;

    public ChunkData ProcessBetaTerrainColumn(
        ChunkData data, int unityX, int unityZ, BlockType[,,] rawTerrain,
        int betaX, int betaZ)
    {
        var localPos = new Vector3Int(unityX, 0, unityZ);
        for (int y = 0; y < data.worldRef.worldHeight; y++)
        {
            localPos.y = y;
            BlockType block = y < Beta173Terrain.TerrainHeight
                ? rawTerrain[betaX, y, betaZ]
                : BlockType.Air;
            data.SetBlock(localPos, block);
        }
        return data;
    }

    public void ProcessFeatures(ChunkData data, int x, int z, Vector3Int mapSeedOffset, int? terrainHeightNoise)
    {
        settings.worldSeedOffset = mapSeedOffset;
        int groundPos = terrainHeightNoise ?? 64;
        var worldPos = new Vector3Int(data.worldPos.x + x, 0, data.worldPos.z + z);
        var localPos = new Vector3Int(x, 0, z);

        if (enableLodes)
        {
            foreach (var lode in lodes)
            {
                for (var y = lode.maxHeight - 1; y >= lode.minHeight; y--)
                {
                    worldPos.y = y;
                    localPos.y = y;
                    if (data.GetBlock(localPos).type is BlockType.Air or BlockType.Water) continue;
                    lode.noiseSettings.worldSeedOffset = mapSeedOffset;
                    if (MyNoise.OctavePerlin3D(worldPos, lode.noiseSettings, lode.threshold))
                        data.SetBlock(localPos, lode.blockType);
                }
            }
        }

        foreach (var layer in featureLayerHandlers)
        {
            for (var y = 0; y < data.worldRef.worldHeight; y++)
            {
                worldPos.y = y;
                localPos.y = y;
                layer.Handle(data, worldPos, localPos, groundPos, mapSeedOffset);
            }
        }
    }

    public TreeData GenerateTreeData(ChunkData data, Vector3Int mapSeedOffset)
    {
        return treeNoiseGenerator == null ? new TreeData() : treeNoiseGenerator.GenerateTreeData(data, mapSeedOffset);
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
        var biomeGenerator = (BiomeGenerator)target;
        DrawDefaultInspector();
        if (biomeGenerator.settings == null) return;
        EditorGUILayout.Space(20);
        var customEditor = Editor.CreateEditor(biomeGenerator.settings);
        customEditor.OnInspectorGUI();
    }
}
#endif
