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
    [Tooltip("Wheat growth stages 0 through 7, on the single crop block.")]
    public Texture2D[] growthStages = new Texture2D[8];
    [NonSerialized] public int[] growthStageIndices;
    public int GrowthStageIndex(int age) => growthStageIndices != null && growthStageIndices.Length == 8 ? growthStageIndices[age & 7] : FaceIndex(Direction.backwards);
    public Texture2D top;
    public Texture2D bottom;
    public Texture2D side;
    // Optional local-space overrides. Existing Top/Bottom/Side assignments stay valid.
    public Texture2D front, back, left, right;
    // Optional part images; the inspector exposes only this block's model slots.
    [HideInInspector] public List<SpecialBlockTexture> specialTextures = new List<SpecialBlockTexture>();
    [NonSerialized] private int[] specialIndices;
    public void ResetPartIndices() { specialIndices=new int[Enum.GetValues(typeof(BlockTexturePart)).Length]; }
    public void SetPartIndex(BlockTexturePart part,int index)
    {
        if(specialIndices==null)ResetPartIndices();
        int p=(int)part;if(p>=0&&p<specialIndices.Length)specialIndices[p]=index;
    }
    public int PartIndex(BlockTexturePart part,int fallback=0)
    {
        int p=(int)part;
        return specialIndices!=null&&p>=0&&p<specialIndices.Length&&specialIndices[p]!=0?specialIndices[p]:fallback;
    }
    public int FaceIndex(BlockType type,Direction direction)
    {
        int fallback=FaceIndex(direction);
        if(type==BlockType.Water||type==BlockType.Lava)
            return PartIndex(direction==Direction.up||direction==Direction.down?BlockTexturePart.FluidStill:BlockTexturePart.FluidFlow,fallback);
        return fallback;
    }
    [Tooltip("Treat face images as animation sheets. Static images remain unchanged when disabled.")]
    public bool animateFaces;
    [Range(1, 64)] public int animationColumns = 1;
    [Tooltip("Zero detects square frames from the sheet width. Frames play left-to-right, top-to-bottom.")]
    [Range(0, 256)] public int animationRows;
    [Tooltip("Zero uses every grid cell. Set a smaller count to exclude unused trailing cells.")]
    [Range(0, 4096)] public int animationFrameCount;
    [Range(0, 120)] public float animationFramesPerSecond = 20;

    // Assigned on the main thread before chunk workers start. No Unity object
    // access is needed when a worker builds UVs. Zero is the missing-image layer.
    [NonSerialized] public int topIndex, bottomIndex, sideIndex;
    [NonSerialized] public int frontIndex, backIndex, leftIndex, rightIndex;

    public int FaceIndex(Direction direction)
    {
        int surface=PartIndex(BlockTexturePart.Surface);
        if(surface!=0)return surface;
        switch (direction)
        {
            case Direction.up: return topIndex;
            case Direction.down: return bottomIndex;
            case Direction.backwards: return frontIndex != 0 ? frontIndex : sideIndex;
            case Direction.forwards: return backIndex != 0 ? backIndex : sideIndex;
            case Direction.left: return leftIndex != 0 ? leftIndex : sideIndex;
            case Direction.right: return rightIndex != 0 ? rightIndex : sideIndex;
            default: return sideIndex;
        }
    }
}
