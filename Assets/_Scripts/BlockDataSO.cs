using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Block Data", menuName = "Data/Block Data")]
public class BlockDataSO : ScriptableObject
{
    [Tooltip("Resolution used for rendering each face image. Smaller images are scaled with point filtering.")]
    [Range(16, 512)] public int faceTextureResolution = 32;
    public List<BlockTypeData> textureDataList;
}

[Serializable]
public class BlockTypeData
{
    public BlockType blockType;
    public TextureData textureData = new TextureData();
    public bool isTransparent = false;
    public bool generateCollider = true;
    [Range(0, 15)] public byte opacity = 15;
    [Range(0, 15)] public byte lightEmission = 0;
    public Color albedoTint = Color.white;
}

[Serializable]
public class TextureData
{
    public Texture2D top;
    public Texture2D bottom;
    public Texture2D side;

    // Assigned on the main thread before chunk workers start. No Unity object
    // access is needed when a worker builds UVs. Zero is the missing-image layer.
    [NonSerialized] public int topIndex, bottomIndex, sideIndex;

    public int FaceIndex(Direction direction)
    {
        switch (direction)
        {
            case Direction.up: return topIndex;
            case Direction.down: return bottomIndex;
            default: return sideIndex;
        }
    }
}
