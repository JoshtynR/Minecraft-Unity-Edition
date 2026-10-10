using UnityEngine;

public class WaterBlock : Block
{
    public WaterBlock(Vector3Int position, ChunkSection section) : base(BlockType.Water, position, section) { }
    public WaterBlock(Block block) : base(block) { }
    public override void OnBlockUpdate()
    {
        if (section != null && chunkData.worldRef.IsWorldCreated)
            chunkData.worldRef.NotifyWaterBlockChanged(globalWorldPosition);
    }
}
