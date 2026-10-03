using UnityEngine;

[CreateAssetMenu(fileName = "Terrain Noise Settings", menuName = "Noise/Terrain Noise Settings")]
public class TerrainNoiseSettings : ScriptableObject
{
    [Header("General")]
    public bool useLayeredTerrain = true;
    [Range(0.1f, 0.9f)] public float baseHeightPercent = 0.48f;

    [Header("Continental")]
    public float continentalScale = 0.0015f;
    public float continentalHeight = 10f;

    [Header("Rolling Hills")]
    public float hillScale = 0.006f;
    public int hillOctaves = 3;
    [Range(0f, 1f)] public float hillPersistence = 0.5f;
    public float hillHeight = 14f;

    [Header("Mountains")]
    public float mountainMaskScale = 0.0018f;
    [Range(0f, 1f)] public float mountainStart = 0.62f;
    public float mountainScale = 0.004f;
    public float mountainHeight = 35f;

    [Header("Detail")]
    public float detailScale = 0.025f;
    public float detailHeight = 2f;
}
